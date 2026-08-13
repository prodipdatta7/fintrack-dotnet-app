using System.Security.Claims;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.BuildingBlocks.Behaviors;
using FinTrack.BuildingBlocks.Persistence;
using FinTrack.BuildingBlocks.Storage;
using FinTrack.Modules.Accounts;
using FinTrack.Modules.Budgets;
using FinTrack.Modules.Categories;
using FinTrack.Modules.Dashboard;
using FinTrack.Modules.Transactions;
using FinTrack.Modules.Users;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. Module Registrations
// ==========================================
builder.Services.AddUsersModule();
builder.Services.AddTransactionsModule();
builder.Services.AddCategoriesModule();
builder.Services.AddDashboardModule();
builder.Services.AddBudgetsModule();
builder.Services.AddAccountsModule();

// Register MediatR pipeline behaviors
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// ==========================================
// 2. Controller & Application Parts Setup
// ==========================================
builder.Services.AddControllers()
    .AddApplicationPart(typeof(FinTrack.Modules.Users.DependencyInjection).Assembly)
    .AddApplicationPart(typeof(FinTrack.Modules.Transactions.DependencyInjection).Assembly)
    .AddApplicationPart(typeof(FinTrack.Modules.Categories.DependencyInjection).Assembly)
    .AddApplicationPart(typeof(FinTrack.Modules.Dashboard.DependencyInjection).Assembly)
    .AddApplicationPart(typeof(FinTrack.Modules.Budgets.DependencyInjection).Assembly)
    .AddApplicationPart(typeof(FinTrack.Modules.Accounts.DependencyInjection).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FinTrack API", Version = "v1" });

    var schemeReference = new OpenApiSecuritySchemeReference("Bearer");

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT Bearer token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        { schemeReference, new List<string>() }
    });
});

// ==========================================
// 3. Infrastructure (MongoDB, Auth, CORS, MassTransit)
// ==========================================
builder.Services.AddMongoDb(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

var storageProvider = builder.Configuration["Storage:Provider"] ?? "Local";
if (string.Equals(storageProvider, "Gcs", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IFileStorageService, GcsFileStorageService>();
}
else
{
    builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
}

var corsOrigins = (builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .Where(static o => !string.IsNullOrWhiteSpace(o))
    .ToArray();

if (corsOrigins.Length == 0 && builder.Environment.IsDevelopment())
{
    corsOrigins = ["http://localhost:4200", "http://localhost:3000"];
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontendApp", policy =>
    {
        // Same-origin Firebase Hosting → Cloud Run rewrite needs no browser CORS.
        // When origins are configured (local ng serve without proxy, or direct Run URL), enable them.
        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            policy.SetIsOriginAllowed(_ => false)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

// MassTransit In-Memory Bus + MongoDB Outbox (Phase 1)
// The Mongo outbox wraps every publish in a Mongo transaction, which standalone servers
// (no replica set) reject with NotSupportedException — so it is config-gated and off in
// Development, where publishes go straight to the in-memory bus instead.
var useMongoOutbox = builder.Configuration.GetValue("MassTransit:UseMongoOutbox", true);
builder.Services.AddMassTransit(cfg =>
{
    cfg.AddConsumers(typeof(FinTrack.Modules.Categories.DependencyInjection).Assembly);
    cfg.AddConsumers(typeof(FinTrack.Modules.Accounts.DependencyInjection).Assembly);

    cfg.UsingInMemory((context, busConfig) =>
    {
        busConfig.ConfigureEndpoints(context);
    });

    if (useMongoOutbox)
    {
        cfg.AddMongoDbOutbox(outbox =>
        {
            outbox.ClientFactory(sp => sp.GetRequiredService<MongoDB.Driver.IMongoClient>());
            outbox.DatabaseFactory(sp => sp.GetRequiredService<MongoDB.Driver.IMongoDatabase>());
            outbox.DuplicateDetectionWindow = TimeSpan.FromSeconds(30);
            outbox.UseBusOutbox();
        });
    }
});

// Firebase ID-token Authentication + Default Deny Fallback Authorization
// Firebase Authentication is the sole identity provider: the API validates Google-signed ID
// tokens via OIDC discovery / JWKS and resolves the token's uid to the Mongo user id.
var firebaseProjectId = builder.Configuration["Firebase:ProjectId"];
if (string.IsNullOrWhiteSpace(firebaseProjectId))
{
    throw new InvalidOperationException(
        "Firebase:ProjectId must be configured. Firebase Authentication is the sole identity " +
        "provider (set env var Firebase__ProjectId or Secret Manager).");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    // Keep raw JWT claim names ("user_id", "email", "name", "sub") instead of letting
    // ASP.NET remap them to ClaimTypes.* URIs — Firebase ID-token handling depends on them.
    options.MapInboundClaims = false;
    options.Authority = $"https://securetoken.google.com/{firebaseProjectId}";
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = $"https://securetoken.google.com/{firebaseProjectId}",
        ValidateAudience = true,
        ValidAudience = firebaseProjectId,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
        // Signing keys are resolved from Google's JWKS via OIDC discovery — no symmetric key.
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            try
            {
                var principal = context.Principal!;
                var firebaseUid = principal.FindFirst("user_id")?.Value
                    ?? principal.FindFirst("sub")?.Value
                    ?? throw new SecurityTokenException("Missing Firebase uid in token.");

                var email = principal.FindFirst("email")?.Value;
                var name = principal.FindFirst("name")?.Value;

                DateTimeOffset? authTime = null;
                var authTimeClaim = principal.FindFirst("auth_time")?.Value;
                if (long.TryParse(authTimeClaim, out var authTimeUnix))
                    authTime = DateTimeOffset.FromUnixTimeSeconds(authTimeUnix);

                var resolver = context.HttpContext.RequestServices
                    .GetRequiredService<FinTrack.Modules.Users.Services.IFirebaseUserResolver>();

                var mongoId = await resolver.ResolveUserIdAsync(
                    firebaseUid, email, name, authTime, context.HttpContext.RequestAborted);

                var identity = (ClaimsIdentity)principal.Identity!;
                // Replace any prior NameIdentifier (Firebase sub) with the Mongo id for ICurrentUser.
                var existing = identity.FindFirst(ClaimTypes.NameIdentifier);
                if (existing is not null)
                    identity.RemoveClaim(existing);
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, mongoId));

                // MapInboundClaims=false keeps raw "email"; also add ClaimTypes.Email for ICurrentUser.
                if (!string.IsNullOrWhiteSpace(email) && identity.FindFirst(ClaimTypes.Email) is null)
                    identity.AddClaim(new Claim(ClaimTypes.Email, email));
            }
            catch (SecurityTokenException ex)
            {
                context.Fail(ex.Message);
            }
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ==========================================
// 4. HTTP Request Pipeline
// ==========================================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Behind Firebase Hosting / Cloud Load Balancer, honor X-Forwarded-* for HTTPS cookies.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
        | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseCors("AllowFrontendApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .AllowAnonymous();

app.MapControllers();

await app.RunAsync();

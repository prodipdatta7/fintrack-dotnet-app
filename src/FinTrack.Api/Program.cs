using System.Text;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.BuildingBlocks.Behaviors;
using FinTrack.BuildingBlocks.Persistence;
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// MassTransit In-Memory Bus + MongoDB Outbox (Phase 1)
builder.Services.AddMassTransit(cfg =>
{
    cfg.AddConsumers(typeof(FinTrack.Modules.Categories.DependencyInjection).Assembly);

    cfg.UsingInMemory((context, busConfig) =>
    {
        busConfig.ConfigureEndpoints(context);
    });

    cfg.AddMongoDbOutbox(outbox =>
    {
        outbox.ClientFactory(sp => sp.GetRequiredService<MongoDB.Driver.IMongoClient>());
        outbox.DatabaseFactory(sp => sp.GetRequiredService<MongoDB.Driver.IMongoDatabase>());
        outbox.DuplicateDetectionWindow = TimeSpan.FromSeconds(30);
        outbox.UseBusOutbox();
    });
});

// JWT Authentication + Default Deny Fallback Authorization
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"] ?? "SuperSecretKeyForLocalDev1234567890!";
var key = Encoding.UTF8.GetBytes(jwtSigningKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "FinTrack",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "FinTrack",
        ClockSkew = TimeSpan.Zero
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

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

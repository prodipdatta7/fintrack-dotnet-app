using System.Text;
using FinTrack.Api;
using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Behaviors;
using FinTrack.Modules.Dashboard;
using FinTrack.Modules.Transactions;
using FinTrack.Modules.Categories;
using FinTrack.Modules.Budgets;
using FinTrack.Modules.Accounts;
using FinTrack.Modules.Users;
using FluentValidation;
using MediatR;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// ==========================================
// PHASE 1: THE BUILDER (Configuration & DI)
// ==========================================

var builder = WebApplication.CreateBuilder(args);

// ---- MongoDB initialization (sync bootstrap, async init at startup) ----
var mongoConnectionString = builder.Configuration.GetValue<string>("MongoDb:ConnectionString")!;
var mongoDbName = builder.Configuration.GetValue<string>("MongoDb:DatabaseName") ?? "FinTrackDb";

// ---- JWT Authentication ----
var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = jwtSection.GetValue<string>("SigningKey")!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection.GetValue<string>("Issuer"),
        ValidAudience = jwtSection.GetValue<string>("Audience"),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
    };
});

// Global authorization fallback: every endpoint requires authentication by default
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy; // RequireAuthenticatedUser
});

// ---- ICurrentUser ----
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

// ---- MediatR with all module assemblies ----
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        typeof(FinTrack.Modules.Dashboard.DependencyInjection).Assembly,
        typeof(FinTrack.Modules.Transactions.DependencyInjection).Assembly,
        typeof(FinTrack.Modules.Categories.DependencyInjection).Assembly,
        typeof(FinTrack.Modules.Budgets.DependencyInjection).Assembly,
        typeof(FinTrack.Modules.Accounts.DependencyInjection).Assembly,
        typeof(FinTrack.Modules.Users.DependencyInjection).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// ---- FluentValidation ----
builder.Services.AddValidatorsFromAssemblies(new[]
{
    typeof(FinTrack.Modules.Transactions.DependencyInjection).Assembly,
    typeof(FinTrack.Modules.Users.DependencyInjection).Assembly,
    typeof(FinTrack.Modules.Categories.DependencyInjection).Assembly,
    typeof(FinTrack.Modules.Budgets.DependencyInjection).Assembly,
    typeof(FinTrack.Modules.Accounts.DependencyInjection).Assembly,
});

// ---- MassTransit + RabbitMQ (publish only in Api) ----
builder.Services.AddMassTransit(cfg =>
{
    cfg.UsingRabbitMq((ctx, rcfg) =>
    {
        rcfg.Host(builder.Configuration.GetValue<string>("RabbitMq:Host") ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration.GetValue<string>("RabbitMq:Username") ?? "guest");
            h.Password(builder.Configuration.GetValue<string>("RabbitMq:Password") ?? "guest");
        });
    });
});

// ---- Module registration ----
builder.Services.AddDashboardModule();
builder.Services.AddTransactionsModule();
builder.Services.AddCategoriesModule();
builder.Services.AddBudgetsModule();
builder.Services.AddAccountsModule();
builder.Services.AddUsersModule(builder.Configuration);

// ---- CORS ----
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy => policy.WithOrigins("http://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

// ==========================================
// PHASE 2: THE APP (HTTP Pipeline)
// ==========================================

var app = builder.Build();

// Initialize MongoDB
await MongoInitializer.InitializeAsync(mongoConnectionString, mongoDbName);

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

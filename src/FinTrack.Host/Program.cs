using FinTrack.BuildingBlocks;
using FinTrack.Modules.Dashboard;
using FinTrack.Modules.Transactions;
using FinTrack.Modules.Categories;
using FinTrack.Modules.Budgets;
using FinTrack.Modules.Accounts;
using FinTrack.Modules.Users;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// ---- MongoDB ----
var mongoConnectionString = builder.Configuration["MongoDb:ConnectionString"]!;
var mongoDbName = builder.Configuration["MongoDb:DatabaseName"] ?? "FinTrackDb";

// ---- System ICurrentUser (Host has no HTTP context) ----
builder.Services.AddSingleton<ICurrentUser, SystemCurrentUser>();

// ---- Module registration ----
builder.Services.AddDashboardModule();
builder.Services.AddTransactionsModule();
builder.Services.AddCategoriesModule();
builder.Services.AddBudgetsModule();
builder.Services.AddAccountsModule();
builder.Services.AddUsersModule(builder.Configuration);

// ---- MediatR ----
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        typeof(FinTrack.Modules.Dashboard.DependencyInjection).Assembly,
        typeof(FinTrack.Modules.Transactions.DependencyInjection).Assembly,
        typeof(FinTrack.Modules.Categories.DependencyInjection).Assembly,
        typeof(FinTrack.Modules.Budgets.DependencyInjection).Assembly,
        typeof(FinTrack.Modules.Accounts.DependencyInjection).Assembly,
        typeof(FinTrack.Modules.Users.DependencyInjection).Assembly);
});

// ---- MassTransit + RabbitMQ (consumers in Host) ----
builder.Services.AddMassTransit(cfg =>
{
    // Register consumers from all modules that have them
    cfg.AddConsumers(typeof(FinTrack.Modules.Dashboard.DependencyInjection).Assembly);
    cfg.AddConsumers(typeof(FinTrack.Modules.Budgets.DependencyInjection).Assembly);
    cfg.AddConsumers(typeof(FinTrack.Modules.Accounts.DependencyInjection).Assembly);
    cfg.AddConsumers(typeof(FinTrack.Modules.Categories.DependencyInjection).Assembly);

    cfg.UsingRabbitMq((ctx, rcfg) =>
    {
        rcfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });

        rcfg.ConfigureEndpoints(ctx);
    });
});

var host = builder.Build();

// Initialize MongoDB
await MongoInitializer.InitializeAsync(mongoConnectionString, mongoDbName);

await host.RunAsync();

// ---- System current user for Host ----
internal sealed class SystemCurrentUser : ICurrentUser
{
    public string? UserId => null;
    public string? Email => null;
    public bool IsAuthenticated => false;
}

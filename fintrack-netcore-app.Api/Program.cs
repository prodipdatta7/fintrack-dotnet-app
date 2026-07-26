using fintrack_netcore_app.Application;
using fintrack_netcore_app.Infrastructure;

// ==========================================
// PHASE 1: THE BUILDER (Configuration & DI)
// ==========================================

var builder = WebApplication.CreateBuilder(args);

// 1. Register Architecture Layers
builder.Services.AddApplication(); // Register Application Layer services
builder.Services.AddInfrastructure(); // Register Infrastructure Layer services

// 2. Configure CORS for the fontend (React) to access the API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy => policy.WithOrigins("http://localhost:3000") // React app URL
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// ==========================================
// PHASE 2: THE APP (HTTP Pipeline & Middleware)
// ==========================================

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");


// app.UseAuthentication(); // Uncomment if authentication is added
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

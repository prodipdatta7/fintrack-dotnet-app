using FinTrack.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. Service Registrations
// ==========================================
builder.Services
    .AddApplicationModules()
    .AddModuleControllers()
    .AddApiSwagger()
    .AddApiCors()
    .AddApiInfrastructure(builder.Configuration)
    .AddFirebaseAuthentication(builder.Configuration, builder.Environment);

// ==========================================
// 2. HTTP Request Pipeline
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
app.UseCors(CorsExtensions.PolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .AllowAnonymous();

app.MapControllers();

await app.RunAsync();

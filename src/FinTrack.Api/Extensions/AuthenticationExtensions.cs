using System.Security.Claims;
using FinTrack.BuildingBlocks.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace FinTrack.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddFirebaseAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<FirebaseOptions>()
            .BindConfiguration(FirebaseOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var firebaseProjectId = configuration["Firebase:ProjectId"];
        if (string.IsNullOrWhiteSpace(firebaseProjectId))
        {
            throw new InvalidOperationException(
                "Firebase:ProjectId must be configured. Firebase Authentication is the sole identity " +
                "provider (set env var Firebase__ProjectId or Secret Manager).");
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = !environment.IsDevelopment();
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

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}

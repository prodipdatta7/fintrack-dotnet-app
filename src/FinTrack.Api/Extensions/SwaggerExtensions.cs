using Microsoft.OpenApi;

namespace FinTrack.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddApiSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
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

        return services;
    }
}

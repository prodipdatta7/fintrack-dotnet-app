using FinTrack.Modules.Assistant.Services;
using FinTrack.Modules.Assistant.Services.Ai;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Modules.Assistant;

public static class DependencyInjection
{
    public static IServiceCollection AddAssistantModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAssistantToolRegistry, AssistantToolRegistry>();
        services.AddSingleton<IAssistantGuardrailsService, AssistantGuardrailsService>();
        services.AddHttpClient<IGeminiAiService, GeminiAiService>();

        return services;
    }
}

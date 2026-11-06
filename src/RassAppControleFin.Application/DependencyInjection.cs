using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RassAppControleFin.Application.Behaviors;
using RassAppControleFin.Application.AI;
using RassAppControleFin.AI;

namespace RassAppControleFin.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            config.AddOpenBehavior(typeof(TelemetryBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
            // A license is supplied only by trusted runtime configuration after a
            // commercial entitlement has been confirmed. No key is stored in code.
            var licenseKey = Environment.GetEnvironmentVariable("MediatR__LicenseKey");
            if (!string.IsNullOrWhiteSpace(licenseKey)) config.LicenseKey = licenseKey;
        });
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IFinanceContextService, FinanceContextService>();
        services.AddSingleton<IPromptBuilder, AiPromptBuilder>();
        services.AddScoped<IAiOrchestrator, AiOrchestrator>();
        return services;
    }
}

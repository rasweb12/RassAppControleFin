using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Http.Resilience;
using Npgsql;
using RassAppControleFin.Application.Abstractions.Persistence;
using RassAppControleFin.Infrastructure.Configuration;
using RassAppControleFin.Infrastructure.Persistence;
using RassAppControleFin.MultiTenancy;
using RassAppControleFin.Security;
using RassAppControleFin.Infrastructure.Identity;
using RassAppControleFin.Application.Abstractions.Identity;
using RassAppControleFin.Infrastructure.AI.Gemini;
using RassAppControleFin.AI;

namespace RassAppControleFin.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(DatabaseConnectionSettings.IsValid, "Invalid or unsafe database configuration.")
            .ValidateOnStart();
        services.AddOptions<NeonOptions>().BindConfiguration(NeonOptions.SectionName)
            .ValidateOnStart();
        services.AddOptions<ResendOptions>().BindConfiguration(ResendOptions.SectionName)
            .ValidateOnStart();
        services.AddOptions<GeminiOptions>().BindConfiguration(GeminiOptions.SectionName)
            .ValidateDataAnnotations().Validate(GeminiOptions.HasSafeModelIdentifier,
                "Configure a supported Gemini model identifier.").ValidateOnStart();
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var connection = DatabaseConnectionSettings.Build(options);
            var dataSource = new NpgsqlDataSourceBuilder(connection.ConnectionString);
            // Only sanitized transaction spans are exported in Sprint 1, never SQL text/parameters.
            dataSource.ConfigureTracing(tracing => tracing.ConfigureCommandFilter(_ => false)
                .ConfigureBatchFilter(_ => false).ConfigureCopyOperationFilter(_ => false));
            return dataSource.Build();
        });
        services.AddDbContext<AppDbContext>((provider, options) =>
        {
            var databaseOptions = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>(), postgres =>
            {
                postgres.CommandTimeout(databaseOptions.CommandTimeoutSeconds);
                postgres.MigrationsHistoryTable("__EFMigrationsHistory", "app");
            });
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            options.UseLoggerFactory(Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        });
        services.TryAddScoped<ITenantContext, UnresolvedTenantContext>();
        services.TryAddScoped<ICurrentUser, UnauthenticatedUser>();
        services.AddScoped<ITenantTransactionFactory, TenantTransactionFactory>();
        services.AddScoped<FinanceUnitFactory>();
        services.AddScoped<IFinanceUnitFactory>(p => p.GetRequiredService<FinanceUnitFactory>());
        services.AddScoped<IFinanceReadStore, FinanceReadStore>();
        services.AddHttpClient("neon-auth").RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false })
            .AddStandardResilienceHandler(options => options.Retry.DisableForUnsafeHttpMethods());
        services.AddSingleton<NeonTokenValidator>();
        services.AddScoped<INeonAuthService, NeonAuthService>();
        services.AddScoped<IIdentityStore, IdentityStore>();
        services.AddHttpClient("gemini", client => client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/"))
            .RemoveAllLoggers().AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 1;
                options.Retry.DisableForUnsafeHttpMethods();
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(35);
            });
        services.AddScoped<IAiProvider, GeminiAiProvider>();
        services.AddScoped<IAiAuditService, AiAuditService>();
        return services;
    }
}

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using RassAppControleFin.Infrastructure.Configuration;

namespace RassAppControleFin.Infrastructure.Persistence;

public sealed class DatabaseHealthCheck(AppDbContext db, IOptions<DatabaseOptions> options,
    ILogger<DatabaseHealthCheck>? logger = null) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ConnectionString))
            return Failed("database.connection_missing");
        try
        {
            if (await DatabaseSafety.VerifyAsync(db, options.Value.RuntimeRole, cancellationToken))
                return HealthCheckResult.Healthy();

            return Failed(await DatabaseSafety.DiagnoseNotReadyAsync(db, cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (PostgresException exception)
        {
            return Failed(exception.SqlState switch
            {
                "28P01" or "28000" => "database.authentication_failed",
                "3D000" or "3F000" or "42P01" => "database.schema_missing",
                "42501" => "database.permission_denied",
                "57014" => "database.timeout",
                _ => "database.connection_failed"
            });
        }
        catch (TimeoutException) { return Failed("database.timeout"); }
        catch (NpgsqlException) { return Failed("database.connection_failed"); }
        catch (Exception) { return Failed("database.connection_failed"); }
    }

    private HealthCheckResult Failed(string category)
    {
        logger?.LogWarning("Database readiness failed. Category={Category}", category);
        return HealthCheckResult.Unhealthy("Database readiness unavailable.", data:
            new Dictionary<string, object> { ["category"] = category });
    }
}

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RassAppControleFin.Infrastructure.Configuration;

namespace RassAppControleFin.Infrastructure.Persistence;

public sealed class DatabaseHealthCheck(AppDbContext db, IOptions<DatabaseOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ConnectionString))
            return HealthCheckResult.Unhealthy("Database is not configured.");
        try
        {
            return await DatabaseSafety.VerifyAsync(db, options.Value.RuntimeRole, cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Database readiness requirements not satisfied.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            // Do not attach provider exceptions: they can contain infrastructure details.
            return HealthCheckResult.Unhealthy("Database unavailable or schema not initialized.");
        }
    }
}

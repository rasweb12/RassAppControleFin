using RassAppControleFin.Contracts;
using RassAppControleFin.Presentation;

namespace RassAppControleFin.Admin.Web.Services;

/// <summary>
/// Resolves display access from API-authoritative data. It is deliberately a
/// navigation aid; every privileged operation remains protected by the API.
/// </summary>
public sealed class AdminAccessClient(ApiGateway gateway, PlatformClient platform)
{
    public async Task<AdminAccessLevel> ResolveAsync(CancellationToken cancellationToken)
    {
        var tenants = await gateway.GetAsync<IReadOnlyList<TenantDto>>("/api/v1/tenants", cancellationToken);
        if (tenants?.Success != true || tenants.Data is null) return AdminAccessLevel.None;

        // A successful platform response is issued only by the Platform.* API policy.
        var hasPlatformAccess = await platform.GetDashboardAsync(cancellationToken) is not null;
        return AdminAccessPolicy.From(hasPlatformAccess, tenants.Data.Select(tenant => tenant.Role));
    }
}

public enum AdminAccessLevel { None, TenantAdmin, SuperAdmin }

public static class AdminAccessPolicy
{
    public static AdminAccessLevel From(bool hasPlatformAccess, IEnumerable<string> tenantRoles)
    {
        if (hasPlatformAccess) return AdminAccessLevel.SuperAdmin;
        return tenantRoles.Any(role => string.Equals(role, "TenantAdmin", StringComparison.Ordinal))
            ? AdminAccessLevel.TenantAdmin
            : AdminAccessLevel.None;
    }
}

using RassAppControleFin.Contracts;
using RassAppControleFin.Presentation;

namespace RassAppControleFin.Admin.Web.Services;

/// <summary>Admin UI client. The API remains the authorization boundary.</summary>
public sealed class PlatformClient(ApiGateway gateway)
{
    public async Task<PlatformDashboardDto?> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var response = await gateway.GetAsync<PlatformDashboardDto>("/api/v1/platform/dashboard", cancellationToken);
        return response?.Success == true ? response.Data : null;
    }
}

public sealed record PlatformDashboardDto(long Users, long Tenants, long SuperAdmins);

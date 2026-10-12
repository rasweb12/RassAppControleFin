using System.Net.Http.Json;
using RassAppControleFin.Contracts;

namespace RassAppControleFin.Admin.Web.Services;

public sealed class ApiOptions
{
    public string BaseUrl { get; init; } = "http://localhost:5080";
}

public sealed class SystemClient(HttpClient client)
{
    public async Task<SystemStatusDto?> GetStatusAsync(CancellationToken cancellationToken)
    {
        var response = await client.GetFromJsonAsync<ApiResponse<SystemStatusDto>>(
            "/api/v1/system", cancellationToken);
        return response?.Success == true ? response.Data : null;
    }
}

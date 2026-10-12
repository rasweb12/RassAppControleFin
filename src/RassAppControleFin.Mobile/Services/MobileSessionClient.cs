using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RassAppControleFin.Contracts;

namespace RassAppControleFin.Mobile.Services;

public sealed class MobileSessionClient(IHttpClientFactory clients, MobileSyncScope syncScope) : IDisposable
{
    private readonly HttpClient _http = clients.CreateClient("mobile-api");
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AuthSessionDto? _session;
    public Guid? TenantId { get; set; }
    public bool IsAuthenticated => _session is not null;
    public string BaseUrl
    {
        get => Preferences.Default.Get("ApiBaseUrl", "https://api.rassapp.invalid");
        set
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https"
                || !string.IsNullOrEmpty(uri.UserInfo)) throw new ArgumentException("HTTPS required.");
            Preferences.Default.Set("ApiBaseUrl", uri.GetLeftPart(UriPartial.Authority));
        }
    }
    public async Task<bool> RestoreAsync()
    {
        try
        {
            var json = await SecureStorage.Default.GetAsync("financial-session");
            _session = json is null ? null : JsonSerializer.Deserialize<AuthSessionDto>(json);
            if (_session is null || _session.SessionExpiresAt <= DateTimeOffset.UtcNow) { Clear(); return false; }
            return true;
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or System.Security.Cryptography.CryptographicException)
        { Clear(); return false; }
    }
    public async Task<ApiResponse<AuthSessionDto>?> LoginAsync(string email, string password, string? name = null)
    {
        var result = await Raw<AuthSessionDto>(HttpMethod.Post, name is null ? "/api/v1/auth/sign-in" : "/api/v1/auth/sign-up",
            name is null ? new SignInRequest(email, password) : new SignUpRequest(name, email, password));
        if (result?.Data is { } session)
        {
            _session = session;
            await Save();
            await SelectTenantAsync();
        }
        return result;
    }
    public async Task<bool> LogoutAsync()
    {
        if (_session is null) return true;
        var result = await Raw<bool>(HttpMethod.Post, "/api/v1/auth/sign-out",
            new RenewSessionRequest(_session.SessionId, _session.SessionToken));
        if (result?.Success != true) return false;
        Clear(); return true;
    }
    public async Task<ApiResponse<T>?> SendAsync<T>(HttpMethod method, string path, object? input = null)
    {
        if (_session is null || Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return null;
        await _gate.WaitAsync();
        try
        {
            if (_session.AccessExpiresAt <= DateTimeOffset.UtcNow.AddMinutes(1))
            {
                var renewed = await Raw<AuthSessionDto>(HttpMethod.Post, "/api/v1/auth/renew",
                    new RenewSessionRequest(_session.SessionId, _session.SessionToken));
                if (renewed?.Data is not { } session) return null;
                _session = session; await Save();
            }
            return await Raw<T>(method, path, input, authorize: true);
        }
        finally { _gate.Release(); }
    }
    public async Task<bool> SelectTenantAsync()
    {
        var tenants = await SendAsync<IReadOnlyList<TenantDto>>(HttpMethod.Get, "/api/v1/tenants");
        var selected = tenants?.Data?.FirstOrDefault();
        if (selected is null) return false;
        TenantId = selected.Id;
        if (_session is not null) await syncScope.SetAsync(selected.Id, _session.Subject);
        return true;
    }
    private async Task<ApiResponse<T>?> Raw<T>(HttpMethod method, string path, object? input, bool authorize = false)
    {
        try
        {
            using var request = new HttpRequestMessage(method, new Uri(new Uri(BaseUrl), path));
            if (input is not null) request.Content = JsonContent.Create(input);
            if (authorize && _session is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);
                request.Headers.Add("X-App-Session", _session.SessionId.ToString());
                if (TenantId is { } tenant) request.Headers.Add("X-Tenant-ID", tenant.ToString());
            }
            using var response = await _http.SendAsync(request);
            if (authorize && response.StatusCode == System.Net.HttpStatusCode.Unauthorized) { Clear(); return null; }
            if (response.Content.Headers.ContentType?.MediaType != "application/json") return null;
            return await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException
            or Polly.Timeout.TimeoutRejectedException or Polly.CircuitBreaker.BrokenCircuitException) { return null; }
    }
    private Task Save() => SecureStorage.Default.SetAsync("financial-session", JsonSerializer.Serialize(_session));
    private void Clear() { _session = null; TenantId = null; syncScope.Clear(); SecureStorage.Default.Remove("financial-session"); }
    public void Dispose() { _http.Dispose(); _gate.Dispose(); }
}

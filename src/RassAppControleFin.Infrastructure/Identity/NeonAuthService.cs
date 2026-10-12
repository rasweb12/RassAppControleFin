using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RassAppControleFin.Application.Abstractions.Identity;
using RassAppControleFin.Infrastructure.Configuration;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Infrastructure.Identity;

public sealed class NeonAuthService(IHttpClientFactory clients, IOptions<NeonOptions> options,
    NeonTokenValidator tokens) : INeonAuthService
{
    private readonly NeonOptions _options = options.Value;
    private static Error Denied => new("AUTH_REJECTED", "Autenticacao recusada.", ErrorType.Unauthorized);
    private static Error Unavailable => new("AUTH_UNAVAILABLE", "Autenticacao indisponivel.", ErrorType.Infrastructure);

    public Task<Result<ProviderSession>> SignInAsync(string email, string password, CancellationToken ct)
        => AuthenticateAsync("sign-in/email", new { email, password, rememberMe = true }, ct);
    public Task<Result<ProviderSession>> SignUpAsync(string name, string email, string password, CancellationToken ct)
        => AuthenticateAsync("sign-up/email", new { name, email, password }, ct);

    private async Task<Result<ProviderSession>> AuthenticateAsync(string path, object payload, CancellationToken ct)
    {
        try
        {
            using var request = Request(HttpMethod.Post, path);
            request.Content = JsonContent.Create(payload);
            using var response = await clients.CreateClient("neon-auth").SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return Result<ProviderSession>.Failure(
                (int)response.StatusCode >= 500 ? Unavailable : Denied);
            var cookies = new CookieContainer();
            if (response.Headers.TryGetValues("Set-Cookie", out var headers))
                foreach (var header in headers) cookies.SetCookies(request.RequestUri!, header);
            var cookie = cookies.GetCookies(request.RequestUri!)[_options.SessionCookieName]?.Value;
            return cookie is null ? Result<ProviderSession>.Failure(Denied) : await RenewAsync(cookie, ct);
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or CookieException or InvalidOperationException
            or Polly.Timeout.TimeoutRejectedException or Polly.CircuitBreaker.BrokenCircuitException)
        { return Result<ProviderSession>.Failure(Unavailable); }
    }

    public async Task<Result<ProviderSession>> RenewAsync(string sessionToken, CancellationToken ct)
    {
        if (!ValidCookie(sessionToken)) return Result<ProviderSession>.Failure(Denied);
        try
        {
            using var sessionRequest = Request(HttpMethod.Get, "get-session", sessionToken);
            using var sessionResponse = await clients.CreateClient("neon-auth").SendAsync(sessionRequest, ct);
            if (!sessionResponse.IsSuccessStatusCode) return Result<ProviderSession>.Failure(Denied);
            using var sessionJson = await JsonDocument.ParseAsync(await sessionResponse.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (sessionJson.RootElement.ValueKind != JsonValueKind.Object
                || !sessionJson.RootElement.TryGetProperty("session", out var session)) return Result<ProviderSession>.Failure(Denied);
            var subject = session.GetProperty("userId").GetString()!;
            var expires = session.GetProperty("expiresAt").GetDateTimeOffset();
            using var tokenRequest = Request(HttpMethod.Get, "token", sessionToken);
            using var tokenResponse = await clients.CreateClient("neon-auth").SendAsync(tokenRequest, ct);
            if (!tokenResponse.IsSuccessStatusCode) return Result<ProviderSession>.Failure(Denied);
            using var tokenJson = await JsonDocument.ParseAsync(await tokenResponse.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var token = tokenJson.RootElement.GetProperty("token").GetString()!;
            var validated = await tokens.ValidateAsync(token, ct);
            if (validated is null || validated.Value.Identity.FindFirst("sub")?.Value != subject || expires <= DateTimeOffset.UtcNow)
                return Result<ProviderSession>.Failure(Denied);
            // Provider rolling renewal can rotate the signed cookie.
            var cookie = sessionToken;
            if (sessionResponse.Headers.TryGetValues("Set-Cookie", out var headers))
            {
                var jar = new CookieContainer();
                foreach (var header in headers) jar.SetCookies(sessionRequest.RequestUri!, header);
                cookie = jar.GetCookies(sessionRequest.RequestUri!)[_options.SessionCookieName]?.Value ?? sessionToken;
            }
            return Result<ProviderSession>.Success(new(subject, token, cookie, validated.Value.Expires, expires));
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or InvalidOperationException or KeyNotFoundException or CookieException
            or Polly.Timeout.TimeoutRejectedException or Polly.CircuitBreaker.BrokenCircuitException)
        { return Result<ProviderSession>.Failure(Unavailable); }
    }

    public async Task<Result> SignOutAsync(string sessionToken, CancellationToken ct)
    {
        if (!ValidCookie(sessionToken)) return Result.Failure(Denied);
        using var request = Request(HttpMethod.Post, "sign-out", sessionToken);
        request.Content = JsonContent.Create(new { });
        try
        {
            using var response = await clients.CreateClient("neon-auth").SendAsync(request, ct);
            return response.IsSuccessStatusCode ? Result.Success() : Result.Failure(Unavailable);
        }
        catch (Exception e) when (e is HttpRequestException or Polly.Timeout.TimeoutRejectedException or Polly.CircuitBreaker.BrokenCircuitException)
        { return Result.Failure(Unavailable); }
    }

    private HttpRequestMessage Request(HttpMethod method, string path, string? cookie = null)
    {
        if (!Uri.TryCreate(_options.AuthBaseUrl.TrimEnd('/') + "/" + path, UriKind.Absolute, out var uri)
            || uri.Scheme != "https") throw new InvalidOperationException("Neon Auth is not configured.");
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("Origin", _options.Origin);
        if (cookie is not null) request.Headers.Add("Cookie", _options.SessionCookieName + "=" + cookie);
        return request;
    }

    private static bool ValidCookie(string value) => value.Length is > 10 and <= 4096
        && value.All(c => c is >= '!' and <= '~' && c is not ';' and not ',' and not '"');
}

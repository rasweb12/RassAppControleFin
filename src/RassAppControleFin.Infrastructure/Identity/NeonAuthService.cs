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
    private static Error AccountAlreadyExists => new("ACCOUNT_ALREADY_EXISTS", "Ja existe um cadastro para este e-mail.", ErrorType.Conflict);

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
            if (path == "sign-up/email" && response.StatusCode == HttpStatusCode.UnprocessableEntity
                && await HasProviderErrorAsync(response, "USER_ALREADY_EXISTS", ct))
                return Result<ProviderSession>.Failure(AccountAlreadyExists);
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

    public async Task<Result> RequestPasswordResetAsync(string email, CancellationToken ct)
    {
        try
        {
            using var request = Request(HttpMethod.Post, "request-password-reset");
            request.Content = JsonContent.Create(new { email, redirectTo = PasswordResetUrl().ToString() });
            using var response = await clients.CreateClient("neon-auth").SendAsync(request, ct);
            // Keep the public response neutral: a provider rejection must not reveal whether an account exists.
            return (int)response.StatusCode >= 500 ? Result.Failure(Unavailable) : Result.Success();
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or InvalidOperationException
            or Polly.Timeout.TimeoutRejectedException or Polly.CircuitBreaker.BrokenCircuitException)
        { return Result.Failure(Unavailable); }
    }

    public async Task<Result> ResetPasswordAsync(string token, string newPassword, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 4096)
            return Result.Failure(Denied);
        try
        {
            using var request = Request(HttpMethod.Post, "reset-password");
            request.Content = JsonContent.Create(new { token, newPassword });
            using var response = await clients.CreateClient("neon-auth").SendAsync(request, ct);
            if (response.IsSuccessStatusCode) return Result.Success();
            return (int)response.StatusCode >= 500 ? Result.Failure(Unavailable) : Result.Failure(Denied);
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or InvalidOperationException
            or Polly.Timeout.TimeoutRejectedException or Polly.CircuitBreaker.BrokenCircuitException)
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

    private Uri PasswordResetUrl()
    {
        if (!Uri.TryCreate(_options.Origin.TrimEnd('/') + "/api/v1/auth/reset-password", UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && !uri.IsLoopback))
            throw new InvalidOperationException("The password reset URL is not configured safely.");
        return uri;
    }

    private static bool ValidCookie(string value) => value.Length is > 10 and <= 4096
        && value.All(c => c is >= '!' and <= '~' && c is not ';' and not ',' and not '"');

    private static async Task<bool> HasProviderErrorAsync(HttpResponseMessage response, string expectedCode,
        CancellationToken ct)
    {
        try
        {
            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("code", out var code)
                && string.Equals(code.GetString(), expectedCode, StringComparison.Ordinal);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

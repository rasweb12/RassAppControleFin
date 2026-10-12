using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Http.Resilience;
using RassAppControleFin.Contracts;

namespace RassAppControleFin.Presentation;

public static class ServerAuthentication
{
    public static void AddServerAuthentication(this WebApplicationBuilder builder, string host)
    {
        var path = builder.Configuration["SessionStore:Path"];
        if (string.IsNullOrWhiteSpace(path))
        {
            if (!builder.Environment.IsDevelopment()) throw new InvalidOperationException("Configure persistent SessionStore:Path.");
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RassAppControleFin", host);
        }
        if (!Path.IsPathFullyQualified(path)) throw new InvalidOperationException("SessionStore:Path must be absolute.");
        var keys = Path.Combine(path, "keys");
        Directory.CreateDirectory(keys);
        var protection = builder.Services.AddDataProtection().SetApplicationName("RassAppControleFin." + host)
            .PersistKeysToFileSystem(new DirectoryInfo(keys));
        if (OperatingSystem.IsWindows()) protection.ProtectKeysWithDpapi();
        builder.Services.AddSingleton<DiskTicketStore>(p => new(Path.Combine(path, "tickets"), p.GetRequiredService<IDataProtectionProvider>()));
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
        {
            options.Cookie.Name = "RassAppControleFin." + host;
            options.Cookie.HttpOnly = true; options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            options.LoginPath = "/login"; options.AccessDeniedPath = "/login";
            options.SlidingExpiration = false;
        });
        builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<DiskTicketStore>((options, store) => options.SessionStore = store);
        builder.Services.AddAuthorization();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddHttpClient("api-session", client =>
            client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5080"))
            .RemoveAllLoggers().AddStandardResilienceHandler(options => options.Retry.DisableForUnsafeHttpMethods());
        builder.Services.AddScoped<ApiGateway>();
    }

    public static void MapServerAuthentication(this WebApplication app)
    {
        app.MapPost("/auth/session", async (HttpContext ctx, IAntiforgery csrf, IHttpClientFactory clients) =>
        {
            try { await csrf.ValidateRequestAsync(ctx); }
            catch (AntiforgeryValidationException) { return Results.BadRequest(); }
            var form = await ctx.Request.ReadFormAsync(ctx.RequestAborted);
            var signup = form["mode"] == "signup";
            object input = signup ? new SignUpRequest(form["name"].ToString(), form["email"].ToString(), form["password"].ToString())
                : new SignInRequest(form["email"].ToString(), form["password"].ToString());
            ApiResponse<AuthSessionDto>? envelope;
            try
            {
                using var response = await clients.CreateClient("api-session").PostAsJsonAsync(
                    signup ? "/api/v1/auth/sign-up" : "/api/v1/auth/sign-in", input, ctx.RequestAborted);
                if (!response.IsSuccessStatusCode) return Results.LocalRedirect("/login?error=auth");
                envelope = await response.Content.ReadFromJsonAsync<ApiResponse<AuthSessionDto>>(ctx.RequestAborted);
            }
            catch (Exception e) when (e is HttpRequestException or JsonException or Polly.Timeout.TimeoutRejectedException or Polly.CircuitBreaker.BrokenCircuitException)
            { return Results.LocalRedirect("/login?error=unavailable"); }
            if (envelope?.Data is not { } session) return Results.LocalRedirect("/login?error=auth");
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, session.Subject),
                new Claim(DiskTicketStore.SessionClaim, JsonSerializer.Serialize(session))], CookieAuthenticationDefaults.AuthenticationScheme);
            await ctx.SignInAsync(new ClaimsPrincipal(identity), new AuthenticationProperties
                { IsPersistent = true, ExpiresUtc = session.SessionExpiresAt });
            return Results.LocalRedirect("/");
        }).AllowAnonymous();
        app.MapPost("/auth/logout", async (HttpContext ctx, IAntiforgery csrf, IHttpClientFactory clients) =>
        {
            try { await csrf.ValidateRequestAsync(ctx); }
            catch (AntiforgeryValidationException) { return Results.BadRequest(); }
            var json = ctx.User.FindFirst(DiskTicketStore.SessionClaim)?.Value;
            if (json is not null)
            {
                var session = JsonSerializer.Deserialize<AuthSessionDto>(json)!;
                try
                {
                    using var response = await clients.CreateClient("api-session").PostAsJsonAsync("/api/v1/auth/sign-out",
                        new RenewSessionRequest(session.SessionId, session.SessionToken), ctx.RequestAborted);
                    if (!response.IsSuccessStatusCode) return Results.LocalRedirect("/?error=logout");
                }
                catch (HttpRequestException) { return Results.LocalRedirect("/?error=logout"); }
            }
            await ctx.SignOutAsync();
            return Results.LocalRedirect("/login");
        }).RequireAuthorization();
    }
}

public sealed class ApiGateway(IHttpClientFactory clients, AuthenticationStateProvider authentication, DiskTicketStore tickets)
{
    private readonly SemaphoreSlim _renewLock = new(1, 1);
    public Guid? TenantId { get; set; }
    public async Task<ApiResponse<T>?> GetAsync<T>(string path, CancellationToken ct = default)
        => await SendAsync<T>(HttpMethod.Get, path, null, ct);
    public async Task<ApiResponse<T>?> PostAsync<T>(string path, object input, CancellationToken ct = default)
        => await SendAsync<T>(HttpMethod.Post, path, input, ct);
    public async Task<ApiResponse<T>?> SendAsync<T>(HttpMethod method, string path, object? input, CancellationToken ct = default)
    {
        try
        {
            var state = await authentication.GetAuthenticationStateAsync();
            var key = state.User.FindFirst(DiskTicketStore.KeyClaim)?.Value;
            if (key is null) return null;
            await _renewLock.WaitAsync(ct);
            AuthSessionDto session;
            try
            {
                var ticket = await tickets.RetrieveAsync(key);
                if (ticket is null) return null;
                session = JsonSerializer.Deserialize<AuthSessionDto>(ticket.Principal.FindFirst(DiskTicketStore.SessionClaim)!.Value)!;
                if (session.AccessExpiresAt < DateTimeOffset.UtcNow.AddMinutes(1))
                {
                    using var renewal = await clients.CreateClient("api-session").PostAsJsonAsync("/api/v1/auth/renew",
                        new RenewSessionRequest(session.SessionId, session.SessionToken), ct);
                    if (!renewal.IsSuccessStatusCode) { await tickets.RemoveAsync(key); return null; }
                    session = (await renewal.Content.ReadFromJsonAsync<ApiResponse<AuthSessionDto>>(ct))!.Data!;
                    var identity = (ClaimsIdentity)ticket.Principal.Identity!;
                    identity.RemoveClaim(identity.FindFirst(DiskTicketStore.SessionClaim)!);
                    identity.AddClaim(new(DiskTicketStore.SessionClaim, JsonSerializer.Serialize(session)));
                    await tickets.RenewAsync(key, ticket);
                }
            }
            finally { _renewLock.Release(); }
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
            request.Headers.Add("X-App-Session", session.SessionId.ToString());
            if (TenantId is { } tenant) request.Headers.Add("X-Tenant-ID", tenant.ToString());
            if (input is not null) request.Content = JsonContent.Create(input);
            using var response = await clients.CreateClient("api-session").SendAsync(request, ct);
            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized) { await tickets.RemoveAsync(key); return null; }
            return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(ct);
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or Polly.Timeout.TimeoutRejectedException or Polly.CircuitBreaker.BrokenCircuitException)
        { return null; }
    }
}

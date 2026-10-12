using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using RassAppControleFin.Contracts;
using RassAppControleFin.Infrastructure.Configuration;
using RassAppControleFin.Infrastructure.Identity;
using RassAppControleFin.Infrastructure.Persistence.Seed;
using RassAppControleFin.Security;
using ScottBrady.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace RassAppControleFin.IntegrationTests.Database;

[Collection(PostgresCollection.Name)]
public sealed class IdentityTests(PostgresFixture fixture)
{
    private const string Issuer = "https://identity.test.invalid";
    private static readonly EdDsaSecurityKey Key = new(EdDsa.Create("Ed25519")) { KeyId = "fixture-ed25519" };
    private static readonly EdDsaSecurityKey OtherKey = new(EdDsa.Create("Ed25519")) { KeyId = "fixture-other-key" };
    private static string Token(string subject, string issuer = Issuer, string audience = Issuer,
        int minutes = 5, SecurityKey? key = null, string algorithm = "EdDSA")
        => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer, audience,
            [new Claim("sub", subject), new Claim("role", "SystemAdmin"), new Claim("TenantId", Guid.NewGuid().ToString())],
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddMinutes(minutes),
            new SigningCredentials(key ?? Key, algorithm)));

    private WebApplicationFactory<Program> Factory(bool realAuth = false)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = fixture.RuntimeConnection,
                ["Neon:AuthBaseUrl"] = "https://ep-quiet-tree-b67qr9k6.neonauth.c-2.sa-east-1.aws.neon.tech/neondb/auth",
                ["Neon:JwksUrl"] = "https://ep-quiet-tree-b67qr9k6.neonauth.c-2.sa-east-1.aws.neon.tech/neondb/auth/.well-known/jwks.json",
                ["Neon:ExpectedIssuer"] = realAuth ? "https://ep-quiet-tree-b67qr9k6.neonauth.c-2.sa-east-1.aws.neon.tech" : Issuer,
                ["Neon:ExpectedAudience"] = realAuth ? "https://ep-quiet-tree-b67qr9k6.neonauth.c-2.sa-east-1.aws.neon.tech" : Issuer
            }));
            if (!realAuth) builder.ConfigureServices(services => services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    var config = new OpenIdConnectConfiguration { Issuer = Issuer };
                    config.SigningKeys.Add(Key);
                    options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(config);
                }));
        });
        return factory;
    }

    [Theory]
    [InlineData("valid", HttpStatusCode.NoContent)]
    [InlineData("issuer", HttpStatusCode.Unauthorized)]
    [InlineData("audience", HttpStatusCode.Unauthorized)]
    [InlineData("expired", HttpStatusCode.Unauthorized)]
    [InlineData("signature", HttpStatusCode.Unauthorized)]
    [InlineData("algorithm", HttpStatusCode.Unauthorized)]
    [InlineData("subject", HttpStatusCode.Unauthorized)]
    [InlineData("session", HttpStatusCode.Unauthorized)]
    public async Task Jwt_trust_and_session_binding_are_enforced(string scenario, HttpStatusCode expected)
    {
        var subject = "identity-fixture-" + Guid.NewGuid().ToString("N");
        var token = Token(scenario == "subject" ? "" : subject,
            scenario == "issuer" ? "https://wrong.invalid" : Issuer,
            scenario == "audience" ? "wrong" : Issuer, scenario == "expired" ? -5 : 5,
            scenario == "signature" ? OtherKey : scenario == "algorithm" ? new SymmetricSecurityKey(new byte[32]) : Key,
            scenario == "algorithm" ? "HS256" : "EdDSA");
        await using var db = fixture.CreateContext();
        var store = new IdentityStore(db, Options.Create(new DatabaseOptions()));
        var provider = new RassAppControleFin.Application.Abstractions.Identity.ProviderSession(subject, token,
            "fixture-signed-cookie-" + Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow.AddMinutes(5), DateTimeOffset.UtcNow.AddHours(1));
        var registered = await store.RegisterSessionAsync(provider, CancellationToken.None);
        await using var factory = Factory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-App-Session", (scenario == "session" ? Guid.NewGuid() : registered.Value.SessionId).ToString());
        using var response = await client.GetAsync("/api/v1/session");
        Assert.Equal(expected, response.StatusCode);
        await store.RevokeSessionAsync(registered.Value.SessionId, provider.SessionToken, provider, CancellationToken.None);
    }

    [Fact]
    public async Task Sessions_revoke_immediately_and_cannot_be_renewed_or_used_by_another_user()
    {
        await using var db = fixture.CreateContext();
        var store = new IdentityStore(db, Options.Create(new DatabaseOptions()));
        var subject = "session-fixture-" + Guid.NewGuid().ToString("N");
        var provider = new RassAppControleFin.Application.Abstractions.Identity.ProviderSession(subject, Token(subject),
            "fixture-cookie-" + Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow.AddMinutes(5), DateTimeOffset.UtcNow.AddHours(1));
        var session = (await store.RegisterSessionAsync(provider, CancellationToken.None)).Value;
        Assert.True(await store.IsSessionActiveAsync(session.SessionId, subject, provider.AccessToken, CancellationToken.None));
        Assert.False(await store.IsSessionActiveAsync(session.SessionId, "another-subject", provider.AccessToken, CancellationToken.None));
        Assert.False((await store.RenewSessionAsync(session.SessionId, "wrong-cookie", provider, CancellationToken.None)).IsSuccess);
        Assert.True((await store.RenewSessionAsync(session.SessionId, provider.SessionToken, provider, CancellationToken.None)).IsSuccess);
        Assert.True((await store.RevokeSessionAsync(session.SessionId, provider.SessionToken, provider, CancellationToken.None)).IsSuccess);
        Assert.False(await store.IsSessionActiveAsync(session.SessionId, subject, provider.AccessToken, CancellationToken.None));
        Assert.False((await store.RenewSessionAsync(session.SessionId, provider.SessionToken, provider, CancellationToken.None)).IsSuccess);
    }

    [Fact]
    public async Task Membership_is_user_scoped_and_does_not_accept_global_roles()
    {
        await using var db = fixture.CreateContext();
        var store = new IdentityStore(db, Options.Create(new DatabaseOptions()));
        Assert.Equal("User", await store.GetMembershipRoleAsync(DevelopmentSeed.Bob, DevelopmentSeed.TenantA, CancellationToken.None));
        Assert.Null(await store.GetMembershipRoleAsync(DevelopmentSeed.Alice, DevelopmentSeed.TenantB, CancellationToken.None));
        Assert.False((await store.AddMemberAsync(DevelopmentSeed.Bob, DevelopmentSeed.TenantA, "forbidden", "TenantAdmin", CancellationToken.None)).IsSuccess);
        Assert.False((await store.AddMemberAsync(DevelopmentSeed.Alice, DevelopmentSeed.TenantA, "forbidden", "SystemAdmin", CancellationToken.None)).IsSuccess);
        Assert.Empty(PermissionCatalog.ForRole("SystemAdmin"));
        Assert.DoesNotContain(Permissions.AccountsRead, PermissionCatalog.ForRole("SupportAgent"));
        Assert.DoesNotContain(Permissions.MembershipsManage, PermissionCatalog.ForRole("User"));
    }

    [Fact, Trait("Category", "LiveAuth")]
    public async Task Live_Neon_signin_renew_tenant_selection_logout_and_replay()
    {
        var secrets = new ConfigurationBuilder().AddUserSecrets("RassAppControleFin-Auth-Development").Build();
        var email = secrets["RASS_AUTH_TEST_EMAIL"] ?? throw new InvalidOperationException("Configure synthetic Neon Auth test credentials.");
        var password = secrets["RASS_AUTH_TEST_PASSWORD"]!;
        await using var factory = Factory(realAuth: true);
        using var client = factory.CreateClient();
        using var login = await client.PostAsJsonAsync("/api/v1/auth/sign-in", new SignInRequest(email, password));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var signed = (await login.Content.ReadFromJsonAsync<ApiResponse<AuthSessionDto>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signed.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/session")).StatusCode);
        client.DefaultRequestHeaders.Add("X-App-Session", signed.SessionId.ToString());
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync("/api/v1/session")).StatusCode);
        var tenants = (await client.GetFromJsonAsync<ApiResponse<List<TenantDto>>>("/api/v1/tenants"))!.Data!;
        Assert.NotEmpty(tenants);
        client.DefaultRequestHeaders.Add("X-Tenant-ID", DevelopmentSeed.TenantA.ToString());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/session")).StatusCode);
        client.DefaultRequestHeaders.Remove("X-Tenant-ID");
        client.DefaultRequestHeaders.Add("X-Tenant-ID", tenants[0].Id.ToString());
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync("/api/v1/session")).StatusCode);
        using var renewal = await client.PostAsJsonAsync("/api/v1/auth/renew", new RenewSessionRequest(signed.SessionId, signed.SessionToken));
        Assert.Equal(HttpStatusCode.OK, renewal.StatusCode);
        var renewed = (await renewal.Content.ReadFromJsonAsync<ApiResponse<AuthSessionDto>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", renewed.AccessToken);
        using var logout = await client.PostAsJsonAsync("/api/v1/auth/sign-out", new RenewSessionRequest(renewed.SessionId, renewed.SessionToken));
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/session")).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        client.DefaultRequestHeaders.Remove("X-Tenant-ID");
        using var replay = await client.PostAsJsonAsync("/api/v1/auth/renew", new RenewSessionRequest(renewed.SessionId, renewed.SessionToken));
        Assert.NotEqual(HttpStatusCode.OK, replay.StatusCode);
    }
}

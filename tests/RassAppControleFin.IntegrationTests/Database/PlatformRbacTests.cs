using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using RassAppControleFin.Application.Abstractions.Identity;
using RassAppControleFin.Contracts;
using RassAppControleFin.Infrastructure.Configuration;
using RassAppControleFin.Infrastructure.Identity;
using ScottBrady.IdentityModel.Tokens;
using Xunit;

namespace RassAppControleFin.IntegrationTests.Database;

[Collection(PostgresCollection.Name)]
public sealed class PlatformRbacTests(PostgresFixture fixture)
{
    private const string Issuer = "https://platform-rbac.test.invalid";
    private static readonly EdDsaSecurityKey Key = new(EdDsa.Create("Ed25519")) { KeyId = "platform-rbac-fixture" };

    [Theory]
    [InlineData("User")]
    [InlineData("TenantAdmin")]
    [InlineData("FinancialAdmin")]
    [InlineData("SupportAgent")]
    public async Task Tenant_roles_and_token_claims_cannot_access_platform_endpoints(string ignoredClientRole)
    {
        var subject = "platform-denied-" + Guid.NewGuid().ToString("N");
        using var client = await CreateAuthenticatedClientAsync(subject, ignoredClientRole);

        using var response = await client.GetAsync("/api/v1/platform/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Synthetic_super_admin_can_access_dashboard_but_not_other_tenant_finances()
    {
        var subject = "platform-admin-" + Guid.NewGuid().ToString("N");
        await SeedSuperAdminAsync(subject);
        using var client = await CreateAuthenticatedClientAsync(subject, "User");

        using var dashboard = await client.GetAsync("/api/v1/platform/dashboard");
        using var finances = await client.GetAsync("/api/v1/accounts");

        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, finances.StatusCode);
    }

    [Fact]
    public async Task Platform_role_grant_is_audited_and_duplicate_or_self_grant_is_rejected()
    {
        var actor = "platform-grant-actor-" + Guid.NewGuid().ToString("N");
        var target = "platform-grant-target-" + Guid.NewGuid().ToString("N");
        await SeedSuperAdminAsync(actor);
        using var client = await CreateAuthenticatedClientAsync(actor, "SystemAdmin");

        var payload = new { UserId = target, Reason = "teste de autorizacao", Confirmed = true };
        using var granted = await client.PostAsJsonAsync($"/api/v1/platform/users/{target}/roles", payload);
        using var duplicate = await client.PostAsJsonAsync($"/api/v1/platform/users/{target}/roles", payload);
        using var self = await client.PostAsJsonAsync($"/api/v1/platform/users/{actor}/roles",
            new { UserId = actor, Reason = "tentativa de auto promocao", Confirmed = true });

        Assert.Equal(HttpStatusCode.OK, granted.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, self.StatusCode);
        await AssertAuditAsync(actor, "platform.role.granted");
    }

    [Fact]
    public async Task Concurrent_grants_for_the_same_user_are_serialized_and_idempotent()
    {
        var actor = "platform-concurrent-actor-" + Guid.NewGuid().ToString("N");
        var target = "platform-concurrent-target-" + Guid.NewGuid().ToString("N");
        await SeedSuperAdminAsync(actor);
        using var firstClient = await CreateAuthenticatedClientAsync(actor, "User");
        using var secondClient = await CreateAuthenticatedClientAsync(actor, "User");
        var payload = new { UserId = target, Reason = "teste concorrente", Confirmed = true };

        var responses = await Task.WhenAll(
            firstClient.PostAsJsonAsync($"/api/v1/platform/users/{target}/roles", payload),
            secondClient.PostAsJsonAsync($"/api/v1/platform/users/{target}/roles", payload));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        Assert.True(await IsActiveAsync(target));
    }

    [Fact]
    public async Task Revocation_is_effective_and_last_super_admin_is_protected()
    {
        var actor = "platform-revoke-actor-" + Guid.NewGuid().ToString("N");
        var target = "platform-revoke-target-" + Guid.NewGuid().ToString("N");
        await SeedSuperAdminAsync(actor);
        await SeedSuperAdminAsync(target);
        var assignment = await GetActiveAssignmentIdAsync(target);
        using var client = await CreateAuthenticatedClientAsync(actor, "User");

        using var revoked = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete,
            $"/api/v1/platform/users/{target}/roles/{assignment}")
        {
            Content = JsonContent.Create(new { Reason = "revogacao de teste", Confirmed = true })
        });
        using var formerAdmin = await CreateAuthenticatedClientAsync(target, "User");
        using var denied = await formerAdmin.GetAsync("/api/v1/platform/dashboard");
        var actorAssignment = await GetActiveAssignmentIdAsync(actor);
        using var last = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete,
            $"/api/v1/platform/users/{actor}/roles/{actorAssignment}")
        {
            Content = JsonContent.Create(new { Reason = "nao pode remover o ultimo", Confirmed = true })
        });

        Assert.Equal(HttpStatusCode.OK, revoked.StatusCode);
        Assert.False(await IsActiveAsync(target));
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, last.StatusCode);
        await AssertAuditAsync(actor, "platform.role.revoked");
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string subject, string clientRole)
    {
        var token = CreateToken(subject, clientRole);
        await using var db = fixture.CreateContext();
        var store = new IdentityStore(db, Options.Create(new DatabaseOptions { RuntimeRole = "rass_app_runtime_v2" }));
        var session = (await store.RegisterSessionAsync(new ProviderSession(subject, token,
            "platform-cookie-" + Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow.AddMinutes(5), DateTimeOffset.UtcNow.AddHours(1)), CancellationToken.None)).Value;
        var factory = Factory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-App-Session", session.SessionId.ToString());
        return client;
    }

    private WebApplicationFactory<Program> Factory() => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:ConnectionString"] = fixture.RuntimeConnection,
            ["Database:RuntimeRole"] = "rass_app_runtime_v2",
            ["Neon:ExpectedIssuer"] = Issuer,
            ["Neon:ExpectedAudience"] = Issuer
        }));
        builder.ConfigureServices(services =>
        {
            services.PostConfigure<DatabaseOptions>(options =>
            {
                options.ConnectionString = fixture.RuntimeConnection;
                options.RuntimeRole = "rass_app_runtime_v2";
            });
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                configuration.SigningKeys.Add(Key);
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            });
        });
    });

    private static string CreateToken(string subject, string role)
        => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(Issuer, Issuer,
            [new Claim("sub", subject), new Claim("role", role)], DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(5),
            new SigningCredentials(Key, "EdDSA")));

    private async Task SeedSuperAdminAsync(string subject)
    {
        await using var connection = new NpgsqlConnection(fixture.AdminConnection);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var scope = new NpgsqlCommand("SELECT set_config('app.user_id', @subject, true), set_config('app.platform_authorized', 'true', true)", connection, transaction);
        scope.Parameters.AddWithValue("subject", subject);
        await scope.ExecuteNonQueryAsync();
        await using var insert = new NpgsqlCommand("""
            INSERT INTO app."PlatformRoleAssignments" ("Id","UserId","Role","IsActive","GrantedBy","GrantedAt","Reason","CreatedAt","UpdatedAt")
            VALUES (@id,@subject,'SuperAdmin',true,@subject,now(),'identidade sintetica de teste',now(),now())
            ON CONFLICT DO NOTHING
            """, connection, transaction);
        insert.Parameters.AddWithValue("id", Guid.NewGuid());
        insert.Parameters.AddWithValue("subject", subject);
        await insert.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }

    private async Task<Guid> GetActiveAssignmentIdAsync(string subject)
    {
        await using var connection = new NpgsqlConnection(fixture.AdminConnection);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var scope = new NpgsqlCommand("SELECT set_config('app.user_id', @subject, true), set_config('app.platform_authorized', 'true', true)", connection, transaction);
        scope.Parameters.AddWithValue("subject", subject);
        await scope.ExecuteNonQueryAsync();
        await using var query = new NpgsqlCommand("SELECT \"Id\" FROM app.\"PlatformRoleAssignments\" WHERE \"UserId\"=@subject AND \"IsActive\"", connection, transaction);
        query.Parameters.AddWithValue("subject", subject);
        var id = (Guid)(await query.ExecuteScalarAsync())!;
        await transaction.CommitAsync();
        return id;
    }

    private async Task<bool> IsActiveAsync(string subject)
    {
        await using var connection = new NpgsqlConnection(fixture.AdminConnection);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var scope = new NpgsqlCommand("SELECT set_config('app.user_id', @subject, true), set_config('app.platform_authorized', 'true', true)", connection, transaction);
        scope.Parameters.AddWithValue("subject", subject);
        await scope.ExecuteNonQueryAsync();
        await using var query = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM app.\"PlatformRoleAssignments\" WHERE \"UserId\"=@subject AND \"IsActive\")", connection, transaction);
        query.Parameters.AddWithValue("subject", subject);
        var active = (bool)(await query.ExecuteScalarAsync())!;
        await transaction.CommitAsync();
        return active;
    }

    private async Task AssertAuditAsync(string subject, string action)
    {
        await using var connection = new NpgsqlConnection(fixture.RuntimeConnection);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await PostgresFixture.SetScopeAsync(connection, transaction, null, subject);
        await using var query = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM app.\"AuditEntries\" WHERE \"UserId\"=@subject AND \"Action\"=@action)", connection, transaction);
        query.Parameters.AddWithValue("subject", subject);
        query.Parameters.AddWithValue("action", action);
        Assert.True((bool)(await query.ExecuteScalarAsync())!);
        await transaction.CommitAsync();
    }
}

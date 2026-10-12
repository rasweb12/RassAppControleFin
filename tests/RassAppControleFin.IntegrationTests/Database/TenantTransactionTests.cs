using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;
using RassAppControleFin.Domain.Entities;
using RassAppControleFin.Infrastructure.Configuration;
using RassAppControleFin.Infrastructure.Persistence;
using RassAppControleFin.Infrastructure.Persistence.Seed;
using RassAppControleFin.MultiTenancy;
using RassAppControleFin.Security;
using RassAppControleFin.SharedKernel.Results;
using Xunit;

namespace RassAppControleFin.IntegrationTests.Database;

[Collection(PostgresCollection.Name)]
public sealed class TenantTransactionTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Ef_filters_and_database_rls_agree_and_dispose_clears_scope()
    {
        await using var db = fixture.CreateContext();
        Assert.Empty(await db.UserProfiles.ToListAsync());
        var begin = await Factory(db, DevelopmentSeed.TenantA, DevelopmentSeed.Alice).BeginAsync(CancellationToken.None);
        Assert.True(begin.IsSuccess, begin.Error?.Code);
        await using (begin.Value)
        {
            var rows = await db.UserProfiles.AsNoTracking().ToListAsync();
            Assert.Single(rows);
            Assert.Equal(DevelopmentSeed.AliceProfile, rows[0].Id);
            Assert.Single(await db.UserProfiles.IgnoreQueryFilters().ToListAsync());
            Assert.Single(await db.Tenants.AsNoTracking().ToListAsync());
        }
        Assert.Empty(await db.UserProfiles.IgnoreQueryFilters().ToListAsync());
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData(false, true, ErrorType.Unauthorized)]
    [InlineData(true, false, ErrorType.Forbidden)]
    public async Task Missing_authorized_identity_or_tenant_is_rejected(bool authenticated, bool resolved, ErrorType expected)
    {
        await using var db = fixture.CreateContext();
        var factory = new TenantTransactionFactory(db,
            new TestTenant(resolved ? DevelopmentSeed.TenantA : null),
            new TestUser(authenticated ? DevelopmentSeed.Alice : null), Options.Create(new DatabaseOptions()));
        var result = await factory.BeginAsync(CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(expected, result.Error!.Type);
        Assert.Null(db.Database.CurrentTransaction);
    }

    [Fact]
    public async Task Nested_transaction_is_rejected()
    {
        await using var db = fixture.CreateContext();
        var factory = Factory(db, DevelopmentSeed.TenantA, DevelopmentSeed.Alice);
        var first = await factory.BeginAsync(CancellationToken.None);
        Assert.True(first.IsSuccess);
        await using var transaction = first.Value;
        var second = await factory.BeginAsync(CancellationToken.None);
        Assert.Equal(ErrorType.Conflict, second.Error!.Type);
    }

    [Fact]
    public async Task Save_changes_without_authorized_transaction_is_rejected()
    {
        await using var db = fixture.CreateContext();
        db.Tenants.Add(Tenant.Create(Guid.NewGuid(), "fixture", DateTimeOffset.UtcNow, "fixture"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
    }

    [Fact]
    public async Task Write_for_another_scope_is_rejected_before_persistence()
    {
        await using var db = fixture.CreateContext();
        var begin = await Factory(db, DevelopmentSeed.TenantA, DevelopmentSeed.Alice).BeginAsync(CancellationToken.None);
        Assert.True(begin.IsSuccess);
        await using var transaction = begin.Value;
        db.UserProfiles.Add(UserProfile.Create(Guid.NewGuid(), DevelopmentSeed.TenantB,
            DevelopmentSeed.Alice, "invalid scope", DateTimeOffset.UtcNow));
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Dispose_without_commit_rolls_back_all_writes()
    {
        var tenant = Guid.NewGuid();
        await using (var db = fixture.CreateContext())
        {
            var begin = await Factory(db, tenant, "rollback-fixture").BeginAsync(CancellationToken.None);
            Assert.True(begin.IsSuccess);
            await using var transaction = begin.Value;
            AddFixture(db, tenant, Guid.NewGuid(), "rollback-fixture");
            await db.SaveChangesAsync();
            Assert.Single(await db.Tenants.ToListAsync());
        }
        await using var verify = fixture.CreateContext();
        var check = await Factory(verify, tenant, "rollback-fixture").BeginAsync(CancellationToken.None);
        Assert.True(check.IsSuccess);
        await using var checkTransaction = check.Value;
        Assert.Empty(await verify.Tenants.ToListAsync());
        Assert.Empty(await verify.UserProfiles.ToListAsync());
    }

    [Fact]
    public async Task Partial_failure_rolls_back_tenant_and_profiles()
    {
        var tenant = Guid.NewGuid();
        const string user = "partial-failure-fixture";
        await using (var db = fixture.CreateContext())
        {
            var begin = await Factory(db, tenant, user).BeginAsync(CancellationToken.None);
            Assert.True(begin.IsSuccess);
            await using var transaction = begin.Value;
            AddFixture(db, tenant, Guid.NewGuid(), user);
            db.UserProfiles.Add(UserProfile.Create(Guid.NewGuid(), tenant, user, "duplicate subject", DateTimeOffset.UtcNow));
            var result = await transaction.CommitAsync(CancellationToken.None);
            Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        }
        await using var verify = fixture.CreateContext();
        var check = await Factory(verify, tenant, user).BeginAsync(CancellationToken.None);
        Assert.True(check.IsSuccess);
        await using var checkTransaction = check.Value;
        Assert.Empty(await verify.Tenants.ToListAsync());
        Assert.Empty(await verify.UserProfiles.ToListAsync());
    }

    [Fact]
    public async Task Concurrency_conflict_is_explicit_and_never_last_write_wins()
    {
        var tenant = Guid.NewGuid();
        var profile = Guid.NewGuid();
        const string user = "concurrency-fixture";
        var connection = new NpgsqlConnectionStringBuilder(fixture.RuntimeConnection) { MaxPoolSize = 2 };
        await using var source = new NpgsqlDataSourceBuilder(connection.ConnectionString).Build();
        AppDbContext Context() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(source,
            postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "app")).Options);
        try
        {
            await using (var setup = Context())
            {
                var begin = await Factory(setup, tenant, user).BeginAsync(CancellationToken.None);
                Assert.True(begin.IsSuccess);
                await using var transaction = begin.Value;
                AddFixture(setup, tenant, profile, user);
                Assert.True((await transaction.CommitAsync(CancellationToken.None)).IsSuccess);
                Assert.Equal(ErrorType.Conflict, (await transaction.CommitAsync(CancellationToken.None)).Error!.Type);
            }
            await using (var first = Context())
            await using (var second = Context())
            {
                var one = await Factory(first, tenant, user).BeginAsync(CancellationToken.None);
                var two = await Factory(second, tenant, user).BeginAsync(CancellationToken.None);
                Assert.True(one.IsSuccess);
                Assert.True(two.IsSuccess);
                await using var transactionOne = one.Value;
                await using var transactionTwo = two.Value;
                var original = await first.UserProfiles.AsTracking().SingleAsync();
                var stale = await second.UserProfiles.AsTracking().SingleAsync();
                original.Rename("committed name");
                stale.Rename("stale name");
                Assert.True((await transactionOne.CommitAsync(CancellationToken.None)).IsSuccess);
                var conflict = await transactionTwo.CommitAsync(CancellationToken.None);
                Assert.Equal(ErrorType.Conflict, conflict.Error!.Type);
                Assert.Equal("VERSION_CONFLICT", conflict.Error.Code);
            }
            await using var verify = Context();
            var check = await Factory(verify, tenant, user).BeginAsync(CancellationToken.None);
            Assert.True(check.IsSuccess);
            await using var checkTransaction = check.Value;
            var saved = await verify.UserProfiles.SingleAsync();
            Assert.Equal("committed name", saved.DisplayName);
            Assert.Equal(2, saved.Version);
        }
        finally
        {
            await using var cleanup = Context();
            var begin = await Factory(cleanup, tenant, user).BeginAsync(CancellationToken.None);
            if (begin.IsSuccess)
            {
                await using var transaction = begin.Value;
                await cleanup.UserProfiles.ExecuteDeleteAsync();
                await cleanup.Tenants.ExecuteDeleteAsync();
                Assert.True((await transaction.CommitAsync(CancellationToken.None)).IsSuccess);
            }
        }
    }

    [Fact]
    public async Task Cancellation_does_not_leave_an_authorized_context()
    {
        await using var db = fixture.CreateContext();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Factory(db, DevelopmentSeed.TenantA, DevelopmentSeed.Alice).BeginAsync(cancellation.Token));
        Assert.Null(db.Database.CurrentTransaction);
        Assert.Empty(await db.UserProfiles.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task Applied_migrations_match_model_and_seed_is_idempotent()
    {
        await using var db = fixture.CreateAdminContext();
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        var applied = await db.Database.GetAppliedMigrationsAsync();
        Assert.Contains(applied, x => x.EndsWith("IdentityFoundation", StringComparison.Ordinal));
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await PostgresFixture.SetScopeAsync(connection, transaction, DevelopmentSeed.TenantA, DevelopmentSeed.Alice);
        await using var count = new NpgsqlCommand("""SELECT count(*) FROM app."UserProfiles" """, connection, transaction);
        Assert.Equal(1L, await count.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Api_readiness_connects_to_real_postgres_without_auth_or_identity()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Database:ConnectionString"] = fixture.RuntimeConnection,
                    ["Database:RuntimeRole"] = "rass_app_runtime"
                })));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        using var protectedResponse = await client.GetAsync("/api/v1/session");
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
    }

    [Fact]
    public async Task Readiness_rejects_a_migration_owner_even_if_expected_role_is_forged()
    {
        await using var db = fixture.CreateAdminContext();
        var owner = new NpgsqlConnectionStringBuilder(fixture.AdminConnection).Username!;
        var options = Options.Create(new DatabaseOptions { ConnectionString = fixture.AdminConnection, RuntimeRole = owner });
        var health = await new DatabaseHealthCheck(db, options).CheckHealthAsync(new());
        Assert.Equal(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, health.Status);
        Assert.Null(health.Exception);
        var begin = await new TenantTransactionFactory(db, new TestTenant(DevelopmentSeed.TenantA),
            new TestUser(DevelopmentSeed.Alice), options).BeginAsync(CancellationToken.None);
        Assert.Equal(ErrorType.Infrastructure, begin.Error!.Type);
    }

    private static void AddFixture(AppDbContext db, Guid tenant, Guid profile, string user)
    {
        db.Tenants.Add(Tenant.Create(tenant, "synthetic fixture", DateTimeOffset.UtcNow, user));
        db.UserProfiles.Add(UserProfile.Create(profile, tenant, user, "original name", DateTimeOffset.UtcNow));
    }

    private static TenantTransactionFactory Factory(AppDbContext db, Guid tenant, string user)
        => new(db, new TestTenant(tenant), new TestUser(user), Options.Create(new DatabaseOptions()));

    private sealed record TestTenant(Guid? TenantId) : ITenantContext
    {
        public bool IsResolved => TenantId is not null;
    }

    private sealed record TestUser(string? Subject) : ICurrentUser
    {
        public bool IsAuthenticated => Subject is not null;
    }
}

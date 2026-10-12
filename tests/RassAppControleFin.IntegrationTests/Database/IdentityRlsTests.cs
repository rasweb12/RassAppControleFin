using Npgsql;
using RassAppControleFin.Infrastructure.Persistence.Seed;
using Xunit;

namespace RassAppControleFin.IntegrationTests.Database;

[Collection(PostgresCollection.Name)]
public sealed class IdentityRlsTests(PostgresFixture fixture)
{
    [Theory]
    [InlineData("TenantMemberships")]
    [InlineData("UserSessions")]
    [InlineData("AuditEntries")]
    public async Task Missing_identity_sees_no_rows(string table)
    {
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = new NpgsqlCommand($"SELECT count(*) FROM app.\"{table}\"", connection, transaction);
        Assert.Equal(0L, await command.ExecuteScalarAsync());
    }
    [Theory]
    [InlineData("User")]
    [InlineData("TenantAdmin")]
    public async Task Non_owner_cannot_assign_or_escalate_membership(string role)
    {
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await PostgresFixture.SetScopeAsync(connection, transaction, DevelopmentSeed.TenantA, DevelopmentSeed.Bob);
        await using var insert = new NpgsqlCommand("""
            INSERT INTO app."TenantMemberships" ("TenantId","UserId","Role","IsActive","IsPersonal","CreatedAt")
            VALUES (@tenant,'forged-user',@role,true,false,now())
            """, connection, transaction);
        insert.Parameters.AddWithValue("tenant", DevelopmentSeed.TenantA);
        insert.Parameters.AddWithValue("role", role);
        var error = await Assert.ThrowsAsync<PostgresException>(() => insert.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
    }
    [Fact]
    public async Task Membership_and_audit_are_not_mutable_by_runtime()
    {
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await PostgresFixture.SetScopeAsync(connection, transaction, DevelopmentSeed.TenantA, DevelopmentSeed.Bob);
        await using var command = new NpgsqlCommand("""
            UPDATE app."TenantMemberships" SET "Role"='TenantAdmin' WHERE "UserId"='fixture-bob'
            """, connection, transaction);
        var error = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
        await transaction.RollbackAsync();
        await using var audit = new NpgsqlCommand("""SELECT has_table_privilege(current_user,'app."AuditEntries"','UPDATE,DELETE')""", connection);
        Assert.False((bool)(await audit.ExecuteScalarAsync())!);
    }
}

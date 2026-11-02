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
        await using var audit = new NpgsqlCommand("""
            SELECT has_table_privilege(current_user,'app."TenantMemberships"','UPDATE'),
                   has_table_privilege(current_user,'app."TenantMemberships"','DELETE'),
                   has_table_privilege(current_user,'app."AuditEntries"','UPDATE'),
                   has_table_privilege(current_user,'app."AuditEntries"','DELETE')
            """, connection);
        await using var reader = await audit.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.False(reader.GetBoolean(0));
        Assert.False(reader.GetBoolean(1));
        Assert.False(reader.GetBoolean(2));
        Assert.False(reader.GetBoolean(3));
    }

    [Fact]
    public async Task Session_registration_and_lookup_are_allowed_only_for_the_scoped_user()
    {
        const string owner = "identity-session-owner";
        const string otherUser = "identity-session-other";
        var sessionId = Guid.NewGuid();

        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await PostgresFixture.SetScopeAsync(connection, transaction, null, owner);
        await using (var insert = new NpgsqlCommand("""
            INSERT INTO app."UserSessions"
                ("Id","UserId","CookieHash","TokenHash","AccessExpiresAt","ExpiresAt","CreatedAt")
            VALUES (@id,@user,@cookie,@token,now() + interval '5 minutes',now() + interval '1 hour',now())
            """, connection, transaction))
        {
            insert.Parameters.AddWithValue("id", sessionId);
            insert.Parameters.AddWithValue("user", owner);
            insert.Parameters.AddWithValue("cookie", new string('a', 64));
            insert.Parameters.AddWithValue("token", new string('b', 64));
            Assert.Equal(1, await insert.ExecuteNonQueryAsync());
        }

        await using (var ownerRead = new NpgsqlCommand("SELECT count(*) FROM app.\"UserSessions\" WHERE \"Id\"=@id", connection, transaction))
        {
            ownerRead.Parameters.AddWithValue("id", sessionId);
            Assert.Equal(1L, await ownerRead.ExecuteScalarAsync());
        }

        await PostgresFixture.SetScopeAsync(connection, transaction, null, otherUser);
        await using (var otherRead = new NpgsqlCommand("SELECT count(*) FROM app.\"UserSessions\" WHERE \"Id\"=@id", connection, transaction))
        {
            otherRead.Parameters.AddWithValue("id", sessionId);
            Assert.Equal(0L, await otherRead.ExecuteScalarAsync());
        }
        await using (var otherUpdate = new NpgsqlCommand("UPDATE app.\"UserSessions\" SET \"RevokedAt\"=now() WHERE \"Id\"=@id", connection, transaction))
        {
            otherUpdate.Parameters.AddWithValue("id", sessionId);
            Assert.Equal(0, await otherUpdate.ExecuteNonQueryAsync());
        }
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Runtime_identity_acl_is_minimal_and_does_not_expose_administration()
    {
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            SELECT
                has_table_privilege(current_user,'app."TenantMemberships"','SELECT'),
                has_table_privilege(current_user,'app."TenantMemberships"','INSERT'),
                has_table_privilege(current_user,'app."TenantMemberships"','UPDATE'),
                has_table_privilege(current_user,'app."UserSessions"','SELECT'),
                has_table_privilege(current_user,'app."UserSessions"','INSERT'),
                has_table_privilege(current_user,'app."UserSessions"','UPDATE'),
                has_table_privilege(current_user,'app."UserSessions"','DELETE'),
                has_table_privilege(current_user,'app."AuditEntries"','SELECT'),
                has_table_privilege(current_user,'app."AuditEntries"','INSERT'),
                has_table_privilege(current_user,'app."AuditEntries"','UPDATE'),
                has_table_privilege(current_user,'app."__EFMigrationsHistory"','SELECT'),
                has_schema_privilege(current_user,'app','CREATE'),
                has_table_privilege(current_user,'app."Tenants"','UPDATE'),
                has_table_privilege(current_user,'app."Tenants"','DELETE'),
                has_function_privilege(current_user,'app.platform_dashboard()','EXECUTE'),
                has_function_privilege(current_user,'app.platform_roles(text)','EXECUTE')
            """, connection);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.True(reader.GetBoolean(0));
        Assert.True(reader.GetBoolean(1));
        Assert.False(reader.GetBoolean(2));
        Assert.True(reader.GetBoolean(3));
        Assert.True(reader.GetBoolean(4));
        Assert.True(reader.GetBoolean(5));
        Assert.False(reader.GetBoolean(6));
        Assert.True(reader.GetBoolean(7));
        Assert.True(reader.GetBoolean(8));
        Assert.False(reader.GetBoolean(9));
        Assert.False(reader.GetBoolean(10));
        Assert.False(reader.GetBoolean(11));
        Assert.False(reader.GetBoolean(12));
        Assert.False(reader.GetBoolean(13));
        Assert.False(reader.GetBoolean(14));
        Assert.False(reader.GetBoolean(15));
    }
}

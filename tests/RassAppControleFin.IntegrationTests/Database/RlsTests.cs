using Npgsql;
using RassAppControleFin.Infrastructure.Persistence.Seed;
using Xunit;

namespace RassAppControleFin.IntegrationTests.Database;

[Collection(PostgresCollection.Name)]
public sealed class RlsTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Runtime_has_no_bypass_membership_ownership_ddl_or_truncate()
    {
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            SELECT current_user = 'rass_app_runtime'
                AND NOT EXISTS (SELECT 1 FROM pg_roles r
                    WHERE pg_has_role(current_user, r.oid, 'MEMBER')
                    AND (r.rolsuper OR r.rolbypassrls OR r.rolcreaterole OR r.rolcreatedb OR r.rolreplication))
                AND NOT has_schema_privilege(current_user, 'app', 'CREATE')
                AND NOT has_table_privilege(current_user, 'app."UserProfiles"', 'TRUNCATE')
                AND (SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
                    WHERE n.nspname = 'app' AND c.relname IN ('Tenants', 'UserProfiles')
                    AND c.relrowsecurity AND c.relforcerowsecurity
                    AND NOT pg_has_role(current_user, c.relowner, 'MEMBER')) = 2
            """, connection);
        Assert.True((bool)(await command.ExecuteScalarAsync())!);
    }

    [Theory]
    [InlineData("alice", "A", 1)]
    [InlineData("bob", "A", 1)]
    [InlineData("carol", "B", 1)]
    [InlineData("alice", "B", 0)]
    [InlineData(null, "A", 0)]
    [InlineData("alice", null, 0)]
    [InlineData(null, null, 0)]
    public async Task Select_isolates_tenant_user_and_missing_context(string? user, string? tenant, int expected)
    {
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await PostgresFixture.SetScopeAsync(connection, transaction, Tenant(tenant), User(user));
        Assert.Equal(expected, await Count(connection, transaction));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Cross_tenant_or_user_insert_is_rejected(bool otherTenant)
    {
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await PostgresFixture.SetScopeAsync(connection, transaction, DevelopmentSeed.TenantA, DevelopmentSeed.Alice);
        var exception = await Assert.ThrowsAsync<PostgresException>(() => InsertProfile(connection, transaction,
            otherTenant ? DevelopmentSeed.TenantB : DevelopmentSeed.TenantA,
            otherTenant ? DevelopmentSeed.Alice : DevelopmentSeed.Bob));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, exception.SqlState);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Cross_tenant_or_user_update_and_delete_affect_no_rows(bool otherTenant)
    {
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await PostgresFixture.SetScopeAsync(connection, transaction, DevelopmentSeed.TenantA, DevelopmentSeed.Alice);
        var id = otherTenant ? DevelopmentSeed.CarolProfile : DevelopmentSeed.BobProfile;
        await using var update = new NpgsqlCommand("""
            UPDATE app."UserProfiles" SET "DisplayName" = 'forbidden' WHERE "Id" = @id
            """, connection, transaction);
        update.Parameters.AddWithValue("id", id);
        Assert.Equal(0, await update.ExecuteNonQueryAsync());
        await using var delete = new NpgsqlCommand("""DELETE FROM app."UserProfiles" WHERE "Id" = @id""", connection, transaction);
        delete.Parameters.AddWithValue("id", id);
        Assert.Equal(0, await delete.ExecuteNonQueryAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_cannot_move_a_visible_row_outside_scope(bool changeTenant)
    {
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await PostgresFixture.SetScopeAsync(connection, transaction, DevelopmentSeed.TenantA, DevelopmentSeed.Alice);
        var sql = changeTenant
            ? """UPDATE app."UserProfiles" SET "TenantId" = @value WHERE "Id" = @id"""
            : """UPDATE app."UserProfiles" SET "UserId" = @value WHERE "Id" = @id""";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", DevelopmentSeed.AliceProfile);
        if (changeTenant) command.Parameters.AddWithValue("value", DevelopmentSeed.TenantB);
        else command.Parameters.AddWithValue("value", "another-subject");
        var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, exception.SqlState);
    }

    [Fact]
    public async Task Missing_context_denies_insert()
    {
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            InsertProfile(connection, transaction, DevelopmentSeed.TenantA, DevelopmentSeed.Alice));
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, exception.SqlState);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task Missing_context_denies_update_and_delete(bool tenantPresent, bool userPresent)
    {
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await PostgresFixture.SetScopeAsync(connection, transaction,
            tenantPresent ? DevelopmentSeed.TenantA : null, userPresent ? DevelopmentSeed.Alice : null);
        await using var update = new NpgsqlCommand("""
            UPDATE app."UserProfiles" SET "DisplayName" = 'forbidden'
            """, connection, transaction);
        Assert.Equal(0, await update.ExecuteNonQueryAsync());
        await using var delete = new NpgsqlCommand("""DELETE FROM app."UserProfiles" """, connection, transaction);
        Assert.Equal(0, await delete.ExecuteNonQueryAsync());
    }

    [Fact]
    public async Task Tenant_table_cannot_read_update_delete_or_insert_another_tenant()
    {
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await PostgresFixture.SetScopeAsync(connection, transaction, DevelopmentSeed.TenantA, DevelopmentSeed.Alice);
        await using var query = new NpgsqlCommand("""SELECT count(*) FROM app."Tenants" WHERE "Id" = @id""", connection, transaction);
        query.Parameters.AddWithValue("id", DevelopmentSeed.TenantB);
        Assert.Equal(0L, await query.ExecuteScalarAsync());
        await using var update = new NpgsqlCommand("""UPDATE app."Tenants" SET "Name" = 'forbidden' WHERE "Id" = @id""", connection, transaction);
        update.Parameters.AddWithValue("id", DevelopmentSeed.TenantB);
        Assert.Equal(0, await update.ExecuteNonQueryAsync());
        await using var delete = new NpgsqlCommand("""DELETE FROM app."Tenants" WHERE "Id" = @id""", connection, transaction);
        delete.Parameters.AddWithValue("id", DevelopmentSeed.TenantB);
        Assert.Equal(0, await delete.ExecuteNonQueryAsync());
        await using var insert = new NpgsqlCommand("""
            INSERT INTO app."Tenants" ("Id", "Name", "CreatedAt") VALUES (@id, 'forbidden', now())
            """, connection, transaction);
        insert.Parameters.AddWithValue("id", Guid.NewGuid());
        var exception = await Assert.ThrowsAsync<PostgresException>(() => insert.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, exception.SqlState);
    }

    [Fact]
    public async Task Authorized_crud_is_possible_and_fixture_changes_roll_back()
    {
        var tenant = Guid.NewGuid();
        const string user = "crud-fixture";
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await PostgresFixture.SetScopeAsync(connection, transaction, tenant, user);
        await using var insertTenant = new NpgsqlCommand("""
            INSERT INTO app."Tenants" ("Id", "Name", "CreatedAt") VALUES (@id, 'CRUD fixture', now())
            """, connection, transaction);
        insertTenant.Parameters.AddWithValue("id", tenant);
        Assert.Equal(1, await insertTenant.ExecuteNonQueryAsync());
        Assert.Equal(1, await InsertProfile(connection, transaction, tenant, user));
        Assert.Equal(1, await Count(connection, transaction));
        await using var update = new NpgsqlCommand("""
            UPDATE app."UserProfiles" SET "DisplayName" = 'updated', "Version" = "Version" + 1
            """, connection, transaction);
        Assert.Equal(1, await update.ExecuteNonQueryAsync());
        await using var delete = new NpgsqlCommand("""DELETE FROM app."UserProfiles" """, connection, transaction);
        Assert.Equal(1, await delete.ExecuteNonQueryAsync());
        await using var deleteTenant = new NpgsqlCommand("""DELETE FROM app."Tenants" """, connection, transaction);
        Assert.Equal(1, await deleteTenant.ExecuteNonQueryAsync());
        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData("commit")]
    [InlineData("rollback")]
    [InlineData("dispose")]
    [InlineData("cancel")]
    public async Task Pooled_connection_does_not_retain_previous_scope(string completion)
    {
        await using (var connection = await fixture.DataSource.OpenConnectionAsync())
        {
            await using var transaction = await connection.BeginTransactionAsync();
            await PostgresFixture.SetScopeAsync(connection, transaction, DevelopmentSeed.TenantA, DevelopmentSeed.Alice);
            Assert.Equal(1, await Count(connection, transaction));
            switch (completion)
            {
                case "commit": await transaction.CommitAsync(); break;
                case "rollback": await transaction.RollbackAsync(); break;
                case "cancel":
                    using (var cancellation = new CancellationTokenSource())
                    {
                        cancellation.Cancel();
                        await using var command = new NpgsqlCommand("SELECT pg_sleep(1)", connection, transaction);
                        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => command.ExecuteScalarAsync(cancellation.Token));
                    }
                    break;
            }
        }
        // MaxPoolSize=1 forces client-pool reuse, including behind Neon's transaction pooler.
        await using var reused = await fixture.DataSource.OpenConnectionAsync();
        await using var next = await reused.BeginTransactionAsync();
        Assert.Equal(0, await Count(reused, next));
        await PostgresFixture.SetScopeAsync(reused, next, DevelopmentSeed.TenantB, DevelopmentSeed.Carol);
        Assert.Equal(1, await Count(reused, next));
    }

    [Theory]
    [InlineData("unique", "23505")]
    [InlineData("version", "23514")]
    [InlineData("foreign-key", "23503")]
    public async Task Constraints_enforce_integrity(string constraint, string sqlState)
    {
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var tenant = constraint == "foreign-key" ? Guid.NewGuid() : DevelopmentSeed.TenantA;
        await PostgresFixture.SetScopeAsync(connection, transaction, tenant, DevelopmentSeed.Alice);
        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            InsertProfile(connection, transaction, tenant, DevelopmentSeed.Alice, constraint == "version" ? 0 : 1));
        Assert.Equal(sqlState, exception.SqlState);
    }

    [Fact]
    public async Task Force_is_configured_without_claiming_it_constrains_a_bypass_owner()
    {
        var admin = new NpgsqlConnectionStringBuilder(fixture.AdminConnection);
        await using var connection = new NpgsqlConnection(admin.ConnectionString);
        await connection.OpenAsync();
        await using var role = new NpgsqlCommand("SELECT rolsuper OR rolbypassrls FROM pg_roles WHERE rolname = current_user", connection);
        var privileged = (bool)(await role.ExecuteScalarAsync())!;
        if (privileged)
        {
            // CI postgres is a superuser; verify FORCE catalog instead of claiming it constrains a superuser.
            await using var catalog = new NpgsqlCommand("""
                SELECT bool_and(c.relforcerowsecurity) FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE n.nspname = 'app' AND c.relname IN ('Tenants', 'UserProfiles')
                """, connection);
            Assert.True((bool)(await catalog.ExecuteScalarAsync())!);
        }
        else
        {
            await using var transaction = await connection.BeginTransactionAsync();
            Assert.Equal(0, await Count(connection, transaction));
        }
    }

    private static Guid? Tenant(string? value) => value switch
    {
        "A" => DevelopmentSeed.TenantA, "B" => DevelopmentSeed.TenantB, _ => null
    };

    private static string? User(string? value) => value switch
    {
        "alice" => DevelopmentSeed.Alice, "bob" => DevelopmentSeed.Bob,
        "carol" => DevelopmentSeed.Carol, _ => null
    };

    private static async Task<long> Count(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        await using var command = new NpgsqlCommand("""SELECT count(*) FROM app."UserProfiles" """, connection, transaction);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<int> InsertProfile(NpgsqlConnection connection, NpgsqlTransaction transaction,
        Guid tenant, string user, long version = 1)
    {
        await using var command = new NpgsqlCommand("""
            INSERT INTO app."UserProfiles" ("TenantId", "Id", "UserId", "DisplayName", "CreatedAt", "Version")
            VALUES (@tenant, @id, @user, 'fixture', now(), @version)
            """, connection, transaction);
        command.Parameters.AddWithValue("tenant", tenant);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("user", user);
        command.Parameters.AddWithValue("version", version);
        return await command.ExecuteNonQueryAsync();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using RassAppControleFin.Infrastructure.Persistence;
using RassAppControleFin.Infrastructure.Persistence.Seed;
using Xunit;

namespace RassAppControleFin.IntegrationTests.Database;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "PostgreSQL";
}

public sealed class PostgresFixture : IAsyncLifetime
{
    public string RuntimeConnection { get; private set; } = "";
    public string AdminConnection { get; private set; } = "";
    public NpgsqlDataSource DataSource { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets("RassAppControleFin-Database-Development")
            .AddEnvironmentVariables().Build();
        AdminConnection = configuration["RASS_DB_TEST_ADMIN"]
            ?? throw new InvalidOperationException("Real database tests require RASS_DB_TEST_ADMIN.");
        RuntimeConnection = configuration["RASS_DB_TEST_RUNTIME"]
            ?? throw new InvalidOperationException("Real database tests require RASS_DB_TEST_RUNTIME.");
        var admin = new NpgsqlConnectionStringBuilder(AdminConnection);
        var runtime = new NpgsqlConnectionStringBuilder(RuntimeConnection);
        if (admin.Database != "rass_sprint1_tests" || runtime.Database != admin.Database
            || admin.Host?.Replace("-pooler", "", StringComparison.Ordinal) !=
                runtime.Host?.Replace("-pooler", "", StringComparison.Ordinal)
            || admin.Host?.Contains("-pooler", StringComparison.Ordinal) == true
            || runtime.Username != "rass_app_runtime")
            throw new InvalidOperationException("Database tests require the dedicated rass_sprint1_tests database.");

        await using (var connection = new NpgsqlConnection(AdminConnection))
        {
            await connection.OpenAsync();
            // CI provisions its own restricted login. Never change existing role passwords.
            await using var query = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'rass_app_runtime')", connection);
            if (!(bool)(await query.ExecuteScalarAsync())!)
            {
                await using var format = new NpgsqlCommand("""
                    SELECT format('CREATE ROLE rass_app_runtime LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS NOINHERIT PASSWORD %L', @password)
                    """, connection);
                format.Parameters.AddWithValue("password", runtime.Password ?? throw new InvalidOperationException("Test runtime password required."));
                var createSql = (string)(await format.ExecuteScalarAsync())!;
                await using var create = new NpgsqlCommand(createSql, connection);
                await create.ExecuteNonQueryAsync();
            }
        }
        await using (var db = CreateAdminContext())
        {
            await db.Database.MigrateAsync();
            await RuntimeRoleBootstrap.ApplyAsync(db, CancellationToken.None);
            await DevelopmentSeed.ApplyAsync(db, CancellationToken.None);
            await DevelopmentSeed.ApplyAsync(db, CancellationToken.None);
        }
        runtime.MaxPoolSize = 1;
        runtime.MinPoolSize = 0;
        runtime.NoResetOnClose = false;
        runtime.IncludeErrorDetail = false;
        runtime.LogParameters = false;
        DataSource = new NpgsqlDataSourceBuilder(runtime.ConnectionString).Build();
    }

    public AppDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(DataSource,
            postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "app")).Options);

    public AppDbContext CreateAdminContext()
        => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(AdminConnection,
            postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "app")).Options);

    public Task DisposeAsync() => DataSource is null ? Task.CompletedTask : DataSource.DisposeAsync().AsTask();

    public static async Task SetScopeAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        Guid? tenantId, string? subject)
    {
        await using var command = new NpgsqlCommand("""
            SELECT set_config('app.tenant_id', @tenant, true), set_config('app.user_id', @subject, true)
            """, connection, transaction);
        command.Parameters.AddWithValue("tenant", tenantId?.ToString() ?? "");
        command.Parameters.AddWithValue("subject", subject ?? "");
        await command.ExecuteNonQueryAsync();
    }
}

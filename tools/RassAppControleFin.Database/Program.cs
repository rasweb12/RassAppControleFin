using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using RassAppControleFin.Infrastructure.Configuration;
using RassAppControleFin.Infrastructure.Persistence;
using RassAppControleFin.Infrastructure.Persistence.Seed;

if (args.Length == 0 || args[0] is not ("migrate" or "bootstrap" or "seed-development" or "probe" or "probe-tests" or "probe-platform-admin-isolated" or "probe-platform-grant-isolated" or "probe-platform-admin-branch" or "probe-upgrade-connections" or "migrate-isolated-tests" or "provision-platform-admin-isolated" or "grant-platform-executor-schema-create-isolated" or "grant-platform-executor-to-migration-owner-isolated"))
{
    Console.Error.WriteLine("Commands: probe | probe-tests | probe-platform-admin-isolated | probe-platform-grant-isolated | probe-platform-admin-branch --expected-host <direct-host> | probe-upgrade-connections --expected-host <direct-host> | migrate-isolated-tests --confirm-isolated-tests | provision-platform-admin-isolated --confirm-isolated-tests | grant-platform-executor-schema-create-isolated --confirm-isolated-tests | grant-platform-executor-to-migration-owner-isolated --confirm-isolated-tests | migrate | bootstrap | seed-development --confirm-development");
    return 2;
}
if (args[0] == "seed-development" && !args.Contains("--confirm-development", StringComparer.Ordinal))
{
    Console.Error.WriteLine("Synthetic seed requires --confirm-development and an isolated database.");
    return 2;
}

try
{
    var configuration = new ConfigurationBuilder()
        .AddUserSecrets("RassAppControleFin-Database-Development")
        .AddEnvironmentVariables().Build();
    if (args[0] == "probe-upgrade-connections")
    {
        var expectedHostIndex = Array.IndexOf(args, "--expected-host");
        var expectedHost = expectedHostIndex >= 0 && expectedHostIndex + 1 < args.Length
            ? args[expectedHostIndex + 1]
            : null;
        var migrationConnection = configuration["RASS_MIGRATIONS_CONNECTION"];
        var runtimeConnection = configuration["RASS_DB_TEST_RUNTIME"];
        if (string.IsNullOrWhiteSpace(expectedHost)
            || string.IsNullOrWhiteSpace(migrationConnection)
            || string.IsNullOrWhiteSpace(runtimeConnection))
        {
            Console.Error.WriteLine("Migration and runtime credentials plus the expected direct endpoint host are required.");
            return 2;
        }

        static bool MatchesDirectVerifyFullEndpoint(NpgsqlConnectionStringBuilder value, string host, string role) =>
            value.Database == "neondb"
            && string.Equals(value.Host, host, StringComparison.OrdinalIgnoreCase)
            && value.Host?.Contains("-pooler", StringComparison.OrdinalIgnoreCase) != true
            && value.Username == role
            && value.SslMode == SslMode.VerifyFull
            && !value.NoResetOnClose
            && !value.Multiplexing;

        NpgsqlConnectionStringBuilder migration;
        NpgsqlConnectionStringBuilder runtime;
        try
        {
            migration = new NpgsqlConnectionStringBuilder(migrationConnection);
            runtime = new NpgsqlConnectionStringBuilder(runtimeConnection);
        }
        catch (ArgumentException)
        {
            Console.Error.WriteLine("Migration or runtime credential has an invalid connection-string format.");
            return 2;
        }

        var migrationMatches = MatchesDirectVerifyFullEndpoint(migration, expectedHost!, "neondb_owner");
        var runtimeMatches = MatchesDirectVerifyFullEndpoint(runtime, expectedHost!, "rass_app_runtime_v2")
            && DatabaseConnectionSettings.IsValid(new DatabaseOptions
            {
                ConnectionString = runtime.ConnectionString,
                RuntimeRole = "rass_app_runtime_v2"
            });
        if (!migrationMatches || !runtimeMatches)
        {
            if (!migrationMatches)
                Console.Error.WriteLine("Migration credential does not target the expected direct VerifyFull endpoint safely.");
            if (!runtimeMatches)
                Console.Error.WriteLine("Runtime credential does not target the expected direct VerifyFull endpoint safely.");
            return 2;
        }

        static async Task<bool> ConfirmIdentityAsync(string connectionString, string role, bool canCreateSchema)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("SELECT current_database(), current_user, has_schema_privilege(current_user, 'app', 'CREATE')", connection);
            await using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return reader.GetString(0) == "neondb" && reader.GetString(1) == role && reader.GetBoolean(2) == canCreateSchema;
        }

        try
        {
            if (!await ConfirmIdentityAsync(migration.ConnectionString, "neondb_owner", true))
            {
                Console.Error.WriteLine("Migration role identity validation failed.");
                return 2;
            }
        }
        catch (PostgresException exception)
        {
            Console.Error.WriteLine($"Migration connection failed (SQLSTATE {exception.SqlState}); details withheld.");
            return 1;
        }
        try
        {
            if (!await ConfirmIdentityAsync(runtime.ConnectionString, "rass_app_runtime_v2", false))
            {
                Console.Error.WriteLine("Runtime role identity validation failed.");
                return 2;
            }
        }
        catch (PostgresException exception)
        {
            Console.Error.WriteLine($"Runtime connection failed (SQLSTATE {exception.SqlState}); details withheld.");
            return 1;
        }

        Console.WriteLine("Migration and runtime credentials validated: direct VerifyFull endpoint, neondb, expected roles, and safe runtime configuration.");
        return 0;
    }
    if (args[0] == "probe-platform-admin-branch")
    {
        var expectedHostIndex = Array.IndexOf(args, "--expected-host");
        var expectedHost = expectedHostIndex >= 0 && expectedHostIndex + 1 < args.Length
            ? args[expectedHostIndex + 1]
            : null;
        var platformConnection = configuration["RASS_DB_TEST_PLATFORM_ADMIN"];
        if (string.IsNullOrWhiteSpace(expectedHost) || string.IsNullOrWhiteSpace(platformConnection))
        {
            Console.Error.WriteLine("A direct endpoint host and platform administration credential are required.");
            return 2;
        }

        NpgsqlConnectionStringBuilder platform;
        try { platform = new NpgsqlConnectionStringBuilder(platformConnection); }
        catch (ArgumentException)
        {
            Console.Error.WriteLine("Platform administration credential has an invalid connection-string format.");
            return 2;
        }
        if (platform.Database != "neondb"
            || !string.Equals(platform.Host, expectedHost, StringComparison.OrdinalIgnoreCase)
            || platform.Host?.Contains("-pooler", StringComparison.OrdinalIgnoreCase) == true
            || platform.Username != "rass_platform_admin_v1"
            || platform.SslMode != SslMode.VerifyFull
            || platform.NoResetOnClose
            || platform.Multiplexing)
        {
            Console.Error.WriteLine("Platform administration credential does not target the expected direct VerifyFull endpoint.");
            return 2;
        }

        try
        {
            await using var platformDbConnection = new NpgsqlConnection(platform.ConnectionString);
            await platformDbConnection.OpenAsync();
            bool valid;
            await using (var command = new NpgsqlCommand("""
                SELECT current_database(), current_user,
                       has_function_privilege(current_user, 'app.platform_dashboard()', 'EXECUTE'),
                       has_function_privilege(current_user, 'app.platform_roles(text)', 'EXECUTE'),
                       has_function_privilege(current_user, 'app.platform_grant_superadmin(uuid,uuid,text,text,text,text)', 'EXECUTE'),
                       has_function_privilege(current_user, 'app.platform_revoke_superadmin(uuid,text,text,uuid,text,text)', 'EXECUTE'),
                       has_table_privilege(current_user, 'app."Transactions"', 'SELECT'),
                       has_table_privilege(current_user, 'app."CreditCardPurchases"', 'SELECT'),
                       has_table_privilege(current_user, 'app."PlatformRoleAssignments"', 'INSERT'),
                       (SELECT rolinherit OR rolcreaterole OR rolcreatedb OR rolbypassrls OR rolsuper
                        FROM pg_roles WHERE rolname = current_user)
                """, platformDbConnection))
            await using (var reader = await command.ExecuteReaderAsync())
            {
                await reader.ReadAsync();
                valid = reader.GetString(0) == "neondb"
                    && reader.GetString(1) == "rass_platform_admin_v1"
                    && reader.GetBoolean(2) && reader.GetBoolean(3) && reader.GetBoolean(4) && reader.GetBoolean(5)
                    && !reader.GetBoolean(6) && !reader.GetBoolean(7) && !reader.GetBoolean(8) && !reader.GetBoolean(9);
            }
            if (!valid)
            {
                Console.Error.WriteLine("Platform administration role validation failed.");
                return 2;
            }

            await using (var dashboard = new NpgsqlCommand("SELECT * FROM app.platform_dashboard()", platformDbConnection))
            await using (var dashboardReader = await dashboard.ExecuteReaderAsync())
                if (!await dashboardReader.ReadAsync())
                {
                    Console.Error.WriteLine("Platform dashboard function returned no row.");
                    return 2;
                }

            try
            {
                await using var finance = new NpgsqlCommand("SELECT count(*) FROM app.\"Transactions\"", platformDbConnection);
                await finance.ExecuteScalarAsync();
                Console.Error.WriteLine("Platform administration role unexpectedly read financial data.");
                return 2;
            }
            catch (PostgresException exception) when (exception.SqlState == "42501") { }

            Console.WriteLine("Platform administration role validated: direct VerifyFull endpoint, authorized functions, and financial-table denial confirmed.");
            return 0;
        }
        catch (PostgresException exception)
        {
            Console.Error.WriteLine($"Platform administration connection failed (SQLSTATE {exception.SqlState}); details withheld.");
            return 1;
        }
    }
    string? isolatedAdminConnection = null;
    if (args[0] is "probe-tests" or "probe-platform-admin-isolated" or "probe-platform-grant-isolated" or "migrate-isolated-tests" or "provision-platform-admin-isolated" or "grant-platform-executor-schema-create-isolated" or "grant-platform-executor-to-migration-owner-isolated")
    {
        var adminConnection = configuration["RASS_DB_TEST_ADMIN"];
        var runtimeConnection = configuration["RASS_DB_TEST_RUNTIME"];
        if (string.IsNullOrWhiteSpace(adminConnection) || string.IsNullOrWhiteSpace(runtimeConnection))
        {
            Console.Error.WriteLine("Isolated test credentials are not configured.");
            return 2;
        }

        var admin = new NpgsqlConnectionStringBuilder(adminConnection!);
        var runtime = new NpgsqlConnectionStringBuilder(runtimeConnection!);
        var expected = admin.Database == "rass_sprint1_tests"
            && runtime.Database == admin.Database
            && !(admin.Host ?? "").Contains("-pooler", StringComparison.OrdinalIgnoreCase)
            && !(runtime.Host ?? "").Contains("-pooler", StringComparison.OrdinalIgnoreCase)
            && admin.SslMode == SslMode.VerifyFull
            && runtime.SslMode == SslMode.VerifyFull
            && runtime.Username == "rass_app_runtime_v2"
            && !string.Equals(admin.Username, runtime.Username, StringComparison.Ordinal);
        if (!expected)
        {
            Console.Error.WriteLine("Configured test credentials do not target the isolated direct endpoint and database.");
            return 2;
        }

        static async Task<(string Database, string Role, bool CanCreateSchema)> IdentifyAsync(string value)
        {
            await using var connection = new NpgsqlConnection(value);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("SELECT current_database(), current_user, has_schema_privilege(current_user, 'app', 'CREATE')", connection);
            await using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return (reader.GetString(0), reader.GetString(1), reader.GetBoolean(2));
        }

        (string Database, string Role, bool CanCreateSchema) adminIdentity;
        try { adminIdentity = await IdentifyAsync(admin.ConnectionString); }
        catch (PostgresException) { Console.Error.WriteLine("Isolated administrative credentials were rejected."); return 1; }
        (string Database, string Role, bool CanCreateSchema) runtimeIdentity;
        try { runtimeIdentity = await IdentifyAsync(runtime.ConnectionString); }
        catch (PostgresException) { Console.Error.WriteLine("Isolated runtime credentials were rejected."); return 1; }
        if (!adminIdentity.CanCreateSchema)
        {
            Console.Error.WriteLine("Configured administrative role cannot create schema objects in app.");
            return 2;
        }
        var safe = DatabaseConnectionSettings.IsValid(new DatabaseOptions
        {
            ConnectionString = runtime.ConnectionString,
            RuntimeRole = runtime.Username ?? string.Empty
        });
        Console.WriteLine($"Isolated admin: database '{adminIdentity.Database}', role '{adminIdentity.Role}', migration schema privilege: {adminIdentity.CanCreateSchema}.");
        Console.WriteLine($"Isolated runtime: database '{runtimeIdentity.Database}', role '{runtimeIdentity.Role}', safe configuration: {safe}.");
        if (args[0] == "probe-tests") return 0;
        if (args[0] is "probe-platform-admin-isolated" or "probe-platform-grant-isolated")
        {
            var platformConnection = configuration["RASS_DB_TEST_PLATFORM_ADMIN"];
            if (string.IsNullOrWhiteSpace(platformConnection))
            {
                Console.Error.WriteLine("Isolated platform administration credential is not configured.");
                return 2;
            }

            var platform = new NpgsqlConnectionStringBuilder(platformConnection);
            if (platform.Database != admin.Database
                || platform.Host?.Replace("-pooler", "", StringComparison.Ordinal) != admin.Host?.Replace("-pooler", "", StringComparison.Ordinal)
                || platform.Host?.Contains("-pooler", StringComparison.Ordinal) == true
                || platform.Username != "rass_platform_admin_v1"
                || platform.SslMode != SslMode.VerifyFull)
            {
                Console.Error.WriteLine("Platform administration credential does not target the isolated direct VerifyFull endpoint.");
                return 2;
            }

            await using var platformProbe = new NpgsqlConnection(platform.ConnectionString);
            await platformProbe.OpenAsync();
            await using (var identity = new NpgsqlCommand("SELECT current_database(), current_user, has_function_privilege(current_user, 'app.platform_dashboard()', 'EXECUTE')", platformProbe))
            await using (var reader = await identity.ExecuteReaderAsync())
            {
                await reader.ReadAsync();
                if (reader.GetString(0) != "rass_sprint1_tests" || reader.GetString(1) != "rass_platform_admin_v1" || !reader.GetBoolean(2))
                {
                    Console.Error.WriteLine("Platform administration role validation failed.");
                    return 2;
                }
            }
            await using (var dashboard = new NpgsqlCommand("SELECT * FROM app.platform_dashboard()", platformProbe))
            await using (var reader = await dashboard.ExecuteReaderAsync())
                if (!await reader.ReadAsync())
                {
                    Console.Error.WriteLine("Platform dashboard function returned no row.");
                    return 2;
                }

            if (args[0] == "probe-platform-admin-isolated")
            {
                Console.WriteLine("Isolated platform administration role and read-only function access validated.");
                return 0;
            }

            await using var transaction = await platformProbe.BeginTransactionAsync();
            try
            {
                await using var grant = new NpgsqlCommand("SELECT * FROM app.platform_grant_superadmin(@id, @audit, @actor, @target, @reason, @correlation)", platformProbe, transaction);
                grant.Parameters.AddWithValue("id", Guid.NewGuid());
                grant.Parameters.AddWithValue("audit", Guid.NewGuid());
                grant.Parameters.AddWithValue("actor", "checkpoint11-probe-actor");
                grant.Parameters.AddWithValue("target", "checkpoint11-probe-target-" + Guid.NewGuid().ToString("N"));
                grant.Parameters.AddWithValue("reason", "isolated transaction rollback probe");
                grant.Parameters.AddWithValue("correlation", Guid.NewGuid().ToString());
                await using (var reader = await grant.ExecuteReaderAsync())
                    if (!await reader.ReadAsync())
                    {
                        Console.Error.WriteLine("Platform grant function returned no row.");
                        return 1;
                    }
                await transaction.RollbackAsync();
                Console.WriteLine("Isolated platform grant function validated and rolled back.");
                return 0;
            }
            catch (PostgresException exception)
            {
                await transaction.RollbackAsync();
                Console.Error.WriteLine($"Platform grant probe failed (SQLSTATE {exception.SqlState}); details withheld.");
                return 1;
            }
        }
        if (!args.Contains("--confirm-isolated-tests", StringComparer.Ordinal))
        {
            Console.Error.WriteLine("Isolated migration requires --confirm-isolated-tests.");
            return 2;
        }
        isolatedAdminConnection = admin.ConnectionString;
    }
    var connectionString = isolatedAdminConnection ?? configuration["RASS_MIGRATIONS_CONNECTION"];
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.Error.WriteLine("RASS_MIGRATIONS_CONNECTION is required.");
        return 2;
    }
    var connection = new NpgsqlConnectionStringBuilder(connectionString);
    if (connection.Host?.Contains("-pooler", StringComparison.OrdinalIgnoreCase) == true)
    {
        Console.Error.WriteLine("Migration tools require a direct connection.");
        return 2;
    }
    connection.IncludeErrorDetail = false;
    connection.LogParameters = false;
    if (args[0] == "provision-platform-admin-isolated")
    {
        await using var provision = new NpgsqlConnection(connection.ConnectionString);
        await provision.OpenAsync();
        await using var command = new NpgsqlCommand("""
            REVOKE neon_superuser FROM rass_platform_executor_owner_v1;
            REVOKE neon_superuser FROM rass_platform_admin_v1;
            ALTER ROLE rass_platform_executor_owner_v1 NOLOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
            ALTER ROLE rass_platform_admin_v1 LOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
            GRANT CREATE ON SCHEMA app TO rass_platform_executor_owner_v1;
            """, provision);
        await command.ExecuteNonQueryAsync();
        Console.WriteLine("Isolated platform administration roles provisioned.");
        return 0;
    }
    if (args[0] == "grant-platform-executor-schema-create-isolated")
    {
        await using var provision = new NpgsqlConnection(connection.ConnectionString);
        await provision.OpenAsync();
        await using var command = new NpgsqlCommand("GRANT CREATE ON SCHEMA app TO rass_platform_executor_owner_v1", provision);
        await command.ExecuteNonQueryAsync();
        Console.WriteLine("Isolated executor schema privilege granted.");
        return 0;
    }
    if (args[0] == "grant-platform-executor-to-migration-owner-isolated")
    {
        await using var provision = new NpgsqlConnection(connection.ConnectionString);
        await provision.OpenAsync();
        await using var command = new NpgsqlCommand("GRANT rass_platform_executor_owner_v1 TO neondb_owner", provision);
        await command.ExecuteNonQueryAsync();
        Console.WriteLine("Isolated migration-owner membership granted.");
        return 0;
    }
    if (args[0] == "probe")
    {
        await using var probe = new NpgsqlConnection(connection.ConnectionString);
        await probe.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT current_database(), current_user", probe);
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        var safe = DatabaseConnectionSettings.IsValid(new DatabaseOptions
        {
            ConnectionString = connection.ConnectionString,
            RuntimeRole = connection.Username ?? string.Empty
        });
        Console.WriteLine($"Connected to database '{reader.GetString(0)}' as role '{reader.GetString(1)}'. Safe configuration: {safe}.");
        return 0;
    }
    var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection.ConnectionString,
        postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "app")).Options;
    await using var db = new AppDbContext(options);
    switch (args[0])
    {
        case "migrate":
        case "migrate-isolated-tests": await db.Database.MigrateAsync(); break;
        case "bootstrap": await RuntimeRoleBootstrap.ApplyAsync(db, CancellationToken.None); break;
        case "seed-development": await DevelopmentSeed.ApplyAsync(db, CancellationToken.None); break;
    }
    Console.WriteLine("Database command completed.");
    return 0;
}
catch (PostgresException exception)
{
    Console.Error.WriteLine($"Database command failed (PostgresException, SQLSTATE {exception.SqlState}); details withheld.");
    return 1;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Database command failed ({exception.GetType().Name}); details withheld.");
    return 1;
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Npgsql;
using RassAppControleFin.Infrastructure.Configuration;
using RassAppControleFin.Infrastructure.Persistence;
using RassAppControleFin.Infrastructure.Persistence.Seed;

const string IsolatedPlatformAdminPasswordHost = "ep-raspy-poetry-b6x8rzs3.c-2.sa-east-1.aws.neon.tech";
const string IsolatedPlatformAdminPasswordDatabase = "rass_sprint1_tests";
const string IsolatedPlatformAdminPasswordBranchId = "br-odd-silence-b646inmx";
const string SimulationPlatformAdminPasswordHost = "ep-old-firefly-b64y6pba.c-2.sa-east-1.aws.neon.tech";
const string SimulationPlatformAdminPasswordDatabase = "rass_sprint1_tests";
const string SimulationPlatformAdminPasswordBranchId = "br-frosty-cherry-b6b841fl";
const string PlatformAdminRole = "rass_platform_admin_v1";
const string IsolatedRbacTestHost = "ep-raspy-poetry-b6x8rzs3.c-2.sa-east-1.aws.neon.tech";
const string IsolatedRbacTestDatabase = "rass_sprint1_tests";
const string IsolatedRbacTestBranchId = "br-odd-silence-b646inmx";
const string ProductionDatabase = "neondb";
const string ProductionMigrationRole = "neondb_owner";
const string ProductionBranchId = "br-lucky-paper-b6egkr8q";

if (args.Length == 0 || args[0] is not ("migrate" or "bootstrap" or "seed-development" or "probe" or "probe-tests" or "probe-platform-admin-isolated" or "probe-platform-grant-isolated" or "probe-platform-admin-branch" or "probe-upgrade-connections" or "migrate-isolated-tests" or "provision-platform-admin-isolated" or "grant-platform-executor-schema-create-isolated" or "grant-platform-executor-to-migration-owner-isolated" or "rotate-platform-admin-password-isolated" or "rotate-platform-admin-password-simulation" or "validate-platform-admin-connection" or "validate-rbac-test-sql-phase" or "validate-rbac-test-connections" or "diagnose-rbac-test-credentials" or "bootstrap-rbac-test-schema" or "execute-rbac-test-sql" or "migrate-rbac-test-database" or "validate-production-upgrade-connection" or "apply-production-upgrade"))
{
    Console.Error.WriteLine("Commands include validate-production-upgrade-connection --expected-host <direct-production-host> --confirmed-branch-id br-lucky-paper-b6egkr8q and apply-production-upgrade with --confirm-production-write. Other commands are intentionally restricted to their documented isolated environments.");
    return 2;
}

if (args[0] is "validate-production-upgrade-connection" or "apply-production-upgrade")
{
    var expectedHostIndex = Array.IndexOf(args, "--expected-host");
    var expectedHost = expectedHostIndex >= 0 && expectedHostIndex + 1 < args.Length ? args[expectedHostIndex + 1] : null;
    var branchIndex = Array.IndexOf(args, "--confirmed-branch-id");
    var branchId = branchIndex >= 0 && branchIndex + 1 < args.Length ? args[branchIndex + 1] : null;
    var apply = args[0] == "apply-production-upgrade";
    var targetIndex = Array.IndexOf(args, "--target");
    var target = targetIndex >= 0 && targetIndex + 1 < args.Length ? args[targetIndex + 1] : null;
    var expectedStartingMigrationCount = target switch
    {
        "20261102010000_PlatformAdministrationHardening" => 10,
        "20261102040000_RuntimeIdentityOperationalAclHardening" => 17,
        _ => -1
    };

    if (string.IsNullOrWhiteSpace(expectedHost)
        || expectedHost.Contains("-pooler", StringComparison.OrdinalIgnoreCase)
        || !string.Equals(branchId, ProductionBranchId, StringComparison.Ordinal)
        || (apply && (!args.Contains("--confirm-production-write", StringComparer.Ordinal)
            || expectedStartingMigrationCount < 0)))
    {
        Console.Error.WriteLine("Production upgrade command was rejected by its destination or explicit-write confirmation policy.");
        return 2;
    }

    // Production credentials are deliberately process-only. No test variable,
    // User Secret, application configuration or fallback can reach this path.
    var rawConnection = Environment.GetEnvironmentVariable("RASS_PRODUCTION_MIGRATIONS_CONNECTION", EnvironmentVariableTarget.Process);
    if (string.IsNullOrWhiteSpace(rawConnection))
    {
        Console.Error.WriteLine("Production migration credential is not configured in the current process.");
        return 2;
    }

    NpgsqlConnectionStringBuilder production;
    try { production = new NpgsqlConnectionStringBuilder(rawConnection); }
    catch (ArgumentException)
    {
        Console.Error.WriteLine("Production migration credential format is invalid.");
        return 2;
    }

    if (!string.Equals(production.Host, expectedHost, StringComparison.OrdinalIgnoreCase)
        || !string.Equals(production.Database, ProductionDatabase, StringComparison.Ordinal)
        || !string.Equals(production.Username, ProductionMigrationRole, StringComparison.Ordinal)
        || string.IsNullOrWhiteSpace(production.Password)
        || production.SslMode != SslMode.VerifyFull
        || production.Host?.Contains("-pooler", StringComparison.OrdinalIgnoreCase) == true
        || production.NoResetOnClose || production.Multiplexing)
    {
        Console.Error.WriteLine("Production migration credential does not meet the direct TLS destination policy.");
        return 2;
    }

    try
    {
        await using var connection = new NpgsqlConnection(production.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT session_user, current_user, current_database(),
              (SELECT count(*) FROM app."__EFMigrationsHistory") AS migration_count,
              EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'rass_app_runtime_v2') AS runtime_exists,
              EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'rass_platform_admin_v1') AS platform_admin_exists,
              EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'rass_platform_executor_owner_v1') AS executor_exists;
            """, connection);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()
            || !string.Equals(reader.GetString(0), ProductionMigrationRole, StringComparison.Ordinal)
            || !string.Equals(reader.GetString(1), ProductionMigrationRole, StringComparison.Ordinal)
            || !string.Equals(reader.GetString(2), ProductionDatabase, StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Production migration identity or database verification failed.");
            return 1;
        }

        var migrationCount = reader.GetInt64(3);
        var rolesReady = reader.GetBoolean(4) && reader.GetBoolean(5) && reader.GetBoolean(6);
        if ((!apply && migrationCount is not (10 or 17 or 20))
            || (apply && migrationCount != expectedStartingMigrationCount))
        {
            Console.Error.WriteLine("Production migration history is not at the approved stage for this operation.");
            return 1;
        }
        if (!rolesReady)
        {
            Console.Error.WriteLine("Production platform roles are not provisioned; no migration was attempted.");
            return 1;
        }

        if (!apply)
        {
            Console.WriteLine("PASS: production preflight confirmed the approved migration identity, 10-migration baseline, and required role presence.");
            return 0;
        }

        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(production.ConnectionString,
            postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "app")).Options;
        await using var migrationDb = new AppDbContext(options);
        await migrationDb.Database.GetService<IMigrator>().MigrateAsync(target);
        Console.WriteLine("PASS: approved production migration stage completed. Run the next documented preflight before proceeding.");
        return 0;
    }
    catch (PostgresException exception)
    {
        Console.Error.WriteLine($"Production upgrade command failed (SQLSTATE {exception.SqlState}); details withheld.");
        return 1;
    }
    catch (NpgsqlException)
    {
        Console.Error.WriteLine("Production upgrade command could not establish the approved database connection.");
        return 1;
    }
}
if (args[0] == "validate-rbac-test-sql-phase")
{
    var phaseIndex = Array.IndexOf(args, "--phase");
    var phase = phaseIndex >= 0 && phaseIndex + 1 < args.Length ? args[phaseIndex + 1] : null;
    if (phase is not ("ownership" or "acl" or "test-seed" or "cleanup" or "pre-validation" or "post-validation"))
    {
        Console.Error.WriteLine("An approved RBAC SQL phase is required.");
        return 2;
    }

    Console.WriteLine($"RBAC SQL phase '{phase}' is approved.");
    return 0;
}
if (args[0] == "seed-development" && !args.Contains("--confirm-development", StringComparer.Ordinal))
{
    Console.Error.WriteLine("Synthetic seed requires --confirm-development and an isolated database.");
    return 2;
}
if (args[0] == "validate-platform-admin-connection")
{
    // Deliberately read only the process environment: this command proves the
    // credential supplied to this exact invocation, without a User Secrets fallback.
    var value = Environment.GetEnvironmentVariable("RASS_DB_TEST_PLATFORM_ADMIN", EnvironmentVariableTarget.Process);
    if (string.IsNullOrWhiteSpace(value))
    {
        Console.Error.WriteLine("FAIL: platform administration credential is not configured in the current process.");
        return 2;
    }

    NpgsqlConnectionStringBuilder connection;
    try { connection = new NpgsqlConnectionStringBuilder(value); }
    catch (ArgumentException)
    {
        Console.Error.WriteLine("FAIL: platform administration credential format is invalid.");
        return 2;
    }

    if (!string.Equals(connection.Username, PlatformAdminRole, StringComparison.Ordinal)
        || string.IsNullOrWhiteSpace(connection.Password)
        || !string.Equals(connection.Host, IsolatedRbacTestHost, StringComparison.OrdinalIgnoreCase)
        || !string.Equals(connection.Database, IsolatedRbacTestDatabase, StringComparison.Ordinal)
        || connection.SslMode != SslMode.VerifyFull
        || connection.Host?.Contains("-pooler", StringComparison.OrdinalIgnoreCase) == true)
    {
        Console.Error.WriteLine("FAIL: platform administration credential does not meet the approved isolated connection policy.");
        return 2;
    }

    try
    {
        await using var database = new NpgsqlConnection(connection.ConnectionString);
        await database.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT session_user, current_user, current_database();", database);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()
            || reader.FieldCount != 3
            || !string.Equals(reader.GetString(0), PlatformAdminRole, StringComparison.Ordinal)
            || !string.Equals(reader.GetString(1), PlatformAdminRole, StringComparison.Ordinal)
            || !string.Equals(reader.GetString(2), IsolatedRbacTestDatabase, StringComparison.Ordinal))
        {
            Console.Error.WriteLine("FAIL: platform administration identity or database verification failed.");
            return 1;
        }

        Console.WriteLine("PASS");
        return 0;
    }
    catch (PostgresException exception)
    {
        Console.Error.WriteLine($"FAIL: PostgreSQL authentication or authorization failed (SQLSTATE {exception.SqlState}).");
        return 1;
    }
    catch (NpgsqlException)
    {
        Console.Error.WriteLine("FAIL: platform administration connection could not be established.");
        return 1;
    }
}

try
{
    var configuration = new ConfigurationBuilder()
        .AddUserSecrets("RassAppControleFin-Database-Development")
        .AddEnvironmentVariables().Build();
    if (args[0] == "diagnose-rbac-test-credentials")
    {
        static void Diagnose(IConfiguration configuration, string name, string expectedRole)
        {
            var processValue = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
            var value = processValue ?? configuration[name];
            if (string.IsNullOrWhiteSpace(value))
            {
                Console.WriteLine($"{name}: configured=false");
                return;
            }

            try
            {
                var connection = new NpgsqlConnectionStringBuilder(value);
                var generic = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = value };
                var genericPassword = generic.ContainsKey("Password") ? Convert.ToString(generic["Password"]) : null;
                var genericKeys = string.Join(",", generic.Keys.Cast<string>()
                    .Where(key => !key.Equals("Password", StringComparison.OrdinalIgnoreCase)
                        && !key.Equals("Pwd", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(key => key, StringComparer.OrdinalIgnoreCase));
                Console.WriteLine($"{name}: source={(processValue is null ? "user-secrets" : "process")}, "
                    + $"hostApproved={string.Equals(connection.Host, IsolatedRbacTestHost, StringComparison.OrdinalIgnoreCase)}, "
                    + $"databaseApproved={string.Equals(connection.Database, IsolatedRbacTestDatabase, StringComparison.Ordinal)}, "
                    + $"roleApproved={string.Equals(connection.Username, expectedRole, StringComparison.Ordinal)}, "
                    + $"verifyFull={connection.SslMode == SslMode.VerifyFull}, "
                    + $"direct={!connection.Host?.Contains("-pooler", StringComparison.OrdinalIgnoreCase)}, "
                    + $"resetOnClose={!connection.NoResetOnClose}, multiplexingDisabled={!connection.Multiplexing}, "
                    + $"passwordPresent={!string.IsNullOrWhiteSpace(connection.Password)}, "
                    + $"genericPasswordKey={generic.ContainsKey("Password") || generic.ContainsKey("Pwd")}, genericPasswordPresent={!string.IsNullOrWhiteSpace(genericPassword)}, "
                    + $"genericKeys={genericKeys}");
            }
            catch (ArgumentException)
            {
                Console.WriteLine($"{name}: configured=true, formatValid=false");
            }
        }

        Diagnose(configuration, "RASS_DB_TEST_ADMIN", "neondb_owner");
        Diagnose(configuration, "RASS_DB_TEST_RUNTIME", "rass_app_runtime_v2");
        Diagnose(configuration, "RASS_DB_TEST_PLATFORM_ADMIN", PlatformAdminRole);
        return 0;
    }
    if (args[0] is "validate-rbac-test-connections" or "bootstrap-rbac-test-schema" or "execute-rbac-test-sql" or "migrate-rbac-test-database")
    {
        const string confirmationFlag = "--confirm-isolated-rbac-tests";
        const string branchFlag = "--confirmed-branch-id";
        const string hostFlag = "--expected-host";
        var branchIndex = Array.IndexOf(args, branchFlag);
        var hostIndex = Array.IndexOf(args, hostFlag);
        var confirmedBranchId = branchIndex >= 0 && branchIndex + 1 < args.Length ? args[branchIndex + 1] : null;
        var expectedHost = hostIndex >= 0 && hostIndex + 1 < args.Length ? args[hostIndex + 1] : null;
        if (!args.Contains(confirmationFlag, StringComparer.Ordinal)
            || !string.Equals(confirmedBranchId, IsolatedRbacTestBranchId, StringComparison.Ordinal)
            || !string.Equals(expectedHost, IsolatedRbacTestHost, StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("RBAC test commands require explicit confirmation of the approved isolated branch and endpoint.");
            return 2;
        }

        static bool IsApprovedTestConnection(NpgsqlConnectionStringBuilder value, string role) =>
            string.Equals(value.Host, IsolatedRbacTestHost, StringComparison.OrdinalIgnoreCase)
            && string.Equals(value.Database, IsolatedRbacTestDatabase, StringComparison.Ordinal)
            && string.Equals(value.Username, role, StringComparison.Ordinal)
            && value.Host?.Contains("-pooler", StringComparison.OrdinalIgnoreCase) != true
            && value.SslMode == SslMode.VerifyFull
            && !value.NoResetOnClose
            && !value.Multiplexing;

        static bool HasUnsafeRoleAttributes(NpgsqlDataReader reader, int offset) =>
            reader.GetBoolean(offset) || reader.GetBoolean(offset + 1) || reader.GetBoolean(offset + 2)
            || reader.GetBoolean(offset + 3) || reader.GetBoolean(offset + 4) || reader.GetBoolean(offset + 5);

        static async Task<bool> VerifyRoleAsync(NpgsqlConnection connection, string expectedRole, bool requireApplicationRoleSafety)
        {
            await using var command = new NpgsqlCommand("""
                SELECT session_user, current_user, current_database(),
                       r.rolsuper, r.rolcreaterole, r.rolcreatedb, r.rolbypassrls, r.rolreplication, r.rolinherit,
                       pg_has_role(r.oid, 'neon_superuser', 'MEMBER')
                FROM pg_roles r
                WHERE r.rolname = current_user
                """, connection);
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return false;
            var roleIsSafe = !requireApplicationRoleSafety || (!HasUnsafeRoleAttributes(reader, 3) && !reader.GetBoolean(9));
            return string.Equals(reader.GetString(0), expectedRole, StringComparison.Ordinal)
                && string.Equals(reader.GetString(1), expectedRole, StringComparison.Ordinal)
                && string.Equals(reader.GetString(2), IsolatedRbacTestDatabase, StringComparison.Ordinal)
                && roleIsSafe;
        }

        static async Task<bool> RuntimeLacksSchemaCreateAsync(NpgsqlConnection connection)
        {
            await using var command = new NpgsqlCommand("SELECT NOT has_schema_privilege(current_user, 'app', 'CREATE')", connection);
            return (bool)(await command.ExecuteScalarAsync())!;
        }

        static async Task<bool> AppSchemaExistsAsync(NpgsqlConnection connection)
        {
            await using var command = new NpgsqlCommand("SELECT to_regnamespace('app') IS NOT NULL", connection);
            return (bool)(await command.ExecuteScalarAsync())!;
        }

        static string IsolatedTestConnection(IConfiguration configuration, string name)
        {
            // The final-validation script may run from a process that did not
            // inherit an interactive PowerShell session. User Secrets remain
            // local-only, and the strict endpoint/role validation below still
            // rejects any value outside the dedicated RBAC test database.
            var value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process)
                ?? configuration[name];
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"{name} must be configured in the current process or local User Secrets.");
            return value;
        }

        NpgsqlConnectionStringBuilder admin;
        NpgsqlConnectionStringBuilder runtime;
        NpgsqlConnectionStringBuilder platform;
        try
        {
            admin = new NpgsqlConnectionStringBuilder(IsolatedTestConnection(configuration, "RASS_DB_TEST_ADMIN"));
            runtime = new NpgsqlConnectionStringBuilder(IsolatedTestConnection(configuration, "RASS_DB_TEST_RUNTIME"));
            platform = new NpgsqlConnectionStringBuilder(IsolatedTestConnection(configuration, "RASS_DB_TEST_PLATFORM_ADMIN"));
        }
        catch (ArgumentException)
        {
            Console.Error.WriteLine("An RBAC test credential has an invalid connection-string format.");
            return 2;
        }

        if (!IsApprovedTestConnection(admin, "neondb_owner")
            || !IsApprovedTestConnection(runtime, "rass_app_runtime_v2")
            || !IsApprovedTestConnection(platform, PlatformAdminRole)
            || !DatabaseConnectionSettings.IsValid(new DatabaseOptions
            {
                ConnectionString = runtime.ConnectionString,
                RuntimeRole = "rass_app_runtime_v2"
            }))
        {
            Console.Error.WriteLine("RBAC test credentials do not target the approved direct VerifyFull test environment safely.");
            return 2;
        }

        try
        {
            Console.WriteLine("RBAC test preflight: opening approved role connections.");
            await using var adminConnection = new NpgsqlConnection(admin.ConnectionString);
            await using var runtimeConnection = new NpgsqlConnection(runtime.ConnectionString);
            await using var platformConnection = new NpgsqlConnection(platform.ConnectionString);
            await adminConnection.OpenAsync();
            await runtimeConnection.OpenAsync();
            await platformConnection.OpenAsync();
            if (!await VerifyRoleAsync(adminConnection, "neondb_owner", requireApplicationRoleSafety: false)
                || !await VerifyRoleAsync(runtimeConnection, "rass_app_runtime_v2", requireApplicationRoleSafety: true)
                || !await VerifyRoleAsync(platformConnection, PlatformAdminRole, requireApplicationRoleSafety: true))
            {
                Console.Error.WriteLine("RBAC test connection identity or role safety validation failed.");
                return 2;
            }

            Console.WriteLine("RBAC test preflight: connection identities and role attributes validated.");

            if (args[0] == "validate-rbac-test-connections")
            {
                Console.WriteLine("RBAC test credentials validated: approved direct VerifyFull endpoint, database, identities, and role safety.");
                return 0;
            }

            if (args[0] == "bootstrap-rbac-test-schema")
            {
                Console.WriteLine("RBAC test bootstrap: checking schema and migration-history absence.");
                await using var state = new NpgsqlCommand("""
                    SELECT to_regnamespace('app') IS NOT NULL,
                           to_regclass('app."__EFMigrationsHistory"') IS NOT NULL,
                           EXISTS (
                               SELECT 1
                               FROM pg_class relation
                               JOIN pg_namespace schema ON schema.oid = relation.relnamespace
                               WHERE schema.nspname = 'app'),
                           EXISTS (
                               SELECT 1
                               FROM pg_proc routine
                               JOIN pg_namespace schema ON schema.oid = routine.pronamespace
                               WHERE schema.nspname = 'app')
                    """, adminConnection);
                await using var stateReader = await state.ExecuteReaderAsync();
                await stateReader.ReadAsync();
                var schemaExists = stateReader.GetBoolean(0);
                var historyExists = stateReader.GetBoolean(1);
                var hasRelations = stateReader.GetBoolean(2);
                var hasRoutines = stateReader.GetBoolean(3);
                await stateReader.CloseAsync();

                if (historyExists)
                {
                    Console.Error.WriteLine("RBAC test schema bootstrap refused because EF migration history already exists.");
                    return 2;
                }
                if (schemaExists)
                {
                    if (hasRelations || hasRoutines)
                    {
                        Console.Error.WriteLine("RBAC test schema bootstrap refused because schema app contains objects without EF migration history; inspect the partial state before proceeding.");
                        return 2;
                    }

                    Console.WriteLine("RBAC test schema bootstrap confirmed: schema app already exists and is empty; no change was made.");
                    return 0;
                }

                Console.WriteLine("RBAC test bootstrap: creating empty app schema.");
                await using var createSchema = new NpgsqlCommand("CREATE SCHEMA app AUTHORIZATION neondb_owner", adminConnection);
                await createSchema.ExecuteNonQueryAsync();
                Console.WriteLine("RBAC test schema bootstrap completed: created only schema app; no EF history or application tables were created.");
                return 0;
            }

            Console.WriteLine("RBAC test preflight: validating runtime schema privilege after schema availability.");
            if (!await AppSchemaExistsAsync(adminConnection))
            {
                Console.Error.WriteLine("Schema app is absent; run bootstrap-rbac-test-schema before RBAC migrations or SQL phases.");
                return 2;
            }
            if (!await RuntimeLacksSchemaCreateAsync(runtimeConnection))
            {
                Console.Error.WriteLine("Runtime role unexpectedly has CREATE on schema app.");
                return 2;
            }

            if (args[0] == "execute-rbac-test-sql")
            {
                var phaseIndex = Array.IndexOf(args, "--phase");
                var phase = phaseIndex >= 0 && phaseIndex + 1 < args.Length ? args[phaseIndex + 1] : null;
                var approvedFiles = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["ownership"] = "02-test-rbac-db-temporary-ownership-permissions.sql",
                    ["acl"] = "05-test-rbac-db-temporary-function-acl-permissions.sql",
                    ["test-seed"] = "09-test-rbac-db-temporary-executor-set.sql",
                    ["cleanup"] = "10-test-rbac-db-cleanup-temporary-executor.sql",
                    ["pre-validation"] = "12-test-rbac-db-pre-validation.sql",
                    ["post-validation"] = "11-test-rbac-db-post-validation.sql"
                };
                if (phase is null || !approvedFiles.TryGetValue(phase, out var approvedFile))
                {
                    Console.Error.WriteLine("An approved RBAC SQL phase is required.");
                    return 2;
                }

                var root = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "infra", "neon", "checkpoint13"));
                var path = Path.GetFullPath(Path.Combine(root, approvedFile));
                if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
                {
                    Console.Error.WriteLine("The approved RBAC SQL file is unavailable.");
                    return 2;
                }

                var sql = await File.ReadAllTextAsync(path);
                await using var command = new NpgsqlCommand(sql, adminConnection);
                await command.ExecuteNonQueryAsync();
                Console.WriteLine($"RBAC SQL phase '{phase}' completed on the approved isolated test database.");
                return 0;
            }

            var targetIndex = Array.IndexOf(args, "--target");
            var target = targetIndex >= 0 && targetIndex + 1 < args.Length ? args[targetIndex + 1] : null;
            var approvedTargets = new HashSet<string>(StringComparer.Ordinal)
            {
                "20261101052030_PlatformRbacFoundation",
                "20261102010000_PlatformAdministrationHardening",
                "20261102020000_PlatformFunctionExecuteHardening",
                "20261102030000_RuntimeSchemaUsageHardening",
                "20261102040000_RuntimeIdentityOperationalAclHardening"
            };
            if (target is null || !approvedTargets.Contains(target))
            {
                Console.Error.WriteLine("An approved RBAC migration target is required.");
                return 2;
            }

            var migrationOptions = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(admin.ConnectionString,
                postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "app")).Options;
            await using var migrationDb = new AppDbContext(migrationOptions);
            Console.WriteLine($"RBAC test migration: invoking EF target '{target}' with the approved migration connection.");
            await migrationDb.Database.GetService<IMigrator>().MigrateAsync(target);
            Console.WriteLine($"RBAC migration target '{target}' completed on the approved isolated test database.");
            return 0;
        }
        catch (PostgresException exception)
        {
            Console.Error.WriteLine($"RBAC test database command failed (SQLSTATE {exception.SqlState}); details withheld.");
            return 1;
        }
    }
    if (args[0] is "rotate-platform-admin-password-isolated" or "rotate-platform-admin-password-simulation")
    {
        var simulation = args[0] == "rotate-platform-admin-password-simulation";
        var expectedHost = simulation ? SimulationPlatformAdminPasswordHost : IsolatedPlatformAdminPasswordHost;
        var expectedDatabase = simulation ? SimulationPlatformAdminPasswordDatabase : IsolatedPlatformAdminPasswordDatabase;
        var expectedBranchId = simulation ? SimulationPlatformAdminPasswordBranchId : IsolatedPlatformAdminPasswordBranchId;
        const string confirmationFlag = "--confirm-platform-admin-password-rotation";
        const string branchFlag = "--confirmed-branch-id";
        var branchIndex = Array.IndexOf(args, branchFlag);
        var confirmedBranchId = branchIndex >= 0 && branchIndex + 1 < args.Length
            ? args[branchIndex + 1]
            : null;
        if (!args.Contains(confirmationFlag, StringComparer.Ordinal)
            || !string.Equals(confirmedBranchId, expectedBranchId, StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Password rotation requires an explicit confirmation of the approved isolated Neon branch.");
            return 2;
        }

        var ownerConnection = configuration["RASS_DB_TEST_ADMIN"];
        // Deliberately read only the process value: this temporary credential must never be written to User Secrets.
        var platformConnection = Environment.GetEnvironmentVariable("RASS_DB_TEST_PLATFORM_ADMIN", EnvironmentVariableTarget.Process);
        if (string.IsNullOrWhiteSpace(ownerConnection) || string.IsNullOrWhiteSpace(platformConnection))
        {
            Console.Error.WriteLine("The owner credential and a process-only platform administration credential are required.");
            return 2;
        }

        NpgsqlConnectionStringBuilder owner;
        NpgsqlConnectionStringBuilder platform;
        try
        {
            owner = new NpgsqlConnectionStringBuilder(ownerConnection);
            platform = new NpgsqlConnectionStringBuilder(platformConnection);
        }
        catch (ArgumentException)
        {
            Console.Error.WriteLine("A password-rotation credential has an invalid connection-string format.");
            return 2;
        }

        static bool IsDirectVerifyFull(NpgsqlConnectionStringBuilder value, string host, string database, string role) =>
            string.Equals(value.Host, host, StringComparison.OrdinalIgnoreCase)
            && string.Equals(value.Database, database, StringComparison.Ordinal)
            && string.Equals(value.Username, role, StringComparison.Ordinal)
            && value.Host?.Contains("-pooler", StringComparison.OrdinalIgnoreCase) != true
            && value.SslMode == SslMode.VerifyFull
            && !value.NoResetOnClose
            && !value.Multiplexing;

        if (!IsDirectVerifyFull(owner, expectedHost, expectedDatabase, "neondb_owner")
            || !IsDirectVerifyFull(platform, expectedHost, expectedDatabase, PlatformAdminRole)
            || string.IsNullOrWhiteSpace(platform.Password)
            || platform.Password.Length < 32)
        {
            Console.Error.WriteLine("Password rotation accepts only the approved direct VerifyFull endpoint, database, roles, and a 32-character-or-longer process-only credential.");
            return 2;
        }

        async Task<(bool Valid, bool ServerAddressAvailable)> VerifyOwnerAndRoleSafetyAsync(NpgsqlConnection connection)
        {
            await using var command = new NpgsqlCommand("""
                SELECT current_database(),
                       current_user,
                       inet_server_addr() IS NOT NULL,
                       (SELECT rolcanlogin
                               AND NOT rolinherit
                               AND NOT rolsuper
                               AND NOT rolcreaterole
                               AND NOT rolcreatedb
                               AND NOT rolbypassrls
                               AND NOT rolreplication
                        FROM pg_roles
                        WHERE rolname = 'rass_platform_admin_v1'),
                       NOT pg_has_role('rass_platform_admin_v1', 'neon_superuser', 'MEMBER')
                """, connection);
            await using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return (
                reader.GetString(0) == expectedDatabase
                && reader.GetString(1) == "neondb_owner"
                && !reader.IsDBNull(3) && reader.GetBoolean(3)
                && reader.GetBoolean(4),
                reader.GetBoolean(2));
        }

        try
        {
            await using var ownerDbConnection = new NpgsqlConnection(owner.ConnectionString);
            await ownerDbConnection.OpenAsync();
            var before = await VerifyOwnerAndRoleSafetyAsync(ownerDbConnection);
            if (!before.Valid)
            {
                Console.Error.WriteLine("The owner identity or platform-administration role attributes are not approved for password rotation.");
                return 2;
            }

            // The password value comes only from the process environment and is bound as a parameter while quoting server-side.
            await using (var alter = new NpgsqlCommand("SELECT format('ALTER ROLE rass_platform_admin_v1 PASSWORD %L', @password)", ownerDbConnection))
            {
                alter.Parameters.AddWithValue("password", platform.Password);
                var statement = (string?)await alter.ExecuteScalarAsync();
                if (string.IsNullOrWhiteSpace(statement))
                {
                    Console.Error.WriteLine("Password rotation statement could not be prepared safely.");
                    return 1;
                }

                await using var executeAlter = new NpgsqlCommand(statement, ownerDbConnection);
                await executeAlter.ExecuteNonQueryAsync();
            }

            var after = await VerifyOwnerAndRoleSafetyAsync(ownerDbConnection);
            if (!after.Valid)
            {
                Console.Error.WriteLine("Platform-administration role attributes changed unexpectedly during password rotation.");
                return 1;
            }

            await using var platformDbConnection = new NpgsqlConnection(platform.ConnectionString);
            await platformDbConnection.OpenAsync();
            await using var identity = new NpgsqlCommand("SELECT current_database(), current_user, inet_server_addr() IS NOT NULL", platformDbConnection);
            await using var identityReader = await identity.ExecuteReaderAsync();
            await identityReader.ReadAsync();
            if (identityReader.GetString(0) != expectedDatabase
                || identityReader.GetString(1) != PlatformAdminRole)
            {
                Console.Error.WriteLine("The rotated platform-administration credential did not authenticate to the approved isolated database.");
                return 1;
            }

            Console.WriteLine($"Platform administration password rotated and authenticated with VerifyFull. Database '{expectedDatabase}', owner 'neondb_owner', role '{PlatformAdminRole}', server address available: {before.ServerAddressAvailable && identityReader.GetBoolean(2)}.");
            return 0;
        }
        catch (PostgresException exception)
        {
            Console.Error.WriteLine($"Platform administration password rotation failed (SQLSTATE {exception.SqlState}); details withheld.");
            return 1;
        }
    }
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

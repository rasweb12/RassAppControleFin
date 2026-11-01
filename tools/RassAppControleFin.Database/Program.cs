using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using RassAppControleFin.Infrastructure.Configuration;
using RassAppControleFin.Infrastructure.Persistence;
using RassAppControleFin.Infrastructure.Persistence.Seed;

if (args.Length == 0 || args[0] is not ("migrate" or "bootstrap" or "seed-development" or "probe" or "probe-tests" or "migrate-isolated-tests"))
{
    Console.Error.WriteLine("Commands: probe | probe-tests | migrate-isolated-tests --confirm-isolated-tests | migrate | bootstrap | seed-development --confirm-development");
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
    string? isolatedAdminConnection = null;
    if (args[0] is "probe-tests" or "migrate-isolated-tests")
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
        case "migrate": await db.Database.MigrateAsync(); break;
        case "bootstrap": await RuntimeRoleBootstrap.ApplyAsync(db, CancellationToken.None); break;
        case "seed-development": await DevelopmentSeed.ApplyAsync(db, CancellationToken.None); break;
    }
    Console.WriteLine("Database command completed.");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Database command failed ({exception.GetType().Name}); details withheld.");
    return 1;
}

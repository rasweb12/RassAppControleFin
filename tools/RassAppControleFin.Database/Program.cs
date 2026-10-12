using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using RassAppControleFin.Infrastructure.Persistence;
using RassAppControleFin.Infrastructure.Persistence.Seed;

if (args.Length == 0 || args[0] is not ("migrate" or "bootstrap" or "seed-development"))
{
    Console.Error.WriteLine("Commands: migrate | bootstrap | seed-development --confirm-development");
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
    var connectionString = configuration["RASS_MIGRATIONS_CONNECTION"];
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

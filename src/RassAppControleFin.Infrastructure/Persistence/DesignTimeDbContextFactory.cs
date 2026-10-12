using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace RassAppControleFin.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Scaffold without a secret; database update requires a direct privileged connection.
        var connection = Environment.GetEnvironmentVariable("RASS_MIGRATIONS_CONNECTION")
            ?? new ConfigurationBuilder().AddUserSecrets("RassAppControleFin-Database-Development")
                .Build()["RASS_MIGRATIONS_CONNECTION"]
            ?? "Host=localhost;Database=rass_design;Username=rass_migrations";
        var settings = new NpgsqlConnectionStringBuilder(connection);
        if (settings.Host?.Contains("-pooler", StringComparison.OrdinalIgnoreCase) == true)
            throw new InvalidOperationException("Migrations require a direct database connection.");
        settings.IncludeErrorDetail = false;
        settings.LogParameters = false;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(settings.ConnectionString, postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "app"))
            .Options;
        return new(options);
    }
}

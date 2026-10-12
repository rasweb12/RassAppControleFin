using Npgsql;
using RassAppControleFin.Infrastructure.Configuration;

namespace RassAppControleFin.Infrastructure.Persistence;

public static class DatabaseConnectionSettings
{
    public static bool IsValid(DatabaseOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.RuntimeRole)) return false;
        if (string.IsNullOrWhiteSpace(options.ConnectionString)) return true;
        try
        {
            var connection = new NpgsqlConnectionStringBuilder(options.ConnectionString);
            var local = connection.Host is "localhost" or "127.0.0.1" or "::1";
            return !string.IsNullOrWhiteSpace(connection.Host) && !string.IsNullOrWhiteSpace(connection.Database)
                && connection.Username == options.RuntimeRole
                && (local || connection.SslMode == SslMode.VerifyFull)
                && !connection.NoResetOnClose && !connection.Multiplexing
                && !connection.IncludeErrorDetail && !connection.LogParameters;
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException) { return false; }
    }

    public static NpgsqlConnectionStringBuilder Build(DatabaseOptions options)
    {
        var connection = new NpgsqlConnectionStringBuilder(string.IsNullOrWhiteSpace(options.ConnectionString)
            ? "Host=localhost;Database=unconfigured;Username=unconfigured"
            : options.ConnectionString)
        {
            MaxPoolSize = options.MaxPoolSize,
            MinPoolSize = 0,
            Timeout = options.ConnectionTimeoutSeconds,
            CommandTimeout = options.CommandTimeoutSeconds,
            IncludeErrorDetail = false,
            LogParameters = false,
            NoResetOnClose = false,
            Multiplexing = false,
            Enlist = false,
            ApplicationName = "RassAppControleFin"
        };
        return connection;
    }
}

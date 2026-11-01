using Microsoft.Extensions.Options;
using Npgsql;
using RassAppControleFin.Infrastructure.Configuration;

namespace RassAppControleFin.Infrastructure.Persistence;

/// <summary>
/// Holds the separate administrative datasource. An absent or unsafe configuration
/// leaves platform mutations unavailable; it never falls back to the financial role.
/// </summary>
public sealed class PlatformAdministrationDataSource : IAsyncDisposable
{
    private readonly NpgsqlDataSource? _dataSource;

    public PlatformAdministrationDataSource(IOptions<PlatformAdministrationOptions> options)
    {
        var value = options.Value;
        if (!string.IsNullOrWhiteSpace(value.ConnectionString)
            && DatabaseConnectionSettings.IsValid(value.ConnectionString, value.Role))
            _dataSource = new NpgsqlDataSourceBuilder(DatabaseConnectionSettings.Build(value).ConnectionString).Build();
    }

    public bool IsAvailable => _dataSource is not null;

    public Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        => _dataSource?.OpenConnectionAsync(cancellationToken).AsTask()
           ?? throw new InvalidOperationException("Platform administration database connection is unavailable.");

    public ValueTask DisposeAsync() => _dataSource?.DisposeAsync() ?? ValueTask.CompletedTask;
}

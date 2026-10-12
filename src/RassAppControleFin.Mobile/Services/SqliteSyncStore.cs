using Microsoft.Data.Sqlite;
using RassAppControleFin.Synchronization;

namespace RassAppControleFin.Mobile.Services;

public sealed class SqliteSyncStore(MobileSyncScope scope) : IOutboxStore, IInboxStore, ISyncQueue, ISyncStateStore, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private SqliteConnection? _connection;

    public async Task EnqueueAsync(SyncOperation operation, CancellationToken cancellationToken)
    {
        EnsureScope(operation.TenantId, operation.UserId, operation.DeviceId);
        await Within(async connection =>
        {
            await using var check = connection.CreateCommand();
            check.CommandText = "SELECT Payload FROM sync_outbox WHERE TenantId=$tenant AND OperationId=$operation";
            check.Parameters.AddWithValue("$tenant", operation.TenantId.ToString());
            check.Parameters.AddWithValue("$operation", operation.OperationId.ToString());
            var existing = await check.ExecuteScalarAsync(cancellationToken) as string;
            if (existing is not null)
            {
                if (!string.Equals(existing, operation.Payload, StringComparison.Ordinal))
                    throw new InvalidOperationException("OperationId already exists with another payload.");
                return;
            }
            await using var insert = connection.CreateCommand();
            insert.CommandText = """
                INSERT INTO sync_outbox (Id,TenantId,UserId,DeviceId,OperationId,EntityId,EntityType,OperationType,
                  CreatedAt,ClientTimestamp,ServerTimestamp,Version,SyncStatus,RetryCount,LastError,SchemaVersion,Payload,LeaseExpiresAt)
                VALUES ($id,$tenant,$user,$device,$operation,$entity,$type,$operationType,$created,$client,NULL,
                  $version,'Pending',$retry,NULL,$schema,$payload,NULL)
                """;
            Bind(insert, operation);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<SyncOperation>> ClaimPendingAsync(int limit, DateTimeOffset leaseExpiresAt,
        CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 100 || leaseExpiresAt <= DateTimeOffset.UtcNow) throw new ArgumentOutOfRangeException(nameof(limit));
        EnsureScope();
        return await Within(async connection =>
        {
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
            await Execute(connection, transaction, """
                UPDATE sync_outbox SET SyncStatus='Pending', LeaseExpiresAt=NULL
                WHERE TenantId=$tenant AND UserId=$user AND DeviceId=$device
                  AND SyncStatus='InFlight' AND LeaseExpiresAt <= $now
                """, cancellationToken, ("$now", DateTimeOffset.UtcNow.ToString("O")));
            var rows = new List<SyncOperation>();
            await using (var select = connection.CreateCommand())
            {
                select.Transaction = transaction;
                select.CommandText = """
                    SELECT Id,TenantId,UserId,DeviceId,OperationId,EntityId,EntityType,OperationType,CreatedAt,
                      ClientTimestamp,ServerTimestamp,Version,SyncStatus,RetryCount,LastError,SchemaVersion,Payload
                    FROM sync_outbox WHERE TenantId=$tenant AND UserId=$user AND DeviceId=$device AND SyncStatus='Pending'
                    ORDER BY CreatedAt,Id LIMIT $limit
                    """;
                ScopeParameters(select);
                select.Parameters.AddWithValue("$limit", limit);
                await using var reader = await select.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken)) rows.Add(Read(reader) with { SyncStatus = SyncStatus.InFlight });
            }
            foreach (var operation in rows)
                await Execute(connection, transaction, """
                    UPDATE sync_outbox SET SyncStatus='InFlight', LeaseExpiresAt=$lease
                    WHERE TenantId=$tenant AND OperationId=$operation
                    """, cancellationToken, ("$lease", leaseExpiresAt.ToString("O")), ("$operation", operation.OperationId.ToString()));
            await transaction.CommitAsync(cancellationToken);
            return (IReadOnlyList<SyncOperation>)rows;
        }, cancellationToken);
    }

    public Task AcknowledgeAsync(Guid operationId, CancellationToken cancellationToken) => Within(async connection =>
    {
        EnsureScope();
        await Execute(connection, null, """
            UPDATE sync_outbox SET SyncStatus='Applied', ServerTimestamp=$now, LeaseExpiresAt=NULL, LastError=NULL
            WHERE TenantId=$tenant AND UserId=$user AND DeviceId=$device AND OperationId=$operation
            """, cancellationToken, ("$now", DateTimeOffset.UtcNow.ToString("O")), ("$operation", operationId.ToString()));
    }, cancellationToken);

    public Task MarkFailedAsync(Guid operationId, string error, bool retryable, CancellationToken cancellationToken) => Within(async connection =>
    {
        EnsureScope();
        var message = string.IsNullOrWhiteSpace(error) ? "SYNC_FAILED" : error[..Math.Min(error.Length, 256)];
        await Execute(connection, null, """
            UPDATE sync_outbox SET SyncStatus=$status, RetryCount=RetryCount+1, LastError=$error, LeaseExpiresAt=NULL
            WHERE TenantId=$tenant AND UserId=$user AND DeviceId=$device AND OperationId=$operation
            """, cancellationToken, ("$status", retryable ? "Pending" : "Failed"), ("$error", message),
            ("$operation", operationId.ToString()));
    }, cancellationToken);

    public Task MarkConflictAsync(SyncConflict conflict, CancellationToken cancellationToken) => Within(async connection =>
    {
        EnsureScope(conflict.TenantId, conflict.UserId, scope.DeviceId!.Value);
        await Execute(connection, null, """
            UPDATE sync_outbox SET SyncStatus='Conflict', LastError=$error, LeaseExpiresAt=NULL
            WHERE TenantId=$tenant AND UserId=$user AND DeviceId=$device AND OperationId=$operation
            """, cancellationToken, ("$error", conflict.Type.ToString()), ("$operation", conflict.OperationId.ToString()));
    }, cancellationToken);

    public Task<bool> ContainsAsync(Guid operationId, CancellationToken cancellationToken) => Within(async connection =>
    {
        EnsureScope();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sync_inbox WHERE TenantId=$tenant AND UserId=$user AND OperationId=$operation";
        ScopeParameters(command);
        command.Parameters.AddWithValue("$operation", operationId.ToString());
        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }, cancellationToken);

    public Task StoreAsync(Guid operationId, CancellationToken cancellationToken) => Within(async connection =>
    {
        EnsureScope();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR IGNORE INTO sync_inbox (TenantId,UserId,OperationId,ReceivedAt)
            VALUES ($tenant,$user,$operation,$received)
            """;
        ScopeParameters(command);
        command.Parameters.AddWithValue("$operation", operationId.ToString());
        command.Parameters.AddWithValue("$received", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }, cancellationToken);

    public Task<string?> GetCursorAsync(CancellationToken cancellationToken) => Within(async connection =>
    {
        EnsureScope();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Cursor FROM sync_state WHERE TenantId=$tenant AND UserId=$user";
        ScopeParameters(command);
        return await command.ExecuteScalarAsync(cancellationToken) as string;
    }, cancellationToken);

    public Task SetCursorAsync(string cursor, CancellationToken cancellationToken) => Within(async connection =>
    {
        EnsureScope();
        if (string.IsNullOrWhiteSpace(cursor) || cursor.Length > 2048) throw new ArgumentException("Invalid cursor.", nameof(cursor));
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO sync_state (TenantId,UserId,Cursor,UpdatedAt) VALUES ($tenant,$user,$cursor,$updated)
            ON CONFLICT(TenantId,UserId) DO UPDATE SET Cursor=excluded.Cursor, UpdatedAt=excluded.UpdatedAt
            """;
        ScopeParameters(command);
        command.Parameters.AddWithValue("$cursor", cursor);
        command.Parameters.AddWithValue("$updated", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }, cancellationToken);

    private async Task<T> Within<T>(Func<SqliteConnection, Task<T>> action, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try { return await action(await OpenAsync(ct)); } finally { _gate.Release(); }
    }
    private async Task Within(Func<SqliteConnection, Task> action, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try { await action(await OpenAsync(ct)); } finally { _gate.Release(); }
    }
    private async Task<SqliteConnection> OpenAsync(CancellationToken ct)
    {
        if (_connection is not null) return _connection;
        var path = Path.Combine(FileSystem.AppDataDirectory, "rass-sync-v1.db");
        _connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWriteCreate }.ToString());
        await _connection.OpenAsync(ct);
        await using var command = _connection.CreateCommand();
        command.CommandText = """
            PRAGMA foreign_keys=ON;
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS sync_outbox (
              Id TEXT NOT NULL, TenantId TEXT NOT NULL, UserId TEXT NOT NULL, DeviceId TEXT NOT NULL, OperationId TEXT NOT NULL,
              EntityId TEXT NOT NULL, EntityType TEXT NOT NULL, OperationType TEXT NOT NULL, CreatedAt TEXT NOT NULL,
              ClientTimestamp TEXT NOT NULL, ServerTimestamp TEXT NULL, Version INTEGER NOT NULL, SyncStatus TEXT NOT NULL,
              RetryCount INTEGER NOT NULL, LastError TEXT NULL, SchemaVersion INTEGER NOT NULL, Payload TEXT NOT NULL,
              LeaseExpiresAt TEXT NULL, PRIMARY KEY (TenantId, OperationId));
            CREATE INDEX IF NOT EXISTS IX_sync_outbox_pending ON sync_outbox(TenantId,UserId,DeviceId,SyncStatus,CreatedAt);
            CREATE TABLE IF NOT EXISTS sync_inbox (
              TenantId TEXT NOT NULL, UserId TEXT NOT NULL, OperationId TEXT NOT NULL, ReceivedAt TEXT NOT NULL,
              PRIMARY KEY (TenantId,UserId,OperationId));
            CREATE TABLE IF NOT EXISTS sync_state (
              TenantId TEXT NOT NULL, UserId TEXT NOT NULL, Cursor TEXT NOT NULL, UpdatedAt TEXT NOT NULL,
              PRIMARY KEY (TenantId,UserId));
            """;
        await command.ExecuteNonQueryAsync(ct);
        return _connection;
    }
    private void EnsureScope() { if (!scope.IsReady) throw new InvalidOperationException("Sync scope is unavailable."); }
    private void EnsureScope(Guid tenantId, string userId, Guid deviceId)
    {
        EnsureScope();
        if (scope.TenantId != tenantId || scope.UserId != userId || scope.DeviceId != deviceId)
            throw new InvalidOperationException("Cross-scope local sync access is denied.");
    }
    private void ScopeParameters(SqliteCommand command)
    {
        command.Parameters.AddWithValue("$tenant", scope.TenantId!.Value.ToString());
        command.Parameters.AddWithValue("$user", scope.UserId!);
        command.Parameters.AddWithValue("$device", scope.DeviceId!.Value.ToString());
    }
    private async Task Execute(SqliteConnection connection, SqliteTransaction? transaction, string sql,
        CancellationToken ct, params (string Name, object Value)[] extra)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction; command.CommandText = sql;
        ScopeParameters(command);
        foreach (var (name, value) in extra) command.Parameters.AddWithValue(name, value);
        await command.ExecuteNonQueryAsync(ct);
    }
    private static void Bind(SqliteCommand command, SyncOperation operation)
    {
        command.Parameters.AddWithValue("$id", operation.Id.ToString()); command.Parameters.AddWithValue("$tenant", operation.TenantId.ToString());
        command.Parameters.AddWithValue("$user", operation.UserId); command.Parameters.AddWithValue("$device", operation.DeviceId.ToString());
        command.Parameters.AddWithValue("$operation", operation.OperationId.ToString()); command.Parameters.AddWithValue("$entity", operation.EntityId.ToString());
        command.Parameters.AddWithValue("$type", operation.EntityType); command.Parameters.AddWithValue("$operationType", operation.OperationType.ToString());
        command.Parameters.AddWithValue("$created", operation.CreatedAt.ToString("O")); command.Parameters.AddWithValue("$client", operation.ClientTimestamp.ToString("O"));
        command.Parameters.AddWithValue("$version", operation.Version); command.Parameters.AddWithValue("$retry", operation.RetryCount);
        command.Parameters.AddWithValue("$schema", operation.SchemaVersion); command.Parameters.AddWithValue("$payload", operation.Payload);
    }
    private static SyncOperation Read(SqliteDataReader r) => new(
        Guid.Parse(r.GetString(0)), Guid.Parse(r.GetString(1)), r.GetString(2), Guid.Parse(r.GetString(3)),
        Guid.Parse(r.GetString(4)), Guid.Parse(r.GetString(5)), r.GetString(6), Enum.Parse<SyncOperationType>(r.GetString(7)),
        DateTimeOffset.Parse(r.GetString(8)), DateTimeOffset.Parse(r.GetString(9)),
        r.IsDBNull(10) ? null : DateTimeOffset.Parse(r.GetString(10)), r.GetInt64(11), Enum.Parse<SyncStatus>(r.GetString(12)),
        r.GetInt32(13), r.IsDBNull(14) ? null : r.GetString(14), r.GetInt32(15), r.GetString(16));
    public async ValueTask DisposeAsync() { if (_connection is not null) await _connection.DisposeAsync(); _gate.Dispose(); }
}

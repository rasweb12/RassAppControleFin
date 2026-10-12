using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Synchronization;

public interface ISyncScope
{
    Guid? TenantId { get; }
    string? UserId { get; }
    Guid? DeviceId { get; }
    bool IsReady { get; }
}

public interface ISyncEngine
{
    Task<Result> SynchronizeAsync(CancellationToken cancellationToken);
}

public interface ISyncQueue
{
    Task EnqueueAsync(SyncOperation operation, CancellationToken cancellationToken);
}

public interface IOutboxStore
{
    Task<IReadOnlyList<SyncOperation>> ClaimPendingAsync(int limit,
        DateTimeOffset leaseExpiresAt, CancellationToken cancellationToken);
    Task AcknowledgeAsync(Guid operationId, CancellationToken cancellationToken);
    Task MarkFailedAsync(Guid operationId, string error, bool retryable, CancellationToken cancellationToken);
    Task MarkConflictAsync(SyncConflict conflict, CancellationToken cancellationToken);
}

public interface IInboxStore
{
    Task<bool> ContainsAsync(Guid operationId, CancellationToken cancellationToken);
    Task StoreAsync(Guid operationId, CancellationToken cancellationToken);
}

public interface ISyncConflictResolver
{
    Task<Result<SyncOperation>> ResolveAsync(SyncConflict conflict,
        CancellationToken cancellationToken);
}

public interface ISyncTransport
{
    Task<Result<SyncBatchResult>> PushAsync(IReadOnlyList<SyncOperation> operations,
        CancellationToken cancellationToken);
    Task<Result<SyncBatchResult>> PullAsync(string? cursor, CancellationToken cancellationToken);
}

public interface ISyncStateStore
{
    Task<string?> GetCursorAsync(CancellationToken cancellationToken);
    Task SetCursorAsync(string cursor, CancellationToken cancellationToken);
}

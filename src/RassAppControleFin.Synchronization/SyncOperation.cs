namespace RassAppControleFin.Synchronization;

public enum SyncOperationType { Create, Update, Delete }
public enum SyncStatus { Pending, InFlight, Applied, Conflict, Failed }
public enum SyncConflictType
{
    TransactionConflict, AccountConflict, BudgetConflict, GoalConflict,
    CategoryConflict, ProfileConflict, TransferConflict
}

public sealed record SyncOperation(
    Guid Id, Guid TenantId, string UserId, Guid DeviceId, Guid OperationId,
    Guid EntityId, string EntityType, SyncOperationType OperationType,
    DateTimeOffset CreatedAt, DateTimeOffset ClientTimestamp,
    DateTimeOffset? ServerTimestamp, long Version, SyncStatus SyncStatus,
    int RetryCount, string? LastError, int SchemaVersion, string Payload);

public sealed record SyncConflict(
    Guid ConflictId, Guid TenantId, string UserId, Guid OperationId, Guid EntityId,
    SyncConflictType Type, long LocalVersion, long ServerVersion,
    string LocalPayload, string ServerPayload, DateTimeOffset DetectedAt);

public sealed record SyncBatchResult(IReadOnlyList<Guid> AppliedOperationIds,
    IReadOnlyList<SyncConflict> Conflicts, string? NextCursor);

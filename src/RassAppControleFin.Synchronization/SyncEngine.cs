using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Synchronization;

public sealed class SyncEngine(IOutboxStore outbox, ISyncQueue queue, IInboxStore inbox, ISyncStateStore state,
    ISyncTransport transport, ISyncConflictResolver conflicts) : ISyncEngine
{
    public async Task<Result> SynchronizeAsync(CancellationToken cancellationToken)
    {
        var pending = await outbox.ClaimPendingAsync(25, DateTimeOffset.UtcNow.AddMinutes(2), cancellationToken);
        if (pending.Count > 0)
        {
            var pushed = await transport.PushAsync(pending, cancellationToken);
            if (!pushed.IsSuccess)
            {
                foreach (var operation in pending)
                    await outbox.MarkFailedAsync(operation.OperationId, pushed.Error!.Code, retryable: true, cancellationToken);
                return Result.Failure(pushed.Error!);
            }
            foreach (var operationId in pushed.Value.AppliedOperationIds)
                await outbox.AcknowledgeAsync(operationId, cancellationToken);
            foreach (var conflict in pushed.Value.Conflicts)
            {
                await outbox.MarkConflictAsync(conflict, cancellationToken);
                var resolution = await conflicts.ResolveAsync(conflict, cancellationToken);
                if (resolution.IsSuccess) await queue.EnqueueAsync(resolution.Value, cancellationToken);
            }
        }

        var pulled = await transport.PullAsync(await state.GetCursorAsync(cancellationToken), cancellationToken);
        if (!pulled.IsSuccess) return Result.Failure(pulled.Error!);
        foreach (var operationId in pulled.Value.AppliedOperationIds)
            await inbox.StoreAsync(operationId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(pulled.Value.NextCursor))
            await state.SetCursorAsync(pulled.Value.NextCursor, cancellationToken);
        return Result.Success();
    }
}

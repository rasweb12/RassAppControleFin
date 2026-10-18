using System.Globalization;
using System.Text;
using MediatR;
using RassAppControleFin.Application.Abstractions.Persistence;
using RassAppControleFin.MultiTenancy;
using RassAppControleFin.Security;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Application.Features.Synchronization;

public sealed record SyncChangeDto(Guid OperationId, Guid ResourceId, string EventType,
    DateTimeOffset OccurredAt);
public sealed record SyncPullView(IReadOnlyList<SyncChangeDto> Changes, string? NextCursor,
    string? AppliedCursor);
public sealed record PullSyncChangesQuery(string? Cursor, int Limit = 100)
    : IRequest<Result<SyncPullView>>;

public sealed class PullSyncChangesHandler(IFinanceSyncReadStore store, IUserContext user,
    ITenantContext tenant) : IRequestHandler<PullSyncChangesQuery, Result<SyncPullView>>
{
    public async Task<Result<SyncPullView>> Handle(PullSyncChangesQuery query, CancellationToken ct)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.Subject))
            return Result<SyncPullView>.Failure(new("IDENTITY_REQUIRED", "Autenticacao necessaria.", ErrorType.Unauthorized));
        if (!tenant.IsResolved || tenant.TenantId is null || !user.Permissions.Contains(Permissions.TransactionsRead))
            return Result<SyncPullView>.Failure(new("SYNC_FORBIDDEN", "Sincronizacao nao autorizada.", ErrorType.Forbidden));
        if (query.Limit is < 1 or > 200)
            return Result<SyncPullView>.Failure(new("SYNC_LIMIT_INVALID", "Limite de sincronizacao invalido.", ErrorType.Validation));
        if (!SyncCursor.TryParse(query.Cursor, out var after, out var afterId))
            return Result<SyncPullView>.Failure(new("SYNC_CURSOR_INVALID", "Cursor de sincronizacao invalido.", ErrorType.Validation));

        var pulled = await store.PullAsync(after, afterId, query.Limit, ct);
        if (!pulled.IsSuccess) return Result<SyncPullView>.Failure(pulled.Error!);
        var changes = pulled.Value.Changes.Select(change => new SyncChangeDto(change.OperationId,
            change.ResourceId, change.EventType, change.OccurredAt)).ToArray();
        var last = pulled.Value.Changes.LastOrDefault();
        var appliedCursor = last is null ? query.Cursor : SyncCursor.Create(last.OccurredAt, last.CursorId);
        return Result<SyncPullView>.Success(new(changes,
            pulled.Value.HasMore ? appliedCursor : null, appliedCursor));
    }
}

public static class SyncCursor
{
    public static string Create(DateTimeOffset timestamp, Guid id) => Convert.ToBase64String(
        Encoding.UTF8.GetBytes($"{timestamp.UtcTicks.ToString(CultureInfo.InvariantCulture)}:{id:N}"));

    public static bool TryParse(string? value, out DateTimeOffset? timestamp, out Guid? id)
    {
        timestamp = null;
        id = null;
        if (string.IsNullOrWhiteSpace(value)) return true;
        try
        {
            var parts = Encoding.UTF8.GetString(Convert.FromBase64String(value)).Split(':', StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || !long.TryParse(parts[0], CultureInfo.InvariantCulture, out var ticks)
                || !Guid.TryParseExact(parts[1], "N", out var parsed)) return false;
            timestamp = new DateTimeOffset(ticks, TimeSpan.Zero);
            id = parsed;
            return true;
        }
        catch (FormatException) { return false; }
        catch (ArgumentOutOfRangeException) { return false; }
    }
}

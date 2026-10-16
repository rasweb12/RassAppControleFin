using Microsoft.EntityFrameworkCore;
using RassAppControleFin.Application.Abstractions.Persistence;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Infrastructure.Persistence;

public sealed class FinanceSyncReadStore(AppDbContext db, FinanceUnitFactory factory) : IFinanceSyncReadStore
{
    public async Task<Result<SyncChangePage>> PullAsync(DateTimeOffset? after, Guid? afterId,
        int limit, CancellationToken ct)
    {
        var opened = await factory.BeginAsync(ct, true);
        if (!opened.IsSuccess) return Result<SyncChangePage>.Failure(opened.Error!);
        await using var scope = opened.Value;

        IQueryable<FinancialOutboxRow> rows = db.Set<FinancialOutboxRow>().AsNoTracking();
        if (after.HasValue && afterId.HasValue)
        {
            var timestamp = after.Value;
            var id = afterId.Value;
            rows = rows.Where(row => row.CreatedAt > timestamp
                || (row.CreatedAt == timestamp && row.Id.CompareTo(id) > 0));
        }
        rows = rows.OrderBy(row => row.CreatedAt).ThenBy(row => row.Id).Take(limit + 1);
        var page = await rows.Select(row => new SyncChange(row.OperationId, row.ResourceId,
            row.EventType, row.CreatedAt, row.Id)).ToListAsync(ct);
        var hasMore = page.Count > limit;
        if (hasMore) page.RemoveAt(page.Count - 1);
        return Result<SyncChangePage>.Success(new(page, hasMore));
    }
}

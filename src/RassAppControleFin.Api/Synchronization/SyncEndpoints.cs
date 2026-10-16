using MediatR;
using RassAppControleFin.Api.Http;
using RassAppControleFin.Application.Features.Synchronization;
using RassAppControleFin.Contracts;
using RassAppControleFin.Security;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Api.Synchronization;

public static class SyncEndpoints
{
    public static void MapSync(this WebApplication app)
    {
        app.MapGet("/api/v1/sync/pull", async (string? cursor, int? limit, ISender sender,
            HttpContext context, CancellationToken ct) =>
        {
            var result = await sender.Send(new PullSyncChangesQuery(cursor, limit ?? 100), ct);
            var response = result.IsSuccess
                ? Result<SyncPullDto>.Success(new(result.Value.Changes.Select(change => new RassAppControleFin.Contracts.SyncChangeDto(
                    change.OperationId, change.ResourceId, change.EventType, change.OccurredAt)).ToArray(),
                    result.Value.NextCursor))
                : Result<SyncPullDto>.Failure(result.Error!);
            return response.ToHttp(context);
        }).RequireAuthorization(Permissions.TransactionsRead).RequireRateLimiting("public-api");
    }
}

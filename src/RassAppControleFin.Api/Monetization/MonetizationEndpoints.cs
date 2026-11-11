using Microsoft.EntityFrameworkCore;
using RassAppControleFin.Api.Http;
using RassAppControleFin.Api.Identity;
using RassAppControleFin.Domain.Entities;
using RassAppControleFin.Infrastructure.Persistence;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Api.Monetization;

public static class MonetizationEndpoints
{
    public static IEndpointRouteBuilder MapMonetization(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/monetization").RequireAuthorization().RequireRateLimiting("public-api");
        group.MapGet("/entitlements", GetEntitlementsAsync);
        return app;
    }

    private static async Task<IResult> GetEntitlementsAsync(AppDbContext db, RequestIdentity identity, HttpContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(identity.Subject)) return Results.Unauthorized();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.user_id', {identity.Subject}, true)", cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var subscriptions = await db.UserSubscriptions.AsNoTracking()
            .Where(subscription => subscription.UserId == identity.Subject)
            .ToListAsync(cancellationToken);
        var plan = SubscriptionEntitlementResolver.EffectivePlan(subscriptions, now);
        var entitlements = await db.PlanEntitlements.AsNoTracking()
            .Where(entitlement => entitlement.Plan == plan)
            .OrderBy(entitlement => entitlement.Feature)
            .Select(entitlement => new EntitlementResponse(entitlement.Feature, entitlement.Enabled, entitlement.Limit))
            .ToListAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return Result<EntitlementsResponse>.Success(new(plan.ToString(), entitlements)).ToHttp(context);
    }

    private sealed record EntitlementsResponse(string Plan, IReadOnlyList<EntitlementResponse> Entitlements);
    private sealed record EntitlementResponse(string Feature, bool Enabled, int? Limit);
}

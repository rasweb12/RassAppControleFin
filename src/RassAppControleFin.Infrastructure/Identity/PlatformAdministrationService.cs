using Microsoft.EntityFrameworkCore;
using RassAppControleFin.Domain.Entities;
using RassAppControleFin.Infrastructure.Persistence;
using RassAppControleFin.Security;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Infrastructure.Identity;

/// <summary>Global operations are authorized from a self-readable assignment before the RLS admin gate is set.</summary>
public sealed class PlatformAdministrationService(AppDbContext db) : IPlatformAdministrationService
{
    private static readonly Error Forbidden = new("PLATFORM_FORBIDDEN", "Permissao de plataforma insuficiente.", ErrorType.Forbidden);
    private static readonly Error Conflict = new("PLATFORM_ROLE_CONFLICT", "Operacao administrativa recusada.", ErrorType.Conflict);

    public async Task<Result<PlatformDashboard>> DashboardAsync(string actor, CancellationToken ct)
    {
        await using var tx = await AuthorizeAsync(actor, ct);
        if (tx is null) return Result<PlatformDashboard>.Failure(Forbidden);
        var users = await db.UserProfiles.IgnoreQueryFilters().Select(x => x.UserId).Distinct().LongCountAsync(ct);
        var tenants = await db.Tenants.IgnoreQueryFilters().LongCountAsync(ct);
        var admins = await db.PlatformRoleAssignments.LongCountAsync(x => x.IsActive && x.Role == PlatformRole.SuperAdmin, ct);
        return Result<PlatformDashboard>.Success(new(users, tenants, admins));
    }

    public async Task<Result<IReadOnlyList<PlatformRoleView>>> RolesAsync(string actor, string userId, CancellationToken ct)
    {
        await using var tx = await AuthorizeAsync(actor, ct);
        if (tx is null) return Result<IReadOnlyList<PlatformRoleView>>.Failure(Forbidden);
        var values = await db.PlatformRoleAssignments.Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt)
            .Select(x => new PlatformRoleView(x.Id, x.UserId, x.Role.ToString(), x.IsActive, x.GrantedBy, x.GrantedAt,
                x.RevokedBy, x.RevokedAt, x.Reason)).ToListAsync(ct);
        return Result<IReadOnlyList<PlatformRoleView>>.Success(values);
    }

    public async Task<Result<PlatformRoleView>> GrantSuperAdminAsync(string actor, string targetUserId, string reason, CancellationToken ct)
    {
        if (actor == targetUserId) return Result<PlatformRoleView>.Failure(Conflict);
        await using var tx = await AuthorizeAsync(actor, ct);
        if (tx is null) return Result<PlatformRoleView>.Failure(Forbidden);
        await LockSuperAdminChangesAsync(ct);
        if (await db.PlatformRoleAssignments.AnyAsync(x => x.UserId == targetUserId && x.Role == PlatformRole.SuperAdmin && x.IsActive, ct))
            return Result<PlatformRoleView>.Failure(Conflict);
        var assignment = PlatformRoleAssignment.Grant(Guid.NewGuid(), targetUserId, PlatformRole.SuperAdmin, actor, reason, DateTimeOffset.UtcNow);
        db.PlatformRoleAssignments.Add(assignment);
        await AuditAsync(actor, "platform.role.granted", assignment.Id, ct);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return Result<PlatformRoleView>.Success(View(assignment));
    }

    public async Task<Result> RevokeSuperAdminAsync(string actor, string targetUserId, Guid assignmentId, string reason, CancellationToken ct)
    {
        await using var tx = await AuthorizeAsync(actor, ct);
        if (tx is null) return Result.Failure(Forbidden);
        await LockSuperAdminChangesAsync(ct);
        // The application context is intentionally NoTracking by default. Revocation
        // changes lifecycle state, therefore this specific aggregate must be tracked.
        var assignment = await db.PlatformRoleAssignments.AsTracking().SingleOrDefaultAsync(x => x.Id == assignmentId, ct);
        if (assignment is null || assignment.UserId != targetUserId || !assignment.IsActive || assignment.UserId == actor
            || await db.PlatformRoleAssignments.CountAsync(x => x.IsActive && x.Role == PlatformRole.SuperAdmin, ct) <= 1)
            return Result.Failure(Conflict);
        assignment.Revoke(actor, reason, DateTimeOffset.UtcNow);
        await AuditAsync(actor, "platform.role.revoked", assignment.Id, ct);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return Result.Success();
    }

    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?> AuthorizeAsync(string actor, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actor)) return null;
        var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('app.user_id', {actor}, true), set_config('app.tenant_id', {""}, true)", ct);
        var active = await db.PlatformRoleAssignments.AnyAsync(x => x.UserId == actor && x.IsActive && x.Role == PlatformRole.SuperAdmin, ct);
        if (!active) { await tx.RollbackAsync(ct); await tx.DisposeAsync(); return null; }
        await db.Database.ExecuteSqlRawAsync("SELECT set_config('app.platform_authorized', 'true', true)", ct);
        db.AuthorizePlatform(actor);
        return tx;
    }

    private Task AuditAsync(string actor, string action, Guid resourceId, CancellationToken ct) => db.Database.ExecuteSqlInterpolatedAsync($"""
        INSERT INTO app."AuditEntries" ("Id","UserId","TenantId","Action","ResourceId","Outcome","CorrelationId","CreatedAt")
        VALUES ({Guid.NewGuid()},{actor},{(Guid?)null},{action},{resourceId},{"success"},{Guid.NewGuid().ToString()},{DateTimeOffset.UtcNow})
        """, ct);

    // The application serializes grants and revocations so a concurrent request cannot
    // observe two different "last active administrator" states.  The database unique
    // index remains the final guard for duplicate active grants.
    private Task LockSuperAdminChangesAsync(CancellationToken ct)
        => db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtext('rass.platform.superadmin'))", ct);

    private static PlatformRoleView View(PlatformRoleAssignment x) => new(x.Id, x.UserId, x.Role.ToString(), x.IsActive, x.GrantedBy, x.GrantedAt, x.RevokedBy, x.RevokedAt, x.Reason);
}

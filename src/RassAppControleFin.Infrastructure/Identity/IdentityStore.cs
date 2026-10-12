using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using RassAppControleFin.Application.Abstractions.Identity;
using RassAppControleFin.Domain.Entities;
using RassAppControleFin.Infrastructure.Configuration;
using RassAppControleFin.Infrastructure.Persistence;
using RassAppControleFin.Security;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Infrastructure.Identity;

public sealed class IdentityStore(AppDbContext db, IOptions<DatabaseOptions> options) : IIdentityStore
{
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static Error Denied => new("SESSION_REJECTED", "Sessao invalida ou encerrada.", ErrorType.Unauthorized);

    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> Scope(string subject, Guid? tenant, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 256) throw new InvalidOperationException("Invalid identity.");
        var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            if (!await DatabaseSafety.VerifyAsync(db, options.Value.RuntimeRole, ct))
                throw new InvalidOperationException("Unsafe database role or schema.");
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                SELECT set_config('app.user_id', {subject}, true),
                       set_config('app.tenant_id', {tenant?.ToString() ?? ""}, true)
                """, ct);
            return transaction;
        }
        catch { await transaction.DisposeAsync(); throw; }
    }

    public async Task<Result<ApplicationSession>> RegisterSessionAsync(ProviderSession session, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        await using var tx = await Scope(session.Subject, null, ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO app."UserSessions"
                ("Id","UserId","CookieHash","TokenHash","AccessExpiresAt","ExpiresAt","CreatedAt")
            VALUES ({id},{session.Subject},{Hash(session.SessionToken)},{Hash(session.AccessToken)},
                {session.AccessExpiresAt},{session.SessionExpiresAt},{DateTimeOffset.UtcNow})
            """, ct);
        await Audit(session.Subject, null, "session.created", id, ct);
        await tx.CommitAsync(ct);
        return Result<ApplicationSession>.Success(new(id, session));
    }

    public async Task<Result<ApplicationSession>> RenewSessionAsync(Guid id, string previousCookie, ProviderSession session, CancellationToken ct)
    {
        await using var tx = await Scope(session.Subject, null, ct);
        var count = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE app."UserSessions" SET "CookieHash"={Hash(session.SessionToken)},
                "TokenHash"={Hash(session.AccessToken)}, "AccessExpiresAt"={session.AccessExpiresAt},
                "ExpiresAt"={session.SessionExpiresAt}
            WHERE "Id"={id} AND "CookieHash"={Hash(previousCookie)}
                AND "RevokedAt" IS NULL AND "ExpiresAt">now()
            """, ct);
        if (count != 1) return Result<ApplicationSession>.Failure(Denied);
        await Audit(session.Subject, null, "session.renewed", id, ct);
        await tx.CommitAsync(ct);
        return Result<ApplicationSession>.Success(new(id, session));
    }

    public async Task<Result> RevokeSessionAsync(Guid id, string previousCookie, ProviderSession session, CancellationToken ct)
    {
        await using var tx = await Scope(session.Subject, null, ct);
        var count = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE app."UserSessions" SET "RevokedAt"=now()
            WHERE "Id"={id} AND "CookieHash"={Hash(previousCookie)}
            """, ct);
        if (count != 1) return Result.Failure(Denied);
        await Audit(session.Subject, null, "session.revoked", id, ct);
        await tx.CommitAsync(ct);
        return Result.Success();
    }

    public async Task<bool> IsSessionActiveAsync(Guid id, string subject, string accessToken, CancellationToken ct)
    {
        await using var tx = await Scope(subject, null, ct);
        return await db.Database.SqlQuery<bool>($"""
            SELECT EXISTS(SELECT 1 FROM app."UserSessions" WHERE "Id"={id}
                AND "TokenHash"={Hash(accessToken)} AND "RevokedAt" IS NULL
                AND "ExpiresAt">now() AND "AccessExpiresAt">now()) AS "Value"
            """).SingleAsync(ct);
    }

    public async Task<Result<TenantInfo>> EnsurePersonalTenantAsync(string subject, CancellationToken ct)
    {
        Guid? personalId;
        await using (var lookup = await Scope(subject, null, ct))
            personalId = await db.Database.SqlQuery<Guid?>($"""
                SELECT "TenantId" AS "Value" FROM app."TenantMemberships" WHERE "IsPersonal" AND "IsActive"
                """).SingleOrDefaultAsync(ct);
        if (personalId is { } id)
        {
            var existing = await ListTenantsAsync(subject, ct);
            return Result<TenantInfo>.Success(existing.Single(t => t.Id == id));
        }
        var tenant = Tenant.Create(Guid.NewGuid(), "Pessoal", DateTimeOffset.UtcNow, subject);
        await using var tx = await Scope(subject, tenant.Id, ct);
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO app."Tenants" ("Id","Name","CreatedAt","OwnerUserId")
                VALUES ({tenant.Id},{tenant.Name},{tenant.CreatedAt},{subject});
                INSERT INTO app."TenantMemberships" ("TenantId","UserId","Role","IsActive","CreatedAt","IsPersonal")
                VALUES ({tenant.Id},{subject},{"TenantAdmin"},true,now(),true);
                INSERT INTO app."UserProfiles" ("Id","TenantId","UserId","DisplayName","CreatedAt","Version")
                VALUES ({Guid.NewGuid()},{tenant.Id},{subject},{"Usuario"},now(),1)
                """, ct);
            await Audit(subject, tenant.Id, "tenant.created", tenant.Id, ct);
            await tx.CommitAsync(ct);
            return Result<TenantInfo>.Success(new(tenant.Id, tenant.Name, "TenantAdmin"));
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await tx.RollbackAsync(ct);
            await tx.DisposeAsync();
            var tenants = await ListTenantsAsync(subject, ct);
            return tenants.Count > 0 ? Result<TenantInfo>.Success(tenants[0])
                : Result<TenantInfo>.Failure(new("ONBOARDING_CONFLICT", "Conflito ao criar tenant.", ErrorType.Conflict));
        }
    }

    public async Task<IReadOnlyList<TenantInfo>> ListTenantsAsync(string subject, CancellationToken ct)
    {
        await using var tx = await Scope(subject, null, ct);
        var memberships = await db.Database.SqlQuery<MembershipRow>($"""
            SELECT "TenantId", "Role" FROM app."TenantMemberships" WHERE "IsActive" AND "UserId"={subject}
            ORDER BY "CreatedAt", "TenantId" LIMIT 100
            """).ToListAsync(ct);
        var tenants = new List<TenantInfo>();
        foreach (var member in memberships)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT set_config('app.tenant_id', {member.TenantId.ToString()}, true)", ct);
            var name = await db.Database.SqlQuery<string>($"""
                SELECT "Name" AS "Value" FROM app."Tenants" WHERE "Id"={member.TenantId}
                """).SingleAsync(ct);
            tenants.Add(new(member.TenantId, name, member.Role));
        }
        return tenants;
    }

    public async Task<string?> GetMembershipRoleAsync(string subject, Guid tenantId, CancellationToken ct)
    {
        await using var tx = await Scope(subject, tenantId, ct);
        return await db.Database.SqlQuery<string>($"""
            SELECT "Role" AS "Value" FROM app."TenantMemberships"
            WHERE "TenantId"={tenantId} AND "UserId"={subject} AND "IsActive"
            """).SingleOrDefaultAsync(ct);
    }

    public async Task<Result> AddMemberAsync(string subject, Guid tenantId, string memberSubject, string role, CancellationToken ct)
    {
        if (!Enum.TryParse<MembershipRole>(role, out var parsed) || !Enum.IsDefined(parsed))
            return Result.Failure(new("INVALID_ROLE", "Perfil invalido.", ErrorType.Validation));
        var ownRole = await GetMembershipRoleAsync(subject, tenantId, ct);
        if (ownRole is null || !PermissionCatalog.ForRole(ownRole).Contains(Permissions.MembershipsManage))
            return Result.Failure(new("MEMBERSHIP_FORBIDDEN", "Permissao insuficiente.", ErrorType.Forbidden));
        var member = new TenantMembership(tenantId, memberSubject, parsed);
        await using var tx = await Scope(subject, tenantId, ct);
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO app."TenantMemberships" ("TenantId","UserId","Role","IsActive","CreatedAt","IsPersonal")
                VALUES ({member.TenantId},{member.UserId},{role},true,now(),false)
                """, ct);
            await Audit(subject, tenantId, "membership.created", tenantId, ct);
            await tx.CommitAsync(ct);
            return Result.Success();
        }
        catch (PostgresException e) when (e.SqlState is PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.InsufficientPrivilege)
        { return Result.Failure(new("MEMBERSHIP_CONFLICT", "Membership recusado.", ErrorType.Conflict)); }
    }

    private Task<int> Audit(string subject, Guid? tenant, string action, Guid resource, CancellationToken ct)
        => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO app."AuditEntries" ("Id","UserId","TenantId","Action","ResourceId","Outcome","CorrelationId","CreatedAt")
            VALUES ({Guid.NewGuid()},{subject},{tenant},{action},{resource},{"success"},
                {System.Diagnostics.Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString()},now())
            """, ct);

    private sealed class MembershipRow { public Guid TenantId { get; set; } public string Role { get; set; } = ""; }
}

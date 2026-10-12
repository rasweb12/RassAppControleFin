using Microsoft.EntityFrameworkCore;

namespace RassAppControleFin.Infrastructure.Persistence.Seed;

public static class DevelopmentSeed
{
    public static readonly Guid TenantA = Guid.Parse("11111111-1111-4111-8111-111111111111");
    public static readonly Guid TenantB = Guid.Parse("22222222-2222-4222-8222-222222222222");
    public static readonly Guid AliceProfile = Guid.Parse("33333333-3333-4333-8333-333333333333");
    public static readonly Guid BobProfile = Guid.Parse("44444444-4444-4444-8444-444444444444");
    public static readonly Guid CarolProfile = Guid.Parse("55555555-5555-4555-8555-555555555555");
    public const string Alice = "fixture-alice";
    public const string Bob = "fixture-bob";
    public const string Carol = "fixture-carol";

    public static async Task ApplyAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        // Synthetic memberships only; never provision provider credentials.
        var rows = new[]
        {
            (TenantA, AliceProfile, Alice, "Fixture tenant A"),
            (TenantA, BobProfile, Bob, "Fixture tenant A"),
            (TenantB, CarolProfile, Carol, "Fixture tenant B")
        };
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        foreach (var (tenantId, profileId, subject, name) in rows)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                SELECT set_config('app.tenant_id', {tenantId.ToString()}, true),
                       set_config('app.user_id', {subject}, true)
                """, cancellationToken);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO app."Tenants" ("Id", "Name", "CreatedAt")
                VALUES ({tenantId}, {name}, {DateTimeOffset.UtcNow})
                ON CONFLICT ("Id") DO NOTHING
                """, cancellationToken);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE app."Tenants" SET "OwnerUserId"={(tenantId == TenantA ? Alice : Carol)} WHERE "Id"={tenantId};
                INSERT INTO app."TenantMemberships" ("TenantId","UserId","Role","IsActive","IsPersonal","CreatedAt")
                VALUES ({tenantId},{subject},{(subject == Bob ? "User" : "TenantAdmin")},true,false,now())
                ON CONFLICT ("TenantId","UserId") DO NOTHING
                """, cancellationToken);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO app."UserProfiles" ("TenantId", "Id", "UserId", "DisplayName", "CreatedAt", "Version")
                VALUES ({tenantId}, {profileId}, {subject}, {subject}, {DateTimeOffset.UtcNow}, 1)
                ON CONFLICT ("TenantId", "UserId") DO NOTHING
                """, cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }
}

using RassAppControleFin.Domain.Entities;
using Xunit;

namespace RassAppControleFin.UnitTests;

public sealed class FoundationDomainTests
{
    [Fact]
    public void Effective_plan_ignores_expired_and_uses_the_current_subscription()
    {
        var now = DateTimeOffset.UtcNow;
        var expired = new UserSubscription(Guid.NewGuid(), "subject", SubscriptionPlan.PremiumAnnual,
            SubscriptionStatus.Active, PaymentProvider.GooglePlay, "expired", now.AddDays(-31), now.AddDays(-1), now.AddDays(-1));
        var active = new UserSubscription(Guid.NewGuid(), "subject", SubscriptionPlan.PremiumMonthly,
            SubscriptionStatus.Active, PaymentProvider.GooglePlay, "active", now.AddDays(-1), now.AddDays(29), now);

        Assert.Equal(SubscriptionPlan.PremiumMonthly,
            SubscriptionEntitlementResolver.EffectivePlan([expired, active], now));
    }

    [Fact]
    public void Lifetime_subscription_rejects_an_artificial_expiry()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentException>(() => new UserSubscription(Guid.NewGuid(), "subject", SubscriptionPlan.PremiumLifetime,
            SubscriptionStatus.Active, PaymentProvider.GooglePlay, "purchase", now, now.AddDays(1), now));
    }

    [Fact]
    public void Payment_event_requires_a_sha256_fingerprint_and_never_accepts_a_raw_token_shape()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentException>(() => new PaymentEvent(Guid.NewGuid(), PaymentProvider.GooglePlay, "event", "token", "product",
            PaymentEventStatus.Received, now, now));
        var eventRecord = new PaymentEvent(Guid.NewGuid(), PaymentProvider.GooglePlay, "event", new string('A', 64), "product",
            PaymentEventStatus.Received, now, now);
        Assert.Equal(64, eventRecord.PurchaseFingerprint.Length);
    }
    [Fact]
    public void Profile_preserves_opaque_subject_and_advances_explicit_version()
    {
        var tenant = Guid.NewGuid();
        var profile = UserProfile.Create(Guid.NewGuid(), tenant, "auth|not-a-uuid", " Alice ", DateTimeOffset.UtcNow);
        Assert.Equal("auth|not-a-uuid", profile.UserId);
        Assert.Equal(tenant, profile.TenantId);
        Assert.Equal("Alice", profile.DisplayName);
        Assert.Equal(1, profile.Version);
        profile.Rename(" Updated ");
        Assert.Equal("Updated", profile.DisplayName);
        Assert.Equal(2, profile.Version);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Empty_names_and_subjects_violate_domain_invariants(string invalid)
    {
        Assert.ThrowsAny<ArgumentException>(() => Tenant.Create(Guid.NewGuid(), invalid, DateTimeOffset.UtcNow, "fixture"));
        Assert.ThrowsAny<ArgumentException>(() => UserProfile.Create(Guid.NewGuid(), Guid.NewGuid(),
            invalid, "fixture", DateTimeOffset.UtcNow));
        Assert.ThrowsAny<ArgumentException>(() => UserProfile.Create(Guid.NewGuid(), Guid.NewGuid(),
            "fixture", invalid, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Distributed_ids_tenant_and_utc_are_required()
    {
        Assert.Throws<ArgumentException>(() => Tenant.Create(Guid.Empty, "fixture", DateTimeOffset.UtcNow, "fixture"));
        Assert.Throws<ArgumentException>(() => UserProfile.Create(Guid.NewGuid(), Guid.Empty, "fixture", "fixture", DateTimeOffset.UtcNow));
        var nonUtc = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-3));
        Assert.Throws<ArgumentException>(() => Tenant.Create(Guid.NewGuid(), "fixture", nonUtc, "fixture"));
        Assert.Throws<ArgumentException>(() => UserProfile.Create(Guid.NewGuid(), Guid.NewGuid(), "fixture", "fixture", nonUtc));
    }

    [Fact]
    public void Field_lengths_match_database_constraints()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Tenant.Create(Guid.NewGuid(), new string('a', 121), DateTimeOffset.UtcNow, "fixture"));
        Assert.Throws<ArgumentOutOfRangeException>(() => UserProfile.Create(Guid.NewGuid(), Guid.NewGuid(),
            new string('a', 257), "fixture", DateTimeOffset.UtcNow));
        var profile = UserProfile.Create(Guid.NewGuid(), Guid.NewGuid(), "fixture", "fixture", DateTimeOffset.UtcNow);
        Assert.Throws<ArgumentOutOfRangeException>(() => profile.Rename(new string('a', 121)));
        Assert.Equal(1, profile.Version);
    }
}

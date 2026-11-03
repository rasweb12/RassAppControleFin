namespace RassAppControleFin.Domain.Entities;

public enum SubscriptionPlan { Free, PremiumMonthly, PremiumAnnual, PremiumLifetime }
public enum SubscriptionStatus { Active, Pending, Expired, Cancelled, Suspended, Refunded }
public enum PaymentProvider { GooglePlay }
public enum PaymentEventStatus { Received, Accepted, Rejected, Reconciled }

/// <summary>Global commercial catalogue. Store prices are references, never a source of billing truth.</summary>
public sealed class PlanDefinition
{
    private PlanDefinition() { }
    public PlanDefinition(SubscriptionPlan plan, string displayName, decimal referencePrice, string currency, bool enabled, bool oneTime)
    {
        if (referencePrice < 0 || string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length > 120 || currency is not "BRL")
            throw new ArgumentException("Invalid plan definition.");
        Plan = plan; DisplayName = displayName.Trim(); ReferencePrice = decimal.Round(referencePrice, 2);
        Currency = currency; Enabled = enabled; OneTime = oneTime;
    }
    public SubscriptionPlan Plan { get; private set; }
    public string DisplayName { get; private set; } = "";
    public decimal ReferencePrice { get; private set; }
    public string Currency { get; private set; } = "BRL";
    public bool Enabled { get; private set; }
    public bool OneTime { get; private set; }
    public string? StoreProductId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public void ConfigureStoreProduct(string? productId, DateTimeOffset now)
    {
        if (now.Offset != TimeSpan.Zero || productId is { Length: > 160 }) throw new ArgumentException("Invalid store product.");
        StoreProductId = string.IsNullOrWhiteSpace(productId) ? null : productId.Trim(); UpdatedAt = now;
    }
}

/// <summary>Feature availability is server-authoritative; clients only render a helpful representation.</summary>
public sealed class PlanEntitlement
{
    private PlanEntitlement() { }
    public PlanEntitlement(SubscriptionPlan plan, string feature, bool enabled, int? limit, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(feature) || feature.Trim().Length > 120 || limit is < 0 || now.Offset != TimeSpan.Zero)
            throw new ArgumentException("Invalid entitlement.");
        Plan = plan; Feature = feature.Trim(); Enabled = enabled; Limit = limit; UpdatedAt = now;
    }
    public SubscriptionPlan Plan { get; private set; }
    public string Feature { get; private set; } = "";
    public bool Enabled { get; private set; }
    public int? Limit { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
}

public sealed class UserSubscription
{
    private UserSubscription() { }
    public UserSubscription(Guid id, string userId, SubscriptionPlan plan, SubscriptionStatus status, PaymentProvider? provider,
        string? externalPurchaseId, DateTimeOffset startedAt, DateTimeOffset? expiresAt, DateTimeOffset now)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(userId) || userId.Trim().Length > 256 ||
            externalPurchaseId is { Length: > 512 } || startedAt.Offset != TimeSpan.Zero || expiresAt?.Offset != TimeSpan.Zero || now.Offset != TimeSpan.Zero)
            throw new ArgumentException("Invalid subscription.");
        if (plan == SubscriptionPlan.PremiumLifetime && expiresAt is not null) throw new ArgumentException("Lifetime plans do not have an artificial expiry.");
        Id = id; UserId = userId.Trim(); Plan = plan; Status = status; Provider = provider;
        ExternalPurchaseId = string.IsNullOrWhiteSpace(externalPurchaseId) ? null : externalPurchaseId.Trim(); StartedAt = startedAt; ExpiresAt = expiresAt; UpdatedAt = now;
    }
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = "";
    public SubscriptionPlan Plan { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public PaymentProvider? Provider { get; private set; }
    public string? ExternalPurchaseId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public void SetStatus(SubscriptionStatus status, DateTimeOffset? expiresAt, DateTimeOffset now)
    {
        if (now.Offset != TimeSpan.Zero || expiresAt?.Offset != TimeSpan.Zero || (Plan == SubscriptionPlan.PremiumLifetime && expiresAt is not null))
            throw new ArgumentException("Invalid subscription transition.");
        Status = status; ExpiresAt = expiresAt; UpdatedAt = now;
    }
}

/// <summary>Idempotency/audit record. Never persist raw Google purchase tokens.</summary>
public sealed class PaymentEvent
{
    private PaymentEvent() { }
    public PaymentEvent(Guid id, PaymentProvider provider, string providerEventId, string purchaseFingerprint, string productId,
        PaymentEventStatus status, DateTimeOffset occurredAt, DateTimeOffset now)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(providerEventId) || providerEventId.Trim().Length > 512 ||
            string.IsNullOrWhiteSpace(purchaseFingerprint) || purchaseFingerprint.Length != 64 || string.IsNullOrWhiteSpace(productId) || productId.Trim().Length > 160 ||
            occurredAt.Offset != TimeSpan.Zero || now.Offset != TimeSpan.Zero) throw new ArgumentException("Invalid payment event.");
        Id = id; Provider = provider; ProviderEventId = providerEventId.Trim(); PurchaseFingerprint = purchaseFingerprint; ProductId = productId.Trim();
        Status = status; OccurredAt = occurredAt; RecordedAt = now;
    }
    public Guid Id { get; private set; }
    public PaymentProvider Provider { get; private set; }
    public string ProviderEventId { get; private set; } = "";
    public string PurchaseFingerprint { get; private set; } = "";
    public string ProductId { get; private set; } = "";
    public PaymentEventStatus Status { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
}

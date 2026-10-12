using RassAppControleFin.Domain.Common;

namespace RassAppControleFin.Domain.Entities;

public sealed class UserProfile : AggregateRoot
{
    private UserProfile(Guid id, Guid tenantId, string userId, string displayName,
        DateTimeOffset createdAt) : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        DisplayName = displayName;
        CreatedAt = createdAt;
    }

    public Guid TenantId { get; private set; }
    public string UserId { get; private set; }
    public string DisplayName { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public long Version { get; private set; } = 1;

    public static UserProfile Create(Guid id, Guid tenantId, string userId,
        string displayName, DateTimeOffset createdAt)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant required.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (userId.Length > 256) throw new ArgumentOutOfRangeException(nameof(userId));
        ValidateName(displayName);
        if (createdAt.Offset != TimeSpan.Zero) throw new ArgumentException("UTC required.", nameof(createdAt));
        return new(id, tenantId, userId, displayName.Trim(), createdAt);
    }

    public void Rename(string displayName)
    {
        ValidateName(displayName);
        DisplayName = displayName.Trim();
        Version = checked(Version + 1);
    }

    private static void ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Trim().Length > 120) throw new ArgumentOutOfRangeException(nameof(name));
    }
}

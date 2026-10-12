using RassAppControleFin.Domain.Common;

namespace RassAppControleFin.Domain.Entities;

public sealed class Tenant : AggregateRoot
{
    private Tenant(Guid id, string name, DateTimeOffset createdAt, string ownerUserId) : base(id)
    {
        Name = name;
        CreatedAt = createdAt;
        OwnerUserId = ownerUserId;
    }

    public string Name { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string OwnerUserId { get; private set; }

    public static Tenant Create(Guid id, string name, DateTimeOffset createdAt, string ownerUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerUserId);
        if (ownerUserId.Length > 256) throw new ArgumentOutOfRangeException(nameof(ownerUserId));
        if (name.Trim().Length > 120) throw new ArgumentOutOfRangeException(nameof(name));
        if (createdAt.Offset != TimeSpan.Zero) throw new ArgumentException("UTC required.", nameof(createdAt));
        return new(id, name.Trim(), createdAt, ownerUserId);
    }
}

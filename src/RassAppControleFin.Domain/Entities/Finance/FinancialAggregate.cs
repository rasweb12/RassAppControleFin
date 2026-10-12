using RassAppControleFin.Domain.Common;

namespace RassAppControleFin.Domain.Entities.Finance;

public interface IFinancialResource { Guid TenantId { get; } string UserId { get; } }
public abstract class FinancialAggregate : AggregateRoot, IFinancialResource
{
    protected FinancialAggregate(Guid id, Guid tenantId, string userId, DateTimeOffset createdAt) : base(id)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (userId.Length > 256 || createdAt.Offset != TimeSpan.Zero) throw new ArgumentException("Invalid scope.");
        TenantId = tenantId; UserId = userId; CreatedAt = createdAt;
    }
    public Guid TenantId { get; private set; }
    public string UserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public long Version { get; protected set; } = 1;
    protected static string NameValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Trim().Length > 120) throw new ArgumentException("Name exceeds limit.");
        return value.Trim();
    }
}

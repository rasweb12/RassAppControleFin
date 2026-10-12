namespace RassAppControleFin.Domain.Entities;

public enum MembershipRole { User, TenantAdmin, SupportAgent, FinancialAdmin }

public sealed class TenantMembership
{
    public Guid TenantId { get; }
    public string UserId { get; }
    public MembershipRole Role { get; }
    public TenantMembership(Guid tenantId, string userId, MembershipRole role)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant required.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (userId.Length > 256 || !Enum.IsDefined(role)) throw new ArgumentException("Invalid membership.");
        TenantId = tenantId; UserId = userId; Role = role;
    }
}

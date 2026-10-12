namespace RassAppControleFin.MultiTenancy;

public interface ITenantContext
{
    Guid? TenantId { get; }
    bool IsResolved { get; }
}

public interface ITenantMembershipService
{
    Task<bool> HasActiveMembershipAsync(string subject, Guid tenantId,
        CancellationToken cancellationToken);
}

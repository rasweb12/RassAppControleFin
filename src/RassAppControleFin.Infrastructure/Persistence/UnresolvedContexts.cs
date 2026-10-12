using RassAppControleFin.MultiTenancy;
using RassAppControleFin.Security;

namespace RassAppControleFin.Infrastructure.Persistence;

internal sealed class UnresolvedTenantContext : ITenantContext
{
    public Guid? TenantId => null;
    public bool IsResolved => false;
}

internal sealed class UnauthenticatedUser : ICurrentUser
{
    public bool IsAuthenticated => false;
    public string? Subject => null;
}

using RassAppControleFin.Application.Abstractions.Identity;
using RassAppControleFin.Security;

namespace RassAppControleFin.Infrastructure.Identity;

/// <summary>Resolves global authority from server-side assignments on every request.</summary>
public sealed class PlatformAuthorizationService(IIdentityStore identities) : IPlatformAuthorizationService
{
    public async Task<IReadOnlySet<string>> PermissionsAsync(string subject, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(subject)) return new HashSet<string>(StringComparer.Ordinal);
        var roles = await identities.GetActivePlatformRolesAsync(subject, cancellationToken);
        return PlatformPermissions.ForRoles(roles);
    }
}

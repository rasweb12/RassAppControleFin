using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Application.Abstractions.Identity;

public sealed record ProviderSession(string Subject, string AccessToken, string SessionToken,
    DateTimeOffset AccessExpiresAt, DateTimeOffset SessionExpiresAt);
public sealed record ApplicationSession(Guid SessionId, ProviderSession Provider);
public sealed record TenantInfo(Guid Id, string Name, string Role);
public sealed record ProfileInfo(Guid Id, string DisplayName, long Version);

public interface INeonAuthService
{
    Task<Result<ProviderSession>> SignInAsync(string email, string password, CancellationToken cancellationToken);
    Task<Result<ProviderSession>> SignUpAsync(string name, string email, string password, CancellationToken cancellationToken);
    Task<Result<ProviderSession>> RenewAsync(string sessionToken, CancellationToken cancellationToken);
    Task<Result> SignOutAsync(string sessionToken, CancellationToken cancellationToken);
    Task<Result> RequestPasswordResetAsync(string email, CancellationToken cancellationToken);
    Task<Result> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken);
}

public interface IIdentityStore
{
    Task<Result<ApplicationSession>> RegisterSessionAsync(ProviderSession session, CancellationToken cancellationToken);
    Task<Result<ApplicationSession>> RenewSessionAsync(Guid id, string previousCookie, ProviderSession session, CancellationToken cancellationToken);
    Task<Result> RevokeSessionAsync(Guid id, string previousCookie, ProviderSession session, CancellationToken cancellationToken);
    Task<bool> IsSessionActiveAsync(Guid id, string subject, string accessToken, CancellationToken cancellationToken);
    Task<Result<TenantInfo>> EnsurePersonalTenantAsync(string subject, CancellationToken cancellationToken);
    Task<IReadOnlyList<TenantInfo>> ListTenantsAsync(string subject, CancellationToken cancellationToken);
    Task<string?> GetMembershipRoleAsync(string subject, Guid tenantId, CancellationToken cancellationToken);
    Task<Result> AddMemberAsync(string subject, Guid tenantId, string memberSubject, string role,
        CancellationToken cancellationToken);
    Task<Result<ProfileInfo>> GetProfileAsync(string subject, Guid tenantId, CancellationToken cancellationToken);
    Task<Result<ProfileInfo>> UpdateProfileAsync(string subject, Guid tenantId, string displayName, long version,
        CancellationToken cancellationToken);
}

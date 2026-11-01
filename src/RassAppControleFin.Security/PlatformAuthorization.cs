namespace RassAppControleFin.Security;

public interface IPlatformAuthorizationService
{
    Task<IReadOnlySet<string>> PermissionsAsync(string subject, CancellationToken cancellationToken);
}

public sealed record PlatformDashboard(long Users, long Tenants, long SuperAdmins);
public sealed record PlatformRoleView(Guid Id, string UserId, string Role, bool IsActive,
    string GrantedBy, DateTimeOffset GrantedAt, string? RevokedBy, DateTimeOffset? RevokedAt, string Reason);

public interface IPlatformAdministrationService
{
    Task<SharedKernel.Results.Result<PlatformDashboard>> DashboardAsync(string actor, CancellationToken cancellationToken);
    Task<SharedKernel.Results.Result<IReadOnlyList<PlatformRoleView>>> RolesAsync(string actor, string userId, CancellationToken cancellationToken);
    Task<SharedKernel.Results.Result<PlatformRoleView>> GrantSuperAdminAsync(string actor, string targetUserId, string reason, CancellationToken cancellationToken);
    Task<SharedKernel.Results.Result> RevokeSuperAdminAsync(string actor, Guid assignmentId, string reason, CancellationToken cancellationToken);
}

public static class PlatformPermissions
{
    public const string DashboardRead = "Platform.Dashboard.Read";
    public const string UsersRead = "Platform.Users.Read";
    public const string UsersManage = "Platform.Users.Manage";
    public const string TenantsRead = "Platform.Tenants.Read";
    public const string TenantsManage = "Platform.Tenants.Manage";
    public const string RolesRead = "Platform.Roles.Read";
    public const string RolesManage = "Platform.Roles.Manage";
    public const string AuditRead = "Platform.Audit.Read";
    public const string PlansRead = "Platform.Plans.Read";
    public const string PlansManage = "Platform.Plans.Manage";
    public const string SubscriptionsRead = "Platform.Subscriptions.Read";
    public const string SubscriptionsManage = "Platform.Subscriptions.Manage";
    public const string EntitlementsManage = "Platform.Entitlements.Manage";

    public static IReadOnlySet<string> ForRoles(IEnumerable<string> roles)
        => roles.Contains("SuperAdmin", StringComparer.Ordinal)
            ? new HashSet<string>(typeof(PlatformPermissions).GetFields()
                .Select(field => (string)field.GetRawConstantValue()!), StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);
}

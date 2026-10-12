using RassAppControleFin.Application.Abstractions.Identity;
using RassAppControleFin.MultiTenancy;
using RassAppControleFin.Security;

namespace RassAppControleFin.Api.Identity;

public sealed class RequestIdentity(IHttpContextAccessor accessor) : IUserContext, ITenantContext
{
    public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated == true;
    public string? Subject => IsAuthenticated ? accessor.HttpContext?.User.FindFirst("sub")?.Value : null;
    public Guid? TenantId { get; private set; }
    public bool IsResolved => TenantId is not null;
    public IReadOnlySet<string> Permissions { get; private set; } = new HashSet<string>();
    public void Resolve(Guid id, string role) { TenantId = id; Permissions = PermissionCatalog.ForRole(role); }
}

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, RequestIdentity identity, IIdentityStore store)
    {
        if (context.Request.Path.StartsWithSegments("/api/v1/auth")) { await next(context); return; }
        var header = context.Request.Headers["X-Tenant-ID"];
        if (header.Count > 0 && identity.Subject is { } subject)
        {
            if (header.Count != 1 || !Guid.TryParse(header[0], out var tenant))
            { context.Response.StatusCode = 400; return; }
            var role = await store.GetMembershipRoleAsync(subject, tenant, context.RequestAborted);
            if (role is null) { context.Response.StatusCode = 403; return; }
            identity.Resolve(tenant, role);
        }
        await next(context);
    }
}

using RassAppControleFin.Api.Http;
using RassAppControleFin.Security;
using RassAppControleFin.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;

namespace RassAppControleFin.Api.Platform;

public sealed record PlatformRoleRequest(string UserId, string Reason, bool Confirmed);
public sealed record PlatformRevokeRequest(string Reason, bool Confirmed);

public static class PlatformEndpoints
{
    public static void MapPlatform(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/platform").RequireAuthorization().RequireRateLimiting("public-api");
        group.MapGet("/dashboard", async (IPlatformAdministrationService service, IUserContext user, HttpContext http, CancellationToken ct)
            => (await service.DashboardAsync(user.Subject!, ct)).ToHttp(http)).RequireAuthorization(PlatformPermissions.DashboardRead);
        group.MapGet("/users/{userId}/roles", async (string userId, IPlatformAdministrationService service, IUserContext user, HttpContext http, CancellationToken ct)
            => (await service.RolesAsync(user.Subject!, userId, ct)).ToHttp(http)).RequireAuthorization(PlatformPermissions.RolesRead);
        group.MapPost("/users/{userId}/roles", async (string userId, PlatformRoleRequest request, IPlatformAdministrationService service, IUserContext user, HttpContext http, CancellationToken ct) =>
        {
            if (!request.Confirmed || request.UserId != userId) return Result.Failure(new("PLATFORM_CONFIRMATION_REQUIRED", "Confirmacao explicita obrigatoria.", ErrorType.Validation)).ToHttp(http);
            return (await service.GrantSuperAdminAsync(user.Subject!, userId, request.Reason, ct)).ToHttp(http);
        }).RequireAuthorization(PlatformPermissions.RolesManage);
        group.MapDelete("/users/{userId}/roles/{assignmentId:guid}", async (string userId, Guid assignmentId, [FromBody] PlatformRevokeRequest request, IPlatformAdministrationService service, IUserContext user, HttpContext http, CancellationToken ct) =>
        {
            if (!request.Confirmed) return Result.Failure(new("PLATFORM_CONFIRMATION_REQUIRED", "Confirmacao explicita obrigatoria.", ErrorType.Validation)).ToHttp(http);
            return (await service.RevokeSuperAdminAsync(user.Subject!, assignmentId, request.Reason, ct)).ToHttp(http);
        }).RequireAuthorization(PlatformPermissions.RolesManage);
    }
}

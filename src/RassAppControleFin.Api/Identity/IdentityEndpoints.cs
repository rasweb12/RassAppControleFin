using MediatR;
using RassAppControleFin.Api.Http;
using RassAppControleFin.Application.Abstractions.Identity;
using RassAppControleFin.Application.Features.Identity;
using RassAppControleFin.Contracts;
using RassAppControleFin.Security;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Api.Identity;

public static class IdentityEndpoints
{
    public static void MapIdentity(this WebApplication app)
    {
        var auth = app.MapGroup("/api/v1/auth").AllowAnonymous().RequireRateLimiting("auth");
        auth.MapPost("/sign-in", async (SignInRequest input, ISender sender, HttpContext ctx, CancellationToken ct)
            => Session(await sender.Send(new SignInCommand(input.Email, input.Password), ct)).ToHttp(ctx));
        auth.MapPost("/sign-up", async (SignUpRequest input, ISender sender, HttpContext ctx, CancellationToken ct)
            => Session(await sender.Send(new SignUpCommand(input.Name, input.Email, input.Password), ct)).ToHttp(ctx));
        auth.MapPost("/renew", async (RenewSessionRequest input, ISender sender, HttpContext ctx, CancellationToken ct)
            => Session(await sender.Send(new RenewSessionCommand(input.SessionId, input.SessionToken), ct)).ToHttp(ctx));
        auth.MapPost("/sign-out", async (RenewSessionRequest input, ISender sender, HttpContext ctx, CancellationToken ct)
            => (await sender.Send(new EndSessionCommand(input.SessionId, input.SessionToken), ct)).ToHttp(ctx));
        auth.MapPost("/request-password-reset", async (PasswordResetRequest input, ISender sender, HttpContext ctx, CancellationToken ct)
            => (await sender.Send(new RequestPasswordResetCommand(input.Email), ct)).ToHttp(ctx));
        auth.MapPost("/reset-password", async (ResetPasswordRequest input, ISender sender, HttpContext ctx, CancellationToken ct)
            => (await sender.Send(new ResetPasswordCommand(input.Token, input.NewPassword), ct)).ToHttp(ctx));
        auth.MapGet("/reset-password", () => Results.Content(PasswordResetPage.Html, "text/html; charset=utf-8"));
        app.MapGet("/api/v1/tenants", async (ISender sender, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListTenantsQuery(), ct);
            return (result.IsSuccess
                ? Result<IReadOnlyList<TenantDto>>.Success(result.Value.Select(x => new TenantDto(x.Id, x.Name, x.Role)).ToArray())
                : Result<IReadOnlyList<TenantDto>>.Failure(result.Error!)).ToHttp(ctx);
        }).RequireAuthorization();
        app.MapPost("/api/v1/tenants/members", async (AddTenantMemberRequest input, ISender sender, HttpContext ctx, CancellationToken ct)
            => (await sender.Send(new AddMemberCommand(input.Subject, input.Role), ct)).ToHttp(ctx))
            .RequireAuthorization(Permissions.MembershipsManage);
    }
    private static Result<AuthSessionDto> Session(Result<ApplicationSession> result) => result.IsSuccess
        ? Result<AuthSessionDto>.Success(new(result.Value.SessionId, result.Value.Provider.AccessToken,
            result.Value.Provider.SessionToken, result.Value.Provider.Subject, result.Value.Provider.AccessExpiresAt,
            result.Value.Provider.SessionExpiresAt))
        : Result<AuthSessionDto>.Failure(result.Error!);
}

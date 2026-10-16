using FluentValidation;
using MediatR;
using RassAppControleFin.Application.Abstractions.Identity;
using RassAppControleFin.MultiTenancy;
using RassAppControleFin.Security;
using RassAppControleFin.SharedKernel.Results;

namespace RassAppControleFin.Application.Features.Identity;

public sealed record SignInCommand(string Email, string Password) : IRequest<Result<ApplicationSession>>;
public sealed record SignUpCommand(string Name, string Email, string Password) : IRequest<Result<ApplicationSession>>;
public sealed record RenewSessionCommand(Guid SessionId, string SessionToken) : IRequest<Result<ApplicationSession>>;
public sealed record EndSessionCommand(Guid SessionId, string SessionToken) : IRequest<Result>;
public sealed record RequestPasswordResetCommand(string Email) : IRequest<Result>;
public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest<Result>;
public sealed record ListTenantsQuery : IRequest<Result<IReadOnlyList<TenantInfo>>>;
public sealed record AddMemberCommand(string Subject, string Role) : IRequest<Result>;

public sealed class SignInValidator : AbstractValidator<SignInCommand>
{
    public SignInValidator()
    { RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254); RuleFor(x => x.Password).NotEmpty().MaximumLength(128); }
}
public sealed class SignUpValidator : AbstractValidator<SignUpCommand>
{
    public SignUpValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Password).Length(8, 128);
    }
}
public sealed class RenewSessionValidator : AbstractValidator<RenewSessionCommand>
{
    public RenewSessionValidator()
    { RuleFor(x => x.SessionId).NotEmpty(); RuleFor(x => x.SessionToken).NotEmpty().MaximumLength(4096); }
}
public sealed class EndSessionValidator : AbstractValidator<EndSessionCommand>
{
    public EndSessionValidator()
    { RuleFor(x => x.SessionId).NotEmpty(); RuleFor(x => x.SessionToken).NotEmpty().MaximumLength(4096); }
}
public sealed class RequestPasswordResetValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetValidator()
    { RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254); }
}
public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(4096);
        RuleFor(x => x.NewPassword).Length(8, 128);
    }
}
public sealed class AddMemberValidator : AbstractValidator<AddMemberCommand>
{
    public AddMemberValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Role).Must(x => x is "User" or "TenantAdmin" or "SupportAgent" or "FinancialAdmin");
    }
}

public sealed class SessionHandlers(INeonAuthService auth, IIdentityStore store, ICurrentUser user, ITenantContext tenant)
    : IRequestHandler<SignInCommand, Result<ApplicationSession>>,
      IRequestHandler<SignUpCommand, Result<ApplicationSession>>,
      IRequestHandler<RenewSessionCommand, Result<ApplicationSession>>,
      IRequestHandler<EndSessionCommand, Result>,
      IRequestHandler<RequestPasswordResetCommand, Result>,
      IRequestHandler<ResetPasswordCommand, Result>,
      IRequestHandler<ListTenantsQuery, Result<IReadOnlyList<TenantInfo>>>,
      IRequestHandler<AddMemberCommand, Result>
{
    public async Task<Result<ApplicationSession>> Handle(SignInCommand command, CancellationToken ct)
        => await Register(await auth.SignInAsync(command.Email, command.Password, ct), ct);
    public async Task<Result<ApplicationSession>> Handle(SignUpCommand command, CancellationToken ct)
        => await Register(await auth.SignUpAsync(command.Name, command.Email, command.Password, ct), ct);
    private async Task<Result<ApplicationSession>> Register(Result<ProviderSession> session, CancellationToken ct)
    {
        if (!session.IsSuccess) return Result<ApplicationSession>.Failure(session.Error!);
        var personal = await store.EnsurePersonalTenantAsync(session.Value.Subject, ct);
        if (!personal.IsSuccess) return Result<ApplicationSession>.Failure(personal.Error!);
        return await store.RegisterSessionAsync(session.Value, ct);
    }
    public async Task<Result<ApplicationSession>> Handle(RenewSessionCommand command, CancellationToken ct)
    {
        var session = await auth.RenewAsync(command.SessionToken, ct);
        return session.IsSuccess ? await store.RenewSessionAsync(command.SessionId, command.SessionToken, session.Value, ct)
            : Result<ApplicationSession>.Failure(session.Error!);
    }
    public async Task<Result> Handle(EndSessionCommand command, CancellationToken ct)
    {
        var session = await auth.RenewAsync(command.SessionToken, ct);
        if (!session.IsSuccess) return Result.Failure(session.Error!);
        var revoked = await store.RevokeSessionAsync(command.SessionId, command.SessionToken, session.Value, ct);
        if (!revoked.IsSuccess) return revoked;
        // Local revocation is committed before contacting the provider.
        await auth.SignOutAsync(session.Value.SessionToken, ct);
        return Result.Success();
    }
    public Task<Result> Handle(RequestPasswordResetCommand command, CancellationToken ct)
        => auth.RequestPasswordResetAsync(command.Email, ct);
    public Task<Result> Handle(ResetPasswordCommand command, CancellationToken ct)
        => auth.ResetPasswordAsync(command.Token, command.NewPassword, ct);
    public async Task<Result<IReadOnlyList<TenantInfo>>> Handle(ListTenantsQuery command, CancellationToken ct)
        => user.IsAuthenticated && user.Subject is not null
            ? Result<IReadOnlyList<TenantInfo>>.Success(await store.ListTenantsAsync(user.Subject, ct))
            : Result<IReadOnlyList<TenantInfo>>.Failure(new("AUTH_REQUIRED", "Autenticacao necessaria.", ErrorType.Unauthorized));
    public Task<Result> Handle(AddMemberCommand command, CancellationToken ct)
        => user.Subject is not null && tenant.TenantId is { } id
            ? store.AddMemberAsync(user.Subject, id, command.Subject, command.Role, ct)
            : Task.FromResult(Result.Failure(new("TENANT_REQUIRED", "Tenant autorizado necessario.", ErrorType.Forbidden)));

}

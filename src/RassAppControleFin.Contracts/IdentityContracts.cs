namespace RassAppControleFin.Contracts;

public sealed record SignInRequest(string Email, string Password);
public sealed record SignUpRequest(string Name, string Email, string Password);
public sealed record RenewSessionRequest(Guid SessionId, string SessionToken);
public sealed record PasswordResetRequest(string Email);
public sealed record ResetPasswordRequest(string Token, string NewPassword);
public sealed record AuthSessionDto(Guid SessionId, string AccessToken, string SessionToken,
    string Subject, DateTimeOffset AccessExpiresAt, DateTimeOffset SessionExpiresAt);
public sealed record TenantDto(Guid Id, string Name, string Role);
public sealed record AddTenantMemberRequest(string Subject, string Role);
public sealed record SessionInfoDto(string Subject, Guid SessionId);

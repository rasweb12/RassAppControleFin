using System.ComponentModel.DataAnnotations;

namespace RassAppControleFin.Infrastructure.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    public string ConnectionString { get; set; } = "";
    public string RuntimeRole { get; set; } = "rass_app_runtime_v2";
    [Range(1, 100)] public int MaxPoolSize { get; set; } = 10;
    [Range(1, 30)] public int ConnectionTimeoutSeconds { get; set; } = 10;
    [Range(1, 120)] public int CommandTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Dedicated, least-privilege connection for global platform administration.
/// It is deliberately separate from the financial runtime connection.
/// </summary>
public sealed class PlatformAdministrationOptions
{
    public const string SectionName = "PlatformAdministration";
    public string ConnectionString { get; set; } = "";
    public string Role { get; set; } = "rass_platform_admin_v1";
    [Range(1, 30)] public int ConnectionTimeoutSeconds { get; set; } = 10;
    [Range(1, 120)] public int CommandTimeoutSeconds { get; set; } = 30;
}

public sealed class NeonOptions
{
    public const string SectionName = "Neon";
    public string ProjectId { get; init; } = "";
    public string BranchId { get; init; } = "";
    public string AuthBaseUrl { get; init; } = "";
    public string JwksUrl { get; init; } = "";
    public string ExpectedIssuer { get; init; } = "";
    public string ExpectedAudience { get; init; } = "";
    public string SessionCookieName { get; init; } = "__Secure-neon-auth.session_token";
    public string Origin { get; init; } = "http://localhost:5081";
}

public sealed class EmailOptions
{
    public const string SectionName = "Email";
    [Required] public string SmtpHost { get; init; } = "smtp-relay.brevo.com";
    [Range(1, 65535)] public int SmtpPort { get; init; } = 587;
    [Required] public string Username { get; init; } = "";
    [Required] public string Password { get; init; } = "";
    [Required, EmailAddress] public string From { get; init; } = "";
    public bool UseSsl { get; init; } = true;
}

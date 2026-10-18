using System.ComponentModel.DataAnnotations;

namespace RassAppControleFin.Infrastructure.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    public string ConnectionString { get; init; } = "";
    public string RuntimeRole { get; init; } = "rass_app_runtime";
    [Range(1, 100)] public int MaxPoolSize { get; init; } = 10;
    [Range(1, 30)] public int ConnectionTimeoutSeconds { get; init; } = 10;
    [Range(1, 120)] public int CommandTimeoutSeconds { get; init; } = 30;
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

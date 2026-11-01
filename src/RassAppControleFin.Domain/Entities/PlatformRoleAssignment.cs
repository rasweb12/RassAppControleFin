namespace RassAppControleFin.Domain.Entities;

/// <summary>Platform roles are deliberately independent from tenant memberships.</summary>
public enum PlatformRole { SuperAdmin }

public sealed class PlatformRoleAssignment
{
    private PlatformRoleAssignment() { }

    private PlatformRoleAssignment(Guid id, string userId, PlatformRole role, string grantedBy,
        string reason, DateTimeOffset now)
    {
        Id = id; UserId = userId; Role = role; GrantedBy = grantedBy; Reason = reason;
        GrantedAt = now; CreatedAt = now; UpdatedAt = now; IsActive = true;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = "";
    public PlatformRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public string GrantedBy { get; private set; } = "";
    public DateTimeOffset GrantedAt { get; private set; }
    public string? RevokedBy { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string Reason { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static PlatformRoleAssignment Grant(Guid id, string userId, PlatformRole role,
        string grantedBy, string reason, DateTimeOffset now)
    {
        ValidateIdentity(userId); ValidateIdentity(grantedBy);
        if (!Enum.IsDefined(role) || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
            throw new ArgumentException("Invalid platform role assignment.");
        if (now.Offset != TimeSpan.Zero) throw new ArgumentException("UTC required.", nameof(now));
        return new PlatformRoleAssignment(id, userId, role, grantedBy, reason.Trim(), now);
    }

    public void Revoke(string revokedBy, string reason, DateTimeOffset now)
    {
        ValidateIdentity(revokedBy);
        if (!IsActive || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
            throw new InvalidOperationException("Platform role cannot be revoked.");
        if (now.Offset != TimeSpan.Zero) throw new ArgumentException("UTC required.", nameof(now));
        IsActive = false; RevokedBy = revokedBy; RevokedAt = now; Reason = reason.Trim(); UpdatedAt = now;
    }

    private static void ValidateIdentity(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 256) throw new ArgumentException("Invalid user identity.");
    }
}

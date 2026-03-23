namespace CephasOps.Domain.Users.Entities;

/// <summary>
/// One-time password reset token. Token value is stored hashed; raw token only in email link. (v1.3 Phase D)
/// </summary>
public class PasswordResetToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    /// <summary>
    /// SHA256 hash of the token (same pattern as RefreshToken). Raw token never stored.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Token expires at this time (UTC).
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// When the token was used (UTC). Null until used. Once set, token is invalid.
    /// </summary>
    public DateTime? UsedAtUtc { get; set; }

    /// <summary>
    /// When the token was created (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual User? User { get; set; }
}

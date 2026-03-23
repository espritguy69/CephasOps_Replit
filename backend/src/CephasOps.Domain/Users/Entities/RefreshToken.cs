namespace CephasOps.Domain.Users.Entities;

/// <summary>
/// Refresh token entity for JWT token refresh functionality
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// User ID this refresh token belongs to
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// The refresh token value (hashed)
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;
    
    /// <summary>
    /// Expiration date/time for the refresh token
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// Whether this token has been revoked
    /// </summary>
    public bool IsRevoked { get; set; } = false;
    
    /// <summary>
    /// When the token was revoked (if revoked)
    /// </summary>
    public DateTime? RevokedAt { get; set; }
    
    /// <summary>
    /// IP address where the token was created
    /// </summary>
    public string? CreatedFromIp { get; set; }

    /// <summary>
    /// User-Agent (device/browser) when the token was created. v1.4 Phase 3 session visibility.
    /// </summary>
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// When the token was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Navigation property to User
    /// </summary>
    public virtual User? User { get; set; }
}


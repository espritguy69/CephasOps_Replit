namespace CephasOps.Application.Auth.DTOs;

/// <summary>
/// Represents an active or revoked session (refresh token). v1.4 Phase 3.
/// </summary>
public class UserSessionDto
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public string? UserEmail { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsRevoked { get; set; }
}

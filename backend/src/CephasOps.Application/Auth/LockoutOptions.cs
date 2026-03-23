namespace CephasOps.Application.Auth;

/// <summary>
/// Configuration for account lockout after failed login attempts (v1.3 Phase C).
/// </summary>
public class LockoutOptions
{
    public const string SectionName = "Auth:Lockout";

    /// <summary>
    /// Number of failed attempts before lockout. Default 5.
    /// </summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>
    /// Lockout duration in minutes. Default 15.
    /// </summary>
    public int LockoutMinutes { get; set; } = 15;
}

namespace CephasOps.Application.Auth;

/// <summary>
/// Alert type constants for suspicious activity detection. v1.4 Phase 2.
/// </summary>
public static class SecurityAlertTypes
{
    /// <summary>More than N LoginFailed events for same user in M minutes.</summary>
    public const string ExcessiveLoginFailures = "ExcessiveLoginFailures";

    /// <summary>More than N PasswordResetRequested events for same user in M minutes.</summary>
    public const string PasswordResetAbuse = "PasswordResetAbuse";

    /// <summary>LoginSuccess from more than N different IPs for same user in M minutes.</summary>
    public const string MultipleIpLogin = "MultipleIpLogin";
}

namespace CephasOps.Application.Auth;

/// <summary>
/// Configurable thresholds for suspicious activity detection. v1.4 Phase 2.
/// </summary>
public static class SecurityDetectionRules
{
    /// <summary>Rule 1: Excessive login failures. Trigger when count &gt; this in WindowMinutes.</summary>
    public const int ExcessiveLoginFailuresThreshold = 10;

    /// <summary>Rule 1: Time window in minutes.</summary>
    public const int ExcessiveLoginFailuresWindowMinutes = 5;

    /// <summary>Rule 2: Password reset abuse. Trigger when count &gt; this in WindowMinutes.</summary>
    public const int PasswordResetAbuseThreshold = 3;

    /// <summary>Rule 2: Time window in minutes.</summary>
    public const int PasswordResetAbuseWindowMinutes = 15;

    /// <summary>Rule 3: Multiple IP login. Trigger when distinct IPs &gt;= this in WindowMinutes.</summary>
    public const int MultipleIpLoginDistinctIpCount = 3;

    /// <summary>Rule 3: Time window in minutes.</summary>
    public const int MultipleIpLoginWindowMinutes = 10;
}

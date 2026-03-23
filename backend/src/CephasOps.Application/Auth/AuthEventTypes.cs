namespace CephasOps.Application.Auth;

/// <summary>
/// Auth event action strings for audit log (EntityType=Auth). v1.4 Phase 1.
/// </summary>
public static class AuthEventTypes
{
    public const string LoginSuccess = "LoginSuccess";
    public const string LoginFailed = "LoginFailed";
    public const string AccountLocked = "AccountLocked";
    public const string PasswordChanged = "PasswordChanged";
    public const string PasswordResetRequested = "PasswordResetRequested";
    public const string PasswordResetCompleted = "PasswordResetCompleted";
    public const string AdminPasswordReset = "AdminPasswordReset";
    public const string TokenRefresh = "TokenRefresh";
    public const string Impersonation = "Impersonation";
}

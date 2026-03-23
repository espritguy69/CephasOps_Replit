namespace CephasOps.Application.Auth;

/// <summary>
/// Configuration for email-based password reset (v1.3 Phase D).
/// </summary>
public class PasswordResetOptions
{
    public const string SectionName = "Auth:PasswordReset";

    /// <summary>
    /// Email account ID used to send reset emails. If null, reset tokens are created but no email is sent.
    /// </summary>
    public Guid? EmailAccountId { get; set; }

    /// <summary>
    /// Token validity in minutes. Default 60.
    /// </summary>
    public int TokenExpiryMinutes { get; set; } = 60;

    /// <summary>
    /// Base URL of the frontend (e.g. https://app.example.com). Used to build the reset link. No trailing slash.
    /// </summary>
    public string FrontendResetUrlBase { get; set; } = "http://localhost:5173";
}

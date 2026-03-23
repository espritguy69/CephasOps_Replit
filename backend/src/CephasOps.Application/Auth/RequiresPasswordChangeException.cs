namespace CephasOps.Application.Auth;

/// <summary>
/// Thrown when credentials are valid but user must change password before logging in.
/// Controller should return 403 with requiresPasswordChange indicator.
/// </summary>
public class RequiresPasswordChangeException : Exception
{
    public RequiresPasswordChangeException()
        : base("You must change your password before signing in.")
    {
    }

    public RequiresPasswordChangeException(string message)
        : base(message)
    {
    }
}

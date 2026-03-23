namespace CephasOps.Application.Auth;

/// <summary>
/// Thrown when login is blocked because the account is temporarily locked due to repeated failed attempts.
/// Controller should return 423 (Locked) or 403 with accountLocked indicator.
/// </summary>
public class AccountLockedException : Exception
{
    public DateTime? LockoutEndUtc { get; }

    public AccountLockedException()
        : base("Your account is temporarily locked due to repeated failed sign-in attempts. Please try again later.")
    {
    }

    public AccountLockedException(DateTime? lockoutEndUtc)
        : base("Your account is temporarily locked due to repeated failed sign-in attempts. Please try again later.")
    {
        LockoutEndUtc = lockoutEndUtc;
    }
}

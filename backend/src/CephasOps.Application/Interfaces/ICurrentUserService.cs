namespace CephasOps.Application.Common.Interfaces;

/// <summary>
/// Service to access current user context from HTTP request
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Current user ID (from JWT token)
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Current company ID (from JWT token or context)
    /// </summary>
    Guid? CompanyId { get; }

    /// <summary>
    /// Current user email
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Whether the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Current user roles (from JWT token or claims)
    /// </summary>
    List<string> Roles { get; }

    /// <summary>
    /// Check if the current user has SuperAdmin role
    /// </summary>
    bool IsSuperAdmin { get; }

    /// <summary>
    /// Current service installer ID (if user is an SI)
    /// </summary>
    Guid? ServiceInstallerId { get; }
}


namespace CephasOps.Application.Departments.Services;

/// <summary>
/// Provides helpers for resolving and enforcing departmental access for the current user.
/// </summary>
public interface IDepartmentAccessService
{
    Task<DepartmentAccessResult> GetAccessAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the current user can operate within the provided department.
    /// Throws UnauthorizedAccessException if access is denied.
    /// </summary>
    Task EnsureAccessAsync(Guid departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve the effective department scope to use for an operation.
    /// </summary>
    Task<Guid?> ResolveDepartmentScopeAsync(Guid? requestedDepartmentId, CancellationToken cancellationToken = default);
}



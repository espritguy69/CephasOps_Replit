namespace CephasOps.Application.Common.Interfaces;

using CephasOps.Application.Common.DTOs;

/// <summary>
/// Resolves the effective company for a user from their department memberships only.
/// Used as request-time fallback when JWT company_id is null/empty. Does not override login-time resolution.
/// </summary>
public interface IUserCompanyFromDepartmentResolver
{
    /// <summary>
    /// Tries to resolve a single company from the user's active department memberships.
    /// Returns a single company only when the user has departments in exactly one company.
    /// </summary>
    /// <param name="userId">User id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Single company id, none, or ambiguous (multiple distinct companies).</returns>
    Task<DepartmentCompanyResolutionResult> TryGetSingleCompanyFromDepartmentsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

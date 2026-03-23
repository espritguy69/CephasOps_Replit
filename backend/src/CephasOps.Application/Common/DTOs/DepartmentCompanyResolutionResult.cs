namespace CephasOps.Application.Common.DTOs;

/// <summary>
/// Result of resolving a single company from a user's department memberships.
/// Used for request-time tenant fallback when JWT company is missing.
/// </summary>
public readonly record struct DepartmentCompanyResolutionResult
{
    /// <summary>Resolved company id when exactly one distinct company exists; null otherwise.</summary>
    public Guid? CompanyId { get; }

    /// <summary>True when user belongs to departments in more than one company; do not resolve.</summary>
    public bool Ambiguous { get; }

    public DepartmentCompanyResolutionResult(Guid? companyId, bool ambiguous)
    {
        CompanyId = companyId;
        Ambiguous = ambiguous;
    }

    public static DepartmentCompanyResolutionResult None => new(null, false);
    public static DepartmentCompanyResolutionResult Single(Guid companyId) => new(companyId, false);
    public static DepartmentCompanyResolutionResult AmbiguousMultiCompany => new(null, true);
}

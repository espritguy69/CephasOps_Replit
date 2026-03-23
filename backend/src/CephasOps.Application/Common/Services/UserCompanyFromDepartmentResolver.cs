using CephasOps.Application.Common.DTOs;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Common.Services;

/// <summary>
/// Resolves the effective company for a user from their active department memberships.
/// Used as request-time fallback when JWT company_id is null/empty.
/// </summary>
public class UserCompanyFromDepartmentResolver : IUserCompanyFromDepartmentResolver
{
    private readonly ApplicationDbContext _context;

    public UserCompanyFromDepartmentResolver(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<DepartmentCompanyResolutionResult> TryGetSingleCompanyFromDepartmentsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var companyIds = await _context.DepartmentMemberships
            .AsNoTracking()
            .Where(m => m.UserId == userId && !m.IsDeleted)
            .Join(
                _context.Departments,
                m => m.DepartmentId,
                d => d.Id,
                (m, d) => d.CompanyId)
            .Where(c => c != null && c != Guid.Empty)
            .Distinct()
            .ToListAsync(cancellationToken);

        return companyIds.Count switch
        {
            0 => DepartmentCompanyResolutionResult.None,
            1 => DepartmentCompanyResolutionResult.Single(companyIds[0]!.Value),
            _ => DepartmentCompanyResolutionResult.AmbiguousMultiCompany
        };
    }
}

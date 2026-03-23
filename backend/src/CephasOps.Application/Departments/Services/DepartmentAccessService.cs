using CephasOps.Application.Common.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Departments.Services;

/// <summary>
/// Resolves the departments a user can access at runtime.
/// </summary>
public class DepartmentAccessService : IDepartmentAccessService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<DepartmentAccessService> _logger;

    public DepartmentAccessService(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        IHostEnvironment hostEnvironment,
        ILogger<DepartmentAccessService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public async Task<DepartmentAccessResult> GetAccessAsync(CancellationToken cancellationToken = default)
    {
        if (_currentUserService.IsSuperAdmin)
        {
            return DepartmentAccessResult.Global;
        }

        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return DepartmentAccessResult.None;
        }

        var query = _context.DepartmentMemberships.AsNoTracking().Where(m => m.UserId == userId.Value);
        if (string.Equals(_hostEnvironment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
        {
            query = query.IgnoreQueryFilters();
        }

        var memberships = await query
            .Select(m => new { m.DepartmentId, m.IsDefault })
            .ToListAsync(cancellationToken);

        if (memberships.Count == 0)
        {
            _logger.LogWarning("User {UserId} has no department memberships", userId);
            // Only SuperAdmin and Admin may have global access without department membership
            if (_currentUserService.Roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
                return DepartmentAccessResult.Global;
            return DepartmentAccessResult.None;
        }

        var ids = memberships.Select(m => m.DepartmentId).Distinct().ToList();
        var defaultDepartment = memberships.FirstOrDefault(m => m.IsDefault)?.DepartmentId
            ?? ids.FirstOrDefault();

        return new DepartmentAccessResult(false, ids, defaultDepartment);
    }

    public async Task EnsureAccessAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        var result = await GetAccessAsync(cancellationToken);
        if (result.HasGlobalAccess)
        {
            return;
        }

        if (!result.DepartmentIds.Contains(departmentId))
        {
            _logger.LogWarning("User {UserId} attempted to access department {DepartmentId} without membership",
                _currentUserService.UserId, departmentId);
            throw new UnauthorizedAccessException("You do not have access to this department");
        }
    }

    public async Task<Guid?> ResolveDepartmentScopeAsync(Guid? requestedDepartmentId, CancellationToken cancellationToken = default)
    {
        var result = await GetAccessAsync(cancellationToken);

        if (result.HasGlobalAccess)
        {
            return requestedDepartmentId;
        }

        if (!requestedDepartmentId.HasValue)
        {
            if (result.DefaultDepartmentId.HasValue)
            {
                return result.DefaultDepartmentId.Value;
            }

            throw new UnauthorizedAccessException("Department selection is required");
        }

        if (!result.DepartmentIds.Contains(requestedDepartmentId.Value))
        {
            throw new UnauthorizedAccessException("You do not have access to this department");
        }

        return requestedDepartmentId;
    }
}



using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Departments.Services;
using CephasOps.Api.Common;
using CephasOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Users API Controller - provides user listing for dropdowns and task assignment.
/// Tenant-scoped: non-SuperAdmin only see users for current tenant (User.CompanyId).
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        ITenantProvider tenantProvider,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ILogger<UsersController> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all active users for dropdowns (e.g., task assignment)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<UserListDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<UserListDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<UserListDto>>>> GetUsers(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (!_currentUserService.IsSuperAdmin && (!companyId.HasValue || companyId.Value == Guid.Empty))
            return this.Error<List<UserListDto>>("Company context is required", 403);

        Guid? departmentScope = null;
        if (departmentId.HasValue || _departmentRequestContext.DepartmentId.HasValue)
        {
            try
            {
                departmentScope = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                return this.Error<List<UserListDto>>("You do not have access to this department", 403);
            }
        }

        var query = _context.Users.AsQueryable();
        if (!_currentUserService.IsSuperAdmin && companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(u => u.CompanyId == companyId.Value);

        // Filter by active status (default to active only)
        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }
        else
        {
            query = query.Where(u => u.IsActive);
        }

        // Filter by department if specified (resolved scope)
        if (departmentScope.HasValue)
        {
            query = query.Where(u => u.DepartmentMemberships.Any(dm => dm.DepartmentId == departmentScope.Value));
        }

        // Search by name or email
        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(u => u.Name.ToLower().Contains(searchLower) || 
                                     u.Email.ToLower().Contains(searchLower));
        }

        var users = await query
            .OrderBy(u => u.Name)
            .Select(u => new UserListDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Phone = u.Phone,
                IsActive = u.IsActive
            })
            .ToListAsync(cancellationToken);

        return this.Success(users);
    }

    /// <summary>
    /// Get a single user by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<UserDetailDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> GetUser(Guid id, CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (!_currentUserService.IsSuperAdmin && (!companyId.HasValue || companyId.Value == Guid.Empty))
            return this.Error<UserDetailDto>("Company context is required", 403);

        var query = _context.Users
            .Include(u => u.DepartmentMemberships)
                .ThenInclude(dm => dm.Department)
            .Where(u => u.Id == id);
        if (!_currentUserService.IsSuperAdmin && companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(u => u.CompanyId == companyId.Value);
        var user = await query.FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            return this.NotFound<UserDetailDto>("User not found");
        }

        try
        {
            var roles = await _context.UserRoles
                .Where(ur => ur.UserId == id)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role!.Name)
                .ToListAsync(cancellationToken);

            return this.Success(new UserDetailDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = roles,
            Departments = user.DepartmentMemberships.Select(dm => new UserDepartmentDto
            {
                DepartmentId = dm.DepartmentId,
                DepartmentName = dm.Department?.Name ?? "",
                Role = dm.Role
            }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user: {UserId}", id);
            return this.Error<UserDetailDto>($"Failed to get user: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Get users by department for assignment dropdowns
    /// </summary>
    [HttpGet("by-department/{departmentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<UserListDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<UserListDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<UserListDto>>>> GetUsersByDepartment(
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        var companyId = _tenantProvider.CurrentTenantId;
        if (!_currentUserService.IsSuperAdmin && (!companyId.HasValue || companyId.Value == Guid.Empty))
            return this.Error<List<UserListDto>>("Company context is required", 403);

        try
        {
            await _departmentAccessService.EnsureAccessAsync(departmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return this.Error<List<UserListDto>>("You do not have access to this department", 403);
        }

        var usersQuery = _context.Users
            .Where(u => u.IsActive)
            .Where(u => u.DepartmentMemberships.Any(dm => dm.DepartmentId == departmentId));
        if (!_currentUserService.IsSuperAdmin && companyId.HasValue && companyId.Value != Guid.Empty)
            usersQuery = usersQuery.Where(u => u.CompanyId == companyId.Value);
        var users = await usersQuery
            .OrderBy(u => u.Name)
            .Select(u => new UserListDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Phone = u.Phone,
                IsActive = u.IsActive
            })
            .ToListAsync(cancellationToken);

        return this.Success(users);
    }
}

#region DTOs

public class UserListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
}

public class UserDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<UserDepartmentDto> Departments { get; set; } = new();
}

public class UserDepartmentDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string? Role { get; set; }
}

#endregion


using CephasOps.Application.Common.Interfaces;
using CephasOps.Domain.Users.Entities;
using CephasOps.Infrastructure.Persistence;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Buildings.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Diagnostic endpoints for troubleshooting and database management
/// </summary>
[ApiController]
[Route("api/diagnostics")]
// No [Authorize] - allow public access for diagnostics
public class DiagnosticsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DiagnosticsController> _logger;
    private readonly IPasswordHasher _passwordHasher;

    public DiagnosticsController(
        ApplicationDbContext context,
        ILogger<DiagnosticsController> logger,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Check if default admin user exists and database seeding status
    /// </summary>
    [HttpGet("check-seeding")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> CheckSeeding()
    {
        try
        {
            // Test database connection first
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                return StatusCode(500, ApiResponse.ErrorResponse("Cannot connect to database. Check connection string and database server."));
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == "simon@cephas.com.my");

            // Company feature removed - no longer checking companies
            // var company = await _context.Companies...

            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "SuperAdmin" && r.Scope == "Global");

            // Check Phase 1 entities (GPON department and types). Diagnostics-only; no tenant data exposed; global filter applies.
#pragma warning disable CEPHAS004 // Tenant-scoped set queried by name for seed/diagnostics check only; endpoint is diagnostics-only.
            var gponDepartment = await _context.Departments
                .FirstOrDefaultAsync(d => d.Name == "GPON");
#pragma warning restore CEPHAS004
            
            var orderTypesCount = await _context.Set<OrderType>()
                .CountAsync();
            var installationTypesCount = await _context.Set<OrderCategory>()
                .CountAsync();
            var buildingTypesCount = await _context.Set<BuildingType>()
                .CountAsync();
            var splitterTypesCount = await _context.Set<SplitterType>()
                .CountAsync();

            var result = new
            {
                databaseConnected = true,
                userExists = user != null,
                userEmail = user?.Email,
                userHasPassword = !string.IsNullOrEmpty(user?.PasswordHash),
                companyExists = false, // Company feature removed
                companyName = (string?)null, // Company feature removed
                roleExists = role != null,
                roleName = role?.Name,
                gponDepartmentExists = gponDepartment != null,
                gponDepartmentName = gponDepartment?.Name,
                phase1Types = new
                {
                    orderTypesCount = orderTypesCount,
                    installationTypesCount = installationTypesCount,
                    buildingTypesCount = buildingTypesCount,
                    splitterTypesCount = splitterTypesCount
                }
            };

            return this.Success<object>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking seeding status");
            return StatusCode(500, ApiResponse.ErrorResponse($"Error checking seeding status: {ex.Message}"));
        }
    }

    /// <summary>
    /// Manually trigger database seeding
    /// </summary>
    [HttpPost("trigger-seed")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> TriggerSeed()
    {
        try
        {
            var seederLogger = HttpContext.RequestServices.GetRequiredService<ILogger<DatabaseSeeder>>();
            var seeder = new DatabaseSeeder(_context, seederLogger);
            
            await seeder.SeedAsync();
            
            // Check results
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == "simon@cephas.com.my");

            var result = new
            {
                success = true,
                message = "Database seeding completed",
                userCreated = user != null,
                userEmail = user?.Email
            };

            return this.Success<object>(result, "Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual seeding");
            return StatusCode(500, ApiResponse.ErrorResponse($"Error during manual seeding: {ex.Message}"));
        }
    }

    /// <summary>
    /// Check admin user status (development only)
    /// </summary>
    [HttpGet("check-admin-user")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> CheckAdminUser()
    {
        try
        {
            const string adminEmail = "simon@cephas.com.my";
            const string expectedPassword = "J@saw007";

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == adminEmail);

            if (user == null)
            {
                var userCheckResult = new
                {
                    userExists = false,
                    message = "User not found in database",
                    recommendation = "Restart the backend server to trigger database seeding"
                };
                return this.Success<object>(userCheckResult);
            }

            var hasPasswordHash = !string.IsNullOrEmpty(user.PasswordHash);
            var passwordValid = hasPasswordHash && user.PasswordHash != null &&
                _passwordHasher.VerifyPassword(expectedPassword, user.PasswordHash);

            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role != null ? ur.Role.Name : null)
                .Where(name => name != null)
                .Cast<string>()
                .ToListAsync();

            var issues = new List<string>();
            if (!user.IsActive) issues.Add("User is not active");
            if (!hasPasswordHash) issues.Add("User has no password hash");
            if (hasPasswordHash && !passwordValid) issues.Add("Password hash does not match expected password");
            if (!userRoles.Any()) issues.Add("User has no roles assigned");

            var result = new
            {
                userExists = true,
                email = user.Email,
                name = user.Name,
                isActive = user.IsActive,
                hasPasswordHash = hasPasswordHash,
                passwordHashLength = user.PasswordHash?.Length ?? 0,
                passwordValid = passwordValid,
                roles = userRoles,
                createdAt = user.CreatedAt,
                issues = issues,
                recommendation = passwordValid && user.IsActive && userRoles.Any()
                    ? "User looks good - check login endpoint logs for more details"
                    : "Restart the backend server to trigger database seeding which will fix these issues"
            };

            return this.Success<object>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking admin user");
            return StatusCode(500, ApiResponse.ErrorResponse($"Error checking admin user: {ex.Message}"));
        }
    }

    /// <summary>
    /// Fix admin user password (development only)
    /// </summary>
    [HttpPost("fix-admin-user")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> FixAdminUser()
    {
        try
        {
            const string adminEmail = "simon@cephas.com.my";
            const string adminPassword = "J@saw007";

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == adminEmail);

            if (user == null)
            {
                return BadRequest("User not found");
            }

            var correctHash = _passwordHasher.HashPassword(adminPassword);
            var changes = new List<string>();

            if (!user.IsActive)
            {
                user.IsActive = true;
                changes.Add("Activated user");
            }

            if (user.PasswordHash != correctHash)
            {
                user.PasswordHash = correctHash;
                changes.Add("Updated password hash (modern format)");
            }

            // Ensure SuperAdmin role
            var superAdminRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "SuperAdmin" && r.Scope == "Global");

            if (superAdminRole != null)
            {
                var hasRole = await _context.UserRoles
                    .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == superAdminRole.Id);

                if (!hasRole)
                {
                    _context.UserRoles.Add(new Domain.Users.Entities.UserRole
                    {
                        UserId = user.Id,
                        CompanyId = null,
                        RoleId = superAdminRole.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                    changes.Add("Assigned SuperAdmin role");
                }
            }

            if (changes.Any())
            {
                await _context.SaveChangesAsync();
                var result = new
                {
                    success = true,
                    changes = changes,
                    message = "User fixed successfully"
                };
                return this.Success<object>(result, "User fixed successfully.");
            }

            var noChangesResult = new
            {
                success = true,
                changes = new List<string>(),
                message = "User already correct - no changes needed"
            };
            return this.Success<object>(noChangesResult, "User already correct - no changes needed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fixing admin user");
            return StatusCode(500, ApiResponse.ErrorResponse($"Error fixing admin user: {ex.Message}"));
        }
    }
}

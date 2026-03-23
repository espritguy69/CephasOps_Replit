using CephasOps.Domain.Authorization;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Domain.Billing.Enums;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Users.Entities;
using CephasOps.Domain.Departments.Entities;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Buildings.Entities;
using CephasOps.Domain.ServiceInstallers.Entities;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Domain.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CephasOps.Domain.Inventory.Entities;

namespace CephasOps.Infrastructure.Persistence;

/// <summary>
/// Database seeder for initial/default data
/// Seeds default company, SuperAdmin role, and default admin user
/// </summary>
public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    // Default SuperAdmin user credentials
    private const string DefaultAdminEmail = "simon@cephas.com.my";
    private const string DefaultAdminPassword = "J@saw007";
    private const string DefaultAdminName = "Simon";

    // Default Company
    private const string DefaultCompanyName = "Cephas";
    private const string DefaultCompanyShortName = "Cephas";

    // Finance HOD user
    private const string FinanceHodName = "Samyu Kavitha";
    private const string FinanceHodEmail = "finance@cephas.com.my";
    private const string FinanceHodPassword = "E5pr!tg@L";
    private const string FinanceRoleName = "FinanceManager";

    public DatabaseSeeder(ApplicationDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seed the database with default data
    /// </summary>
    public async Task SeedAsync()
    {
        TenantSafetyGuard.EnterPlatformBypass();
        try
        {
            _logger.LogInformation("Starting database seed...");

            // Test database connectivity first
            if (!await _context.Database.CanConnectAsync())
            {
                _logger.LogError("Cannot connect to database. Please check your connection string and network connectivity.");
                _logger.LogError("This may be due to:");
                _logger.LogError("  1. Network connectivity issues");
                _logger.LogError("  2. DNS resolution failure (check if db.jgahsbfoydwdgipcjvxe.supabase.co resolves)");
                _logger.LogError("  3. Supabase database paused (free tier databases pause after inactivity)");
                _logger.LogError("  4. Firewall blocking the connection");
                throw new InvalidOperationException("Cannot connect to database. Check connection string and network connectivity.");
            }

            // Ensure database is created and migrated
            await _context.Database.MigrateAsync();

            // SaaS hardening: ensure default trial billing plan exists (provisioning resolves slug "trial")
            await EnsureDefaultTrialBillingPlanAsync();

            // Seed default company (single-company model)
            var company = await SeedDefaultCompanyAsync();

            // Seed all required roles
            var superAdminRole = await SeedSuperAdminRoleAsync();
            var adminRole = await EnsureRoleAsync("Admin", "Global");
            await SeedDirectorRoleAsync();
            await SeedHeadOfDepartmentRoleAsync();
            await SeedSupervisorRoleAsync();

            // Seed RBAC v2: permissions and role-permission assignments
            await SeedPermissionsAsync();
            await SeedRolePermissionsAsync(superAdminRole.Id, adminRole.Id);

            // Seed default admin user (no company required)
            await SeedDefaultAdminUserAsync(null, superAdminRole.Id);

            // Seed GPON department and default types (no company required)
            var gponDepartment = await SeedGponDepartmentAsync(null);

            // Seed Finance HOD user tied to GPON
            await SeedFinanceHodUserAsync(null, gponDepartment?.Id);

            // Seed default verticals removed (company feature disabled)
            // await SeedDefaultVerticalsAsync(company.Id);

            // Seed default types for GPON department (no company required)
            await SeedDefaultOrderTypesAsync(null, gponDepartment?.Id);
            await SeedDefaultOrderCategoriesAsync(null, gponDepartment?.Id);
            await SeedDefaultBuildingTypesAsync(null, gponDepartment?.Id);
            await SeedDefaultSplitterTypesAsync(null, gponDepartment?.Id);

            // Seed GPON service installers - DISABLED (installers will be imported separately)
            // await SeedGponServiceInstallersAsync(null, gponDepartment?.Id);

            // Seed default materials - DISABLED (materials should be imported via CSV)
            // Materials can be imported using: backend/scripts/import-materials.ps1
            // See: backend/scripts/materials-default.csv for default materials data
            // await SeedDefaultMaterialsAsync(null);

            // Seed material categories from existing materials
            // Note: This will work even if materials are imported via CSV instead of seeding
            await SeedMaterialCategoriesFromMaterialsAsync(null);
            
            // Seed default material categories if none exist
            await SeedDefaultMaterialCategoriesAsync(null);

            // Seed default parser templates for TIME orders
            await SeedDefaultParserTemplatesAsync();
            await SeedDefaultSkillsAsync(company?.Id);

            // Seed default guard conditions and side effects for workflow engine (settings-driven)
            await SeedDefaultGuardConditionsAsync(company?.Id);
            await SeedDefaultSideEffectsAsync(company?.Id);

            // Seed default Invoice document template
            await SeedDefaultInvoiceTemplateAsync(company?.Id);

            // Seed SMS/WhatsApp notification settings
            await SeedSmsWhatsAppGlobalSettingsAsync();

            // Seed default MovementTypes and LocationTypes
            await SeedDefaultMovementTypesAsync(company?.Id);
            await SeedDefaultLocationTypesAsync(company?.Id);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Database seed completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding database");
            throw;
        }
        finally
        {
            TenantSafetyGuard.ExitPlatformBypass();
        }
    }

    /// <summary>
    /// Ensures a default "trial" billing plan exists so provisioning without explicit PlanSlug always resolves.
    /// Idempotent: only inserts when no plan with slug "trial" exists. See docs/saas_scaling/SAAS_OPERATIONS_HARDENING_REPORT.md.
    /// </summary>
    private async Task EnsureDefaultTrialBillingPlanAsync()
    {
        var exists = await _context.BillingPlans.AnyAsync(p => p.Slug == "trial");
        if (exists) return;

        var now = DateTime.UtcNow;
        _context.BillingPlans.Add(new BillingPlan
        {
            Id = Guid.NewGuid(),
            Name = "Trial",
            Slug = "trial",
            BillingCycle = BillingCycle.Monthly,
            Price = 0,
            Currency = "MYR",
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created default billing plan: slug=trial (evaluation tenants).");
    }

    private async Task<Role> EnsureRoleAsync(string roleName, string scope)
    {
        var existingRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == roleName && r.Scope == scope);

        if (existingRole != null)
        {
            return existingRole;
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            Scope = scope
        };

        _context.Roles.Add(role);
        _logger.LogInformation("Created role {RoleName} ({Scope})", roleName, scope);
        return role;
    }

    private async Task SeedPermissionsAsync()
    {
        foreach (var name in PermissionCatalog.All)
        {
            var exists = await _context.Permissions.AnyAsync(p => p.Name == name);
            if (exists) continue;
            _context.Permissions.Add(new Permission
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = null
            });
            _logger.LogInformation("Created permission {PermissionName}", name);
        }
    }

    private async Task SeedRolePermissionsAsync(Guid superAdminRoleId, Guid adminRoleId)
    {
        var permissionIdsByName = await _context.Permissions
            .Where(p => PermissionCatalog.All.Contains(p.Name))
            .ToDictionaryAsync(p => p.Name, p => p.Id);

        foreach (var name in PermissionCatalog.All)
        {
            if (!permissionIdsByName.TryGetValue(name, out var permissionId)) continue;

            var superAdminHas = await _context.RolePermissions
                .AnyAsync(rp => rp.RoleId == superAdminRoleId && rp.PermissionId == permissionId);
            if (!superAdminHas)
            {
                _context.RolePermissions.Add(new RolePermission { RoleId = superAdminRoleId, PermissionId = permissionId });
                _logger.LogDebug("Assigned permission {PermissionName} to SuperAdmin", name);
            }

            // Admin gets admin.*, payout.*, rates.*, payroll.*, orders.*, reports.*, inventory.*, jobs.*, settings.* by default (RBAC v2 Phase 2–4)
            var adminGets = name.StartsWith("admin.", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("payout.", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("rates.", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("payroll.", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("orders.", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("reports.", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("inventory.", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("jobs.", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("settings.", StringComparison.OrdinalIgnoreCase);
            if (!adminGets) continue;
            var adminHas = await _context.RolePermissions
                .AnyAsync(rp => rp.RoleId == adminRoleId && rp.PermissionId == permissionId);
            if (!adminHas)
            {
                _context.RolePermissions.Add(new RolePermission { RoleId = adminRoleId, PermissionId = permissionId });
                _logger.LogDebug("Assigned permission {PermissionName} to Admin", name);
            }
        }
    }

    private async Task<Company> SeedDefaultCompanyAsync()
    {
        // If a company already exists, just return it (single-company model)
        var existingCompany = await _context.Companies
            .OrderBy(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (existingCompany != null)
        {
            _logger.LogInformation("Default company already exists: {CompanyName}", existingCompany.LegalName);
            return existingCompany;
        }

        var company = new Company
        {
            Id = Guid.NewGuid(),
            LegalName = DefaultCompanyName,
            ShortName = DefaultCompanyShortName,
            Vertical = "General",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Companies.Add(company);
        _logger.LogInformation("Created default company: {CompanyName}", DefaultCompanyName);

        return company;
    }

    private async Task<Role> SeedSuperAdminRoleAsync()
    {
        var existingRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == "SuperAdmin" && r.Scope == "Global");

        if (existingRole != null)
        {
            _logger.LogInformation("SuperAdmin role already exists");
            return existingRole;
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "SuperAdmin",
            Scope = "Global"
        };

        _context.Roles.Add(role);
        _logger.LogInformation("Created SuperAdmin role");

        return role;
    }

    private async Task<Role> SeedDirectorRoleAsync()
    {
        var existingRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == "Director" && r.Scope == "Global");

        if (existingRole != null)
        {
            _logger.LogInformation("Director role already exists");
            return existingRole;
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "Director",
            Scope = "Global"
        };

        _context.Roles.Add(role);
        _logger.LogInformation("Created Director role");

        return role;
    }

    private async Task<Role> SeedHeadOfDepartmentRoleAsync()
    {
        var existingRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == "HeadOfDepartment" && r.Scope == "Global");

        if (existingRole != null)
        {
            _logger.LogInformation("HeadOfDepartment role already exists");
            return existingRole;
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "HeadOfDepartment",
            Scope = "Global"
        };

        _context.Roles.Add(role);
        _logger.LogInformation("Created HeadOfDepartment role");

        return role;
    }

    private async Task<Role> SeedSupervisorRoleAsync()
    {
        var existingRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == "Supervisor" && r.Scope == "Global");

        if (existingRole != null)
        {
            _logger.LogInformation("Supervisor role already exists");
            return existingRole;
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "Supervisor",
            Scope = "Global"
        };

        _context.Roles.Add(role);
        _logger.LogInformation("Created Supervisor role");

        return role;
    }

    private async Task SeedDefaultAdminUserAsync(Guid? companyId, Guid superAdminRoleId)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == DefaultAdminEmail);

        if (existingUser != null)
        {
            _logger.LogInformation("Default admin user already exists: {Email}", DefaultAdminEmail);

            // Ensure user is active
            if (!existingUser.IsActive)
            {
                existingUser.IsActive = true;
                _logger.LogInformation("Activated existing admin user");
            }

            // Ensure user has correct password hash (in case it was corrupted or changed)
            var correctHash = HashPassword(DefaultAdminPassword);
            if (existingUser.PasswordHash != correctHash)
            {
                existingUser.PasswordHash = correctHash;
                _logger.LogInformation("Updated password hash for existing admin user");
            }

            // Company feature removed - no longer linking users to companies
            // var existingUserCompany = await _context.UserCompanies...

            // Ensure user has SuperAdmin role (no company required for global roles)
            var existingUserRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == existingUser.Id && 
                                          ur.RoleId == superAdminRoleId);

            if (existingUserRole == null)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = existingUser.Id,
                    CompanyId = null, // Global role, no company required
                    RoleId = superAdminRoleId,
                    CreatedAt = DateTime.UtcNow
                });
                _logger.LogInformation("Assigned SuperAdmin role to existing user");
            }

            return;
        }

        // Create new user
        var passwordHash = HashPassword(DefaultAdminPassword);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = DefaultAdminName,
            Email = DefaultAdminEmail,
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        // Company feature removed - no longer linking users to companies
        // _context.UserCompanies.Add(new UserCompany { ... });

        // Assign SuperAdmin role (global role, no company required)
        _context.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            CompanyId = null, // Global role, no company required
            RoleId = superAdminRoleId,
            CreatedAt = DateTime.UtcNow
        });

        _logger.LogInformation("Created default admin user: {Email}", DefaultAdminEmail);
    }

    private async Task SeedFinanceHodUserAsync(Guid? companyId, Guid? departmentId)
    {
        if (!departmentId.HasValue)
        {
            _logger.LogWarning("Cannot seed Finance HOD user because GPON department is missing");
            return;
        }

        var financeRole = await EnsureRoleAsync(FinanceRoleName, "Global");

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == FinanceHodEmail);

        if (existingUser == null)
        {
            existingUser = new User
            {
                Id = Guid.NewGuid(),
                Name = FinanceHodName,
                Email = FinanceHodEmail,
                PasswordHash = HashPassword(FinanceHodPassword),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(existingUser);
            _logger.LogInformation("Created Finance HOD user {Email}", FinanceHodEmail);
        }
        else
        {
            _logger.LogInformation("Finance HOD user already exists {Email}", FinanceHodEmail);
        }

        var hasFinanceRole = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == existingUser.Id && ur.RoleId == financeRole.Id);
        if (!hasFinanceRole)
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = existingUser.Id,
                CompanyId = companyId,
                RoleId = financeRole.Id,
                CreatedAt = DateTime.UtcNow
            });
            _logger.LogInformation("Assigned Finance role to {Email}", FinanceHodEmail);
        }

        var hasMembership = await _context.DepartmentMemberships
            .AnyAsync(m => m.UserId == existingUser.Id && m.DepartmentId == departmentId.Value);
        if (!hasMembership)
        {
            _context.DepartmentMemberships.Add(new DepartmentMembership
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                DepartmentId = departmentId.Value,
                UserId = existingUser.Id,
                Role = "HOD",
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            _logger.LogInformation("Linked {Email} to GPON department as HOD", FinanceHodEmail);
        }
    }

    /// <summary>
    /// Hash password using SHA256 (simple approach)
    /// For production, consider using BCrypt or ASP.NET Core Identity PasswordHasher
    /// </summary>
    public static string HashPassword(string password)
    {
        // For now, use SHA256 with salt
        // In production, use BCrypt or ASP.NET Core Identity's PasswordHasher
        using var sha256 = SHA256.Create();
        var salt = "CephasOps_Salt_2024"; // In production, use a random salt per user
        var saltedPassword = password + salt;
        var bytes = Encoding.UTF8.GetBytes(saltedPassword);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Verify password hash
    /// </summary>
    public static bool VerifyPassword(string password, string hash)
    {
        var computedHash = HashPassword(password);
        return computedHash == hash;
    }

    /// <summary>
    /// Seed or find GPON department
    /// </summary>
    private async Task<Department?> SeedGponDepartmentAsync(Guid? companyId)
    {
        var existingDepartment = await _context.Departments
            .FirstOrDefaultAsync(d => d.CompanyId == companyId && 
                                     (d.Code == "GPON" || d.Name.Contains("GPON")));

        if (existingDepartment != null)
        {
            _logger.LogInformation("GPON department already exists: {DepartmentName}", existingDepartment.Name);
            return existingDepartment;
        }

        // Try to find any department with GPON in name
        var gponDept = await _context.Departments
            .FirstOrDefaultAsync(d => d.CompanyId == companyId && d.Name.ToUpper().Contains("GPON"));

        if (gponDept != null)
        {
            _logger.LogInformation("Found GPON-related department: {DepartmentName}", gponDept.Name);
            return gponDept;
        }

        // If no GPON department exists, create it
        var department = new Department
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId, // Can be null now
            Name = "GPON",
            Code = "GPON",
            Description = "GPON Operations Department",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Departments.Add(department);
        _logger.LogInformation("Created GPON department");

        return department;
    }

    /// <summary>
    /// Seed default Order Types (parent/subtype hierarchy).
    /// Idempotent: upsert by Code. Parents first, then children with ParentOrderTypeId.
    /// </summary>
    private async Task SeedDefaultOrderTypesAsync(Guid? companyId, Guid? departmentId)
    {
        var parents = new[]
        {
            new { Code = "ACTIVATION", Name = "Activation", DisplayOrder = 1, Description = "New installation + activation of service" },
            new { Code = "MODIFICATION", Name = "Modification", DisplayOrder = 2, Description = "Modification of existing service" },
            new { Code = "ASSURANCE", Name = "Assurance", DisplayOrder = 3, Description = "Fault repair and troubleshooting" },
            new { Code = "VALUE_ADDED_SERVICE", Name = "Value Added Service", DisplayOrder = 4, Description = "Additional services beyond standard installation/repair" }
        };

        var parentIdsByCode = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in parents)
        {
            var existing = await _context.OrderTypes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(ot => ot.CompanyId == companyId && ot.Code == p.Code && ot.ParentOrderTypeId == null);

            if (existing == null)
            {
                var orderType = new OrderType
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    DepartmentId = departmentId,
                    ParentOrderTypeId = null,
                    Name = p.Name,
                    Code = p.Code,
                    Description = p.Description,
                    DisplayOrder = p.DisplayOrder,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.OrderTypes.Add(orderType);
                await _context.SaveChangesAsync();
                parentIdsByCode[p.Code] = orderType.Id;
                _logger.LogInformation("Created Order Type (parent): {Name}", p.Name);
            }
            else
            {
                parentIdsByCode[p.Code] = existing.Id;
            }
        }

        var children = new[]
        {
            new { ParentCode = "MODIFICATION", Code = "INDOOR", Name = "Indoor", DisplayOrder = 1, Description = "Indoor modification" },
            new { ParentCode = "MODIFICATION", Code = "OUTDOOR", Name = "Outdoor", DisplayOrder = 2, Description = "Outdoor modification" },
            new { ParentCode = "ASSURANCE", Code = "STANDARD", Name = "Standard", DisplayOrder = 1, Description = "Standard assurance" },
            new { ParentCode = "ASSURANCE", Code = "REPULL", Name = "Repull", DisplayOrder = 2, Description = "Repull assurance" },
            new { ParentCode = "VALUE_ADDED_SERVICE", Code = "UPGRADE", Name = "Upgrade", DisplayOrder = 1, Description = "Upgrade" },
            new { ParentCode = "VALUE_ADDED_SERVICE", Code = "IAD", Name = "IAD", DisplayOrder = 2, Description = "IAD" },
            new { ParentCode = "VALUE_ADDED_SERVICE", Code = "FIXED_IP", Name = "Fixed IP", DisplayOrder = 3, Description = "Fixed IP" }
        };

        foreach (var c in children)
        {
            if (!parentIdsByCode.TryGetValue(c.ParentCode, out var parentId))
                continue;

            // Skip if this subtype already exists, or a legacy equivalent (e.g. MODIFICATION_INDOOR vs INDOOR)
            var legacyCodes = GetLegacyOrderTypeCodesForSubtype(c.ParentCode, c.Code);
            var exists = await _context.OrderTypes
                .IgnoreQueryFilters()
                .AnyAsync(ot => ot.CompanyId == companyId && ot.ParentOrderTypeId == parentId
                    && (ot.Code == c.Code || legacyCodes.Contains(ot.Code)));

            if (!exists)
            {
                var orderType = new OrderType
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    DepartmentId = departmentId,
                    ParentOrderTypeId = parentId,
                    Name = c.Name,
                    Code = c.Code,
                    Description = c.Description,
                    DisplayOrder = c.DisplayOrder,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.OrderTypes.Add(orderType);
                _logger.LogInformation("Created Order Type (subtype): {ParentCode} -> {Name}", c.ParentCode, c.Name);
            }
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Legacy flat codes that represent the same subtype (e.g. MODIFICATION_INDOOR = INDOOR under MODIFICATION).
    /// Used to avoid creating duplicate subtype rows when migration has already linked legacy rows.
    /// </summary>
    private static IReadOnlySet<string> GetLegacyOrderTypeCodesForSubtype(string parentCode, string subtypeCode)
    {
        if (parentCode == "MODIFICATION" && subtypeCode == "INDOOR") return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "MODIFICATION_INDOOR" };
        if (parentCode == "MODIFICATION" && subtypeCode == "OUTDOOR") return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "MODIFICATION_OUTDOOR" };
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Seed default Installation Types
    /// Uses IgnoreQueryFilters() to check for soft-deleted records and prevent re-creation
    /// </summary>
    private async Task SeedDefaultOrderCategoriesAsync(Guid? companyId, Guid? departmentId)
    {
        var orderCategories = new[]
        {
            new { Name = "TIME-FTTH", Code = "TIME-FTTH", DisplayOrder = 1, Description = "Fibre to the Home" },
            new { Name = "TIME-FTTR", Code = "TIME-FTTR", DisplayOrder = 2, Description = "Fibre to the Room" },
            new { Name = "TIME-FTTC", Code = "TIME-FTTC", DisplayOrder = 3, Description = "Fibre to the Charge" }
        };

        foreach (var orderCategoryData in orderCategories)
        {
            // Use IgnoreQueryFilters to also check for soft-deleted records
            var exists = await _context.OrderCategories
                .IgnoreQueryFilters()
                .AnyAsync(oc => oc.CompanyId == companyId && oc.Code == orderCategoryData.Code);

            if (!exists)
            {
                var orderCategory = new OrderCategory
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    DepartmentId = departmentId,
                    Name = orderCategoryData.Name,
                    Code = orderCategoryData.Code,
                    Description = orderCategoryData.Description,
                    DisplayOrder = orderCategoryData.DisplayOrder,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.OrderCategories.Add(orderCategory);
                _logger.LogInformation("Created Order Category: {Name}", orderCategoryData.Name);
            }
        }
    }

    /// <summary>
    /// Seed default Building Types (actual building classifications, not installation methods)
    /// Uses IgnoreQueryFilters() to check for soft-deleted records and prevent re-creation
    /// </summary>
    private async Task SeedDefaultBuildingTypesAsync(Guid? companyId, Guid? departmentId)
    {
        var buildingTypes = new[]
        {
            // Residential Types
            new { Name = "Condominium", Code = "CONDO", DisplayOrder = 1, Description = "High-rise residential building" },
            new { Name = "Apartment", Code = "APARTMENT", DisplayOrder = 2, Description = "Multi-unit residential building" },
            new { Name = "Service Apartment", Code = "SERVICE_APT", DisplayOrder = 3, Description = "Serviced residential units" },
            new { Name = "Flat", Code = "FLAT", DisplayOrder = 4, Description = "Low-rise residential units" },
            new { Name = "Terrace House", Code = "TERRACE", DisplayOrder = 5, Description = "Row houses" },
            new { Name = "Semi-Detached", Code = "SEMI_DETACHED", DisplayOrder = 6, Description = "Semi-detached houses" },
            new { Name = "Bungalow", Code = "BUNGALOW", DisplayOrder = 7, Description = "Single-story detached house" },
            new { Name = "Townhouse", Code = "TOWNHOUSE", DisplayOrder = 8, Description = "Multi-story attached houses" },
            
            // Commercial Types
            new { Name = "Office Tower", Code = "OFFICE_TOWER", DisplayOrder = 10, Description = "High-rise office building" },
            new { Name = "Office Building", Code = "OFFICE", DisplayOrder = 11, Description = "Low to mid-rise office building" },
            new { Name = "Shop Office", Code = "SHOP_OFFICE", DisplayOrder = 12, Description = "Mixed shop and office building" },
            new { Name = "Shopping Mall", Code = "MALL", DisplayOrder = 13, Description = "Retail shopping complex" },
            new { Name = "Hotel", Code = "HOTEL", DisplayOrder = 14, Description = "Hotel or resort building" },
            
            // Mixed Use
            new { Name = "Mixed Development", Code = "MIXED", DisplayOrder = 20, Description = "Mixed residential and commercial" },
            
            // Others
            new { Name = "Industrial", Code = "INDUSTRIAL", DisplayOrder = 30, Description = "Industrial or warehouse building" },
            new { Name = "Warehouse", Code = "WAREHOUSE", DisplayOrder = 31, Description = "Storage or warehouse facility" },
            new { Name = "Educational", Code = "EDUCATIONAL", DisplayOrder = 32, Description = "School or educational institution" },
            new { Name = "Government", Code = "GOVERNMENT", DisplayOrder = 33, Description = "Government building" },
            new { Name = "Other", Code = "OTHER", DisplayOrder = 99, Description = "Other building type" }
        };

        foreach (var buildingTypeData in buildingTypes)
        {
            // Use IgnoreQueryFilters to also check for soft-deleted records
            var exists = await _context.BuildingTypes
                .IgnoreQueryFilters()
                .AnyAsync(bt => bt.CompanyId == companyId && bt.Code == buildingTypeData.Code);

            if (!exists)
            {
                var buildingType = new BuildingType
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    DepartmentId = departmentId,
                    Name = buildingTypeData.Name,
                    Code = buildingTypeData.Code,
                    Description = buildingTypeData.Description,
                    DisplayOrder = buildingTypeData.DisplayOrder,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.BuildingTypes.Add(buildingType);
                _logger.LogInformation("Created Building Type: {Name}", buildingTypeData.Name);
            }
        }
    }

    /// <summary>
    /// Seed default Splitter Types
    /// Uses IgnoreQueryFilters() to check for soft-deleted records and prevent re-creation
    /// </summary>
    private async Task SeedDefaultSplitterTypesAsync(Guid? companyId, Guid? departmentId)
    {
        var splitterTypes = new (string Name, string Code, int TotalPorts, int? StandbyPortNumber, int DisplayOrder, string Description)[]
        {
            ("1:8", "1_8", 8, null, 1, "1:8 Splitter (8 ports)"),
            ("1:12", "1_12", 12, null, 2, "1:12 Splitter (12 ports)"),
            ("1:32", "1_32", 32, 32, 3, "1:32 Splitter (32 ports, port 32 is standby)")
        };

        foreach (var splitterTypeData in splitterTypes)
        {
            // Use IgnoreQueryFilters to also check for soft-deleted records
            // Check by Code only (since we're in single-company mode, Code should be unique)
            // This prevents re-creating records that were intentionally deleted
            var exists = await _context.SplitterTypes
                .IgnoreQueryFilters()
                .Where(st => st.Code == splitterTypeData.Code && st.IsDeleted == false)
                .AnyAsync();

            if (!exists)
            {
                var splitterType = new SplitterType
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    DepartmentId = departmentId,
                    Name = splitterTypeData.Name,
                    Code = splitterTypeData.Code,
                    TotalPorts = splitterTypeData.TotalPorts,
                    StandbyPortNumber = splitterTypeData.StandbyPortNumber,
                    Description = splitterTypeData.Description,
                    DisplayOrder = splitterTypeData.DisplayOrder,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.SplitterTypes.Add(splitterType);
                _logger.LogInformation("Created Splitter Type: {Name}", splitterTypeData.Name);
            }
            else
            {
                _logger.LogInformation("Splitter Type already exists, skipping: {Code}", splitterTypeData.Code);
            }
        }
    }

    /// <summary>
    /// Seed GPON Service Installers
    /// Only seeds if the table is empty (first-time setup)
    /// After initial setup, service installers should be managed through the UI/API
    /// </summary>
    private async Task SeedGponServiceInstallersAsync(Guid? companyId, Guid? departmentId)
    {
        // Seed data removed - installers will be imported separately
        _logger.LogInformation("Service installer seeding disabled - installers will be imported separately");
        return;
    }

    // Company feature removed - Verticals table removed
    // private async Task SeedDefaultVerticalsAsync(Guid companyId) { ... }

    /// <summary>
    /// Seed default skills for service installers
    /// Seeds all 33 required skills organized by category
    /// </summary>
    private async Task SeedDefaultSkillsAsync(Guid? companyId)
    {
        var skills = new (string Name, string Code, string Category, string? Description, int DisplayOrder)[]
        {
            // Fiber Skills (9)
            ("Fiber cable installation (indoor)", "FIBER_CABLE_INDOOR", "FiberSkills", "Installation of fiber cables in indoor environments", 1),
            ("Fiber cable installation (outdoor/aerial)", "FIBER_CABLE_OUTDOOR", "FiberSkills", "Installation of fiber cables in outdoor/aerial environments", 2),
            ("Fiber splicing (mechanical)", "FIBER_SPLICE_MECHANICAL", "FiberSkills", "Mechanical fiber splicing techniques", 3),
            ("Fiber splicing (fusion)", "FIBER_SPLICE_FUSION", "FiberSkills", "Fusion fiber splicing techniques", 4),
            ("Fiber connector termination (SC/LC)", "FIBER_CONNECTOR_TERMINATION", "FiberSkills", "Termination of SC/LC fiber connectors", 5),
            ("OTDR testing", "OTDR_TESTING", "FiberSkills", "Optical Time Domain Reflectometer testing", 6),
            ("Optical power meter usage", "OPTICAL_POWER_METER", "FiberSkills", "Using optical power meters for signal measurement", 7),
            ("Visual fault locator (VFL)", "VFL_USAGE", "FiberSkills", "Using Visual Fault Locator for fiber troubleshooting", 8),
            ("Drop cable installation", "DROP_CABLE_INSTALL", "FiberSkills", "Installation of drop cables from distribution point to customer premises", 9),
            
            // Network & Equipment (7)
            ("ONT installation and configuration", "ONT_INSTALL_CONFIG", "NetworkEquipment", "Installation and configuration of Optical Network Terminals", 10),
            ("Router setup and configuration", "ROUTER_SETUP", "NetworkEquipment", "Setting up and configuring routers", 11),
            ("Wi-Fi optimization", "WIFI_OPTIMIZATION", "NetworkEquipment", "Optimizing Wi-Fi networks for performance", 12),
            ("IPTV setup", "IPTV_SETUP", "NetworkEquipment", "Setting up IPTV services", 13),
            ("Mesh network installation", "MESH_NETWORK", "NetworkEquipment", "Installation of mesh Wi-Fi networks", 14),
            ("Basic network troubleshooting", "NETWORK_TROUBLESHOOTING", "NetworkEquipment", "Basic troubleshooting of network issues", 15),
            ("Speed test and verification", "SPEED_TEST", "NetworkEquipment", "Performing speed tests and verifying service quality", 16),
            
            // Installation Methods (6)
            ("Aerial installation (pole-to-building)", "AERIAL_INSTALL", "InstallationMethods", "Aerial fiber installation from pole to building", 17),
            ("Underground/conduit installation", "UNDERGROUND_INSTALL", "InstallationMethods", "Underground and conduit-based fiber installation", 18),
            ("Indoor cable routing", "INDOOR_ROUTING", "InstallationMethods", "Routing fiber cables within buildings", 19),
            ("Wall penetration and patching", "WALL_PENETRATION", "InstallationMethods", "Penetrating walls and patching holes for cable routing", 20),
            ("Cable management and labeling", "CABLE_MANAGEMENT", "InstallationMethods", "Proper cable management and labeling practices", 21),
            ("Weatherproofing", "WEATHERPROOFING", "InstallationMethods", "Weatherproofing outdoor installations", 22),
            
            // Safety & Compliance (6)
            ("Working at heights certified", "HEIGHTS_CERTIFIED", "SafetyCompliance", "Certification for working at heights", 23),
            ("Electrical safety awareness", "ELECTRICAL_SAFETY", "SafetyCompliance", "Awareness of electrical safety procedures", 24),
            ("TNB clearance procedures", "TNB_CLEARANCE", "SafetyCompliance", "Understanding TNB (Tenaga Nasional Berhad) clearance procedures", 25),
            ("Confined space entry", "CONFINED_SPACE", "SafetyCompliance", "Certification for confined space entry", 26),
            ("PPE usage", "PPE_USAGE", "SafetyCompliance", "Proper use of Personal Protective Equipment", 27),
            ("First Aid certified", "FIRST_AID", "SafetyCompliance", "First Aid certification", 28),
            
            // Customer Service (5)
            ("Customer communication", "CUSTOMER_COMMUNICATION", "CustomerService", "Effective communication with customers", 29),
            ("Service demonstration", "SERVICE_DEMO", "CustomerService", "Demonstrating services to customers", 30),
            ("Technical explanation to customers", "TECH_EXPLANATION", "CustomerService", "Explaining technical concepts to non-technical customers", 31),
            ("Professional conduct", "PROFESSIONAL_CONDUCT", "CustomerService", "Maintaining professional conduct during installations", 32),
            ("Site cleanliness", "SITE_CLEANLINESS", "CustomerService", "Maintaining cleanliness at installation sites", 33)
        };

        foreach (var skillData in skills)
        {
            // Check if skill already exists by Code (unique per company)
            var exists = await _context.Skills
                .IgnoreQueryFilters()
                .Where(s => s.Code == skillData.Code && s.CompanyId == companyId && s.IsDeleted == false)
                .AnyAsync();

            if (!exists)
            {
                var skill = new Skill
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    Name = skillData.Name,
                    Code = skillData.Code,
                    Category = skillData.Category,
                    Description = skillData.Description,
                    DisplayOrder = skillData.DisplayOrder,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Skills.Add(skill);
                _logger.LogInformation("Created Skill: {Name} ({Category})", skillData.Name, skillData.Category);
            }
            else
            {
                _logger.LogInformation("Skill already exists, skipping: {Code}", skillData.Code);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedDefaultMaterialsAsync(Guid? companyId)
    {
        _logger.LogInformation("Seeding default materials...");

        // Get GPON department for material assignment
        var gponDepartment = await _context.Departments
            .FirstOrDefaultAsync(d => d.Name == "GPON");

        var defaultMaterials = new[]
        {
            // ONT / Router (Serialized)
            new { ItemCode = "CAE-000-0820", Description = "Huawei HG8145X6 - Dual-band WiFi 6 ONT", Category = "ONT / Router", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "CAE-000-0780", Description = "Huawei HG8145V5 - Dual-band ONT", Category = "ONT / Router", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "CAE-000-0830", Description = "Huawei HN8245X6s-8N-30 (2GB) - Enhanced ONT with 2GB", Category = "ONT / Router", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "CAE-000-0840", Description = "Huawei HG8245X6-8N-30 (1GB) - ONT (1GB)", Category = "ONT / Router", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "CAE-000-0851", Description = "Huawei K153 - Router/ONT", Category = "Router / ONT", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "CAE-000-0860", Description = "Huawei HG8145B7N - FTTH ONT", Category = "ONT", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            
            // Router / AP (Serialized)
            new { ItemCode = "CAE-000-0770", Description = "Huawei WA8021V5 - WiFi 5 Access Point", Category = "Router / AP", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            
            // Router (Serialized)
            new { ItemCode = "CAE-000-0760", Description = "TP-Link HC420 - Wireless Router", Category = "Router", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "CAE-000-0750", Description = "TP-Link EC440 - Dual-band Router", Category = "Router", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "CAE-000-0320", Description = "TP-Link Archer C1200 - Home WiFi Router", Category = "Router", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "CAE-000-0550", Description = "TP-Link EC230-G1 - Mesh Router", Category = "Router / Mesh", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "CAE-000-0290", Description = "D-Link 850L - Broadband Router", Category = "Router", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "CAE-000-0350", Description = "D-Link DIR-882 - High-performance Router", Category = "Router", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "CAE-000-0900", Description = "ZyXEL EX3300-T0 - WiFi Router", Category = "Router", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "CAE-000-0850", Description = "Huawei V163 - Customer Premise Router", Category = "Router", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "CAE-CEL-0010", Description = "Skyworth RN685 (Celcom) - Celcom HSBA Router", Category = "Router", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "CAE-000-1000", Description = "Skyworth RN685 (Digi) - Digi HSBA Router", Category = "Router", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "CAE-CEL-1001", Description = "TP-Link EX510 (Digi) - Digi HSBA Router", Category = "Router", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "CAE-CEL-0020", Description = "TP-Link EX510 (Celcom) - Celcom HSBA Router", Category = "Router", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "CAE-UME-0010", Description = "D-Link DIR-X1860Z (Umobile) - WiFi 6 Router", Category = "Router", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "CAE-CDI-1001", Description = "TP-Link EX510 (CelcomDigi) - CelcomDigi HSBA Router", Category = "Router", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "CAE-CDI-1002", Description = "TP-Link EX820 (CelcomDigi) - WiFi 6 Router", Category = "Router", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            
            // ONU (Serialized)
            new { ItemCode = "PON-AHW-0350", Description = "Huawei HG8240H5 - Optical Network Unit", Category = "ONU", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "PON-AHW-0353", Description = "Huawei HG8140H5 - Optical Network Unit", Category = "ONU", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            
            // Phone (Serialized)
            new { ItemCode = "CAE-000-0210", Description = "Motorola C1001LA - Cordless Phone (Black)", Category = "Phone", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            
            // IAD (Serialized)
            new { ItemCode = "ACS-IAD-0050", Description = "Yeastar IAD 4 ports - 4-Port Integrated Access Device", Category = "IAD", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "ACS-IAD-0070", Description = "D-Link IAD 4 Ports - 4-Port IAD", Category = "IAD", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "IAD DLINK", Description = "DVG-5004S - VoIP IAD", Category = "IAD", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "ACS-IAD-0020", Description = "IAD 8 Ports - 8-Port Integrated Access Device", Category = "IAD", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "ACS-IAD-0080", Description = "Synway IAD 4 Ports - Synway IAD (4 Ports)", Category = "IAD", UnitOfMeasure = "Unit", IsSerialised = true, DefaultCost = 0m, IsActive = true },
            
            // Connector (Non-Serialized)
            new { ItemCode = "OFA-000-1000", Description = "SC/UPC Fast Connector - FAST Connector – Litech (Blue)", Category = "Connector", UnitOfMeasure = "Unit", IsSerialised = false, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "OFA-000-1010", Description = "SC/APC Fast Connector - FAST Connector – Litech (Green APC)", Category = "Connector", UnitOfMeasure = "Unit", IsSerialised = false, DefaultCost = 0m, IsActive = true },
            
            // Patchcord (Non-Serialized)
            new { ItemCode = "OFA-PTC-0540", Description = "SC-SC SM Simplex 3m - Fiber Patchcord (3m)", Category = "Patchcord", UnitOfMeasure = "Piece", IsSerialised = false, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "OFA-PTC-0070", Description = "SC/APC-SC SM Simplex 6m - Fiber Patchcord (6m)", Category = "Patchcord", UnitOfMeasure = "Piece", IsSerialised = false, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "OFA-PTC-0830", Description = "SC/UPC-SC/UPC 10m - Fiber Patchcord (10m)", Category = "Patchcord", UnitOfMeasure = "Piece", IsSerialised = false, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "OFA-PTC-0840", Description = "SC/UPC-SC/UPC 15m - Fiber Patchcord (15m)", Category = "Patchcord", UnitOfMeasure = "Piece", IsSerialised = false, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "OFA-PTC-0820", Description = "SC/UPC Patchcord 6m - Fiber Patchcord (6m)", Category = "Patchcord", UnitOfMeasure = "Piece", IsSerialised = false, DefaultCost = 0m, IsActive = true },
            
            // Drop Cable (Non-Serialized)
            new { ItemCode = "OFC-002-SMDC", Description = "Drop Cable SM 2 Core - Fiber Drop Cable", Category = "Drop Cable", UnitOfMeasure = "Meter", IsSerialised = false, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "OFC-DRC-0080", Description = "SC/APC Drop Cable 80m - Outdoor Drop Cable 80m", Category = "Drop Cable", UnitOfMeasure = "Unit", IsSerialised = false, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "OFC-DRC-0100", Description = "SC/APC Drop Cable 100m - Outdoor Drop Cable 100m", Category = "Drop Cable", UnitOfMeasure = "Unit", IsSerialised = false, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "OFC-DRC-P080", Description = "Drop Cable 80m (RDF Pole) - RDF Pole Outdoor Drop Cable", Category = "Drop Cable", UnitOfMeasure = "Unit", IsSerialised = false, DefaultCost = 0m, IsActive = true },
            
            // Accessories (Non-Serialized)
            new { ItemCode = "FTB-001-001", Description = "Fiber Termination Box - Outdoor FTB (2 Core)", Category = "Accessories", UnitOfMeasure = "Unit", IsSerialised = false, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "CAE-000-0852", Description = "Huawei ATB - Access Termination Box", Category = "Accessories", UnitOfMeasure = "Unit", IsSerialised = false, DefaultCost = 0m, IsActive = true },
            
            // Fiber Cable (Non-Serialized)
            new { ItemCode = "CAE-000-0853", Description = "Huawei Transparent Fibre 50m - Clear Indoor Fiber Cable", Category = "Fiber Cable", UnitOfMeasure = "Meter", IsSerialised = false, DefaultCost = 0m, IsActive = true },
            
            // Distribution (Non-Serialized)
            new { ItemCode = "CAE-000-0854", Description = "Huawei FDU - Fiber Distribution Unit", Category = "Distribution", UnitOfMeasure = "Unit", IsSerialised = false, DefaultCost = 0m, IsActive = true },
            new { ItemCode = "CAE-000-0855", Description = "Huawei FMC - Fiber Management Cabinet", Category = "Distribution", UnitOfMeasure = "Unit", IsSerialised = false, DefaultCost = 0m, IsActive = true }
        };

        foreach (var materialData in defaultMaterials)
        {
            // Use IgnoreQueryFilters to also check for soft-deleted records
            var existing = await _context.Materials
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.ItemCode == materialData.ItemCode && (m.CompanyId == companyId || (companyId == null && m.CompanyId == Guid.Empty)));

            if (existing == null)
            {
                var material = new Material
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId ?? Guid.Empty,
                    ItemCode = materialData.ItemCode,
                    Description = materialData.Description,
                    Category = materialData.Category,
                    UnitOfMeasure = materialData.UnitOfMeasure,
                    IsSerialised = materialData.IsSerialised,
                    DefaultCost = materialData.DefaultCost,
                    DepartmentId = gponDepartment?.Id, // Assign to GPON department
                    IsActive = materialData.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Materials.Add(material);
                _logger.LogInformation("Created material: {ItemCode}", materialData.ItemCode);
            }
        }
    }

    /// <summary>
    /// Seed material categories from existing materials
    /// Extracts unique categories from Materials table and creates MaterialCategory entries
    /// </summary>
    private async Task SeedMaterialCategoriesFromMaterialsAsync(Guid? companyId)
    {
        _logger.LogInformation("Seeding material categories from existing materials...");

        // Get all distinct categories from materials (excluding null/empty)
        var distinctCategories = await _context.Materials
            .Where(m => !string.IsNullOrEmpty(m.Category) && (companyId == null || m.CompanyId == companyId))
            .Select(m => m.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        if (!distinctCategories.Any())
        {
            _logger.LogInformation("No categories found in materials to migrate");
            return;
        }

        _logger.LogInformation("Found {Count} unique categories in materials", distinctCategories.Count);

        int displayOrder = 0;
        foreach (var categoryName in distinctCategories)
        {
            // Use IgnoreQueryFilters to also check for soft-deleted records
            var existingCategory = await _context.Set<MaterialCategory>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Name == categoryName && (companyId == null || c.CompanyId == companyId));

            if (existingCategory == null)
            {
                var category = new MaterialCategory
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId ?? Guid.Empty,
                    Name = categoryName,
                    Description = null,
                    DisplayOrder = displayOrder++,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Set<MaterialCategory>().Add(category);
                _logger.LogInformation("Created material category: {CategoryName}", categoryName);
            }
            else
            {
                _logger.LogInformation("Material category already exists, skipping: {CategoryName}", categoryName);
            }
        }

        _logger.LogInformation("Seeded {Count} material categories from materials", distinctCategories.Count);
    }

    /// <summary>
    /// Seed default material categories if none exist
    /// Creates common categories used in GPON/ISP operations
    /// </summary>
    private async Task SeedDefaultMaterialCategoriesAsync(Guid? companyId)
    {
        _logger.LogInformation("Seeding default material categories...");

        // Check if any categories exist
        var existingCount = await _context.Set<MaterialCategory>()
            .Where(c => companyId == null || c.CompanyId == companyId)
            .CountAsync();

        if (existingCount > 0)
        {
            _logger.LogInformation("Material categories already exist ({Count}), skipping default seed", existingCount);
            return;
        }

        // Default categories for GPON/ISP operations
        var defaultCategories = new[]
        {
            new { Name = "ONU", Description = "Optical Network Units - Customer premises equipment", DisplayOrder = 1 },
            new { Name = "Fiber Cable", Description = "Fiber optic cables (indoor, outdoor, aerial)", DisplayOrder = 2 },
            new { Name = "Splitter", Description = "Optical splitters for fiber distribution", DisplayOrder = 3 },
            new { Name = "Accessories", Description = "Termination boxes, connectors, adapters", DisplayOrder = 4 },
            new { Name = "Distribution", Description = "Distribution units, cabinets, enclosures", DisplayOrder = 5 },
            new { Name = "Tools", Description = "Installation tools and equipment", DisplayOrder = 6 },
            new { Name = "Consumables", Description = "Consumable items (cable ties, labels, etc.)", DisplayOrder = 7 },
            new { Name = "Spare Parts", Description = "Spare parts and replacement components", DisplayOrder = 8 }
        };

        foreach (var catData in defaultCategories)
        {
            var existingCategory = await _context.Set<MaterialCategory>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Name == catData.Name && (companyId == null || c.CompanyId == companyId));

            if (existingCategory == null)
            {
                var category = new MaterialCategory
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId ?? Guid.Empty,
                    Name = catData.Name,
                    Description = catData.Description,
                    DisplayOrder = catData.DisplayOrder,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Set<MaterialCategory>().Add(category);
                _logger.LogInformation("Created default material category: {CategoryName}", catData.Name);
            }
        }

        _logger.LogInformation("Seeded {Count} default material categories", defaultCategories.Length);
    }

    /// <summary>
    /// Seed default parser templates for TIME orders
    /// </summary>
    private async Task SeedDefaultParserTemplatesAsync()
    {
        _logger.LogInformation("Seeding default parser templates...");

        var templates = new[]
        {
            new ParserTemplate
            {
                Id = Guid.NewGuid(),
                Name = "TIME Activation",
                Code = "TIME_ACTIVATION",
                PartnerPattern = "*@time.com.my",
                SubjectPattern = "*Activation*",
                OrderTypeCode = "ACTIVATION",
                Priority = 100,
                IsActive = true,
                AutoApprove = false,
                Description = "Parses TIME FTTH/HSBB activation work orders",
                CreatedAt = DateTime.UtcNow
            },
            new ParserTemplate
            {
                Id = Guid.NewGuid(),
                Name = "TIME Modification (Indoor)",
                Code = "TIME_MOD_INDOOR",
                PartnerPattern = "*@time.com.my",
                SubjectPattern = "*Modification*Indoor*",
                OrderTypeCode = "MODIFICATION_INDOOR",
                Priority = 95,
                IsActive = true,
                AutoApprove = false,
                Description = "Parses TIME indoor modification work orders",
                CreatedAt = DateTime.UtcNow
            },
            new ParserTemplate
            {
                Id = Guid.NewGuid(),
                Name = "TIME Modification (Outdoor)",
                Code = "TIME_MOD_OUTDOOR",
                PartnerPattern = "*@time.com.my",
                SubjectPattern = "*Modification*Outdoor*",
                OrderTypeCode = "MODIFICATION_OUTDOOR",
                Priority = 95,
                IsActive = true,
                AutoApprove = false,
                Description = "Parses TIME outdoor modification work orders",
                CreatedAt = DateTime.UtcNow
            },
            new ParserTemplate
            {
                Id = Guid.NewGuid(),
                Name = "TIME Modification (General)",
                Code = "TIME_MODIFICATION",
                PartnerPattern = "*@time.com.my",
                SubjectPattern = "*Modification*",
                OrderTypeCode = "MODIFICATION",
                Priority = 90,
                IsActive = true,
                AutoApprove = false,
                Description = "Parses TIME general modification work orders",
                CreatedAt = DateTime.UtcNow
            },
            new ParserTemplate
            {
                Id = Guid.NewGuid(),
                Name = "TIME Termination",
                Code = "TIME_TERMINATION",
                PartnerPattern = "*@time.com.my",
                SubjectPattern = "*Termination*",
                OrderTypeCode = "TERMINATION",
                Priority = 80,
                IsActive = true,
                AutoApprove = false,
                Description = "Parses TIME termination/cancellation work orders",
                CreatedAt = DateTime.UtcNow
            },
            new ParserTemplate
            {
                Id = Guid.NewGuid(),
                Name = "TIME Relocation",
                Code = "TIME_RELOCATION",
                PartnerPattern = "*@time.com.my",
                SubjectPattern = "*Relocation*",
                OrderTypeCode = "RELOCATION",
                Priority = 85,
                IsActive = true,
                AutoApprove = false,
                Description = "Parses TIME relocation work orders",
                CreatedAt = DateTime.UtcNow
            },
            new ParserTemplate
            {
                Id = Guid.NewGuid(),
                Name = "TIME Assurance",
                Code = "TIME_ASSURANCE",
                PartnerPattern = "*@time.com.my",
                SubjectPattern = "*Assurance*",
                OrderTypeCode = "ASSURANCE",
                Priority = 70,
                IsActive = true,
                AutoApprove = false,
                Description = "Parses TIME assurance/troubleshooting work orders",
                CreatedAt = DateTime.UtcNow
            },
            new ParserTemplate
            {
                Id = Guid.NewGuid(),
                Name = "TIME General (Fallback)",
                Code = "TIME_GENERAL",
                PartnerPattern = "*@time.com.my",
                SubjectPattern = "*Work Order*",
                OrderTypeCode = "GENERAL",
                Priority = 10,
                IsActive = true,
                AutoApprove = false,
                Description = "Fallback template for TIME work orders that don't match other patterns",
                CreatedAt = DateTime.UtcNow
            },
            new ParserTemplate
            {
                Id = Guid.NewGuid(),
                Name = "Celcom HSBB",
                Code = "CELCOM_HSBB",
                PartnerPattern = "*celcom*",
                SubjectPattern = "*HSBB*",
                OrderTypeCode = "ACTIVATION",
                Priority = 100,
                IsActive = true,
                AutoApprove = false,
                Description = "Parses Celcom HSBB work orders via TIME",
                CreatedAt = DateTime.UtcNow
            },
            new ParserTemplate
            {
                Id = Guid.NewGuid(),
                Name = "TIME Payment Advice",
                Code = "TIME_PAYMENT_ADVICE",
                PartnerPattern = "*@time.com.my",
                SubjectPattern = "*Payment Advice*|*Payment*",
                OrderTypeCode = null,
                Priority = 11,
                IsActive = true,
                AutoApprove = false,
                Description = "Parses payment advice emails from TIME",
                CreatedAt = DateTime.UtcNow
            },
            new ParserTemplate
            {
                Id = Guid.NewGuid(),
                Name = "TIME Reschedule Notification",
                Code = "TIME_RESCHEDULE",
                PartnerPattern = "*@time.com.my",
                SubjectPattern = "*Reschedule*|*Rescheduled*",
                OrderTypeCode = null,
                Priority = 12,
                IsActive = true,
                AutoApprove = false,
                Description = "Parses reschedule notification emails from TIME",
                CreatedAt = DateTime.UtcNow
            },
            new ParserTemplate
            {
                Id = Guid.NewGuid(),
                Name = "TIME Customer Uncontactable",
                Code = "TIME_CUSTOMER_UNCONTACTABLE",
                PartnerPattern = "*@time.com.my",
                SubjectPattern = "*Customer Uncontactable*|*Uncontactable*",
                OrderTypeCode = null,
                Priority = 13,
                IsActive = true,
                AutoApprove = false,
                Description = "Parses customer uncontactable notification emails from TIME",
                CreatedAt = DateTime.UtcNow
            },
            new ParserTemplate
            {
                Id = Guid.NewGuid(),
                Name = "TIME RFB Meeting Notification",
                Code = "TIME_RFB",
                PartnerPattern = "*@time.com.my",
                SubjectPattern = "*RFB MEETING*|*RFB Meeting*|*Request for Building*",
                OrderTypeCode = null,
                Priority = 14,
                IsActive = true,
                AutoApprove = false,
                Description = "Parses RFB meeting notification emails from TIME. Extracts building information, meeting details, and BM contact information.",
                CreatedAt = DateTime.UtcNow
            },
            new ParserTemplate
            {
                Id = Guid.NewGuid(),
                Name = "TIME Withdrawal Notification",
                Code = "TIME_WITHDRAWAL",
                PartnerPattern = "*@time.com.my",
                SubjectPattern = "*Withdraw*|*Withdrawn*|*Confirm Withdraw*",
                OrderTypeCode = null,
                Priority = 15,
                IsActive = true,
                AutoApprove = false,
                Description = "Parses withdrawal notification emails from TIME. Extracts Service ID and updates order status to Cancelled.",
                CreatedAt = DateTime.UtcNow
            }
        };

        foreach (var template in templates)
        {
            var exists = await _context.ParserTemplates
                .AnyAsync(t => t.Code == template.Code);

            if (!exists)
            {
                _context.ParserTemplates.Add(template);
                _logger.LogInformation("Created parser template: {Name} ({Code})", template.Name, template.Code);
            }
            else
            {
                _logger.LogInformation("Parser template already exists, skipping: {Code}", template.Code);
            }
        }
    }

    /// <summary>
    /// Seed default guard condition definitions for Order entity
    /// These are stored in settings - fully configurable, no hardcoding
    /// </summary>
    private async Task SeedDefaultGuardConditionsAsync(Guid? companyId)
    {
        if (!companyId.HasValue)
        {
            var company = await _context.Companies.FirstOrDefaultAsync();
            if (company == null)
            {
                _logger.LogWarning("No company found, skipping guard condition seeding");
                return;
            }
            companyId = company.Id;
        }

        var guardConditions = new[]
        {
            new GuardConditionDefinition
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId.Value,
                Key = "photosRequired",
                Name = "Photos Required",
                Description = "Checks if photos are uploaded for the order",
                EntityType = "Order",
                ValidatorType = "PhotosRequiredValidator",
                ValidatorConfigJson = JsonSerializer.Serialize(new { checkFlag = true, checkFiles = true }),
                IsActive = true,
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new GuardConditionDefinition
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId.Value,
                Key = "docketUploaded",
                Name = "Docket Uploaded",
                Description = "Checks if docket is uploaded for the order",
                EntityType = "Order",
                ValidatorType = "DocketUploadedValidator",
                ValidatorConfigJson = JsonSerializer.Serialize(new { checkFlag = true, checkDockets = true }),
                IsActive = true,
                DisplayOrder = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new GuardConditionDefinition
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId.Value,
                Key = "splitterAssigned",
                Name = "Splitter Assigned",
                Description = "Checks if splitter port is assigned to the order",
                EntityType = "Order",
                ValidatorType = "SplitterAssignedValidator",
                ValidatorConfigJson = null,
                IsActive = true,
                DisplayOrder = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new GuardConditionDefinition
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId.Value,
                Key = "serialNumbersValidated",
                Name = "Serial Numbers Validated",
                Description = "Checks if serial numbers are validated for the order",
                EntityType = "Order",
                ValidatorType = "SerialsValidatedValidator",
                ValidatorConfigJson = null,
                IsActive = true,
                DisplayOrder = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new GuardConditionDefinition
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId.Value,
                Key = "materialsSpecified",
                Name = "Materials Specified",
                Description = "Checks if materials are specified for the order",
                EntityType = "Order",
                ValidatorType = "MaterialsSpecifiedValidator",
                ValidatorConfigJson = null,
                IsActive = true,
                DisplayOrder = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new GuardConditionDefinition
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId.Value,
                Key = "siaAssigned",
                Name = "SI Assigned",
                Description = "Checks if Service Installer (SI) is assigned to the order",
                EntityType = "Order",
                ValidatorType = "SiAssignedValidator",
                ValidatorConfigJson = null,
                IsActive = true,
                DisplayOrder = 6,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new GuardConditionDefinition
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId.Value,
                Key = "appointmentDateSet",
                Name = "Appointment Date Set",
                Description = "Checks if appointment date is set for the order",
                EntityType = "Order",
                ValidatorType = "AppointmentDateSetValidator",
                ValidatorConfigJson = null,
                IsActive = true,
                DisplayOrder = 7,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new GuardConditionDefinition
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId.Value,
                Key = "buildingSelected",
                Name = "Building Selected",
                Description = "Checks if building is selected for the order",
                EntityType = "Order",
                ValidatorType = "BuildingSelectedValidator",
                ValidatorConfigJson = null,
                IsActive = true,
                DisplayOrder = 8,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new GuardConditionDefinition
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId.Value,
                Key = "customerContactProvided",
                Name = "Customer Contact Provided",
                Description = "Checks if customer contact (phone or email) is provided for the order",
                EntityType = "Order",
                ValidatorType = "CustomerContactProvidedValidator",
                ValidatorConfigJson = null,
                IsActive = true,
                DisplayOrder = 9,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new GuardConditionDefinition
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId.Value,
                Key = "noBlockersActive",
                Name = "No Active Blockers",
                Description = "Checks if there are no active blockers for the order",
                EntityType = "Order",
                ValidatorType = "NoActiveBlockersValidator",
                ValidatorConfigJson = null,
                IsActive = true,
                DisplayOrder = 10,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        foreach (var guardCondition in guardConditions)
        {
            var exists = await _context.Set<GuardConditionDefinition>()
                .AnyAsync(gcd => gcd.CompanyId == companyId.Value 
                    && gcd.Key == guardCondition.Key 
                    && gcd.EntityType == guardCondition.EntityType
                    && !gcd.IsDeleted);

            if (!exists)
            {
                _context.Set<GuardConditionDefinition>().Add(guardCondition);
                _logger.LogInformation("Created guard condition definition: {Key} ({Name})", guardCondition.Key, guardCondition.Name);
            }
            else
            {
                _logger.LogInformation("Guard condition definition already exists, skipping: {Key}", guardCondition.Key);
            }
        }
    }

    /// <summary>
    /// Seed default side effect definitions for Order entity
    /// These are stored in settings - fully configurable, no hardcoding
    /// </summary>
    private async Task SeedDefaultSideEffectsAsync(Guid? companyId)
    {
        if (!companyId.HasValue)
        {
            var company = await _context.Companies.FirstOrDefaultAsync();
            if (company == null)
            {
                _logger.LogWarning("No company found, skipping side effect seeding");
                return;
            }
            companyId = company.Id;
        }

        var sideEffects = new[]
        {
            new SideEffectDefinition
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId.Value,
                Key = "notify",
                Name = "Send Notification",
                Description = "Sends a notification to relevant users when workflow transition occurs",
                EntityType = "Order",
                ExecutorType = "NotifySideEffectExecutor",
                ExecutorConfigJson = JsonSerializer.Serialize(new { template = "OrderStatusChange" }),
                IsActive = true,
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SideEffectDefinition
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId.Value,
                Key = "createStockMovement",
                Name = "Create Stock Movement",
                Description = "Creates stock movement records when workflow transition occurs",
                EntityType = "Order",
                ExecutorType = "CreateStockMovementSideEffectExecutor",
                ExecutorConfigJson = null,
                IsActive = true,
                DisplayOrder = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SideEffectDefinition
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId.Value,
                Key = "createOrderStatusLog",
                Name = "Create Order Status Log",
                Description = "Creates an order status log entry when workflow transition occurs",
                EntityType = "Order",
                ExecutorType = "CreateOrderStatusLogSideEffectExecutor",
                ExecutorConfigJson = null,
                IsActive = true,
                DisplayOrder = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SideEffectDefinition
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId.Value,
                Key = "updateOrderFlags",
                Name = "Update Order Flags",
                Description = "Updates order flags (DocketUploaded, PhotosUploaded, etc.) when workflow transition occurs",
                EntityType = "Order",
                ExecutorType = "UpdateOrderFlagsSideEffectExecutor",
                ExecutorConfigJson = null,
                IsActive = true,
                DisplayOrder = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SideEffectDefinition
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId.Value,
                Key = "triggerInvoiceEligibility",
                Name = "Trigger Invoice Eligibility",
                Description = "Checks and updates invoice eligibility flag when workflow transition occurs",
                EntityType = "Order",
                ExecutorType = "TriggerInvoiceEligibilitySideEffectExecutor",
                ExecutorConfigJson = JsonSerializer.Serialize(new { requireDocket = true, requirePhotos = true, requireSerials = true }),
                IsActive = true,
                DisplayOrder = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new SideEffectDefinition
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId.Value,
                Key = "createInstallerTask",
                Name = "Create Installer Task",
                Description = "Creates a single task for the assigned service installer when order transitions to Assigned (idempotent per order).",
                EntityType = "Order",
                ExecutorType = "CreateInstallerTaskSideEffectExecutor",
                ExecutorConfigJson = null,
                IsActive = true,
                DisplayOrder = 6,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        foreach (var sideEffect in sideEffects)
        {
            var exists = await _context.Set<SideEffectDefinition>()
                .AnyAsync(sed => sed.CompanyId == companyId.Value 
                    && sed.Key == sideEffect.Key 
                    && sed.EntityType == sideEffect.EntityType
                    && !sed.IsDeleted);

            if (!exists)
            {
                _context.Set<SideEffectDefinition>().Add(sideEffect);
                _logger.LogInformation("Created side effect definition: {Key} ({Name})", sideEffect.Key, sideEffect.Name);
            }
            else
            {
                _logger.LogInformation("Side effect definition already exists, skipping: {Key}", sideEffect.Key);
            }
        }
    }

    /// <summary>
    /// Seed default Invoice document template (accounting-style layout)
    /// </summary>
    private async Task SeedDefaultInvoiceTemplateAsync(Guid? companyId)
    {
        if (!companyId.HasValue)
        {
            var company = await _context.Companies.FirstOrDefaultAsync();
            if (company == null)
            {
                _logger.LogWarning("No company found, skipping default Invoice template seed");
                return;
            }
            companyId = company.Id;
        }

        var exists = await _context.DocumentTemplates
            .IgnoreQueryFilters()
            .AnyAsync(t => t.CompanyId == companyId && t.DocumentType == "Invoice" && t.PartnerId == null && !t.IsDeleted);

        if (exists)
        {
            _logger.LogInformation("Default Invoice template already exists for company {CompanyId}", companyId);
            return;
        }

        const string htmlBody = @"<div class=""invoice-document"" style=""font-family: system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif; font-size: 11pt; line-height: 1.4; color: #1a1a1a; max-width: 210mm; margin: 0 auto; padding: 0;"">
  {{#if company.letterhead}}
  <div class=""letterhead"" style=""margin-bottom: 24px; padding-bottom: 16px; border-bottom: 1px solid #e5e7eb;"">
    <h1 style=""font-size: 18pt; font-weight: 700; margin: 0 0 6px 0; color: #111827;"">{{company.letterhead.name}}</h1>
    {{#if company.letterhead.address}}
    <div style=""font-size: 10pt; color: #4b5563; margin: 2px 0;"">{{company.letterhead.address}}</div>
    {{/if}}
    {{#if company.letterhead.phone}}{{#if company.letterhead.email}}
    <div style=""font-size: 10pt; color: #4b5563; margin: 2px 0;"">{{company.letterhead.phone}} | {{company.letterhead.email}}</div>
    {{/if}}{{/if}}
    {{#if company.letterhead.registrationNo}}
    <div style=""font-size: 10pt; color: #4b5563; margin: 2px 0;"">Registration: {{company.letterhead.registrationNo}}</div>
    {{/if}}
  </div>
  {{/if}}

  <h2 style=""margin: 0 0 16px 0; font-size: 16pt; font-weight: 700;"">INVOICE</h2>

  <div style=""display: flex; justify-content: space-between; gap: 24px; margin-bottom: 24px;"">
    <div style=""flex: 1;"">
      <h3 style=""font-size: 9pt; font-weight: 600; text-transform: uppercase; letter-spacing: 0.05em; color: #6b7280; margin: 0 0 8px 0;"">Bill To</h3>
      <div style=""font-weight: 600; font-size: 12pt; margin-bottom: 4px;"">{{partner.name}}</div>
      {{#if partner.address}}
      <pre style=""font-size: 10pt; color: #4b5563; margin: 2px 0; white-space: pre-wrap; font-family: inherit;"">{{partner.address}}</pre>
      {{/if}}
      <div style=""font-size: 10pt; color: #4b5563; margin: 2px 0;"">Person in charge: {{partner.contactName}}</div>
      {{#if partner.contactPhone}}
      <div style=""font-size: 10pt; color: #4b5563; margin: 2px 0;"">TEL: {{partner.contactPhone}}</div>
      {{/if}}
      <div style=""font-size: 10pt; color: #4b5563; margin: 2px 0;"">Subject: {{billToSubject}}</div>
    </div>
    <div style=""text-align: right; min-width: 180px;"">
      <div style=""display: flex; justify-content: flex-end; gap: 12px; margin: 4px 0;""><span style=""font-weight: 500; color: #6b7280; min-width: 90px;"">Date Issued:</span><span>{{date invoice.date ""dd MMM yyyy""}}</span></div>
      <div style=""display: flex; justify-content: flex-end; gap: 12px; margin: 4px 0;""><span style=""font-weight: 500; color: #6b7280; min-width: 90px;"">Invoice No.:</span><span>{{invoice.number}}</span></div>
      <div style=""display: flex; justify-content: flex-end; gap: 12px; margin: 4px 0;""><span style=""font-weight: 500; color: #6b7280; min-width: 90px;"">Terms:</span><span>Net {{invoice.termsInDays}} days</span></div>
      <div style=""display: flex; justify-content: flex-end; gap: 12px; margin: 4px 0;""><span style=""font-weight: 500; color: #6b7280; min-width: 90px;"">Prepared By:</span><span>Cephas Admin</span></div>
      {{#if invoice.dueDate}}
      <div style=""display: flex; justify-content: flex-end; gap: 12px; margin: 4px 0;""><span style=""font-weight: 500; color: #6b7280; min-width: 90px;"">Due Date:</span><span>{{date invoice.dueDate ""dd MMM yyyy""}}</span></div>
      {{/if}}
    </div>
  </div>

  <table style=""width: 100%; border-collapse: collapse; margin-bottom: 24px;"">
    <thead>
      <tr style=""background: #f9fafb; border-bottom: 2px solid #e5e7eb;"">
        <th style=""padding: 10px 12px; text-align: center; font-size: 9pt; font-weight: 600; text-transform: uppercase; color: #6b7280; width: 5%;"">No</th>
        <th style=""padding: 10px 12px; text-align: left; font-size: 9pt; font-weight: 600; text-transform: uppercase; color: #6b7280; width: 38%;"">Description</th>
        <th style=""padding: 10px 12px; text-align: right; font-size: 9pt; font-weight: 600; text-transform: uppercase; color: #6b7280; width: 10%;"">Qty</th>
        <th style=""padding: 10px 12px; text-align: right; font-size: 9pt; font-weight: 600; text-transform: uppercase; color: #6b7280; width: 15%;"">Unit Price</th>
        <th style=""padding: 10px 12px; text-align: right; font-size: 9pt; font-weight: 600; text-transform: uppercase; color: #6b7280; width: 12%;"">Discount</th>
        <th style=""padding: 10px 12px; text-align: right; font-size: 9pt; font-weight: 600; text-transform: uppercase; color: #6b7280; width: 18%;"">Total</th>
      </tr>
    </thead>
    <tbody>
      {{#each lineItems}}
      <tr style=""border-bottom: 1px solid #f3f4f6;"">
        <td style=""padding: 10px 12px; text-align: center; font-size: 10pt;"">{{index}}</td>
        <td style=""padding: 10px 12px; font-size: 9pt; line-height: 1.3; white-space: pre-wrap;"">{{description}}</td>
        <td style=""padding: 10px 12px; text-align: right; font-size: 10pt;"">{{quantity}}</td>
        <td style=""padding: 10px 12px; text-align: right; font-size: 10pt;"">{{currency unitPrice}}</td>
        <td style=""padding: 10px 12px; text-align: right; font-size: 10pt;"">0.00</td>
        <td style=""padding: 10px 12px; text-align: right; font-size: 10pt;"">{{currency total}}</td>
      </tr>
      {{/each}}
    </tbody>
  </table>

  <div style=""margin-left: auto; max-width: 280px; padding-top: 12px;"">
    <div style=""display: flex; justify-content: space-between; padding: 6px 0; font-size: 10pt;""><span style=""color: #6b7280;"">Subtotal:</span><span>{{currency invoice.subTotal}}</span></div>
    <div style=""display: flex; justify-content: space-between; padding: 6px 0; font-size: 10pt;""><span style=""color: #6b7280;"">Tax:</span><span>{{currency invoice.taxAmount}}</span></div>
    <div style=""display: flex; justify-content: space-between; padding: 12px 0 6px 0; font-size: 12pt; font-weight: 700; border-top: 2px solid #111827; margin-top: 8px; color: #111827;""><span>Total:</span><span>{{currency invoice.totalAmount}}</span></div>
  </div>

  <div style=""margin-top: 32px; padding-top: 16px; border-top: 1px solid #e5e7eb; font-size: 9pt; color: #374151;"">
    <div>Bank Name: AgroBank</div>
    <div>Bank Account Number: 1005511000058559</div>
    <div>Payable to: CEPHAS TRADING &amp; SERVICES</div>
    <div style=""margin-top: 10px; color: #9ca3af; font-style: italic;"">&quot;This is a computer generated document. No signature is required.&quot;</div>
  </div>

  <p style=""margin-top: 24px; font-size: 0.9em; color: #666;"">Generated: {{generatedAt}}</p>
</div>";

        var template = new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId.Value,
            Name = "Default Invoice",
            DocumentType = "Invoice",
            PartnerId = null,
            IsActive = true,
            Engine = "Handlebars",
            HtmlBody = htmlBody,
            Description = "Default accounting-style invoice template with company letterhead, Bill To, and line items",
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.DocumentTemplates.Add(template);
        _logger.LogInformation("Created default Invoice document template for company {CompanyId}", companyId);
    }

    /// <summary>
    /// Seed SMS/WhatsApp notification GlobalSettings
    /// </summary>
    private async Task SeedSmsWhatsAppGlobalSettingsAsync()
    {
        var settings = new[]
        {
            // SMS Settings
            new { Key = "SMS_Enabled", Value = "false", ValueType = "Bool", Description = "Enable SMS notifications", Module = "Notifications" },
            new { Key = "SMS_Provider", Value = "None", ValueType = "String", Description = "SMS provider (Twilio, SMS_Gateway, None)", Module = "Notifications" },
            new { Key = "SMS_Twilio_AccountSid", Value = "", ValueType = "String", Description = "Twilio Account SID (encrypted)", Module = "Notifications" },
            new { Key = "SMS_Twilio_AuthToken", Value = "", ValueType = "String", Description = "Twilio Auth Token (encrypted)", Module = "Notifications" },
            new { Key = "SMS_Twilio_FromNumber", Value = "", ValueType = "String", Description = "Twilio From Phone Number", Module = "Notifications" },
            new { Key = "SMS_AutoSendOnStatusChange", Value = "false", ValueType = "Bool", Description = "Automatically send SMS when order status changes", Module = "Notifications" },
            new { Key = "SMS_RetryAttempts", Value = "3", ValueType = "Int", Description = "Number of retry attempts for failed SMS", Module = "Notifications" },
            new { Key = "SMS_RetryDelaySeconds", Value = "5", ValueType = "Int", Description = "Delay between SMS retry attempts (seconds)", Module = "Notifications" },
            
            // WhatsApp Settings
            new { Key = "WhatsApp_Enabled", Value = "false", ValueType = "Bool", Description = "Enable WhatsApp notifications", Module = "Notifications" },
            new { Key = "WhatsApp_Provider", Value = "None", ValueType = "String", Description = "WhatsApp provider (Twilio, None)", Module = "Notifications" },
            new { Key = "WhatsApp_Twilio_AccountSid", Value = "", ValueType = "String", Description = "Twilio Account SID for WhatsApp (encrypted)", Module = "Notifications" },
            new { Key = "WhatsApp_Twilio_AuthToken", Value = "", ValueType = "String", Description = "Twilio Auth Token for WhatsApp (encrypted)", Module = "Notifications" },
            new { Key = "WhatsApp_Twilio_FromNumber", Value = "", ValueType = "String", Description = "Twilio WhatsApp From Number", Module = "Notifications" },
            new { Key = "WhatsApp_AutoSendOnStatusChange", Value = "false", ValueType = "Bool", Description = "Automatically send WhatsApp when order status changes", Module = "Notifications" },
            new { Key = "WhatsApp_RetryAttempts", Value = "3", ValueType = "Int", Description = "Number of retry attempts for failed WhatsApp", Module = "Notifications" },
            new { Key = "WhatsApp_RetryDelaySeconds", Value = "5", ValueType = "Int", Description = "Delay between WhatsApp retry attempts (seconds)", Module = "Notifications" },

            // MyInvois E-Invoice Settings
            new { Key = "EInvoice_Enabled", Value = "false", ValueType = "Bool", Description = "Enable e-invoice submission (MyInvois)", Module = "Billing" },
            new { Key = "EInvoice_Provider", Value = "Null", ValueType = "String", Description = "E-invoice provider (MyInvois, Null)", Module = "Billing" },
            new { Key = "MyInvois_BaseUrl", Value = "https://api-sandbox.myinvois.hasil.gov.my", ValueType = "String", Description = "MyInvois API base URL", Module = "Billing" },
            new { Key = "MyInvois_ClientId", Value = "", ValueType = "String", Description = "MyInvois Client ID (encrypted)", Module = "Billing" },
            new { Key = "MyInvois_ClientSecret", Value = "", ValueType = "String", Description = "MyInvois Client Secret (encrypted)", Module = "Billing" },
            new { Key = "MyInvois_Enabled", Value = "false", ValueType = "Bool", Description = "Enable MyInvois integration", Module = "Billing" },
            
            // Template Mapping Settings (optional - can be overridden)
            new { Key = "Notification_Assigned_SmsTemplateCode", Value = "ASSIGNED", ValueType = "String", Description = "SMS template code for Assigned status", Module = "Notifications" },
            new { Key = "Notification_Assigned_WhatsAppTemplateCode", Value = "ASSIGNED", ValueType = "String", Description = "WhatsApp template code for Assigned status", Module = "Notifications" },
            new { Key = "Notification_OnTheWay_SmsTemplateCode", Value = "OTW", ValueType = "String", Description = "SMS template code for OnTheWay status", Module = "Notifications" },
            new { Key = "Notification_OnTheWay_WhatsAppTemplateCode", Value = "OTW", ValueType = "String", Description = "WhatsApp template code for OnTheWay status", Module = "Notifications" },
            new { Key = "Notification_MetCustomer_SmsTemplateCode", Value = "MET_CUSTOMER", ValueType = "String", Description = "SMS template code for MetCustomer status", Module = "Notifications" },
            new { Key = "Notification_MetCustomer_WhatsAppTemplateCode", Value = "MET_CUSTOMER", ValueType = "String", Description = "WhatsApp template code for MetCustomer status", Module = "Notifications" },
            new { Key = "Notification_OrderCompleted_SmsTemplateCode", Value = "IN_PROGRESS", ValueType = "String", Description = "SMS template code for OrderCompleted status", Module = "Notifications" },
            new { Key = "Notification_OrderCompleted_WhatsAppTemplateCode", Value = "IN_PROGRESS", ValueType = "String", Description = "WhatsApp template code for OrderCompleted status", Module = "Notifications" },
            new { Key = "Notification_Completed_SmsTemplateCode", Value = "COMPLETED", ValueType = "String", Description = "SMS template code for Completed status", Module = "Notifications" },
            new { Key = "Notification_Completed_WhatsAppTemplateCode", Value = "COMPLETED", ValueType = "String", Description = "WhatsApp template code for Completed status", Module = "Notifications" },
            new { Key = "Notification_Cancelled_SmsTemplateCode", Value = "CANCELLED", ValueType = "String", Description = "SMS template code for Cancelled status", Module = "Notifications" },
            new { Key = "Notification_Cancelled_WhatsAppTemplateCode", Value = "CANCELLED", ValueType = "String", Description = "WhatsApp template code for Cancelled status", Module = "Notifications" },
            new { Key = "Notification_ReschedulePendingApproval_SmsTemplateCode", Value = "RESCHEDULED", ValueType = "String", Description = "SMS template code for ReschedulePendingApproval status", Module = "Notifications" },
            new { Key = "Notification_ReschedulePendingApproval_WhatsAppTemplateCode", Value = "RESCHEDULED", ValueType = "String", Description = "WhatsApp template code for ReschedulePendingApproval status", Module = "Notifications" },
            new { Key = "Notification_Blocker_SmsTemplateCode", Value = "BLOCKER", ValueType = "String", Description = "SMS template code for Blocker status", Module = "Notifications" },
            new { Key = "Notification_Blocker_WhatsAppTemplateCode", Value = "BLOCKER", ValueType = "String", Description = "WhatsApp template code for Blocker status", Module = "Notifications" },
            
            // Unified Messaging Routing Settings
            new { Key = "Messaging_SendSmsFallback", Value = "true", ValueType = "Bool", Description = "Send SMS alongside WhatsApp for non-urgent messages (optional fallback)", Module = "Notifications" },
            new { Key = "Messaging_AutoDetectWhatsApp", Value = "true", ValueType = "Bool", Description = "Automatically detect if customer uses WhatsApp by attempting to send", Module = "Notifications" },
            new { Key = "Messaging_WhatsAppRetryOnFailure", Value = "true", ValueType = "Bool", Description = "Retry with SMS if WhatsApp fails", Module = "Notifications" }
        };

        foreach (var settingData in settings)
        {
            var exists = await _context.GlobalSettings
                .AnyAsync(s => s.Key == settingData.Key);

            if (!exists)
            {
                var setting = new GlobalSetting
                {
                    Id = Guid.NewGuid(),
                    Key = settingData.Key,
                    Value = settingData.Value,
                    ValueType = settingData.ValueType,
                    Description = settingData.Description,
                    Module = settingData.Module,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.GlobalSettings.Add(setting);
                _logger.LogInformation("Created GlobalSetting: {Key}", settingData.Key);
            }
        }
    }

    /// <summary>
    /// Seed default MovementTypes for stock movements
    /// </summary>
    private async Task SeedDefaultMovementTypesAsync(Guid? companyId)
    {
        var companyIdValue = companyId ?? Guid.Empty;

        var movementTypes = new[]
        {
            // Inbound movements
            new { Code = "GRN", Name = "Goods Receipt Note", Description = "Receipt of materials from supplier", Direction = "In", RequiresFromLocation = false, RequiresToLocation = true, RequiresOrderId = false, RequiresServiceInstallerId = false, RequiresPartnerId = false, AffectsStockBalance = true, StockImpact = "Positive", SortOrder = 1 },
            new { Code = "ReturnFromSI", Name = "Return from Service Installer", Description = "Materials returned from service installer to warehouse", Direction = "In", RequiresFromLocation = false, RequiresToLocation = true, RequiresOrderId = false, RequiresServiceInstallerId = true, RequiresPartnerId = false, AffectsStockBalance = true, StockImpact = "Positive", SortOrder = 2 },
            new { Code = "ReturnFromCustomer", Name = "Return from Customer", Description = "Materials returned from customer site", Direction = "In", RequiresFromLocation = false, RequiresToLocation = true, RequiresOrderId = true, RequiresServiceInstallerId = false, RequiresPartnerId = false, AffectsStockBalance = true, StockImpact = "Positive", SortOrder = 3 },
            
            // Outbound movements
            new { Code = "IssueToSI", Name = "Issue to Service Installer", Description = "Materials issued to service installer for installation", Direction = "Out", RequiresFromLocation = true, RequiresToLocation = false, RequiresOrderId = false, RequiresServiceInstallerId = true, RequiresPartnerId = false, AffectsStockBalance = true, StockImpact = "Negative", SortOrder = 4 },
            new { Code = "IssueToOrder", Name = "Issue to Order", Description = "Materials issued directly to order/customer site", Direction = "Out", RequiresFromLocation = true, RequiresToLocation = false, RequiresOrderId = true, RequiresServiceInstallerId = false, RequiresPartnerId = false, AffectsStockBalance = true, StockImpact = "Negative", SortOrder = 5 },
            new { Code = "ReturnFaulty", Name = "Return Faulty", Description = "Faulty materials returned to warehouse/RMA", Direction = "In", RequiresFromLocation = false, RequiresToLocation = true, RequiresOrderId = true, RequiresServiceInstallerId = true, RequiresPartnerId = false, AffectsStockBalance = true, StockImpact = "Positive", SortOrder = 6 },
            
            // Transfer movements
            new { Code = "Transfer", Name = "Transfer", Description = "Transfer materials between locations", Direction = "Transfer", RequiresFromLocation = true, RequiresToLocation = true, RequiresOrderId = false, RequiresServiceInstallerId = false, RequiresPartnerId = false, AffectsStockBalance = true, StockImpact = "Neutral", SortOrder = 7 },
            new { Code = "TransferToRMA", Name = "Transfer to RMA", Description = "Transfer faulty materials to RMA location", Direction = "Transfer", RequiresFromLocation = true, RequiresToLocation = true, RequiresOrderId = false, RequiresServiceInstallerId = false, RequiresPartnerId = false, AffectsStockBalance = true, StockImpact = "Neutral", SortOrder = 8 },
            
            // Adjustment movements
            new { Code = "Adjustment", Name = "Stock Adjustment", Description = "Stock count adjustment (increase or decrease)", Direction = "Adjust", RequiresFromLocation = false, RequiresToLocation = true, RequiresOrderId = false, RequiresServiceInstallerId = false, RequiresPartnerId = false, AffectsStockBalance = true, StockImpact = "Positive", SortOrder = 9 },
            new { Code = "AdjustmentDown", Name = "Stock Adjustment (Decrease)", Description = "Stock count adjustment (decrease)", Direction = "Adjust", RequiresFromLocation = true, RequiresToLocation = false, RequiresOrderId = false, RequiresServiceInstallerId = false, RequiresPartnerId = false, AffectsStockBalance = true, StockImpact = "Negative", SortOrder = 10 },
            
            // Write-off movements
            new { Code = "WriteOff", Name = "Write Off", Description = "Materials written off (damaged, expired, etc.)", Direction = "Out", RequiresFromLocation = true, RequiresToLocation = false, RequiresOrderId = false, RequiresServiceInstallerId = false, RequiresPartnerId = false, AffectsStockBalance = true, StockImpact = "Negative", SortOrder = 11 }
        };

        foreach (var mtData in movementTypes)
        {
            var exists = await _context.MovementTypes
                .AnyAsync(mt => mt.Code == mtData.Code && 
                               (mt.CompanyId == companyIdValue || mt.CompanyId == Guid.Empty));

            if (!exists)
            {
                var movementType = new MovementType
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyIdValue,
                    Code = mtData.Code,
                    Name = mtData.Name,
                    Description = mtData.Description,
                    Direction = mtData.Direction,
                    RequiresFromLocation = mtData.RequiresFromLocation,
                    RequiresToLocation = mtData.RequiresToLocation,
                    RequiresOrderId = mtData.RequiresOrderId,
                    RequiresServiceInstallerId = mtData.RequiresServiceInstallerId,
                    RequiresPartnerId = mtData.RequiresPartnerId,
                    AffectsStockBalance = mtData.AffectsStockBalance,
                    StockImpact = mtData.StockImpact,
                    IsActive = true,
                    SortOrder = mtData.SortOrder,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.MovementTypes.Add(movementType);
                _logger.LogInformation("Created MovementType: {Code} ({Name})", mtData.Code, mtData.Name);
            }
        }
    }

    /// <summary>
    /// Seed default LocationTypes for stock locations
    /// </summary>
    private async Task SeedDefaultLocationTypesAsync(Guid? companyId)
    {
        var companyIdValue = companyId ?? Guid.Empty;

        var locationTypes = new[]
        {
            new { Code = "Warehouse", Name = "Warehouse", Description = "Main warehouse location", RequiresServiceInstallerId = false, RequiresBuildingId = false, RequiresWarehouseId = false, AutoCreate = true, AutoCreateTrigger = (string?)"WarehouseCreated", SortOrder = 1 },
            new { Code = "SI", Name = "Service Installer", Description = "Service installer stock location", RequiresServiceInstallerId = true, RequiresBuildingId = false, RequiresWarehouseId = false, AutoCreate = true, AutoCreateTrigger = (string?)"ServiceInstallerCreated", SortOrder = 2 },
            new { Code = "CustomerSite", Name = "Customer Site", Description = "Customer installation site", RequiresServiceInstallerId = false, RequiresBuildingId = true, RequiresWarehouseId = false, AutoCreate = true, AutoCreateTrigger = (string?)"BuildingCreated", SortOrder = 3 },
            new { Code = "RMA", Name = "RMA Location", Description = "Return Merchandise Authorization location", RequiresServiceInstallerId = false, RequiresBuildingId = false, RequiresWarehouseId = false, AutoCreate = false, AutoCreateTrigger = (string?)null, SortOrder = 4 },
            new { Code = "Transit", Name = "Transit", Description = "Materials in transit", RequiresServiceInstallerId = false, RequiresBuildingId = false, RequiresWarehouseId = false, AutoCreate = false, AutoCreateTrigger = (string?)null, SortOrder = 5 },
            new { Code = "Supplier", Name = "Supplier", Description = "Supplier location (for tracking)", RequiresServiceInstallerId = false, RequiresBuildingId = false, RequiresWarehouseId = false, AutoCreate = false, AutoCreateTrigger = (string?)null, SortOrder = 6 }
        };

        foreach (var ltData in locationTypes)
        {
            var exists = await _context.LocationTypes
                .AnyAsync(lt => lt.Code == ltData.Code && 
                               (lt.CompanyId == companyIdValue || lt.CompanyId == Guid.Empty));

            if (!exists)
            {
                var locationType = new LocationType
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyIdValue,
                    Code = ltData.Code,
                    Name = ltData.Name,
                    Description = ltData.Description,
                    RequiresServiceInstallerId = ltData.RequiresServiceInstallerId,
                    RequiresBuildingId = ltData.RequiresBuildingId,
                    RequiresWarehouseId = ltData.RequiresWarehouseId,
                    AutoCreate = ltData.AutoCreate,
                    AutoCreateTrigger = ltData.AutoCreateTrigger,
                    IsActive = true,
                    SortOrder = ltData.SortOrder,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.LocationTypes.Add(locationType);
                _logger.LogInformation("Created LocationType: {Code} ({Name})", ltData.Code, ltData.Name);
            }
        }
    }
}


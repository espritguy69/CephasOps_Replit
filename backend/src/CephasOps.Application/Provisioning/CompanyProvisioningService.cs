using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Provisioning.DTOs;
using CephasOps.Domain.Billing.Enums;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Companies.Enums;
using CephasOps.Domain.Departments.Entities;
using CephasOps.Domain.Tenants.Entities;
using CephasOps.Domain.Users.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Provisioning;

/// <summary>Provisions new tenants: Company, Tenant, default departments, tenant admin user. Transactional.</summary>
public class CompanyProvisioningService : ICompanyProvisioningService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<CompanyProvisioningService> _logger;

    private static readonly (string Name, string Code, string Description)[] DefaultDepartments =
    {
        ("Operations (GPON)", "GPON", "GPON Operations Department"),
        ("Finance", "FINANCE", "Finance and Billing"),
        ("Inventory", "INVENTORY", "Inventory and Warehouse"),
        ("Scheduler", "SCHEDULER", "Field Operations and Scheduling"),
        ("Admin", "ADMIN", "Administration")
    };

    public CompanyProvisioningService(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<CompanyProvisioningService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<ProvisionTenantResultDto> ProvisionAsync(ProvisionTenantRequestDto request, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(request.CompanyCode);
        var slug = NormalizeSlug(request.Slug ?? code);
        var adminEmail = request.AdminEmail?.Trim().ToLowerInvariant() ?? "";

        if (string.IsNullOrEmpty(code)) throw new ArgumentException("Company code is required.", nameof(request.CompanyCode));
        if (string.IsNullOrEmpty(adminEmail)) throw new ArgumentException("Admin email is required.", nameof(request.AdminEmail));

        if (await IsCompanyCodeInUseAsync(code, cancellationToken))
            throw new InvalidOperationException($"A company with code '{code}' already exists.");
        if (await IsSlugInUseAsync(slug, cancellationToken))
            throw new InvalidOperationException($"Tenant slug '{slug}' is already in use.");
        if (await _context.Users.AnyAsync(u => u.Email == adminEmail, cancellationToken))
            throw new InvalidOperationException($"A user with email '{adminEmail}' already exists.");

        var status = ParseStatus(request.InitialStatus);
        var adminPassword = !string.IsNullOrWhiteSpace(request.AdminPassword)
            ? request.AdminPassword
            : GenerateTemporaryPassword();
        var mustChangePassword = string.IsNullOrWhiteSpace(request.AdminPassword);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            return await TenantScopeExecutor.RunWithPlatformBypassAsync<ProvisionTenantResultDto>(async (ct) =>
            {
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.CompanyName.Trim(),
                Slug = slug,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            _context.Tenants.Add(tenant);

            var company = new Company
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                LegalName = request.CompanyName.Trim(),
                ShortName = request.CompanyName.Trim(),
                Code = code,
                Vertical = "General",
                IsActive = true,
                Status = status,
                CreatedAt = DateTime.UtcNow,
                DefaultTimezone = request.DefaultTimezone ?? "Asia/Kuala_Lumpur",
                DefaultLocale = request.DefaultLocale ?? "en-MY"
            };
            _context.Companies.Add(company);

            await _context.SaveChangesAsync(ct);

            var departments = new List<Department>();
            foreach (var (name, deptCode, description) in DefaultDepartments)
            {
                var dept = new Department
                {
                    Id = Guid.NewGuid(),
                    CompanyId = company.Id,
                    Name = name,
                    Code = deptCode,
                    Description = description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Departments.Add(dept);
                departments.Add(dept);
            }
            await _context.SaveChangesAsync(ct);

            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin", ct)
                ?? await _context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin", ct);
            if (adminRole == null)
                throw new InvalidOperationException("No Admin or SuperAdmin role found. Seed roles first.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                Name = request.AdminFullName.Trim(),
                Email = adminEmail,
                PasswordHash = _passwordHasher.HashPassword(adminPassword),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                MustChangePassword = mustChangePassword
            };
            _context.Users.Add(user);

            _context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = adminRole.Id,
                CompanyId = company.Id,
                CreatedAt = DateTime.UtcNow
            });

            var firstDept = departments[0];
            foreach (var dept in departments)
            {
                _context.DepartmentMemberships.Add(new DepartmentMembership
                {
                    Id = Guid.NewGuid(),
                    CompanyId = company.Id,
                    DepartmentId = dept.Id,
                    UserId = user.Id,
                    Role = dept.Code == "ADMIN" ? "HOD" : "Member",
                    IsDefault = dept.Id == firstDept.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync(ct);

            Guid? subscriptionId = null;
            string? planSlug = null;
            var planSlugToUse = !string.IsNullOrWhiteSpace(request.PlanSlug) ? request.PlanSlug.Trim() : "trial";
            var plan = await _context.BillingPlans.FirstOrDefaultAsync(p => p.Slug == planSlugToUse && p.IsActive, ct);
            if (plan == null && string.IsNullOrWhiteSpace(request.PlanSlug))
                plan = await _context.BillingPlans.OrderBy(p => p.CreatedAtUtc).FirstOrDefaultAsync(p => p.IsActive, ct);
            if (plan != null)
            {
                var trialDays = request.TrialDays ?? 14;
                var isTrial = status == CompanyStatus.Trial || string.IsNullOrWhiteSpace(request.PlanSlug);
                var sub = new Domain.Billing.Entities.TenantSubscription
                {
                    TenantId = tenant.Id,
                    BillingPlanId = plan.Id,
                    Status = isTrial ? TenantSubscriptionStatus.Trialing : TenantSubscriptionStatus.Active,
                    StartedAtUtc = DateTime.UtcNow,
                    CurrentPeriodEndUtc = DateTime.UtcNow.AddMonths(1),
                    TrialEndsAtUtc = isTrial ? DateTime.UtcNow.AddDays(trialDays) : null,
                    BillingCycle = plan.BillingCycle,
                    SeatLimit = null,
                    StorageLimitBytes = null,
                    NextBillingDateUtc = isTrial ? DateTime.UtcNow.AddDays(trialDays) : DateTime.UtcNow.AddMonths(1),
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                _context.TenantSubscriptions.Add(sub);
                await _context.SaveChangesAsync(ct);
                subscriptionId = sub.Id;
                planSlug = plan.Slug;
                company.SubscriptionId = sub.Id;
                await _context.SaveChangesAsync(ct);
            }

            await transaction.CommitAsync(ct);

            _logger.LogInformation("Provisioned tenant: CompanyId={CompanyId}, Code={Code}, AdminUserId={UserId}",
                company.Id, company.Code, user.Id);

            return new ProvisionTenantResultDto
            {
                TenantId = tenant.Id,
                CompanyId = company.Id,
                CompanyCode = company.Code,
                CompanyName = company.LegalName,
                Slug = tenant.Slug,
                Status = company.Status.ToString(),
                AdminUserId = user.Id,
                AdminEmail = user.Email,
                MustChangePassword = mustChangePassword,
                Departments = departments.Select(d => new ProvisionedDepartmentDto { Id = d.Id, Name = d.Name, Code = d.Code ?? "" }).ToList(),
                SubscriptionId = subscriptionId,
                PlanSlug = planSlug
            };
            }, cancellationToken);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> IsCompanyCodeInUseAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeCode(code);
        if (string.IsNullOrEmpty(normalized)) return false;
        return await _context.Companies.AnyAsync(c => c.Code != null && c.Code.ToLower() == normalized.ToLower(), cancellationToken);
    }

    public async Task<bool> IsSlugInUseAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeSlug(slug);
        if (string.IsNullOrEmpty(normalized)) return false;
        return await _context.Tenants.AnyAsync(t => t.Slug.ToLower() == normalized.ToLower(), cancellationToken);
    }

    private static string NormalizeCode(string? value) => string.IsNullOrWhiteSpace(value) ? "" : value.Trim().ToUpperInvariant();
    private static string NormalizeSlug(string? value) => string.IsNullOrWhiteSpace(value) ? "" : value.Trim().ToLowerInvariant();
    private static string GenerateTemporaryPassword() => "Temp@" + Guid.NewGuid().ToString("N")[..12];
    private static CompanyStatus ParseStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return CompanyStatus.Active;
        return Enum.TryParse<CompanyStatus>(value, true, out var s) ? s : CompanyStatus.Active;
    }
}

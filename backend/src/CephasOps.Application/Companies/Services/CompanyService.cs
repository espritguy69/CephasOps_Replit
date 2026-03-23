using CephasOps.Application.Companies.DTOs;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Companies.Enums;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Companies.Services;

/// <summary>
/// Application service that encapsulates company CRUD logic.
/// </summary>
public class CompanyService : ICompanyService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CompanyService> _logger;

    public CompanyService(
        ApplicationDbContext context,
        ILogger<CompanyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<CompanyDto>> GetCompaniesAsync(
        bool? isActive = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Company>()
            .Include(c => c.Tenant)
            .AsNoTracking();

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(c =>
                c.LegalName.ToLower().Contains(normalizedSearch) ||
                c.ShortName.ToLower().Contains(normalizedSearch) ||
                c.Vertical.ToLower().Contains(normalizedSearch));
        }

        var companies = await query
            .OrderBy(c => c.ShortName)
            .ToListAsync(cancellationToken);

        return companies.Select(MapToDto).ToList();
    }

    public async Task<CompanyDto?> GetCompanyByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var company = await _context.Set<Company>()
            .Include(c => c.Tenant)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return company == null ? null : MapToDto(company);
    }

    public async Task<CompanyDto?> SetCompanyStatusAsync(Guid companyId, CompanyStatus status, CancellationToken cancellationToken = default)
    {
        var company = await _context.Set<Company>()
            .Include(c => c.Tenant)
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company == null)
            return null;
        company.Status = status;
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Company {CompanyId} status set to {Status}", companyId, status);
        return MapToDto(company);
    }

    public async Task<CompanyDto> CreateCompanyAsync(CreateCompanyDto dto, CancellationToken cancellationToken = default)
    {
        // Enforce single-company model: only one company record is allowed
        var existingCount = await _context.Set<Company>()
            .CountAsync(cancellationToken);
        if (existingCount > 0)
        {
            throw new InvalidOperationException("A company already exists. Only a single company is allowed.");
        }

        var normalizedShortName = NormalizeRequiredValue(dto.ShortName, nameof(dto.ShortName));

        var shortNameExists = await _context.Set<Company>()
            .AnyAsync(c => c.ShortName.ToLower() == normalizedShortName.ToLower(), cancellationToken);

        if (shortNameExists)
        {
            throw new InvalidOperationException($"A company with short name '{dto.ShortName}' already exists.");
        }

        var company = new Company
        {
            Id = Guid.NewGuid(),
            LegalName = NormalizeRequiredValue(dto.LegalName, nameof(dto.LegalName)),
            ShortName = normalizedShortName,
            Vertical = NormalizeRequiredValue(dto.Vertical, nameof(dto.Vertical)),
            RegistrationNo = NormalizeOptionalValue(dto.RegistrationNo),
            TaxId = NormalizeOptionalValue(dto.TaxId),
            Address = NormalizeOptionalValue(dto.Address),
            Phone = NormalizeOptionalValue(dto.Phone),
            Email = NormalizeOptionalValue(dto.Email),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            // Locale Settings
            DefaultTimezone = dto.DefaultTimezone ?? "Asia/Kuala_Lumpur",
            DefaultDateFormat = dto.DefaultDateFormat ?? "dd/MM/yyyy",
            DefaultTimeFormat = dto.DefaultTimeFormat ?? "hh:mm a",
            DefaultCurrency = dto.DefaultCurrency ?? "MYR",
            DefaultLocale = dto.DefaultLocale ?? "en-MY"
        };

        _context.Add(company);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Company created: {CompanyId} ({ShortName})", company.Id, company.ShortName);

        return MapToDto(company);
    }

    public async Task<CompanyDto> UpdateCompanyAsync(
        Guid id,
        UpdateCompanyDto dto,
        CancellationToken cancellationToken = default)
    {
        var company = await _context.Set<Company>()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (company == null)
        {
            throw new KeyNotFoundException($"Company with ID {id} not found");
        }

        if (dto.ShortName is not null &&
            !string.Equals(dto.ShortName.Trim(), company.ShortName, StringComparison.OrdinalIgnoreCase))
        {
            var normalizedShortName = NormalizeRequiredValue(dto.ShortName, nameof(dto.ShortName));
            var normalizedComparison = normalizedShortName.ToLower();
            var duplicateShortName = await _context.Set<Company>()
                .AnyAsync(c => c.Id != id && c.ShortName.ToLower() == normalizedComparison, cancellationToken);

            if (duplicateShortName)
            {
                throw new InvalidOperationException($"A company with short name '{dto.ShortName}' already exists.");
            }

            company.ShortName = normalizedShortName;
        }

        if (dto.LegalName is not null)
        {
            company.LegalName = NormalizeRequiredValue(dto.LegalName, nameof(dto.LegalName));
        }

        if (dto.Vertical is not null)
        {
            company.Vertical = NormalizeRequiredValue(dto.Vertical, nameof(dto.Vertical));
        }

        if (dto.RegistrationNo is not null)
        {
            company.RegistrationNo = NormalizeOptionalValue(dto.RegistrationNo);
        }

        if (dto.TaxId is not null)
        {
            company.TaxId = NormalizeOptionalValue(dto.TaxId);
        }

        if (dto.Address is not null)
        {
            company.Address = NormalizeOptionalValue(dto.Address);
        }

        if (dto.Phone is not null)
        {
            company.Phone = NormalizeOptionalValue(dto.Phone);
        }

        if (dto.Email is not null)
        {
            company.Email = NormalizeOptionalValue(dto.Email);
        }

        if (dto.IsActive.HasValue)
        {
            company.IsActive = dto.IsActive.Value;
        }

        // Locale Settings
        if (dto.DefaultTimezone is not null)
        {
            company.DefaultTimezone = dto.DefaultTimezone.Trim();
        }

        if (dto.DefaultDateFormat is not null)
        {
            company.DefaultDateFormat = dto.DefaultDateFormat.Trim();
        }

        if (dto.DefaultTimeFormat is not null)
        {
            company.DefaultTimeFormat = dto.DefaultTimeFormat.Trim();
        }

        if (dto.DefaultCurrency is not null)
        {
            company.DefaultCurrency = dto.DefaultCurrency.Trim();
        }

        if (dto.DefaultLocale is not null)
        {
            company.DefaultLocale = dto.DefaultLocale.Trim();
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Company updated: {CompanyId}", company.Id);

        return MapToDto(company);
    }

    public async Task DeleteCompanyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var company = await _context.Set<Company>()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (company == null)
        {
            throw new KeyNotFoundException($"Company with ID {id} not found");
        }

        _context.Remove(company);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Company deleted: {CompanyId}", id);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to delete company {CompanyId}", id);
            throw new InvalidOperationException("Unable to delete company because it is referenced by other records.", ex);
        }
    }

    private static CompanyDto MapToDto(Company company) => new()
    {
        Id = company.Id,
        TenantId = company.TenantId,
        TenantSlug = company.Tenant?.Slug,
        Status = company.Status.ToString(),
        Code = company.Code,
        LegalName = company.LegalName,
        ShortName = company.ShortName,
        Vertical = company.Vertical,
        RegistrationNo = company.RegistrationNo,
        TaxId = company.TaxId,
        Address = company.Address,
        Phone = company.Phone,
        Email = company.Email,
        IsActive = company.IsActive,
        CreatedAt = company.CreatedAt,
        // Locale Settings
        DefaultTimezone = company.DefaultTimezone,
        DefaultDateFormat = company.DefaultDateFormat,
        DefaultTimeFormat = company.DefaultTimeFormat,
        DefaultCurrency = company.DefaultCurrency,
        DefaultLocale = company.DefaultLocale
    };

    private static string NormalizeRequiredValue(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{propertyName} is required.", propertyName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}



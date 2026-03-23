using CephasOps.Application.Companies.DTOs;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Companies.Services;

/// <summary>
/// Partner service implementation
/// </summary>
public class PartnerService : IPartnerService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PartnerService> _logger;

    public PartnerService(
        ApplicationDbContext context,
        ILogger<PartnerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<PartnerDto>> GetPartnersAsync(Guid? companyId, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new List<PartnerDto>();

        var query = _context.Partners.Where(p => p.CompanyId == effectiveCompanyId.Value);

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        var partners = await query
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return partners.Select(p => new PartnerDto
        {
            Id = p.Id,
            CompanyId = p.CompanyId,
            Name = p.Name,
            Code = p.Code,
            PartnerType = p.PartnerType,
            GroupId = p.GroupId,
            DepartmentId = p.DepartmentId,
            BillingAddress = p.BillingAddress,
            ContactName = p.ContactName,
            ContactEmail = p.ContactEmail,
            ContactPhone = p.ContactPhone,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    public async Task<PartnerDto?> GetPartnerByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return null;

        var partner = await _context.Partners
            .Where(p => p.Id == id && p.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (partner == null)
        {
            return null;
        }

        return new PartnerDto
        {
            Id = partner.Id,
            CompanyId = partner.CompanyId,
            Name = partner.Name,
            Code = partner.Code,
            PartnerType = partner.PartnerType,
            GroupId = partner.GroupId,
            DepartmentId = partner.DepartmentId,
            BillingAddress = partner.BillingAddress,
            ContactName = partner.ContactName,
            ContactEmail = partner.ContactEmail,
            ContactPhone = partner.ContactPhone,
            IsActive = partner.IsActive,
            CreatedAt = partner.CreatedAt
        };
    }

    public async Task<PartnerDto> CreatePartnerAsync(CreatePartnerDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to create a partner.");

        var partner = new CephasOps.Domain.Companies.Entities.Partner
        {
            Id = Guid.NewGuid(),
            CompanyId = effectiveCompanyId.Value,
            Name = dto.Name,
            Code = dto.Code,
            PartnerType = dto.PartnerType,
            GroupId = dto.GroupId,
            DepartmentId = dto.DepartmentId,
            BillingAddress = dto.BillingAddress,
            ContactName = dto.ContactName,
            ContactEmail = dto.ContactEmail,
            ContactPhone = dto.ContactPhone,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Partners.Add(partner);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Partner created: {PartnerId}, Name: {Name}", partner.Id, partner.Name);

        return new PartnerDto
        {
            Id = partner.Id,
            CompanyId = partner.CompanyId,
            Name = partner.Name,
            Code = partner.Code,
            PartnerType = partner.PartnerType,
            GroupId = partner.GroupId,
            DepartmentId = partner.DepartmentId,
            BillingAddress = partner.BillingAddress,
            ContactName = partner.ContactName,
            ContactEmail = partner.ContactEmail,
            ContactPhone = partner.ContactPhone,
            IsActive = partner.IsActive,
            CreatedAt = partner.CreatedAt
        };
    }

    public async Task<PartnerDto> UpdatePartnerAsync(Guid id, UpdatePartnerDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to update a partner.");

        var partner = await _context.Partners
            .Where(p => p.Id == id && p.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (partner == null)
        {
            throw new KeyNotFoundException($"Partner with ID {id} not found");
        }

        if (dto.Name != null) partner.Name = dto.Name;
        if (dto.Code != null) partner.Code = dto.Code;
        if (dto.PartnerType != null) partner.PartnerType = dto.PartnerType;
        if (dto.GroupId.HasValue) partner.GroupId = dto.GroupId;
        if (dto.DepartmentId.HasValue) partner.DepartmentId = dto.DepartmentId;
        if (dto.BillingAddress != null) partner.BillingAddress = dto.BillingAddress;
        if (dto.ContactName != null) partner.ContactName = dto.ContactName;
        if (dto.ContactEmail != null) partner.ContactEmail = dto.ContactEmail;
        if (dto.ContactPhone != null) partner.ContactPhone = dto.ContactPhone;
        if (dto.IsActive.HasValue) partner.IsActive = dto.IsActive.Value;

        partner.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Partner updated: {PartnerId}", id);

        return new PartnerDto
        {
            Id = partner.Id,
            CompanyId = partner.CompanyId,
            Name = partner.Name,
            Code = partner.Code,
            PartnerType = partner.PartnerType,
            GroupId = partner.GroupId,
            DepartmentId = partner.DepartmentId,
            BillingAddress = partner.BillingAddress,
            ContactName = partner.ContactName,
            ContactEmail = partner.ContactEmail,
            ContactPhone = partner.ContactPhone,
            IsActive = partner.IsActive,
            CreatedAt = partner.CreatedAt
        };
    }

    public async Task DeletePartnerAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to delete a partner.");

        var partner = await _context.Partners
            .Where(p => p.Id == id && p.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (partner == null)
        {
            throw new KeyNotFoundException($"Partner with ID {id} not found");
        }

        _context.Partners.Remove(partner);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Partner deleted: {PartnerId}", id);
    }
}


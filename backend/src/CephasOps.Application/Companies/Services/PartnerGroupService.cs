using CephasOps.Application.Companies.DTOs;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Companies.Services;

/// <summary>
/// Partner group service implementation
/// </summary>
public class PartnerGroupService : IPartnerGroupService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PartnerGroupService> _logger;

    public PartnerGroupService(
        ApplicationDbContext context,
        ILogger<PartnerGroupService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<PartnerGroupDto>> GetPartnerGroupsAsync(Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new List<PartnerGroupDto>();

        var partnerGroups = await _context.PartnerGroups
            .Where(pg => pg.CompanyId == effectiveCompanyId.Value)
            .OrderBy(pg => pg.Name)
            .ToListAsync(cancellationToken);

        return partnerGroups.Select(pg => new PartnerGroupDto
        {
            Id = pg.Id,
            CompanyId = pg.CompanyId,
            Name = pg.Name,
            CreatedAt = pg.CreatedAt,
            UpdatedAt = pg.UpdatedAt
        }).ToList();
    }

    public async Task<PartnerGroupDto?> GetPartnerGroupByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.PartnerGroups.Where(pg => pg.Id == id);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(pg => pg.CompanyId == companyId.Value);
        }
        var partnerGroup = await query.FirstOrDefaultAsync(cancellationToken);

        if (partnerGroup == null)
        {
            return null;
        }

        return new PartnerGroupDto
        {
            Id = partnerGroup.Id,
            CompanyId = partnerGroup.CompanyId,
            Name = partnerGroup.Name,
            CreatedAt = partnerGroup.CreatedAt,
            UpdatedAt = partnerGroup.UpdatedAt
        };
    }

    public async Task<PartnerGroupDto> CreatePartnerGroupAsync(CreatePartnerGroupDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to create a partner group.");

        var partnerGroup = new PartnerGroup
        {
            Id = Guid.NewGuid(),
            CompanyId = effectiveCompanyId.Value,
            Name = dto.Name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PartnerGroups.Add(partnerGroup);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Partner group created: {PartnerGroupId}, Name: {Name}", partnerGroup.Id, partnerGroup.Name);

        return new PartnerGroupDto
        {
            Id = partnerGroup.Id,
            CompanyId = partnerGroup.CompanyId,
            Name = partnerGroup.Name,
            CreatedAt = partnerGroup.CreatedAt,
            UpdatedAt = partnerGroup.UpdatedAt
        };
    }

    public async Task<PartnerGroupDto> UpdatePartnerGroupAsync(Guid id, UpdatePartnerGroupDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.PartnerGroups.Where(pg => pg.Id == id);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(pg => pg.CompanyId == companyId.Value);
        }
        var partnerGroup = await query.FirstOrDefaultAsync(cancellationToken);

        if (partnerGroup == null)
        {
            throw new KeyNotFoundException($"Partner group with ID {id} not found");
        }

        if (dto.Name != null)
        {
            partnerGroup.Name = dto.Name;
        }

        partnerGroup.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Partner group updated: {PartnerGroupId}", id);

        return new PartnerGroupDto
        {
            Id = partnerGroup.Id,
            CompanyId = partnerGroup.CompanyId,
            Name = partnerGroup.Name,
            CreatedAt = partnerGroup.CreatedAt,
            UpdatedAt = partnerGroup.UpdatedAt
        };
    }

    public async Task DeletePartnerGroupAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to delete a partner group.");

        var partnerGroup = await _context.PartnerGroups
            .Where(pg => pg.Id == id && pg.CompanyId == effectiveCompanyId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (partnerGroup == null)
        {
            throw new KeyNotFoundException($"Partner group with ID {id} not found");
        }

        // Check if any partners are using this group
        var partnersInGroup = await _context.Partners
            .Where(p => p.GroupId == id)
            .AnyAsync(cancellationToken);

        if (partnersInGroup)
        {
            throw new InvalidOperationException($"Cannot delete partner group with ID {id} because it has associated partners. Please reassign or remove partners first.");
        }

        _context.PartnerGroups.Remove(partnerGroup);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Partner group deleted: {PartnerGroupId}", id);
    }
}


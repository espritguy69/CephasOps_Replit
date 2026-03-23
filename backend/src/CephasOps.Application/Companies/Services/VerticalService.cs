using CephasOps.Application.Companies.DTOs;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Companies.Services;

/// <summary>
/// Vertical service implementation
/// </summary>
public class VerticalService : IVerticalService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VerticalService> _logger;

    public VerticalService(ApplicationDbContext context, ILogger<VerticalService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<VerticalDto>> GetVerticalsAsync(Guid? companyId, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting verticals for company {CompanyId}", companyId);

        var query = _context.Verticals.AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(v => v.CompanyId == companyId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(v => v.IsActive == isActive.Value);
        }

        var verticals = await query
            .OrderBy(v => v.DisplayOrder)
            .ThenBy(v => v.Name)
            .ToListAsync(cancellationToken);

        return verticals.Select(VerticalDto.FromEntity).ToList();
    }

    public async Task<VerticalDto?> GetVerticalByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting vertical {VerticalId} for company {CompanyId}", id, companyId);

        var query = _context.Verticals.Where(v => v.Id == id);
        if (companyId.HasValue)
        {
            query = query.Where(v => v.CompanyId == companyId.Value);
        }

        var vertical = await query.FirstOrDefaultAsync(cancellationToken);
        return vertical != null ? VerticalDto.FromEntity(vertical) : null;
    }

    public async Task<VerticalDto> CreateVerticalAsync(CreateVerticalDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating vertical for company {CompanyId}", companyId);

        var name = NormalizeRequired(dto.Name, nameof(dto.Name));
        var code = NormalizeRequired(dto.Code, nameof(dto.Code)).ToUpperInvariant();

        var vertical = new Vertical
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = name,
            Code = code,
            Description = NormalizeOptional(dto.Description),
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder
        };

        _context.Verticals.Add(vertical);
        await _context.SaveChangesAsync(cancellationToken);

        return VerticalDto.FromEntity(vertical);
    }

    public async Task<VerticalDto> UpdateVerticalAsync(Guid id, UpdateVerticalDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating vertical {VerticalId} for company {CompanyId}", id, companyId);

        var query = _context.Verticals.Where(v => v.Id == id);
        if (companyId.HasValue)
        {
            query = query.Where(v => v.CompanyId == companyId.Value);
        }

        var vertical = await query.FirstOrDefaultAsync(cancellationToken);

        if (vertical == null)
        {
            throw new KeyNotFoundException($"Vertical with ID {id} not found");
        }

        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            vertical.Name = NormalizeRequired(dto.Name, nameof(dto.Name));
        }

        if (dto.Code is not null)
        {
            vertical.Code = NormalizeRequired(dto.Code, nameof(dto.Code)).ToUpperInvariant();
        }

        if (dto.Description is not null)
        {
            vertical.Description = NormalizeOptional(dto.Description);
        }

        if (dto.DisplayOrder.HasValue)
        {
            vertical.DisplayOrder = dto.DisplayOrder.Value;
        }

        if (dto.IsActive.HasValue)
        {
            vertical.IsActive = dto.IsActive.Value;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return VerticalDto.FromEntity(vertical);
    }

    public async Task DeleteVerticalAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting vertical {VerticalId} for company {CompanyId}", id, companyId);

        var query = _context.Verticals.Where(v => v.Id == id);
        if (companyId.HasValue)
        {
            query = query.Where(v => v.CompanyId == companyId.Value);
        }

        var vertical = await query.FirstOrDefaultAsync(cancellationToken);

        if (vertical == null)
        {
            throw new KeyNotFoundException($"Vertical with ID {id} not found");
        }

        _context.Verticals.Remove(vertical);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeRequired(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{propertyName} is required.", propertyName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}



using CephasOps.Application.Buildings.DTOs;
using CephasOps.Domain.Buildings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Buildings.Services;

/// <summary>
/// InstallationMethod service implementation
/// </summary>
public class InstallationMethodService : IInstallationMethodService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InstallationMethodService> _logger;

    public InstallationMethodService(ApplicationDbContext context, ILogger<InstallationMethodService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<InstallationMethodDto>> GetInstallationMethodsAsync(Guid? companyId, Guid? departmentId = null, string? category = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.InstallationMethods.AsQueryable();
        
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(im => im.CompanyId == companyId.Value);
        }

        // Filter by department if specified, but also include global methods (DepartmentId == null)
        // This ensures that shared/global installation methods are available to all departments
        if (departmentId.HasValue)
        {
            query = query.Where(im => im.DepartmentId == departmentId.Value || im.DepartmentId == null);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(im => im.Category == category);
        }

        if (isActive.HasValue)
        {
            query = query.Where(im => im.IsActive == isActive.Value);
        }

        var installationMethods = await query
            .OrderBy(im => im.DisplayOrder)
            .ThenBy(im => im.Name)
            .ToListAsync(cancellationToken);

        return installationMethods.Select(MapToDto).ToList();
    }

    public async Task<InstallationMethodDto?> GetInstallationMethodByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.InstallationMethods.Where(im => im.Id == id);
        
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(im => im.CompanyId == companyId.Value);
        }
        
        var installationMethod = await query.FirstOrDefaultAsync(cancellationToken);

        return installationMethod != null ? MapToDto(installationMethod) : null;
    }

    public async Task<InstallationMethodDto> CreateInstallationMethodAsync(CreateInstallationMethodDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        // Check for duplicate name (case-insensitive)
        var duplicateNameQuery = _context.InstallationMethods
            .Where(im => EF.Functions.ILike(im.Name, dto.Name.Trim()));
        
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            duplicateNameQuery = duplicateNameQuery.Where(im => im.CompanyId == companyId.Value);
        }
        
        var duplicateName = await duplicateNameQuery.FirstOrDefaultAsync(cancellationToken);
        if (duplicateName != null)
        {
            throw new InvalidOperationException($"An installation method with the name '{dto.Name}' already exists.");
        }

        // Check for duplicate code (case-insensitive)
        if (!string.IsNullOrWhiteSpace(dto.Code))
        {
            var duplicateCodeQuery = _context.InstallationMethods
                .Where(im => EF.Functions.ILike(im.Code, dto.Code.Trim()));
            
            if (companyId.HasValue && companyId.Value != Guid.Empty)
            {
                duplicateCodeQuery = duplicateCodeQuery.Where(im => im.CompanyId == companyId.Value);
            }
            
            var duplicateCode = await duplicateCodeQuery.FirstOrDefaultAsync(cancellationToken);
            if (duplicateCode != null)
            {
                throw new InvalidOperationException($"An installation method with the code '{dto.Code}' already exists.");
            }
        }

        var installationMethod = new InstallationMethod
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = dto.Name.Trim(),
            Code = dto.Code?.Trim() ?? string.Empty,
            Category = dto.Category,
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.InstallationMethods.Add(installationMethod);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("InstallationMethod created: {InstallationMethodId}, Name: {Name}", installationMethod.Id, installationMethod.Name);

        return MapToDto(installationMethod);
    }

    public async Task<InstallationMethodDto> UpdateInstallationMethodAsync(Guid id, UpdateInstallationMethodDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.InstallationMethods.Where(im => im.Id == id);
        
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(im => im.CompanyId == companyId.Value);
        }
        
        var installationMethod = await query.FirstOrDefaultAsync(cancellationToken);

        if (installationMethod == null)
        {
            throw new KeyNotFoundException($"InstallationMethod with ID {id} not found");
        }

        // Check for duplicate name (case-insensitive) - exclude current record
        if (!string.IsNullOrEmpty(dto.Name) && dto.Name.Trim() != installationMethod.Name)
        {
            var duplicateNameQuery = _context.InstallationMethods
                .Where(im => im.Id != id && EF.Functions.ILike(im.Name, dto.Name.Trim()));
            
            if (companyId.HasValue && companyId.Value != Guid.Empty)
            {
                duplicateNameQuery = duplicateNameQuery.Where(im => im.CompanyId == companyId.Value);
            }
            
            var duplicateName = await duplicateNameQuery.FirstOrDefaultAsync(cancellationToken);
            if (duplicateName != null)
            {
                throw new InvalidOperationException($"An installation method with the name '{dto.Name}' already exists.");
            }
        }

        // Check for duplicate code (case-insensitive) - exclude current record
        if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code.Trim() != installationMethod.Code)
        {
            var duplicateCodeQuery = _context.InstallationMethods
                .Where(im => im.Id != id && EF.Functions.ILike(im.Code, dto.Code.Trim()));
            
            if (companyId.HasValue && companyId.Value != Guid.Empty)
            {
                duplicateCodeQuery = duplicateCodeQuery.Where(im => im.CompanyId == companyId.Value);
            }
            
            var duplicateCode = await duplicateCodeQuery.FirstOrDefaultAsync(cancellationToken);
            if (duplicateCode != null)
            {
                throw new InvalidOperationException($"An installation method with the code '{dto.Code}' already exists.");
            }
        }

        installationMethod.Name = dto.Name?.Trim() ?? installationMethod.Name;
        installationMethod.Code = dto.Code?.Trim() ?? installationMethod.Code;
        installationMethod.Category = dto.Category;
        installationMethod.Description = dto.Description;
        installationMethod.IsActive = dto.IsActive;
        installationMethod.DisplayOrder = dto.DisplayOrder;
        installationMethod.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("InstallationMethod updated: {InstallationMethodId}", id);

        return MapToDto(installationMethod);
    }

    public async Task DeleteInstallationMethodAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.InstallationMethods.Where(im => im.Id == id);
        
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(im => im.CompanyId == companyId.Value);
        }
        
        var installationMethod = await query.FirstOrDefaultAsync(cancellationToken);

        if (installationMethod == null)
        {
            throw new KeyNotFoundException($"InstallationMethod with ID {id} not found");
        }

        // Check if any buildings are using this installation method
        var hasBuildings = await _context.Buildings.AnyAsync(b => b.InstallationMethodId == id, cancellationToken);
        if (hasBuildings)
        {
            throw new InvalidOperationException($"Cannot delete InstallationMethod {id} because it is being used by buildings");
        }

        _context.InstallationMethods.Remove(installationMethod);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("InstallationMethod deleted: {InstallationMethodId}", id);
    }

    private static InstallationMethodDto MapToDto(InstallationMethod installationMethod)
    {
        return new InstallationMethodDto
        {
            Id = installationMethod.Id,
            Name = installationMethod.Name,
            Code = installationMethod.Code,
            Category = installationMethod.Category,
            Description = installationMethod.Description,
            IsActive = installationMethod.IsActive,
            DisplayOrder = installationMethod.DisplayOrder,
            CreatedAt = installationMethod.CreatedAt,
            UpdatedAt = installationMethod.UpdatedAt
        };
    }
}


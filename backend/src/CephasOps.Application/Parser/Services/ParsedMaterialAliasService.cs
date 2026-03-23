using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Utilities;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Parser.Services;

public class ParsedMaterialAliasService : IParsedMaterialAliasService
{
    private readonly ApplicationDbContext _context;

    public ParsedMaterialAliasService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ParsedMaterialAliasDto> CreateAliasAsync(Guid companyId, Guid userId, CreateParsedMaterialAliasRequest request, CancellationToken cancellationToken = default)
    {
        var aliasText = request.AliasText?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(aliasText))
            throw new ArgumentException("AliasText is required.", nameof(request));

        var normalized = MaterialNameNormalizer.Normalize(aliasText);
        if (string.IsNullOrEmpty(normalized))
            throw new ArgumentException("AliasText normalizes to empty.", nameof(request));

        var material = await _context.Materials
            .FirstOrDefaultAsync(m => m.Id == request.MaterialId && m.CompanyId == companyId, cancellationToken);
        if (material == null)
            throw new InvalidOperationException("Material not found or does not belong to company.");

        var entity = new ParsedMaterialAlias
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            AliasText = aliasText,
            NormalizedAliasText = normalized,
            MaterialId = request.MaterialId,
            CreatedByUserId = userId,
            Source = "ParserManualResolve",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.ParsedMaterialAliases.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return ToDto(entity, material);
    }

    /// <inheritdoc />
    public async Task<List<ParsedMaterialAliasDto>> ListAliasesAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var list = await _context.ParsedMaterialAliases
            .Where(a => a.CompanyId == companyId)
            .OrderBy(a => a.NormalizedAliasText)
            .ToListAsync(cancellationToken);
        var materialIds = list.Select(a => a.MaterialId).Distinct().ToList();
        var materials = await _context.Materials
            .Where(m => materialIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, cancellationToken);
        return list.Select(a => ToDto(a, materials.GetValueOrDefault(a.MaterialId))).ToList();
    }

    private static ParsedMaterialAliasDto ToDto(ParsedMaterialAlias a, CephasOps.Domain.Inventory.Entities.Material? material)
    {
        return new ParsedMaterialAliasDto
        {
            Id = a.Id,
            CompanyId = a.CompanyId,
            AliasText = a.AliasText,
            NormalizedAliasText = a.NormalizedAliasText,
            MaterialId = a.MaterialId,
            MaterialItemCode = material?.ItemCode,
            MaterialDescription = material?.Description,
            CreatedByUserId = a.CreatedByUserId,
            Source = a.Source,
            IsActive = a.IsActive,
            CreatedAt = a.CreatedAt
        };
    }
}

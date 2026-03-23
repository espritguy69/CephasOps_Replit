using CephasOps.Application.Buildings.DTOs;
using CephasOps.Application.Parser.Utilities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace CephasOps.Application.Buildings.Services;

/// <summary>
/// Service for matching parsed building data against existing buildings
/// </summary>
public class BuildingMatchingService : IBuildingMatchingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BuildingMatchingService> _logger;

    public BuildingMatchingService(
        ApplicationDbContext context,
        ILogger<BuildingMatchingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BuildingDto?> FindMatchingBuildingAsync(
        string? parsedBuildingName,
        string? parsedAddress,
        string? parsedCity,
        string? parsedPostcode,
        string? buildingCode,
        Guid? companyId,
        CancellationToken cancellationToken = default)
    {
        if (companyId == null || companyId == Guid.Empty)
        {
            _logger.LogWarning("Cannot match building without company context");
            return null;
        }

        // PRIORITY 1: Try exact match by building code (if provided)
        if (!string.IsNullOrWhiteSpace(buildingCode))
        {
            var normalizedCode = NormalizeString(buildingCode);
            var matchByCode = await _context.Buildings
                .Where(b => b.CompanyId == companyId && !b.IsDeleted)
                .Where(b => b.Code != null && EF.Functions.ILike(b.Code, normalizedCode))
                .FirstOrDefaultAsync(cancellationToken);

            if (matchByCode != null)
            {
                _logger.LogInformation("Building matched by code: {Code} -> {BuildingId}", buildingCode, matchByCode.Id);
                return MapToDto(matchByCode);
            }
        }

        // PRIORITY 2: Try match by normalized name + postcode (address abbreviations normalized: Jln/Jalan, Tmn/Taman, Lrg/Lorong)
        if (!string.IsNullOrWhiteSpace(parsedBuildingName) && !string.IsNullOrWhiteSpace(parsedPostcode))
        {
            var normalizedName = NormalizeForMatching(parsedBuildingName);
            var normalizedPostcode = NormalizeString(parsedPostcode);

            var candidates = await _context.Buildings
                .Where(b => b.CompanyId == companyId && !b.IsDeleted)
                .Where(b => EF.Functions.ILike(b.Postcode, normalizedPostcode))
                .Where(b => b.Name != null)
                .ToListAsync(cancellationToken);

            var matchByNamePostcode = candidates
                .FirstOrDefault(b => NormalizeForMatching(b.Name!) == normalizedName);

            if (matchByNamePostcode != null)
            {
                _logger.LogInformation("Building matched by name + postcode: {Name} + {Postcode} -> {BuildingId}", 
                    parsedBuildingName, parsedPostcode, matchByNamePostcode.Id);
                return MapToDto(matchByNamePostcode);
            }
        }

        // PRIORITY 3: Try match by normalized name + city (address abbreviations normalized)
        if (!string.IsNullOrWhiteSpace(parsedBuildingName) && !string.IsNullOrWhiteSpace(parsedCity))
        {
            var normalizedName = NormalizeForMatching(parsedBuildingName);
            var normalizedCity = NormalizeString(parsedCity);

            var candidates = await _context.Buildings
                .Where(b => b.CompanyId == companyId && !b.IsDeleted)
                .Where(b => b.Name != null && EF.Functions.ILike(b.City, $"%{normalizedCity}%"))
                .ToListAsync(cancellationToken);

            var matchByNameCity = candidates
                .FirstOrDefault(b => NormalizeForMatching(b.Name!) == normalizedName);

            if (matchByNameCity != null)
            {
                _logger.LogInformation("Building matched by name + city: {Name} + {City} -> {BuildingId}", 
                    parsedBuildingName, parsedCity, matchByNameCity.Id);
                return MapToDto(matchByNameCity);
            }
        }

        // No reliable match found
        _logger.LogInformation("No building match found for: Name={Name}, City={City}, Postcode={Postcode}", 
            parsedBuildingName, parsedCity, parsedPostcode);
        return null;
    }

    public async Task<List<BuildingMatchCandidateDto>> FindFuzzyBuildingCandidatesAsync(
        string? parsedBuildingName,
        string? parsedCity,
        string? parsedPostcode,
        Guid? companyId,
        double minScore = 0.85,
        int maxResults = 3,
        CancellationToken cancellationToken = default)
    {
        var result = new List<BuildingMatchCandidateDto>();
        if (companyId == null || companyId == Guid.Empty || string.IsNullOrWhiteSpace(parsedBuildingName))
        {
            _logger.LogDebug("Fuzzy building candidates skipped: missing company or building name");
            return result;
        }

        var query = _context.Buildings
            .Where(b => b.CompanyId == companyId && !b.IsDeleted && b.Name != null);

        if (!string.IsNullOrWhiteSpace(parsedPostcode))
        {
            var pc = parsedPostcode.Trim();
            query = query.Where(b => b.Postcode != null && b.Postcode.Trim() == pc);
        }
        else if (!string.IsNullOrWhiteSpace(parsedCity))
        {
            var city = parsedCity.Trim();
            query = query.Where(b => b.City != null && EF.Functions.ILike(b.City, $"%{city}%"));
        }

        var candidates = await query
            .OrderBy(b => b.Name)
            .Take(500)
            .ToListAsync(cancellationToken);

        var scored = new List<(Domain.Buildings.Entities.Building b, double score)>();
        foreach (var b in candidates)
        {
            var score = AddressParser.FuzzyMatchBuildingName(parsedBuildingName!, b.Name!);
            if (score >= minScore)
                scored.Add((b, score));
        }

        foreach (var (b, score) in scored.OrderByDescending(x => x.score).Take(maxResults))
        {
            result.Add(new BuildingMatchCandidateDto
            {
                Building = MapToListItemDto(b),
                SimilarityScore = Math.Round(score, 2)
            });
        }

        if (result.Count > 0)
            _logger.LogInformation("Fuzzy building candidates for '{Name}': {Count} matches (minScore={MinScore})",
                parsedBuildingName, result.Count, minScore);

        return result;
    }

    private static BuildingListItemDto MapToListItemDto(Domain.Buildings.Entities.Building b)
    {
        return new BuildingListItemDto
        {
            Id = b.Id,
            Name = b.Name ?? string.Empty,
            Code = b.Code,
#pragma warning disable CS0618
            PropertyType = b.PropertyType?.ToString(),
#pragma warning restore CS0618
            BuildingTypeId = b.BuildingTypeId,
            City = b.City ?? string.Empty,
            State = b.State ?? string.Empty,
            Area = b.Area,
            RfbAssignedDate = b.RfbAssignedDate,
            FirstOrderDate = b.FirstOrderDate,
            IsActive = b.IsActive,
            OrdersCount = 0
        };
    }

    /// <summary>
    /// Normalize for matching: expand address abbreviations (Jln→Jalan, Tmn→Taman, Lrg→Lorong) then trim, lowercase, remove extra spaces and punctuation.
    /// </summary>
    private static string NormalizeForMatching(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        var withFullAddress = AddressParser.NormalizeStreetNames(input);
        return NormalizeStringStatic(withFullAddress);
    }

    /// <summary>
    /// Normalize string for matching: trim, lowercase, remove extra spaces and punctuation
    /// </summary>
    private static string NormalizeStringStatic(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        var normalized = input.Trim().ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"[.,;:""'(){}[\]]", "");
        normalized = Regex.Replace(normalized, @"\s+", " ");
        return normalized.Trim();
    }

    private string NormalizeString(string input) => NormalizeStringStatic(input);

    private BuildingDto MapToDto(Domain.Buildings.Entities.Building building)
    {
        return new BuildingDto
        {
            Id = building.Id,
            CompanyId = building.CompanyId,
            DepartmentId = building.DepartmentId,
            Name = building.Name,
            Code = building.Code,
            AddressLine1 = building.AddressLine1,
            AddressLine2 = building.AddressLine2,
            City = building.City,
            State = building.State,
            Postcode = building.Postcode,
            Area = building.Area,
            Latitude = building.Latitude,
            Longitude = building.Longitude,
#pragma warning disable CS0618 // Type or member is obsolete - kept for backward compatibility
            PropertyType = building.PropertyType,
#pragma warning restore CS0618
            BuildingTypeId = null, // Obsolete: Use PropertyType enum instead
            InstallationMethodId = building.InstallationMethodId,
            RfbAssignedDate = building.RfbAssignedDate,
            FirstOrderDate = building.FirstOrderDate,
            Notes = building.Notes,
            IsActive = building.IsActive,
            CreatedAt = building.CreatedAt,
            UpdatedAt = building.UpdatedAt
        };
    }
}


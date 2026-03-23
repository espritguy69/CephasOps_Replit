using CephasOps.Application.Buildings.DTOs;

namespace CephasOps.Application.Buildings.Services;

/// <summary>
/// Service for matching parsed building data against existing buildings
/// </summary>
public interface IBuildingMatchingService
{
    /// <summary>
    /// Attempts to find an existing building that matches the parsed data
    /// </summary>
    /// <param name="parsedBuildingName">Building name from parser</param>
    /// <param name="parsedAddress">Full address text from parser</param>
    /// <param name="parsedCity">City from parser</param>
    /// <param name="parsedPostcode">Postcode from parser</param>
    /// <param name="buildingCode">Optional building code from parser</param>
    /// <param name="companyId">Company ID for scoping</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matched building DTO or null if no reliable match found</returns>
    Task<BuildingDto?> FindMatchingBuildingAsync(
        string? parsedBuildingName,
        string? parsedAddress,
        string? parsedCity,
        string? parsedPostcode,
        string? buildingCode,
        Guid? companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns fuzzy-matched building candidates (e.g. "ROYCE RESIDENCE" → "Royce Residences") for user selection.
    /// Uses Levenshtein-based similarity; only candidates with score &gt;= minScore are returned, up to maxResults.
    /// </summary>
    Task<List<BuildingMatchCandidateDto>> FindFuzzyBuildingCandidatesAsync(
        string? parsedBuildingName,
        string? parsedCity,
        string? parsedPostcode,
        Guid? companyId,
        double minScore = 0.85,
        int maxResults = 3,
        CancellationToken cancellationToken = default);
}


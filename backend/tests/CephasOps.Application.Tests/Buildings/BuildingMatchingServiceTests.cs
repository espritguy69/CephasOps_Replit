using CephasOps.Application.Buildings.Services;
using CephasOps.Domain.Buildings.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Buildings;

/// <summary>
/// Unit tests for BuildingMatchingService (exact and fuzzy building matching). Tenant-scoped (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class BuildingMatchingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<BuildingMatchingService>> _mockLogger;
    private readonly BuildingMatchingService _service;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public BuildingMatchingServiceTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<BuildingMatchingService>>();
        _service = new BuildingMatchingService(_dbContext, _mockLogger.Object);
    }

    #region FindMatchingBuildingAsync

    [Fact]
    public async Task FindMatchingBuildingAsync_NullCompanyId_ReturnsNull()
    {
        var result = await _service.FindMatchingBuildingAsync(
            "Some Building", null, "KL", "50000", null, null);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindMatchingBuildingAsync_EmptyCompanyId_ReturnsNull()
    {
        var result = await _service.FindMatchingBuildingAsync(
            "Some Building", null, "KL", "50000", null, Guid.Empty);

        result.Should().BeNull();
    }

    [Fact(Skip = "InMemory provider does not support Npgsql EF.Functions.ILike; use integration tests with PostgreSQL.")]
    public async Task FindMatchingBuildingAsync_MatchByCode_ReturnsBuilding()
    {
        var building = await CreateTestBuildingAsync("Tower A", postcode: "50000", code: "BLD-001");

        var result = await _service.FindMatchingBuildingAsync(
            null, null, null, null, "BLD-001", _companyId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(building.Id);
        result.Name.Should().Be("Tower A");
        result.Code.Should().Be("BLD-001");
    }

    [Fact(Skip = "InMemory provider does not support Npgsql EF.Functions.ILike; use integration tests with PostgreSQL.")]
    public async Task FindMatchingBuildingAsync_MatchByNameAndPostcode_ReturnsBuilding()
    {
        await CreateTestBuildingAsync("Royce Residences", postcode: "50450", code: "RR-1");

        var result = await _service.FindMatchingBuildingAsync(
            "Royce Residences", null, "Kuala Lumpur", "50450", null, _companyId);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Royce Residences");
        result.Postcode.Should().Be("50450");
    }

    [Fact(Skip = "InMemory provider does not support Npgsql EF.Functions.ILike; use integration tests with PostgreSQL.")]
    public async Task FindMatchingBuildingAsync_MatchByNameAndCity_ReturnsBuilding()
    {
        await CreateTestBuildingAsync("Tower B", city: "Petaling Jaya", postcode: "46000");

        var result = await _service.FindMatchingBuildingAsync(
            "Tower B", null, "Petaling Jaya", null, null, _companyId);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Tower B");
        result.City.Should().Be("Petaling Jaya");
    }

    [Fact(Skip = "InMemory provider does not support Npgsql EF.Functions.ILike; use integration tests with PostgreSQL.")]
    public async Task FindMatchingBuildingAsync_NoMatch_ReturnsNull()
    {
        await CreateTestBuildingAsync("Other Tower", postcode: "50000");

        var result = await _service.FindMatchingBuildingAsync(
            "Nonexistent Building", null, "KL", "50000", null, _companyId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FindMatchingBuildingAsync_ExcludesDeletedBuildings()
    {
        var building = await CreateTestBuildingAsync("Deleted Tower", postcode: "50000", code: "DEL-1", isDeleted: true);

        var result = await _service.FindMatchingBuildingAsync(
            null, null, null, null, "DEL-1", _companyId);

        result.Should().BeNull();
    }

    #endregion

    #region FindFuzzyBuildingCandidatesAsync

    [Fact]
    public async Task FindFuzzyBuildingCandidatesAsync_NullCompanyId_ReturnsEmpty()
    {
        var result = await _service.FindFuzzyBuildingCandidatesAsync(
            "Royce Residence", null, null, null);

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task FindFuzzyBuildingCandidatesAsync_EmptyBuildingName_ReturnsEmpty()
    {
        var result = await _service.FindFuzzyBuildingCandidatesAsync(
            "", "KL", "50000", _companyId);

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task FindFuzzyBuildingCandidatesAsync_ReturnsCandidatesAboveMinScore()
    {
        await CreateTestBuildingAsync("Royce Residences", city: "Kuala Lumpur", postcode: "50450");
        await CreateTestBuildingAsync("Royce Residence Tower", city: "Kuala Lumpur", postcode: "50450");

        var result = await _service.FindFuzzyBuildingCandidatesAsync(
            "ROYCE RESIDENCE",
            parsedCity: "Kuala Lumpur",
            parsedPostcode: "50450",
            _companyId,
            minScore: 0.7,
            maxResults: 5);

        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
        result.Should().OnlyContain(c => c.SimilarityScore >= 0.7);
        result.Should().OnlyContain(c => c.Building != null);
    }

    [Fact]
    public async Task FindFuzzyBuildingCandidatesAsync_RespectsMaxResults()
    {
        await CreateTestBuildingAsync("Royce Residences", city: "KL", postcode: "50450");
        await CreateTestBuildingAsync("Royce Residence Tower", city: "KL", postcode: "50450");
        await CreateTestBuildingAsync("Royce Residency", city: "KL", postcode: "50450");

        var result = await _service.FindFuzzyBuildingCandidatesAsync(
            "Royce",
            parsedCity: "KL",
            parsedPostcode: "50450",
            _companyId,
            minScore: 0.5,
            maxResults: 2);

        result.Should().NotBeNull();
        result.Should().HaveCountLessOrEqualTo(2);
    }

    [Fact]
    public async Task FindFuzzyBuildingCandidatesAsync_NoCandidates_ReturnsEmpty()
    {
        await CreateTestBuildingAsync("Completely Different Name", city: "KL", postcode: "50000");

        var result = await _service.FindFuzzyBuildingCandidatesAsync(
            "Royce Residences",
            parsedCity: "KL",
            parsedPostcode: "50000",
            _companyId,
            minScore: 0.95);

        result.Should().NotBeNull().And.BeEmpty();
    }

    #endregion

    #region Helpers

    private async Task<Building> CreateTestBuildingAsync(
        string name,
        string? city = "Kuala Lumpur",
        string? postcode = "50000",
        string? code = null,
        bool isDeleted = false)
    {
        var building = new Building
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Name = name,
            Code = code ?? $"BLD-{Guid.NewGuid():N}".Substring(0, 12),
            City = city ?? string.Empty,
            Postcode = postcode ?? string.Empty,
            State = "Selangor",
            AddressLine1 = "123 Test St",
            IsActive = true,
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Buildings.Add(building);
        await _dbContext.SaveChangesAsync();
        return building;
    }

    #endregion

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _dbContext?.Dispose();
    }
}

using CephasOps.Application.Buildings.DTOs;
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
/// Unit tests for BuildingService. Tenant-scoped entities require TenantScope (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class BuildingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<BuildingService>> _mockLogger;
    private readonly BuildingService _service;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public BuildingServiceTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<BuildingService>>();
        _service = new BuildingService(_dbContext, _mockLogger.Object);
    }

    #region GetBuildings Tests

    [Fact]
    public async Task GetBuildingsAsync_NoFilters_ReturnsAllBuildings()
    {
        // Arrange
        await CreateTestBuildingAsync("Building A", "MDU");
        await CreateTestBuildingAsync("Building B", "SDU");
        await CreateTestBuildingAsync("Building C", "MDU", companyId: Guid.NewGuid()); // Different company

        // Act
        var result = await _service.GetBuildingsAsync(_companyId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(b => b.Name.StartsWith("Building"));
    }

    [Fact]
    public async Task GetBuildingsAsync_WithPropertyTypeFilter_ReturnsFilteredBuildings()
    {
        // Arrange
        await CreateTestBuildingAsync("Building A", "MDU");
        await CreateTestBuildingAsync("Building B", "SDU");
        await CreateTestBuildingAsync("Building C", "MDU");

        // Act
        var result = await _service.GetBuildingsAsync(_companyId, propertyType: "MDU");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(b => b.PropertyType == "MDU");
    }

    [Fact]
    public async Task GetBuildingsAsync_WithIsActiveFilter_ReturnsFilteredBuildings()
    {
        // Arrange
        await CreateTestBuildingAsync("Building A", "MDU", isActive: true);
        await CreateTestBuildingAsync("Building B", "SDU", isActive: false);
        await CreateTestBuildingAsync("Building C", "MDU", isActive: true);

        // Act
        var result = await _service.GetBuildingsAsync(_companyId, isActive: true);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(b => b.IsActive == true);
    }

    #endregion

    #region GetBuildingById Tests

    [Fact]
    public async Task GetBuildingByIdAsync_ValidId_ReturnsBuilding()
    {
        // Arrange
        var building = await CreateTestBuildingAsync("Test Building", "MDU");

        // Act
        var result = await _service.GetBuildingByIdAsync(building.Id, _companyId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(building.Id);
        result.Name.Should().Be("Test Building");
    }

    [Fact]
    public async Task GetBuildingByIdAsync_InvalidId_ReturnsNull()
    {
        // Act
        var result = await _service.GetBuildingByIdAsync(Guid.NewGuid(), _companyId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBuildingByIdAsync_DifferentCompany_ReturnsNull()
    {
        // Arrange
        var otherCompanyId = Guid.NewGuid();
        var building = await CreateTestBuildingAsync("Test Building", "MDU", companyId: otherCompanyId);

        // Act
        var result = await _service.GetBuildingByIdAsync(building.Id, _companyId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private async Task<Building> CreateTestBuildingAsync(
        string name,
        string propertyType,
        Guid? companyId = null,
        bool isActive = true)
    {
        var building = new Building
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId ?? _companyId,
            Name = name,
            Code = $"BLD-{Guid.NewGuid():N}",
            PropertyType = propertyType,
            AddressLine1 = "123 Test Street",
            City = "Kuala Lumpur",
            State = "Selangor",
            Postcode = "50000",
            IsActive = isActive,
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


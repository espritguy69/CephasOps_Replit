using CephasOps.Application.Departments.DTOs;
using CephasOps.Application.Departments.Services;
using CephasOps.Domain.Departments.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Departments;

/// <summary>
/// Unit tests for DepartmentService. Tenant-scoped entities require TenantScope (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class DepartmentServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<DepartmentService>> _mockLogger;
    private readonly Mock<IDepartmentAccessService> _mockAccessService;
    private readonly DepartmentService _service;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public DepartmentServiceTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<DepartmentService>>();
        _mockAccessService = new Mock<IDepartmentAccessService>();
        
        // Setup default access (global access)
        _mockAccessService
            .Setup(x => x.GetAccessAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(DepartmentAccessResult.Global);
        
        _service = new DepartmentService(_dbContext, _mockLogger.Object, _mockAccessService.Object);
    }

    #region GetDepartments Tests

    [Fact]
    public async Task GetDepartmentsAsync_NoFilters_ReturnsAllDepartments()
    {
        // Arrange
        await CreateTestDepartmentAsync("GPON");
        await CreateTestDepartmentAsync("NWO");
        await CreateTestDepartmentAsync("CWO", companyId: Guid.NewGuid()); // Different company

        // Act
        var result = await _service.GetDepartmentsAsync(_companyId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(d => d.CompanyId == _companyId);
    }

    [Fact]
    public async Task GetDepartmentsAsync_WithIsActiveFilter_ReturnsFilteredDepartments()
    {
        // Arrange
        await CreateTestDepartmentAsync("GPON", isActive: true);
        await CreateTestDepartmentAsync("NWO", isActive: false);
        await CreateTestDepartmentAsync("CWO", isActive: true);

        // Act
        var result = await _service.GetDepartmentsAsync(_companyId, isActive: true);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(d => d.IsActive == true);
    }

    #endregion

    #region GetDepartmentById Tests

    [Fact]
    public async Task GetDepartmentByIdAsync_ValidId_ReturnsDepartment()
    {
        // Arrange
        var department = await CreateTestDepartmentAsync("GPON");

        // Act
        var result = await _service.GetDepartmentByIdAsync(department.Id, _companyId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(department.Id);
        result.Name.Should().Be("GPON");
    }

    [Fact]
    public async Task GetDepartmentByIdAsync_InvalidId_ReturnsNull()
    {
        // Act
        var result = await _service.GetDepartmentByIdAsync(Guid.NewGuid(), _companyId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateDepartment Tests

    [Fact]
    public async Task CreateDepartmentAsync_ValidDepartment_CreatesSuccessfully()
    {
        // Arrange
        var dto = new CreateDepartmentDto
        {
            Name = "Test Department",
            Code = "TEST",
            Description = "Test Description"
        };

        // Act
        var result = await _service.CreateDepartmentAsync(dto, _companyId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Test Department");
        result.Code.Should().Be("TEST");
        result.IsActive.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private async Task<Department> CreateTestDepartmentAsync(
        string name,
        Guid? companyId = null,
        bool isActive = true)
    {
        var department = new Department
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId ?? _companyId,
            Name = name,
            Code = name.Substring(0, Math.Min(4, name.Length)).ToUpper(),
            Description = $"Test {name} Department",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Departments.Add(department);
        await _dbContext.SaveChangesAsync();
        return department;
    }

    #endregion

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _dbContext?.Dispose();
    }
}


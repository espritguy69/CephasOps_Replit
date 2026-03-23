using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Domain.Inventory.Entities;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Settings;

/// <summary>
/// Unit tests for MaterialTemplateService CRUD operations. Tenant-scoped (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class MaterialTemplateServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<MaterialTemplateService>> _mockLogger;
    private readonly MaterialTemplateService _service;
    private readonly Guid _companyId;
    private readonly Guid _userId;
    private readonly Guid? _previousTenantId;

    public MaterialTemplateServiceTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        _userId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<MaterialTemplateService>>();
        _service = new MaterialTemplateService(_dbContext, _mockLogger.Object);
    }

    #region Create Tests

    [Fact]
    public async Task CreateTemplateAsync_ValidTemplate_CreatesSuccessfully()
    {
        // Arrange
        var material = await CreateTestMaterialAsync();
        var dto = new CreateMaterialTemplateDto
        {
            Name = "Test Template",
            OrderType = "Activation",
            Items = new List<CreateMaterialTemplateItemDto>
            {
                new() { MaterialId = material.Id, Quantity = 10, Notes = "Test item" }
            }
        };

        // Act
        var result = await _service.CreateTemplateAsync(dto, _companyId, _userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Test Template");
        result.OrderType.Should().Be("Activation");
        result.IsActive.Should().BeTrue();
        result.Items.Should().HaveCount(1);
        result.Items[0].MaterialId.Should().Be(material.Id);
        result.Items[0].Quantity.Should().Be(10);
    }

    [Fact]
    public async Task CreateTemplateAsync_WithDefaultFlag_UnsetsOtherDefaults()
    {
        // Arrange
        var material = await CreateTestMaterialAsync();
        var existingTemplate = await CreateTestTemplateAsync("Activation", isDefault: true);
        
        var dto = new CreateMaterialTemplateDto
        {
            Name = "New Default Template",
            OrderType = "Activation",
            IsDefault = true,
            Items = new List<CreateMaterialTemplateItemDto>
            {
                new() { MaterialId = material.Id, Quantity = 5 }
            }
        };

        // Act
        var result = await _service.CreateTemplateAsync(dto, _companyId, _userId);

        // Assert
        result.IsDefault.Should().BeTrue();
        
        var existingTemplateAfter = await _dbContext.MaterialTemplates.FindAsync(existingTemplate.Id);
        existingTemplateAfter!.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task CreateTemplateAsync_InvalidMaterialId_ThrowsException()
    {
        // Arrange
        var dto = new CreateMaterialTemplateDto
        {
            Name = "Test Template",
            OrderType = "Activation",
            Items = new List<CreateMaterialTemplateItemDto>
            {
                new() { MaterialId = Guid.NewGuid(), Quantity = 10 }
            }
        };

        // Act
        var act = async () => await _service.CreateTemplateAsync(dto, _companyId, _userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Material*not found*");
    }

    [Fact]
    public async Task CreateTemplateAsync_MaterialFromDifferentCompany_ThrowsException()
    {
        // Arrange
        var otherCompanyId = Guid.NewGuid();
        var material = await CreateTestMaterialAsync(companyId: otherCompanyId);
        var dto = new CreateMaterialTemplateDto
        {
            Name = "Test Template",
            OrderType = "Activation",
            Items = new List<CreateMaterialTemplateItemDto>
            {
                new() { MaterialId = material.Id, Quantity = 10 }
            }
        };

        // Act
        var act = async () => await _service.CreateTemplateAsync(dto, _companyId, _userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*doesn't belong to company*");
    }

    #endregion

    #region Read Tests

    [Fact]
    public async Task GetTemplatesAsync_NoFilters_ReturnsAllTemplates()
    {
        // Arrange
        await CreateTestTemplateAsync("Activation");
        await CreateTestTemplateAsync("Assurance");
        await CreateTestTemplateAsync("Modification", companyId: Guid.NewGuid()); // Different company

        // Act
        var result = await _service.GetTemplatesAsync(_companyId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.CompanyId == _companyId);
    }

    [Fact]
    public async Task GetTemplatesAsync_WithOrderTypeFilter_ReturnsFilteredTemplates()
    {
        // Arrange
        await CreateTestTemplateAsync("Activation");
        await CreateTestTemplateAsync("Assurance");
        await CreateTestTemplateAsync("Activation");

        // Act
        var result = await _service.GetTemplatesAsync(_companyId, orderType: "Activation");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.OrderType == "Activation");
    }

    [Fact]
    public async Task GetTemplatesAsync_WithIsActiveFilter_ReturnsFilteredTemplates()
    {
        // Arrange
        await CreateTestTemplateAsync("Activation", isActive: true);
        await CreateTestTemplateAsync("Assurance", isActive: false);
        await CreateTestTemplateAsync("Modification", isActive: true);

        // Act
        var result = await _service.GetTemplatesAsync(_companyId, isActive: true);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.IsActive == true);
    }

    [Fact]
    public async Task GetTemplateByIdAsync_ValidId_ReturnsTemplate()
    {
        // Arrange
        var template = await CreateTestTemplateAsync("Activation");

        // Act
        var result = await _service.GetTemplateByIdAsync(template.Id, _companyId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(template.Id);
        result.Name.Should().Be(template.Name);
    }

    [Fact]
    public async Task GetTemplateByIdAsync_InvalidId_ReturnsNull()
    {
        // Act
        var result = await _service.GetTemplateByIdAsync(Guid.NewGuid(), _companyId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTemplateByIdAsync_DifferentCompany_ReturnsNull()
    {
        // Arrange
        var otherCompanyId = Guid.NewGuid();
        var template = await CreateTestTemplateAsync("Activation", companyId: otherCompanyId);

        // Act
        var result = await _service.GetTemplateByIdAsync(template.Id, _companyId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetEffectiveTemplateAsync_PartnerSpecific_ReturnsPartnerTemplate()
    {
        // Arrange
        var partnerId = Guid.NewGuid();
        var material = await CreateTestMaterialAsync();
        
        // Create partner-specific template
        var partnerTemplate = await CreateTestTemplateAsync(
            "Activation", 
            partnerId: partnerId,
            items: new List<(Guid MaterialId, decimal Quantity)>
            {
                (material.Id, 5)
            });
        
        // Create default template
        await CreateTestTemplateAsync(
            "Activation",
            isDefault: true,
            items: new List<(Guid MaterialId, decimal Quantity)>
            {
                (material.Id, 10)
            });

        // Act
        var result = await _service.GetEffectiveTemplateAsync(_companyId, partnerId, "Activation", null, null);

        // Assert
        result.Should().NotBeNull();
        result!.PartnerId.Should().Be(partnerId);
    }

    [Fact]
    public async Task GetEffectiveTemplateAsync_NoPartnerSpecific_FallsBackToDefault()
    {
        // Arrange
        var material = await CreateTestMaterialAsync();
        var defaultTemplate = await CreateTestTemplateAsync(
            "Activation",
            isDefault: true,
            items: new List<(Guid MaterialId, decimal Quantity)>
            {
                (material.Id, 10)
            });

        // Act
        var result = await _service.GetEffectiveTemplateAsync(_companyId, null, "Activation", null, null);

        // Assert
        result.Should().NotBeNull();
        result!.IsDefault.Should().BeTrue();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task UpdateTemplateAsync_ValidUpdate_UpdatesSuccessfully()
    {
        // Arrange
        var template = await CreateTestTemplateAsync("Activation");
        var dto = new UpdateMaterialTemplateDto
        {
            Name = "Updated Name",
            IsActive = false
        };

        // Act
        var result = await _service.UpdateTemplateAsync(template.Id, dto, _companyId, _userId);

        // Assert
        result.Name.Should().Be("Updated Name");
        result.IsActive.Should().BeFalse();
    }

    [Fact(Skip = "InMemory provider concurrency behavior differs from PostgreSQL; replace/delete items test needs integration test.")]
    public async Task UpdateTemplateAsync_UpdateItems_ReplacesAllItems()
    {
        // Arrange
        var material1 = await CreateTestMaterialAsync();
        var material2 = await CreateTestMaterialAsync();
        var template = await CreateTestTemplateAsync(
            "Activation",
            items: new List<(Guid MaterialId, decimal Quantity)>
            {
                (material1.Id, 10)
            });

        // Verify items are saved
        var itemCount = await _dbContext.MaterialTemplateItems
            .CountAsync(i => i.MaterialTemplateId == template.Id);
        itemCount.Should().Be(1);

        var dto = new UpdateMaterialTemplateDto
        {
            Items = new List<CreateMaterialTemplateItemDto>
            {
                new() { MaterialId = material2.Id, Quantity = 20 }
            }
        };

        // Act
        var result = await _service.UpdateTemplateAsync(template.Id, dto, _companyId, _userId);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].MaterialId.Should().Be(material2.Id);
        result.Items[0].Quantity.Should().Be(20);
        
        // Verify in database - reload to ensure changes are persisted
        _dbContext.ChangeTracker.Clear(); // Clear change tracker to force reload
        var templateAfter = await _dbContext.MaterialTemplates
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == template.Id);
        templateAfter!.Items.Should().HaveCount(1);
        templateAfter.Items[0].MaterialId.Should().Be(material2.Id);
    }

    [Fact]
    public async Task UpdateTemplateAsync_SetAsDefault_UnsetsOtherDefaults()
    {
        // Arrange
        var existingDefault = await CreateTestTemplateAsync("Activation", isDefault: true);
        var template = await CreateTestTemplateAsync("Activation", isDefault: false);
        
        var dto = new UpdateMaterialTemplateDto
        {
            IsDefault = true
        };

        // Act
        var result = await _service.UpdateTemplateAsync(template.Id, dto, _companyId, _userId);

        // Assert
        result.IsDefault.Should().BeTrue();
        
        var existingDefaultAfter = await _dbContext.MaterialTemplates.FindAsync(existingDefault.Id);
        existingDefaultAfter!.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTemplateAsync_InvalidId_ThrowsException()
    {
        // Arrange
        var dto = new UpdateMaterialTemplateDto { Name = "Updated" };

        // Act
        var act = async () => await _service.UpdateTemplateAsync(Guid.NewGuid(), dto, _companyId, _userId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteTemplateAsync_ValidId_DeletesSuccessfully()
    {
        // Arrange
        var template = await CreateTestTemplateAsync("Activation");

        // Act
        await _service.DeleteTemplateAsync(template.Id, _companyId);

        // Assert
        var deleted = await _dbContext.MaterialTemplates.FindAsync(template.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTemplateAsync_InvalidId_ThrowsException()
    {
        // Act
        var act = async () => await _service.DeleteTemplateAsync(Guid.NewGuid(), _companyId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeleteTemplateAsync_DifferentCompany_ThrowsException()
    {
        // Arrange
        var otherCompanyId = Guid.NewGuid();
        var template = await CreateTestTemplateAsync("Activation", companyId: otherCompanyId);

        // Act
        var act = async () => await _service.DeleteTemplateAsync(template.Id, _companyId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region SetAsDefault Tests

    [Fact]
    public async Task SetAsDefaultAsync_ValidId_SetsAsDefault()
    {
        // Arrange
        var template = await CreateTestTemplateAsync("Activation", isDefault: false);

        // Act
        var result = await _service.SetAsDefaultAsync(template.Id, _companyId, _userId);

        // Assert
        result.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task SetAsDefaultAsync_UnsetsOtherDefaults()
    {
        // Arrange
        var existingDefault = await CreateTestTemplateAsync("Activation", isDefault: true);
        var template = await CreateTestTemplateAsync("Activation", isDefault: false);

        // Act
        await _service.SetAsDefaultAsync(template.Id, _companyId, _userId);

        // Assert
        var existingDefaultAfter = await _dbContext.MaterialTemplates.FindAsync(existingDefault.Id);
        existingDefaultAfter!.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task SetAsDefaultAsync_InvalidId_ThrowsException()
    {
        // Act
        var act = async () => await _service.SetAsDefaultAsync(Guid.NewGuid(), _companyId, _userId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region Helper Methods

    private async Task<Material> CreateTestMaterialAsync(Guid? companyId = null)
    {
        var material = new Material
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId ?? _companyId,
            ItemCode = $"MAT-{Guid.NewGuid():N}",
            Description = "Test Material",
            UnitOfMeasure = "PCS",
            IsSerialised = false,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Materials.Add(material);
        await _dbContext.SaveChangesAsync();
        return material;
    }

    private async Task<MaterialTemplate> CreateTestTemplateAsync(
        string orderType,
        Guid? companyId = null,
        Guid? partnerId = null,
        Guid? buildingTypeId = null,
        bool isDefault = false,
        bool isActive = true,
        List<(Guid MaterialId, decimal Quantity)>? items = null)
    {
        var material = items == null || items.Count == 0 
            ? await CreateTestMaterialAsync(companyId ?? _companyId)
            : null;

        var template = new MaterialTemplate
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId ?? _companyId,
            Name = $"Test Template {orderType}",
            OrderType = orderType,
            PartnerId = partnerId,
            BuildingTypeId = buildingTypeId,
            IsDefault = isDefault,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = _userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedByUserId = _userId
        };

        if (items != null && items.Count > 0)
        {
            foreach (var (materialId, quantity) in items)
            {
                var mat = await _dbContext.Materials.FindAsync(materialId);
                template.Items.Add(new MaterialTemplateItem
                {
                    Id = Guid.NewGuid(),
                    MaterialTemplateId = template.Id,
                    MaterialId = materialId,
                    Quantity = quantity,
                    UnitOfMeasure = mat?.UnitOfMeasure ?? "PCS",
                    IsSerialised = mat?.IsSerialised ?? false,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        else if (material != null)
        {
            template.Items.Add(new MaterialTemplateItem
            {
                Id = Guid.NewGuid(),
                MaterialTemplateId = template.Id,
                MaterialId = material.Id,
                Quantity = 1,
                UnitOfMeasure = material.UnitOfMeasure,
                IsSerialised = material.IsSerialised,
                CreatedAt = DateTime.UtcNow
            });
        }

        _dbContext.MaterialTemplates.Add(template);
        await _dbContext.SaveChangesAsync();
        return template;
    }

    #endregion

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _dbContext?.Dispose();
    }
}


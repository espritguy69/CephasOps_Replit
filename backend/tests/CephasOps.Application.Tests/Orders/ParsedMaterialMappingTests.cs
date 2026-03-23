using CephasOps.Application.Buildings.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Common.Services;
using CephasOps.Application.Inventory.Services;
using CephasOps.Application.Notifications.Services;
using CephasOps.Application.Orders.Services;
using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Utilities;
using CephasOps.Application.Rates.Services;
using CephasOps.Application.Settings.Services;
using CephasOps.Application.Workflow.Services;
using CephasOps.Domain.Buildings.Entities;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Departments.Entities;
using CephasOps.Domain.Inventory.Entities;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Orders;

/// <summary>
/// Targeted tests for parsed-material-to-order mapping (CreateFromParsedDraftAsync + ApplyParsedMaterialsAsync).
/// Verifies ItemCode/Description resolution, no duplicate rows, quantity/notes, and that unmatched materials do not break order creation. Tenant-scoped (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class ParsedMaterialMappingTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly OrderService _orderService;
    private readonly Guid _companyId;
    private readonly Guid _partnerId;
    private readonly Guid _buildingId;
    private readonly Guid _orderTypeId;
    private readonly Guid _orderCategoryId;
    private readonly Guid _materialId;
    private readonly Guid? _previousTenantId;

    public ParsedMaterialMappingTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        _partnerId = Guid.NewGuid();
        _buildingId = Guid.NewGuid();
        _orderTypeId = Guid.NewGuid();
        _orderCategoryId = Guid.NewGuid();
        _materialId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"ParsedMaterial_{Guid.NewGuid()}")
            .Options;
        _context = new ApplicationDbContext(options);
        SeedData();
        _orderService = CreateOrderService();
    }

    private void SeedData()
    {
        var deptId = Guid.NewGuid();
        _context.Departments.Add(new Department
        {
            Id = deptId,
            CompanyId = _companyId,
            Name = "Test Dept",
            Code = "D1"
        });
        _context.Partners.Add(new Partner
        {
            Id = _partnerId,
            CompanyId = _companyId,
            Name = "Test Partner",
            Code = "P1",
            IsActive = true
        });
        _context.OrderTypes.Add(new OrderType
        {
            Id = _orderTypeId,
            Name = "FTTH",
            Code = "FTTH",
            IsActive = true
        });
        _context.OrderCategories.Add(new OrderCategory
        {
            Id = _orderCategoryId,
            CompanyId = _companyId,
            Name = "FTTH",
            Code = "FTTH",
            IsActive = true
        });
        _context.Buildings.Add(new Building
        {
            Id = _buildingId,
            CompanyId = _companyId,
            DepartmentId = deptId,
            Name = "Test Building",
            AddressLine1 = "1 Test St",
            City = "City",
            State = "State",
            Postcode = "50000"
        });
        _context.Materials.Add(new Material
        {
            Id = _materialId,
            CompanyId = _companyId,
            ItemCode = "ONU",
            Description = "ONU",
            UnitOfMeasure = "pcs",
            DefaultCost = 10m
        });
        _context.SaveChanges();
    }

    private OrderService CreateOrderService()
    {
        var logger = new Mock<ILogger<OrderService>>();
        var buildingService = new Mock<IBuildingService>();
        var blockerValidation = new Mock<CephasOps.Application.Orders.Services.IBlockerValidationService>();
        var workflowEngine = new Mock<IWorkflowEngineService>();
        var workflowDefinitions = new Mock<IWorkflowDefinitionsService>();
        var slaProfile = new Mock<ISlaProfileService>();
        var automationRule = new Mock<IAutomationRuleService>();
        var businessHours = new Mock<IBusinessHoursService>();
        var escalationRule = new Mock<IEscalationRuleService>();
        var approvalWorkflow = new Mock<IApprovalWorkflowService>();
        var orderTypeService = new Mock<IOrderTypeService>();
        var notificationService = new Mock<INotificationService>();
        var encryptionService = new Mock<CephasOps.Domain.Common.Services.IEncryptionService>();
        var materialTemplateService = new Mock<IMaterialTemplateService>();
        materialTemplateService
            .Setup(x => x.GetEffectiveTemplateAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CephasOps.Application.Settings.DTOs.MaterialTemplateDto?)null);
        var inventoryService = new Mock<IInventoryService>();
        var effectiveScopeResolver = new Mock<IEffectiveScopeResolver>();
        effectiveScopeResolver.Setup(x => x.GetOrderTypeCodeForScopeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        effectiveScopeResolver.Setup(x => x.ResolveFromEntityAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((CephasOps.Application.Common.DTOs.EffectiveOrderScope?)null);
        var orderPayoutSnapshot = new Mock<IOrderPayoutSnapshotService>();
        orderPayoutSnapshot.Setup(x => x.CreateSnapshotForOrderIfEligibleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
        orderPayoutSnapshot.Setup(x => x.GetSnapshotByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((CephasOps.Application.Rates.DTOs.OrderPayoutSnapshotDto?)null);
        orderPayoutSnapshot.Setup(x => x.GetPayoutWithSnapshotOrLiveAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>())).ReturnsAsync(new CephasOps.Application.Rates.DTOs.OrderPayoutSnapshotResponseDto());

        return new OrderService(
            _context,
            logger.Object,
            buildingService.Object,
            blockerValidation.Object,
            workflowEngine.Object,
            workflowDefinitions.Object,
            slaProfile.Object,
            automationRule.Object,
            businessHours.Object,
            escalationRule.Object,
            approvalWorkflow.Object,
            orderTypeService.Object,
            notificationService.Object,
            encryptionService.Object,
            materialTemplateService.Object,
            inventoryService.Object,
            effectiveScopeResolver.Object,
            orderPayoutSnapshot.Object);
    }

    private static CreateOrderFromDraftDto ValidDraft(Guid companyId, Guid partnerId, Guid buildingId, Guid orderCategoryId, List<ParsedDraftMaterialDto>? materials = null)
    {
        return new CreateOrderFromDraftDto
        {
            ParsedOrderDraftId = Guid.NewGuid(),
            CompanyId = companyId,
            PartnerId = partnerId,
            BuildingId = buildingId,
            OrderCategoryId = orderCategoryId,
            OrderTypeHint = "FTTH",
            ServiceId = "TBBN_TEST_001",
            CustomerName = "Test Customer",
            CustomerPhone = "0123456789",
            AddressText = "1 Test Street",
            AppointmentDate = DateTime.UtcNow.Date.AddDays(1),
            AppointmentWindow = "09:00-12:00",
            Materials = materials
        };
    }

    [Fact]
    public async Task CreateFromParsedDraft_WithItemCodeMatch_CreatesOrderMaterialUsage()
    {
        var dto = ValidDraft(_companyId, _partnerId, _buildingId, _orderCategoryId, new List<ParsedDraftMaterialDto>
        {
            new() { Id = Guid.NewGuid(), Name = "ONU", Quantity = 2, UnitOfMeasure = "pcs", Notes = "Parsed note" }
        });
        var userId = Guid.NewGuid();

        var result = await _orderService.CreateFromParsedDraftAsync(dto, userId, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.OrderId.Should().NotBeNull();
        var usages = await _context.OrderMaterialUsage.Where(u => u.OrderId == result.OrderId!.Value).ToListAsync();
        usages.Should().ContainSingle();
        usages[0].MaterialId.Should().Be(_materialId);
        usages[0].Quantity.Should().Be(2);
        usages[0].Notes.Should().Contain("From parser");
        usages[0].Notes.Should().Contain("Unit: pcs");
        usages[0].Notes.Should().Contain("Parsed note");
    }

    [Fact]
    public async Task CreateFromParsedDraft_WithDescriptionMatch_CreatesOrderMaterialUsage()
    {
        var materialByDesc = new Material
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            ItemCode = "OTHER",
            Description = "Fibre Cable",
            UnitOfMeasure = "m",
            DefaultCost = 5m
        };
        _context.Materials.Add(materialByDesc);
        await _context.SaveChangesAsync();

        var dto = ValidDraft(_companyId, _partnerId, _buildingId, _orderCategoryId, new List<ParsedDraftMaterialDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Fibre Cable", Quantity = 10 }
        });
        var userId = Guid.NewGuid();

        var result = await _orderService.CreateFromParsedDraftAsync(dto, userId, CancellationToken.None);

        result.Success.Should().BeTrue();
        var usages = await _context.OrderMaterialUsage.Where(u => u.OrderId == result.OrderId!.Value).ToListAsync();
        usages.Should().ContainSingle(u => u.MaterialId == materialByDesc.Id && u.Quantity == 10);
    }

    [Fact]
    public async Task CreateFromParsedDraft_AllUnmatched_OrderStillSucceeds()
    {
        var dto = ValidDraft(_companyId, _partnerId, _buildingId, _orderCategoryId, new List<ParsedDraftMaterialDto>
        {
            new() { Id = Guid.NewGuid(), Name = "NonExistentMaterialXYZ", Quantity = 1 }
        });
        var userId = Guid.NewGuid();

        var result = await _orderService.CreateFromParsedDraftAsync(dto, userId, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.OrderId.Should().NotBeNull();
        var usages = await _context.OrderMaterialUsage.Where(u => u.OrderId == result.OrderId!.Value).ToListAsync();
        usages.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateFromParsedDraft_MultipleLinesSameMaterial_NoDuplicateRows()
    {
        var dto = ValidDraft(_companyId, _partnerId, _buildingId, _orderCategoryId, new List<ParsedDraftMaterialDto>
        {
            new() { Id = Guid.NewGuid(), Name = "ONU", Quantity = 2 },
            new() { Id = Guid.NewGuid(), Name = "ONU", Quantity = 3 }
        });
        var userId = Guid.NewGuid();

        var result = await _orderService.CreateFromParsedDraftAsync(dto, userId, CancellationToken.None);

        result.Success.Should().BeTrue();
        var usages = await _context.OrderMaterialUsage.Where(u => u.OrderId == result.OrderId!.Value && u.MaterialId == _materialId).ToListAsync();
        usages.Should().ContainSingle();
        usages[0].Quantity.Should().Be(2);
    }

    [Fact]
    public async Task CreateFromParsedDraft_EmptyAndWhitespaceNames_SkippedSafely()
    {
        var dto = ValidDraft(_companyId, _partnerId, _buildingId, _orderCategoryId, new List<ParsedDraftMaterialDto>
        {
            new() { Id = Guid.NewGuid(), Name = "", Quantity = 1 },
            new() { Id = Guid.NewGuid(), Name = "   ", Quantity = 1 },
            new() { Id = Guid.NewGuid(), Name = "ONU", Quantity = 1 }
        });
        var userId = Guid.NewGuid();

        var result = await _orderService.CreateFromParsedDraftAsync(dto, userId, CancellationToken.None);

        result.Success.Should().BeTrue();
        var usages = await _context.OrderMaterialUsage.Where(u => u.OrderId == result.OrderId!.Value).ToListAsync();
        usages.Should().ContainSingle();
        usages[0].MaterialId.Should().Be(_materialId);
    }

    [Fact]
    public async Task CreateFromParsedDraft_QuantityNullOrZero_DefaultsToOne()
    {
        var dto = ValidDraft(_companyId, _partnerId, _buildingId, _orderCategoryId, new List<ParsedDraftMaterialDto>
        {
            new() { Id = Guid.NewGuid(), Name = "ONU", Quantity = null },
            new() { Id = Guid.NewGuid(), Name = "Fibre Cable", Quantity = 0 }
        });
        var materialByDesc = new Material
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            ItemCode = "OTHER2",
            Description = "Fibre Cable",
            UnitOfMeasure = "m",
            DefaultCost = 0m
        };
        _context.Materials.Add(materialByDesc);
        await _context.SaveChangesAsync();

        var userId = Guid.NewGuid();
        var result = await _orderService.CreateFromParsedDraftAsync(dto, userId, CancellationToken.None);

        result.Success.Should().BeTrue();
        var usages = await _context.OrderMaterialUsage.Where(u => u.OrderId == result.OrderId!.Value).ToListAsync();
        usages.Should().HaveCount(2);
        usages.Should().Contain(u => u.MaterialId == _materialId && u.Quantity == 1);
        usages.Should().Contain(u => u.MaterialId == materialByDesc.Id && u.Quantity == 1);
    }

    [Fact]
    public async Task CreateFromParsedDraft_WhenDefaultWouldAddSameMaterial_DoesNotDuplicate()
    {
        var buildingDefaultMaterial = new BuildingDefaultMaterial
        {
            Id = Guid.NewGuid(),
            BuildingId = _buildingId,
            OrderTypeId = _orderTypeId,
            MaterialId = _materialId,
            DefaultQuantity = 1,
            IsActive = true
        };
        _context.Set<BuildingDefaultMaterial>().Add(buildingDefaultMaterial);
        await _context.SaveChangesAsync();

        var dto = ValidDraft(_companyId, _partnerId, _buildingId, _orderCategoryId, new List<ParsedDraftMaterialDto>
        {
            new() { Id = Guid.NewGuid(), Name = "ONU", Quantity = 2 }
        });
        dto.ServiceId = "TBBN_DEFAULT_NO_DUP";
        var userId = Guid.NewGuid();

        var result = await _orderService.CreateFromParsedDraftAsync(dto, userId, CancellationToken.None);

        result.Success.Should().BeTrue();
        var usages = await _context.OrderMaterialUsage.Where(u => u.OrderId == result.OrderId!.Value && u.MaterialId == _materialId).ToListAsync();
        usages.Should().ContainSingle("parsed material should not duplicate row when default already added same MaterialId");
        usages[0].Quantity.Should().Be(1, "building default is applied first with DefaultQuantity 1; parsed is skipped to avoid duplicate");
    }

    [Fact]
    public async Task CreateFromParsedDraft_NullOrEmptyMaterials_DoesNotThrow()
    {
        var dtoNull = ValidDraft(_companyId, _partnerId, _buildingId, _orderCategoryId, null);
        dtoNull.ServiceId = "TBBN_NULL_MAT";
        var dtoEmpty = ValidDraft(_companyId, _partnerId, _buildingId, _orderCategoryId, new List<ParsedDraftMaterialDto>());
        dtoEmpty.ServiceId = "TBBN_EMPTY_MAT";
        var userId = Guid.NewGuid();

        var resultNull = await _orderService.CreateFromParsedDraftAsync(dtoNull, userId, CancellationToken.None);
        var resultEmpty = await _orderService.CreateFromParsedDraftAsync(dtoEmpty, userId, CancellationToken.None);

        resultNull.Success.Should().BeTrue();
        resultEmpty.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CreateFromParsedDraft_WithAliasMatch_ResolvesToMaterial()
    {
        const string parsedName = "Legacy ONU Plug";
        var normalized = MaterialNameNormalizer.Normalize(parsedName) ?? "";
        _context.Set<ParsedMaterialAlias>().Add(new ParsedMaterialAlias
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            AliasText = parsedName,
            NormalizedAliasText = normalized,
            MaterialId = _materialId,
            IsActive = true,
            Source = "ParserManualResolve"
        });
        await _context.SaveChangesAsync();

        var dto = ValidDraft(_companyId, _partnerId, _buildingId, _orderCategoryId, new List<ParsedDraftMaterialDto>
        {
            new() { Id = Guid.NewGuid(), Name = parsedName, Quantity = 1 }
        });
        var userId = Guid.NewGuid();

        var result = await _orderService.CreateFromParsedDraftAsync(dto, userId, CancellationToken.None);

        result.Success.Should().BeTrue();
        var usages = await _context.OrderMaterialUsage.Where(u => u.OrderId == result.OrderId!.Value).ToListAsync();
        usages.Should().ContainSingle();
        usages[0].MaterialId.Should().Be(_materialId);
        usages[0].Quantity.Should().Be(1);
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context?.Dispose();
    }
}

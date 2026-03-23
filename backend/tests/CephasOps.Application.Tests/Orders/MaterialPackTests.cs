using CephasOps.Application.Inventory.DTOs;
using CephasOps.Application.Inventory.Services;
using CephasOps.Application.Orders.DTOs;
using CephasOps.Application.Orders.Services;
using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Orders;

[Collection("TenantScopeTests")]
public class MaterialPackTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IMaterialTemplateService> _mockTemplateService;
    private readonly Mock<IStockLedgerService> _mockLedgerService;
    private readonly MaterialCollectionService _service;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public MaterialPackTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _mockTemplateService = new Mock<IMaterialTemplateService>();
        _mockLedgerService = new Mock<IStockLedgerService>();
        var mockLogger = new Mock<ILogger<MaterialCollectionService>>();
        _service = new MaterialCollectionService(
            _context,
            _mockTemplateService.Object,
            _mockLedgerService.Object,
            mockLogger.Object);
    }

    [Fact]
    public async Task GetMaterialPackAsync_WhenOrderExistsWithNoSi_ReturnsPackWithNoSiMessage()
    {
        var companyId = _companyId;
        var orderId = Guid.NewGuid();
        _context.Orders.Add(new Order
        {
            Id = orderId,
            CompanyId = companyId,
            ServiceId = "TBBN-1",
            Status = "Pending",
            AssignedSiId = null,
            PartnerId = Guid.NewGuid(),
            OrderTypeId = Guid.NewGuid(),
            BuildingId = Guid.NewGuid(),
            SourceSystem = "Manual",
            AddressLine1 = "A",
            City = "B",
            State = "C",
            Postcode = "D"
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetMaterialPackAsync(orderId, companyId);

        result.Should().NotBeNull();
        result.OrderId.Should().Be(orderId);
        result.ServiceInstallerId.Should().BeNull();
        result.RequiresCollection.Should().BeFalse();
        result.Message.Should().Contain("No SI assigned");
        result.RequiredMaterials.Should().BeEmpty();
        result.MissingMaterials.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMaterialPackAsync_WhenOrderNotFound_ThrowsKeyNotFoundException()
    {
        var orderId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.GetMaterialPackAsync(orderId, companyId));
    }

    [Fact]
    public async Task GetMaterialPackAsync_ReturnsRequiredAndMissingLists()
    {
        var companyId = _companyId;
        var orderId = Guid.NewGuid();
        var siId = Guid.NewGuid();
        _context.Orders.Add(new Order
        {
            Id = orderId,
            CompanyId = companyId,
            ServiceId = "TBBN-2",
            Status = "Assigned",
            AssignedSiId = siId,
            PartnerId = Guid.NewGuid(),
            OrderTypeId = Guid.NewGuid(),
            BuildingId = Guid.NewGuid(),
            SourceSystem = "Manual",
            AddressLine1 = "A",
            City = "B",
            State = "C",
            Postcode = "D"
        });
        await _context.SaveChangesAsync();
        _mockTemplateService
            .Setup(x => x.GetEffectiveTemplateAsync(companyId, It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MaterialTemplateDto?)null);
        _mockLedgerService
            .Setup(x => x.GetLedgerDerivedBalancesAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LedgerDerivedBalanceDto>());

        var result = await _service.GetMaterialPackAsync(orderId, companyId);

        result.Should().NotBeNull();
        result.OrderId.Should().Be(orderId);
        result.RequiredMaterials.Should().NotBeNull();
        result.MissingMaterials.Should().NotBeNull();
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
    }
}

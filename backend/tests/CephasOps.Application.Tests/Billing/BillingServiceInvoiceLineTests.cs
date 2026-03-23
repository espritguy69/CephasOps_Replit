using CephasOps.Application.Billing.Services;
using CephasOps.Application.Commands;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Billing;

/// <summary>
/// Tests for automatic invoice line resolution from orders using BillingRatecard. Tenant-scoped (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class BillingServiceInvoiceLineTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly BillingService _service;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public BillingServiceInvoiceLineTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        SeedData();
        _service = new BillingService(
            _context,
            Mock.Of<ILogger<BillingService>>(),
            new CommandProcessingLogStore(_context, Mock.Of<ILogger<CommandProcessingLogStore>>()));
    }

    private void SeedData()
    {
        _context.Companies.Add(new Company
        {
            Id = _companyId,
            ShortName = "Test",
            LegalName = "Test Co",
            IsActive = true
        });
        var partnerId = Guid.NewGuid();
        _context.Partners.Add(new Partner
        {
            Id = partnerId,
            CompanyId = _companyId,
            Name = "Partner A",
            PartnerType = "Telco",
            IsActive = true
        });
        var orderTypeId = Guid.NewGuid();
        _context.OrderTypes.Add(new OrderType
        {
            Id = orderTypeId,
            CompanyId = _companyId,
            Name = "Activation",
            Code = "ACT",
            IsActive = true
        });
        var orderCategoryId = Guid.NewGuid();
        _context.OrderCategories.Add(new OrderCategory
        {
            Id = orderCategoryId,
            CompanyId = _companyId,
            Name = "FTTH",
            Code = "FTTH",
            IsActive = true
        });
        var buildingId = Guid.NewGuid();
        _context.Buildings.Add(new Domain.Buildings.Entities.Building
        {
            Id = buildingId,
            CompanyId = _companyId,
            Name = "B1",
            IsActive = true
        });
        var orderId = Guid.NewGuid();
        _context.Orders.Add(new Order
        {
            Id = orderId,
            CompanyId = _companyId,
            PartnerId = partnerId,
            OrderTypeId = orderTypeId,
            OrderCategoryId = orderCategoryId,
            BuildingId = buildingId,
            ServiceId = "SVC001",
            Status = "Pending",
            SourceSystem = "Manual",
            AddressLine1 = "A1",
            City = "KL",
            State = "KL",
            Postcode = "50000"
        });
        _context.BillingRatecards.Add(new BillingRatecard
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            PartnerId = partnerId,
            OrderTypeId = orderTypeId,
            ServiceCategory = "FTTH",
            InstallationMethodId = null,
            Amount = 150m,
            IsActive = true
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task ResolveInvoiceLineFromOrderAsync_WhenOrderHasCategoryAndMatchingRatecard_ReturnsLine()
    {
        var orderId = _context.Orders.First(o => o.CompanyId == _companyId).Id;

        var result = await _service.ResolveInvoiceLineFromOrderAsync(orderId, _companyId);

        result.Should().NotBeNull();
        result!.OrderId.Should().Be(orderId);
        result.UnitPrice.Should().Be(150m);
        result.Quantity.Should().Be(1);
        result.Description.Should().NotBeNullOrEmpty();
        result.BillingRatecardId.Should().NotBeNull();
    }

    [Fact]
    public async Task ResolveInvoiceLineFromOrderAsync_WhenOrderHasNoOrderCategoryId_ReturnsNull()
    {
        var orderNoCategory = new Order
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            PartnerId = _context.Partners.First().Id,
            OrderTypeId = _context.OrderTypes.First().Id,
            OrderCategoryId = null,
            BuildingId = _context.Buildings.First().Id,
            ServiceId = "SVC002",
            Status = "Pending",
            SourceSystem = "Manual",
            AddressLine1 = "A2",
            City = "KL",
            State = "KL",
            Postcode = "50000"
        };
        _context.Orders.Add(orderNoCategory);
        await _context.SaveChangesAsync();

        var result = await _service.ResolveInvoiceLineFromOrderAsync(orderNoCategory.Id, _companyId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task BuildInvoiceLinesFromOrdersAsync_WithValidOrders_ReturnsLinesAndNoUnresolved()
    {
        var orderIds = await _context.Orders.Where(o => o.CompanyId == _companyId).Select(o => o.Id).ToListAsync();

        var result = await _service.BuildInvoiceLinesFromOrdersAsync(orderIds, _companyId);

        result.LineItems.Should().HaveCount(1);
        result.LineItems[0].UnitPrice.Should().Be(150m);
        result.LineItems[0].Quantity.Should().Be(1);
        result.LineItems[0].OrderId.Should().NotBeNull();
        result.UnresolvedOrderIds.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildInvoiceLinesFromOrdersAsync_WithMixedOrders_ReportsUnresolved()
    {
        var orderNoCategory = new Order
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            PartnerId = _context.Partners.First().Id,
            OrderTypeId = _context.OrderTypes.First().Id,
            OrderCategoryId = null,
            BuildingId = _context.Buildings.First().Id,
            ServiceId = "SVC002",
            Status = "Pending",
            SourceSystem = "Manual",
            AddressLine1 = "A2",
            City = "KL",
            State = "KL",
            Postcode = "50000"
        };
        _context.Orders.Add(orderNoCategory);
        await _context.SaveChangesAsync();
        var orderIds = new List<Guid> { _context.Orders.First(o => o.OrderCategoryId != null).Id, orderNoCategory.Id };

        var result = await _service.BuildInvoiceLinesFromOrdersAsync(orderIds, _companyId);

        result.LineItems.Should().HaveCount(1);
        result.UnresolvedOrderIds.Should().Contain(orderNoCategory.Id);
        result.Messages.Should().Contain(m => m.Contains(orderNoCategory.Id.ToString()));
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
    }
}

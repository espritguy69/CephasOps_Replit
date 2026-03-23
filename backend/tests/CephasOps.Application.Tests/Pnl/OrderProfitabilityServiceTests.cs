using System.Threading;
using CephasOps.Application.Billing.Services;
using CephasOps.Application.Commands;
using CephasOps.Application.Pnl.DTOs;
using CephasOps.Application.Pnl.Services;
using CephasOps.Application.Rates.Services;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Rates.Entities;
using CephasOps.Domain.ServiceInstallers.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Pnl;

/// <summary>
/// Tests for per-order profitability: revenue (BillingRatecard) and payout (RateEngineService).
/// </summary>
[Collection("TenantScopeTests")]
public class OrderProfitabilityServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly OrderProfitabilityService _service;
    private readonly Guid _companyId;
    private readonly Guid _partnerId;
    private readonly Guid _partnerGroupId;
    private readonly Guid _orderTypeId;
    private readonly Guid _orderCategoryId;
    private readonly Guid _orderId;
    private readonly Guid _siId;
    private readonly Guid? _previousTenantId;

    public OrderProfitabilityServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _companyId = Guid.NewGuid();
        _partnerGroupId = Guid.NewGuid();
        _partnerId = Guid.NewGuid();
        _orderTypeId = Guid.NewGuid();
        _orderCategoryId = Guid.NewGuid();
        _orderId = Guid.NewGuid();
        _siId = Guid.NewGuid();
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId;
        SeedBaseData();
        var billingService = new BillingService(
            _context,
            Mock.Of<ILogger<BillingService>>(),
            new CommandProcessingLogStore(_context, Mock.Of<ILogger<CommandProcessingLogStore>>()));
        var cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var rateEngine = new RateEngineService(_context, cache, Mock.Of<ILogger<RateEngineService>>());
        _service = new OrderProfitabilityService(_context, billingService, rateEngine, Mock.Of<ILogger<OrderProfitabilityService>>());
    }

    private void SeedBaseData()
    {
        _context.Companies.Add(new Company
        {
            Id = _companyId,
            ShortName = "Test",
            LegalName = "Test Co",
            IsActive = true
        });
        _context.PartnerGroups.Add(new PartnerGroup
        {
            Id = _partnerGroupId,
            CompanyId = _companyId,
            Name = "PG1"
        });
        _context.Partners.Add(new Partner
        {
            Id = _partnerId,
            CompanyId = _companyId,
            GroupId = _partnerGroupId,
            Name = "Partner A",
            PartnerType = "Telco",
            IsActive = true
        });
        _context.OrderTypes.Add(new OrderType
        {
            Id = _orderTypeId,
            CompanyId = _companyId,
            Name = "Activation",
            Code = "ACT",
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
        var buildingId = Guid.NewGuid();
        _context.Buildings.Add(new Domain.Buildings.Entities.Building
        {
            Id = buildingId,
            CompanyId = _companyId,
            Name = "B1",
            IsActive = true
        });
        _context.ServiceInstallers.Add(new ServiceInstaller
        {
            Id = _siId,
            CompanyId = _companyId,
            Name = "SI One",
            SiLevel = Domain.ServiceInstallers.Enums.InstallerLevel.Senior,
            IsActive = true
        });
        _context.Orders.Add(new Order
        {
            Id = _orderId,
            CompanyId = _companyId,
            PartnerId = _partnerId,
            OrderTypeId = _orderTypeId,
            OrderCategoryId = _orderCategoryId,
            BuildingId = buildingId,
            AssignedSiId = _siId,
            ServiceId = "SVC001",
            Status = "Completed",
            SourceSystem = "Manual",
            AddressLine1 = "A1",
            City = "KL",
            State = "KL",
            Postcode = "50000"
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task CalculateOrderProfitability_RevenueAndPayoutFound_ReturnsResolved()
    {
        _context.BillingRatecards.Add(new BillingRatecard
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            PartnerId = _partnerId,
            OrderTypeId = _orderTypeId,
            ServiceCategory = "FTTH",
            Amount = 200m,
            IsActive = true
        });
        _context.Set<GponSiJobRate>().Add(new GponSiJobRate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            OrderTypeId = _orderTypeId,
            OrderCategoryId = _orderCategoryId,
            SiLevel = "Senior",
            PartnerGroupId = _partnerGroupId,
            PayoutAmount = 80m,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var result = await WithTenantAsync(() => _service.CalculateOrderProfitabilityAsync(_orderId, _companyId));

        result.Should().NotBeNull();
        result!.Status.Should().Be(OrderProfitabilityStatus.Resolved);
        result.RevenueAmount.Should().Be(200m);
        result.PayoutAmount.Should().Be(80m);
        result.ProfitAmount.Should().Be(120m);
        result.MarginPercent.Should().Be(60m);
        result.RevenueSource.Should().Be("BillingRatecard");
        result.PayoutSource.Should().NotBeNullOrEmpty();
        result.MaterialCostAmount.Should().Be(0);
    }

    [Fact]
    public async Task CalculateOrderProfitability_RevenueFoundPayoutMissing_ReturnsPartial()
    {
        _context.BillingRatecards.Add(new BillingRatecard
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            PartnerId = _partnerId,
            OrderTypeId = _orderTypeId,
            ServiceCategory = "FTTH",
            Amount = 200m,
            IsActive = true
        });
        await _context.SaveChangesAsync();
        // No GponSiJobRate - payout will not resolve

        var result = await WithTenantAsync(() => _service.CalculateOrderProfitabilityAsync(_orderId, _companyId));

        result.Should().NotBeNull();
        result!.Status.Should().Be(OrderProfitabilityStatus.Partial);
        result.RevenueAmount.Should().Be(200m);
        result.PayoutAmount.Should().BeNull();
        result.ProfitAmount.Should().BeNull();
        result.ReasonCodes.Should().Contain(OrderProfitabilityReasonCodes.NoSiRateFound);
    }

    [Fact]
    public async Task CalculateOrderProfitability_RevenueMissingPayoutFound_ReturnsPartial()
    {
        _context.Set<GponSiJobRate>().Add(new GponSiJobRate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            OrderTypeId = _orderTypeId,
            OrderCategoryId = _orderCategoryId,
            SiLevel = "Senior",
            PartnerGroupId = _partnerGroupId,
            PayoutAmount = 80m,
            IsActive = true
        });
        await _context.SaveChangesAsync();
        // No BillingRatecard - revenue will not resolve

        var result = await WithTenantAsync(() => _service.CalculateOrderProfitabilityAsync(_orderId, _companyId));

        result.Should().NotBeNull();
        result!.Status.Should().Be(OrderProfitabilityStatus.Partial);
        result.RevenueAmount.Should().BeNull();
        result.PayoutAmount.Should().Be(80m);
        result.ProfitAmount.Should().BeNull();
        result.ReasonCodes.Should().Contain(OrderProfitabilityReasonCodes.NoBillingRatecardFound);
    }

    [Fact]
    public async Task CalculateOrderProfitability_BothMissing_ReturnsUnresolved()
    {
        // No BillingRatecard, no GponSiJobRate
        var result = await WithTenantAsync(() => _service.CalculateOrderProfitabilityAsync(_orderId, _companyId));

        result.Should().NotBeNull();
        result!.Status.Should().Be(OrderProfitabilityStatus.Unresolved);
        result.RevenueAmount.Should().BeNull();
        result.PayoutAmount.Should().BeNull();
        result.ProfitAmount.Should().BeNull();
    }

    [Fact]
    public async Task CalculateOrderProfitability_MarginCalculationCorrect()
    {
        _context.BillingRatecards.Add(new BillingRatecard
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            PartnerId = _partnerId,
            OrderTypeId = _orderTypeId,
            ServiceCategory = "FTTH",
            Amount = 100m,
            IsActive = true
        });
        _context.Set<GponSiJobRate>().Add(new GponSiJobRate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            OrderTypeId = _orderTypeId,
            OrderCategoryId = _orderCategoryId,
            SiLevel = "Senior",
            PartnerGroupId = _partnerGroupId,
            PayoutAmount = 25m,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var result = await WithTenantAsync(() => _service.CalculateOrderProfitabilityAsync(_orderId, _companyId));

        result!.ProfitAmount.Should().Be(75m);
        result.MarginPercent.Should().Be(75m);
    }

    [Fact]
    public async Task CalculateOrderProfitability_UsesBillingResolution_RevenueSourceBillingRatecard()
    {
        _context.BillingRatecards.Add(new BillingRatecard
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            PartnerId = _partnerId,
            OrderTypeId = _orderTypeId,
            ServiceCategory = "FTTH",
            Amount = 150m,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var result = await WithTenantAsync(() => _service.CalculateOrderProfitabilityAsync(_orderId, _companyId));

        result!.RevenueSource.Should().Be("BillingRatecard");
    }

    [Fact]
    public async Task CalculateOrderProfitability_UsesRateEngine_PayoutSourceSet()
    {
        _context.Set<GponSiJobRate>().Add(new GponSiJobRate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            OrderTypeId = _orderTypeId,
            OrderCategoryId = _orderCategoryId,
            SiLevel = "Senior",
            PartnerGroupId = _partnerGroupId,
            PayoutAmount = 60m,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var result = await WithTenantAsync(() => _service.CalculateOrderProfitabilityAsync(_orderId, _companyId));

        result!.PayoutSource.Should().NotBeNullOrEmpty();
        result.PayoutAmount.Should().Be(60m);
    }

    [Fact]
    public async Task CalculateOrderProfitability_OrderNotFound_ReturnsUnresolvedWithReason()
    {
        var result = await WithTenantAsync(() => _service.CalculateOrderProfitabilityAsync(Guid.NewGuid(), _companyId));

        result.Should().NotBeNull();
        result!.Status.Should().Be(OrderProfitabilityStatus.Unresolved);
        result.ReasonCodes.Should().Contain(OrderProfitabilityReasonCodes.OrderNotFound);
    }

    [Fact]
    public async Task CalculateOrderProfitability_WrongCompany_ReturnsUnresolvedWithOrderNotFound()
    {
        var wrongCompanyId = Guid.NewGuid();
        var result = await _service.CalculateOrderProfitabilityAsync(_orderId, wrongCompanyId);

        result.Should().NotBeNull();
        result!.Status.Should().Be(OrderProfitabilityStatus.Unresolved);
        result.ReasonCodes.Should().Contain(OrderProfitabilityReasonCodes.OrderNotFound);
    }

    [Fact]
    public async Task CalculateOrderProfitability_WhenCompanyIdEmpty_Throws()
    {
        var act = () => _service.CalculateOrderProfitabilityAsync(_orderId, Guid.Empty);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CalculateOrderProfitability*Company context is required*");
    }

    [Fact]
    public async Task CalculateOrdersProfitability_Bulk_ReturnsList()
    {
        _context.BillingRatecards.Add(new BillingRatecard
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            PartnerId = _partnerId,
            OrderTypeId = _orderTypeId,
            ServiceCategory = "FTTH",
            Amount = 100m,
            IsActive = true
        });
        _context.Set<GponSiJobRate>().Add(new GponSiJobRate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            OrderTypeId = _orderTypeId,
            OrderCategoryId = _orderCategoryId,
            SiLevel = "Senior",
            PartnerGroupId = _partnerGroupId,
            PayoutAmount = 40m,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var results = await WithTenantAsync(() => _service.CalculateOrdersProfitabilityAsync(new[] { _orderId }, _companyId));

        results.Should().HaveCount(1);
        results[0].OrderId.Should().Be(_orderId);
        results[0].Status.Should().Be(OrderProfitabilityStatus.Resolved);
    }

    /// <summary>
    /// Runs an async service call with tenant scope so query filters and guards see scope.
    /// Uses TenantPreservingSyncContext (with captured ExecutionContext) so continuations see tenant.
    /// </summary>
    private async Task<T> WithTenantAsync<T>(Func<Task<T>> act)
    {
        var prev = TenantScope.CurrentTenantId;
        var prevCtx = SynchronizationContext.Current;
        try
        {
            TenantScope.CurrentTenantId = _companyId;
            SynchronizationContext.SetSynchronizationContext(new TenantPreservingSyncContext(_companyId, null, prevCtx));
            return await act();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prevCtx);
            TenantScope.CurrentTenantId = prev;
        }
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
    }
}

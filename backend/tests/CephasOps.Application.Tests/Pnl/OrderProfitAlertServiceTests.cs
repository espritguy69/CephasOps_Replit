using CephasOps.Application.Billing.Services;
using CephasOps.Application.Commands;
using CephasOps.Application.Pnl;
using CephasOps.Application.Pnl.DTOs;
using CephasOps.Application.Pnl.Services;
using CephasOps.Application.Rates.Services;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Pnl.Entities;
using CephasOps.Domain.Rates.Entities;
using CephasOps.Domain.ServiceInstallers.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Pnl;

/// <summary>
/// Tests for order financial alerts derived from profitability. Tenant-scoped (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class OrderProfitAlertServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly OrderProfitabilityService _profitabilityService;
    private readonly OrderProfitAlertService _alertService;
    private readonly Guid _companyId;
    private readonly Guid _partnerId;
    private readonly Guid _partnerGroupId;
    private readonly Guid _orderTypeId;
    private readonly Guid _orderCategoryId;
    private readonly Guid _orderId;
    private readonly Guid _siId;
    private readonly Guid? _previousTenantId;

    public OrderProfitAlertServiceTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        _partnerGroupId = Guid.NewGuid();
        _partnerId = Guid.NewGuid();
        _orderTypeId = Guid.NewGuid();
        _orderCategoryId = Guid.NewGuid();
        _orderId = Guid.NewGuid();
        _siId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        SeedBaseData();
        var billingService = new BillingService(
            _context,
            Mock.Of<ILogger<BillingService>>(),
            new CommandProcessingLogStore(_context, Mock.Of<ILogger<CommandProcessingLogStore>>()));
        var cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var rateEngine = new RateEngineService(_context, cache, Mock.Of<ILogger<RateEngineService>>());
        _profitabilityService = new OrderProfitabilityService(_context, billingService, rateEngine, Mock.Of<ILogger<OrderProfitabilityService>>());
        _alertService = new OrderProfitAlertService(
            _profitabilityService,
            Options.Create(new ProfitabilityAlertsOptions { LowMarginThresholdPercent = 10 }),
            _context,
            Mock.Of<IOrderFinancialAlertNotifier>(),
            Mock.Of<ILogger<OrderProfitAlertService>>());
    }

    private void SeedBaseData()
    {
        _context.Companies.Add(new Company { Id = _companyId, ShortName = "Test", LegalName = "Test Co", IsActive = true });
        _context.PartnerGroups.Add(new PartnerGroup { Id = _partnerGroupId, CompanyId = _companyId, Name = "PG1" });
        _context.Partners.Add(new Partner { Id = _partnerId, CompanyId = _companyId, GroupId = _partnerGroupId, Name = "Partner A", PartnerType = "Telco", IsActive = true });
        _context.OrderTypes.Add(new OrderType { Id = _orderTypeId, CompanyId = _companyId, Name = "Activation", Code = "ACT", IsActive = true });
        _context.OrderCategories.Add(new OrderCategory { Id = _orderCategoryId, CompanyId = _companyId, Name = "FTTH", Code = "FTTH", IsActive = true });
        var buildingId = Guid.NewGuid();
        _context.Buildings.Add(new Domain.Buildings.Entities.Building { Id = buildingId, CompanyId = _companyId, Name = "B1", IsActive = true });
        _context.ServiceInstallers.Add(new ServiceInstaller { Id = _siId, CompanyId = _companyId, Name = "SI One", SiLevel = Domain.ServiceInstallers.Enums.InstallerLevel.Senior, IsActive = true });
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
    public async Task EvaluateOrderAlerts_NegativeProfit_ReturnsNegativeProfitAlert()
    {
        _context.BillingRatecards.Add(new BillingRatecard { Id = Guid.NewGuid(), CompanyId = _companyId, PartnerId = _partnerId, OrderTypeId = _orderTypeId, ServiceCategory = "FTTH", Amount = 50m, IsActive = true });
        _context.Set<GponSiJobRate>().Add(new GponSiJobRate { Id = Guid.NewGuid(), CompanyId = _companyId, OrderTypeId = _orderTypeId, OrderCategoryId = _orderCategoryId, SiLevel = "Senior", PartnerGroupId = _partnerGroupId, PayoutAmount = 80m, IsActive = true });
        await _context.SaveChangesAsync();

        var result = await _alertService.EvaluateOrderAlertsAsync(_orderId, _companyId);

        result.Alerts.Should().Contain(a => a.AlertCode == OrderFinancialAlertCodes.NegativeProfit && a.Severity == OrderFinancialAlertSeverity.Critical);
    }

    [Fact]
    public async Task EvaluateOrderAlerts_PayoutExceedsRevenue_ReturnsPayoutExceedsRevenueAlert()
    {
        _context.BillingRatecards.Add(new BillingRatecard { Id = Guid.NewGuid(), CompanyId = _companyId, PartnerId = _partnerId, OrderTypeId = _orderTypeId, ServiceCategory = "FTTH", Amount = 60m, IsActive = true });
        _context.Set<GponSiJobRate>().Add(new GponSiJobRate { Id = Guid.NewGuid(), CompanyId = _companyId, OrderTypeId = _orderTypeId, OrderCategoryId = _orderCategoryId, SiLevel = "Senior", PartnerGroupId = _partnerGroupId, PayoutAmount = 100m, IsActive = true });
        await _context.SaveChangesAsync();

        var result = await _alertService.EvaluateOrderAlertsAsync(_orderId, _companyId);

        result.Alerts.Should().Contain(a => a.AlertCode == OrderFinancialAlertCodes.PayoutExceedsRevenue && a.Severity == OrderFinancialAlertSeverity.Critical);
    }

    [Fact]
    public async Task EvaluateOrderAlerts_LowMargin_ReturnsLowMarginAlert()
    {
        _context.BillingRatecards.Add(new BillingRatecard { Id = Guid.NewGuid(), CompanyId = _companyId, PartnerId = _partnerId, OrderTypeId = _orderTypeId, ServiceCategory = "FTTH", Amount = 100m, IsActive = true });
        _context.Set<GponSiJobRate>().Add(new GponSiJobRate { Id = Guid.NewGuid(), CompanyId = _companyId, OrderTypeId = _orderTypeId, OrderCategoryId = _orderCategoryId, SiLevel = "Senior", PartnerGroupId = _partnerGroupId, PayoutAmount = 95m, IsActive = true });
        await _context.SaveChangesAsync();

        var result = await _alertService.EvaluateOrderAlertsAsync(_orderId, _companyId);

        result.Alerts.Should().Contain(a => a.AlertCode == OrderFinancialAlertCodes.LowMargin && a.Severity == OrderFinancialAlertSeverity.Warning);
    }

    [Fact]
    public async Task EvaluateOrderAlerts_MissingRevenue_ReturnsNoBillingRateFoundOrUnresolved()
    {
        _context.Set<GponSiJobRate>().Add(new GponSiJobRate { Id = Guid.NewGuid(), CompanyId = _companyId, OrderTypeId = _orderTypeId, OrderCategoryId = _orderCategoryId, SiLevel = "Senior", PartnerGroupId = _partnerGroupId, PayoutAmount = 60m, IsActive = true });
        await _context.SaveChangesAsync();

        var result = await _alertService.EvaluateOrderAlertsAsync(_orderId, _companyId);

        result.Alerts.Should().Contain(a => a.AlertCode == OrderFinancialAlertCodes.NoBillingRateFound);
    }

    [Fact]
    public async Task EvaluateOrderAlerts_MissingPayout_ReturnsNoPayoutRateFoundOrUnresolved()
    {
        _context.BillingRatecards.Add(new BillingRatecard { Id = Guid.NewGuid(), CompanyId = _companyId, PartnerId = _partnerId, OrderTypeId = _orderTypeId, ServiceCategory = "FTTH", Amount = 200m, IsActive = true });
        await _context.SaveChangesAsync();

        var result = await _alertService.EvaluateOrderAlertsAsync(_orderId, _companyId);

        result.Alerts.Should().Contain(a => a.AlertCode == OrderFinancialAlertCodes.NoPayoutRateFound);
    }

    [Fact]
    public async Task EvaluateOrderAlerts_OrderCategoryMissing_ReturnsOrderCategoryMissingAlert()
    {
        var orderNoCategory = new Order
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            PartnerId = _partnerId,
            OrderTypeId = _orderTypeId,
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

        var result = await _alertService.EvaluateOrderAlertsAsync(orderNoCategory.Id, _companyId);

        result.Alerts.Should().Contain(a => a.AlertCode == OrderFinancialAlertCodes.OrderCategoryMissing && a.Severity == OrderFinancialAlertSeverity.Critical);
    }

    [Fact]
    public async Task EvaluateOrderAlerts_ThresholdConfigRespected_NoLowMarginWhenAboveThreshold()
    {
        var highThresholdService = new OrderProfitAlertService(
            _profitabilityService,
            Options.Create(new ProfitabilityAlertsOptions { LowMarginThresholdPercent = 5 }),
            _context,
            Mock.Of<IOrderFinancialAlertNotifier>(),
            Mock.Of<ILogger<OrderProfitAlertService>>());
        _context.BillingRatecards.Add(new BillingRatecard { Id = Guid.NewGuid(), CompanyId = _companyId, PartnerId = _partnerId, OrderTypeId = _orderTypeId, ServiceCategory = "FTTH", Amount = 100m, IsActive = true });
        _context.Set<GponSiJobRate>().Add(new GponSiJobRate { Id = Guid.NewGuid(), CompanyId = _companyId, OrderTypeId = _orderTypeId, OrderCategoryId = _orderCategoryId, SiLevel = "Senior", PartnerGroupId = _partnerGroupId, PayoutAmount = 94m, IsActive = true });
        await _context.SaveChangesAsync();

        var result = await highThresholdService.EvaluateOrderAlertsAsync(_orderId, _companyId);

        result.Alerts.Should().NotContain(a => a.AlertCode == OrderFinancialAlertCodes.LowMargin);
    }

    [Fact]
    public async Task EvaluateOrderAlerts_GoodMargin_NoCriticalAlerts()
    {
        _context.BillingRatecards.Add(new BillingRatecard { Id = Guid.NewGuid(), CompanyId = _companyId, PartnerId = _partnerId, OrderTypeId = _orderTypeId, ServiceCategory = "FTTH", Amount = 200m, IsActive = true });
        _context.Set<GponSiJobRate>().Add(new GponSiJobRate { Id = Guid.NewGuid(), CompanyId = _companyId, OrderTypeId = _orderTypeId, OrderCategoryId = _orderCategoryId, SiLevel = "Senior", PartnerGroupId = _partnerGroupId, PayoutAmount = 80m, IsActive = true });
        await _context.SaveChangesAsync();

        var result = await _alertService.EvaluateOrderAlertsAsync(_orderId, _companyId);

        result.Alerts.Should().NotContain(a => a.Severity == OrderFinancialAlertSeverity.Critical);
        result.HighestSeverity.Should().Be(OrderFinancialAlertSeverity.Warning);
    }

    [Fact]
    public async Task EvaluateOrdersAlerts_Bulk_ReturnsList()
    {
        _context.BillingRatecards.Add(new BillingRatecard { Id = Guid.NewGuid(), CompanyId = _companyId, PartnerId = _partnerId, OrderTypeId = _orderTypeId, ServiceCategory = "FTTH", Amount = 100m, IsActive = true });
        _context.Set<GponSiJobRate>().Add(new GponSiJobRate { Id = Guid.NewGuid(), CompanyId = _companyId, OrderTypeId = _orderTypeId, OrderCategoryId = _orderCategoryId, SiLevel = "Senior", PartnerGroupId = _partnerGroupId, PayoutAmount = 40m, IsActive = true });
        await _context.SaveChangesAsync();

        var results = await _alertService.EvaluateOrdersAlertsAsync(new[] { _orderId }, _companyId);

        results.Should().HaveCount(1);
        results[0].OrderId.Should().Be(_orderId);
    }

    // --- GetOrderFinancialAlertSummariesAsync (persisted alerts, for enrichment) ---

    [Fact]
    public async Task GetOrderFinancialAlertSummaries_NoAlerts_ReturnsEmptyList()
    {
        var summaries = await _alertService.GetOrderFinancialAlertSummariesAsync(_companyId, new[] { _orderId }, default);
        summaries.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrderFinancialAlertSummaries_SingleOrder_ReturnsCountAndHighestSeverity()
    {
        _context.OrderFinancialAlerts.Add(CreatePersistedAlert(_companyId, _orderId, "LOW_MARGIN", OrderFinancialAlertSeverity.Warning));
        _context.OrderFinancialAlerts.Add(CreatePersistedAlert(_companyId, _orderId, "INSTALLATION_METHOD_MISSING", OrderFinancialAlertSeverity.Info));
        await _context.SaveChangesAsync();

        var summaries = await _alertService.GetOrderFinancialAlertSummariesAsync(_companyId, new[] { _orderId }, default);

        summaries.Should().HaveCount(1);
        summaries[0].OrderId.Should().Be(_orderId);
        summaries[0].ActiveAlertCount.Should().Be(2);
        summaries[0].HighestAlertSeverity.Should().Be(OrderFinancialAlertSeverity.Warning);
    }

    [Fact]
    public async Task GetOrderFinancialAlertSummaries_Paged_MultipleOrders_Batch()
    {
        var orderId2 = Guid.NewGuid();
        _context.OrderFinancialAlerts.Add(CreatePersistedAlert(_companyId, _orderId, "NEGATIVE_PROFIT", OrderFinancialAlertSeverity.Critical));
        _context.OrderFinancialAlerts.Add(CreatePersistedAlert(_companyId, orderId2, "LOW_MARGIN", OrderFinancialAlertSeverity.Warning));
        await _context.SaveChangesAsync();

        var summaries = await _alertService.GetOrderFinancialAlertSummariesAsync(_companyId, new[] { _orderId, orderId2 }, default);

        summaries.Should().HaveCount(2);
        summaries.Should().Contain(s => s.OrderId == _orderId && s.ActiveAlertCount == 1 && s.HighestAlertSeverity == OrderFinancialAlertSeverity.Critical);
        summaries.Should().Contain(s => s.OrderId == orderId2 && s.ActiveAlertCount == 1 && s.HighestAlertSeverity == OrderFinancialAlertSeverity.Warning);
    }

    [Fact]
    public async Task GetOrderFinancialAlertSummaries_MultipleAlerts_SeverityPriority_CriticalWins()
    {
        _context.OrderFinancialAlerts.Add(CreatePersistedAlert(_companyId, _orderId, "NEGATIVE_PROFIT", OrderFinancialAlertSeverity.Critical));
        _context.OrderFinancialAlerts.Add(CreatePersistedAlert(_companyId, _orderId, "LOW_MARGIN", OrderFinancialAlertSeverity.Warning));
        _context.OrderFinancialAlerts.Add(CreatePersistedAlert(_companyId, _orderId, "INSTALLATION_METHOD_MISSING", OrderFinancialAlertSeverity.Info));
        await _context.SaveChangesAsync();

        var summaries = await _alertService.GetOrderFinancialAlertSummariesAsync(_companyId, new[] { _orderId }, default);

        summaries.Should().HaveCount(1);
        summaries[0].ActiveAlertCount.Should().Be(3);
        summaries[0].HighestAlertSeverity.Should().Be(OrderFinancialAlertSeverity.Critical);
    }

    [Fact]
    public async Task GetOrderFinancialAlertSummaries_IgnoresInactiveAlerts()
    {
        _context.OrderFinancialAlerts.Add(CreatePersistedAlert(_companyId, _orderId, "LOW_MARGIN", OrderFinancialAlertSeverity.Warning, isActive: true));
        _context.OrderFinancialAlerts.Add(CreatePersistedAlert(_companyId, _orderId, "NEGATIVE_PROFIT", OrderFinancialAlertSeverity.Critical, isActive: false));
        await _context.SaveChangesAsync();

        var summaries = await _alertService.GetOrderFinancialAlertSummariesAsync(_companyId, new[] { _orderId }, default);

        summaries.Should().HaveCount(1);
        summaries[0].ActiveAlertCount.Should().Be(1);
        summaries[0].HighestAlertSeverity.Should().Be(OrderFinancialAlertSeverity.Warning);
    }

    private static OrderFinancialAlert CreatePersistedAlert(Guid companyId, Guid orderId, string code, string severity, bool isActive = true)
    {
        return new OrderFinancialAlert
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            OrderId = orderId,
            AlertCode = code,
            Severity = severity,
            Message = "Test",
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
    }
}

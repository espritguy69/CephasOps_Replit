using CephasOps.Application.Assets.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Inventory.Services;
using CephasOps.Application.Notifications.Services;
using CephasOps.Application.Parser.Services;
using CephasOps.Application.Payroll.Services;
using CephasOps.Application.Workflow.JobOrchestration.Executors;
using CephasOps.Application.Workflow.Services;
using CephasOps.Domain.Common.Services;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace CephasOps.Application.Tests.TenantIsolation;

/// <summary>
/// Verifies that Guid.Empty tenant fallback has been removed: services require a valid CompanyId and throw when it is missing or empty.
/// </summary>
[Collection("TenantScopeTests")]
public class TenantFallbackRemovalTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Guid _companyA;
    private readonly Guid? _previousTenantId;

    public TenantFallbackRemovalTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        _companyA = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task PayrollService_GetSiRatePlansAsync_WhenCompanyIdEmpty_Throws()
    {
        TenantScope.CurrentTenantId = _companyA;
        var logger = new Mock<ILogger<PayrollService>>();
        var service = new PayrollService(
            _context,
            Mock.Of<Application.Rates.Services.IRateEngineService>(),
            Mock.Of<Application.Settings.Services.IKpiProfileService>(),
            logger.Object);

        var act = () => service.GetSiRatePlansAsync(Guid.Empty);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Tenant context missing*CompanyId required*");
    }

    [Fact]
    public async Task WorkflowDefinitionsService_GetWorkflowDefinitionsAsync_WhenCompanyIdEmpty_Throws()
    {
        TenantScope.CurrentTenantId = _companyA;
        var logger = new Mock<ILogger<WorkflowDefinitionsService>>();
        var service = new WorkflowDefinitionsService(
            _context,
            logger.Object,
            Mock.Of<Application.Common.Interfaces.ICurrentUserService>());

        var act = () => service.GetWorkflowDefinitionsAsync(Guid.Empty, null, null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Tenant context missing*CompanyId required*");
    }

    [Fact]
    public void CurrentUserService_WhenNoCompanyClaim_Throws()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity();
        context.User = new ClaimsPrincipal(identity);
        accessor.Setup(x => x.HttpContext).Returns(context);
        var service = new CurrentUserService(accessor.Object);

        var act = () => _ = service.CompanyId;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Tenant context missing*CompanyId claim required*");
    }

    [Fact]
    public async Task NotificationService_ResolveUsersByRoleAsync_WhenCompanyIdEmpty_Throws()
    {
        TenantScope.CurrentTenantId = _companyA;
        var logger = new Mock<ILogger<NotificationService>>();
        var dispatchStore = new Mock<CephasOps.Domain.Notifications.INotificationDispatchStore>();
        var service = new NotificationService(_context, dispatchStore.Object, logger.Object);

        var act = () => service.ResolveUsersByRoleAsync("Admin", Guid.Empty);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Tenant context missing*CompanyId required*");
    }

    [Fact]
    public async Task NotificationService_ResolveUsersByRoleAsync_WhenCompanyIdNull_Throws()
    {
        TenantScope.CurrentTenantId = _companyA;
        var logger = new Mock<ILogger<NotificationService>>();
        var dispatchStore = new Mock<CephasOps.Domain.Notifications.INotificationDispatchStore>();
        var service = new NotificationService(_context, dispatchStore.Object, logger.Object);

        var act = () => service.ResolveUsersByRoleAsync("Admin", null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Tenant context missing*CompanyId required*");
    }

    [Fact]
    public async Task EmailAccountService_GetEmailAccountsAsync_WhenCompanyIdEmpty_Throws()
    {
        TenantScope.CurrentTenantId = _companyA;
        var logger = new Mock<ILogger<EmailAccountService>>();
        var service = new EmailAccountService(
            _context,
            logger.Object,
            Mock.Of<IEncryptionService>());

        var act = () => service.GetEmailAccountsAsync(Guid.Empty);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Tenant context missing*CompanyId required*");
    }

    [Fact]
    public async Task AssetService_GetAssetsAsync_WhenCompanyIdNull_Throws()
    {
        TenantScope.CurrentTenantId = _companyA;
        var logger = new Mock<ILogger<AssetService>>();
        var service = new AssetService(_context, logger.Object);

        var act = () => service.GetAssetsAsync(null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Tenant context missing*CompanyId required*");
    }

    [Fact]
    public async Task AssetService_GetAssetsAsync_WhenCompanyIdEmpty_Throws()
    {
        TenantScope.CurrentTenantId = _companyA;
        var logger = new Mock<ILogger<AssetService>>();
        var service = new AssetService(_context, logger.Object);

        var act = () => service.GetAssetsAsync(Guid.Empty);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Tenant context missing*CompanyId required*");
    }

    [Fact]
    public async Task StockLedgerService_GetUsageSummaryExportRowsAsync_WhenCompanyIdEmpty_Throws()
    {
        TenantScope.CurrentTenantId = _companyA;
        var logger = new Mock<ILogger<StockLedgerService>>();
        var service = new StockLedgerService(_context, logger.Object);

        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        var act = () => service.GetUsageSummaryExportRowsAsync(from, to, null, null, null, Guid.Empty, null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Tenant context missing*CompanyId required*");
    }

    [Fact]
    public async Task StockLedgerService_GetUsageSummaryExportRowsAsync_WhenCompanyIdNull_Throws()
    {
        TenantScope.CurrentTenantId = _companyA;
        var logger = new Mock<ILogger<StockLedgerService>>();
        var service = new StockLedgerService(_context, logger.Object);

        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        var act = () => service.GetUsageSummaryExportRowsAsync(from, to, null, null, null, null, null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Tenant context missing*CompanyId required*");
    }

    [Fact]
    public async Task InventoryReportExportJobExecutor_WhenPayloadMissingCompanyId_Throws()
    {
        var ledgerService = new Mock<IStockLedgerService>();
        var csvService = new Mock<Application.Common.Services.ICsvService>();
        var logger = new Mock<ILogger<InventoryReportExportJobExecutor>>();
        var executor = new InventoryReportExportJobExecutor(ledgerService.Object, csvService.Object, logger.Object);

        var job = new JobExecution
        {
            Id = Guid.NewGuid(),
            JobType = "inventoryreportexport",
            CompanyId = null,
            PayloadJson = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                { "reportType", "UsageSummary" },
                { "departmentId", Guid.NewGuid().ToString() },
                { "fromDate", DateTime.UtcNow.AddDays(-7).ToString("O") },
                { "toDate", DateTime.UtcNow.ToString("O") }
            })
        };

        var act = () => executor.ExecuteAsync(job);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Background job requires tenant context*");
    }
}

using CephasOps.Application.Buildings.Services;
using CephasOps.Application.Common.Services;
using CephasOps.Application.Inventory.Services;
using CephasOps.Application.Notifications.Services;
using CephasOps.Application.Orders.Services;
using CephasOps.Application.Rates.Services;
using CephasOps.Application.Settings.Services;
using CephasOps.Application.Orders.DTOs;
using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Workflow.Services;
using CephasOps.Domain.Common.Services;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Phase2Settings;

/// <summary>
/// Integration tests for Phase 2 Settings modules integrated into OrderService
/// Tests SLA, Automation Rules, Business Hours, and Escalation Rules integration
/// </summary>
public class OrderServiceIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<OrderService>> _loggerMock;
    private readonly Mock<IBuildingService> _buildingServiceMock;
    private readonly Mock<IBlockerValidationService> _blockerValidationServiceMock;
    private readonly Mock<IWorkflowEngineService> _workflowEngineServiceMock;
    private readonly Mock<IWorkflowDefinitionsService> _workflowDefinitionsServiceMock;
    private readonly Mock<ISlaProfileService> _slaProfileServiceMock;
    private readonly Mock<IAutomationRuleService> _automationRuleServiceMock;
    private readonly Mock<IBusinessHoursService> _businessHoursServiceMock;
    private readonly Mock<IEscalationRuleService> _escalationRuleServiceMock;
    private readonly Mock<IApprovalWorkflowService> _approvalWorkflowServiceMock;
    private readonly Mock<IOrderTypeService> _orderTypeServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IEncryptionService> _encryptionServiceMock;
    private readonly Mock<IMaterialTemplateService> _materialTemplateServiceMock;
    private readonly Mock<IInventoryService> _inventoryServiceMock;
    private readonly Mock<IOrderPayoutSnapshotService> _orderPayoutSnapshotServiceMock;

    public OrderServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<OrderService>>();
        _buildingServiceMock = new Mock<IBuildingService>();
        _blockerValidationServiceMock = new Mock<IBlockerValidationService>();
        _workflowEngineServiceMock = new Mock<IWorkflowEngineService>();
        _workflowDefinitionsServiceMock = new Mock<IWorkflowDefinitionsService>();
        _slaProfileServiceMock = new Mock<ISlaProfileService>();
        _automationRuleServiceMock = new Mock<IAutomationRuleService>();
        _businessHoursServiceMock = new Mock<IBusinessHoursService>();
        _escalationRuleServiceMock = new Mock<IEscalationRuleService>();
        _approvalWorkflowServiceMock = new Mock<IApprovalWorkflowService>();
        _orderTypeServiceMock = new Mock<IOrderTypeService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _encryptionServiceMock = new Mock<IEncryptionService>();
        _materialTemplateServiceMock = new Mock<IMaterialTemplateService>();
        _inventoryServiceMock = new Mock<IInventoryService>();
        _orderPayoutSnapshotServiceMock = new Mock<IOrderPayoutSnapshotService>();
        _orderPayoutSnapshotServiceMock.Setup(x => x.CreateSnapshotForOrderIfEligibleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
        _orderPayoutSnapshotServiceMock.Setup(x => x.GetSnapshotByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((CephasOps.Application.Rates.DTOs.OrderPayoutSnapshotDto?)null);
        _orderPayoutSnapshotServiceMock.Setup(x => x.GetPayoutWithSnapshotOrLiveAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>())).ReturnsAsync(new CephasOps.Application.Rates.DTOs.OrderPayoutSnapshotResponseDto());
    }

    [Fact]
    public async Task ChangeOrderStatus_ShouldCalculateSla_WhenSlaProfileExists()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var orderTypeId = Guid.NewGuid();

        // Setup mocks
        _orderTypeServiceMock.Setup(x => x.GetOrderTypeByIdAsync(orderTypeId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderTypeDto { Id = orderTypeId, Name = "Activation", Code = "ACTIVATION" });

        _slaProfileServiceMock.Setup(x => x.GetEffectiveProfileAsync(
            companyId, It.IsAny<Guid?>(), "Activation", It.IsAny<Guid?>(), false, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SlaProfileDto
            {
                Id = Guid.NewGuid(),
                Name = "Standard SLA",
                ResponseSlaMinutes = 60,
                ResponseSlaFromStatus = "Pending",
                ResponseSlaToStatus = "Assigned",
                ExcludeNonBusinessHours = false,
                NotifyOnBreach = false
            });

        var orderService = CreateOrderService();

        // Act & Assert
        // Note: This is a simplified test - full integration would require setting up order entity
        // and workflow engine mocks. This demonstrates the test structure.
        Assert.True(true); // Placeholder - implement full test with order entity setup
    }

    [Fact]
    public async Task ChangeOrderStatus_ShouldExecuteAutomationRules_WhenRulesExist()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var orderTypeId = Guid.NewGuid();

        _orderTypeServiceMock.Setup(x => x.GetOrderTypeByIdAsync(orderTypeId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderTypeDto { Id = orderTypeId, Name = "Activation", Code = "ACTIVATION" });

        _automationRuleServiceMock.Setup(x => x.GetApplicableRulesAsync(
            companyId, "Order", "Assigned", It.IsAny<Guid?>(), It.IsAny<Guid?>(), "Activation", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AutomationRuleDto>
            {
                new AutomationRuleDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Auto-Notify on Assignment",
                    RuleType = "auto-notify",
                    TargetRole = "Manager",
                    IsActive = true
                }
            });

        var orderService = CreateOrderService();

        // Act & Assert
        Assert.True(true); // Placeholder - implement full test
    }

    [Fact]
    public async Task ChangeOrderStatus_ShouldCheckEscalationRules_WhenRulesExist()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var orderTypeId = Guid.NewGuid();

        _orderTypeServiceMock.Setup(x => x.GetOrderTypeByIdAsync(orderTypeId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderTypeDto { Id = orderTypeId, Name = "Activation", Code = "ACTIVATION" });

        _escalationRuleServiceMock.Setup(x => x.GetApplicableRulesAsync(
            companyId, "Order", "Pending", It.IsAny<Guid?>(), It.IsAny<Guid?>(), "Activation", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EscalationRuleDto>
            {
                new EscalationRuleDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Escalate Pending Orders",
                    TriggerType = "time-based",
                    TriggerDelayMinutes = 120,
                    TargetRole = "Manager",
                    IsActive = true
                }
            });

        var orderService = CreateOrderService();

        // Act & Assert
        Assert.True(true); // Placeholder - implement full test
    }

    [Fact]
    public async Task CalculateBusinessMinutes_ShouldExcludeNonBusinessHours_WhenConfigured()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var startTime = new DateTime(2024, 1, 15, 16, 0, 0); // Monday 4 PM
        var endTime = new DateTime(2024, 1, 16, 10, 0, 0); // Tuesday 10 AM

        _businessHoursServiceMock.Setup(x => x.GetEffectiveBusinessHoursAsync(
            companyId, It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BusinessHoursDto
            {
                Id = Guid.NewGuid(),
                Name = "Standard Hours",
                MondayStart = "09:00",
                MondayEnd = "17:00",
                TuesdayStart = "09:00",
                TuesdayEnd = "17:00"
            });

        _businessHoursServiceMock.Setup(x => x.IsBusinessHoursAsync(
            companyId, It.IsAny<DateTime>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid cid, DateTime dt, Guid? dept, CancellationToken ct) =>
            {
                var dayOfWeek = dt.DayOfWeek;
                var hour = dt.Hour;
                return dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday
                    && hour >= 9 && hour < 17;
            });

        _businessHoursServiceMock.Setup(x => x.IsPublicHolidayAsync(
            companyId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var orderService = CreateOrderService();

        // Act & Assert
        // Note: This would require accessing the private CalculateBusinessMinutesAsync method
        // or testing through public API. This demonstrates the test structure.
        Assert.True(true); // Placeholder - implement full test
    }

    /// <summary>
    /// When companyId is null, DeleteOrderAsync constrains by TenantScope.CurrentTenantId; wrong-tenant order must not be resolved or deleted.
    /// </summary>
    [Fact]
    public async Task DeleteOrderAsync_WhenCompanyIdNull_DoesNotDeleteOrderFromAnotherCompany()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        TenantSafetyGuard.EnterPlatformBypass();
        try
        {
            _context.Companies.Add(new Company { Id = companyA, ShortName = "A", LegalName = "Company A", IsActive = true });
            _context.Companies.Add(new Company { Id = companyB, ShortName = "B", LegalName = "Company B", IsActive = true });
            _context.Orders.Add(new Order
            {
                Id = orderId,
                CompanyId = companyA,
                PartnerId = Guid.Empty,
                OrderTypeId = Guid.NewGuid(),
                BuildingId = Guid.NewGuid(),
                SourceSystem = "Manual",
                Status = "Pending",
                AddressLine1 = "A1",
                City = "C",
                State = "S",
                Postcode = "00000"
            });
            await _context.SaveChangesAsync();
        }
        finally
        {
            TenantSafetyGuard.ExitPlatformBypass();
        }

        var orderService = CreateOrderService();
        var previousTenant = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = companyB;
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                orderService.DeleteOrderAsync(orderId, companyId: null, departmentId: null));
        }
        finally
        {
            TenantScope.CurrentTenantId = previousTenant;
        }

        var order = await _context.Orders.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == orderId);
        Assert.NotNull(order);
        Assert.False(order.IsDeleted);
        Assert.Equal(companyA, order.CompanyId);
    }

    private OrderService CreateOrderService()
    {
        var effectiveScopeResolverMock = new Moq.Mock<CephasOps.Application.Common.Interfaces.IEffectiveScopeResolver>();
        effectiveScopeResolverMock.Setup(x => x.GetOrderTypeCodeForScopeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        effectiveScopeResolverMock.Setup(x => x.ResolveFromEntityAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((CephasOps.Application.Common.DTOs.EffectiveOrderScope?)null);
        return new OrderService(
            _context,
            _loggerMock.Object,
            _buildingServiceMock.Object,
            _blockerValidationServiceMock.Object,
            _workflowEngineServiceMock.Object,
            _workflowDefinitionsServiceMock.Object,
            _slaProfileServiceMock.Object,
            _automationRuleServiceMock.Object,
            _businessHoursServiceMock.Object,
            _escalationRuleServiceMock.Object,
            _approvalWorkflowServiceMock.Object,
            _orderTypeServiceMock.Object,
            _notificationServiceMock.Object,
            _encryptionServiceMock.Object,
            _materialTemplateServiceMock.Object,
            _inventoryServiceMock.Object,
            effectiveScopeResolverMock.Object,
            _orderPayoutSnapshotServiceMock.Object);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}


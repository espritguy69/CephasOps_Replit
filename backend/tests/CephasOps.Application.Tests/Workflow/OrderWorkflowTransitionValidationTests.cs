using System.Threading;
using CephasOps.Application.Common.DTOs;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Workflow;
using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Application.Workflow.Services;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Orders.Enums;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Workflow;

/// <summary>
/// Validates that order status transitions are enforced by the workflow engine.
/// Source of truth: 07_gpon_order_workflow.sql and docs/05_data_model/WORKFLOW_STATUS_REFERENCE.md.
/// </summary>
[Collection("TenantScopeTests")]
public class OrderWorkflowTransitionValidationTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IWorkflowDefinitionsService> _mockWorkflowDefinitionsService;
    private readonly Mock<IOrderPricingContextResolver> _mockOrderPricingContextResolver;
    private readonly Mock<IEffectiveScopeResolver> _mockEffectiveScopeResolver;
    private readonly GuardConditionValidatorRegistry _guardRegistry;
    private readonly SideEffectExecutorRegistry _sideEffectRegistry;
    private readonly WorkflowEngineService _service;
    private readonly Guid _companyId;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid? _previousTenantId;

    public OrderWorkflowTransitionValidationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        _mockWorkflowDefinitionsService = new Mock<IWorkflowDefinitionsService>();
        _mockOrderPricingContextResolver = new Mock<IOrderPricingContextResolver>();
        _mockEffectiveScopeResolver = new Mock<IEffectiveScopeResolver>();
        _mockOrderPricingContextResolver
            .Setup(x => x.ResolveFromOrderAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderPricingContext());

        var mockLoggerG = new Mock<ILogger<GuardConditionValidatorRegistry>>();
        var mockLoggerS = new Mock<ILogger<SideEffectExecutorRegistry>>();
        _guardRegistry = new GuardConditionValidatorRegistry(
            Enumerable.Empty<IGuardConditionValidator>(),
            _dbContext,
            mockLoggerG.Object);
        _sideEffectRegistry = new SideEffectExecutorRegistry(
            Enumerable.Empty<ISideEffectExecutor>(),
            _dbContext,
            mockLoggerS.Object);

        var mockLogger = new Mock<ILogger<WorkflowEngineService>>();
        var mockScheduler = new Mock<CephasOps.Application.Scheduler.Services.ISchedulerService>();

        _service = new WorkflowEngineService(
            _dbContext,
            mockLogger.Object,
            _mockWorkflowDefinitionsService.Object,
            _mockOrderPricingContextResolver.Object,
            _mockEffectiveScopeResolver.Object,
            _guardRegistry,
            _sideEffectRegistry,
            mockScheduler.Object);
    }

    private WorkflowDefinitionDto CreateGponOrderWorkflowDefinition()
    {
        var transitions = new List<WorkflowTransitionDto>();
        void Add(string from, string to)
        {
            transitions.Add(new WorkflowTransitionDto
            {
                Id = Guid.NewGuid(),
                FromStatus = from,
                ToStatus = to,
                IsActive = true,
                AllowedRoles = new List<string> { "Admin", "SI", "Ops" },
                DisplayOrder = transitions.Count + 1
            });
        }

        Add("Pending", "Assigned");
        Add("Pending", "Cancelled");
        Add("Assigned", "OnTheWay");
        Add("Assigned", "Blocker");
        Add("Assigned", "ReschedulePendingApproval");
        Add("Assigned", "Cancelled");
        Add("OnTheWay", "MetCustomer");
        Add("OnTheWay", "Blocker");
        Add("MetCustomer", "OrderCompleted");
        Add("MetCustomer", "Blocker");
        Add("Blocker", "MetCustomer");
        Add("Blocker", "Assigned");
        Add("Blocker", "ReschedulePendingApproval");
        Add("Blocker", "Cancelled");
        Add("ReschedulePendingApproval", "Assigned");
        Add("ReschedulePendingApproval", "Cancelled");
        Add("OrderCompleted", "DocketsReceived");
        Add("DocketsReceived", "DocketsVerified");
        Add("DocketsReceived", "DocketsRejected");
        Add("DocketsRejected", "DocketsReceived");
        Add("DocketsVerified", "DocketsUploaded");
        Add("DocketsUploaded", "ReadyForInvoice");
        Add("ReadyForInvoice", "Invoiced");
        Add("Invoiced", "SubmittedToPortal");
        Add("SubmittedToPortal", "Completed");
        Add("Invoiced", "Rejected");
        Add("SubmittedToPortal", "Rejected");
        Add("Rejected", "ReadyForInvoice");
        Add("Rejected", "Reinvoice");
        Add("Reinvoice", "Invoiced");

        return new WorkflowDefinitionDto
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            EntityType = "Order",
            Name = "Order Workflow",
            IsActive = true,
            Transitions = transitions
        };
    }

    private void SetupWorkflowDefinition(WorkflowDefinitionDto? def = null)
    {
        var d = def ?? CreateGponOrderWorkflowDefinition();
        _mockWorkflowDefinitionsService
            .Setup(x => x.GetEffectiveWorkflowDefinitionAsync(_companyId, "Order", It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(d);
    }

    private async Task<Order> CreateOrderAsync(string status)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();
        return order;
    }

    /// <summary>
    /// Runs test body with tenant scope. Uses TenantPreservingSyncContext (with captured ExecutionContext)
    /// so continuations see tenant when xUnit does not flow AsyncLocal across await.
    /// </summary>
    private async Task RunWithTenantAsync(Func<Task> act)
    {
        var prev = TenantScope.CurrentTenantId;
        var prevCtx = SynchronizationContext.Current;
        try
        {
            TenantScope.CurrentTenantId = _companyId;
            SynchronizationContext.SetSynchronizationContext(new TenantPreservingSyncContext(_companyId, null, prevCtx));
            await act();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prevCtx);
            TenantScope.CurrentTenantId = prev;
        }
    }

    [Fact]
    public async Task ValidTransition_Pending_To_Assigned_Succeeds()
    {
        await RunWithTenantAsync(async () =>
        {
        SetupWorkflowDefinition();
        var order = await CreateOrderAsync(OrderStatus.Pending);
        var dto = new ExecuteTransitionDto { EntityType = "Order", EntityId = order.Id, TargetStatus = OrderStatus.Assigned };

        var job = await _service.ExecuteTransitionAsync(_companyId, dto, _userId);

        job.State.Should().Be("Succeeded");
        var updated = await _dbContext.Orders.FindAsync(order.Id);
        updated!.Status.Should().Be(OrderStatus.Assigned);
        });
    }

    [Fact]
    public async Task ValidTransition_Assigned_To_OnTheWay_Succeeds()
    {
        await RunWithTenantAsync(async () =>
        {
        SetupWorkflowDefinition();
        var order = await CreateOrderAsync(OrderStatus.Assigned);
        var dto = new ExecuteTransitionDto { EntityType = "Order", EntityId = order.Id, TargetStatus = OrderStatus.OnTheWay };

        var job = await _service.ExecuteTransitionAsync(_companyId, dto, _userId);

        job.State.Should().Be("Succeeded");
        (await _dbContext.Orders.FindAsync(order.Id))!.Status.Should().Be(OrderStatus.OnTheWay);
        });
    }

    [Fact]
    public async Task ValidTransition_OnTheWay_To_MetCustomer_Succeeds()
    {
        await RunWithTenantAsync(async () =>
        {
        SetupWorkflowDefinition();
        var order = await CreateOrderAsync(OrderStatus.OnTheWay);
        var dto = new ExecuteTransitionDto { EntityType = "Order", EntityId = order.Id, TargetStatus = OrderStatus.MetCustomer };

        var job = await _service.ExecuteTransitionAsync(_companyId, dto, _userId);

        job.State.Should().Be("Succeeded");
        (await _dbContext.Orders.FindAsync(order.Id))!.Status.Should().Be(OrderStatus.MetCustomer);
        });
    }

    [Fact]
    public async Task ValidTransition_MetCustomer_To_OrderCompleted_Succeeds()
    {
        await RunWithTenantAsync(async () =>
        {
        SetupWorkflowDefinition();
        var order = await CreateOrderAsync(OrderStatus.MetCustomer);
        var dto = new ExecuteTransitionDto { EntityType = "Order", EntityId = order.Id, TargetStatus = OrderStatus.OrderCompleted };

        var job = await _service.ExecuteTransitionAsync(_companyId, dto, _userId);

        job.State.Should().Be("Succeeded");
        (await _dbContext.Orders.FindAsync(order.Id))!.Status.Should().Be(OrderStatus.OrderCompleted);
        });
    }

    [Fact]
    public async Task ValidTransition_Blocker_To_Assigned_Succeeds()
    {
        await RunWithTenantAsync(async () =>
        {
        SetupWorkflowDefinition();
        var order = await CreateOrderAsync(OrderStatus.Blocker);
        var dto = new ExecuteTransitionDto { EntityType = "Order", EntityId = order.Id, TargetStatus = OrderStatus.Assigned };

        var job = await _service.ExecuteTransitionAsync(_companyId, dto, _userId);

        job.State.Should().Be("Succeeded");
        (await _dbContext.Orders.FindAsync(order.Id))!.Status.Should().Be(OrderStatus.Assigned);
        });
    }

    [Fact]
    public async Task ValidTransition_DocketsReceived_To_DocketsRejected_Succeeds()
    {
        await RunWithTenantAsync(async () =>
        {
        SetupWorkflowDefinition();
        var order = await CreateOrderAsync(OrderStatus.DocketsReceived);
        var dto = new ExecuteTransitionDto { EntityType = "Order", EntityId = order.Id, TargetStatus = OrderStatus.DocketsRejected };

        var job = await _service.ExecuteTransitionAsync(_companyId, dto, _userId);

        job.State.Should().Be("Succeeded");
        (await _dbContext.Orders.FindAsync(order.Id))!.Status.Should().Be(OrderStatus.DocketsRejected);
        });
    }

    [Fact]
    public async Task InvalidTransition_Pending_To_MetCustomer_ThrowsWithAllowedNext()
    {
        await RunWithTenantAsync(async () =>
        {
        SetupWorkflowDefinition();
        var order = await CreateOrderAsync(OrderStatus.Pending);
        var dto = new ExecuteTransitionDto { EntityType = "Order", EntityId = order.Id, TargetStatus = OrderStatus.MetCustomer };

        var act = () => _service.ExecuteTransitionAsync(_companyId, dto, _userId);

        var ex = await act.Should().ThrowAsync<Exception>();
        ex.Which.Message.Should().Contain(OrderStatus.Pending).And.Contain(OrderStatus.MetCustomer).And.Contain("Allowed next");
        (await _dbContext.Orders.FindAsync(order.Id))!.Status.Should().Be(OrderStatus.Pending);
        });
    }

    [Fact]
    public async Task InvalidTransition_Assigned_To_OrderCompleted_ThrowsWithAllowedNext()
    {
        await RunWithTenantAsync(async () =>
        {
        SetupWorkflowDefinition();
        var order = await CreateOrderAsync(OrderStatus.Assigned);
        var dto = new ExecuteTransitionDto { EntityType = "Order", EntityId = order.Id, TargetStatus = OrderStatus.OrderCompleted };

        // SiWorkflowGuard rejects invalid jump with InvalidOperationException (before DB transition lookup)
        var ex = await FluentAssertions.FluentActions.Awaiting(() => _service.ExecuteTransitionAsync(_companyId, dto, _userId))
            .Should().ThrowAsync<Exception>();
        ex.Which.Should().BeOfType<InvalidOperationException>();
        ex.Which.Message.Should().Contain(OrderStatus.Assigned).And.Contain(OrderStatus.OrderCompleted);
        (await _dbContext.Orders.FindAsync(order.Id))!.Status.Should().Be(OrderStatus.Assigned);
        });
    }

    [Fact]
    public async Task InvalidTransition_Pending_To_Reinvoice_Throws()
    {
        await RunWithTenantAsync(async () =>
        {
        SetupWorkflowDefinition();
        var order = await CreateOrderAsync(OrderStatus.Pending);
        var dto = new ExecuteTransitionDto { EntityType = "Order", EntityId = order.Id, TargetStatus = OrderStatus.Reinvoice };

        await FluentAssertions.FluentActions.Awaiting(() => _service.ExecuteTransitionAsync(_companyId, dto, _userId))
            .Should().ThrowAsync<Exception>();
        (await _dbContext.Orders.FindAsync(order.Id))!.Status.Should().Be(OrderStatus.Pending);
        });
    }

    [Fact]
    public async Task InvalidTransition_Cancelled_To_Assigned_Throws()
    {
        await RunWithTenantAsync(async () =>
        {
        SetupWorkflowDefinition();
        var order = await CreateOrderAsync(OrderStatus.Cancelled);
        var dto = new ExecuteTransitionDto { EntityType = "Order", EntityId = order.Id, TargetStatus = OrderStatus.Assigned };

        await FluentAssertions.FluentActions.Awaiting(() => _service.ExecuteTransitionAsync(_companyId, dto, _userId))
            .Should().ThrowAsync<Exception>();
        (await _dbContext.Orders.FindAsync(order.Id))!.Status.Should().Be(OrderStatus.Cancelled);
        });
    }

    [Fact]
    public async Task Bypass_DirectExecuteTransition_InvalidJump_StillValidated()
    {
        await RunWithTenantAsync(async () =>
        {
        SetupWorkflowDefinition();
        var order = await CreateOrderAsync(OrderStatus.Pending);
        var dto = new ExecuteTransitionDto { EntityType = "Order", EntityId = order.Id, TargetStatus = OrderStatus.OrderCompleted };

        await FluentAssertions.FluentActions.Awaiting(() => _service.ExecuteTransitionAsync(_companyId, dto, _userId))
            .Should().ThrowAsync<Exception>();

        var updated = await _dbContext.Orders.FindAsync(order.Id);
        updated!.Status.Should().Be(OrderStatus.Pending);
        });
    }

    [Fact]
    public async Task SameStatus_DuplicateTransition_AssignedToAssigned_Throws()
    {
        await RunWithTenantAsync(async () =>
        {
        SetupWorkflowDefinition();
        var order = await CreateOrderAsync(OrderStatus.Assigned);
        var dto = new ExecuteTransitionDto { EntityType = "Order", EntityId = order.Id, TargetStatus = OrderStatus.Assigned };

        var act = () => _service.ExecuteTransitionAsync(_companyId, dto, _userId);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("already in the requested status")
            .And.Contain("Duplicate or no-op");

        (await _dbContext.Orders.FindAsync(order.Id))!.Status.Should().Be(OrderStatus.Assigned);
        });
    }

    [Fact]
    public async Task ReschedulePendingApproval_WithoutReasonInPayload_Throws()
    {
        await RunWithTenantAsync(async () =>
        {
            SetupWorkflowDefinition();
            var order = await CreateOrderAsync(OrderStatus.Assigned);
            var dto = new ExecuteTransitionDto
            {
                EntityType = "Order",
                EntityId = order.Id,
                TargetStatus = OrderStatus.ReschedulePendingApproval,
                Payload = null
            };

            var act = () => _service.ExecuteTransitionAsync(_companyId, dto, _userId);

            (await act.Should().ThrowAsync<InvalidOperationException>())
                .Which.Message.Should().Contain("Reschedule reason is required");

            (await _dbContext.Orders.FindAsync(order.Id))!.Status.Should().Be(OrderStatus.Assigned);
        });
    }

    [Fact]
    public async Task ReschedulePendingApproval_WithReasonInPayload_Succeeds()
    {
        await RunWithTenantAsync(async () =>
        {
            SetupWorkflowDefinition();
            var order = await CreateOrderAsync(OrderStatus.Assigned);
            var dto = new ExecuteTransitionDto
            {
                EntityType = "Order",
                EntityId = order.Id,
                TargetStatus = OrderStatus.ReschedulePendingApproval,
                Payload = new Dictionary<string, object> { ["reason"] = "Customer requested later date" }
            };

            var job = await _service.ExecuteTransitionAsync(_companyId, dto, _userId);

            job.State.Should().Be("Succeeded");
            (await _dbContext.Orders.FindAsync(order.Id))!.Status.Should().Be(OrderStatus.ReschedulePendingApproval);
        });
    }

    [Fact]
    public async Task WrongCompany_DoesNotResolveOrder_Throws()
    {
        await RunWithTenantAsync(async () =>
        {
            SetupWorkflowDefinition();
            var order = await CreateOrderAsync(OrderStatus.Pending);
            var wrongCompanyId = Guid.NewGuid();
            _mockWorkflowDefinitionsService
                .Setup(x => x.GetEffectiveWorkflowDefinitionAsync(wrongCompanyId, "Order", It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateGponOrderWorkflowDefinition());
            var dto = new ExecuteTransitionDto { EntityType = "Order", EntityId = order.Id, TargetStatus = OrderStatus.Assigned };

            var act = () => _service.ExecuteTransitionAsync(wrongCompanyId, dto, _userId);

            await act.Should().ThrowAsync<InvalidOperationException>();
            (await _dbContext.Orders.FindAsync(order.Id))!.Status.Should().Be(OrderStatus.Pending);
        });
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _dbContext?.Dispose();
    }
}

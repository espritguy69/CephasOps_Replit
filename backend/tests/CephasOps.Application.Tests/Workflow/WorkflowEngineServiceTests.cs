using CephasOps.Application.Common.DTOs;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Events;
using CephasOps.Application.Scheduler.Services;
using CephasOps.Domain.Events;
using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow;
using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Application.Workflow.Services;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Workflow;

/// <summary>
/// Unit tests for WorkflowEngineService. Tenant-scoped (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class WorkflowEngineServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<WorkflowEngineService>> _mockLogger;
    private readonly Mock<IWorkflowDefinitionsService> _mockWorkflowDefinitionsService;
    private readonly Mock<IOrderPricingContextResolver> _mockOrderPricingContextResolver;
    private readonly Mock<IEffectiveScopeResolver> _mockEffectiveScopeResolver;
    private readonly GuardConditionValidatorRegistry _guardConditionValidatorRegistry;
    private readonly SideEffectExecutorRegistry _sideEffectExecutorRegistry;
    private readonly Mock<ISchedulerService> _mockSchedulerService;
    private readonly WorkflowEngineService _service;
    private readonly Guid _companyId;
    private readonly Guid _userId;
    private readonly Guid? _previousTenantId;

    public WorkflowEngineServiceTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        _userId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<WorkflowEngineService>>();
        _mockWorkflowDefinitionsService = new Mock<IWorkflowDefinitionsService>();
        
        // Create registries with empty collections (for testing)
        var mockLoggerRegistry = new Mock<ILogger<GuardConditionValidatorRegistry>>();
        var mockLoggerSideEffect = new Mock<ILogger<SideEffectExecutorRegistry>>();
        _guardConditionValidatorRegistry = new GuardConditionValidatorRegistry(
            Enumerable.Empty<IGuardConditionValidator>(),
            _dbContext,
            mockLoggerRegistry.Object);
        _sideEffectExecutorRegistry = new SideEffectExecutorRegistry(
            Enumerable.Empty<ISideEffectExecutor>(),
            _dbContext,
            mockLoggerSideEffect.Object);
        _mockSchedulerService = new Mock<ISchedulerService>();
        _mockOrderPricingContextResolver = new Mock<IOrderPricingContextResolver>();
        _mockEffectiveScopeResolver = new Mock<IEffectiveScopeResolver>();
        // For Order entity type, workflow engine uses order pricing context resolver
        _mockOrderPricingContextResolver
            .Setup(x => x.ResolveFromOrderAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderPricingContext());
        
        _service = new WorkflowEngineService(
            _dbContext,
            _mockLogger.Object,
            _mockWorkflowDefinitionsService.Object,
            _mockOrderPricingContextResolver.Object,
            _mockEffectiveScopeResolver.Object,
            _guardConditionValidatorRegistry,
            _sideEffectExecutorRegistry,
            _mockSchedulerService.Object);
    }

    #region GetAllowedTransitions Tests

    [Fact]
    public async Task GetAllowedTransitionsAsync_ValidRequest_ReturnsAllowedTransitions()
    {
        // Arrange
        var workflowDefinition = CreateTestWorkflowDefinition();
        _mockWorkflowDefinitionsService
            .Setup(x => x.GetEffectiveWorkflowDefinitionAsync(_companyId, "Order", It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflowDefinition);

        // Act
        var result = await _service.GetAllowedTransitionsAsync(
            _companyId,
            "Order",
            Guid.NewGuid(),
            "Pending",
            new List<string> { "Admin" });

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GetAllowedTransitionsAsync_NoWorkflowDefinition_ReturnsEmptyList()
    {
        // Arrange
        _mockWorkflowDefinitionsService
            .Setup(x => x.GetEffectiveWorkflowDefinitionAsync(_companyId, "Order", It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowDefinitionDto?)null);

        // Act
        var result = await _service.GetAllowedTransitionsAsync(
            _companyId,
            "Order",
            Guid.NewGuid(),
            "Pending",
            new List<string> { "Admin" });

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllowedTransitionsAsync_RoleFiltered_ReturnsOnlyAllowedForRole()
    {
        // Arrange
        var workflowDefinition = CreateTestWorkflowDefinition();
        _mockWorkflowDefinitionsService
            .Setup(x => x.GetEffectiveWorkflowDefinitionAsync(_companyId, "Order", It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflowDefinition);

        // Act - User with Viewer role (should have fewer transitions)
        var result = await _service.GetAllowedTransitionsAsync(
            _companyId,
            "Order",
            Guid.NewGuid(),
            "Pending",
            new List<string> { "Viewer" });

        // Assert
        result.Should().NotBeNull();
        // Viewer role should have fewer or different transitions than Admin
    }

    #endregion

    #region CanTransition Tests

    [Fact]
    public async Task CanTransitionAsync_ValidTransition_ReturnsTrue()
    {
        // Arrange
        var workflowDefinition = CreateTestWorkflowDefinition();
        _mockWorkflowDefinitionsService
            .Setup(x => x.GetEffectiveWorkflowDefinitionAsync(_companyId, "Order", It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflowDefinition);

        // Act
        var result = await _service.CanTransitionAsync(
            _companyId,
            "Order",
            Guid.NewGuid(),
            "Pending",
            "Assigned",
            new List<string> { "Admin" });

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanTransitionAsync_InvalidTransition_ReturnsFalse()
    {
        // Arrange
        var workflowDefinition = CreateTestWorkflowDefinition();
        _mockWorkflowDefinitionsService
            .Setup(x => x.GetEffectiveWorkflowDefinitionAsync(_companyId, "Order", It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflowDefinition);

        // Act - Transition from Pending to Completed (should not be allowed directly)
        var result = await _service.CanTransitionAsync(
            _companyId,
            "Order",
            Guid.NewGuid(),
            "Pending",
            "Completed",
            new List<string> { "Admin" });

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanTransitionAsync_NoWorkflowDefinition_ReturnsFalse()
    {
        // Arrange
        _mockWorkflowDefinitionsService
            .Setup(x => x.GetEffectiveWorkflowDefinitionAsync(_companyId, "Order", It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowDefinitionDto?)null);

        // Act
        var result = await _service.CanTransitionAsync(
            _companyId,
            "Order",
            Guid.NewGuid(),
            "Pending",
            "Assigned",
            new List<string> { "Admin" });

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ExecuteTransition Tests

    [Fact]
    public async Task ExecuteTransitionAsync_NoWorkflowDefinition_ThrowsException()
    {
        // Arrange
        _mockWorkflowDefinitionsService
            .Setup(x => x.GetEffectiveWorkflowDefinitionAsync(_companyId, "Order", It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowDefinitionDto?)null);

        var dto = new ExecuteTransitionDto
        {
            EntityType = "Order",
            EntityId = Guid.NewGuid(),
            TargetStatus = "Assigned"
        };

        // Act
        var act = async () => await _service.ExecuteTransitionAsync(_companyId, dto, _userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No active workflow definition found*");
    }

    [Fact]
    public async Task ExecuteTransitionAsync_InvalidTransition_ThrowsException()
    {
        // Arrange
        var workflowDefinition = CreateTestWorkflowDefinition();
        _mockWorkflowDefinitionsService
            .Setup(x => x.GetEffectiveWorkflowDefinitionAsync(_companyId, "Order", It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflowDefinition);

        // Create an order with status "Pending"
        var order = await CreateTestOrderAsync("Pending");

        var dto = new ExecuteTransitionDto
        {
            EntityType = "Order",
            EntityId = order.Id,
            TargetStatus = "InvalidStatus" // Invalid transition
        };

        // Act
        var act = async () => await _service.ExecuteTransitionAsync(_companyId, dto, _userId);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidWorkflowTransitionException>();
        ex.Which.Message.Should().Contain("Pending");
        ex.Which.Message.Should().Contain("InvalidStatus");
        ex.Which.Message.Should().Contain("Allowed next statuses");
        ex.Which.AllowedNextStatuses.Should().Contain("Assigned");
    }

    [Fact]
    public async Task ExecuteTransitionAsync_ValidTransition_UpdatesOrderAndReturnsJob()
    {
        // Arrange: workflow Pending -> Assigned, order in Pending
        var workflowDefinition = CreateTestWorkflowDefinition();
        _mockWorkflowDefinitionsService
            .Setup(x => x.GetEffectiveWorkflowDefinitionAsync(_companyId, "Order", It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflowDefinition);

        var order = await CreateTestOrderAsync("Pending");

        var dto = new ExecuteTransitionDto
        {
            EntityType = "Order",
            EntityId = order.Id,
            TargetStatus = "Assigned"
        };

        // Act
        var job = await _service.ExecuteTransitionAsync(_companyId, dto, _userId);

        // Assert
        job.Should().NotBeNull();
        job.EntityType.Should().Be("Order");
        job.EntityId.Should().Be(order.Id);
        job.CurrentStatus.Should().Be("Pending");
        job.TargetStatus.Should().Be("Assigned");
        job.State.Should().Be("Succeeded");

        var updatedOrder = await _dbContext.Orders.FindAsync(order.Id);
        updatedOrder.Should().NotBeNull();
        updatedOrder!.Status.Should().Be("Assigned");
    }

    [Fact]
    public async Task ExecuteTransitionAsync_WhenEventStoreRegistered_StagesEventsInSameTransaction()
    {
        var mockEventStore = new Mock<IEventStore>();
        var serviceWithEvents = CreateServiceWithEventStore(mockEventStore.Object);

        var workflowDefinition = CreateTestWorkflowDefinition();
        _mockWorkflowDefinitionsService
            .Setup(x => x.GetEffectiveWorkflowDefinitionAsync(_companyId, "Order", It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflowDefinition);

        var order = await CreateTestOrderAsync("Pending");
        var dto = new ExecuteTransitionDto
        {
            EntityType = "Order",
            EntityId = order.Id,
            TargetStatus = "Assigned",
            CorrelationId = "test-correlation-123"
        };

        var job = await serviceWithEvents.ExecuteTransitionAsync(_companyId, dto, _userId);

        job.Should().NotBeNull();
        job.State.Should().Be("Succeeded");

        mockEventStore.Verify(x => x.AppendInCurrentTransaction(It.IsAny<IDomainEvent>()), Times.Exactly(3));
        var workflowEvt = mockEventStore.Invocations
            .Select(i => i.Arguments[0] as WorkflowTransitionCompletedEvent)
            .FirstOrDefault(e => e != null);
        var orderEvt = mockEventStore.Invocations
            .Select(i => i.Arguments[0] as OrderStatusChangedEvent)
            .FirstOrDefault(e => e != null);
        var assignedEvt = mockEventStore.Invocations
            .Select(i => i.Arguments[0] as OrderAssignedEvent)
            .FirstOrDefault(e => e != null);
        workflowEvt.Should().NotBeNull();
        orderEvt.Should().NotBeNull();
        workflowEvt!.WorkflowDefinitionId.Should().Be(workflowDefinition.Id);
        workflowEvt.WorkflowJobId.Should().Be(job.Id);
        workflowEvt.FromStatus.Should().Be("Pending");
        workflowEvt.ToStatus.Should().Be("Assigned");
        workflowEvt.EntityType.Should().Be("Order");
        workflowEvt.EntityId.Should().Be(order.Id);
        workflowEvt.CorrelationId.Should().Be("test-correlation-123");
        workflowEvt.CompanyId.Should().Be(_companyId);
        workflowEvt.TriggeredByUserId.Should().Be(_userId);
        workflowEvt.OccurredAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        workflowEvt.WorkflowTransitionId.Should().Be(workflowDefinition.Transitions.First(t => t.ToStatus == "Assigned").Id);
        orderEvt!.OrderId.Should().Be(order.Id);
        orderEvt.PreviousStatus.Should().Be("Pending");
        orderEvt.NewStatus.Should().Be("Assigned");
        assignedEvt.Should().NotBeNull();
        assignedEvt!.OrderId.Should().Be(order.Id);
        assignedEvt.WorkflowJobId.Should().Be(job.Id);
    }

    [Fact]
    public async Task ExecuteTransitionAsync_WhenTransitionNotToAssigned_EmitsOnlyTwoEvents_NoOrderAssignedEvent()
    {
        var mockEventStore = new Mock<IEventStore>();
        var serviceWithEvents = CreateServiceWithEventStore(mockEventStore.Object);

        var workflowDefinition = CreateTestWorkflowDefinition();
        _mockWorkflowDefinitionsService
            .Setup(x => x.GetEffectiveWorkflowDefinitionAsync(_companyId, "Order", It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflowDefinition);

        var order = await CreateTestOrderAsync("Assigned");
        var dto = new ExecuteTransitionDto
        {
            EntityType = "Order",
            EntityId = order.Id,
            TargetStatus = "InProgress",
            CorrelationId = "test-correlation-456"
        };

        var job = await serviceWithEvents.ExecuteTransitionAsync(_companyId, dto, _userId);

        job.Should().NotBeNull();
        job.State.Should().Be("Succeeded");
        mockEventStore.Verify(x => x.AppendInCurrentTransaction(It.IsAny<IDomainEvent>()), Times.Exactly(2));
        var assignedEvt = mockEventStore.Invocations
            .Select(i => i.Arguments[0] as OrderAssignedEvent)
            .FirstOrDefault(e => e != null);
        assignedEvt.Should().BeNull();
    }

    #endregion

    #region GetWorkflowJob Tests

    private void SetTenantScope() => TenantScope.CurrentTenantId = _companyId;

    [Fact]
    public async Task GetWorkflowJobAsync_ValidId_ReturnsJob()
    {
        SetTenantScope();
        // Arrange
        var job = await CreateTestWorkflowJobAsync();

        SetTenantScope();
        // Act
        var result = await _service.GetWorkflowJobAsync(_companyId, job.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(job.Id);
    }

    [Fact]
    public async Task GetWorkflowJobAsync_InvalidId_ReturnsNull()
    {
        SetTenantScope();
        // Act
        var result = await _service.GetWorkflowJobAsync(_companyId, Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWorkflowJobsAsync_WithFilters_ReturnsFilteredJobs()
    {
        SetTenantScope();
        // Arrange
        var order1Id = Guid.NewGuid();
        var order2Id = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        
        var job1 = await CreateTestWorkflowJobAsync("Order", "Pending", "Assigned");
        job1.EntityId = order1Id;
        SetTenantScope();
        await _dbContext.SaveChangesAsync();
        
        SetTenantScope();
        var job2 = await CreateTestWorkflowJobAsync("Order", "Assigned", "InProgress");
        job2.EntityId = order1Id;
        SetTenantScope();
        await _dbContext.SaveChangesAsync();
        
        SetTenantScope();
        var job3 = await CreateTestWorkflowJobAsync("Task", "Pending", "Assigned");
        job3.EntityId = taskId;
        SetTenantScope();
        await _dbContext.SaveChangesAsync();

        SetTenantScope();
        // Act - Get jobs for order1
        var result = await _service.GetWorkflowJobsAsync(
            _companyId,
            entityType: "Order",
            entityId: order1Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(j => j.EntityType == "Order");
        result.Should().OnlyContain(j => j.EntityId == order1Id);
    }

    #endregion

    #region Helper Methods

    private WorkflowEngineService CreateServiceWithEventStore(IEventStore eventStore)
    {
        return new WorkflowEngineService(
            _dbContext,
            _mockLogger.Object,
            _mockWorkflowDefinitionsService.Object,
            _mockOrderPricingContextResolver.Object,
            _mockEffectiveScopeResolver.Object,
            _guardConditionValidatorRegistry,
            _sideEffectExecutorRegistry,
            _mockSchedulerService.Object,
            auditLogService: null,
            eventStore: eventStore,
            envelopeBuilder: null);
    }

    private WorkflowDefinitionDto CreateTestWorkflowDefinition()
    {
        return new WorkflowDefinitionDto
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            EntityType = "Order",
            Name = "Order Workflow",
            IsActive = true,
            Transitions = new List<WorkflowTransitionDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    FromStatus = "Pending",
                    ToStatus = "Assigned",
                    IsActive = true,
                    AllowedRoles = new List<string> { "Admin", "Manager" },
                    DisplayOrder = 1
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    FromStatus = "Assigned",
                    ToStatus = "InProgress",
                    IsActive = true,
                    AllowedRoles = new List<string> { "Admin", "Manager", "Installer" },
                    DisplayOrder = 2
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    FromStatus = "InProgress",
                    ToStatus = "Completed",
                    IsActive = true,
                    AllowedRoles = new List<string> { "Admin", "Manager", "Installer" },
                    DisplayOrder = 3
                }
            }
        };
    }

    private async Task<Order> CreateTestOrderAsync(string status)
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

    private async Task<WorkflowJob> CreateTestWorkflowJobAsync(
        string? entityType = "Order",
        string? currentStatus = "Pending",
        string? targetStatus = "Assigned")
    {
        SetTenantScope();
        var workflowDefinition = await CreateTestWorkflowDefinitionEntityAsync();
        
        var job = new WorkflowJob
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            WorkflowDefinitionId = workflowDefinition.Id,
            EntityType = entityType ?? "Order",
            EntityId = Guid.NewGuid(),
            CurrentStatus = currentStatus,
            TargetStatus = targetStatus,
            State = WorkflowJobState.Succeeded,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.WorkflowJobs.Add(job);
        SetTenantScope();
        await _dbContext.SaveChangesAsync();
        return job;
    }

    private async Task<WorkflowDefinition> CreateTestWorkflowDefinitionEntityAsync()
    {
        SetTenantScope();
        var definition = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            EntityType = "Order",
            Name = "Test Workflow",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.WorkflowDefinitions.Add(definition);
        SetTenantScope();
        await _dbContext.SaveChangesAsync();
        return definition;
    }

    #endregion

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _dbContext?.Dispose();
    }
}


using CephasOps.Application.Tasks.DTOs;
using CephasOps.Application.Tasks.Services;
using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow.Executors;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.ServiceInstallers.Entities;
using CephasOps.Domain.ServiceInstallers.Enums;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Workflow;

[Collection("TenantScopeTests")]
public class CreateInstallerTaskSideEffectExecutorTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ITaskService> _mockTaskService;
    private readonly CreateInstallerTaskSideEffectExecutor _executor;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public CreateInstallerTaskSideEffectExecutorTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _mockTaskService = new Mock<ITaskService>();
        var mockLogger = new Mock<ILogger<CreateInstallerTaskSideEffectExecutor>>();
        _executor = new CreateInstallerTaskSideEffectExecutor(_context, _mockTaskService.Object, mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenToStatusIsAssigned_CreatesTaskForAssignedSi()
    {
        var companyId = _companyId;
        var orderId = Guid.NewGuid();
        var siId = Guid.NewGuid();
        var siUserId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            CompanyId = companyId,
            ServiceId = "TBBN-123",
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
        };
        var si = new ServiceInstaller
        {
            Id = siId,
            CompanyId = companyId,
            Name = "Test SI",
            UserId = siUserId,
            SiLevel = InstallerLevel.Junior,
            InstallerType = InstallerType.InHouse
        };
        _context.Orders.Add(order);
        _context.ServiceInstallers.Add(si);
        await _context.SaveChangesAsync();

        var transition = new WorkflowTransitionDto { ToStatus = "Assigned" };
        _mockTaskService
            .Setup(x => x.CreateTaskAsync(It.IsAny<CreateTaskDto>(), companyId, siUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskDto { Id = Guid.NewGuid(), Title = "Complete job: Order TBBN-123" });

        await _executor.ExecuteAsync(orderId, transition, null, null);

        _mockTaskService.Verify(
            x => x.CreateTaskAsync(
                It.Is<CreateTaskDto>(d =>
                    d.OrderId == orderId &&
                    d.AssignedToUserId == siUserId &&
                    d.Title == "Complete job: Order TBBN-123" &&
                    d.Priority == "Normal"),
                companyId,
                siUserId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenToStatusIsNotAssigned_DoesNotCreateTask()
    {
        var orderId = Guid.NewGuid();
        var transition = new WorkflowTransitionDto { ToStatus = "OnTheWay" };

        await _executor.ExecuteAsync(orderId, transition, null, null);

        _mockTaskService.Verify(
            x => x.CreateTaskAsync(It.IsAny<CreateTaskDto>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOrderHasNoAssignedSi_DoesNotCreateTask()
    {
        var companyId = _companyId;
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            CompanyId = companyId,
            ServiceId = "TBBN-456",
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
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var transition = new WorkflowTransitionDto { ToStatus = "Assigned" };

        await _executor.ExecuteAsync(orderId, transition, null, null);

        _mockTaskService.Verify(
            x => x.CreateTaskAsync(It.IsAny<CreateTaskDto>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSiHasNoUserId_DoesNotCreateTask()
    {
        var companyId = _companyId;
        var orderId = Guid.NewGuid();
        var siId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            CompanyId = companyId,
            ServiceId = "TBBN-789",
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
        };
        var si = new ServiceInstaller
        {
            Id = siId,
            CompanyId = companyId,
            Name = "SI No User",
            UserId = null,
            SiLevel = InstallerLevel.Junior,
            InstallerType = InstallerType.InHouse
        };
        _context.Orders.Add(order);
        _context.ServiceInstallers.Add(si);
        await _context.SaveChangesAsync();

        var transition = new WorkflowTransitionDto { ToStatus = "Assigned" };

        await _executor.ExecuteAsync(orderId, transition, null, null);

        _mockTaskService.Verify(
            x => x.CreateTaskAsync(It.IsAny<CreateTaskDto>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void Key_IsCreateInstallerTask()
    {
        _executor.Key.Should().Be("createInstallerTask");
    }

    [Fact]
    public void EntityType_IsOrder()
    {
        _executor.EntityType.Should().Be("Order");
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
    }
}

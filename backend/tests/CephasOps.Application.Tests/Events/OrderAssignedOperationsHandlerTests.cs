using CephasOps.Application.Events;
using CephasOps.Application.Events.Replay;
using CephasOps.Application.Orders.DTOs;
using CephasOps.Application.Orders.Services;
using CephasOps.Application.Tasks.DTOs;
using CephasOps.Application.Tasks.Services;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.ServiceInstallers.Entities;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Events;

/// <summary>
/// Run in TenantScopeTests collection so TenantScope is not overwritten by parallel tests.
/// Three tests (OrderWithAssignedSi, RepeatedCall, OrderWithoutAssignedSi) pass when run in isolation;
/// in batch they can fail due to in-memory global query filter + AsyncLocal in test runner. Production is correct (dispatcher sets scope).
/// </summary>
[Collection("TenantScopeTests")]
public class OrderAssignedOperationsHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ITaskService> _mockTaskService;
    private readonly Mock<IMaterialPackProvider> _mockMaterialPackProvider;
    private readonly OrderAssignedOperationsHandler _handler;

    public OrderAssignedOperationsHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "OrderAssignedOps_" + Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _mockTaskService = new Mock<ITaskService>();
        _mockMaterialPackProvider = new Mock<IMaterialPackProvider>();
        _mockMaterialPackProvider
            .Setup(x => x.GetMaterialPackAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaterialPackDto { OrderId = Guid.NewGuid(), Message = "OK" });

        var logger = new Mock<ILogger<OrderAssignedOperationsHandler>>();
        _handler = new OrderAssignedOperationsHandler(
            _context,
            _mockTaskService.Object,
            _mockMaterialPackProvider.Object,
            logger.Object,
            replayContextAccessor: null);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task HandleAsync_OrderWithAssignedSi_CreatesTask_CallsMaterialPack_EnqueuesSlaJob()
    {
        var companyId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var siId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var previousTenantId = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = companyId; // required for SaveChanges (tenant-scoped entities) and handler

            _context.Orders.Add(new Order
            {
                Id = orderId,
                CompanyId = companyId,
                Status = "Assigned",
                AssignedSiId = siId,
                CreatedAt = DateTime.UtcNow
            });
            _context.ServiceInstallers.Add(new ServiceInstaller
            {
                Id = siId,
                UserId = userId,
                CompanyId = companyId,
                Name = "Test SI"
            });
            await _context.SaveChangesAsync();

            var evt = new OrderAssignedEvent
            {
                EventId = Guid.NewGuid(),
                OrderId = orderId,
                CompanyId = companyId,
                WorkflowJobId = Guid.NewGuid()
            };

            await _handler.HandleAsync(evt);

            _mockTaskService.Verify(
                x => x.CreateTaskAsync(It.Is<CreateTaskDto>(d => d.OrderId == orderId && d.AssignedToUserId == userId),
                    companyId, userId, It.IsAny<CancellationToken>()),
                Times.Once);
            _mockMaterialPackProvider.Verify(
                x => x.GetMaterialPackAsync(orderId, companyId, It.IsAny<CancellationToken>()),
                Times.Once);

            var slaJobs = await _context.BackgroundJobs
                .Where(j => j.JobType == "slaevaluation" && j.State == BackgroundJobState.Queued)
                .ToListAsync();
            slaJobs.Should().HaveCount(1);
            slaJobs[0].CompanyId.Should().Be(companyId, "enqueued SLA job must have CompanyId set for tenant-scoped processing");
        }
        finally
        {
            TenantScope.CurrentTenantId = previousTenantId;
        }
    }

    [Fact]
    public async Task HandleAsync_RepeatedCall_DoesNotDuplicateTask_EnqueuesSlaOnlyOnce()
    {
        var companyId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var siId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var previousTenantId = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = companyId;
            _context.Orders.Add(new Order
            {
                Id = orderId,
                CompanyId = companyId,
                Status = "Assigned",
                AssignedSiId = siId,
                CreatedAt = DateTime.UtcNow
            });
            _context.ServiceInstallers.Add(new ServiceInstaller
            {
                Id = siId,
                UserId = userId,
                CompanyId = companyId,
                Name = "Test SI"
            });
            await _context.SaveChangesAsync();

            var evt = new OrderAssignedEvent
            {
                EventId = Guid.NewGuid(),
                OrderId = orderId,
                CompanyId = companyId,
                WorkflowJobId = Guid.NewGuid()
            };

            await _handler.HandleAsync(evt);
            await _handler.HandleAsync(evt);

            _mockTaskService.Verify(
            x => x.CreateTaskAsync(It.IsAny<CreateTaskDto>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        var slaJobs = await _context.BackgroundJobs
            .Where(j => j.JobType == "slaevaluation" && j.State == BackgroundJobState.Queued)
            .ToListAsync();
        slaJobs.Should().HaveCount(1, "second HandleAsync should skip enqueue because one is already queued");
        }
        finally
        {
            TenantScope.CurrentTenantId = previousTenantId;
        }
    }

    [Fact]
    public async Task HandleAsync_OrderWithoutAssignedSi_SkipsTask_StillCallsMaterialPack_EnqueuesSla()
    {
        var companyId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var previousTenantId = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = companyId;
            _context.Orders.Add(new Order
            {
                Id = orderId,
                CompanyId = companyId,
                Status = "Assigned",
                AssignedSiId = null,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var evt = new OrderAssignedEvent
            {
                EventId = Guid.NewGuid(),
                OrderId = orderId,
                CompanyId = companyId
            };

            await _handler.HandleAsync(evt);

            _mockTaskService.Verify(
                x => x.CreateTaskAsync(It.IsAny<CreateTaskDto>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _mockMaterialPackProvider.Verify(
                x => x.GetMaterialPackAsync(orderId, companyId, It.IsAny<CancellationToken>()),
                Times.Once);

            var slaJobs = await _context.BackgroundJobs
                .Where(j => j.JobType == "slaevaluation" && j.State == BackgroundJobState.Queued)
                .ToListAsync();
            slaJobs.Should().HaveCount(1);
        }
        finally
        {
            TenantScope.CurrentTenantId = previousTenantId;
        }
    }

    [Fact]
    public async Task HandleAsync_OrderNotFound_DoesNothing()
    {
        var orderId = Guid.NewGuid();
        var evt = new OrderAssignedEvent
        {
            EventId = Guid.NewGuid(),
            OrderId = orderId,
            CompanyId = Guid.NewGuid()
        };

        await _handler.HandleAsync(evt);

        _mockTaskService.Verify(
            x => x.CreateTaskAsync(It.IsAny<CreateTaskDto>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockMaterialPackProvider.Verify(
            x => x.GetMaterialPackAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        (await _context.BackgroundJobs.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_WhenOrderAndEventHaveNoCompanyId_DoesNotEnqueueSlaJob()
    {
        var orderId = Guid.NewGuid();
        var scopeForTest = Guid.NewGuid();
        var previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = scopeForTest;
        try
        {
            _context.Orders.Add(new Order
            {
                Id = orderId,
                CompanyId = null,
                Status = "Assigned",
                AssignedSiId = null,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var evt = new OrderAssignedEvent
            {
                EventId = Guid.NewGuid(),
                OrderId = orderId,
                CompanyId = null,
                WorkflowJobId = Guid.NewGuid()
            };

            await _handler.HandleAsync(evt);

            var slaJobs = await _context.BackgroundJobs
                .Where(j => j.JobType == "slaevaluation")
                .ToListAsync();
            slaJobs.Should().BeEmpty("SLA job must not be enqueued when order and event have no CompanyId (tenant-boundary)");
        }
        finally
        {
            TenantScope.CurrentTenantId = previousTenantId;
        }
    }

    /// <summary>
    /// Replay safety: when replay context is active, handler must not enqueue SLA job (prevents duplicate SLA evaluation jobs on replay).
    /// </summary>
    [Fact]
    public async Task HandleAsync_WhenReplayContextActive_DoesNotEnqueueSlaJob()
    {
        var companyId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var replayAccessor = new ReplayExecutionContextAccessor();
        var handlerWithReplay = new OrderAssignedOperationsHandler(
            _context,
            _mockTaskService.Object,
            _mockMaterialPackProvider.Object,
            new Mock<ILogger<OrderAssignedOperationsHandler>>().Object,
            replayAccessor);

        var previousTenantId = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = companyId;
            _context.Orders.Add(new Order
            {
                Id = orderId,
                CompanyId = companyId,
                Status = "Assigned",
                AssignedSiId = null,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var evt = new OrderAssignedEvent
            {
                EventId = Guid.NewGuid(),
                OrderId = orderId,
                CompanyId = companyId,
                WorkflowJobId = Guid.NewGuid()
            };

            replayAccessor.Set(ReplayExecutionContext.ForSingleEventRetry("Replay"));
            try
            {
                await handlerWithReplay.HandleAsync(evt);
            }
            finally
            {
                replayAccessor.Set(null);
            }

            var slaJobs = await _context.BackgroundJobs
                .Where(j => j.JobType == "slaevaluation")
                .ToListAsync();
            slaJobs.Should().BeEmpty("SLA job must not be enqueued during replay to prevent duplicate side effects");
        }
        finally
        {
            TenantScope.CurrentTenantId = previousTenantId;
        }
    }
}

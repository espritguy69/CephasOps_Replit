using CephasOps.Application.Workflow.JobOrchestration;
using CephasOps.Domain.Workflow;
using CephasOps.Domain.Workflow.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Workflow.JobOrchestration;

public class JobExecutionEnqueuerTests
{
    private readonly Mock<IJobExecutionStore> _store;
    private readonly Mock<ILogger<JobExecutionEnqueuer>> _logger;
    private readonly JobExecutionEnqueuer _enqueuer;

    public JobExecutionEnqueuerTests()
    {
        _store = new Mock<IJobExecutionStore>();
        _logger = new Mock<ILogger<JobExecutionEnqueuer>>();
        _enqueuer = new JobExecutionEnqueuer(_store.Object, _logger.Object);
    }

    [Fact]
    public async Task EnqueueAsync_Adds_JobExecution_With_Pending_Status_And_JobType()
    {
        JobExecution? captured = null;
        _store
            .Setup(s => s.AddAsync(It.IsAny<JobExecution>(), It.IsAny<CancellationToken>()))
            .Callback<JobExecution, CancellationToken>((j, _) => captured = j)
            .Returns(Task.CompletedTask);

        await _enqueuer.EnqueueAsync("PnlRebuild", "{\"companyId\":\"a1b2c3d4-0000-0000-0000-000000000001\",\"period\":\"2026-03\"}",
            companyId: Guid.Parse("a1b2c3d4-0000-0000-0000-000000000001"));

        _store.Verify(s => s.AddAsync(It.IsAny<JobExecution>(), It.IsAny<CancellationToken>()), Times.Once);
        captured.Should().NotBeNull();
        captured!.JobType.Should().Be("PnlRebuild");
        captured.Status.Should().Be("Pending");
        captured.PayloadJson.Should().Contain("2026-03");
        captured.CompanyId.Should().Be(Guid.Parse("a1b2c3d4-0000-0000-0000-000000000001"));
        captured.MaxAttempts.Should().Be(5);
    }

    [Fact]
    public async Task EnqueueAsync_With_Null_Payload_Uses_Empty_Object_Json()
    {
        JobExecution? captured = null;
        _store
            .Setup(s => s.AddAsync(It.IsAny<JobExecution>(), It.IsAny<CancellationToken>()))
            .Callback<JobExecution, CancellationToken>((j, _) => captured = j)
            .Returns(Task.CompletedTask);

        await _enqueuer.EnqueueAsync("TestJob", null!, companyId: null);

        captured!.PayloadJson.Should().Be("{}");
    }
}

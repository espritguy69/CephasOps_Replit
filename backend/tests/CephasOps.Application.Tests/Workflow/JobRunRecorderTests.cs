using CephasOps.Application.Workflow.JobObservability;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Workflow;

public class JobRunRecorderTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public JobRunRecorderTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "JobRunRecorder_" + Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task StartAsync_CreatesJobRun_WithRunningStatus()
    {
        var recorder = new JobRunRecorder(_context);
        var dto = new StartJobRunDto
        {
            JobName = "Test Job",
            JobType = "TestType",
            TriggerSource = "Manual",
            CorrelationId = "corr-1"
        };

        var id = await recorder.StartAsync(dto);

        id.Should().NotBe(Guid.Empty);
        var run = await _context.JobRuns.FindAsync(id);
        run.Should().NotBeNull();
        run!.JobName.Should().Be("Test Job");
        run.JobType.Should().Be("TestType");
        run.Status.Should().Be("Running");
        run.TriggerSource.Should().Be("Manual");
        run.CorrelationId.Should().Be("corr-1");
    }

    [Fact]
    public async Task CompleteAsync_UpdatesStatus_AndSetsDuration()
    {
        var recorder = new JobRunRecorder(_context);
        var id = await recorder.StartAsync(new StartJobRunDto
        {
            JobName = "Complete Test",
            JobType = "Test",
            TriggerSource = "System"
        });

        await recorder.CompleteAsync(id);

        var run = await _context.JobRuns.FindAsync(id);
        run.Should().NotBeNull();
        run!.Status.Should().Be("Succeeded");
        run.CompletedAtUtc.Should().NotBeNull();
        run.DurationMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task FailAsync_UpdatesStatus_AndStoresSanitizedError()
    {
        var recorder = new JobRunRecorder(_context);
        var id = await recorder.StartAsync(new StartJobRunDto
        {
            JobName = "Fail Test",
            JobType = "Test",
            TriggerSource = "System"
        });

        await recorder.FailAsync(id, new FailJobRunDto
        {
            ErrorMessage = "Something broke",
            ErrorDetails = "at line 42"
        });

        var run = await _context.JobRuns.FindAsync(id);
        run.Should().NotBeNull();
        run!.Status.Should().Be("Failed");
        run.ErrorMessage.Should().Be("Something broke");
        run.ErrorDetails.Should().Contain("line 42");
        run.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelAsync_UpdatesStatus_ToCancelled()
    {
        var recorder = new JobRunRecorder(_context);
        var id = await recorder.StartAsync(new StartJobRunDto
        {
            JobName = "Cancel Test",
            JobType = "Test",
            TriggerSource = "System"
        });

        await recorder.CancelAsync(id);

        var run = await _context.JobRuns.FindAsync(id);
        run.Should().NotBeNull();
        run!.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task StartAsync_WithParentJobRunId_SetsParentJobRunId()
    {
        var recorder = new JobRunRecorder(_context);
        var parentId = Guid.NewGuid();
        var id = await recorder.StartAsync(new StartJobRunDto
        {
            JobName = "Retry Run",
            JobType = "EmailIngest",
            TriggerSource = "Retry",
            ParentJobRunId = parentId
        });

        var run = await _context.JobRuns.FindAsync(id);
        run.Should().NotBeNull();
        run!.ParentJobRunId.Should().Be(parentId);
        run.TriggerSource.Should().Be("Retry");
    }
}

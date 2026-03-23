using CephasOps.Application.Workflow.Services;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Workflow;

/// <summary>
/// Workflow orchestrator: StartWorkflowAsync creates instance; AdvanceStepAsync updates step.
/// </summary>
public class WorkflowOrchestratorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _dbContext;
    private readonly IWorkflowOrchestrator _orchestrator;

    public WorkflowOrchestratorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _dbContext.Database.EnsureCreated();
        _orchestrator = new WorkflowOrchestratorService(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task StartWorkflowAsync_CreatesInstanceAndFirstStep()
    {
        var dto = await _orchestrator.StartWorkflowAsync(
            "OrderFulfilment",
            "Order",
            Guid.NewGuid(),
            "{\"orderId\":\"123\"}",
            Guid.NewGuid(),
            "corr-1");

        dto.Should().NotBeNull();
        dto.Id.Should().NotBe(Guid.Empty);
        dto.WorkflowType.Should().Be("OrderFulfilment");
        dto.EntityType.Should().Be("Order");
        dto.CurrentStep.Should().Be("Started");
        dto.Status.Should().Be("Running");
        dto.CorrelationId.Should().Be("corr-1");

        var instance = await _orchestrator.GetInstanceAsync(dto.Id);
        instance.Should().NotBeNull();
        instance!.CurrentStep.Should().Be("Started");

        var steps = await _dbContext.WorkflowStepRecords.Where(s => s.WorkflowInstanceId == dto.Id).ToListAsync();
        steps.Should().HaveCount(1);
        steps[0].StepName.Should().Be("Started");
        steps[0].Status.Should().Be("Completed");
    }

    [Fact]
    public async Task AdvanceStepAsync_UpdatesCurrentStepAndAppendsRecord()
    {
        var dto = await _orchestrator.StartWorkflowAsync("Test", "Order", Guid.NewGuid(), null, null, null);
        var instanceId = dto.Id;

        await _orchestrator.AdvanceStepAsync(instanceId, "Step2", "{\"data\":\"x\"}");

        var instance = await _orchestrator.GetInstanceAsync(instanceId);
        instance.Should().NotBeNull();
        instance!.CurrentStep.Should().Be("Step2");

        var steps = await _dbContext.WorkflowStepRecords.Where(s => s.WorkflowInstanceId == instanceId).OrderBy(s => s.StartedAt).ToListAsync();
        steps.Should().HaveCount(2);
        steps[1].StepName.Should().Be("Step2");
        steps[1].PayloadJson.Should().Be("{\"data\":\"x\"}");
        steps[1].Status.Should().Be("Completed");
    }
}

using CephasOps.Application.Workflow.JobOrchestration;
using CephasOps.Domain.Workflow;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Workflow.JobOrchestration;

/// <summary>
/// Phase 4: Operational queryability (summary and list by status).
/// Tenant-scoped JobExecution requires TenantScope before SaveChanges (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class JobExecutionQueryServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly JobExecutionQueryService _queryService;
    private readonly Guid _companyId = Guid.NewGuid();

    public JobExecutionQueryServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "JobExecutionQuery_" + Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _queryService = new JobExecutionQueryService(_context);
    }

    [Fact]
    public async Task GetSummaryAsync_Counts_By_Status()
    {
        var previous = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = _companyId;
            _context.JobExecutions.Add(new JobExecution { CompanyId = _companyId, JobType = "A", Status = JobExecutionStatus.Pending, NextRunAtUtc = null });
            _context.JobExecutions.Add(new JobExecution { CompanyId = _companyId, JobType = "B", Status = JobExecutionStatus.Running });
            _context.JobExecutions.Add(new JobExecution { CompanyId = _companyId, JobType = "C", Status = JobExecutionStatus.Failed, NextRunAtUtc = DateTime.UtcNow.AddHours(1) });
            _context.JobExecutions.Add(new JobExecution { CompanyId = _companyId, JobType = "D", Status = JobExecutionStatus.DeadLetter });
            _context.JobExecutions.Add(new JobExecution { CompanyId = _companyId, JobType = "E", Status = JobExecutionStatus.Succeeded });
            await _context.SaveChangesAsync();
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }

        var summary = await _queryService.GetSummaryAsync();

        summary.PendingCount.Should().Be(1);
        summary.RunningCount.Should().Be(1);
        summary.FailedRetryScheduledCount.Should().Be(1);
        summary.DeadLetterCount.Should().Be(1);
        summary.SucceededCount.Should().Be(1);
    }

    [Fact]
    public async Task GetDeadLetterAsync_Returns_Only_DeadLetter()
    {
        var previous = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = _companyId;
            _context.JobExecutions.Add(new JobExecution { CompanyId = _companyId, JobType = "X", Status = JobExecutionStatus.DeadLetter, LastError = "Poison" });
            _context.JobExecutions.Add(new JobExecution { CompanyId = _companyId, JobType = "Y", Status = JobExecutionStatus.Succeeded });
            await _context.SaveChangesAsync();
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }

        var list = await _queryService.GetDeadLetterAsync(10);

        list.Should().HaveCount(1);
        list[0].JobType.Should().Be("X");
        list[0].LastError.Should().Be("Poison");
    }

    public void Dispose() => _context.Dispose();
}

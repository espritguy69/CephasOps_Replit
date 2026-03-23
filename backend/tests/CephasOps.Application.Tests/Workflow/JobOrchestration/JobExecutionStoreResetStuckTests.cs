using CephasOps.Domain.Workflow;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Workflow.JobOrchestration;

/// <summary>
/// Phase 4: Stale-lease recovery (ResetStuckRunningAsync). Tenant-scoped (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class JobExecutionStoreResetStuckTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly JobExecutionStore _store;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public JobExecutionStoreResetStuckTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "JobExecutionResetStuck_" + Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _store = new JobExecutionStore(_context);
    }

    [Fact]
    public async Task ResetStuckRunningAsync_Resets_Expired_Lease_To_Pending()
    {
        var job = new JobExecution
        {
            CompanyId = _companyId,
            JobType = "Test",
            PayloadJson = "{}",
            Status = JobExecutionStatus.Running,
            ProcessingNodeId = "node1",
            ProcessingLeaseExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5),
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-10)
        };
        _context.JobExecutions.Add(job);
        await _context.SaveChangesAsync();

        var count = await _store.ResetStuckRunningAsync(TimeSpan.FromMinutes(15), CancellationToken.None);

        count.Should().Be(1);
        var updated = await _context.JobExecutions.FirstAsync(j => j.Id == job.Id);
        updated.Status.Should().Be(JobExecutionStatus.Pending);
        updated.ProcessingNodeId.Should().BeNull();
        updated.ProcessingLeaseExpiresAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task ResetStuckRunningAsync_Leaves_Active_Lease_Unchanged()
    {
        var job = new JobExecution
        {
            CompanyId = _companyId,
            JobType = "Test",
            PayloadJson = "{}",
            Status = JobExecutionStatus.Running,
            ProcessingNodeId = "node1",
            ProcessingLeaseExpiresAtUtc = DateTime.UtcNow.AddMinutes(10)
        };
        _context.JobExecutions.Add(job);
        await _context.SaveChangesAsync();

        var count = await _store.ResetStuckRunningAsync(TimeSpan.FromMinutes(15), CancellationToken.None);

        count.Should().Be(0);
        var unchanged = await _context.JobExecutions.FirstAsync(j => j.Id == job.Id);
        unchanged.Status.Should().Be(JobExecutionStatus.Running);
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
    }
}

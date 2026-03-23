using CephasOps.Application.Workflow.JobObservability;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Workflow;

public class JobRunRetentionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public JobRunRetentionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "JobRunRetention_" + Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task PurgeAsync_DeletesOnlyCompletedOlderThanCutoff()
    {
        var now = DateTime.UtcNow;
        var old = now.AddDays(-100);
        var recent = now.AddDays(-5);

        _context.JobRuns.Add(new JobRun
        {
            Id = Guid.NewGuid(),
            JobType = "Test",
            JobName = "Test",
            Status = "Succeeded",
            StartedAtUtc = old,
            CompletedAtUtc = old.AddMinutes(1),
            CreatedAtUtc = old,
            UpdatedAtUtc = old
        });
        _context.JobRuns.Add(new JobRun
        {
            Id = Guid.NewGuid(),
            JobType = "Test",
            JobName = "Test",
            Status = "Running",
            StartedAtUtc = old,
            CompletedAtUtc = null,
            CreatedAtUtc = old,
            UpdatedAtUtc = now
        });
        _context.JobRuns.Add(new JobRun
        {
            Id = Guid.NewGuid(),
            JobType = "Test",
            JobName = "Test",
            Status = "Succeeded",
            StartedAtUtc = recent,
            CompletedAtUtc = recent.AddMinutes(1),
            CreatedAtUtc = recent,
            UpdatedAtUtc = recent
        });
        await _context.SaveChangesAsync();

        var cutoff = now.AddDays(-90);
        var service = new JobRunRetentionService(_context);
        var deleted = await service.PurgeAsync(cutoff, 1000);

        deleted.Should().Be(1);
        var remaining = await _context.JobRuns.ToListAsync();
        remaining.Should().HaveCount(2);
        remaining.Should().OnlyContain(r => r.CompletedAtUtc == null || r.CompletedAtUtc >= cutoff);
    }

    [Fact]
    public async Task PurgeAsync_RespectsBatchSize()
    {
        var now = DateTime.UtcNow;
        var old = now.AddDays(-200);
        for (var i = 0; i < 5; i++)
        {
            _context.JobRuns.Add(new JobRun
            {
                Id = Guid.NewGuid(),
                JobType = "Test",
                JobName = "Test",
                Status = "Succeeded",
                StartedAtUtc = old,
                CompletedAtUtc = old.AddMinutes(1),
                CreatedAtUtc = old,
                UpdatedAtUtc = old
            });
        }
        await _context.SaveChangesAsync();

        var service = new JobRunRetentionService(_context);
        var deleted = await service.PurgeAsync(now.AddDays(-90), batchSize: 2);
        deleted.Should().Be(5);
        (await _context.JobRuns.CountAsync()).Should().Be(0);
    }
}

using CephasOps.Application.Workflow.JobObservability;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Workflow;

public class JobDefinitionProviderTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public JobDefinitionProviderTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "JobDefinitionProvider_" + Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task GetByJobTypeAsync_ReturnsDefault_WhenNotInDb()
    {
        var provider = new JobDefinitionProvider(_context);
        var def = await provider.GetByJobTypeAsync("EmailIngest");
        def.Should().NotBeNull();
        def!.JobType.Should().Be("EmailIngest");
        def.DisplayName.Should().Be("Email Ingest");
        def.RetryAllowed.Should().BeTrue();
        def.MaxRetries.Should().Be(3);
        def.DefaultStuckThresholdSeconds.Should().Be(600);
    }

    [Fact]
    public async Task GetByJobTypeAsync_ReturnsDefault_ForSlaEvaluation()
    {
        var provider = new JobDefinitionProvider(_context);
        var def = await provider.GetByJobTypeAsync("slaevaluation");
        def.Should().NotBeNull();
        def!.JobType.Should().Be("slaevaluation");
        def.DisplayName.Should().Be("SLA Evaluation");
        def.RetryAllowed.Should().BeTrue();
        def.MaxRetries.Should().Be(2);
    }

    [Fact]
    public async Task GetByJobTypeAsync_ReturnsNull_ForUnknownJobType()
    {
        var provider = new JobDefinitionProvider(_context);
        var def = await provider.GetByJobTypeAsync("UnknownJobType123");
        def.Should().BeNull();
    }

    [Fact]
    public async Task GetByJobTypeAsync_ReturnsFromDb_WhenExists()
    {
        _context.JobDefinitions.Add(new JobDefinition
        {
            Id = Guid.NewGuid(),
            JobType = "CustomJob",
            DisplayName = "Custom Job",
            RetryAllowed = false,
            MaxRetries = 0,
            DefaultStuckThresholdSeconds = 120,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var provider = new JobDefinitionProvider(_context);
        var def = await provider.GetByJobTypeAsync("CustomJob");
        def.Should().NotBeNull();
        def!.DisplayName.Should().Be("Custom Job");
        def.RetryAllowed.Should().BeFalse();
        def.DefaultStuckThresholdSeconds.Should().Be(120);
    }

    [Fact]
    public async Task GetAllAsync_IncludesDefaultsAndDb()
    {
        _context.JobDefinitions.Add(new JobDefinition
        {
            Id = Guid.NewGuid(),
            JobType = "DbOnlyJob",
            DisplayName = "DB Only",
            RetryAllowed = true,
            MaxRetries = 2,
            DefaultStuckThresholdSeconds = 300,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var provider = new JobDefinitionProvider(_context);
        var all = await provider.GetAllAsync();
        all.Should().NotBeEmpty();
        all.Select(d => d.JobType).Should().Contain("DbOnlyJob");
        all.Select(d => d.JobType).Should().Contain("EmailIngest");
    }
}

using CephasOps.Application.Integration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CephasOps.Infrastructure.Persistence;
using Xunit;

namespace CephasOps.Application.Tests.Integration;

public class EventPlatformRetentionResultTests
{
    [Fact]
    public void TotalDeleted_sums_all_category_counts()
    {
        var result = new EventPlatformRetentionResult
        {
            EventStoreDeleted = 10,
            EventProcessingLogDeleted = 20,
            OutboundDeliveriesDeleted = 5,
            InboundReceiptsDeleted = 3,
            ExternalIdempotencyDeleted = 50
        };
        result.TotalDeleted.Should().Be(88);
    }

    [Fact]
    public void TotalDeleted_when_all_zero_is_zero()
    {
        var result = new EventPlatformRetentionResult();
        result.TotalDeleted.Should().Be(0);
    }
}

public class EventPlatformRetentionOptionsTests
{
    [Fact]
    public void Defaults_match_documentation()
    {
        var opts = new EventPlatformRetentionOptions();
        opts.EventStoreProcessedAndDeadLetterDays.Should().Be(90);
        opts.EventProcessingLogCompletedDays.Should().Be(90);
        opts.OutboundDeliveredDays.Should().Be(60);
        opts.InboundProcessedDays.Should().Be(90);
        opts.ExternalIdempotencyCompletedDays.Should().Be(7);
        opts.MaxDeletesPerTablePerRun.Should().Be(1000);
        opts.Enabled.Should().BeTrue();
        opts.RunIntervalSeconds.Should().Be(86400);
    }

    [Fact]
    public void SectionName_is_EventPlatformRetention()
    {
        EventPlatformRetentionOptions.SectionName.Should().Be("EventPlatformRetention");
    }
}

public class EventPlatformRetentionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public EventPlatformRetentionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "EventPlatformRetention_" + Guid.NewGuid().ToString("N"))
            .Options;
        _context = new ApplicationDbContext(options);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task RunRetentionAsync_with_all_days_zero_does_not_throw_and_returns_zero_deleted()
    {
        var opts = new EventPlatformRetentionOptions
        {
            EventStoreProcessedAndDeadLetterDays = 0,
            EventProcessingLogCompletedDays = 0,
            OutboundDeliveredDays = 0,
            InboundProcessedDays = 0,
            ExternalIdempotencyCompletedDays = 0
        };
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<EventPlatformRetentionService>();
        var service = new EventPlatformRetentionService(_context, Options.Create(opts), logger);
        var result = await service.RunRetentionAsync();
        result.TotalDeleted.Should().Be(0);
        result.Errors.Should().BeEmpty();
        (result.RunCompletedAtUtc >= result.RunStartedAtUtc).Should().BeTrue();
    }
}

using CephasOps.Application.Events;
using CephasOps.Application.Events.Backpressure;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CephasOps.Application.Tests.Events;

/// <summary>
/// Phase 8: Adaptive backpressure service.
/// </summary>
public class Phase8BackpressureTests
{
    [Fact]
    public void GetState_WhenSnapshotNull_ReturnsNone()
    {
        var options = Options.Create(new EventBusDispatcherOptions());
        var backpressureOptions = Options.Create(new BackpressureOptions());
        var service = new EventBusBackpressureService(options, backpressureOptions, snapshot: null);
        var state = service.GetState();
        state.Level.Should().Be(BackpressureLevel.None);
        state.PendingCount.Should().Be(0);
    }

    [Fact]
    public void GetSuggestedBatchSize_WhenNone_ReturnsNull()
    {
        var options = Options.Create(new EventBusDispatcherOptions());
        var service = new EventBusBackpressureService(options, null, null);
        service.GetSuggestedBatchSize(20).Should().BeNull();
    }

    [Fact]
    public void GetSuggestedBatchSize_WhenPaused_ReturnsZero()
    {
        var snapshot = new EventBusMetricsSnapshot();
        snapshot.Update(25000, 0, 0, 0); // above PausedPendingThreshold (20000)
        var options = Options.Create(new EventBusDispatcherOptions());
        var backpressureOptions = Options.Create(new BackpressureOptions());
        var service = new EventBusBackpressureService(options, backpressureOptions, snapshot);
        var state = service.GetState();
        state.Level.Should().Be(BackpressureLevel.Paused);
        service.GetSuggestedBatchSize(20).Should().Be(0);
    }

    [Fact]
    public void GetSuggestedDelayMs_WhenNone_ReturnsZero()
    {
        var service = new EventBusBackpressureService(Options.Create(new EventBusDispatcherOptions()), null, null);
        service.GetSuggestedDelayMs().Should().Be(0);
    }

    [Fact]
    public void GetSuggestedDelayMs_WhenReduced_ReturnsReducedDelay()
    {
        var snapshot = new EventBusMetricsSnapshot();
        snapshot.Update(1500, 0, 0, 0); // above ReducedPendingThreshold (1000)
        var options = Options.Create(new EventBusDispatcherOptions());
        var backpressureOptions = Options.Create(new BackpressureOptions { ReducedDelayMs = 500 });
        var service = new EventBusBackpressureService(options, backpressureOptions, snapshot);
        service.GetState().Level.Should().Be(BackpressureLevel.Reduced);
        service.GetSuggestedDelayMs().Should().Be(500);
    }
}

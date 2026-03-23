using CephasOps.Application.Events.DTOs;
using CephasOps.Application.Events.Replay;
using FluentAssertions;
using Xunit;

namespace CephasOps.Application.Tests.Events;

public class OperationalReplayPolicyTests
{
    private readonly IEventReplayPolicy _singleEventPolicy = new EventReplayPolicy();
    private readonly IOperationalReplayPolicy _policy = new OperationalReplayPolicy(new EventReplayPolicy());

    [Fact]
    public void CheckEligibility_WorkflowTransitionCompleted_WithinWindow_Eligible()
    {
        var entry = new ReplayEligibilityInputDto
        {
            EventId = Guid.NewGuid(),
            EventType = "WorkflowTransitionCompleted",
            CompanyId = Guid.NewGuid(),
            OccurredAtUtc = DateTime.UtcNow.AddDays(-7)
        };
        var request = new ReplayRequestDto();
        var result = _policy.CheckEligibility(entry, request, DateTime.UtcNow);
        result.Eligible.Should().BeTrue();
        result.BlockedReason.Should().BeNullOrEmpty();
    }

    [Fact]
    public void CheckEligibility_UnknownEventType_Blocked()
    {
        var entry = new ReplayEligibilityInputDto
        {
            EventId = Guid.NewGuid(),
            EventType = "NonExistentEventType",
            CompanyId = Guid.NewGuid(),
            OccurredAtUtc = DateTime.UtcNow.AddDays(-1)
        };
        var request = new ReplayRequestDto();
        var result = _policy.CheckEligibility(entry, request, DateTime.UtcNow);
        result.Eligible.Should().BeFalse();
        result.BlockedReason.Should().Contain("not allowed for replay");
    }

    [Fact]
    public void CheckEligibility_OutsideReplayWindow_Blocked()
    {
        var entry = new ReplayEligibilityInputDto
        {
            EventId = Guid.NewGuid(),
            EventType = "WorkflowTransitionCompleted",
            CompanyId = Guid.NewGuid(),
            OccurredAtUtc = DateTime.UtcNow.AddDays(-60)
        };
        var request = new ReplayRequestDto();
        var result = _policy.CheckEligibility(entry, request, DateTime.UtcNow);
        result.Eligible.Should().BeFalse();
        result.BlockedReason.Should().Contain("replay window");
    }

    [Fact]
    public void IsDestructiveEventType_Unknown_ReturnsFalse()
    {
        _policy.IsDestructiveEventType("WorkflowTransitionCompleted").Should().BeFalse();
    }

    [Fact]
    public void IsCompanyBlocked_AnyCompany_ReturnsFalse()
    {
        _policy.IsCompanyBlocked(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void MaxReplayWindowDays_ReturnsValue()
    {
        _policy.MaxReplayWindowDays.Should().HaveValue();
        _policy.MaxReplayWindowDays!.Value.Should().BePositive();
    }

    [Fact]
    public void MaxReplayCountPerRequest_ReturnsValue()
    {
        _policy.MaxReplayCountPerRequest.Should().HaveValue();
        _policy.MaxReplayCountPerRequest!.Value.Should().BePositive();
    }
}

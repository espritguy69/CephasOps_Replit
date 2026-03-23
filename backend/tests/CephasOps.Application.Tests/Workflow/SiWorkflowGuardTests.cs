using CephasOps.Application.Workflow;
using CephasOps.Domain.Orders.Enums;
using FluentAssertions;
using Xunit;

namespace CephasOps.Application.Tests.Workflow;

/// <summary>
/// SI workflow guard: valid transitions succeed, invalid jumps and completion without MetCustomer fail.
/// </summary>
public class SiWorkflowGuardTests
{
    [Theory]
    [InlineData(OrderStatus.Assigned, OrderStatus.OnTheWay)]
    [InlineData(OrderStatus.OnTheWay, OrderStatus.MetCustomer)]
    [InlineData(OrderStatus.MetCustomer, OrderStatus.OrderCompleted)]
    [InlineData(OrderStatus.Pending, OrderStatus.Assigned)]
    [InlineData(OrderStatus.Assigned, OrderStatus.Blocker)]
    [InlineData(OrderStatus.Assigned, OrderStatus.ReschedulePendingApproval)]
    [InlineData(OrderStatus.Blocker, OrderStatus.Assigned)]
    [InlineData(OrderStatus.ReschedulePendingApproval, OrderStatus.Assigned)]
    [InlineData(OrderStatus.OrderCompleted, OrderStatus.DocketsReceived)]
    [InlineData(OrderStatus.SubmittedToPortal, OrderStatus.Completed)]
    public void RequireValidOrderTransition_AllowedTransitions_DoesNotThrow(string from, string to)
    {
        var act = () => SiWorkflowGuard.RequireValidOrderTransition(from, to);
        act.Should().NotThrow();
        SiWorkflowGuard.IsAllowedOrderTransition(from, to).Should().BeTrue();
    }

    [Theory]
    [InlineData(OrderStatus.Assigned, OrderStatus.Completed)]
    [InlineData(OrderStatus.Assigned, OrderStatus.OrderCompleted)]
    [InlineData(OrderStatus.Assigned, OrderStatus.MetCustomer)]
    [InlineData(OrderStatus.OnTheWay, OrderStatus.Completed)]
    [InlineData(OrderStatus.OnTheWay, OrderStatus.OrderCompleted)]
    public void RequireValidOrderTransition_InvalidJumps_Throws(string from, string to)
    {
        var act = () => SiWorkflowGuard.RequireValidOrderTransition(from, to);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*invalid transition*")
            .Which.Message.Should().Contain(from).And.Contain(to);
        SiWorkflowGuard.IsAllowedOrderTransition(from, to).Should().BeFalse();
    }

    [Fact]
    public void RequireValidOrderTransition_AssignedToCompleted_ThrowsWithAllowedList()
    {
        var act = () => SiWorkflowGuard.RequireValidOrderTransition(OrderStatus.Assigned, OrderStatus.Completed);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Assigned*Completed*")
            .Which.Message.Should().Contain("Allowed next statuses");
    }

    [Fact]
    public void RequireValidOrderTransition_CompleteWithoutMetCustomer_Throws()
    {
        // OrderCompleted only allowed from MetCustomer; Completed only from SubmittedToPortal
        var act = () => SiWorkflowGuard.RequireValidOrderTransition(OrderStatus.Assigned, OrderStatus.OrderCompleted);
        act.Should().Throw<InvalidOperationException>();
        act = () => SiWorkflowGuard.RequireValidOrderTransition(OrderStatus.OnTheWay, OrderStatus.OrderCompleted);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RequireValidOrderTransition_EmptyCurrent_Throws()
    {
        var act = () => SiWorkflowGuard.RequireValidOrderTransition("", OrderStatus.OnTheWay);
        act.Should().Throw<InvalidOperationException>().WithMessage("*current status*");
    }

    [Fact]
    public void RequireValidOrderTransition_EmptyTarget_Throws()
    {
        var act = () => SiWorkflowGuard.RequireValidOrderTransition(OrderStatus.Assigned, "");
        act.Should().Throw<InvalidOperationException>().WithMessage("*target status*");
    }

    [Fact]
    public void RequireValidOrderTransition_SameStatus_Throws()
    {
        var act = () => SiWorkflowGuard.RequireValidOrderTransition(OrderStatus.Assigned, OrderStatus.Assigned);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Assigned*Assigned*");
        SiWorkflowGuard.IsAllowedOrderTransition(OrderStatus.Assigned, OrderStatus.Assigned).Should().BeFalse();
    }

    [Fact]
    public void RequireRescheduleReason_WhenReasonProvided_DoesNotThrow()
    {
        var act = () => SiWorkflowGuard.RequireRescheduleReason("Customer requested later date");
        act.Should().NotThrow();
        act = () => SiWorkflowGuard.RequireRescheduleReason("Building issue", "Reschedule");
        act.Should().NotThrow();
    }

    [Fact]
    public void RequireRescheduleReason_WhenReasonNull_Throws()
    {
        var act = () => SiWorkflowGuard.RequireRescheduleReason(null);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Reschedule reason is required*");
    }

    [Fact]
    public void RequireRescheduleReason_WhenReasonWhitespace_Throws()
    {
        var act = () => SiWorkflowGuard.RequireRescheduleReason("   ");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Reschedule reason is required*");
    }
}

using CephasOps.Application.Events.DTOs;
using CephasOps.Application.Events.Ledger.DTOs;

namespace CephasOps.Application.Events.Ledger;

public sealed class LedgerFamilyRegistry : ILedgerFamilyRegistry
{
    private static readonly IReadOnlyList<LedgerFamilyDescriptorDto> Families = new List<LedgerFamilyDescriptorDto>
    {
        new()
        {
            Id = LedgerFamilies.WorkflowTransition,
            DisplayName = "Workflow transition",
            Description = "Workflow transition completed (from WorkflowTransitionCompletedEvent). Entity is typically Order or other workflow-driven entity.",
            OrderingStrategyId = OrderingStrategies.OccurredAtUtcAscendingEventIdAscending,
            OrderingGuaranteeLevel = OrderingGuaranteeLevels.StrongDeterministic
        },
        new()
        {
            Id = LedgerFamilies.ReplayOperationCompleted,
            DisplayName = "Replay operation completed",
            Description = "Replay operation reached terminal state (Completed, Cancelled, or Failed).",
            OrderingStrategyId = OrderingStrategies.OccurredAtUtcAscendingEventIdAscending,
            OrderingGuaranteeLevel = OrderingGuaranteeLevels.StrongDeterministic
        },
        new()
        {
            Id = LedgerFamilies.OrderLifecycle,
            DisplayName = "Order lifecycle",
            Description = "Order status changes (from OrderStatusChangedEvent). Category: StatusChanged.",
            OrderingStrategyId = OrderingStrategies.OccurredAtUtcAscendingEventIdAscending,
            OrderingGuaranteeLevel = OrderingGuaranteeLevels.StrongDeterministic
        }
    };

    public IReadOnlyList<LedgerFamilyDescriptorDto> GetAll() => Families;
}

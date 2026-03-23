using CephasOps.Domain.Common;

namespace CephasOps.Domain.Orders.Entities;

/// <summary>
/// Order status log entity - tracks all status changes
/// </summary>
public class OrderStatusLog : CompanyScopedEntity
{
    public Guid OrderId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? TransitionReason { get; set; }
    public Guid? TriggeredByUserId { get; set; }
    public Guid? TriggeredBySiId { get; set; }
    public string Source { get; set; } = string.Empty; // SIApp, AdminPortal, Scheduler, System, Parser
    public string? MetadataJson { get; set; } // GPS, device info, etc.
}


using CephasOps.Domain.Common;

namespace CephasOps.Domain.RMA.Entities;

/// <summary>
/// RMA request item entity
/// </summary>
public class RmaRequestItem : CompanyScopedEntity
{
    /// <summary>
    /// RMA request ID
    /// </summary>
    public Guid RmaRequestId { get; set; }

    /// <summary>
    /// Serialised item ID
    /// </summary>
    public Guid SerialisedItemId { get; set; }

    /// <summary>
    /// Original order ID (if applicable)
    /// </summary>
    public Guid? OriginalOrderId { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Result (Repaired, Replaced, Credited, Scrapped)
    /// </summary>
    public string? Result { get; set; }

    // Navigation properties
    public RmaRequest? RmaRequest { get; set; }
}


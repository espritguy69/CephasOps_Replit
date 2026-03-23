using CephasOps.Domain.Common;

namespace CephasOps.Domain.RMA.Entities;

/// <summary>
/// RMA request entity
/// </summary>
public class RmaRequest : CompanyScopedEntity
{
    /// <summary>
    /// Partner ID
    /// </summary>
    public Guid PartnerId { get; set; }

    /// <summary>
    /// RMA number from partner (if provided)
    /// </summary>
    public string? RmaNumber { get; set; }

    /// <summary>
    /// Request date
    /// </summary>
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Reason for RMA
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Status (Requested, InTransit, Closed)
    /// </summary>
    public string Status { get; set; } = "Requested";

    /// <summary>
    /// MRA document file ID (if attached)
    /// </summary>
    public Guid? MraDocumentId { get; set; }

    /// <summary>
    /// User ID who created this request
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    // Navigation properties
    public ICollection<RmaRequestItem> Items { get; set; } = new List<RmaRequestItem>();
}


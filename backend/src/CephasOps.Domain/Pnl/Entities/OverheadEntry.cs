using CephasOps.Domain.Common;

namespace CephasOps.Domain.Pnl.Entities;

/// <summary>
/// Overhead entry entity (indirect costs)
/// </summary>
public class OverheadEntry : CompanyScopedEntity
{
    /// <summary>
    /// Cost centre ID
    /// </summary>
    public Guid CostCentreId { get; set; }

    /// <summary>
    /// Period (e.g., "2025-01")
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// Amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Allocation basis (e.g., percentage to ISP vs Barbershop)
    /// </summary>
    public string? AllocationBasis { get; set; }

    /// <summary>
    /// User ID who created this entry
    /// </summary>
    public Guid CreatedByUserId { get; set; }
}


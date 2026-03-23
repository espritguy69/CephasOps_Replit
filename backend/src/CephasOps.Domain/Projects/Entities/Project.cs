using CephasOps.Domain.Common;

namespace CephasOps.Domain.Projects.Entities;

/// <summary>
/// Project entity for managing projects (Solar, ISP installations, etc.)
/// </summary>
public class Project : CompanyScopedEntity
{
    /// <summary>
    /// Project code (unique identifier)
    /// </summary>
    public string ProjectCode { get; set; } = string.Empty;

    /// <summary>
    /// Project name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Project description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Partner ID (if project is for a partner)
    /// </summary>
    public Guid? PartnerId { get; set; }

    /// <summary>
    /// Department ID
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Cost Centre ID
    /// </summary>
    public Guid? CostCentreId { get; set; }

    /// <summary>
    /// Project type: Solar, ISP, GPON, Maintenance, Other
    /// </summary>
    public string ProjectType { get; set; } = "Other";

    /// <summary>
    /// Status: Planning, InProgress, OnHold, Completed, Cancelled
    /// </summary>
    public string Status { get; set; } = "Planning";

    /// <summary>
    /// Customer name
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Customer phone
    /// </summary>
    public string? CustomerPhone { get; set; }

    /// <summary>
    /// Customer email
    /// </summary>
    public string? CustomerEmail { get; set; }

    /// <summary>
    /// Site address
    /// </summary>
    public string? SiteAddress { get; set; }

    /// <summary>
    /// City
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// State
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Postcode
    /// </summary>
    public string? Postcode { get; set; }

    /// <summary>
    /// GPS coordinates
    /// </summary>
    public string? GpsCoordinates { get; set; }

    /// <summary>
    /// Planned start date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Planned end date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Actual start date
    /// </summary>
    public DateTime? ActualStartDate { get; set; }

    /// <summary>
    /// Actual end date
    /// </summary>
    public DateTime? ActualEndDate { get; set; }

    /// <summary>
    /// Budget amount
    /// </summary>
    public decimal? BudgetAmount { get; set; }

    /// <summary>
    /// Contract value
    /// </summary>
    public decimal? ContractValue { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "MYR";

    /// <summary>
    /// Project manager user ID
    /// </summary>
    public Guid? ProjectManagerId { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// User ID who created this project
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    // Navigation properties
    public virtual ICollection<BoqItem> BoqItems { get; set; } = new List<BoqItem>();
}

/// <summary>
/// Bill of Quantities (BOQ) item for projects
/// </summary>
public class BoqItem : CompanyScopedEntity
{
    /// <summary>
    /// Parent project ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Material ID (if material item)
    /// </summary>
    public Guid? MaterialId { get; set; }

    /// <summary>
    /// Line number for ordering
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Section / category (e.g., "Civil Works", "Electrical", "Materials")
    /// </summary>
    public string? Section { get; set; }

    /// <summary>
    /// Item type: Material, Labor, Equipment, Subcontract, Other
    /// </summary>
    public string ItemType { get; set; } = "Material";

    /// <summary>
    /// Item description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// SKU / Part number
    /// </summary>
    public string? Sku { get; set; }

    /// <summary>
    /// Unit of measure
    /// </summary>
    public string Unit { get; set; } = "pcs";

    /// <summary>
    /// Quantity
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit rate / price
    /// </summary>
    public decimal UnitRate { get; set; }

    /// <summary>
    /// Line total
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Markup percentage (for selling price calculation)
    /// </summary>
    public decimal MarkupPercent { get; set; }

    /// <summary>
    /// Selling price (after markup)
    /// </summary>
    public decimal SellingPrice { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this item is optional
    /// </summary>
    public bool IsOptional { get; set; }

    // Navigation
    public virtual Project? Project { get; set; }
}


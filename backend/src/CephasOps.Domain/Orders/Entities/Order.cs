using CephasOps.Domain.Buildings.Entities;
using CephasOps.Domain.Common;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Orders.Enums;

namespace CephasOps.Domain.Orders.Entities;

/// <summary>
/// Order entity - represents a work order/job
/// </summary>
public class Order : CompanyScopedEntity
{
    public Guid PartnerId { get; set; }
    /// <summary>Navigation to Partner (for DTO projection; not persisted as composite).</summary>
    public virtual Partner? Partner { get; set; }
    public string SourceSystem { get; set; } = string.Empty; // EmailParser, Manual, API, Import
    public Guid? SourceEmailId { get; set; }
    public Guid OrderTypeId { get; set; }
    
    /// <summary>
    /// Order Category ID (FTTH, FTTO, FTTR, FTTC) - represents the service/technology category
    /// Only used for Activation orders. Previously known as InstallationTypeId.
    /// </summary>
    public Guid? OrderCategoryId { get; set; }
    /// <summary>Navigation to OrderCategory (for DTO projection).</summary>
    public virtual OrderCategory? OrderCategory { get; set; }
    
    /// <summary>
    /// Installation method ID (Prelaid, Non-Prelaid, etc.) - represents site condition/installation method
    /// Required for rate keying. Per GPON_RATECARDS.md: OrderType + OrderCategory + InstallationMethod
    /// </summary>
    public Guid? InstallationMethodId { get; set; }
    /// <summary>Navigation to InstallationMethod (for DTO projection).</summary>
    public virtual InstallationMethod? InstallationMethod { get; set; }
    
    /// <summary>
    /// Service ID type: TBBN for TIME direct customers, PartnerServiceId for wholesale partners
    /// </summary>
    public ServiceIdType? ServiceIdType { get; set; }
    
    /// <summary>
    /// Service ID value (TBBN or Partner Service ID)
    /// </summary>
    public string ServiceId { get; set; } = string.Empty;
    public string? TicketId { get; set; }
    public string? AwoNumber { get; set; }
    public string? ExternalRef { get; set; }
    public string Status { get; set; } = "Pending";
    public string? StatusReason { get; set; }
    public string? Priority { get; set; } // Low, Normal, High, Critical
    public Guid BuildingId { get; set; }
    public string? BuildingName { get; set; }
    public string? UnitNo { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Postcode { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    
    /// <summary>
    /// Old/Previous address - required for Modification/Relocation orders (Outdoor)
    /// </summary>
    public string? OldAddress { get; set; }
    
    /// <summary>
    /// Relocation type: "Indoor" (same unit, different room) or "Outdoor" (different address)
    /// Per ORDERS_MODULE.md section 7
    /// </summary>
    public string? RelocationType { get; set; }
    
    /// <summary>
    /// For Indoor Relocation: the old location within the unit (e.g., "Bedroom 3")
    /// Per ORDERS_MODULE.md section 7.1
    /// </summary>
    public string? OldLocationNote { get; set; }
    
    /// <summary>
    /// For Indoor Relocation: the new location within the unit (e.g., "Living Hall")
    /// Per ORDERS_MODULE.md section 7.1
    /// </summary>
    public string? NewLocationNote { get; set; }
    
    /// <summary>
    /// Package/Plan name from partner (e.g., "TIME Fibre 600Mbps Home Broadband")
    /// </summary>
    public string? PackageName { get; set; }
    
    /// <summary>
    /// Bandwidth (e.g., "600 Mbps")
    /// </summary>
    public string? Bandwidth { get; set; }
    
    /// <summary>
    /// ONU Serial Number
    /// </summary>
    public string? OnuSerialNumber { get; set; }
    
    /// <summary>
    /// ONU Password (encrypted) - for Modification Outdoor orders
    /// </summary>
    public string? OnuPasswordEncrypted { get; set; }
    
    /// <summary>
    /// VOIP Service ID if applicable
    /// </summary>
    public string? VoipServiceId { get; set; }
    
    // Network Info fields (FIBER INTERNET section)
    /// <summary>
    /// Network package/plan name (multi-line)
    /// </summary>
    public string? NetworkPackage { get; set; }
    
    /// <summary>
    /// Network bandwidth (e.g., "600 Mbps")
    /// </summary>
    public string? NetworkBandwidth { get; set; }
    
    /// <summary>
    /// Network login ID
    /// </summary>
    public string? NetworkLoginId { get; set; }
    
    /// <summary>
    /// Network password (masked in UI)
    /// </summary>
    public string? NetworkPassword { get; set; }
    
    /// <summary>
    /// WAN IP address
    /// </summary>
    public string? NetworkWanIp { get; set; }
    
    /// <summary>
    /// LAN IP address
    /// </summary>
    public string? NetworkLanIp { get; set; }
    
    /// <summary>
    /// Gateway IP address
    /// </summary>
    public string? NetworkGateway { get; set; }
    
    /// <summary>
    /// Subnet mask
    /// </summary>
    public string? NetworkSubnetMask { get; set; }
    
    // VOIP fields (VOIP section)
    /// <summary>
    /// VOIP password (split from "Service ID / Password" format)
    /// </summary>
    public string? VoipPassword { get; set; }
    
    /// <summary>
    /// VOIP ONU IP address
    /// </summary>
    public string? VoipIpAddressOnu { get; set; }
    
    /// <summary>
    /// VOIP ONU Gateway
    /// </summary>
    public string? VoipGatewayOnu { get; set; }
    
    /// <summary>
    /// VOIP ONU Subnet Mask
    /// </summary>
    public string? VoipSubnetMaskOnu { get; set; }
    
    /// <summary>
    /// VOIP SRP IP address
    /// </summary>
    public string? VoipIpAddressSrp { get; set; }
    
    /// <summary>
    /// VOIP remarks/notes (multi-line)
    /// </summary>
    public string? VoipRemarks { get; set; }
    
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerPhone2 { get; set; }
    public string? CustomerEmail { get; set; }
    
    /// <summary>
    /// Issue description for Assurance orders (e.g., "Link Down", "LOSi", "LOBi")
    /// Extracted from email/parser
    /// </summary>
    public string? Issue { get; set; }
    
    /// <summary>
    /// Solution/resolution for Assurance orders
    /// Entered by SI/Admin after meeting customer (status >= MetCustomer)
    /// </summary>
    public string? Solution { get; set; }
    
    public string? OrderNotesInternal { get; set; }
    public string? PartnerNotes { get; set; }
    public DateTime? RequestedAppointmentAt { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan AppointmentWindowFrom { get; set; }
    public TimeSpan AppointmentWindowTo { get; set; }
    public Guid? AssignedSiId { get; set; }
    public Guid? AssignedTeamId { get; set; }
    public string? KpiCategory { get; set; }
    public DateTime? KpiDueAt { get; set; }
    public DateTime? KpiBreachedAt { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool HasReschedules { get; set; }
    public int RescheduleCount { get; set; }
    public bool DocketUploaded { get; set; }
    
    /// <summary>
    /// Docket number - unique identifier for the physical docket
    /// Per ORDER_LIFECYCLE.md: Docket must be tracked for invoice submission
    /// </summary>
    public string? DocketNumber { get; set; }
    
    public bool PhotosUploaded { get; set; }
    public bool SerialsValidated { get; set; }
    public bool InvoiceEligible { get; set; }
    
    /// <summary>
    /// Splitter number used for this order
    /// Required before Docket Verification
    /// </summary>
    public string? SplitterNumber { get; set; }
    
    /// <summary>
    /// Splitter location (e.g., "MDF Level 1", "Riser Room Floor 5")
    /// Required before Docket Verification
    /// </summary>
    public string? SplitterLocation { get; set; }
    
    /// <summary>
    /// Splitter port number used
    /// Required before Docket Verification
    /// </summary>
    public string? SplitterPort { get; set; }
    
    /// <summary>
    /// Splitter ID reference (if linked to Splitter entity)
    /// </summary>
    public Guid? SplitterId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public string? PnlPeriod { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledByUserId { get; set; }

    // Navigation properties
    /// <summary>
    /// Status change history for this order
    /// </summary>
    public virtual ICollection<OrderStatusLog> StatusLogs { get; set; } = new List<OrderStatusLog>();

    /// <summary>
    /// Reschedule history for this order
    /// </summary>
    public virtual ICollection<OrderReschedule> Reschedules { get; set; } = new List<OrderReschedule>();

    /// <summary>
    /// Blockers raised for this order
    /// </summary>
    public virtual ICollection<OrderBlocker> Blockers { get; set; } = new List<OrderBlocker>();

    /// <summary>
    /// Serialised material replacements (RMA) for Assurance orders
    /// Per INVENTORY_AND_RMA_MODULE.md: Requires TIME approval
    /// </summary>
    public virtual ICollection<OrderMaterialReplacement> MaterialReplacements { get; set; } = new List<OrderMaterialReplacement>();

    /// <summary>
    /// Non-serialised material replacements for Assurance orders
    /// Per INVENTORY_AND_RMA_MODULE.md: No approval required
    /// </summary>
    public virtual ICollection<OrderNonSerialisedReplacement> NonSerialisedReplacements { get; set; } = new List<OrderNonSerialisedReplacement>();
}


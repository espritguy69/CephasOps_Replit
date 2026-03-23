using CephasOps.Application.Parser.DTOs;
using CephasOps.Domain.Orders.Enums;

namespace CephasOps.Application.Orders.DTOs;

/// <summary>
/// Order DTO
/// </summary>
public class OrderDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public Guid PartnerId { get; set; }
    /// <summary>Partner short code (e.g. TIME). For display and derived label.</summary>
    public string? PartnerCode { get; set; }
    /// <summary>Order category code (e.g. FTTH, FTTO). Installation Type = OrderCategory.</summary>
    public string? OrderCategoryCode { get; set; }
    /// <summary>Installation method code (e.g. PRELAID, SDU_RDF).</summary>
    public string? InstallationMethodCode { get; set; }
    /// <summary>Installation method name for display (e.g. Prelaid, SDU / RDF Pole).</summary>
    public string? InstallationMethodName { get; set; }
    /// <summary>Display-only: Partner.Code + "-" + OrderCategory.Code (e.g. TIME-FTTH). Not persisted.</summary>
    public string? DerivedPartnerCategoryLabel { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public Guid? SourceEmailId { get; set; }
    public Guid OrderTypeId { get; set; }
    public ServiceIdType? ServiceIdType { get; set; }
    public string ServiceId { get; set; } = string.Empty;
    public string? TicketId { get; set; }
    public string? AwoNumber { get; set; }
    public string? ExternalRef { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? StatusReason { get; set; }
    public string? Priority { get; set; }
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
    
    // Relocation fields per ORDERS_MODULE.md section 7
    public string? RelocationType { get; set; }
    public string? OldAddress { get; set; }
    public string? OldLocationNote { get; set; }
    public string? NewLocationNote { get; set; }
    
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerPhone2 { get; set; }
    public string? CustomerEmail { get; set; }
    
    /// <summary>
    /// Issue description for Assurance orders
    /// </summary>
    public string? Issue { get; set; }
    
    /// <summary>
    /// Solution/resolution for Assurance orders
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
    public bool HasReschedules { get; set; }
    public int RescheduleCount { get; set; }
    public bool DocketUploaded { get; set; }
    public bool PhotosUploaded { get; set; }
    public bool SerialsValidated { get; set; }
    public bool InvoiceEligible { get; set; }
    
    // Splitter fields
    public string? SplitterNumber { get; set; }
    public string? SplitterLocation { get; set; }
    public string? SplitterPort { get; set; }
    public Guid? SplitterId { get; set; }
    
    // Material replacements for Assurance orders
    public List<OrderMaterialReplacementDto> MaterialReplacements { get; set; } = new();
    public List<OrderNonSerialisedReplacementDto> NonSerialisedReplacements { get; set; } = new();
    public Guid? InvoiceId { get; set; }
    public Guid? PayrollPeriodId { get; set; }
    public string? PnlPeriod { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Optional per-order profitability: billing revenue (from BillingRatecard). Populated when list/drilldown requests profitability.
    /// </summary>
    public decimal? RevenueAmount { get; set; }
    /// <summary>
    /// Optional per-order profitability: SI payout (from RateEngine). Populated when list/drilldown requests profitability.
    /// </summary>
    public decimal? PayoutAmount { get; set; }
    /// <summary>
    /// Optional per-order profitability: profit (revenue - payout - material - other). Populated when list/drilldown requests profitability.
    /// </summary>
    public decimal? ProfitAmount { get; set; }

    /// <summary>
    /// Optional: true if order has at least one financial alert. Populated when includeFinancialAlerts or includeFinancialAlertsSummary is requested.
    /// </summary>
    public bool? HasFinancialAlert { get; set; }
    /// <summary>
    /// Optional: highest severity among financial alerts (Critical, Warning, Info). Populated when includeFinancialAlerts or includeFinancialAlertsSummary is requested.
    /// </summary>
    public string? HighestAlertSeverity { get; set; }
    /// <summary>
    /// Optional: number of financial alerts. Populated when includeFinancialAlerts is requested (computed). Prefer ActiveAlertCount for summary (persisted).
    /// </summary>
    public int? AlertCount { get; set; }
    /// <summary>
    /// Optional: number of active persisted financial alerts. Populated when includeFinancialAlertsSummary is requested (from OrderFinancialAlerts).
    /// </summary>
    public int? ActiveAlertCount { get; set; }
    
    /// <summary>
    /// Order Category ID (FTTH, FTTO, FTTR, FTTC) - represents the service/technology category.
    /// Only used for Activation orders. Previously known as InstallationTypeId.
    /// </summary>
    public Guid? OrderCategoryId { get; set; }
    
    /// <summary>
    /// Installation method ID (Prelaid, Non-Prelaid, etc.) - represents site condition/installation method.
    /// Per GPON_RATECARDS.md: OrderType + OrderCategory + InstallationMethod determines rate.
    /// </summary>
    public Guid? InstallationMethodId { get; set; }
    
    public List<ParsedDraftMaterialDto> ParsedMaterials { get; set; } = new();

    /// <summary>
    /// For parser-origin orders: count of parsed materials that could not be matched to Material master (audit).
    /// Null for non-parser orders.
    /// </summary>
    public int? UnmatchedParsedMaterialCount { get; set; }

    /// <summary>
    /// For parser-origin orders: names of parsed materials that could not be matched (audit).
    /// Null for non-parser orders.
    /// </summary>
    public List<string>? UnmatchedParsedMaterialNames { get; set; }
    
    // Network Info fields
    public string? PackageName { get; set; }
    public string? Bandwidth { get; set; }
    public string? NetworkPackage { get; set; }
    public string? NetworkBandwidth { get; set; }
    public string? NetworkLoginId { get; set; }
    public string? NetworkPassword { get; set; }
    public string? NetworkWanIp { get; set; }
    public string? NetworkLanIp { get; set; }
    public string? NetworkGateway { get; set; }
    public string? NetworkSubnetMask { get; set; }
    
    // VOIP fields
    public string? VoipServiceId { get; set; }
    public string? VoipPassword { get; set; }
    public string? VoipIpAddressOnu { get; set; }
    public string? VoipGatewayOnu { get; set; }
    public string? VoipSubnetMaskOnu { get; set; }
    public string? VoipIpAddressSrp { get; set; }
    public string? VoipRemarks { get; set; }
}

/// <summary>
/// Create order request DTO
/// </summary>
public class CreateOrderDto
{
    public Guid PartnerId { get; set; }
    public Guid OrderTypeId { get; set; }
    public ServiceIdType? ServiceIdType { get; set; }
    public string ServiceId { get; set; } = string.Empty;
    public string? TicketId { get; set; }
    public string? AwoNumber { get; set; }
    public string? ExternalRef { get; set; }
    public string? Priority { get; set; }
    public Guid BuildingId { get; set; }
    public string? UnitNo { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Postcode { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    
    // Relocation fields per ORDERS_MODULE.md section 7
    /// <summary>
    /// Relocation type: "Indoor" (same unit, different room) or "Outdoor" (different address)
    /// </summary>
    public string? RelocationType { get; set; }
    /// <summary>
    /// For Outdoor Relocation: the old/previous address
    /// </summary>
    public string? OldAddress { get; set; }
    /// <summary>
    /// For Indoor Relocation: the old location within the unit (e.g., "Bedroom 3")
    /// </summary>
    public string? OldLocationNote { get; set; }
    /// <summary>
    /// For Indoor Relocation: the new location within the unit (e.g., "Living Hall")
    /// </summary>
    public string? NewLocationNote { get; set; }
    
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerPhone2 { get; set; }
    public string? CustomerEmail { get; set; }
    
    /// <summary>
    /// Issue description for Assurance orders
    /// </summary>
    public string? Issue { get; set; }
    
    /// <summary>
    /// Solution/resolution for Assurance orders
    /// </summary>
    public string? Solution { get; set; }
    
    public string? OrderNotesInternal { get; set; }
    public string? PartnerNotes { get; set; }
    public DateTime? RequestedAppointmentAt { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan AppointmentWindowFrom { get; set; }
    public TimeSpan AppointmentWindowTo { get; set; }
    public Guid? DepartmentId { get; set; }
    
    /// <summary>
    /// Order Category ID (FTTH, FTTO, FTTR, FTTC) - represents the service/technology category.
    /// Only used for Activation orders. Previously known as InstallationTypeId.
    /// </summary>
    public Guid? OrderCategoryId { get; set; }
    
    /// <summary>
    /// Installation method ID (Prelaid, Non-Prelaid, etc.) - represents site condition/installation method.
    /// Per GPON_RATECARDS.md: OrderType + OrderCategory + InstallationMethod determines rate.
    /// </summary>
    public Guid? InstallationMethodId { get; set; }
    
    // Splitter fields (optional at creation, required before docket verification)
    public string? SplitterNumber { get; set; }
    public string? SplitterLocation { get; set; }
    public string? SplitterPort { get; set; }
    
    // Network Info fields
    public string? NetworkPackage { get; set; }
    public string? NetworkBandwidth { get; set; }
    public string? NetworkLoginId { get; set; }
    public string? NetworkPassword { get; set; }
    public string? NetworkWanIp { get; set; }
    public string? NetworkLanIp { get; set; }
    public string? NetworkGateway { get; set; }
    public string? NetworkSubnetMask { get; set; }
    
    // VOIP fields
    public string? VoipServiceId { get; set; }
    public string? VoipPassword { get; set; }
    public string? VoipIpAddressOnu { get; set; }
    public string? VoipGatewayOnu { get; set; }
    public string? VoipSubnetMaskOnu { get; set; }
    public string? VoipIpAddressSrp { get; set; }
    public string? VoipRemarks { get; set; }
}

/// <summary>
/// Update order request DTO
/// </summary>
public class UpdateOrderDto
{
    public string? TicketId { get; set; }
    public string? ExternalRef { get; set; }
    public string? Priority { get; set; }
    public string? UnitNo { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Postcode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    
    /// <summary>
    /// Issue description for Assurance orders
    /// </summary>
    public string? Issue { get; set; }
    
    /// <summary>
    /// Solution/resolution for Assurance orders
    /// </summary>
    public string? Solution { get; set; }
    
    public string? OrderNotesInternal { get; set; }
    public string? PartnerNotes { get; set; }
    public DateTime? RequestedAppointmentAt { get; set; }
    public DateTime? AppointmentDate { get; set; }
    public TimeSpan? AppointmentWindowFrom { get; set; }
    public TimeSpan? AppointmentWindowTo { get; set; }
}

/// <summary>
/// Change order status request DTO
/// </summary>
public class ChangeOrderStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Order status log DTO - tracks status change history
/// </summary>
public class OrderStatusLogDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? TransitionReason { get; set; }
    public Guid? TriggeredByUserId { get; set; }
    public string? TriggeredByUserName { get; set; }
    public Guid? TriggeredBySiId { get; set; }
    public string? TriggeredBySiName { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Order reschedule DTO - tracks reschedule history
/// </summary>
public class OrderRescheduleDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public string? RequestedByUserName { get; set; }
    public Guid? RequestedBySiId { get; set; }
    public string? RequestedBySiName { get; set; }
    public string RequestedBySource { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime OriginalDate { get; set; }
    public TimeSpan OriginalWindowFrom { get; set; }
    public TimeSpan OriginalWindowTo { get; set; }
    public DateTime NewDate { get; set; }
    public TimeSpan NewWindowFrom { get; set; }
    public TimeSpan NewWindowTo { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ApprovalSource { get; set; }
    public Guid? ApprovalEmailId { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? StatusChangedByUserId { get; set; }
    public string? StatusChangedByUserName { get; set; }
    public DateTime? StatusChangedAt { get; set; }
    public bool IsSameDayReschedule { get; set; }
    public Guid? SameDayEvidenceAttachmentId { get; set; }
    public string? SameDayEvidenceNotes { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Order material replacement DTO - serialised material swap for Assurance orders
/// </summary>
public class OrderMaterialReplacementDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid OldMaterialId { get; set; }
    public string? OldMaterialName { get; set; }
    public string OldSerialNumber { get; set; } = string.Empty;
    public Guid? OldSerialisedItemId { get; set; }
    public Guid NewMaterialId { get; set; }
    public string? NewMaterialName { get; set; }
    public string NewSerialNumber { get; set; } = string.Empty;
    public Guid? NewSerialisedItemId { get; set; }
    public string? ApprovedBy { get; set; }
    public string? ApprovalNotes { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ReplacementReason { get; set; }
    public Guid? ReplacedBySiId { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public DateTime RecordedAt { get; set; }
    public Guid? RmaRequestId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Create/Update order material replacement DTO
/// </summary>
public class CreateOrderMaterialReplacementDto
{
    public Guid OldMaterialId { get; set; }
    public string OldSerialNumber { get; set; } = string.Empty;
    public Guid? OldSerialisedItemId { get; set; }
    public Guid NewMaterialId { get; set; }
    public string NewSerialNumber { get; set; } = string.Empty;
    public Guid? NewSerialisedItemId { get; set; }
    public string? ApprovedBy { get; set; }
    public string? ApprovalNotes { get; set; }
    public string? ReplacementReason { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Order non-serialised replacement DTO - for patch cords, connectors, etc.
/// </summary>
public class OrderNonSerialisedReplacementDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid MaterialId { get; set; }
    public string? MaterialName { get; set; }
    public decimal QuantityReplaced { get; set; }
    public string? Unit { get; set; }
    public string? ReplacementReason { get; set; }
    public string? Remark { get; set; }
    public Guid? ReplacedBySiId { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public DateTime RecordedAt { get; set; }
}

/// <summary>
/// Create/Update order non-serialised replacement DTO
/// </summary>
public class CreateOrderNonSerialisedReplacementDto
{
    public Guid MaterialId { get; set; }
    public decimal QuantityReplaced { get; set; }
    public string? Unit { get; set; }
    public string? ReplacementReason { get; set; }
    public string? Remark { get; set; }
}


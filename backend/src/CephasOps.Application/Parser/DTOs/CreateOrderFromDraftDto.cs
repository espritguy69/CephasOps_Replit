using CephasOps.Application.Buildings.DTOs;

namespace CephasOps.Application.Parser.DTOs;

/// <summary>
/// DTO for creating an order from a parsed draft
/// </summary>
public class CreateOrderFromDraftDto
{
    /// <summary>
    /// The parsed order draft ID
    /// </summary>
    public Guid ParsedOrderDraftId { get; set; }

    /// <summary>
    /// Company ID (inherited from parse session)
    /// </summary>
    public Guid? CompanyId { get; set; }

    /// <summary>
    /// Partner ID (detected from email)
    /// </summary>
    public Guid? PartnerId { get; set; }

    /// <summary>
    /// Building ID (resolved from address)
    /// </summary>
    public Guid? BuildingId { get; set; }

    /// <summary>
    /// Source email message ID
    /// </summary>
    public Guid? SourceEmailId { get; set; }

    /// <summary>
    /// Order type ID (resolved from OrderTypeHint)
    /// </summary>
    public Guid? OrderTypeId { get; set; }

    /// <summary>
    /// Order category ID (FTTH, FTTO, etc.). Required for rate resolution; resolved from draft/building/default if not set.
    /// </summary>
    public Guid? OrderCategoryId { get; set; }

    /// <summary>
    /// Installation method ID (Prelaid, Non-Prelaid, etc.). Optional; resolved from building if not set.
    /// </summary>
    public Guid? InstallationMethodId { get; set; }

    /// <summary>
    /// Order type hint from parser (FTTH, FTTO, HSBB, Assurance, Modification)
    /// </summary>
    public string? OrderTypeHint { get; set; }

    /// <summary>
    /// Service ID - primary identifier for activation orders
    /// </summary>
    public string? ServiceId { get; set; }

    /// <summary>
    /// Ticket ID - required for assurance orders (TTKT)
    /// </summary>
    public string? TicketId { get; set; }

    /// <summary>
    /// Customer name
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Customer phone (will be auto-fixed)
    /// </summary>
    public string? CustomerPhone { get; set; }

    /// <summary>
    /// Customer email (if available)
    /// </summary>
    public string? CustomerEmail { get; set; }

    /// <summary>
    /// Additional contact number for Assurance orders
    /// </summary>
    public string? AdditionalContactNumber { get; set; }

    /// <summary>
    /// Issue description for Assurance orders (e.g., "Link Down", "LOSi", "LOBi")
    /// </summary>
    public string? Issue { get; set; }

    /// <summary>
    /// Full address text
    /// </summary>
    public string? AddressText { get; set; }

    /// <summary>
    /// Old/Previous address - required for Modification/Relocation orders
    /// </summary>
    public string? OldAddress { get; set; }

    /// <summary>
    /// Appointment date
    /// </summary>
    public DateTime? AppointmentDate { get; set; }

    /// <summary>
    /// Appointment window string (e.g., "09:00-11:00")
    /// </summary>
    public string? AppointmentWindow { get; set; }

    /// <summary>
    /// Validation notes from parser
    /// </summary>
    public string? ValidationNotes { get; set; }

    /// <summary>
    /// External reference (e.g., work order URL)
    /// </summary>
    public string? ExternalRef { get; set; }

    /// <summary>
    /// Package/Plan name from partner
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
    /// ONU Password (plain text - will be encrypted before storage)
    /// </summary>
    public string? OnuPassword { get; set; }

    /// <summary>
    /// VOIP Service ID if applicable
    /// </summary>
    public string? VoipServiceId { get; set; }
    
    /// <summary>
    /// Service ID type (TBBN or Partner Service ID)
    /// </summary>
    public CephasOps.Domain.Orders.Enums.ServiceIdType? ServiceIdType { get; set; }
    
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
    public string? VoipPassword { get; set; }
    public string? VoipIpAddressOnu { get; set; }
    public string? VoipGatewayOnu { get; set; }
    public string? VoipSubnetMaskOnu { get; set; }
    public string? VoipIpAddressSrp { get; set; }
    public string? VoipRemarks { get; set; }

    /// <summary>
    /// Raw remarks from the source document (includes technical details)
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// Parsed materials from the draft (optional). Resolved materials will be added as OrderMaterialUsage during order creation.
    /// </summary>
    public List<ParsedDraftMaterialDto>? Materials { get; set; }
}

/// <summary>
/// Result of order creation from draft
/// </summary>
public class CreateOrderFromDraftResult
{
    /// <summary>
    /// Whether order creation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Created order ID (if successful)
    /// </summary>
    public Guid? OrderId { get; set; }

    /// <summary>
    /// Error message (if failed)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Validation errors (if any)
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// Building lookup result - indicates if a new building was detected
    /// </summary>
    public BuildingDetectionResult? BuildingDetection { get; set; }

    /// <summary>
    /// Create a successful result
    /// </summary>
    public static CreateOrderFromDraftResult Succeeded(Guid orderId) => new()
    {
        Success = true,
        OrderId = orderId
    };

    /// <summary>
    /// Create a failed result
    /// </summary>
    public static CreateOrderFromDraftResult Failed(string errorMessage, List<string>? validationErrors = null) => new()
    {
        Success = false,
        ErrorMessage = errorMessage,
        ValidationErrors = validationErrors ?? new List<string>()
    };

    /// <summary>
    /// Create a result that requires building creation
    /// </summary>
    public static CreateOrderFromDraftResult RequiresBuilding(BuildingDetectionResult buildingDetection) => new()
    {
        Success = false,
        ErrorMessage = "Building not found. Please create or select a building to continue.",
        BuildingDetection = buildingDetection
    };
}

/// <summary>
/// Building detection result for order creation
/// </summary>
public class BuildingDetectionResult
{
    /// <summary>
    /// Detected building name from address parsing
    /// </summary>
    public string? DetectedBuildingName { get; set; }

    /// <summary>
    /// Detected address components
    /// </summary>
    public string? DetectedAddress { get; set; }
    public string? DetectedCity { get; set; }
    public string? DetectedState { get; set; }
    public string? DetectedPostcode { get; set; }

    /// <summary>
    /// Matched building (if found)
    /// </summary>
    public BuildingListItemDto? MatchedBuilding { get; set; }

    /// <summary>
    /// Similar buildings that might match (for user selection)
    /// </summary>
    public List<BuildingListItemDto> SimilarBuildings { get; set; } = new();
}


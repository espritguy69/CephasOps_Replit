using CephasOps.Domain.Common;

namespace CephasOps.Domain.Parser.Entities;

/// <summary>
/// Parsed order draft entity - intermediate representation before Order creation
/// </summary>
public class ParsedOrderDraft : CompanyScopedEntity
{
    public Guid ParseSessionId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? BuildingId { get; set; }
    
    /// <summary>
    /// Building name extracted from address (for matching)
    /// </summary>
    public string? BuildingName { get; set; }
    
    /// <summary>
    /// Building resolution status: "Existing" (matched), "New" (needs creation), or null (not processed)
    /// </summary>
    public string? BuildingStatus { get; set; }
    
    public string? ServiceId { get; set; }
    public string? TicketId { get; set; }
    
    /// <summary>
    /// AWO Number - required for Assurance orders
    /// </summary>
    public string? AwoNumber { get; set; }
    
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    
    /// <summary>
    /// Additional contact number for Assurance orders
    /// </summary>
    public string? AdditionalContactNumber { get; set; }
    
    /// <summary>
    /// Issue description for Assurance orders (e.g., "Link Down", "LOSi", "LOBi")
    /// </summary>
    public string? Issue { get; set; }
    
    public string? AddressText { get; set; }
    
    /// <summary>
    /// Old/Previous address - required for Modification/Relocation orders
    /// </summary>
    public string? OldAddress { get; set; }
    
    public DateTime? AppointmentDate { get; set; }
    public string? AppointmentWindow { get; set; }
    public string? OrderTypeHint { get; set; }
    
    /// <summary>
    /// Order type code detected from file/email (e.g., "MODIFICATION_OUTDOOR", "ACTIVATION")
    /// </summary>
    public string? OrderTypeCode { get; set; }

    /// <summary>
    /// Order category ID (FTTH, FTTO, etc.) for parser review/edit and order creation.
    /// </summary>
    public Guid? OrderCategoryId { get; set; }
    
    /// <summary>
    /// Package/Plan name from partner
    /// </summary>
    public string? PackageName { get; set; }
    
    /// <summary>
    /// Bandwidth
    /// </summary>
    public string? Bandwidth { get; set; }
    
    /// <summary>
    /// ONU Serial Number
    /// </summary>
    public string? OnuSerialNumber { get; set; }

    /// <summary>
    /// ONU Password (plain text - will be encrypted when order is created)
    /// </summary>
    public string? OnuPassword { get; set; }

    /// <summary>
    /// Network Login ID / Username (plain text - will be encrypted when order is created)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Network Password (plain text - will be encrypted when order is created)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Internet WAN IP Address
    /// </summary>
    public string? InternetWanIp { get; set; }

    /// <summary>
    /// Internet LAN IP Address
    /// </summary>
    public string? InternetLanIp { get; set; }

    /// <summary>
    /// Internet Gateway IP Address
    /// </summary>
    public string? InternetGateway { get; set; }

    /// <summary>
    /// Internet Subnet Mask
    /// </summary>
    public string? InternetSubnetMask { get; set; }

    /// <summary>
    /// VOIP Service ID
    /// </summary>
    public string? VoipServiceId { get; set; }
    
    /// <summary>
    /// Raw remarks from the source document
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// Extra/unmapped parser info (e.g. AnyUnhandledSections from Excel) - read-only, for display on Create Order page.
    /// </summary>
    public string? AdditionalInformation { get; set; }
    
    /// <summary>
    /// Source filename
    /// </summary>
    public string? SourceFileName { get; set; }
    
    /// <summary>
    /// SHA256 hash of the source file content (for duplicate detection)
    /// </summary>
    public string? FileHash { get; set; }
    
    /// <summary>
    /// Materials captured from parser (stored as JSON payload)
    /// </summary>
    public string? ParsedMaterialsJson { get; set; }

    /// <summary>
    /// Number of parsed materials that could not be matched to Material master (audit; set at create-from-draft).
    /// </summary>
    public int? UnmatchedMaterialCount { get; set; }

    /// <summary>
    /// Names of parsed materials that could not be matched (JSON array of strings; audit; set at create-from-draft).
    /// </summary>
    public string? UnmatchedMaterialNamesJson { get; set; }
    
    public decimal ConfidenceScore { get; set; }
    public string ValidationStatus { get; set; } = "Pending"; // Pending, Valid, NeedsReview, Rejected
    public string? ValidationNotes { get; set; }
    public Guid? CreatedOrderId { get; set; }
    public Guid? CreatedByUserId { get; set; }
}


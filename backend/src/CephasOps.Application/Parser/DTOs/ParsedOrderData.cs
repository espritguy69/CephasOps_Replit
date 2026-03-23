namespace CephasOps.Application.Parser.DTOs;

/// <summary>
/// Parsed order data from TIME Excel
/// </summary>
public class ParsedOrderData
{
    // Core identification
    public string? ServiceId { get; set; }
    public string? TicketId { get; set; }
    
    /// <summary>
    /// AWO Number - required for Assurance orders
    /// </summary>
    public string? AwoNumber { get; set; }
    
    public string OrderTypeCode { get; set; } = string.Empty;
    public string OrderTypeHint { get; set; } = string.Empty;
    public string PartnerCode { get; set; } = "TIME";

    // Customer details
    public string? CustomerName { get; set; }
    public string? ContactPerson { get; set; }
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
    
    /// <summary>
    /// Last Fault Date - used to determine if assurance is still under warranty
    /// If fault is within 30 days from Last Fault Date, customer might not pay
    /// </summary>
    public DateTime? LastFaultDate { get; set; }

    // Address
    public string? ServiceAddress { get; set; }
    public string? OldAddress { get; set; }

    // Appointment
    public DateTime? AppointmentDateTime { get; set; }
    public string? AppointmentWindow { get; set; }

    // Technical details
    public string? PackageName { get; set; }
    public string? Bandwidth { get; set; }
    public string? OnuSerialNumber { get; set; }
    public string? OnuPassword { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? VoipServiceId { get; set; }
    public string? VoipPassword { get; set; }

    // Internet network details (FIBER INTERNET section)
    public string? InternetWanIp { get; set; }
    public string? InternetLanIp { get; set; }
    public string? InternetGateway { get; set; }
    public string? InternetSubnetMask { get; set; }

    // VOIP network details (VOIP section)
    public string? VoipOnuIp { get; set; }
    public string? VoipGateway { get; set; }
    public string? VoipSubnetMask { get; set; }
    public string? VoipSrpIp { get; set; }
    public string? VoipRemarks { get; set; }

    // Notes
    public string? Remarks { get; set; }
    public string? SplitterLocation { get; set; }

    /// <summary>
    /// Extra/unmapped parser info (e.g. unhandled Excel rows) - stored in draft.AdditionalInformation.
    /// </summary>
    public string? AdditionalInformation { get; set; }

    // Source tracking
    public string? SourceFileName { get; set; }
    public decimal ConfidenceScore { get; set; } = 0.8m;

    // Materials parsed from sheet
    public List<ParsedOrderMaterialLine> Materials { get; set; } = new();
}

/// <summary>
/// Material line parsed from Excel
/// </summary>
public class ParsedOrderMaterialLine
{
    public string Name { get; set; } = string.Empty;
    public string? ActionTag { get; set; } // "ADD", "REMOVE", "REPLACE", etc.
    public decimal? Quantity { get; set; }
    public string? UnitOfMeasure { get; set; }
    public string? Notes { get; set; }
    public bool IsRequired { get; set; } = false; // Whether material must be provided (based on X → YES/No logic)
}


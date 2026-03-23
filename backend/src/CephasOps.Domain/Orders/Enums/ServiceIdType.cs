namespace CephasOps.Domain.Orders.Enums;

/// <summary>
/// Service ID type - distinguishes TBBN from Partner Service ID
/// </summary>
public enum ServiceIdType
{
    /// <summary>
    /// TIME direct customer TBBN (format: TBBN[A-Z]?\d+)
    /// Examples: TBBN1234567, TBBNA12345, TBBNB1234
    /// </summary>
    Tbbn = 1,
    
    /// <summary>
    /// Partner Service ID for wholesale partners (Digi, Celcom, U Mobile, etc.)
    /// Examples: CELCOM0016996, DIGI0012345
    /// </summary>
    PartnerServiceId = 2
}


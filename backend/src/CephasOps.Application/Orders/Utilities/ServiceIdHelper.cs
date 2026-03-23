using System.Text.RegularExpressions;
using CephasOps.Domain.Orders.Enums;

namespace CephasOps.Application.Orders.Utilities;

using ServiceIdType = CephasOps.Domain.Orders.Enums.ServiceIdType;

/// <summary>
/// Helper utility for Service ID detection and partner auto-selection
/// </summary>
public static class ServiceIdHelper
{
    /// <summary>
    /// TBBN pattern: TBBN followed by optional letter (A/B) and digits, ending with optional letter
    /// Examples: TBBN1234567, TBBNA12345, TBBNB1234
    /// </summary>
    private static readonly Regex TbbnPattern = new(@"^TBBN[A-Z]?\d+[A-Z]?$", RegexOptions.IgnoreCase);

    /// <summary>
    /// Partner Service ID patterns
    /// </summary>
    private static readonly Dictionary<string, string[]> PartnerServiceIdPatterns = new()
    {
        { "DIGI", new[] { @"^DIGI\d+$", @"^DIGI00\d+$" } },
        { "CELCOM", new[] { @"^CELCOM\d+$", @"^CELCOM00\d+$" } },
        { "CELCOMDIGI", new[] { @"^CELCOMDIGI\d+$", @"^CELCOMDIGI00\d+$" } },
        { "UMOBILE", new[] { @"^UMOBILE\d+$", @"^UMOBILE00\d+$" } }
    };

    /// <summary>
    /// Detect Service ID type from value
    /// </summary>
    public static ServiceIdType? DetectServiceIdType(string? serviceId)
    {
        if (string.IsNullOrWhiteSpace(serviceId))
            return null;

        var trimmed = serviceId.Trim().ToUpperInvariant();

        // Check TBBN pattern
        if (TbbnPattern.IsMatch(trimmed))
            return ServiceIdType.Tbbn;

        // Check Partner Service ID patterns
        foreach (var (partner, patterns) in PartnerServiceIdPatterns)
        {
            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(trimmed, pattern, RegexOptions.IgnoreCase))
                    return ServiceIdType.PartnerServiceId;
            }
        }

        // Default: if starts with TBBN, assume TBBN
        if (trimmed.StartsWith("TBBN", StringComparison.OrdinalIgnoreCase))
            return ServiceIdType.Tbbn;

        // Otherwise, assume Partner Service ID (for unknown formats)
        return ServiceIdType.PartnerServiceId;
    }

    /// <summary>
    /// Auto-detect partner code from Service ID
    /// </summary>
    public static string? DetectPartnerFromServiceId(string? serviceId)
    {
        if (string.IsNullOrWhiteSpace(serviceId))
            return null;

        var trimmed = serviceId.Trim().ToUpperInvariant();

        // Check each partner pattern
        foreach (var (partner, patterns) in PartnerServiceIdPatterns)
        {
            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(trimmed, pattern, RegexOptions.IgnoreCase))
                    return partner;
            }
        }

        // If TBBN, return TIME
        if (TbbnPattern.IsMatch(trimmed) || trimmed.StartsWith("TBBN", StringComparison.OrdinalIgnoreCase))
            return "TIME";

        return null;
    }

    /// <summary>
    /// Auto-select partner based on order type, building, installation method, or service ID
    /// </summary>
    public static string? AutoSelectPartner(
        string? orderType,
        string? buildingType,
        string? installationMethod,
        string? serviceId)
    {
        // Assurance → TIME ASSURANCE
        if (!string.IsNullOrWhiteSpace(orderType) && 
            orderType.Contains("Assurance", StringComparison.OrdinalIgnoreCase))
        {
            return "TIME ASSURANCE";
        }

        // FTTO installation → TIME FTTO
        if (!string.IsNullOrWhiteSpace(installationMethod) && 
            installationMethod.Contains("FTTO", StringComparison.OrdinalIgnoreCase))
        {
            return "TIME FTTO";
        }

        if (!string.IsNullOrWhiteSpace(buildingType) && 
            buildingType.Contains("FTTO", StringComparison.OrdinalIgnoreCase))
        {
            return "TIME FTTO";
        }

        // Service ID detection
        if (!string.IsNullOrWhiteSpace(serviceId))
        {
            var detectedPartner = DetectPartnerFromServiceId(serviceId);
            if (!string.IsNullOrWhiteSpace(detectedPartner))
                return detectedPartner;
        }

        return null;
    }

    /// <summary>
    /// Validate Service ID format
    /// </summary>
    public static bool IsValidServiceId(string? serviceId, ServiceIdType? type)
    {
        if (string.IsNullOrWhiteSpace(serviceId))
            return false;

        if (!type.HasValue)
            return true; // No type specified, accept any non-empty value

        return type.Value switch
        {
            ServiceIdType.Tbbn => TbbnPattern.IsMatch(serviceId.Trim()),
            ServiceIdType.PartnerServiceId => serviceId.Trim().Length > 0, // Partner IDs vary
            _ => true
        };
    }
}


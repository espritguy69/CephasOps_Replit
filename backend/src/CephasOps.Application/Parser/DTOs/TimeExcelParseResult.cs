namespace CephasOps.Application.Parser.DTOs;

/// <summary>
/// Result of parsing a TIME Excel file
/// </summary>
public class TimeExcelParseResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public ParsedOrderData? OrderData { get; set; }

    /// <summary>
    /// Parse status: Success, FailedRequiredFields, FailedValidation, etc.
    /// Enables actionable handling; do not treat partial data as usable when status is not Success.
    /// </summary>
    public string ParseStatus { get; set; } = "Unknown";

    /// <summary>
    /// Explainable parse report (missing fields, sheet/header, confidence breakdown). Persist or log per attachment.
    /// </summary>
    public ParseReport? ParseReport { get; set; }

    /// <summary>
    /// Failure category for diagnostics: NONE, DATA_MISSING, LAYOUT_DRIFT, CONVERSION_ISSUE, VALIDATION_FAIL, PARSE_ERROR.
    /// Optional; also present on ParseReport when available.
    /// </summary>
    public string? ParseFailureCategory { get; set; }
}


namespace CephasOps.Application.Parser.DTOs;

/// <summary>
/// JSON-serializable report per email+attachment for parse diagnostics and confidence explanation.
/// Persisted or logged to make failures actionable.
/// </summary>
public class ParseReport
{
    /// <summary>Email message ID when parse is triggered from email ingestion.</summary>
    public string? MessageId { get; set; }

    /// <summary>Email subject when parse is triggered from email.</summary>
    public string? Subject { get; set; }

    /// <summary>Attachment filename.</summary>
    public string AttachmentFileName { get; set; } = string.Empty;

    /// <summary>Detected file signature (e.g. D0CF11E0 for .xls).</summary>
    public string? DetectedFileSignatureType { get; set; }

    /// <summary>File size in bytes.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>SHA256 hash of file content (hex).</summary>
    public string? FileHash { get; set; }

    /// <summary>Engine(s) used: ExcelDataReader, Syncfusion, or both.</summary>
    public string EngineUsed { get; set; } = string.Empty;

    /// <summary>Whether .xls was converted to .xlsx before parsing.</summary>
    public bool ConversionPerformed { get; set; }

    /// <summary>Temp path of converted file if conversion was performed.</summary>
    public string? ConvertedFilePath { get; set; }

    /// <summary>Size of converted file in bytes.</summary>
    public long? ConvertedFileSizeBytes { get; set; }

    /// <summary>Selected worksheet name.</summary>
    public string? SelectedSheetName { get; set; }

    /// <summary>Score per sheet (sheet name -> score).</summary>
    public Dictionary<string, int>? SheetScores { get; set; }

    /// <summary>Detected header row (1-based).</summary>
    public int? DetectedHeaderRow { get; set; }

    /// <summary>Header row score (number of known labels matched).</summary>
    public int? HeaderScore { get; set; }

    /// <summary>Summary of extracted required and key optional fields (field name -> present).</summary>
    public Dictionary<string, bool>? ExtractedFieldsSummary { get; set; }

    /// <summary>List of required field names that were missing.</summary>
    public List<string> MissingRequiredFields { get; set; } = new();

    /// <summary>Confidence breakdown: requiredCoverage, validity, enrichment (0-1).</summary>
    public ConfidenceBreakdown? ConfidenceBreakdown { get; set; }

    /// <summary>Final confidence score (0-1).</summary>
    public decimal FinalConfidenceScore { get; set; }

    /// <summary>Status: Success, FailedRequiredFields, FailedValidation, etc.</summary>
    public string ParseStatus { get; set; } = "Unknown";

    /// <summary>Failure reason category: NONE, DATA_MISSING, LAYOUT_DRIFT, CONVERSION_ISSUE, VALIDATION_FAIL, PARSE_ERROR.</summary>
    public string ParseFailureCategory { get; set; } = "NONE";

    /// <summary>Per-field provenance (label/match type/location only; no values).</summary>
    public List<FieldDiagnosticEntry>? FieldDiagnostics { get; set; }

    /// <summary>Best sheet score (highest label hits).</summary>
    public int? SheetScoreBest { get; set; }

    /// <summary>Second-best sheet score.</summary>
    public int? SheetScoreSecondBest { get; set; }

    /// <summary>Total label hits for required fields in selected sheet.</summary>
    public int? TotalLabelHitsForRequiredFields { get; set; }

    /// <summary>Template profile ID used for this parse (Phase 8; PII-safe).</summary>
    public Guid? TemplateProfileId { get; set; }

    /// <summary>Template profile name (Phase 8; PII-safe).</summary>
    public string? TemplateProfileName { get; set; }

    /// <summary>Whether layout drift was detected vs baseline (Phase 8).</summary>
    public bool? DriftDetected { get; set; }

    /// <summary>Short drift signature for LAYOUT_DRIFT triage (Phase 8).</summary>
    public string? DriftSignature { get; set; }
}

/// <summary>
/// Confidence breakdown for explainability.
/// </summary>
public class ConfidenceBreakdown
{
    /// <summary>Required field coverage (0-1). Should be 1 if parse is usable.</summary>
    public decimal RequiredCoveragePercent { get; set; }

    /// <summary>Validity checks: phone format, non-empty address, etc. (0-1).</summary>
    public decimal ValidityPercent { get; set; }

    /// <summary>Order type certainty (0-1).</summary>
    public decimal OrderTypeCertaintyPercent { get; set; }

    /// <summary>Optional enrichment: appointment, package, ONU, etc. (0-1).</summary>
    public decimal EnrichmentPercent { get; set; }
}

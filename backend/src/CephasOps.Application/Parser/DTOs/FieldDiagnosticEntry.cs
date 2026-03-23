namespace CephasOps.Application.Parser.DTOs;

/// <summary>
/// Per-field provenance: how a required/optional field was found (no PII values).
/// </summary>
public class FieldDiagnosticEntry
{
    public string FieldName { get; set; } = string.Empty;
    public bool Found { get; set; }
    /// <summary>Sanitized label name only (e.g. "Customer Name"); never the cell value.</summary>
    public string? MatchedLabel { get; set; }
    /// <summary>Exact | Contains | NormalizedExact | NormalizedContains</summary>
    public string MatchType { get; set; } = string.Empty;
    /// <summary>TableHeader | KeyValueRight | KeyValueBelow | Other</summary>
    public string ExtractionMode { get; set; } = "Other";
    public string? SheetName { get; set; }
    public int? RowIndex { get; set; }
    public int? ColumnIndex { get; set; }
}

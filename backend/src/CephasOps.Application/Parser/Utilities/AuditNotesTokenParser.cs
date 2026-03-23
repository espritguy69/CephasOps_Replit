namespace CephasOps.Application.Parser.Utilities;

/// <summary>
/// Phase 10: Parses ValidationNotes [Audit] segment into structured tokens. Missing tokens are null/empty. PII-safe (only token keys/field names, no customer values).
/// </summary>
public static class AuditNotesTokenParser
{
    public const string AuditPrefix = "[Audit] ";

    /// <summary>
    /// Parse the audit segment from ValidationNotes. Returns null if no [Audit] segment; otherwise all parsed tokens (missing keys => null/empty).
    /// </summary>
    public static AuditNotesTokens? Parse(string? validationNotes)
    {
        if (string.IsNullOrEmpty(validationNotes)) return null;
        var idx = validationNotes.IndexOf(AuditPrefix, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var segment = validationNotes.Substring(idx + AuditPrefix.Length);
        var tokens = new AuditNotesTokens();
        foreach (var part in segment.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0) continue;
            var key = part.Substring(0, eq).Trim();
            var value = part.Substring(eq + 1).Trim();
            switch (key.ToUpperInvariant())
            {
                case "PROFILE":
                    if (Guid.TryParse(value, out var pid)) tokens.ProfileId = pid;
                    break;
                case "PROFILENAME":
                    tokens.ProfileName = value;
                    break;
                case "CATEGORY":
                    tokens.Category = value;
                    break;
                case "DRIFTDETECTED":
                    tokens.DriftDetected = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "DRIFTSIGNATURE":
                    tokens.DriftSignature = value;
                    break;
                case "MISSING":
                    tokens.Missing = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                    break;
                case "HEADERSCORE":
                    if (int.TryParse(value, out var hs)) tokens.HeaderScore = hs;
                    break;
                case "BESTSHEETSCORE":
                    if (int.TryParse(value, out var bs)) tokens.BestSheetScore = bs;
                    break;
                case "TEMPLATEACTION":
                    tokens.TemplateAction = value;
                    break;
                case "PARSESTATUS":
                    tokens.ParseStatus = value;
                    break;
                case "SHEET":
                    tokens.Sheet = value;
                    break;
                case "HEADERROW":
                    if (int.TryParse(value, out var hr)) tokens.HeaderRow = hr;
                    break;
                case "REQUIREDFOUNDBY":
                    tokens.RequiredFoundBy = value;
                    break;
                default:
                    break;
            }
        }
        return tokens.ProfileId.HasValue || !string.IsNullOrEmpty(tokens.Category) ? tokens : null;
    }
}

/// <summary>
/// Parsed tokens from a single draft's ValidationNotes [Audit] segment. All optional; PII-safe.
/// </summary>
public class AuditNotesTokens
{
    public Guid? ProfileId { get; set; }
    public string? ProfileName { get; set; }
    public string? Category { get; set; }
    public bool? DriftDetected { get; set; }
    public string? DriftSignature { get; set; }
    public List<string>? Missing { get; set; }
    public int? HeaderScore { get; set; }
    public int? BestSheetScore { get; set; }
    public string? TemplateAction { get; set; }
    public string? ParseStatus { get; set; }
    public string? Sheet { get; set; }
    public int? HeaderRow { get; set; }
    public string? RequiredFoundBy { get; set; }
}

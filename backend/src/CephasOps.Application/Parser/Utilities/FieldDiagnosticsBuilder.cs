using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Utilities.ExcelParsing;

namespace CephasOps.Application.Parser.Utilities;

/// <summary>
/// Builds per-field provenance (label/match type/location only; no cell values / PII).
/// </summary>
public static class FieldDiagnosticsBuilder
{
    public const int MaxRowsToScan = 30;

    /// <summary>Required and key optional fields with their label synonym sets.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> FieldLabelSynonyms = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
    {
        ["ServiceId"] = new[] { "Service ID", "SERVICE ID", "TBBN", "ServiceID", "PARTNER SERVICE ID" },
        ["TicketId"] = new[] { "Ticket ID", "TTKT", "Ticket", "TICKET ID" },
        ["CustomerName"] = new[] { "Customer Name", "CUSTOMER NAME", "Name", "Contact Person" },
        ["ServiceAddress"] = new[] { "Service Address", "SERVICE ADDRESS", "Installation Address", "New Address", "NEW ADDRESS" },
        ["CustomerPhone"] = new[] { "Contact No", "CONTACT NO", "Phone", "Contact", "Mobile" },
        ["AppointmentDateTime"] = new[] { "Appointment Date", "APPOINTMENT DATE", "Date", "Preferred Date", "Time" },
        ["PackageName"] = new[] { "Package Name", "PACKAGE", "Package", "Plan Name" },
        ["OnuSerialNumber"] = new[] { "Serial Number", "SERIAL NUMBER", "ONU Serial", "ONU Serial Number" },
        ["MaterialsSummary"] = new[] { "Materials", "MATERIALS", "Item", "Quantity" }
    };

    /// <summary>
    /// Build field diagnostics by scanning the sheet for known labels. Does not read or store any cell values (no PII).
    /// </summary>
    public static List<FieldDiagnosticEntry> Build(
        ISheetCellReader sheet,
        string? sheetName,
        IReadOnlyDictionary<string, int>? sheetScores,
        int? headerRow,
        int? headerScore)
        => Build(sheet, sheetName, sheetScores, headerRow, headerScore, null, null);

    /// <summary>
    /// Build field diagnostics with optional profile synonym overrides and row range (Phase 8). Overrides take precedence per field.
    /// </summary>
    public static List<FieldDiagnosticEntry> Build(
        ISheetCellReader sheet,
        string? sheetName,
        IReadOnlyDictionary<string, int>? sheetScores,
        int? headerRow,
        int? headerScore,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? synonymOverrides,
        (int Min, int Max)? headerRowRange)
    {
        if (sheet == null) return new List<FieldDiagnosticEntry>();
        var effectiveSynonyms = MergeSynonyms(synonymOverrides);
        int maxRow = headerRowRange.HasValue ? Math.Min(sheet.LastRow, headerRowRange.Value.Max) : Math.Min(sheet.LastRow, MaxRowsToScan);
        int minRow = headerRowRange?.Min ?? 1;
        maxRow = Math.Max(minRow, maxRow);
        var results = new List<FieldDiagnosticEntry>();
        int lastCol = sheet.LastColumn;

        foreach (var (fieldName, synonyms) in effectiveSynonyms)
        {
            var entry = new FieldDiagnosticEntry { FieldName = fieldName, Found = false, SheetName = sheetName };
            for (int row = minRow; row <= maxRow; row++)
            {
                for (int col = 1; col <= lastCol; col++)
                {
                    var cellText = sheet.GetCellText(row, col);
                    if (string.IsNullOrWhiteSpace(cellText)) continue;

                    string? matchedSynonym = null;
                    string matchType = "Other";
                    foreach (var syn in synonyms)
                    {
                        if (ExcelLabelNormalizer.ExactMatch(cellText, syn))
                        {
                            matchedSynonym = syn;
                            matchType = "NormalizedExact";
                            break;
                        }
                        if (ExcelLabelNormalizer.ContainsMatch(cellText, syn))
                        {
                            matchedSynonym = syn;
                            matchType = "NormalizedContains";
                            break;
                        }
                    }
                    if (matchedSynonym == null) continue;

                    entry.Found = true;
                    entry.MatchedLabel = matchedSynonym;
                    entry.MatchType = matchType;
                    entry.ExtractionMode = "KeyValueRight";
                    entry.RowIndex = row;
                    entry.ColumnIndex = col;
                    break;
                }
                if (entry.Found) break;
            }
            results.Add(entry);
        }

        return results;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> MergeSynonyms(
        IReadOnlyDictionary<string, IReadOnlyList<string>>? overrides)
    {
        if (overrides == null || overrides.Count == 0) return FieldLabelSynonyms;
        var merged = new Dictionary<string, IReadOnlyList<string>>(FieldLabelSynonyms);
        foreach (var kv in overrides)
            if (kv.Value != null && kv.Value.Count > 0)
                merged[kv.Key] = kv.Value;
        return merged;
    }

    /// <summary>
    /// Compute SheetScoreBest and SheetScoreSecondBest from sheet scores dictionary (ordered by value desc).
    /// </summary>
    public static (int? Best, int? SecondBest) GetBestSheetScores(IReadOnlyDictionary<string, int>? sheetScores)
    {
        if (sheetScores == null || sheetScores.Count == 0) return (null, null);
        var ordered = sheetScores.Values.OrderByDescending(v => v).ToList();
        return (ordered.Count > 0 ? ordered[0] : null, ordered.Count > 1 ? ordered[1] : null);
    }

    /// <summary>
    /// Total count of required-field diagnostics that were Found. Used for TotalLabelHitsForRequiredFields.
    /// </summary>
    public static int CountRequiredFieldsFound(IReadOnlyList<FieldDiagnosticEntry>? diagnostics)
    {
        if (diagnostics == null) return 0;
        var required = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ServiceId", "TicketId", "CustomerName", "ServiceAddress", "CustomerPhone" };
        return diagnostics.Count(d => d.Found && required.Contains(d.FieldName));
    }

    /// <summary>
    /// True if any missing required field has its labels present elsewhere (layout drift signal).
    /// </summary>
    public static bool RequiredLabelsFoundElsewhere(
        IReadOnlyList<string> missingRequiredFields,
        IReadOnlyList<FieldDiagnosticEntry>? diagnostics)
    {
        if (missingRequiredFields == null || missingRequiredFields.Count == 0 || diagnostics == null) return false;
        var missingSet = new HashSet<string>(missingRequiredFields, StringComparer.OrdinalIgnoreCase);
        foreach (var d in diagnostics)
        {
            if (missingSet.Contains(d.FieldName) && d.Found)
                return true;
        }
        return false;
    }
}

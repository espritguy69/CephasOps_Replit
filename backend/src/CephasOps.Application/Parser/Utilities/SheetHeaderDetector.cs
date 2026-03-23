using CephasOps.Application.Parser.Utilities.ExcelParsing;

namespace CephasOps.Application.Parser.Utilities;

/// <summary>
/// Scores worksheets by presence of known labels in first 30 rows and detects header row.
/// Deterministic selection: choose highest scoring sheet; header row = row 1-30 with best label score.
/// </summary>
public static class SheetHeaderDetector
{
    /// <summary>Max rows to scan for header/labels.</summary>
    public const int MaxRowsToScan = 30;

    /// <summary>Label synonym sets for scoring. Each group is one "concept"; one match per group counts.</summary>
    public static readonly IReadOnlyList<IReadOnlyList<string>> LabelSynonymSets = new List<IReadOnlyList<string>>
    {
        new[] { "Service ID", "SERVICE ID", "TBBN", "ServiceID", "PARTNER SERVICE ID" },
        new[] { "Customer Name", "CUSTOMER NAME", "Name" },
        new[] { "Service Address", "SERVICE ADDRESS", "Installation Address", "New Address" },
        new[] { "Contact No", "CONTACT NO", "Phone", "Contact", "Mobile" },
        new[] { "Old Address", "OLD ADDRESS", "Previous Address" },
        new[] { "Email", "EMAIL", "E-mail" },
        new[] { "Appointment Date", "APPOINTMENT DATE", "Date" },
        new[] { "Package", "PACKAGE", "Package Name" },
        new[] { "Remarks", "REMARKS", "Note", "Notes" }
    };

    /// <summary>
    /// Score a sheet by counting how many label synonym sets have at least one match in the first 30 rows.
    /// </summary>
    public static int ScoreSheet(ISheetCellReader sheet)
    {
        if (sheet == null) return 0;
        int lastRow = Math.Min(sheet.LastRow, MaxRowsToScan);
        int lastCol = sheet.LastColumn;
        var setMatched = new bool[LabelSynonymSets.Count];

        for (int row = 1; row <= lastRow; row++)
        {
            for (int col = 1; col <= lastCol; col++)
            {
                var cellValue = sheet.GetCellText(row, col);
                for (int i = 0; i < LabelSynonymSets.Count; i++)
                {
                    if (setMatched[i]) continue;
                    if (ExcelLabelNormalizer.MatchesAny(cellValue, LabelSynonymSets[i]))
                        setMatched[i] = true;
                }
            }
        }
        return setMatched.Count(x => x);
    }

    /// <summary>
    /// Score a single row by counting how many label synonym sets have a match in this row.
    /// </summary>
    public static int ScoreHeaderRow(ISheetCellReader sheet, int row)
    {
        if (sheet == null || row < 1) return 0;
        int lastCol = sheet.LastColumn;
        int score = 0;
        for (int col = 1; col <= lastCol; col++)
        {
            var cellValue = sheet.GetCellText(row, col);
            foreach (var synonymSet in LabelSynonymSets)
            {
                if (ExcelLabelNormalizer.MatchesAny(cellValue, synonymSet))
                {
                    score++;
                    break;
                }
            }
        }
        return score;
    }

    /// <summary>
    /// Find the row in [1, MaxRowsToScan] with the highest header score. Returns 1-based row index and score.
    /// </summary>
    public static (int Row1Based, int Score) DetectHeaderRow(ISheetCellReader sheet)
        => DetectHeaderRow(sheet, 1, MaxRowsToScan);

    /// <summary>
    /// Find the row in [minRow, maxRow] with the highest header score (Phase 8 profile header range hint).
    /// </summary>
    public static (int Row1Based, int Score) DetectHeaderRow(ISheetCellReader sheet, int minRow, int maxRow)
    {
        if (sheet == null) return (1, 0);
        minRow = Math.Max(1, minRow);
        int lastRow = Math.Min(sheet.LastRow, Math.Max(minRow, maxRow));
        int bestRow = minRow;
        int bestScore = 0;
        for (int row = minRow; row <= lastRow; row++)
        {
            int s = ScoreHeaderRow(sheet, row);
            if (s > bestScore)
            {
                bestScore = s;
                bestRow = row;
            }
        }
        return (bestRow, bestScore);
    }
}

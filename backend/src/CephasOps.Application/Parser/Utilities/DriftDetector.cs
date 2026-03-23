using CephasOps.Application.Parser.DTOs;

namespace CephasOps.Application.Parser.Utilities;

/// <summary>
/// Phase 8: Detects layout drift by comparing current ParseReport to a baseline (last successful parse for same profile).
/// If no baseline exists, DriftDetected is false and no signature is generated.
/// </summary>
public static class DriftDetector
{
    /// <summary>
    /// Result of drift detection: whether drift was detected, list of signals, and a short signature for triage.
    /// </summary>
    public readonly struct DriftResult
    {
        public bool DriftDetected { get; }
        public IReadOnlyList<string> DriftSignals { get; }
        public string DriftSignature { get; }

        public DriftResult(bool driftDetected, IReadOnlyList<string> driftSignals, string driftSignature)
        {
            DriftDetected = driftDetected;
            DriftSignals = driftSignals ?? new List<string>();
            DriftSignature = driftSignature ?? string.Empty;
        }
    }

    /// <summary>
    /// Baseline from last successful parse for a profile (extracted from ValidationNotes audit).
    /// </summary>
    public class DriftBaseline
    {
        public string? SelectedSheetName { get; set; }
        public int? DetectedHeaderRow { get; set; }
        public int? HeaderScore { get; set; }
        public int? SheetScoreBest { get; set; }
    }

    /// <summary>
    /// Compare current parse report to baseline using profile drift thresholds. Returns drift result; if no baseline, DriftDetected is false.
    /// </summary>
    public static DriftResult Detect(
        ParseReport current,
        DriftBaseline? baseline,
        TemplateProfileDriftThresholds? thresholds)
    {
        if (baseline == null)
            return new DriftResult(false, new List<string>(), string.Empty);

        var signals = new List<string>();
        int bestSheetDrop = thresholds?.BestSheetScoreDrop ?? 3;
        int headerScoreDrop = thresholds?.HeaderScoreDrop ?? 2;
        int headerRowShift = thresholds?.HeaderRowShift ?? 3;

        if (!string.IsNullOrEmpty(current.SelectedSheetName) && !string.IsNullOrEmpty(baseline.SelectedSheetName)
            && !string.Equals(current.SelectedSheetName.Trim(), baseline.SelectedSheetName.Trim(), StringComparison.OrdinalIgnoreCase))
            signals.Add($"SheetChanged:{baseline.SelectedSheetName}->{current.SelectedSheetName}");

        if (baseline.DetectedHeaderRow.HasValue && current.DetectedHeaderRow.HasValue)
        {
            int shift = current.DetectedHeaderRow.Value - baseline.DetectedHeaderRow.Value;
            if (Math.Abs(shift) >= headerRowShift)
                signals.Add($"HeaderRowShift:{(shift >= 0 ? "+" : "")}{shift}");
        }

        if (baseline.HeaderScore.HasValue && current.HeaderScore.HasValue)
        {
            int drop = baseline.HeaderScore.Value - current.HeaderScore.Value;
            if (drop >= headerScoreDrop)
                signals.Add($"HeaderScoreDrop:-{drop}");
        }

        if (baseline.SheetScoreBest.HasValue && current.SheetScoreBest.HasValue)
        {
            int drop = baseline.SheetScoreBest.Value - current.SheetScoreBest.Value;
            if (drop >= bestSheetDrop)
                signals.Add($"BestSheetScoreDrop:-{drop}");
        }

        bool detected = signals.Count > 0;
        string signature = detected ? string.Join(";", signals) : string.Empty;
        return new DriftResult(detected, signals, signature);
    }
}

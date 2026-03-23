using CephasOps.Application.Parser.DTOs;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Phase 10: Recommendation rules for drift report. Single source of truth for tests and DriftReportService.
/// </summary>
public static class DriftReportRecommendations
{
    public const double HighDriftRateThreshold = 0.15;

    public static void Apply(ProfileDriftSummary summary)
    {
        var recs = summary.Recommendations;
        if (summary.ProfileId != Guid.Empty)
            recs.Add("Run replay-profile-pack --profileId " + summary.ProfileId + " before enabling changes.");

        var driftRate = summary.DriftDetectedRate;
        var hasSheetChanged = summary.TopDriftSignatures.Any(s => s.Signature.Contains("SheetChanged", StringComparison.OrdinalIgnoreCase));
        var hasHeaderShift = summary.TopDriftSignatures.Any(s => s.Signature.Contains("HeaderRowShift", StringComparison.OrdinalIgnoreCase) || s.Signature.Contains("HeaderScoreDrop", StringComparison.OrdinalIgnoreCase));
        var dataMissingCount = summary.CountByCategory.GetValueOrDefault("DATA_MISSING", 0);
        var layoutDriftCount = summary.CountByCategory.GetValueOrDefault("LAYOUT_DRIFT", 0);
        var conversionCount = summary.CountByCategory.GetValueOrDefault("CONVERSION_ISSUE", 0);

        if (driftRate >= HighDriftRateThreshold && hasSheetChanged)
            recs.Add("Consider updating preferredSheetNames in PROFILE_JSON (drift signatures show sheet changes).");
        if (hasHeaderShift)
            recs.Add("Consider tightening or adjusting headerRowRange in PROFILE_JSON (HeaderRowShift/HeaderScoreDrop detected).");
        if (dataMissingCount > 0 && dataMissingCount >= layoutDriftCount)
            recs.Add("DATA_MISSING dominant: enforce vendor Excel spec. Top missing fields: " + string.Join(", ", summary.TopMissingFields.Take(5).Select(f => f.FieldName)));
        if (conversionCount > 0)
            recs.Add("CONVERSION_ISSUE present: check .xls conversion and file integrity.");
    }
}

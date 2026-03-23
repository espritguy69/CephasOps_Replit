using System.Globalization;
using System.Text;
using CephasOps.Application.Parser.DTOs;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Phase 10: Console and Markdown formatters for drift-report CLI. PII-safe (only token/field names).
/// </summary>
public static class DriftReportFormatters
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static string FormatConsole(DriftReportResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Drift Report (last " + result.Days + " days) — Generated " + result.GeneratedAtUtc.ToString("yyyy-MM-dd HH:mm", Invariant) + " UTC");
        sb.AppendLine("Total drafts with audit: " + result.TotalDrafts + " | Profiles: " + result.ProfilesWithDrafts);
        if (!string.IsNullOrEmpty(result.ProfileIdFilter))
            sb.AppendLine("Filter: ProfileId = " + result.ProfileIdFilter);
        sb.AppendLine();

        foreach (var s in result.ProfileSummaries)
        {
            sb.AppendLine("--- " + s.ProfileName + " (" + s.ProfileId + ") ---");
            sb.AppendLine("  Total: " + s.TotalDrafts + " | NeedsReview: " + s.NeedsReviewCount + " | DriftDetected: " + s.DriftDetectedCount + " (" + s.DriftDetectedRate.ToString("P1", Invariant) + ")");
            if (s.CountByCategory.Count > 0)
                sb.AppendLine("  Categories: " + string.Join(", ", s.CountByCategory.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).Select(kv => kv.Key + "=" + kv.Value)));
            if (s.TopDriftSignatures.Count > 0)
            {
                sb.AppendLine("  Top drift signatures:");
                foreach (var sig in s.TopDriftSignatures)
                    sb.AppendLine($"    - {sig.Signature}: {sig.Count}");
            }
            if (s.TopMissingFields.Count > 0)
            {
                sb.AppendLine("  Top missing fields:");
                foreach (var f in s.TopMissingFields)
                    sb.AppendLine($"    - {f.FieldName}: {f.Count}");
            }
            if (s.AvgHeaderScore.HasValue || s.AvgBestSheetScore.HasValue)
                sb.AppendLine("  Avg HeaderScore: " + (s.AvgHeaderScore.HasValue ? s.AvgHeaderScore.Value.ToString("F1", Invariant) : "n/a") + " | Avg BestSheetScore: " + (s.AvgBestSheetScore.HasValue ? s.AvgBestSheetScore.Value.ToString("F1", Invariant) : "n/a"));
            if (s.RecentProfileChange)
                sb.AppendLine("  Recent profile change: Yes");
            if (s.ProfileVersion != null || s.EffectiveFrom != null || s.Owner != null)
                sb.AppendLine($"  Version: {s.ProfileVersion ?? "n/a"} | EffectiveFrom: {s.EffectiveFrom ?? "n/a"} | Owner: {s.Owner ?? "n/a"}");
            if (s.Recommendations.Count > 0)
            {
                sb.AppendLine("  Recommendations:");
                foreach (var r in s.Recommendations)
                    sb.AppendLine($"    • {r}");
            }
            sb.AppendLine();
        }

        if (result.ReplayRegressionsByProfile != null && result.ReplayRegressionsByProfile.Count > 0)
        {
            sb.AppendLine("--- Replay regressions (same period) ---");
            foreach (var kv in result.ReplayRegressionsByProfile.OrderByDescending(x => x.Value).ThenBy(x => x.Key))
                sb.AppendLine("  " + kv.Key + ": " + kv.Value);
        }
        return sb.ToString();
    }

    public static string FormatMarkdown(DriftReportResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Drift Report");
        sb.AppendLine();
        sb.AppendLine("> Last **" + result.Days + "** days | Generated **" + result.GeneratedAtUtc.ToString("yyyy-MM-dd HH:mm", Invariant) + "** UTC");
        sb.AppendLine();
        sb.AppendLine("## Executive summary");
        sb.AppendLine();
        sb.AppendLine($"- **Total drafts** (with audit): {result.TotalDrafts}");
        sb.AppendLine($"- **Profiles with drafts**: {result.ProfilesWithDrafts}");
        if (!string.IsNullOrEmpty(result.ProfileIdFilter))
            sb.AppendLine($"- **Filter**: ProfileId = `{EscapeMd(result.ProfileIdFilter)}`");
        sb.AppendLine();

        foreach (var s in result.ProfileSummaries)
        {
            sb.AppendLine($"## {EscapeMd(s.ProfileName)}");
            sb.AppendLine();
            sb.AppendLine("| Metric | Value |");
            sb.AppendLine("|--------|-------|");
            sb.AppendLine($"| Total drafts | {s.TotalDrafts} |");
            sb.AppendLine($"| Needs review | {s.NeedsReviewCount} |");
            sb.AppendLine("| Drift detected | " + s.DriftDetectedCount + " (" + s.DriftDetectedRate.ToString("P1", Invariant) + ") |");
            if (s.AvgHeaderScore.HasValue) sb.AppendLine("| Avg HeaderScore | " + s.AvgHeaderScore.Value.ToString("F1", Invariant) + " |");
            if (s.AvgBestSheetScore.HasValue) sb.AppendLine("| Avg BestSheetScore | " + s.AvgBestSheetScore.Value.ToString("F1", Invariant) + " |");
            if (s.RecentProfileChange) sb.AppendLine("| Recent profile change | Yes |");
            sb.AppendLine($"| ProfileId | `{s.ProfileId}` |");
            if (s.ProfileVersion != null) sb.AppendLine($"| Version | {EscapeMd(s.ProfileVersion)} |");
            if (s.EffectiveFrom != null) sb.AppendLine($"| Effective from | {EscapeMd(s.EffectiveFrom)} |");
            if (s.Owner != null) sb.AppendLine($"| Owner | {EscapeMd(s.Owner)} |");
            sb.AppendLine();

            if (s.CountByCategory.Count > 0)
            {
                sb.AppendLine("### Categories");
                sb.AppendLine();
                sb.AppendLine("| Category | Count |");
                sb.AppendLine("|----------|-------|");
                foreach (var kv in s.CountByCategory.OrderByDescending(x => x.Value).ThenBy(x => x.Key))
                    sb.AppendLine("| " + EscapeMd(kv.Key) + " | " + kv.Value + " |");
                sb.AppendLine();
            }

            if (s.TopDriftSignatures.Count > 0)
            {
                sb.AppendLine("### Top drift signatures");
                sb.AppendLine();
                foreach (var sig in s.TopDriftSignatures)
                    sb.AppendLine($"- **{EscapeMd(sig.Signature)}**: {sig.Count}");
                sb.AppendLine();
            }

            if (s.TopMissingFields.Count > 0)
            {
                sb.AppendLine("### Top missing fields");
                sb.AppendLine();
                foreach (var f in s.TopMissingFields)
                    sb.AppendLine($"- **{EscapeMd(f.FieldName)}**: {f.Count}");
                sb.AppendLine();
            }

            if (s.Recommendations.Count > 0)
            {
                sb.AppendLine("### Recommendations");
                sb.AppendLine();
                foreach (var r in s.Recommendations)
                    sb.AppendLine($"- {EscapeMd(r)}");
                sb.AppendLine();
            }
        }

        if (result.ReplayRegressionsByProfile != null && result.ReplayRegressionsByProfile.Count > 0)
        {
            sb.AppendLine("## Replay regressions (same period)");
            sb.AppendLine();
            sb.AppendLine("| ProfileId | Regressions |");
            sb.AppendLine("|-----------|-------------|");
            foreach (var kv in result.ReplayRegressionsByProfile.OrderByDescending(x => x.Value).ThenBy(x => x.Key))
                sb.AppendLine("| `" + kv.Key + "` | " + kv.Value + " |");
        }
        return sb.ToString();
    }

    private static string EscapeMd(string? s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Replace("|", "\\|", StringComparison.Ordinal).Replace("\r", "").Replace("\n", " ");
    }
}

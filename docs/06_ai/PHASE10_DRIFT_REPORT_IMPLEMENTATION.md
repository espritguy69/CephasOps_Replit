# Phase 10: Automated Profile Drift Insights (CLI-only) — Implementation Report

## Summary

Phase 10 adds a **drift-report** CLI command that produces daily/weekly, PII-safe reports from `ParsedOrderDrafts` ValidationNotes `[Audit]` segments and optional `ParserReplayRuns` data. No parser or validation workflow logic was changed; no schema changes; CLI-only.

---

## Changed / Added Files

### Application (Parser)

| File | Purpose |
|------|---------|
| `Application/Parser/Utilities/AuditNotesTokenParser.cs` | Parses `[Audit]` segment from ValidationNotes into `AuditNotesTokens` (ProfileId, Category, DriftDetected, DriftSignature, Missing, HeaderScore, BestSheetScore, etc.). PII-safe. |
| `Application/Parser/DTOs/ProfileDriftSummary.cs` | Per-profile summary: TotalDrafts, NeedsReviewCount, CountByCategory, DriftDetectedCount/Rate, TopDriftSignatures, TopMissingFields, Avg scores, RecentProfileChange, ProfileVersion/EffectiveFrom/Owner, Recommendations. |
| `Application/Parser/DTOs/DriftReportResult.cs` | Full report: Days, GeneratedAtUtc, TotalDrafts, ProfilesWithDrafts, ProfileIdFilter, ProfileSummaries, ReplayRegressionsByProfile (optional). |
| `Application/Parser/Services/IDriftReportService.cs` | Interface for building the report. |
| `Application/Parser/Services/DriftReportService.cs` | Queries drafts by date (and optional profileId), parses audit tokens, groups by profile, aggregates, loads profile lifecycle via `ITemplateProfileService`, applies recommendation rules. |
| `Application/Parser/Services/DriftReportRecommendations.cs` | Shared recommendation rules (single source for service + tests): replay-profile-pack, preferredSheetNames, headerRowRange, DATA_MISSING/vendor spec, CONVERSION_ISSUE. |
| `Application/Parser/Services/DriftReportFormatters.cs` | `FormatConsole(DriftReportResult)` and `FormatMarkdown(DriftReportResult)` for CLI output. |

### API

| File | Purpose |
|------|---------|
| `Program.cs` | Registered `IDriftReportService`/`DriftReportService`; added **drift-report** CLI branch (before replay CLI): parses `--days`, `--profileId`, `--format`, `--out`, `--includeReplay`, runs report, prints or writes markdown, then `Environment.Exit(0)`. |

### Tests (Phase10)

| File | Purpose |
|------|---------|
| `Parser/Phase10/AuditNotesTokenParserTests.cs` | Parsing: null/empty, no audit segment, no ProfileId/Category, full tokens, missing tokens, DriftDetected/Signature, HeaderScore/BestSheetScore, TemplateAction, Missing list, case-insensitive prefix. |
| `Parser/Phase10/DriftReportRecommendationRulesTests.cs` | Recommendation rules: replay-profile-pack for non-empty profile, preferredSheetNames (high drift + SheetChanged), headerRowRange (HeaderRowShift/HeaderScoreDrop), DATA_MISSING + top missing, CONVERSION_ISSUE, Unassigned no replay-pack. |
| `Parser/Phase10/DriftReportServiceTests.cs` | Grouping by ProfileId, top signatures/missing aggregation, PII safety (report does not contain ServiceId/CustomerName/Address/Phone values; only token field names). |
| `Parser/Phase10/DriftReportFormattersTests.cs` | Console: days/total/recommendations; Markdown: executive summary, tables, categories, signatures, missing fields, pipe escaping. |

---

## Test Commands

```bash
# Run all Phase 10 tests
dotnet test backend/tests/CephasOps.Application.Tests/CephasOps.Application.Tests.csproj --filter "FullyQualifiedName~Phase10"

# Run drift-report CLI (requires appsettings/connection string)
dotnet run --project backend/src/CephasOps.Api/CephasOps.Api.csproj -- drift-report --days 7
dotnet run --project backend/src/CephasOps.Api/CephasOps.Api.csproj -- drift-report --days 14 --profileId <guid>
dotnet run --project backend/src/CephasOps.Api/CephasOps.Api.csproj -- drift-report --days 7 --format markdown --out report.md
dotnet run --project backend/src/CephasOps.Api/CephasOps.Api.csproj -- drift-report --days 7 --includeReplay
```

---

## Sample Drift-Report Output Structure

### Console (default)

```
Drift Report (last 7 days) — Generated 2026-02-09 12:00 UTC
Total drafts with audit: 150 | Profiles: 3

--- Vendor A (a1b2c3d4-...) ---
  Total: 80 | NeedsReview: 12 | DriftDetected: 8 (10.0%)
  Categories: LAYOUT_DRIFT=8, DATA_MISSING=5
  Top drift signatures:
    - SheetChanged:Orders->Data: 6
    - HeaderRowShift:2->3: 2
  Top missing fields:
    - ServiceId: 4
    - CustomerName: 2
  Recommendations:
    • Run replay-profile-pack --profileId a1b2c3d4-... before enabling changes.
    • Consider updating preferredSheetNames in PROFILE_JSON (drift signatures show sheet changes).
```

### Markdown (--format markdown)

```markdown
# Drift Report

> Last **7** days | Generated **2026-02-09 12:00** UTC

## Executive summary

- **Total drafts** (with audit): 150
- **Profiles with drafts**: 3

## Vendor A

| Metric | Value |
|--------|-------|
| Total drafts | 80 |
| Needs review | 12 |
| Drift detected | 8 (10.0%) |
...

### Categories
| Category | Count |
|----------|-------|
| LAYOUT_DRIFT | 8 |
...

### Top drift signatures
- **SheetChanged:Orders->Data**: 6
...

### Recommendations
- Run replay-profile-pack --profileId ... before enabling changes.
- Consider updating preferredSheetNames in PROFILE_JSON (drift signatures show sheet changes).
```

---

## Acceptance Criteria

- **Drift-report produces a useful, PII-safe report:** Only audit tokens and field names (e.g. Missing=ServiceId,CustomerName) appear; no draft customer values (ServiceId, CustomerName, Address, Phone) are printed or stored.
- **Report identifies top drifting profiles and signatures:** Grouped by ProfileId/ProfileName; top 5 drift signatures and top 10 missing fields per profile.
- **Report provides concrete recommended actions per profile:** Replay-profile-pack always (when profile is set); preferredSheetNames when high drift + SheetChanged; headerRowRange when HeaderRowShift/HeaderScoreDrop; DATA_MISSING + top missing; CONVERSION_ISSUE for .xls.
- **Markdown output is clean and readable:** Executive summary, tables per profile, bullets for signatures/missing, recommendations.
- **Tests pass:** 25 Phase10 tests (parser, recommendation rules, service grouping/PII, formatters).

---

## Optional: Replay Regressions

With `--includeReplay`, the service queries `ParserReplayRuns` for the same period where `RegressionDetected` and parses `ResultSummary` JSON for `newProfileId` to populate `ReplayRegressionsByProfile` in the result. Console and Markdown formatters output a “Replay regressions (same period)” section when present.

---

## Confirmed: Exit codes, config, stability, PII, dry-run

- **Deterministic exit codes:** Success exits 0; any failure exits 1.
- **Clean config error handling:** Startup or report failures are caught, message to stderr, exit 1.
- **Stable markdown:** InvariantCulture for date/number; deterministic ordering (TotalDrafts then ProfileId; categories/replay by count then key).
- **No PII leakage:** Only ValidationNotes (tokens), ValidationStatus, CreatedAt from drafts.
- **--dry-run:** Report printed; file not written when --out set; stderr note.

# Phase 8: Template Profiles v2 + Drift Auto-Triage — Implementation Report

## Summary

Phase 8 adds **Template Profiles** (stored in existing `ParserTemplates` via `Description` with `PROFILE_JSON:` prefix), **parser hints** (preferred sheets, header row range, synonym overrides), **DriftDetector** with baseline from last successful draft, and **enrichment** of ParseReport, ValidationNotes, and Replay ResultSummary with profile and drift (PII-safe). No parser business rules were changed; all behavior is additive and behind safe defaults.

---

## 1. Files Changed / Added

### New files

| Path | Purpose |
|------|--------|
| `Application/Parser/DTOs/TemplateProfileConfig.cs` | JSON-serializable profile config (matchRules, parseHints, driftThresholds) |
| `Application/Parser/DTOs/TemplateProfileContext.cs` | Context passed to parser (profile id/name, preferred sheets, header range, synonym overrides, drift thresholds) |
| `Application/Parser/Services/ITemplateProfileService.cs` | Get best-matching profile for email/attachment |
| `Application/Parser/Services/TemplateProfileService.cs` | Loads enabled templates, parses PROFILE_JSON from Description, match specificity: partnerId > filenameRegex > senderDomain > subjectContains |
| `Application/Parser/Services/IDriftBaselineProvider.cs` | Get baseline from last Valid draft for a profile |
| `Application/Parser/Services/DriftBaselineProvider.cs` | Reads baseline from ValidationNotes audit segment |
| `Application/Parser/Utilities/DriftDetector.cs` | Compares current ParseReport to baseline; produces DriftDetected, DriftSignals, DriftSignature |
| `tests/.../Parser/Phase8/TemplateProfileServiceTests.cs` | Profile matching, invalid JSON ignored, enabled=false, specificity |
| `tests/.../Parser/Phase8/DriftDetectorTests.cs` | No baseline, sheet change, header shift, score drops, within thresholds |
| `tests/.../Parser/Phase8/DriftBaselineProviderTests.cs` | GetBaselineAsync with Valid draft |
| `tests/.../Parser/Phase8/EnrichmentAndReplayPhase8Tests.cs` | Replay ResultSummary includes profile + drift |

### Modified files

| Path | Changes |
|------|--------|
| `Application/Parser/DTOs/ParseReport.cs` | Added TemplateProfileId, TemplateProfileName, DriftDetected, DriftSignature |
| `Application/Parser/Services/ITimeExcelParserService.cs` | `ParseAsync(IFormFile, TemplateProfileContext?, CancellationToken)` |
| `Application/Parser/Services/ISyncfusionExcelParserService.cs` | Same optional `TemplateProfileContext?` |
| `Application/Parser/Services/SyncfusionExcelParserService.cs` | Optional profileContext; preferred sheet boost; header range (min/max); FieldDiagnosticsBuilder with overrides; inject IDriftBaselineProvider; set profile + drift on report when LAYOUT_DRIFT |
| `Application/Parser/Utilities/SheetHeaderDetector.cs` | `DetectHeaderRow(sheet, minRow, maxRow)` overload |
| `Application/Parser/Utilities/FieldDiagnosticsBuilder.cs` | `Build(..., synonymOverrides, headerRowRange)`; MergeSynonyms |
| `Application/Parser/Services/ParsedOrderDraftEnrichmentService.cs` | BuildParseReportAuditLine appends Profile=, ProfileName=, DriftDetected=, DriftSignature=, TemplateAction=UpdateProfile when LAYOUT_DRIFT + DriftDetected |
| `Application/Parser/Services/ParserReplayService.cs` | ParseValidationNotesDiagnostics parses profile/drift from audit; ResultSummary and reasonForChange include old/new profile + drift |
| `Api/Program.cs` | Registered ITemplateProfileService, IDriftBaselineProvider |
| `Application/Parser/Services/ParserReplayService.cs` | ParseAsync(formFile, null, cancellationToken) |
| `Application/Parser/Services/EmailIngestionService.cs` | ParseAsync(..., null, cancellationToken) (2 call sites) |
| `Application/Parser/Services/ParserService.cs` | ParseAsync(..., null, cancellationToken) |
| `tests/.../Parser/Services/ParserReplayServiceIntegrationTests.cs` | Mock ParseAsync(..., It.IsAny<TemplateProfileContext?>(), ...) |
| `tests/.../Parser/Utilities/SheetHeaderDetectorTests.cs` | DetectHeaderRow_WithRange_ScansOnlyMinMaxRows |
| `tests/.../Parser/FieldDiagnosticsBuilderTests.cs` | Build_WithSynonymOverrides_UsesOverridesForField |

---

## 2. Storage: ProfileConfig

- **No new columns.** ProfileConfig is stored in `ParserTemplates.Description` with prefix `PROFILE_JSON: {...}`.
- Valid JSON after the prefix is deserialized into `TemplateProfileConfig`; invalid or missing prefix is ignored (no throw).

---

## 3. Behavior Summary

- **TemplateProfileService**: Loads active templates, parses Description for `PROFILE_JSON:`, keeps only `enabled: true`. Best match by specificity: partnerId (1000) > filenameRegex (100) > senderDomain (10) > subjectContains (1).
- **Parser**: If `profileContext` is provided: (1) preferred sheet names get +2 score when score ≥ 1; (2) header detection uses `headerRowRange` (min..max); (3) field diagnostics use merged synonyms (profile overrides win). If no profile, behavior is unchanged (Phase 7).
- **DriftDetector**: Baseline = latest Valid ParsedOrderDraft for same ParserTemplateId; baseline parsed from ValidationNotes `[Audit] Sheet=...; HeaderRow=...; HeaderScore=...; BestSheetScore=...`. Compares sheet name, header row shift, header score drop, best sheet score drop against profile driftThresholds (defaults 3, 2, 3). No baseline ⇒ DriftDetected = false.
- **LAYOUT_DRIFT**: When category is LAYOUT_DRIFT and profile + DriftDetector ran: ParseReport gets DriftDetected, DriftSignature; ValidationNotes audit line gets Profile=, DriftDetected=, DriftSignature=, and TemplateAction=UpdateProfile when DriftDetected is true.

---

## 4. Tests (10+)

| Test | Coverage |
|------|----------|
| GetBestMatchProfileAsync_NoTemplates_ReturnsNull | No templates |
| GetBestMatchProfileAsync_InvalidJsonInDescription_IgnoresTemplate | Invalid JSON safely ignored |
| GetBestMatchProfileAsync_ValidProfile_Disabled_ReturnsNull | enabled=false |
| GetBestMatchProfileAsync_SenderDomainMatch_ReturnsProfile | Sender domain match |
| GetBestMatchProfileAsync_PartnerIdMatch_BeatsSenderDomain | Specificity: partnerId > senderDomain |
| Detect_NoBaseline_ReturnsNotDetected | No baseline ⇒ not detected |
| Detect_SheetChanged_BeyondThreshold_Detected | Sheet name change |
| Detect_HeaderRowShift_BeyondThreshold_Detected | Header row shift |
| Detect_HeaderScoreDrop_BeyondThreshold_Detected | Header score drop |
| Detect_BestSheetScoreDrop_BeyondThreshold_Detected | Best sheet score drop |
| Detect_WithinThresholds_NotDetected | Within thresholds |
| GetBaselineAsync_NoValidDraft_ReturnsNull | No Valid draft |
| GetBaselineAsync_ValidDraft_ReturnsBaseline | Baseline from audit segment |
| DetectHeaderRow_WithRange_ScansOnlyMinMaxRows | Header range restricts scan |
| Build_WithSynonymOverrides_UsesOverridesForField | Synonym overrides applied |
| Replay_ResultSummary_IncludesProfileAndDriftFields | ResultSummary + reasonForChange include profile/drift |

All 174 Parser-related tests pass (including existing Phase 6/7 and new Phase 8).

---

## 5. How to Validate Using Replay-Pack

1. **Run replay on recent attachments (e.g. last 30 days)**  
   Use existing replay API or harness that calls `IParserReplayService.ReplayByAttachmentIdAsync` for each attachment.

2. **Inspect ResultSummary**  
   For runs where the new parser path was used, ResultSummary JSON should include when applicable:
   - `oldProfileId`, `oldProfileName`, `oldDriftDetected`, `oldDriftSignature`
   - `newProfileId`, `newProfileName`, `newDriftDetected`, `newDriftSignature`
   - `reasonForChange` containing `DriftSignature=...` when category is LAYOUT_DRIFT and drift was detected.

3. **Enable profiles gradually**  
   - Add 1–2 partner profiles: set `Description` to `PROFILE_JSON: {"profileId":"...", "profileName":"...", "enabled": true, "matchRules": {...}, "parseHints": {...}, "driftThresholds": {...}}`.
   - Keep `enabled: false` until replay-pack has been run and reviewed.
   - Wire ingestion to resolve profile (e.g. call `ITemplateProfileService.GetBestMatchProfileAsync(sender, subject, fileName, partnerId, companyId)`) and pass the returned context into `ParseAsync(file, profileContext, cancellationToken)` when you are ready to use profiles in production.

4. **Check ValidationNotes**  
   For LAYOUT_DRIFT parses with a profile and drift detection, draft ValidationNotes should contain the audit segment with `Profile=...`, `DriftDetected=...`, `DriftSignature=...`, and optionally `TemplateAction=UpdateProfile`.

---

## 6. Rollout (Safe Defaults)

- Profiles are used only when `enabled: true` in ProfileConfig and a template matches.
- Parser behavior is unchanged when no profile is passed (`profileContext == null`).
- Drift detection runs only when category is LAYOUT_DRIFT and a profile context and IDriftBaselineProvider are present; no baseline ⇒ DriftDetected = false.
- Structure Gate unchanged: missing required ⇒ Success=false, ParseStatus=FailedRequiredFields, confidence=0; no partial success.

---

## 7. Test Commands

```bash
cd backend
dotnet test tests/CephasOps.Application.Tests/CephasOps.Application.Tests.csproj --filter "FullyQualifiedName~Parser"
```

To run only Phase 8 and related tests:

```bash
dotnet test tests/CephasOps.Application.Tests/CephasOps.Application.Tests.csproj --filter "FullyQualifiedName~Phase8|FullyQualifiedName~ParserReplayService|FullyQualifiedName~SheetHeaderDetector|FullyQualifiedName~FieldDiagnosticsBuilder"
```

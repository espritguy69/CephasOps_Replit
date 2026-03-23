# Phase 9: Profile Lifecycle + Profile Pack Regression — Implementation Report

## Summary

Phase 9 adds **profile versioning and pack** fields to PROFILE_JSON (no schema change), **replay-profile-pack** and **replay-profiles** CLI commands, **ResultSummary** lifecycle metadata, **Profile_Lifecycle.md** operations doc, and **guardrails** (NO-GO output and exit code 1 when regressions exist). All behavior is additive and backward compatible.

---

## 1. Files Changed / Added

### New files

| Path | Purpose |
|------|--------|
| `Application/Parser/DTOs/ProfileLifecycleContext.cs` | Optional context for replay (profileId, profileName, profileVersion, effectiveFrom, owner, packName, profileChangeNotes) |
| `docs/operations/Profile_Lifecycle.md` | How to edit PROFILE_JSON, version, pack, replay commands, rollback |
| `docs/06_ai/PHASE9_PROFILE_LIFECYCLE_REPORT.md` | This report |
| `tests/.../Parser/Phase9/TemplateProfileConfigV2Tests.cs` | Versioning fields parsing, missing fields default null, pack parsing, GetProfileConfigById null cases |
| `tests/.../Parser/Phase9/PackResolutionTests.cs` | ResolvePackAttachmentIds: attachmentIds direct, parseSessionIds fallback, empty pack |
| `tests/.../Parser/Phase9/ReplayLifecycleAndProfilesTests.cs` | ResultSummary includes version/owner/packName, GetAttachmentIdsForProfile, GetAllProfileConfigs enabledOnly, result category/reason |

### Modified files

| Path | Changes |
|------|--------|
| `Application/Parser/DTOs/TemplateProfileConfig.cs` | Added ProfileVersion, EffectiveFrom, ChangeNotes, Owner, Pack (ProfilePackConfig with AttachmentIds, ParseSessionIds, PackName, PackDescription) |
| `Application/Parser/Services/ITemplateProfileService.cs` | GetProfileConfigByIdAsync, GetAllProfileConfigsAsync, ResolvePackAttachmentIdsAsync |
| `Application/Parser/Services/TemplateProfileService.cs` | Implemented GetProfileConfigByIdAsync (by template Id), GetAllProfileConfigsAsync (enabledOnly), ResolvePackAttachmentIdsAsync (attachmentIds + parseSessionIds fallback) |
| `Application/Parser/DTOs/ParserReplayResult.cs` | OldParseFailureCategory, NewParseFailureCategory, ReasonForChange, NewDriftSignature |
| `Application/Parser/Services/IParserReplayService.cs` | ReplayByAttachmentIdAsync(..., ProfileLifecycleContext?), GetAttachmentIdsForProfileAsync(profileId, days) |
| `Application/Parser/Services/ParserReplayService.cs` | ReplayByAttachmentIdAsync accepts lifecycleContext; BuildResultSummaryJson adds profileId, profileName, profileVersion, effectiveFrom, owner, packName, profileChangeNotes; result populated with category/reason/drift; GetAttachmentIdsForProfileAsync; ReplayPackAsync/ReplayByParseSessionIdAsync pass null for lifecycle |
| `Api/Program.cs` | replay-profile-pack --profileId; replay-profiles --days; NO-GO section and exit 1 when regressions; using Parser.DTOs |

---

## 2. Profile JSON schema v2 (no new DB column)

Stored in `ParserTemplates.Description` as `PROFILE_JSON: { ... }`. New optional fields:

- **profileVersion** (string, e.g. `"1.0.0"`)
- **effectiveFrom** (ISO date string)
- **changeNotes** (string)
- **owner** (string)
- **pack**: **attachmentIds** (array of Guid), **parseSessionIds** (array of Guid), **packName**, **packDescription**

If absent, all default to null/empty. TemplateProfileService parses and exposes them.

---

## 3. CLI commands

### replay-profile-pack

```bash
dotnet run --project src/CephasOps.Api -- replay-profile-pack --profileId <guid>
```

- **profileId:** ParserTemplate Id (the row that contains PROFILE_JSON).
- Loads profile config (enabled or not). Resolves pack from attachmentIds then parseSessionIds.
- Replays each attachment via ParserReplayService with TriggeredBy=ProfilePack and ProfileLifecycleContext.
- Output: Total, Regressions, Improvements, NoChange, Errors; first 50 result lines.
- If **Regressions > 0:** NO-GO section with attachmentId, old/new status, old/new category, DriftSignature, ReasonForChange; guidance to revert profileVersion/restore JSON. **Exit code 1.**

### replay-profiles

```bash
dotnet run --project src/CephasOps.Api -- replay-profiles --days 30
```

- **days:** Default 30. For each **enabled** profile, selects recent drafts where ValidationNotes contains `Profile=<profileId>` (audit segment), resolves distinct attachments, replays each with lifecycle context.
- Output: Per-profile metrics; overall total and regressions. **Exit code 1** if any regressions.

---

## 4. ResultSummary lifecycle metadata

When replay is run with ProfileLifecycleContext (replay-profile-pack or replay-profiles), ResultSummary JSON includes:

- **profileId**, **profileName**
- **profileVersion**, **effectiveFrom**, **owner**
- **packName** (if pack replay)
- **profileChangeNotes**

Existing Phase 8 fields (old/new profile, drift) remain.

---

## 5. Guardrails

- **No auto-disable:** Profiles are never disabled by code. Human-in-the-loop only.
- **replay-profile-pack:** On regressions, prints NO-GO and exit 1; suggests reverting version or restoring previous JSON.
- **ParserReplayRun:** Runs from replay-profile-pack use TriggeredBy=ProfilePack so they are searchable.

---

## 6. Tests (10+)

| Test | Coverage |
|------|----------|
| GetProfileConfigByIdAsync_ParsesVersioningFields_WhenPresent | profileVersion, effectiveFrom, changeNotes, owner |
| GetProfileConfigByIdAsync_MissingVersioningFields_DefaultsNull | Backward compat |
| GetProfileConfigByIdAsync_ParsesPack_WhenPresent | packName, packDescription, attachmentIds |
| GetProfileConfigByIdAsync_NotFound_ReturnsNull | Missing template |
| GetProfileConfigByIdAsync_TemplateHasNoProfileJson_ReturnsNull | No PROFILE_JSON prefix |
| ResolvePackAttachmentIdsAsync_AttachmentIdsDirect_ReturnsThem | attachmentIds only |
| ResolvePackAttachmentIdsAsync_ParseSessionIdsFallback_ResolvesViaSessionAndDraftFileName | parseSessionIds path |
| ResolvePackAttachmentIdsAsync_EmptyPack_ReturnsEmpty | Empty pack |
| ReplayByAttachmentIdAsync_WithLifecycleContext_ResultSummaryIncludesVersionOwnerPackName | Lifecycle in ResultSummary |
| GetAttachmentIdsForProfileAsync_ReturnsAttachmentsForDraftsWithProfileInNotes | replay-profiles grouping |
| GetAllProfileConfigsAsync_EnabledOnly_ReturnsOnlyEnabledConfigs | enabledOnly filter |
| ReplayByAttachmentIdAsync_Result_PopulatesCategoryAndReasonForChange | Old/New category, Reason, DriftSignature on result |

All 183 Parser-related tests pass.

---

## 7. How to run and interpret

### Run replay-profile-pack

1. Get the ParserTemplate Id for the profile (e.g. from DB or admin).
2. Ensure the template’s Description contains PROFILE_JSON with a **pack** (attachmentIds and/or parseSessionIds).
3. Run:
   ```bash
   dotnet run --project src/CephasOps.Api -- replay-profile-pack --profileId <that-guid>
   ```
4. **Interpret:** Total = number of attachments replayed. Regressions = count of runs where replay is worse than baseline. If Regressions > 0, the NO-GO block lists each regression with attachment id, status/category, drift signature, and reason; fix the profile or revert and re-run until clean, then consider enabling.

### Run replay-profiles

```bash
dotnet run --project src/CephasOps.Api -- replay-profiles --days 30
```

- Uses only **enabled** profiles. For each, finds drafts in the last 30 days whose audit segment contains that profile’s Profile= id, then replays those attachments. Per-profile and overall regression counts; exit 1 if any regression.

### Test commands

```bash
cd backend
dotnet test tests/CephasOps.Application.Tests/CephasOps.Application.Tests.csproj --filter "FullyQualifiedName~Phase9"
dotnet test tests/CephasOps.Application.Tests/CephasOps.Application.Tests.csproj --filter "FullyQualifiedName~Parser"
```

---

## 8. Rollout

- No schema changes; PROFILE_JSON is backward compatible.
- New CLI commands are additive. Existing replay and replay-pack behavior unchanged.
- Run replay-profile-pack before enabling or after editing a profile. Use replay-profiles to monitor enabled profiles over recent traffic.

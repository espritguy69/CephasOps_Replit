# Profile Lifecycle and Safe Profile Updates

This document describes how to manage **Template Profiles** (Phase 8/9) safely: editing PROFILE_JSON, versioning, maintaining regression packs, and using replay commands before enabling profiles in production.

---

## 1. Where profile config is stored

- **Table:** `ParserTemplates`
- **Column:** `Description` (nullable string)
- **Format:** Profile JSON is stored with a prefix: `PROFILE_JSON: { ... }`
- **No separate column:** All profile fields (match rules, parse hints, drift thresholds, versioning, pack) live in this single JSON block. No schema change is required.

---

## 2. PROFILE_JSON schema (v2 with versioning and pack)

Example (minimal):

```json
{
  "profileId": "00000000-0000-0000-0000-000000000000",
  "profileName": "TIME FTTH",
  "enabled": false,
  "profileVersion": "1.0.0",
  "effectiveFrom": "2025-02-01",
  "changeNotes": "Initial pack",
  "owner": "ops@example.com",
  "matchRules": {
    "senderDomains": ["time.com.my"],
    "subjectContains": ["FTTH"],
    "filenameRegex": [".*\\.xlsx?"],
    "partnerIds": []
  },
  "parseHints": {
    "preferredSheetNames": ["Orders", "Sheet1"],
    "headerRowRange": { "min": 1, "max": 30 },
    "requiredFieldSynonymOverrides": {}
  },
  "driftThresholds": {
    "bestSheetScoreDrop": 3,
    "headerScoreDrop": 2,
    "headerRowShift": 3
  },
  "pack": {
    "packName": "TIME golden set",
    "packDescription": "Curated attachments for regression",
    "attachmentIds": ["guid1", "guid2"],
    "parseSessionIds": []
  }
}
```

**Backward compatibility:** If `profileVersion`, `effectiveFrom`, `changeNotes`, `owner`, or `pack` are absent, they are treated as null/empty. Existing profiles without these fields continue to work.

**Validation:** Validate JSON before saving (e.g. paste into a JSON validator). Invalid JSON in `Description` is ignored by the parser; the template will not act as a profile until the JSON is fixed.

---

## 3. How to edit PROFILE_JSON safely

1. **Get current value:** Read `ParserTemplates.Description` for the template (e.g. via admin UI or DB).
2. **Ensure prefix:** The value must start with `PROFILE_JSON: ` (space after colon is optional but the prefix is required).
3. **Edit only the JSON part:** After the prefix, edit the JSON. Keep `profileId` and `profileName` in sync with the template if you use them.
4. **Validate:** Run the JSON through a validator. Ensure all strings are quoted and commas are correct.
5. **Save:** Update `Description` and leave the rest of the row unchanged.

---

## 4. Versioning and change notes

- **profileVersion:** String (e.g. `"1.0.0"`). Bump when you change match rules, parse hints, or drift thresholds.
- **effectiveFrom:** ISO date string (e.g. `"2025-02-01"`). When this version became or will become effective.
- **changeNotes:** Short note describing the change (e.g. "Added preferredSheetNames for Orders").
- **owner:** Email or name of the person responsible for the profile.

**Before enabling in production:**

1. Bump `profileVersion` (e.g. 1.0.0 → 1.1.0).
2. Set `effectiveFrom` to the intended date.
3. Fill `changeNotes` with a one-line summary.
4. Run **replay-profile-pack** (see below). If there are regressions, fix the profile or revert the JSON; do not enable until the pack is green.

---

## 5. Maintaining a pack

- **pack.attachmentIds:** Preferred. List of `EmailAttachment` IDs (GUIDs) that are your “golden” files for this profile.
- **pack.parseSessionIds:** Optional fallback. List of `ParseSession` IDs; the system resolves attachments via session + draft `SourceFileName` (same logic as replay-by-session).
- **pack.packName** / **pack.packDescription:** Informational; used in replay output and ResultSummary.

**To add/remove attachments:**

1. Identify the correct `EmailAttachment` IDs (e.g. from replay-pack output or from the email/parser UI).
2. Edit the `pack.attachmentIds` array in PROFILE_JSON: add or remove GUIDs.
3. Re-run **replay-profile-pack** to confirm no regressions.

---

## 6. Running replay-profile-pack before enabling

**Command:**

```bash
dotnet run --project src/CephasOps.Api -- replay-profile-pack --profileId <ParserTemplateId>
```

**Behavior:**

- Loads the profile config by template ID (even if `enabled` is false).
- Resolves pack attachments from `attachmentIds` and, if needed, `parseSessionIds`.
- Replays each attachment through the current parser and records results in `ParserReplayRuns` with `TriggeredBy=ProfilePack`.
- Prints: Total, Regressions, Improvements, NoChange, Errors.
- If **Regressions > 0:** prints a **NO-GO** section with attachment id, old/new status, old/new category, drift signature, reason; suggests reverting profileVersion or restoring previous JSON. **Exit code 1.**

**Recommendation:** Run this after every profile JSON change. Only set `enabled: true` when the pack shows no regressions.

---

## 7. Running replay-profiles (per-profile regression over recent traffic)

**Command:**

```bash
dotnet run --project src/CephasOps.Api -- replay-profiles --days 30
```

**Behavior:**

- For each **enabled** profile, finds recent drafts (last N days) whose `ValidationNotes` contain `Profile=<profileId>` (from the audit segment).
- Resolves distinct attachments from those drafts and replays each with profile lifecycle context.
- Prints per-profile metrics and overall total/regressions.
- **Exit code 1** if any profile has regressions.

Use this to catch regressions across recent production-like traffic for all enabled profiles.

---

## 8. Rollback procedure

If a profile update causes regressions (in pack or in replay-profiles):

1. **Revert the JSON:** Restore the previous `ParserTemplates.Description` (either the whole value or the PROFILE_JSON block). You can keep a backup of the last-known-good JSON in version control or a doc.
2. **Optionally revert version:** Set `profileVersion` and `changeNotes` to reflect the rollback (e.g. "Reverted to 1.0.0 – 1.1.0 caused regressions").
3. **Re-run replay-profile-pack** to confirm the reverted profile is green.
4. **Do not auto-disable in code:** Rollback is a human decision; the system only reports NO-GO and exit code 1.

---

## 9. ResultSummary and lifecycle metadata

When you run **replay-profile-pack** or **replay-profiles**, each `ParserReplayRun.ResultSummary` JSON may include:

- **profileId**, **profileName**
- **profileVersion**, **effectiveFrom**, **owner**
- **packName** (if run was from a pack)
- **profileChangeNotes**

Use these to trace why a regression occurred (e.g. after a profile version bump or owner change).

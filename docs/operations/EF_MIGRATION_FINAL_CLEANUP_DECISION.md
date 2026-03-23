# EF Migration Final Cleanup — Decision

**Date:** Finalization and historical cleanup pass.  
**Outcome:** Documentation and classification only. No migrations deleted or archived.

---

## Was any old migration deleted?

**No.** Zero migration files were deleted. No `.cs` or `.Designer.cs` migration files were removed.

---

## Was any migration archived?

**No.** No migration was moved to an archive folder. No archive folder was created. Preservation is safer than false cleanup; no migration met the bar for “safe archive candidate” (see `docs/operations/EF_MIGRATION_FINAL_CLEANUP_AUDIT.md`).

---

## Which migrations remain active?

**94** migrations remain **active** (EF-discoverable). They have a `.Designer.cs` and appear in `dotnet ef migrations list`. They are the only migrations applied by `dotnet ef database update` and the migration bundle. Full list: `docs/operations/EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md` §2.1. Last: **20260309120000_AddJobRunEventId**.

---

## Which remain script-only?

**44** migrations remain **script-only** (no Designer). They are **not** applied by `dotnet ef database update`. Apply them via idempotent or repair scripts when their schema is required. Full list and script references: `docs/operations/EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md` §2.2 and `docs/operations/EF_MIGRATION_FINAL_CLEANUP_AUDIT.md`.

---

## Which remain legacy reference only?

**None.** No migration was classified as “legacy reference only.” All 142 are either active (95) or script history (47).

---

## Is the repository now operationally clean enough for day-to-day work?

**Yes.** The migration path is clearly documented: 95 active (EF path), 47 script-only (apply via scripts). One authoritative audit, manifest, runbook, and future authoring rules are in place. No clutter was removed (no deletions or archival), but the **decision set** is clear so developers know what to use and what not to touch.

---

## What is still deferred to re-baseline?

- **Snapshot reconciliation:** The model snapshot remains behind the current domain model. A one-time sync migration or a full re-baseline (see `docs/operations/EF_REBASELINE_PLAN.md`) is deferred. Use PendingModelChangesWarning suppression and the current chain.
- **Designer regeneration:** No Designer files were regenerated for the 47; they remain script-only by design.
- **Archival:** No migrations were archived; all stay in `Migrations/` for traceability and script-based application.

---

## What is the one next step before anyone adds another migration?

- **Read** `docs/operations/EF_FUTURE_AUTHORING_RULES.md` and `backend/scripts/MIGRATION_RUNBOOK.md`.
- **Create** the new migration with `dotnet ef migrations add <Name> --project ...` so it gets a Designer and stays in the discovered chain. Do not delete the Designer.
- If the generated migration is **unusually large** (snapshot drift), review it and consider a dedicated snapshot reconciliation pass before committing.

---

## Validation (Phase 7)

- **File counts:** `backend/scripts/audit-migration-designers.ps1` reports: **142** main migration `.cs` files, **95** with Designer, **47** missing Designer. No migration files were moved or deleted; the `Migrations/` folder contains the full set.
- **Docs:** `docs/MIGRATION_HYGIENE.md`, `docs/operations/EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md`, `docs/operations/EF_MIGRATION_RECOVERY_DECISION.md`, `docs/operations/EF_MIGRATION_OPERATIONAL_CLOSURE_DECISION.md`, and `backend/scripts/MIGRATION_RUNBOOK.md` are consistent with 95 discoverable and 47 script-only. No contradictions.
- **Active chain:** All 95 discoverable migrations remain in `Migrations/` with their `.Designer.cs`; none were archived or moved.

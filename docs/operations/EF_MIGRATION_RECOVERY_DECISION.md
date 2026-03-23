# EF Migration Recovery — Final Decision

**Date:** End of migration recovery pass.  
**Outcome:** Partial recovery only; no Designer regeneration; no snapshot change; re-baseline deferred.

---

## 1. Is the migration chain now fully discoverable?

**No.** The chain remains **partially discoverable**: **95 migrations** have a `.Designer.cs` and are discovered by `dotnet ef migrations list` / `dotnet ef database update`. **44 migrations** have no Designer and are **not** discoverable; they remain script-only and are tracked in the no-Designer manifest and classification doc.

---

## 2. How many missing designers were recovered?

**Zero.** No `.Designer.cs` files were regenerated. All 44 no-Designer migrations were classified as either **SAFE TO LEAVE SCRIPT-ONLY** (26) or **NEEDS MANUAL INTERVENTION** (18). Reconstructing correct historical `BuildTargetModel` for any of them without the original snapshot was deemed unsafe; see `EF_NO_DESIGNER_RECOVERY_CLASSIFICATION.md`.

---

## 3. Which migrations remain intentionally script-only?

All **44** migrations listed in `EF_MIGRATION_FULL_AUDIT_RECOVERY_PASS.md` (§2.2) remain script-only. They are listed and classified in `EF_NO_DESIGNER_RECOVERY_CLASSIFICATION.md`. Apply them via idempotent scripts (or repair scripts where they exist); see `EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md` and `backend/scripts/MIGRATION_RUNBOOK.md`.

---

## 4. Is the snapshot safe for future migration authoring?

**Only with caveats.** The snapshot is **drifted** (behind the current model and many migrations). Adding a new migration with `dotnet ef migrations add` will produce a **large diff** because EF compares the current model to this old snapshot. PendingModelChangesWarning is **suppressed** so that `database update` and tooling can run. For **future authoring**, developers can continue to add migrations with `dotnet ef migrations add`; the new migration will capture the delta from the current snapshot to the current model (so the first such migration after this pass could be large). Long-term, snapshot reconciliation (sync migration or re-baseline) is recommended in a dedicated pass; see `EF_MODEL_SNAPSHOT_RECONCILIATION_REPORT.md` and `EF_REBASELINE_PLAN.md`.

---

## 5. Is re-baseline still recommended?

**Deferred, not “recommended now”.** Re-baseline is **recommended only when** the team is ready to invest in a two-track strategy (legacy chain for existing DBs, baseline for new/greenfield) and to maintain the plan in `EF_REBASELINE_PLAN.md`. This pass did **not** execute any re-baseline; it only produced the plan.

---

## 6. Remaining risks

- **44 script-only migrations:** Must be applied via scripts when their schema is required; ordering and duplicate timestamps are documented but remain a source of possible human error.
- **Snapshot drift:** Noisy diffs on next `migrations add`; PendingModelChangesWarning suppressed.
- **Duplicate timestamps:** 10 pairs of migrations share the same timestamp; ordering is by full migration ID; do not rename without explicit need and documentation.
- **Build blocker:** Infrastructure project currently fails to build due to `NotificationDispatchStore.cs` (Application reference); unrelated to migrations but blocks `dotnet ef` when it triggers a build. Use `--no-build` after a successful build where possible.

---

## 7. What should developers do next when adding a migration?

1. Use **`dotnet ef migrations add YourMigrationName`** from the Api project (with Design-time factory and connection configured). This generates both the migration `.cs` and the `.Designer.cs`; **do not delete the Designer.**
2. For risky operations (e.g. dropping an index), prefer **idempotent SQL** in the migration (e.g. `DROP INDEX IF EXISTS`).
3. If the generated migration is unexpectedly large (because of snapshot drift), review it carefully; consider whether to accept it as a one-time “sync” or to defer and do a dedicated snapshot reconciliation pass first.

---

## 8. What should operations use for deployment?

- **Existing/drifted DBs:** Prefer **idempotent SQL script** (generate with `dotnet ef migrations script --idempotent`) or apply scripts from the runbook; use repair scripts when tables are missing. See `backend/scripts/MIGRATION_RUNBOOK.md` and execution strategy by environment.
- **Aligned dev DBs:** **`dotnet ef database update`** when no process is locking the Api DLLs (or use `--no-build` after a successful build).
- **Staging/Prod:** Prefer **migration bundle** or **idempotent script**; backup first; apply no-Designer migrations via scripts per manifest when required.

---

## 9. Operational recommendation (summary)

**Partial recovery only; manual intervention required for long-term hygiene.**

- **Recovered:** Full audit, classification of all 44 no-Designer migrations, snapshot reconciliation report, re-baseline plan, validation report. No Designer files were regenerated; no snapshot or migration logic was changed.
- **Next steps:** (1) Keep using the discovered chain and script-only migrations per runbook and manifest. (2) When ready, either run a one-time sync migration (idempotent) to align snapshot with model, or execute the re-baseline plan for new environments. (3) Fix Infrastructure build (NotificationDispatchStore) so that full build and EF commands without `--no-build` succeed.

**Final cleanup:** See **`docs/operations/EF_MIGRATION_FINAL_CLEANUP_AUDIT.md`** and **`docs/operations/EF_MIGRATION_FINAL_CLEANUP_DECISION.md`** for the authoritative classification (95 active, 44 script-only) and **`docs/operations/EF_FUTURE_AUTHORING_RULES.md`** for strict authoring rules. No migrations were deleted or archived.

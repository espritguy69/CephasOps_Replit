# EF Migration Operational Closure — Decision Note

**Date:** Operational closure pass (validation, runbook, no-Designer manifest).  
**Status:** Chain is operationally usable under defined conditions.

---

## Is the current chain operationally usable now?

**Yes**, for environments that meet the conditions below. The discovered migration chain can be applied via `dotnet ef database update` or the migration bundle, and repaired migrations (20260309053019, 20260309065620) are idempotent so drifted DBs (e.g. schema applied via scripts) do not fail on missing/extra objects.

---

## For which environment types?

| Type | Usable? | Condition |
|------|---------|-----------|
| **Local / dev (aligned)** | Yes | No CephasOps.Api process running when running EF tools; use `--no-build` after build. |
| **Local / dev (drifted)** | Yes | Use idempotent script or run `database update` after applying stabilization (repaired migrations are in codebase). Run repair scripts if tables are missing. |
| **Staging / prod** | Yes | Prefer migration bundle or idempotent script. Backup first. Apply no-Designer migrations via scripts from manifest when required. |

---

## What exact conditions must be met?

1. **EF tools (local):** No process holding Api output DLLs (stop `dotnet run` / debugger) when running `dotnet ef database update` or `dotnet ef migrations list`; or use `--no-build` after a successful build.
2. **Database:** Connection string set (e.g. `ASPNETCORE_ENVIRONMENT=Development`, or `ConnectionStrings__DefaultConnection`).
3. **Stabilization:** Codebase includes repaired migrations 20260309053019 (ParsedMaterialAliases conditional) and 20260309065620 (EventStore Phase 7 + EventStoreAttemptHistory idempotent). No snapshot or re-baseline required for current use.
4. **No-Designer migrations:** If the app requires schema from migrations that have no Designer, apply them via idempotent scripts (see manifest); do not rely on `database update` to create those objects.

---

## What must happen before a future sync migration or re-baseline?

- **Sync migration (one-time):** When the team wants the snapshot to match the current model, generate a single migration (e.g. `dotnet ef migrations add SyncSnapshot`), make its `Up()` idempotent (ADD COLUMN IF NOT EXISTS, CREATE TABLE IF NOT EXISTS, etc.), review and test, then apply. Requires backup and a dedicated pass.
- **Re-baseline:** Only if the chain is abandoned for new environments; would require a new “initial” migration from current model and a separate strategy for existing DBs. Not recommended until the above is insufficient.

No obligation to do either now; the chain is usable as-is for apply and daily use.

---

## Final cleanup and authoring

- **Final cleanup audit:** `docs/operations/EF_MIGRATION_FINAL_CLEANUP_AUDIT.md` — classification of all 139 migrations (95 active, 44 script-only); no migrations deleted or archived.
- **Future authoring:** `docs/operations/EF_FUTURE_AUTHORING_RULES.md` — how to add migrations, apply path, and what not to touch.

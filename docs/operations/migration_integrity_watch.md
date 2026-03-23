# Migration Integrity Watch

**Date:** March 2026  
**Purpose:** Watchdog for EF Core migration chain safety, schema drift risk, and operational cautions. Documentation only; no migration or snapshot changes.

**Related:** [EF_MIGRATION_OPERATIONAL_CLOSURE_DECISION.md](EF_MIGRATION_OPERATIONAL_CLOSURE_DECISION.md) | [MIGRATION_RUNBOOK.md](../../backend/scripts/MIGRATION_RUNBOOK.md) | [architecture_watchdog_summary.md](../architecture/architecture_watchdog_summary.md)

---

## 1. Executive summary

The EF migration chain is **operationally usable** under documented conditions (see EF_MIGRATION_OPERATIONAL_CLOSURE_DECISION). This watch consolidates **migration chain safety**, **schema drift risk**, and **operational cautions** for future DB changes. **Do not** modify historical migrations, regenerate Designers blindly, or change ApplicationDbContextModelSnapshot unless the task is snapshot reconciliation. Use `backend/scripts/validate-migration-hygiene.ps1` and the official create path `backend/scripts/create-migration.ps1`.

---

## 2. Migration chain safety observations

| Observation | Status | Notes |
|-------------|--------|------|
| **Total migrations** | ~142 main .cs files; 95 EF-discoverable (with Designer); 47 intentional script-only (per governance rule) | Counts must stay aligned with validator and manifest. |
| **Idempotent repairs** | 20260309053019 (ParsedMaterialAliases conditional), 20260309065620 (EventStore Phase 7 + EventStoreAttemptHistory) | Drifted DBs can apply without failing on missing/extra objects when using idempotent script. |
| **Apply path** | `dotnet ef database update` or migration bundle or idempotent SQL script | Prefer bundle or idempotent script for staging/prod; backup first. |
| **No-Designer migrations** | In manifest; apply via idempotent scripts when app requires schema | Do not rely on `database update` to create those objects. |
| **Design-time factory** | ApplicationDbContextFactory; PendingModelChangesWarning may appear | Runtime Program.cs suppresses; use idempotent script approach when tools fail. |

---

## 3. Schema drift risk

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| **Snapshot out of sync with model** | Medium over time | Generate a single sync migration (e.g. SyncSnapshot) with idempotent Up(); do not casually change snapshot. |
| **Unusually large new migration** | Low (governance) | Treat as snapshot drift; stop, document, use dedicated sync or re-baseline. |
| **Manual DB changes outside migrations** | Medium in some environments | Document in runbooks; prefer migration scripts for repeatability. |
| **Script-only migration not in manifest** | Low if process followed | Validator and PR checklist; classify before committing. |

---

## 4. Baseline and snapshot risks

- **ApplicationDbContextModelSnapshot.cs:** Single source of truth for EF model at latest migration. **Do not** edit unless task is explicit snapshot reconciliation. Any edit can cause next migration to be a large “sync” migration.
- **Re-baseline:** Only if chain is abandoned for new environments; would require new “initial” migration and strategy for existing DBs. Not recommended until necessary.

---

## 5. Operational cautions for future DB changes

1. **New migration:** Use `create-migration.ps1 -MigrationName "DescriptiveName"`. Ensure both main .cs and .Designer.cs exist unless intentionally script-only.
2. **Validator:** Always run `validate-migration-hygiene.ps1`; if it fails, fix before merging.
3. **Staging/prod:** Backup before apply; prefer migration bundle or idempotent script; avoid running EF tools against live DB from dev box.
4. **No process holding DLLs:** When running EF tools locally, stop `dotnet run` / debugger or use `--no-build` after build.
5. **Counts:** When adding script-only or normal migrations, update validator baseline, migration hygiene doc, no-Designer manifest, and runbook per governance.

---

## 6. Related artifacts

- [EF_MIGRATION_OPERATIONAL_CLOSURE_DECISION](EF_MIGRATION_OPERATIONAL_CLOSURE_DECISION.md)
- [EF_NO_DESIGNER_MIGRATIONS_MANIFEST](EF_NO_DESIGNER_MIGRATIONS_MANIFEST.md)
- [EF_FUTURE_AUTHORING_RULES](EF_FUTURE_AUTHORING_RULES.md)
- [MIGRATION_RUNBOOK](../../backend/scripts/MIGRATION_RUNBOOK.md)
- [.cursor/rules/ef-migration-governance.mdc](../../.cursor/rules/ef-migration-governance.mdc)

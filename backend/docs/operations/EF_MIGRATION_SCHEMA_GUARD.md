# EF Migration Schema Guard — Platform Safety

**Purpose:** Prevent silent drift between `__EFMigrationsHistory` and live database schema so that "migration history says up to date" never again masks missing required tables or columns.  
**Scope:** Development, staging, and production. Documentation and process only; no automatic migration at application startup.

---

## 1. Problem summary

We had a situation where:

- `__EFMigrationsHistory` showed migrations applied (including `20260310031127_AddExternalIntegrationBus`).
- The application expected four tables: ConnectorDefinitions, ConnectorEndpoints, ExternalIdempotencyRecords, OutboundIntegrationAttempts.
- Those tables did **not** exist in the database.

Result: runtime "relation does not exist" errors despite EF reporting no pending migrations. See `SCHEMA_VERIFICATION_AUDIT_REPORT.md` and `SCHEMA_DRIFT_REMEDIATION_EXECUTION_PLAN.md` for the audit and remediation.

---

## 2. Root cause pattern (why drift was possible)

**Process/architecture, not tool failure:**

1. **Multiple application paths**  
   Schema can be applied via: (a) `dotnet ef database update`, (b) idempotent SQL script (full generated script), (c) migration bundle, (d) **repair/partial scripts** that create only some objects and then **insert the migration ID** into `__EFMigrationsHistory`.

2. **Recording without full apply**  
   A repair script (`apply-AddExternalIntegrationBus-repair.sql`) was used when the full idempotent script failed or was not run. That script created only a subset of the migration’s objects (e.g. JobExecutions, OutboundIntegrationDeliveries) and then inserted `20260310031127_AddExternalIntegrationBus` into `__EFMigrationsHistory`. The rest of the migration (ConnectorDefinitions, ConnectorEndpoints, ExternalIdempotencyRecords, OutboundIntegrationAttempts) was never applied.

3. **No verification that schema matches history**  
   There was no required post-apply step to confirm that every table/column the EF model expects actually exists when a migration is recorded as applied. So history and schema could diverge without detection.

4. **Application does not run migrations at startup**  
   The API does not call `Database.Migrate()`. That is intentional (per AGENTS.md, `database update` can fail with PendingModelChangesWarning; idempotent script is preferred). So the only way schema gets applied is by an operator or pipeline running the chosen path. If that path is a partial script + history insert, drift is possible.

**Summary:** Drift occurred because a **partial** schema change was applied and the migration was **recorded as applied** in history, without applying the full migration and without verifying that the live schema matched the model.

---

## 3. Chosen safeguard

**One canonical migration application path + no recording without full apply + required post-apply verification.**

1. **Canonical path**  
   For any environment, schema changes must be applied through **one** of:
   - **Migration bundle** (recommended for CI/CD / server): `CephasOps.MigrationsBundle.exe` with connection string.
   - **Full idempotent script**: generated with `dotnet ef migrations script --idempotent ...`, reviewed, then applied in full via `psql` (or equivalent).

   Use repair or ad hoc scripts **only** when they are additive (e.g. create missing objects) and **do not** insert a migration ID into `__EFMigrationsHistory` for a migration they did not fully apply. If a repair script is used to unblock, it must be followed by either applying the full migration (so history can be updated by the normal path) or by a documented remediation that creates all missing objects and does not add new history rows for migrations already listed.

2. **Never record a migration unless its full schema is applied**  
   **Forbidden:** Inserting a row into `__EFMigrationsHistory` for a migration when only part of that migration’s schema (e.g. only some tables) was applied.  
   **Allowed:** Insert only when the **entire** migration has been applied (by bundle, full idempotent script, or a documented remediation script that creates every object that migration introduces).  
   New repair scripts must not write to `__EFMigrationsHistory` for a migration they do not fully apply; existing scripts that do so (e.g. the AddExternalIntegrationBus repair) are legacy and must not be used as a template for new ones.

3. **Required post-apply verification**  
   After every migration deployment (bundle or idempotent script), run **schema verification** so that missing model-required objects are detected immediately:
   - Run `backend/scripts/check-migration-state.sql` (includes integration-bus and other model-required tables).
   - Confirm all expected tables exist; if any are missing, treat as drift and remediate before considering the deployment complete (see §7).

4. **Startup schema guard**  
   The application runs a **startup schema guard** that checks that four critical tables (ConnectorDefinitions, ConnectorEndpoints, ExternalIdempotencyRecords, OutboundIntegrationAttempts) exist. If any are missing, the application **fails to start** with a clear error and directs operators to this document and to `STARTUP_SCHEMA_GUARD.md`. This prevents the previous situation where the app started but later failed at runtime with "relation does not exist". See `backend/docs/operations/STARTUP_SCHEMA_GUARD.md`.

5. **No automatic migration at startup**  
   The application does not and should not call `Database.Migrate()` at startup. Migration application remains an explicit operator/pipeline step. This document does not change that.

---

## 4. Required operator workflow

For **every** schema deployment (Development, staging, production):

1. **Backup** the database (or confirm PITR).
2. **Apply** migrations via the **canonical path only**:
   - **Preferred:** Run the migration bundle with the appropriate connection string, **or**
   - **Alternative:** Generate the full idempotent script, review it, then run it in full against the target database.
3. **Do not** run a repair or partial script that inserts a migration ID into `__EFMigrationsHistory` unless that script applies the **full** schema for that migration (or follow the exception in §3.1).
4. **Verify:** Run `backend/scripts/check-migration-state.sql` and confirm:
   - No model-required tables are missing (see script output).
   - Any migration ID present in `__EFMigrationsHistory` corresponds to schema that is fully present (no partial applies).
5. **If verification fails:** Treat as schema drift; do not add further migration history until drift is resolved (see §7).

For **creating** migrations: continue to use `backend/scripts/create-migration.ps1` (or `dotnet ef migrations add`) and follow `MIGRATION_RUNBOOK.md` and the EF migration governance rules.

---

## 5. What is allowed

- Applying migrations via the **migration bundle** or the **full idempotent script**.
- Using **additive** repair scripts that only create missing objects and **do not** insert into `__EFMigrationsHistory` (e.g. `apply-schema-drift-remediation.sql` for the four integration-bus tables).
- Running **script-only** migrations (no Designer) via their dedicated idempotent scripts, and then inserting the migration ID **only after** the full script has been applied and verified.
- Post-apply verification with `check-migration-state.sql` and any other documented schema checks.

---

## 6. What is forbidden

- **Inserting a migration ID into `__EFMigrationsHistory`** when only part of that migration’s schema was applied (e.g. a "repair" that creates some tables and then records the migration).
- Applying schema via **ad hoc** SQL or one-off scripts that are not the bundle, the full idempotent script, or a documented remediation/repair script that complies with §3 and §5.
- **Skipping** post-apply schema verification after a migration deployment.
- Using **partial** repair scripts that write to `__EFMigrationsHistory` as a standard deployment path; such scripts are legacy and must not be replicated for new migrations.

---

## 7. If drift is detected again

1. **Do not** insert or remove rows in `__EFMigrationsHistory` to "fix" history unless you have applied or reverted the **full** migration.
2. **Assess:** Run a schema verification audit (e.g. compare live schema to the EF model or use `check-migration-state.sql` and the pattern in `SCHEMA_VERIFICATION_AUDIT_REPORT.md`).
3. **Remediate:** Apply only the **missing** objects (additive), using an idempotent script that creates tables/indexes that the model expects but the database lacks. Use documentation such as `SCHEMA_DRIFT_REMEDIATION_EXECUTION_PLAN.md` and the operator summary.
4. **Verify again:** Re-run `check-migration-state.sql` (and any other checks) until all model-required objects are present.
5. **Document:** Update operations docs and, if needed, add the remediation script to the repo and reference it in the runbook so the same drift cannot recur unnoticed.

---

## 8. References

- `backend/docs/operations/STARTUP_SCHEMA_GUARD.md` — Startup fail-fast check for critical tables; operator guidance when the guard fails.
- `backend/docs/operations/MIGRATION_AUDIT.md` — Operational audit for migration rollouts (who, when, verification/smoke result). Optional recording after deployment.
- `backend/docs/operations/SCHEMA_VERIFICATION_AUDIT_REPORT.md` — Audit that identified the four missing tables.
- `backend/docs/operations/SCHEMA_DRIFT_REMEDIATION_EXECUTION_PLAN.md` — Remediation execution and validation.
- `backend/docs/operations/SCHEMA_DRIFT_REMEDIATION_OPERATOR_SUMMARY.md` — Operator summary and completed outcome.
- `backend/scripts/MIGRATION_RUNBOOK.md` — Recommended deployment workflow and when to use bundle vs idempotent script.
- `backend/scripts/check-migration-state.sql` — Post-apply schema verification (includes model-required tables).
- AGENTS.md — Database setup and idempotent script approach.

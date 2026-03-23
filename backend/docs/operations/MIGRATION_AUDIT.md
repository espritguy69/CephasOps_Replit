# Migration Deployment Audit (MigrationAudit)

## Purpose

**MigrationAudit** is a lightweight operational audit mechanism that records **when** a migration rollout was executed in an environment, **by whom**, and **with what result** (verification and smoke test). It does **not** replace `__EFMigrationsHistory`. EF continues to use `__EFMigrationsHistory` to decide which migrations to apply. MigrationAudit is for **human and process audit**: who deployed what, when, and whether checks passed.

- **Operational audit only** — not schema control. The application does not read or write this table at runtime.
- **Operator-recorded** — records are inserted by operators (or a documented script) after a successful rollout, not automatically at app startup. This avoids any risk of audit logging blocking application startup.

---

## What it records

| Column | Description |
|--------|-------------|
| **Environment** | Development, Staging, or Production. |
| **MigrationId** | EF migration ID (e.g. `20260313025530_EnsureEmailMessageBodyAndErrorColumnsAreText`). |
| **AppliedAtUtc** | When the migration was applied (UTC). |
| **AppliedBy** | Who or what applied it (operator name, pipeline ID, service account). |
| **MethodUsed** | How it was applied: EF database update, Idempotent script, Migration bundle. |
| **VerificationStatus** | Pass, Fail, or Skipped (post-apply verification). |
| **SmokeTestStatus** | Pass, Fail, Skipped, or N/A. |
| **Notes** | Optional (e.g. ticket ref, issues). |

---

## When it must be written

- **After** the migration has been applied via the canonical path (bundle or idempotent script).
- **After** post-apply verification has been run and has **passed** (e.g. `check-migration-state.sql` or migration-specific verification script).
- **After** any required smoke test for that migration has **passed** (if applicable).

Record a row **per environment, per migration**. For a staged rollout (e.g. Development → Staging → Production), insert one row when Development is done, one when Staging is done, one when Production is done.

---

## What is mandatory before recording success

Before inserting a row with `VerificationStatus = 'Pass'` and `SmokeTestStatus = 'Pass'`:

1. **Migration applied** — The migration ID must appear in `__EFMigrationsHistory` for that database.
2. **Verification passed** — Run the required verification (e.g. `check-migration-state.sql`, or a migration-specific script like `verify-20260313025530-email-messages-text.sql`) and confirm all checks pass.
3. **Smoke test passed** — If the migration’s runbook or operator summary requires a smoke test (e.g. long-email ingestion), it must have been run and passed.

Do **not** record `VerificationStatus = 'Pass'` or `SmokeTestStatus = 'Pass'` if verification or smoke test failed or was skipped without justification. Use `Fail` or `Skipped` and put details in **Notes**.

---

## How operators use it

1. **Apply** the migration (bundle or idempotent script) in the target environment.
2. **Run** the required post-apply verification script; fix any drift before proceeding.
3. **Run** any required smoke test for that migration.
4. **Insert** one row into `MigrationAudit` using the documented template or script:
   - Use **`backend/scripts/record-migration-audit.sql`** — edit the placeholder values (Environment, MigrationId, AppliedBy, MethodUsed, VerificationStatus, SmokeTestStatus, Notes), then run the script against the target database.

The application **never** writes to `MigrationAudit`. No automatic recording at startup; no dependency on audit success for app health.

---

## Failed or partial rollouts

- **Verification failed:** Do not record `VerificationStatus = 'Pass'`. Either fix the schema and re-run verification, or record `VerificationStatus = 'Fail'` and describe in Notes. Do not promote to the next environment until verification passes.
- **Smoke test failed:** Record `SmokeTestStatus = 'Fail'` and Notes. Do not promote until the issue is resolved and smoke test passes.
- **Partial apply / rollback:** If a migration was partially applied or rolled back, you may record a row with `VerificationStatus = 'Fail'` or `Skipped` and explain in Notes. This keeps an audit trail without falsely indicating success.

---

## Relationship to __EFMigrationsHistory

- **`__EFMigrationsHistory`** — Used by EF to know which migrations have been applied. Do not insert into it unless the **full** migration has been applied (see `EF_MIGRATION_SCHEMA_GUARD.md`).
- **MigrationAudit** — Used by operators to record **deployment events**: when, where, by whom, and with what verification/smoke result. It does not drive EF behavior. It is supplementary audit data only.

---

## Relationship to verification scripts and smoke tests

- **Verification scripts** (e.g. `check-migration-state.sql`, `verify-20260313025530-email-messages-text.sql`) — Run **before** you record a successful audit row. MigrationAudit does not replace running these scripts; it records that you ran them and what the result was.
- **Smoke tests** — Per-migration (e.g. long-email ingestion for the EmailMessages text columns migration). Run them as per the migration’s operator summary; then set `SmokeTestStatus` accordingly in the audit row.

---

## Table and migration

- **Table:** `MigrationAudit` (created by EF migration `20260313031056_AddMigrationAuditTable`).
- **Rollout:** This migration is **approved for the normal rollout path** (Development → Staging → Production) with the rest of pending migrations (bundle or idempotent script). No special handling required.
- **Recording path:** Operator (or pipeline) runs `record-migration-audit.sql` (or equivalent INSERT) after successful apply + verification + smoke test. No application code writes to this table.

See **MIGRATION_RUNBOOK.md** and per-migration operator summaries (e.g. **MIGRATION_20260313025530_OPERATOR_EXECUTION_SUMMARY.md**) for the full deployment and verification workflow.

# Schema drift remediation — operator summary

**Purpose:** Run the documented schema drift fix (4 missing tables only) safely. No new migration, no code changes, no extra schema, no FKs from InboundWebhookReceipts/OutboundIntegrationDeliveries to ConnectorEndpoints.

---

## Readiness status

- **Ready:** Audit report, execution plan, idempotent remediation SQL, and validation queries are in place. A single runnable script and optional helper validation scripts are provided.
- **Development:** Remediation **executed successfully**; Development DB is aligned for the 4 remediated tables; migration history unchanged. See Completed outcome below.
- **Other environments:** Manual steps required (backup, pre-validation, run script, post-validation, optional data-integrity checks).

---

## Scope confirmation

Remediation adds **only** these 4 tables and their indexes (no other schema objects):

| # | Table | Dependency |
|---|--------|------------|
| 1 | ConnectorDefinitions | None |
| 2 | ConnectorEndpoints | FK to ConnectorDefinitions |
| 3 | ExternalIdempotencyRecords | None |
| 4 | OutboundIntegrationAttempts | FK to OutboundIntegrationDeliveries |

**Not in scope:** No FKs from `InboundWebhookReceipts` or `OutboundIntegrationDeliveries` to `ConnectorEndpoints`. Add those only after data validation (orphan checks) and resolution.

---

## Safety checks

- **Idempotent:** All statements use `CREATE TABLE IF NOT EXISTS` / `CREATE INDEX IF NOT EXISTS`; safe to re-run.
- **No migration history change:** Script does not insert or update `__EFMigrationsHistory`.
- **Single source of SQL:** Use `backend/scripts/apply-schema-drift-remediation.sql` only; it matches the audit report §4.
- **Validation coverage:** Post-fix checks cover table existence, expected index counts, expected FKs only (2 FKs), and unchanged migration history. Data-integrity section covers orphan `ConnectorEndpointId` in InboundWebhookReceipts and OutboundIntegrationDeliveries.

---

## Gaps found (and addressed)

- **Runnable script:** Execution plan referred to copying SQL from the doc. **Addressed:** Added `backend/scripts/apply-schema-drift-remediation.sql` as the canonical runnable script.
- **FK validation:** Post-fix checklist mentioned “expected FKs only” but had no SQL to verify. **Addressed:** Added “After fix — confirm expected FKs only” query (expect exactly 2 FK rows).
- **Migration history:** Checklist said “unchanged” but had no concrete check. **Addressed:** Added “Migration history unchanged” (record count before, re-run after; must match).
- **Exact order:** Execution and validation order was implied but not listed in one place. **Addressed:** Added “Operator runbook (exact order)” in the execution plan with step-by-step execution and validation order.

No unsafe or inconsistent content was found in the audit report or the remediation SQL (scope, dependency order, and “no FKs to ConnectorEndpoints” rule are correct).

---

## Documentation updates made

1. **New file:** `backend/scripts/apply-schema-drift-remediation.sql` — idempotent SQL for the 4 tables and indexes only; content matches audit report §4.
2. **Updated:** `backend/docs/operations/SCHEMA_DRIFT_REMEDIATION_EXECUTION_PLAN.md`:
   - Pre-requisites and execution checklist now reference the script file and a migration-history baseline step.
   - Added “After fix — confirm expected FKs only” (SQL + expected 2 rows).
   - Added “Migration history unchanged” (SQL before/after, same count).
   - Post-fix validation checklist updated to reference the new validation queries.
   - Summary table updated to cite the script as canonical source.
   - Added “Operator runbook (exact order)” with execution order and validation order.

No changes were made to `SCHEMA_VERIFICATION_AUDIT_REPORT.md` or to application code, EF model, or migrations.

---

## Final operator instructions

**What is already ready**

- Audit report: `backend/docs/operations/SCHEMA_VERIFICATION_AUDIT_REPORT.md`
- Execution plan and validation: `backend/docs/operations/SCHEMA_DRIFT_REMEDIATION_EXECUTION_PLAN.md`
- Runnable remediation script: `backend/scripts/apply-schema-drift-remediation.sql` (4 tables + indexes only; no FKs from InboundWebhookReceipts/OutboundIntegrationDeliveries to ConnectorEndpoints)
- Optional helper validation scripts: `backend/scripts/pre-check-migration-count.sql`, `backend/scripts/post-check-index-counts.sql`, `backend/scripts/post-check-fks.sql`

**What still needs manual execution (other environments only)**

For environments other than Development: backup, pre-validation, run the remediation script, post-validation, and (optionally) data-integrity checks. Development has already been remediated.

**Exact execution order**

1. Backup  
2. Pre-check: “Before fix — confirm the 4 tables are missing” (expect 0 rows)  
3. Baseline: “Migration history unchanged” (record count)  
4. Optional: Data integrity checks (InboundWebhookReceipts, OutboundIntegrationDeliveries)  
5. Apply: `psql -h <host> -p <port> -U <user> -d cephasops -f backend/scripts/apply-schema-drift-remediation.sql` (or run the file in your SQL client)  
6. Post-check: “After fix — confirm the 4 tables exist” (expect 4 rows)  
7. Post-check: “After fix — confirm expected indexes only” or combined table+index count (2, 3, 3, 2)  
8. Post-check: “After fix — confirm expected FKs only” (expect 2 FK rows)  
9. Post-check: “Migration history unchanged” (same count as baseline)  
10. Optional: Re-run data integrity checks  

**Exact validation order (after apply)**

1. Tables exist (4 rows)  
2. Index counts (2, 3, 3, 2)  
3. FKs (exactly 2; none from InboundWebhookReceipts/OutboundIntegrationDeliveries to ConnectorEndpoints)  
4. Migration history count unchanged  

All validation SQL is in `SCHEMA_DRIFT_REMEDIATION_EXECUTION_PLAN.md`. Rollback: drop the 4 tables in reverse order (OutboundIntegrationAttempts → ConnectorEndpoints → ExternalIdempotencyRecords → ConnectorDefinitions); no migration history change.

---

## Completed outcome (Development)

- **Remediation executed successfully** on the Development database.
- **4 tables restored and verified:** ConnectorDefinitions, ConnectorEndpoints, ExternalIdempotencyRecords, OutboundIntegrationAttempts; expected indexes and FKs confirmed; `__EFMigrationsHistory` unchanged (e.g. 117).
- **No application code or EF migration changes.**  
The Development database is now reconciled with the current EF model for the remediated integration-bus objects.

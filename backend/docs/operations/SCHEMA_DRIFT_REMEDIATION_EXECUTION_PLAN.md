# Schema Drift Remediation — Execution Plan & Validation

**Purpose:** Safest execution plan to add the 4 missing tables (and only those) to the Development database, with validation and data-integrity checks.  
**Source:** Findings and idempotent SQL from [SCHEMA_VERIFICATION_AUDIT_REPORT.md](./SCHEMA_VERIFICATION_AUDIT_REPORT.md).  
**Constraints:** No new migration, no application code changes, no extra schema objects.  
**Status:** Remediation **executed successfully on Development** — see Completed outcome below.

---

## Summary

- **Drift:** Four tables exist in the EF model but are missing in the Development DB: `ConnectorDefinitions`, `ConnectorEndpoints`, `ExternalIdempotencyRecords`, `OutboundIntegrationAttempts`.
- **Cause:** Migration `20260310031127_AddExternalIntegrationBus` was applied via a repair script that created only a subset of objects and recorded the migration; the four tables were never created.
- **Remediation:** Run the idempotent SQL from the audit report (§4) in order. All statements use `IF NOT EXISTS` / `IF NOT EXISTS` so re-runs are safe.
- **FKs to ConnectorEndpoints:** Do **not** add FKs from `InboundWebhookReceipts` or `OutboundIntegrationDeliveries` to `ConnectorEndpoints` until data-integrity checks pass and orphan `ConnectorEndpointId` values are resolved (backfill or null).

---

## Execution Plan

### Pre-requisites

- [ ] Database: Development `cephasops` (localhost or target instance).
- [ ] Backup or snapshot taken (see Rollback considerations).
- [ ] No uncommitted migration or schema changes in progress.
- [ ] **Canonical SQL:** Use the runnable script `backend/scripts/apply-schema-drift-remediation.sql` (same content as audit report §4). Do not copy-paste from the doc.

### Execution checklist

1. [ ] **Backup** — Ensure a backup or point-in-time recovery option is available.
2. [ ] **Pre-check** — Run the “Validation SQL (before fix)” section below and save results; confirm the 4 tables are missing.
3. [ ] **Migration history baseline** — Run “Migration history unchanged” query below and record the count; you will re-run after remediation to confirm it is unchanged.
4. [ ] **Orphan check (optional but recommended)** — Run the “Data integrity checks” queries for `ConnectorEndpointId`; record counts of existing rows and any distinct `ConnectorEndpointId` values in `InboundWebhookReceipts` and `OutboundIntegrationDeliveries`.
5. [ ] **Apply SQL** — Execute `backend/scripts/apply-schema-drift-remediation.sql` (e.g. `psql -f apply-schema-drift-remediation.sql` or your client’s “run file”). Order inside the script is fixed: ConnectorDefinitions → ConnectorEndpoints → ExternalIdempotencyRecords → OutboundIntegrationAttempts.
6. [ ] **Post-fix validation** — Run the “Post-fix validation checklist” and all “Validation SQL (after fix)” queries below; confirm tables, indexes, and FKs match expectations and migration history count is unchanged.
7. [ ] **Data integrity** — Re-run “Data integrity checks”; interpret results and only then consider adding FKs to `ConnectorEndpoints` (see recommendation at end).

### Post-fix validation checklist

- [ ] All four tables exist: `ConnectorDefinitions`, `ConnectorEndpoints`, `ExternalIdempotencyRecords`, `OutboundIntegrationAttempts` (use “After fix — confirm the 4 tables exist”).
- [ ] Each table has exactly the indexes listed in “Validation SQL (after fix)” / combined table+index count (no extra indexes).
- [ ] Expected FKs only: run “After fix — confirm expected FKs only”; exactly 2 FKs (ConnectorEndpoints → ConnectorDefinitions, OutboundIntegrationAttempts → OutboundIntegrationDeliveries); no FKs from InboundWebhookReceipts or OutboundIntegrationDeliveries to ConnectorEndpoints.
- [ ] Migration history unchanged: “Migration history unchanged” query returns the same count as recorded before remediation.
- [ ] Application starts and integration/connector code paths that use the 4 tables run without “relation does not exist” errors.

---

## Validation SQL

### Before fix — confirm the 4 tables are missing

```sql
-- Expect 0 rows for each.
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_name IN (
    'ConnectorDefinitions',
    'ConnectorEndpoints',
    'ExternalIdempotencyRecords',
    'OutboundIntegrationAttempts'
  )
ORDER BY table_name;
```

### After fix — confirm the 4 tables exist

```sql
-- Expect exactly 4 rows.
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_name IN (
    'ConnectorDefinitions',
    'ConnectorEndpoints',
    'ExternalIdempotencyRecords',
    'OutboundIntegrationAttempts'
  )
ORDER BY table_name;
```

### After fix — confirm expected indexes only

```sql
-- ConnectorDefinitions: expect 2 (PK + IX_ConnectorDefinitions_ConnectorKey).
SELECT indexname
FROM pg_indexes
WHERE schemaname = 'public' AND tablename = 'ConnectorDefinitions'
ORDER BY indexname;

-- ConnectorEndpoints: expect 3 (PK + IX_ConnectorEndpoints_CompanyId + IX_ConnectorEndpoints_ConnectorDefinitionId_CompanyId).
SELECT indexname
FROM pg_indexes
WHERE schemaname = 'public' AND tablename = 'ConnectorEndpoints'
ORDER BY indexname;

-- ExternalIdempotencyRecords: expect 3 (PK + IX_ExternalIdempotencyRecords_IdempotencyKey + IX_ExternalIdempotencyRecords_ConnectorKey_CompletedAtUtc).
SELECT indexname
FROM pg_indexes
WHERE schemaname = 'public' AND tablename = 'ExternalIdempotencyRecords'
ORDER BY indexname;

-- OutboundIntegrationAttempts: expect 2 (PK + IX_OutboundIntegrationAttempts_OutboundDeliveryId).
SELECT indexname
FROM pg_indexes
WHERE schemaname = 'public' AND tablename = 'OutboundIntegrationAttempts'
ORDER BY indexname;
```

### Single combined post-fix check (tables + index counts)

```sql
SELECT
  t.table_name,
  (SELECT count(*) FROM pg_indexes i WHERE i.schemaname = 'public' AND i.tablename = t.table_name) AS index_count
FROM information_schema.tables t
WHERE t.table_schema = 'public'
  AND t.table_name IN (
    'ConnectorDefinitions',
    'ConnectorEndpoints',
    'ExternalIdempotencyRecords',
    'OutboundIntegrationAttempts'
  )
ORDER BY t.table_name;
```

Expected result after fix:

| table_name                 | index_count |
|----------------------------|-------------|
| ConnectorDefinitions       | 2           |
| ConnectorEndpoints         | 3           |
| ExternalIdempotencyRecords | 3           |
| OutboundIntegrationAttempts | 2           |

### After fix — confirm expected FKs only

Remediation must add exactly **2** foreign keys: (1) ConnectorEndpoints → ConnectorDefinitions, (2) OutboundIntegrationAttempts → OutboundIntegrationDeliveries. There must be **no** FKs from InboundWebhookReceipts or OutboundIntegrationDeliveries to ConnectorEndpoints.

```sql
-- List FK constraints on the 4 new tables. Expect exactly 2 rows.
SELECT
  c.conname AS fk_name,
  (SELECT relname FROM pg_class WHERE oid = c.conrelid) AS from_table,
  (SELECT relname FROM pg_class WHERE oid = c.confrelid) AS to_table
FROM pg_constraint c
JOIN pg_namespace n ON n.oid = c.connamespace
WHERE n.nspname = 'public'
  AND c.contype = 'f'
  AND (
    c.conrelid IN (
      'public."ConnectorDefinitions"'::regclass,
      'public."ConnectorEndpoints"'::regclass,
      'public."ExternalIdempotencyRecords"'::regclass,
      'public."OutboundIntegrationAttempts"'::regclass
    )
    OR c.confrelid IN (
      'public."ConnectorDefinitions"'::regclass,
      'public."ConnectorEndpoints"'::regclass,
      'public."ExternalIdempotencyRecords"'::regclass,
      'public."OutboundIntegrationAttempts"'::regclass
    )
  )
ORDER BY from_table, to_table;
```

Expected rows (order may vary):

| fk_name | from_table | to_table |
|---------|------------|----------|
| FK_ConnectorEndpoints_ConnectorDefinitions_ConnectorDefinitionId | ConnectorEndpoints | ConnectorDefinitions |
| FK_OutboundIntegrationAttempts_OutboundIntegrationDeliveries_OutboundDeliveryId | OutboundIntegrationAttempts | OutboundIntegrationDeliveries |

### Migration history unchanged

Remediation does **not** insert or update `__EFMigrationsHistory`. Verify before and after:

```sql
-- Run BEFORE remediation and record the result (e.g. 99).
SELECT count(*) AS migration_count FROM "__EFMigrationsHistory";

-- Run AFTER remediation. Result must be identical to the value recorded before.
SELECT count(*) AS migration_count FROM "__EFMigrationsHistory";
```

**Helper scripts (optional):** For quick runs without copy-paste, use `backend/scripts/pre-check-migration-count.sql`, `backend/scripts/post-check-index-counts.sql`, and `backend/scripts/post-check-fks.sql`; they contain the migration-count, combined table+index, and FK queries above.

---

## Data integrity checks

Run these **before** adding any FK from `InboundWebhookReceipts` or `OutboundIntegrationDeliveries` to `ConnectorEndpoints`. They help decide whether to backfill endpoints, null invalid IDs, or leave FKs off.

### InboundWebhookReceipts — orphan ConnectorEndpointId

```sql
-- Total rows and distinct ConnectorEndpointId (before ConnectorEndpoints exists, all are “orphans”).
SELECT
  count(*) AS total_rows,
  count(DISTINCT "ConnectorEndpointId") AS distinct_connector_endpoint_ids
FROM "InboundWebhookReceipts";

-- Distinct ConnectorEndpointId values (for backfill or review).
SELECT DISTINCT "ConnectorEndpointId"
FROM "InboundWebhookReceipts"
ORDER BY "ConnectorEndpointId";

-- After ConnectorEndpoints exists: rows whose ConnectorEndpointId is NOT in ConnectorEndpoints (orphans).
SELECT i."Id", i."ConnectorEndpointId", i."ConnectorKey", i."ReceivedAtUtc"
FROM "InboundWebhookReceipts" i
LEFT JOIN "ConnectorEndpoints" e ON e."Id" = i."ConnectorEndpointId"
WHERE e."Id" IS NULL
ORDER BY i."ReceivedAtUtc" DESC;
```

### OutboundIntegrationDeliveries — orphan ConnectorEndpointId

```sql
-- Total rows and distinct ConnectorEndpointId.
SELECT
  count(*) AS total_rows,
  count(DISTINCT "ConnectorEndpointId") AS distinct_connector_endpoint_ids
FROM "OutboundIntegrationDeliveries";

-- Distinct ConnectorEndpointId values.
SELECT DISTINCT "ConnectorEndpointId"
FROM "OutboundIntegrationDeliveries"
ORDER BY "ConnectorEndpointId";

-- After ConnectorEndpoints exists: rows whose ConnectorEndpointId is NOT in ConnectorEndpoints (orphans).
SELECT o."Id", o."ConnectorEndpointId", o."EventType", o."Status", o."CreatedAtUtc"
FROM "OutboundIntegrationDeliveries" o
LEFT JOIN "ConnectorEndpoints" e ON e."Id" = o."ConnectorEndpointId"
WHERE e."Id" IS NULL
ORDER BY o."CreatedAtUtc" DESC;
```

### Interpretation and next step

- If **no rows** (or only a few known test rows) reference `ConnectorEndpointId`, you can add FKs after inserting the corresponding `ConnectorEndpoints` (and optionally `ConnectorDefinitions`) rows, or after nulling/updating invalid IDs if the model allows.
- If **many orphan rows** exist, either: (1) backfill `ConnectorDefinitions` and `ConnectorEndpoints` so every referenced `ConnectorEndpointId` exists, then add the FKs; or (2) leave FKs off until application logic or a separate cleanup aligns data. **Recommendation:** Add FKs to `ConnectorEndpoints` only after data validation passes (no orphans, or orphans resolved).

---

## Rollback considerations

- **No migration history change:** Remediation does not insert or update `__EFMigrationsHistory`, so no migration rollback is involved.
- **Rollback = drop the 4 objects:** To undo, drop in **reverse** order of creation (to satisfy FK dependencies):
  1. `OutboundIntegrationAttempts` (FK to OutboundIntegrationDeliveries)
  2. `ConnectorEndpoints` (FK to ConnectorDefinitions)
  3. `ExternalIdempotencyRecords`
  4. `ConnectorDefinitions`
- **Data loss:** The four tables are new and empty after remediation; dropping them does not remove data from existing tables. Existing data in `InboundWebhookReceipts` and `OutboundIntegrationDeliveries` is unchanged by the remediation script.
- **Backup:** Take a backup or ensure PITR before running the remediation so you can restore if something else is run by mistake.

---

## Summary table

| Item | Action |
|------|--------|
| **Scope** | Add only: ConnectorDefinitions, ConnectorEndpoints, ExternalIdempotencyRecords, OutboundIntegrationAttempts + their indexes. |
| **Source of SQL** | `backend/scripts/apply-schema-drift-remediation.sql` (canonical); same as SCHEMA_VERIFICATION_AUDIT_REPORT.md §4. |
| **Execution** | Run remediation script; use pre- and post-validation + data-integrity checks. |
| **FKs to ConnectorEndpoints** | Add only after data validation; resolve orphans first. |
| **Rollback** | Drop the 4 tables in reverse dependency order; no migration history change. |

---

## Operator runbook (exact order)

**Execution order**

1. Backup DB (or confirm PITR).
2. Pre-check: run “Before fix — confirm the 4 tables are missing”; expect 0 rows.
3. Baseline: run “Migration history unchanged”; record the count.
4. (Optional) Run “Data integrity checks” for InboundWebhookReceipts and OutboundIntegrationDeliveries; record counts and distinct `ConnectorEndpointId` values.
5. Apply: run `backend/scripts/apply-schema-drift-remediation.sql` (e.g. `psql -h localhost -p 5432 -U postgres -d cephasops -f backend/scripts/apply-schema-drift-remediation.sql`).
6. Post-check: run “After fix — confirm the 4 tables exist” (expect 4 rows).
7. Post-check: run “After fix — confirm expected indexes only” (or combined table+index count); expect index counts 2, 3, 3, 2.
8. Post-check: run “After fix — confirm expected FKs only”; expect exactly 2 FK rows.
9. Post-check: run “Migration history unchanged” again; count must equal baseline.
10. (Optional) Re-run “Data integrity checks” (orphan ConnectorEndpointId); use results to decide whether to add FKs to ConnectorEndpoints later.

**Validation order (after apply)**

1. Tables exist (4 rows).  
2. Index counts per table (2, 3, 3, 2).  
3. FK check (exactly 2 FKs, none from InboundWebhookReceipts/OutboundIntegrationDeliveries to ConnectorEndpoints).  
4. Migration history count unchanged.

---

## Completed outcome (Development)

- **Remediation executed successfully** on the Development database.
- **4 tables created and verified:** ConnectorDefinitions, ConnectorEndpoints, ExternalIdempotencyRecords, OutboundIntegrationAttempts.
- **Expected indexes and FKs confirmed** (index counts 2, 3, 3, 2; exactly 2 FKs).
- **`__EFMigrationsHistory` unchanged** (e.g. 117 rows; no insert/update).
- **No application code or EF migration changes.**  
The Development database is now aligned with the current EF model for the remediated integration-bus objects. For other environments, follow the same execution and validation order using `backend/scripts/apply-schema-drift-remediation.sql`.

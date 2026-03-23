# Rollout Readiness: Migration 20260313025530_EnsureEmailMessageBodyAndErrorColumnsAreText

## 1. Scope confirmation

**Migration file:** `20260313025530_EnsureEmailMessageBodyAndErrorColumnsAreText.cs`

**Confirmed:** The migration **only** changes the following columns on table **EmailMessages**:

| Column        | Table         | Change                                      |
|---------------|---------------|---------------------------------------------|
| BodyText      | EmailMessages | `character varying(2000)` or existing `text` → `text` |
| BodyHtml      | EmailMessages | same                                        |
| BodyPreview   | EmailMessages | `character varying(2000)` → `text`           |
| ParserError   | EmailMessages | `character varying(2000)` → `text`           |

- **No other tables** are modified.
- **No other columns** on `EmailMessages` are modified.
- **No indexes** are added, dropped, or changed.
- **No FKs** or other constraints are touched.

**Supporting artifacts reviewed:**

- `backend/docs/operations/EMAIL_INGESTION_VARCHAR2000_INVESTIGATION.md` – aligns with migration scope and intent.
- `EmailMessageConfiguration.cs` – only maps the same four properties to `.HasColumnType("text")`; no new `HasMaxLength` or truncation.
- `ApplicationDbContextModelSnapshot.cs` – `EmailMessage` entity has `BodyHtml`, `BodyPreview`, `BodyText`, `ParserError` as `text` with no max length.

**Business logic and truncation:**

- **No business logic changes:** No changes to `EmailIngestionService` or other application code as part of this migration. Body preview remains intentionally capped at 500 characters in code; no new truncation was added.
- **No silent truncation:** Schema change only; application continues to store full `BodyText`/`BodyHtml` and does not truncate them.

---

## 2. Risk assessment

| Risk | Level | Mitigation |
|------|--------|------------|
| Data loss | **Low** | PostgreSQL `ALTER COLUMN ... TYPE text` from `varchar(n)` does not truncate; existing data is preserved. |
| Downtime | **Low** | Column type change in PostgreSQL is a metadata/rewrite operation; table remains available. On large tables, lock duration may increase; consider applying during a maintenance window if `EmailMessages` is very large. |
| Rollback | **Low** | Down migration only reverts `BodyPreview` and `ParserError` to `varchar(2000)`. If those columns contain values longer than 2000 after migration, Down would fail or truncate; prefer fixing forward rather than rolling back. |
| Application compatibility | **Low** | EF model and configuration already expect `text` for these columns; no application code change required for the migration. |
| Tenant safety | **None** | No tenant-scoping or guard logic changed; table remains tenant-scoped by existing design. |

**Overall:** Safe for staged rollout (Development → Staging → Production) with verification at each stage.

---

## 3. Rollout checklist (Development → Staging → Production)

Use this in order; do not promote to the next environment until the current one is verified.

### Pre-rollout (once, before Development)

- [ ] Backup target database (or confirm automated backups).
- [ ] Confirm migration is in the branch/artifact that will be deployed.
- [ ] (Optional) Run `dotnet ef migrations script` for this migration only and review generated SQL.

### Development

- [ ] Apply migration (e.g. `dotnet ef database update` or run idempotent script including this migration).
- [ ] Run **Post-apply verification** (Section 4) on Development.
- [ ] Run **Smoke test: long email ingestion** (Section 5) on Development.
- [ ] Confirm no new errors in logs related to `EmailMessages` or `EmailIngestionService`.
- [ ] Sign-off: Development rollout complete.

### Staging

- [ ] Backup Staging database.
- [ ] Apply same migration to Staging (same mechanism as Dev).
- [ ] Run **Post-apply verification** on Staging.
- [ ] Run **Smoke test: long email ingestion** on Staging (or equivalent: trigger ingestion with long body).
- [ ] Confirm no regressions in existing email/parser flows.
- [ ] Sign-off: Staging rollout complete.

### Production

- [ ] Schedule maintenance window if `EmailMessages` is large (optional but recommended).
- [ ] Backup Production database.
- [ ] Apply migration to Production.
- [ ] Run **Post-apply verification** on Production.
- [ ] Run **Smoke test: long email ingestion** (or monitor first few long-body ingestions).
- [ ] Monitor logs and metrics for `EmailIngestionService` and parser errors.
- [ ] Sign-off: Production rollout complete.

---

## 4. Post-apply verification checklist

Run these after applying the migration in each environment.

### 4.1 Migration recorded

```sql
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20260313025530_EnsureEmailMessageBodyAndErrorColumnsAreText';
```

- [ ] Exactly one row returned.

### 4.2 Column types on EmailMessages

```sql
SELECT column_name, data_type, character_maximum_length
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'EmailMessages'
  AND column_name IN ('BodyText', 'BodyHtml', 'BodyPreview', 'ParserError')
ORDER BY column_name;
```

- [ ] All four columns show `data_type` = `text` and `character_maximum_length` is null.
- [ ] If any show `character varying` with `character_maximum_length` = 2000, the migration did not apply correctly; investigate before proceeding.

### 4.3 No unexpected schema change

- [ ] Table `EmailMessages` still exists with expected indexes (e.g. `IX_EmailMessages_CompanyId_MessageId`, `IX_EmailMessages_CompanyId_ParserStatus_ReceivedAt`) – no drops.
- [ ] Row count or sample rows unchanged (optional spot check).

---

## 5. Smoke test checklist: long email ingestion

Use this to confirm that long email bodies no longer cause “value too long for type character varying(2000)”.

### 5.1 Prerequisites

- [ ] Environment has email ingestion configured (e.g. Graph API or test mailbox).
- [ ] Ability to send or simulate an email with a long body (e.g. >2000 characters plain and/or HTML).

### 5.2 Steps

1. **Trigger ingestion** of an email whose plain and/or HTML body is longer than 2000 characters (e.g. 3000+ characters).
2. **Confirm success:** No exception from `EmailIngestionService`; `SaveChangesAsync` completes.
3. **Confirm persistence:** Query `EmailMessages` for the new row; verify `BodyText` and/or `BodyHtml` contain the full content (length or substring check).
4. **Optional – parser error path:** If possible, trigger a parsing path that sets `ParserError` (e.g. malformed content that produces a long exception message). Confirm the row is saved and `ParserError` is stored without truncation or DB error.

### 5.3 Pass criteria

- [ ] Long-body email is ingested without “value too long for type character varying(2000)”.
- [ ] Stored body length matches expected (no silent truncation at 2000).
- [ ] No new application or DB errors in logs during the test.

---

## 6. Output summary

| Item | Status |
|------|--------|
| **Scope** | Confirmed: only BodyText, BodyHtml, BodyPreview, ParserError on EmailMessages. |
| **Business logic / truncation** | None; schema-only change. |
| **Risk** | Low; safe for staged rollout with verification. |
| **Rollout path** | Development → Staging → Production with checklists above. |
| **Verification** | Migration history + column types + optional data spot check. |
| **Smoke test** | Long email ingestion to validate fix and no truncation. |

No code changes were made during this readiness check; the migration and configuration are consistent and safe to roll out as-is.

---

## 7. Operator execution

For staged execution and post-deployment verification, use:

- **Operator summary:** [MIGRATION_20260313025530_OPERATOR_EXECUTION_SUMMARY.md](./MIGRATION_20260313025530_OPERATOR_EXECUTION_SUMMARY.md) – apply order, verification order, smoke test, rollback caution.
- **Read-only verification script:** `backend/scripts/verify-20260313025530-email-messages-text.sql` – run after apply in each environment.

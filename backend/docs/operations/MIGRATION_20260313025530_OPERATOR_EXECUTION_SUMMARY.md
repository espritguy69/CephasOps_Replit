# Migration 20260313025530 – Operator Execution Summary

**Migration:** `EnsureEmailMessageBodyAndErrorColumnsAreText`  
**Scope:** Table `EmailMessages` only – columns `BodyText`, `BodyHtml`, `BodyPreview`, `ParserError` → PostgreSQL `text`.  
**Rollout status:** Passed readiness review; **approved for staged rollout.**

**Approved execution path:** Development → Staging → Production.

**Required post-apply checks (per environment):**
- Migration row present in `__EFMigrationsHistory`
- `EmailMessages.BodyText` = `text`
- `EmailMessages.BodyHtml` = `text`
- `EmailMessages.BodyPreview` = `text`
- `EmailMessages.ParserError` = `text`
- Long-email ingestion succeeds without varchar(2000) overflow

**Rollback caution:** Do not run Down if `BodyPreview` or `ParserError` contain values longer than 2000 characters.

---

## Rollout status

| Environment   | Apply order | Verification | Smoke test |
|---------------|-------------|--------------|------------|
| Development   | 1st         | Required     | Required   |
| Staging       | 2nd         | Required     | Required   |
| Production    | 3rd         | Required     | Required   |

Do not promote to the next environment until the current one is verified and smoke-tested.

---

## Operator steps (exact apply order)

### 1. Pre-apply (once per environment)

1. **Backup** the database (or confirm automated backup).
2. Confirm the deployment artifact includes migration `20260313025530_EnsureEmailMessageBodyAndErrorColumnsAreText`.
3. (Optional) For Production, schedule a maintenance window if `EmailMessages` is very large.

### 2. Apply migration (exact order)

1. **Development**  
   - Run: `dotnet ef database update` from the API project (or apply the idempotent migrations script that includes this migration).  
   - Proceed only when the command completes without error.

2. **Staging**  
   - After Dev verification and smoke test pass: backup Staging, then run the same apply command/script for Staging.  
   - Proceed only when the command completes without error.

3. **Production**  
   - After Staging verification and smoke test pass: backup Production, then run the same apply command/script for Production.  
   - Proceed only when the command completes without error.

Apply **only** via the chosen canonical path (e.g. `dotnet ef database update` or your idempotent script). Do not manually run raw `ALTER TABLE` unless directed by a separate runbook.

---

## Verification steps (exact order)

Run **after** the migration apply in **each** environment, in this order:

1. **Migration recorded**  
   - Run script: `backend/scripts/verify-20260313025530-email-messages-text.sql`  
   - Or run manually:
     ```sql
     SELECT "MigrationId", "ProductVersion"
     FROM "__EFMigrationsHistory"
     WHERE "MigrationId" = '20260313025530_EnsureEmailMessageBodyAndErrorColumnsAreText';
     ```
   - **Expect:** Exactly one row.

2. **Column types**  
   - Use the same script (it includes column checks), or run:
     ```sql
     SELECT column_name, data_type, character_maximum_length
     FROM information_schema.columns
     WHERE table_schema = 'public' AND table_name = 'EmailMessages'
       AND column_name IN ('BodyText', 'BodyHtml', 'BodyPreview', 'ParserError')
     ORDER BY column_name;
     ```
   - **Expect:** All four rows with `data_type` = `text` and `character_maximum_length` null.  
   - **Fail:** Any column still `character varying` with length 2000 → do not promote; investigate.

3. **Table intact**  
   - Confirm table `EmailMessages` exists and expected indexes (e.g. `IX_EmailMessages_CompanyId_MessageId`) are present.  
   - Optional: spot-check row count or a few rows unchanged.

Sign-off the environment only when all verification steps pass.

4. **Record deployment audit (recommended)**  
   After verification and smoke test pass, record the rollout in **MigrationAudit** so the deployment is auditable. Use `backend/scripts/record-migration-audit.sql` (edit Environment, MigrationId, AppliedBy, MethodUsed, VerificationStatus, SmokeTestStatus, Notes), then run it against the database. See **MIGRATION_AUDIT.md** for when and how to use it.

---

## Smoke test steps

Run in **each** environment after verification:

1. **Trigger ingestion** of an email whose body (plain and/or HTML) is **longer than 2000 characters** (e.g. 3000+).
2. **Expect:**  
   - No exception from `EmailIngestionService`; `SaveChangesAsync` completes.  
   - No error: "value too long for type character varying(2000)".
3. **Confirm:** Query the new row in `EmailMessages`; `BodyText` and/or `BodyHtml` contain the full content (no truncation at 2000).
4. **Optional:** Trigger a path that sets `ParserError` with a long message; confirm the row saves without DB error or truncation.

**Pass:** Long-body email ingests successfully and stored length matches; no new errors in logs.

---

## Rollback caution

- **Do not run the migration Down** if, after the migration, **any** of `BodyPreview` or `ParserError` already contain values **longer than 2000 characters**.  
  Reverting those columns to `character varying(2000)` would require truncation or would fail; prefer fixing forward (e.g. keep migration applied and fix application issues).
- **BodyText** and **BodyHtml** are **not** reverted in Down (they remain `text`). Down only reverts `BodyPreview` and `ParserError` to `varchar(2000)`.
- If rollback is considered, check lengths first, e.g.:
  ```sql
  SELECT MAX(LENGTH("BodyPreview")) AS max_preview, MAX(LENGTH("ParserError")) AS max_error FROM "EmailMessages";
  ```
  If either max &gt; 2000, do not run Down without a separate remediation plan.

---

## Helper scripts added

| Script | Purpose |
|--------|--------|
| `backend/scripts/verify-20260313025530-email-messages-text.sql` | Read-only post-apply verification: migration in history + four columns `text`. |
| `backend/scripts/record-migration-audit.sql` | Optional: record this rollout in MigrationAudit after verification and smoke test pass. See **MIGRATION_AUDIT.md**. |

Run the verification script after applying the migration in each environment (Development, Staging, Production) and use its output to complete the verification steps above. Optionally run the audit script to record the deployment.

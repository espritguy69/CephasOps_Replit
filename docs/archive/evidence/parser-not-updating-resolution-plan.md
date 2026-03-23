# Parser Not Updating — Production-Safe Resolution Plan

**Goal:** Identify why `/orders/parser` is not updating and produce a production-safe fix plan.  
**Constraints:** Prefer config/ops/DB fixes first; small code changes only if necessary.

---

## Root cause (single sentence)

**Parser list does not update because at least one of the following is true:** no EmailIngest jobs are being created (scheduler not running or no active email account with poll interval > 0), jobs are stuck Queued (processor not running or not picking jobs), jobs are Failing (mail connection/auth/decrypt or parsing error), or emails are pulled and sessions created but template/rules cause drafts to be skipped or not created — **determine the exact break using the triage below.**

---

## Evidence (DB queries / log lines / screens)

Run these in order and record results. Use a DB client (psql, DBeaver, etc.) connected to the `cephasops` database.

### Step A — Confirm background jobs & scheduler health

**A.1 — EmailIngest jobs (last 24h)**

```sql
-- Copy-paste and run. Replace NOW() - interval '24 hours' if you need longer window.
SELECT "Id", "JobType", "State", "CreatedAt", "StartedAt", "CompletedAt", "LastError", "RetryCount", "PayloadJson"
FROM "BackgroundJobs"
WHERE "JobType" = 'EmailIngest'
  AND "CreatedAt" >= NOW() - interval '24 hours'
ORDER BY "CreatedAt" DESC
LIMIT 20;
```

**BackgroundJob.State values:** `0` = Queued, `1` = Running, `2` = Succeeded, `3` = Failed.

**Record:**
- **Is scheduler running?** YES if you see log line: `Email ingestion scheduler service started` (search app logs for this). NO if host is down or service never logs this.
- **Are EmailIngest jobs created periodically?** YES if the query returns rows with `CreatedAt` spread over time (e.g. every poll interval). NO if zero rows or only very old rows.
- **Are they completing? failing? stuck queued?** Check `State` (0=Queued, 1=Running, 2=Succeeded, 3=Failed). Check `LastError` for failed jobs.
- **Last successful EmailIngest job timestamp:** `MAX("CompletedAt")` where `State = 2`:

```sql
SELECT MAX("CompletedAt") AS "LastSuccessfulEmailIngest"
FROM "BackgroundJobs"
WHERE "JobType" = 'EmailIngest' AND "State" = 2;
```

**A.2 — Background jobs UI + API**

- **UI:** Open `/admin/background-jobs` (Admin/SuperAdmin). Check "Background Jobs" page loads; note "Recent Jobs" and "Email polling" accounts (last polled, status).
- **API:** `GET /api/background-jobs/health` and `GET /api/background-jobs/summary`. In health, check `emailPolling.accounts[].lastPolledAt` and `minutesSinceLastPoll` / `status`.

**A.3 — Job execution logs**

Search application logs for:

- `Email ingestion scheduler service started` → scheduler up
- `Background job processor service started` → processor up
- `Processing background job ... of type EmailIngest` → job picked
- `Email ingest job completed successfully` → success
- `Error processing email ingest job` or `Email ingestion failed` → failure (note `LastError` and exception message)

---

### Step B — Validate email account + template binding

**B.1 — Email account for admin@cephas.com.my**

```sql
SELECT "Id", "Name", "CompanyId", "Provider", "Host", "Port", "UseSsl",
       "Username", CASE WHEN "Password" IS NOT NULL AND "Password" != '' THEN 'SET' ELSE 'MISSING' END AS "PasswordSet",
       "IsActive", "PollIntervalSec", "DefaultParserTemplateId", "LastPolledAt", "CreatedAt"
FROM "EmailAccounts"
WHERE "Username" ILIKE '%admin%cephas%' OR "Name" ILIKE '%admin%cephas%';
```

**Return:**
- **Account exists + active + poll interval > 0:** Row exists, `IsActive` = true, `PollIntervalSec` > 0.
- **Protocol (POP3/IMAP), host/port/SSL:** `Provider` (e.g. `IMAP`/`POP3`), `Host`, `Port`, `UseSsl` match your mail server.
- **Credentials present:** `PasswordSet` = `SET`. Decrypt is app-side; if jobs fail with auth errors, decrypt or password is wrong.
- **Template assigned:** `DefaultParserTemplateId` IS NOT NULL (or a ParserRule/parser template matches by subject/sender).

**B.2 — Parser template and rules**

```sql
-- Template linked to the account (replace EMAIL_ACCOUNT_ID if needed)
SELECT pt."Id", pt."Name", pt."Code", pt."IsActive", pt."ExpectedAttachmentTypes",
       ea."Id" AS "EmailAccountId", ea."Name" AS "EmailAccountName"
FROM "ParserTemplates" pt
LEFT JOIN "EmailAccounts" ea ON ea."DefaultParserTemplateId" = pt."Id"
WHERE ea."Username" ILIKE '%admin%cephas%' OR ea."Name" ILIKE '%admin%cephas%';

-- Rules for that template (use template Id from above)
SELECT pr."Id", pr."ParserTemplateId", pr."IsActive", pr."MatchType", pr."MatchValue"
FROM "ParserRules" pr
WHERE pr."ParserTemplateId" IN (
  SELECT "DefaultParserTemplateId" FROM "EmailAccounts"
  WHERE "Username" ILIKE '%admin%cephas%' OR "Name" ILIKE '%admin%cephas%'
);
```

**Return:**
- **Template assigned:** DefaultParserTemplateId points to a row in ParserTemplates.
- **Template active:** ParserTemplate.`IsActive` = true.
- **Rules enabled:** At least one ParserRule with `IsActive` = true (or template matching logic accepts the emails).

---

### Step C — Validate data chain

**C.1 — Latest timestamps**

```sql
-- Latest EmailMessage (by account — replace with your EmailAccountId if needed)
SELECT "Id", "EmailAccountId", "Subject", "ReceivedAt", "ParserStatus", "CreatedAt"
FROM "EmailMessages"
WHERE "EmailAccountId" = (SELECT "Id" FROM "EmailAccounts" WHERE "Username" ILIKE '%admin%cephas%' LIMIT 1)
ORDER BY "CreatedAt" DESC
LIMIT 5;

-- Latest ParseSession (Email source)
SELECT "Id", "EmailMessageId", "Status", "SourceType", "SourceDescription", "CreatedAt", "CompletedAt"
FROM "ParseSessions"
WHERE "SourceType" = 'Email'
ORDER BY "CreatedAt" DESC
LIMIT 5;

-- Latest ParsedOrderDraft
SELECT "Id", "ParseSessionId", "ValidationStatus", "ServiceId", "CreatedAt"
FROM "ParsedOrderDrafts"
WHERE "IsDeleted" = false
ORDER BY "CreatedAt" DESC
LIMIT 5;
```

**Return — where chain breaks:**

| Observation | Break point |
|-------------|------------|
| No recent EmailMessage rows | **Email pull** (job not running or mail fetch failing) |
| EmailMessage recent, no ParseSession | **Job execution** (job fails before session) or **ParseSession creation** (template/routing skips) |
| ParseSession recent, Status = 'Skipped' | **ParseSession creation** (template/attachment rules skip) |
| ParseSession recent, no ParsedOrderDraft | **Draft creation** (parsing logic or template doesn’t create drafts) |
| ParsedOrderDraft recent | **No break (UI)** — check UI/filters/API next |

---

### Step D — UI / filters / sorting

- **Default filters:** Parser list page uses no default validationStatus/sourceType/status (only page/sort). So **default filters are not hiding drafts** unless you changed the frontend.
- **API returns drafts but UI not rendering:** Call `GET /api/parser/drafts?page=1&pageSize=50&sortBy=createdAt&sortOrder=desc`. Compare `totalCount` and `items[]` with what the UI shows. If API has rows and UI doesn’t, it’s a UI bug (e.g. paging, key, or permission).
- **UI bug?** YES only if API returns drafts and UI does not show them after clearing any manual filters.

---

## Fix plan (ordered)

### 1) Triage (≈15 minutes)

1. **Background Jobs UI** → filter or inspect for **EmailIngest** (use DB query A.1 if UI doesn’t filter by type).
2. **If NO jobs** → Scheduler not enqueueing or no active account (Step 2, Case 1).
3. **If jobs FAIL** → Check `LastError` and logs (Step 2, Case 3).
4. **If jobs COMPLETE but no drafts** → Template match / parsing / rules (Step 2, Case 4).
5. **If drafts exist in DB but not in UI** → Step 2, Case 5.

**Outcome:** One clear break point (no jobs / stuck queued / failing / no session / no draft / UI).

---

### 2) Likely root causes + fix actions

**Case 1 — No EmailIngest jobs created**

| Cause | Fix |
|-------|-----|
| EmailAccount inactive or PollIntervalSec = 0 | Set `IsActive = true`, `PollIntervalSec` = 300 (or desired seconds). |
| Scheduler not running in environment | Confirm hosted services start (no startup exception). Check logs for "Email ingestion scheduler service started". |
| DB/env missing, host crashes early | Ensure `ConnectionStrings__DefaultConnection` and app settings are correct in deployment. |

**Verify:**

```sql
-- After fix: new job within one poll interval
SELECT "Id", "CreatedAt", "State" FROM "BackgroundJobs"
WHERE "JobType" = 'EmailIngest' ORDER BY "CreatedAt" DESC LIMIT 1;
```

- New row with `CreatedAt` within last (PollIntervalSec + 60) seconds; then job moves to Completed.

---

**Case 2 — Jobs created but stuck Queued/Running**

| Cause | Fix |
|-------|-----|
| BackgroundJobProcessor not running | Restart app; confirm log "Background job processor service started". |
| Worker crash loop | Check logs for unhandled exceptions in job processor; fix config/DB/dependency. |

**Verify:**

- Queued jobs get `StartedAt` and then `CompletedAt`; `State` becomes Succeeded (or Failed with clear LastError).

---

**Case 3 — Jobs failing**

| Cause | Fix |
|-------|-----|
| POP3/IMAP connection/auth failure | Correct Host, Port, UseSsl in EmailAccounts. Test with a mail client or manual script. |
| Credential decrypt failure | Ensure encryption key/IV in config match what was used to store password; re-save password if needed. |
| Throttling/timeouts | Increase timeouts if needed; reduce PollIntervalSec temporarily. |

**Verify:**

```sql
SELECT "LastError", "CompletedAt", "State" FROM "BackgroundJobs"
WHERE "JobType" = 'EmailIngest' AND "State" = 3
ORDER BY "UpdatedAt" DESC LIMIT 1;
```

- After fix, next run: `State` = 2 (Succeeded) and new rows in EmailMessages.

---

**Case 4 — EmailMessage created but no ParseSession / Drafts**

| Cause | Fix |
|-------|-----|
| No active template / DefaultParserTemplateId missing | Set EmailAccounts.`DefaultParserTemplateId` to a valid, active ParserTemplate Id. |
| Template inactive | Set ParserTemplates.`IsActive` = true. |
| Rules too strict / invalid | Adjust ParserRules (MatchType/MatchValue) or ensure at least one rule matches your test emails. |
| Attachment parsing skipped | Ensure template ExpectedAttachmentTypes and email attachments align; or use a template that allows body-only (e.g. assurance). |

**Verify:**

```sql
SELECT "Id", "Status", "SourceDescription", "CreatedAt" FROM "ParseSessions"
WHERE "SourceType" = 'Email' ORDER BY "CreatedAt" DESC LIMIT 3;
```

- New ParseSession rows; Status not stuck as 'Skipped' for emails you expect to process. Then check ParsedOrderDrafts for new rows.

---

**Case 5 — Drafts exist but UI not showing**

| Cause | Fix |
|-------|-----|
| Paging/sort showing old page | Increase page size or go to page 1; sort by createdAt desc. |
| API filter mismatch | Ensure no query params excluding the new drafts. |
| Permission/context | Confirm user has access to parser; no department/company filter hiding rows (parser is company-scoped only in this codebase). |

**Verify:**

- `GET /api/parser/drafts?page=1&pageSize=50&sortBy=createdAt&sortOrder=desc` returns the new drafts in `items`; UI shows them.

---

## Production checklist + rollback

### Pre-production (this week)

- [ ] Run full triage (Steps A–D) in staging/UAT and record results.
- [ ] Fix the identified case (config/DB first).
- [ ] Confirm EmailIngest jobs complete and new drafts appear in parser list.
- [ ] Document the exact EmailAccount Id, ParserTemplate Id, and PollIntervalSec used in prod.
- [ ] Ensure production connection string and encryption keys are set (no committed secrets).
- [ ] If any code change: small, reviewed, covered by existing or new tests.

### Deployment day

- [ ] Apply DB/config changes (EmailAccounts.IsActive, PollIntervalSec, DefaultParserTemplateId) if any.
- [ ] Deploy app (or restart) so hosted services start.
- [ ] Within 2× PollIntervalSec: run A.1 query and confirm at least one new EmailIngest job and one Completed.
- [ ] Check health: `GET /api/background-jobs/health` — emailPolling status Healthy/Warning for the account.
- [ ] Send a test email (or use existing inbox); within 5 minutes confirm new draft in DB and in UI at `/orders/parser`.

### Monitoring / alerts (must-have)

- [ ] **Alert:** No successful EmailIngest job in the last 2× PollIntervalSec (e.g. 10 min if poll = 300s). Use A.1 / last successful query or health API.
- [ ] **Alert:** EmailIngest job failed (State = 3). Log LastError to your logging/monitoring system.
- [ ] **Dashboard:** Count of EmailIngest by State (Queued, Running, Succeeded, Failed) and last CompletedAt; EmailAccounts.LastPolledAt.

### Smoke test checklist

1. **Scheduler:** Log contains "Email ingestion scheduler service started".
2. **Processor:** Log contains "Background job processor service started".
3. **Jobs:** DB has recent EmailIngest rows; at least one Succeeded in last 10 minutes (or 2× poll interval).
4. **Account:** EmailAccounts for admin mailbox has IsActive=1, PollIntervalSec>0, LastPolledAt updated recently.
5. **Chain:** Latest EmailMessage → ParseSession → ParsedOrderDraft for that account exist and are recent.
6. **UI:** `/orders/parser` (parser list) shows latest drafts; API `GET /api/parser/drafts` returns same.

### Rollback

- **Config/DB only:** Revert EmailAccounts (IsActive=0 or PollIntervalSec=0, or DefaultParserTemplateId=NULL). No app rollback needed.
- **App deploy:** Redeploy previous version. Hosted services will restart; in-flight jobs may retry on next cycle.
- **No schema rollback** needed for this plan (no migrations in scope).

---

## Summary table

| Step | Check | DB/API/Log |
|------|--------|------------|
| A   | Scheduler running | Log: "Email ingestion scheduler service started" |
| A   | EmailIngest jobs created | Query A.1: rows with recent CreatedAt |
| A   | Jobs completing | Query A.1: State=2, LastSuccessfulEmailIngest |
| B   | Account active + template | Queries B.1, B.2 |
| C   | Chain break | Queries C.1 → EmailMessage → ParseSession → Draft |
| D   | UI bug | API `/api/parser/drafts` vs UI |
| Fix | Per case 1–5 | Apply fix then re-run relevant verify query/API |

Use this document as the single runbook for triage, fix, verification, and production rollout.

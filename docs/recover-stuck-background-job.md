# Runbook: Recover stuck EmailIngest BackgroundJob

**Purpose:** Clear the BackgroundJob that has been stuck in `Running` since 2026-01-05 so the email ingestion scheduler can create and run new jobs.

**Scope:** No application code changes. Database-only recovery script + app restart.

---

## 1. Prerequisites

- **DB backup:** Take a full backup of the database (or at least the `BackgroundJobs` table) before running the SQL script. Example:
  ```bash
  pg_dump -h <host> -U <user> -d cephasops -t '"BackgroundJobs"' -F c -f backup_backgroundjobs_$(date +%Y%m%d_%H%M).dump
  ```
- **Maintenance window:** Schedule a short window (e.g. 5–10 minutes) when the CephasOps API/host process can be stopped so that:
  - No new jobs are created or processed while we change the stuck job.
  - The recovery script runs against a quiescent state.
- **Access:** Database connection (e.g. `psql` or your organisation’s SQL client) with permission to `SELECT`/`UPDATE` on `BackgroundJobs` and to create a session-scoped temp table.
- **Script:** Use `scripts/test/recover_stuck_emailingest_job.sql` or `recover_stuck_emailingest_job_20251217.sql` (run from project root or with correct path). **Run with `psql`** so that `\set ON_ERROR_STOP on` is honoured: the script sets this at the top so that if the guard raises (job not found or not in `Running` state), the script stops immediately and backup/update are not executed.

---

## 2. Steps

### 2.1 Stop the application

- Stop the CephasOps API / host process (e.g. stop the service or kill the process) so that:
  - The BackgroundJob processor is not running.
  - The EmailIngestion scheduler is not running.
- Confirm the process has exited (e.g. no `CephasOps.Api` or dotnet process for the app).

### 2.2 Run the SQL script

- Connect to the same database the app uses (e.g. `cephasops`).
- Run the recovery script with **psql** (required for `ON_ERROR_STOP` so the script stops on guard failure):
  ```bash
  psql -h <host> -p 5432 -U <user> -d cephasops -f scripts/test/recover_stuck_emailingest_job.sql
  ```
- Check the output:
  - **BEFORE:** One row with `State = Running`, `CompletedAt = null`.
  - **BACKUP:** One row copied into temp table `backup_background_job_stuck_emailingest_20260105`.
  - **UPDATE:** One row updated.
  - **AFTER:** Same row with `State = Failed`, `CompletedAt` and `LastError` set.

If the script raises an error (e.g. “job not found” or “job is not in Running state”), do not proceed; investigate and fix before retrying.

### 2.3 Start the application

- Start the CephasOps API / host process again.
- Ensure it starts without errors and can connect to the database.

---

## 3. Verification queries

Run these **after** the app has been restarted and has run for at least one scheduler cycle (e.g. 1–2 minutes).

### 3.1 Stuck job is no longer Running

```sql
SELECT "Id", "JobType", "State", "CompletedAt", "LastError"
FROM "BackgroundJobs"
WHERE "Id" = '93840d17-21e0-44f6-8225-bb85b8f4a8f8'::uuid;
```

Expected: `State = Failed`, `CompletedAt` and `LastError` set (e.g. `LastError` like `Recovered: stuck Running since 2026-01-05`).

### 3.2 New EmailIngest jobs created

```sql
SELECT "Id", "State", "CreatedAt", "CompletedAt"
FROM "BackgroundJobs"
WHERE "JobType" = 'EmailIngest'
ORDER BY "CreatedAt" DESC
LIMIT 5;
```

Expected: At least one **new** job (CreatedAt after the recovery run), typically `Queued` or `Running` or `Succeeded`.

### 3.3 New EmailIngest jobs completed

```sql
SELECT "State", COUNT(*) AS cnt
FROM "BackgroundJobs"
WHERE "JobType" = 'EmailIngest'
  AND "CreatedAt" >= NOW() - INTERVAL '1 hour'
GROUP BY "State";
```

Expected: Some `Succeeded` (and/or `Failed`) in the last hour if the scheduler and processor are working.

### 3.4 LastPolledAt updated (email pull running)

```sql
SELECT "Name", "Username", "LastPolledAt", "IsActive"
FROM "EmailAccounts"
WHERE "IsDeleted" = false AND "Username" ILIKE '%admin@cephas.com.my%';
```

Expected: `LastPolledAt` more recent than before recovery (e.g. within the last few minutes).

### 3.5 EmailMessage count increases (optional)

```sql
SELECT COUNT(*) AS total,
       COUNT(*) FILTER (WHERE "CreatedAt" >= NOW() - INTERVAL '1 hour') AS last_hour
FROM "EmailMessages"
WHERE "IsDeleted" = false;
```

Expected: If there is new mail, `last_hour` (or a later interval) may increase over time.

---

## 4. Scheduler guard: “skip enqueue when a Running/Queued EmailIngest exists”

The scheduler **does** skip creating a new EmailIngest job when there is already a **Queued** or **Running** EmailIngest job for the same account.

- **Where it is:**  
  `backend/src/CephasOps.Application/Workflow/Services/EmailIngestionSchedulerService.cs`  
  Method: `ScheduleEmailIngestionJobsAsync`.

- **What it does:**
  - Loads all EmailIngest jobs where `State` is not `Succeeded` and not `Failed` (i.e. Queued or Running).
  - For each active email account, checks whether any of those jobs has `PayloadJson` containing that account’s Id.
  - If such a job exists, it **does not** create a new job and logs (Debug):  
    `"Skipping account {AccountName} - job already exists (JobId: {JobId}, State: {State})"`.
  - If none exists, it creates a new Queued EmailIngest job for that account.

- **Why the stuck job blocked ingestion:**  
  The stuck job had `State = Running` and its payload contained the email account Id. So every scheduler cycle saw “an active job for this account” and never enqueued a new one.

- **How to verify the guard is no longer triggered after recovery:**
  1. **Logs:** After recovery and app restart, at Debug level look for the skip message:  
     `Skipping account ... - job already exists`.  
     You should **not** see this for the recovered account when no other Queued/Running EmailIngest exists for it. Instead you may see:  
     `Scheduled email ingestion job for account ...` or `Created N email ingestion job(s)`.
  2. **Database:** Run verification query 3.2 above. New jobs with `CreatedAt` after the recovery time confirm the scheduler is no longer skipping enqueue for that account (because the only “active” job for it is now `Failed`).

---

## 5. Rollback (if needed)

If you must restore the row to its pre-recovery state (e.g. for investigation):

- The script backs up the row into a **session-scoped** temp table `backup_background_job_stuck_emailingest_20260105`. That table exists only in the session that ran the script and is dropped when the session ends, so it is **not** available in a new session.
- For a durable backup, before running the recovery script, run:
  ```sql
  CREATE TABLE IF NOT EXISTS backup_background_job_93840d17 AS
  SELECT * FROM "BackgroundJobs" WHERE "Id" = '93840d17-21e0-44f6-8225-bb85b8f4a8f8'::uuid;
  ```
  Then to rollback (only if you really need the job back in “Running” for forensics):
  ```sql
  UPDATE "BackgroundJobs" b
  SET "State" = backup."State", "CompletedAt" = backup."CompletedAt", "LastError" = backup."LastError", "UpdatedAt" = backup."UpdatedAt"
  FROM backup_background_job_93840d17 backup
  WHERE b."Id" = backup."Id";
  ```
  Prefer not to rollback to Running in production; use only for analysis on a copy if needed.

---

## 6. Summary

| Step | Action |
|------|--------|
| 1 | Take DB backup; schedule maintenance window. |
| 2 | Stop the application. |
| 3 | Run `scripts/test/recover_stuck_emailingest_job.sql`. |
| 4 | Start the application. |
| 5 | Run verification queries (3.1–3.5) and confirm new EmailIngest jobs and, if applicable, updated `LastPolledAt` and new `EmailMessages`. |

The script only updates the single stuck job Id; it does not change any other rows or application code.

---

## 7. Automatic stale recovery

The application now includes a **stale-job reaper** and a **scheduler guard** so that a stuck `Running` EmailIngest job no longer blocks ingestion forever.

### 7.1 Config keys

| Key | Default | Description |
|-----|---------|-------------|
| `BackgroundJobs:StaleRunning:EmailIngestMinutes` | 10 | After this many minutes, a Running EmailIngest job is treated as stale and reaped (marked Failed). |
| `BackgroundJobs:StaleRunning:DefaultMinutes` | 120 | Same for all other job types (e.g. PnlRebuild). |

Override via `appsettings.json` or environment variables (e.g. `BackgroundJobs__StaleRunning__EmailIngestMinutes=10`).

### 7.2 What “stale” means

- **Stale Running job:** A job with `State = Running` whose **running duration** exceeds the threshold for its `JobType`.
- **Running duration** is computed as: `now - effectiveStart`, where **effectiveStart** = `StartedAt` if set, else `UpdatedAt`, else `CreatedAt`.
- **Reaper** (in `BackgroundJobProcessorService`): On each processing loop, finds Running jobs older than the threshold, sets them to `Failed` with `LastError = 'Recovered: stale Running timeout'`, and logs one line per reaped job (JobId, JobType, runningDuration, threshold).
- **Scheduler guard** (in `EmailIngestionSchedulerService`): When deciding if an account already has an “active” job, **stale** Running jobs are ignored, so the scheduler can enqueue a new EmailIngest job for that account. The reaper will mark the stale one Failed on the next processor cycle.

### 7.3 How to verify

**Manual verification (recommended):**

1. **Create a fake stuck job** (use an existing active EmailAccount Id, e.g. from `EmailAccounts` where `Username ILIKE '%admin@cephas.com.my%'`):
   ```sql
   INSERT INTO "BackgroundJobs" (
     "Id", "JobType", "PayloadJson", "State", "RetryCount", "MaxRetries", "Priority",
     "CreatedAt", "StartedAt", "UpdatedAt"
   ) VALUES (
     gen_random_uuid(),
     'EmailIngest',
     '{"emailAccountId":"<YOUR_EMAIL_ACCOUNT_ID>"}',
     'Running',
     0, 3, 1,
     NOW() - INTERVAL '1 hour',
     NOW() - INTERVAL '1 hour',  -- StartedAt 1 hour ago
     NOW() - INTERVAL '1 hour'
   );
   ```
2. **Start the application** (or ensure it is running).
3. **Within one processor loop (e.g. 30–60 seconds):**
   - The reaper should mark the fake job `Failed` with `LastError = 'Recovered: stale Running timeout'`. Check logs for: `Reaped stale Running job ... runningDuration ... threshold 10m`.
   - The scheduler should create a **new** EmailIngest job for that account (no longer blocked by the stale one).
4. **After the new job runs:** `EmailAccounts.LastPolledAt` for that account should update; new `EmailMessages` / `ParsedOrderDrafts` may appear if there is mail.

**Queries:**

- Reaped job: `SELECT "Id", "State", "LastError", "CompletedAt" FROM "BackgroundJobs" WHERE "JobType" = 'EmailIngest' ORDER BY "CreatedAt" DESC LIMIT 5;`
- New job for account: same query; expect a new row with `State` Queued/Running/Succeeded and `CreatedAt` after the fake job.
- LastPolledAt: `SELECT "Username", "LastPolledAt" FROM "EmailAccounts" WHERE "Id" = '<YOUR_EMAIL_ACCOUNT_ID>';`

### 7.4 Fallback

The **manual SQL recovery script** (`scripts/test/recover_stuck_emailingest_job.sql` or `recover_stuck_emailingest_job_20251217.sql`) remains valid. Run it with **psql** so that `\set ON_ERROR_STOP on` takes effect and the script stops immediately if the guard raises (no backup or update is run). Use it when:
- You need to clear a specific stuck job immediately (e.g. during a maintenance window without waiting for the reaper), or
- The app is stopped and you want to fix the row before restarting.

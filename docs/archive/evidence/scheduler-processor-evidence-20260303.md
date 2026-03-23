# Scheduler & processor evidence (2026-03-03)

Evidence only. Prove whether scheduler and processor are running today and whether we use the correct database.

---

## 1) DB time + environment

| Metric | Value |
|--------|--------|
| **db_now** | 2026-03-03 12:00:46+08 |
| **db_name** | cephasops |
| **host_ip** | ::1 |
| **port** | 5432 |
| **current_schema** | public |

**App connection string** (from `appsettings.Development.json`):  
`Host=localhost;Port=5432;Database=cephasops;Username=postgres;...`

**Conclusion:** Same database. localhost/::1, port 5432, database `cephasops`. Schema is default `public`.

---

## 2) BackgroundJob activity (all job types)

**Created in last 24h, by JobType and State:**

| JobType | State | cnt |
|---------|--------|-----|
| pnlrebuild | Succeeded | 4 |
| populatestockbylocationsnapshots | Succeeded | 4 |
| reconcileledgerbalancecache | Succeeded | 4 |

**EmailIngest in last 24h:** 0 (no rows).

**Newest 20 BackgroundJobs (all time):**  
Top rows are pnlrebuild, populatestockbylocationsnapshots, reconcileledgerbalancecache with CreatedAt 2026-03-02, StartedAt/CompletedAt 2026-03-03 02:23–03:14+08. Then older stock snapshot jobs from Feb. Then EmailIngest: one **Failed** (93840d17, CompletedAt 2026-03-03 11:47:36 — manual recovery), two **Succeeded** from 2026-01-05.

**EmailIngest jobs from logs (current state):**

| Id | State | CreatedAt | CompletedAt | last_error |
|----|--------|-----------|-------------|------------|
| 31f4cdbd-6805-475c-89c7-1fd60d46b3d5 | **Running** | 2025-12-17 19:04:37+08 | (null) | — |
| 93840d17-21e0-44f6-8225-bb85b8f4a8f8 | Failed | 2026-01-05 08:33:21+08 | 2026-03-03 11:47:36+08 | Recovered: stuck Running since 2026-01-05 |

So: **one EmailIngest job (31f4cdbd) is still Running**; the other (93840d17) was manually failed.

---

## 3) Hosted service confirmation (logs)

**Startup lines (proof both services started):**

- `2026-03-03 10:22:36.560 +08:00 [INF] ... BackgroundJobProcessorService Background job processor service started`
- `2026-03-03 10:22:36.560 +08:00 [INF] ... EmailIngestionSchedulerService Email ingestion scheduler service started`

Same in second log file at 10:38:06 and 10:41:07 and 10:50:30 (restarts).

**Periodic scheduler logs (sample — last 50 relevant lines with timestamps):**

Scheduler runs every ~30s and logs (Debug) when it skips an account because a job already exists:

- 10:22:38.828 Skipping account Simon - job already exists (JobId: "31f4cdbd-...", State: "Running")
- 10:22:38.835 Skipping account Admin CEPHAS - job already exists (JobId: "93840d17-...", State: "Running")
- 10:23:08.847 Processing 3 background jobs (processor)
- 10:23:08.853 Skipping account Simon - job already exists (31f4cdbd, Running)
- 10:23:08.858 Skipping account Admin CEPHAS - job already exists (93840d17, Running)
- … every ~30s through 10:38:39.721 (cephasops-20260303.log) and 10:52:01.491 (cephasops-20260303_001.log)

No log line “Created N email ingestion job(s)” in the sampled logs. Processor logs show it processing pnlrebuild, reconcileledgerbalancecache, populatestockbylocationsnapshots and completing them successfully.

**Conclusion:** Processor and scheduler both started and ran (logs from 10:22 to at least 10:52). Scheduler did not enqueue any EmailIngest job because it considered both accounts already covered by existing jobs (31f4cdbd Running, 93840d17 Running until manual recovery at 11:47).

---

## 4) EmailAccounts sanity (admin@cephas.com.my)

| Field | Value |
|--------|--------|
| Id | 637fb63a-fc82-49a2-826f-17976c1f959d |
| Name | Admin CEPHAS |
| Username | admin@cephas.com.my |
| IsActive | true |
| PollIntervalSec | 60 |
| LastPolledAt | 2026-01-08 10:34:21+08 |
| last_polled_is_today | **false** |

**Is LastPolledAt changing today?** **NO** (LastPolledAt is 2026-01-08; no poll today).

---

## 5) Conclusion

**C) Scheduler running but not enqueueing (explain why)**

- **DB is correct:** Same database (cephasops, localhost/::1:5432) as the app connection string.
- **Processor and scheduler are running (when the app is up):** Logs show both services starting and the scheduler firing every ~30s and the processor handling pnlrebuild, reconcileledgerbalancecache, populatestockbylocationsnapshots.
- **Why no EmailIngest:** The scheduler treats any **Queued** or **Running** EmailIngest job for an account as “job already exists” and does not enqueue another. Two such jobs were present:
  - **93840d17** (Admin CEPHAS): was **Running** until manually set to **Failed** at 11:47 on 2026-03-03.
  - **31f4cdbd** (Simon): still **Running** (CreatedAt 2025-12-17, no CompletedAt).
So while the app was running (logs up to 10:52), the scheduler correctly skipped both accounts. After 11:47, Admin CEPHAS is unblocked in the DB, but **no new EmailIngest jobs appear in the last 24h**, so either:
  - the app was **not running** after the recovery (no scheduler cycles to enqueue for Admin CEPHAS), or  
  - the app running after 11:47 does not include the **stale-running reaper** (so 31f4cdbd would never be reaped and the scheduler would still see an “active” job for Simon; Admin CEPHAS could still get new jobs — the absence of any suggests the app may not be running or not hitting this DB).
- **LastPolledAt** for admin@cephas.com.my is not from today, consistent with no EmailIngest run today.

**Summary:** Scheduler and processor **do** run when the app is up and use the **correct** DB. EmailIngest is not enqueued because the scheduler sees existing “active” EmailIngest jobs (one still **Running**). After manually failing the other, there is no evidence of app activity or new EmailIngest jobs, so the app is likely **not running** after the manual recovery, or the running instance does not have the new reaper/scheduler logic.

---

*Evidence from: `scripts/test/scheduler_processor_evidence.sql` and `backend/src/CephasOps.Api/logs/cephasops-20260303.log`, `cephasops-20260303_001.log`*

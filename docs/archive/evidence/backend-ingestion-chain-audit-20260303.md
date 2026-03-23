# Backend ingestion chain audit (2026-03-03)

**Context:** ParsedOrderDraft has no new rows since 2026-01-05. Frontend is correct; backend ingestion is not producing new drafts. Evidence-only audit (no code changes).

---

## 1) Background jobs (EmailIngest, last 14 days)

**Query:** `BackgroundJobs` where `JobType = 'EmailIngest'` and `CreatedAt >= NOW() - 14 days`.

| Result | Value |
|--------|--------|
| **Count by State** | (0 rows) — no EmailIngest jobs in last 14 days |
| **Newest CreatedAt** | (null) |
| **Newest CompletedAt** | (null) |
| **Last successful (Succeeded) job** | (null) |
| **Last 5 Failed jobs** | (0 rows) |

**All-time (last 3 EmailIngest jobs):**

| Id | State | CreatedAt | CompletedAt | LastError (preview) |
|----|--------|------------|-------------|----------------------|
| 93840d17-21e0-44f6-8225-bb85b8f4a8f8 | **Running** | 2026-01-05 08:33:21+08 | **(null)** | (empty) |
| 08bc86bf-3aee-43e3-8bcd-67523bedc658 | Succeeded | 2026-01-05 08:31:51+08 | 2026-01-05 16:31:56+08 | — |
| c8c32748-a436-48b0-99ef-2ff9dad7f7eb | Succeeded | 2026-01-05 08:30:51+08 | 2026-01-05 16:30:54+08 | — |

**Evidence:** One EmailIngest job has been in state **Running** since 2026-01-05 08:33:21 with **CompletedAt = null**. No EmailIngest jobs created in the last 14 days.

---

## 2) EmailAccounts (admin@cephas.com.my)

**Matched by:** `Username ILIKE '%admin@cephas.com.my%'` (Name = "Admin CEPHAS").

| Field | Value |
|--------|--------|
| **Id** | 637fb63a-fc82-49a2-826f-17976c1f959d |
| **Name** | Admin CEPHAS |
| **Username** | admin@cephas.com.my |
| **IsActive** | true |
| **PollIntervalSec** | 60 |
| **LastPolledAt** | 2026-01-08 10:34:21+08 |
| **Provider** | POP3 |
| **Host** | mail.cephas.com.my |
| **Port** | 995 |
| **UseSsl** | true |
| **Username present?** | yes |
| **Password present?** | yes |
| **DefaultParserTemplateId** | (null) |

**PASS/FAIL for “scheduler should ingest this account”:** **PASS** — Account is active, has credentials, and has a poll interval. Scheduler has enough to attempt ingestion. Note: **DefaultParserTemplateId** is null; parsing may rely on rules or fallback behaviour.

---

## 3) EmailMessage (last 14 days, that EmailAccountId)

**Query:** `EmailMessages` for account `637fb63a-fc82-49a2-826f-17976c1f959d`, `CreatedAt >= NOW() - 14 days`, `IsDeleted = false`.

| Result | Value |
|--------|--------|
| **Count last 14 days** | 0 |
| **Newest CreatedAt** | (null) |
| **Newest Subject** | (null) |
| **Newest From** | (null) |

**Evidence:** No email messages for this account in the last 14 days. If the chain reached “email pull”, we would see rows here; with no EmailIngest jobs in 14 days, pull has not run in that window.

---

## 4) ParseSession (last 14 days)

**Query:** `ParseSessions`, `IsDeleted = false`, `CreatedAt >= NOW() - 14 days`.

| Result | Value |
|--------|--------|
| **Count last 14 days** | 0 |
| **Newest CreatedAt** | (null) |
| **Sample Status** | (null) |
| **Last 10 rows** | (0 rows) |

**Evidence:** No parse sessions in the last 14 days. Consistent with no new emails and no new ingestion jobs.

---

## 5) ParsedOrderDrafts (last 14 days)

| Result | Value |
|--------|--------|
| **drafts_last_14d** | 0 |

(Already established: no new drafts since 2026-01-05.)

---

## Conclusion: CHAIN BREAK

**CHAIN BREAK = Jobs stuck/failed**

- One **EmailIngest** job is stuck in state **Running** since **2026-01-05 08:33:21+08** with **CompletedAt = null**.
- No EmailIngest jobs have been **created** in the last 14 days (so the break is not only “jobs not created” in that window; the last created job never completed).
- That single stuck **Running** job can block the pipeline by:
  - Causing the processor to treat it as still in progress and not complete new work, and/or
  - Causing the scheduler to avoid enqueueing further EmailIngest jobs for the same account or globally.
- Downstream evidence is consistent: no EmailMessages in last 14 days (pull not run), no ParseSessions, no new ParsedOrderDrafts.

**Not** the other options:

- **EmailIngest jobs not created** — Jobs were created up to 2026-01-05; the issue is the last one never completed and no new ones appear afterward.
- **Email pull not happening** — Consequence of no successful/run jobs, not the root cause.
- **ParseSession not created** / **Draft creation skipped** — No new emails/sessions in 14 days, so the break is upstream.

**Recommended next step (no code change):** Manually set the stuck job’s state to **Failed** (and set **CompletedAt** and optionally **LastError**) so the pipeline can advance and new jobs can be created/processed. Then confirm whether the scheduler creates new EmailIngest jobs and ingestion resumes.

---

*Evidence from: `scripts/test/backend_chain_audit.sql`*

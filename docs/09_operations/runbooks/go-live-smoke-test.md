# Go-live smoke test (~10 min)

**Purpose:** Confirm email ingestion and parser flow after deploy. All checks read-only except sending one test email.

**Endpoint:** `GET /api/admin/diagnostics/email-ingestion` (requires auth).  
**Frontend:** `/orders/parser` (e.g. filter `validationStatus=NeedsReview`).

```bash
curl -s -H "Authorization: Bearer <TOKEN>" -H "Accept: application/json" "http://localhost:5000/api/admin/diagnostics/email-ingestion"
```
Replace `<TOKEN>` with a valid JWT (e.g. from login).

---

## Checklist

| # | Check | PASS / FAIL |
|---|--------|-------------|
| 1 | **Diagnostics returns 200** — `GET /api/admin/diagnostics/email-ingestion` with valid Bearer token returns HTTP 200 and JSON with `success: true`, `data` present. | ☐ |
| 2 | **lastSuccessfulEmailIngestAt within 2× poll interval** — In `data`, `lastSuccessfulEmailIngestAt` is within the last 2× the account poll interval (e.g. if PollIntervalSec = 60, within last 2 min). | ☐ |
| 3 | **Succeeded jobs in last 24h** — `data.emailIngestJobsLast24hByState.Succeeded` > 0. | ☐ |
| 4 | **Admin CEPHAS LastPolledAt updates** — In `data.emailAccountsLastPolledAt`, find "Admin CEPHAS" (or admin@cephas.com.my); note `lastPolledAt`. Wait ≥1 poll interval, call diagnostics again; `lastPolledAt` is newer. | ☐ |
| 5 | **draftsCreatedToday increases after test email** — Note `data.draftsCreatedToday`. Send a test order email to the ingest mailbox; wait for next poll + process. Call diagnostics again; `draftsCreatedToday` increased. | ☐ |
| 6 | **Parser list shows new draft** — Open `/orders/parser` (e.g. Validation status = Needs Review). New draft from step 5 appears; "last updated" is recent. | ☐ |
| 7 | **Recovery runbook & script** — Runbook: [docs/recover-stuck-background-job.md](recover-stuck-background-job.md). SQL: [scripts/test/recover_stuck_emailingest_job.sql](../scripts/test/recover_stuck_emailingest_job.sql) (or `recover_stuck_emailingest_job_20251217.sql` for the other job). Use if EmailIngest is stuck in Running. | ☐ |

---

## Notes

- **Poll interval:** Per account (e.g. Email system settings). Default often 60s → "within 2 min" = 120s.
- **Token:** Use a valid JWT (e.g. SuperAdmin) for the diagnostics endpoint.
- **Failure:** If any check fails, see [recover-stuck-background-job.md](recover-stuck-background-job.md) and scheduler/processor logs before go-live.

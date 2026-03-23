# Trace Explorer Runbook

**Purpose:** How to use the Operational Timeline / Trace Explorer for root-cause investigation and day-to-day operations.

---

## 1. What the Trace Explorer Is

The Trace Explorer shows a **single chronological timeline** for a given ID or entity. It merges:

- Workflow transitions (requested, started, completed)  
- Events (emitted, processed)  
- Event handler runs (started, succeeded/failed)  
- Background job runs (queued, started, completed/failed)  

So you can see the full execution chain in one place instead of jumping between Event Bus Monitor and Background Jobs.

---

## 2. How to Open the Trace Explorer

- **Admin UI:** **Admin → Trace Explorer** (or navigate to `/admin/trace-explorer`).  
- **From Event Bus Monitor:** On an event detail, use **View full trace in Trace Explorer** or **View trace by Correlation ID**.  
- **From Background Jobs:** On a job run detail, use **View full trace in Trace Explorer** or **View trace by Correlation ID**.

---

## 3. Search Options

| What you have | What to do |
|---------------|------------|
| **Correlation ID** | Paste it in “Search by ID” and click Search. Or open from Event/Job run detail: “View trace by Correlation ID”. |
| **Event ID** | Paste the GUID in “Search by ID” and click Search. Or use Event Bus Monitor → event → “View full trace in Trace Explorer”. |
| **Job Run ID** | Paste the GUID in “Search by ID” and click Search. Or use Background Jobs → run → “View full trace in Trace Explorer”. |
| **Workflow Job ID** | Paste the GUID in “Search by ID” and click Search. |
| **Entity (e.g. Order)** | Under “Search by Entity”, set Type (e.g. `Order`) and Entity ID (GUID), then “Load timeline”. |

If you enter a GUID, the backend tries Event → Job Run → Workflow Job in that order and returns the first match.

---

## 4. Reading the Timeline

- **Order:** Oldest at the top, newest at the bottom.  
- **Rows:** Each row is one “thing that happened” (e.g. “Workflow completed: Order Pending Provision”, “Event: WorkflowTransitionCompleted”, “Job: Email Ingest — Failed”).  
- **Status badges:** Green = success/processed, red = failed/dead-letter, blue = running/pending.  
- **Expand:** Click a row to see extra detail (CorrelationId, Source, Entity, Handler, link to Event Bus / Trace Explorer).  
- **Links:** Use the link on the right (or in expanded detail) to open that event or job run in Event Bus Monitor or Trace Explorer (by Job Run / Workflow Job ID).

---

## 5. Root-Cause Workflow

1. **Start from where you see the problem**  
   - Failed event in Event Bus Monitor → open that event’s trace (event ID or correlation ID).  
   - Failed job run in Background Jobs → open that run’s trace (job run ID or correlation ID).

2. **Scan the timeline**  
   - See the order: workflow → event → handler/job runs.  
   - Find the first failed or dead-letter step.

3. **Use the detail**  
   - Expand the failing row: error message (Summary/DetailSummary), HandlerName, RelatedId.  
   - Use “Open in Event Bus Monitor” or “Open in Trace Explorer” to drill into that event or run.

4. **Entity-centric view**  
   - If you care about “everything that happened for this Order”, use Search by Entity (e.g. Order + order ID).  
   - Helps when the same entity is touched by multiple workflows or events.

---

## 6. Common Investigation Patterns

| Scenario | Action |
|----------|--------|
| “This event failed; what else was part of the same flow?” | Open trace by Event ID or Correlation ID; look at events and handler runs before/after. |
| “This job run failed; was it triggered by an event?” | Open trace by Job Run ID; check for EventEmitted/EventHandler rows and the same CorrelationId. |
| “What happened to this order in the system?” | Search by Entity (Order, &lt;orderId&gt;); review workflow and event rows. |
| “How many things are broken in the last 24h?” | Check the metrics line at the top of Trace Explorer (failed/dead-letter events and job runs, chains with failures). |

---

## 7. API (for scripts / integrations)

- Base paths: **/api/trace** or **/api/operational-trace**.  
- Endpoints:  
  - `GET .../correlation/{correlationId}` or `.../by-correlation/{correlationId}`  
  - `GET .../event/{eventId}` or `.../by-event/{eventId}`  
  - `GET .../jobrun/{jobRunId}` or `.../by-job-run/{jobRunId}`  
  - `GET .../workflowjob/{workflowJobId}` or `.../by-workflow-job/{workflowJobId}`  
  - `GET .../entity?entityType=...&entityId=...` or `.../by-entity?entityType=...&entityId=...`  
  - `GET .../metrics?fromUtc=...&toUtc=...` (optional; default last 24h)  
- Optional **limit** (e.g. `?limit=500`) to cap timeline size; response includes TotalCount when limit is used.  
- Auth: same as rest of API (e.g. Jobs policy / JobsView permission). Company scoping is applied automatically.

---

## 8. Limitations

- **No “HTTP request started” row** — Requests are not stored; only workflow/event/job data.  
- **Replay/retry** — Manual event replay/retry is not stored as timeline items; only job retries appear (as new JobRun with ParentJobRunId).  
- **Async event handlers** — Their job runs are tied to the event via EventId; the same chain may appear under the event’s correlation or under the background job ID, depending on how the run was created.  
- **Large entity timelines** — Use the **limit** parameter or entity search with awareness that very active entities can have many items.

For more detail on what is and isn’t in the timeline, see **docs/OPERATIONAL_TIMELINE_AUDIT.md** and **docs/OPERATIONAL_TIMELINE_ARCHITECTURE.md**.

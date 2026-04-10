# SLA Intelligence — Architecture

**Date:** 2026-03-09  
**Status:** Implemented (additive; no changes to Workflow Engine or Event Bus behaviour).

---

## 1. Overview

SLA Intelligence adds **automatic breach detection** for workflow transitions, event processing, and background jobs by evaluating existing observability data (WorkflowJob, EventStore, JobRun) against configurable rules. It does **not** modify the Workflow Engine or Event Bus; it only reads timestamps and writes breach records and optional alerts.

---

## 2. Components

| Component | Location | Purpose |
|-----------|----------|---------|
| **SlaRule** | Domain/Sla/Entities | Rule definition: RuleType, TargetType, TargetName, MaxDurationSeconds, WarningThresholdSeconds, EscalationThresholdSeconds, Enabled. Company-scoped. |
| **SlaBreach** | Domain/Sla/Entities | Record of a detected breach/warning/escalation: TargetId, CorrelationId, Severity, Status (Open/Acknowledged/Resolved). Does not duplicate trace data. |
| **SlaEvaluationService** | Application/Sla | Evaluates all enabled rules against WorkflowJob, EventStore, JobRun; creates SlaBreach records; triggers alerts for Critical severity. |
| **ISlaAlertSender** | Application/Sla | Sends alerts (webhook, optional email) when critical breaches are recorded. Payload includes CorrelationId, TargetId, Trace Explorer link. |
| **SlaMonitorController** | Api/Controllers | REST API: GET /api/sla/breaches, GET /api/sla/dashboard, GET/POST/PUT /api/sla/rules, PATCH /api/sla/breaches/{id} for status. |
| **SLA Dashboard UI** | frontend/pages/admin/SlaMonitorPage.tsx | Admin page at /admin/sla-monitor: dashboard cards, breach table, filters, link to Trace Explorer. |

---

## 3. Rule types and evaluation

- **WorkflowTransition:** Compares WorkflowJob duration (CreatedAt → CompletedAt) to rule thresholds. TargetName can be workflow definition ID, EntityType, or "*".
- **EventProcessing:** Compares EventStore duration (CreatedAtUtc → ProcessedAtUtc) to rule thresholds. TargetName is EventType or "*".
- **BackgroundJob:** Compares JobRun duration (StartedAtUtc → CompletedAtUtc or DurationMs) to rule thresholds. TargetName is JobType/JobName or "*".
- **EventChainStall:** Detects events in Pending/Processing older than MaxDurationSeconds; records Critical breach and optional alert.

Severity is derived from thresholds:  
- duration ≥ EscalationThresholdSeconds → **Critical**  
- duration ≥ MaxDurationSeconds → **Breach**  
- duration ≥ WarningThresholdSeconds (if set) → **Warning**

---

## 4. Data flow

1. **Evaluation** runs via background job type `slaevaluation` (or API-triggered). No new instrumentation; it queries existing tables.
2. **SlaEvaluationService.EvaluateAsync** loads enabled SlaRules, queries WorkflowJobs/EventStore/JobRuns (last 24h window), computes durations, and inserts SlaBreach rows when thresholds are exceeded. Duplicate Open breaches for the same (RuleId, TargetType, TargetId) are skipped.
3. For each **Critical** breach, **ISlaAlertSender.SendBreachAlertAsync** is invoked (webhook POST, optional email). Payload includes TraceExplorerLink built from app configuration (SlaAlerts:TraceExplorerBaseUrl).
4. **API** and **UI** read SlaBreaches and SlaRules with company scoping (SuperAdmin sees all; others see their company).

---

## 5. Configuration

- **SlaAlerts** (appsettings):
  - `TraceExplorerBaseUrl`: Base URL of the frontend for Trace Explorer links in alerts (e.g. `https://app.cephasops.com`).
  - `WebhookUrl`: Optional. When set, critical breach payloads are POSTed here as JSON.
  - `EmailEnabled`, `EmailRecipients`: Optional; email alerting can be extended in SlaAlertSender.

- **SLA rules** are stored in `SlaRules`; create/update via API or (in future) admin UI for rules.

---

## 6. Integration with Trace Explorer

- Each breach stores **CorrelationId** and **TargetId** (and TargetType). The UI and alert payloads build a link to Trace Explorer using:
  - `?correlationId=...` when CorrelationId is present, or
  - `?workflowJobId=...` / `?eventId=...` / `?jobRunId=...` from TargetType and TargetId.
- This allows operators to go from a breach directly to the full timeline without duplicating trace data.

---

## 7. Security and scoping

- **API** uses existing Jobs policy (`Authorize(Policy = "Jobs")`) and RequirePermission(JobsView / JobsAdmin). Company filter is applied for non–SuperAdmin users.
- **Rules** and **breaches** are company-scoped; evaluation respects rule CompanyId and only considers data for that company (or all companies when rule CompanyId is null and user is SuperAdmin).

---

## 8. Scheduling evaluation

- Enqueue a background job of type **slaevaluation** (payload optional: `{ "companyId": "..." }`) to run evaluation. No built-in schedule; use your existing scheduler or a cron-triggered job to enqueue periodically (e.g. every 15 minutes).

---

## 9. References

- **Data audit:** `docs/SLA_INTELLIGENCE_AUDIT.md` — timestamps and delays available for SLA.
- **Runbook:** `docs/SLA_MONITOR_RUNBOOK.md` — configuration, investigation flow, alert handling.

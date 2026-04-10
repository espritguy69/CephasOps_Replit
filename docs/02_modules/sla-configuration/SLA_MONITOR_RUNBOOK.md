# SLA Monitor — Runbook

**Date:** 2026-03-09  
**Audience:** Operations, support, and admins.

---

## 1. Purpose

The SLA Monitor provides:

- **Visibility** into operational SLA breaches (workflow transitions, event processing, background jobs, and event chain stalls).
- **Dashboard** with open breaches, critical count, average resolution time, and most common breached targets.
- **Link to Trace Explorer** for each breach to investigate the full correlation chain.
- **Alerting** (webhook, optional email) when critical breaches are detected.

---

## 2. Access

- **URL:** `/admin/sla-monitor` (frontend).
- **Permissions:** Same as Background Jobs / Event Bus — `jobs.view` to see data, `jobs.admin` to create/update rules and acknowledge/resolve breaches.
- **Company scoping:** Non–SuperAdmin users see only their company’s rules and breaches.

---

## 3. SLA rule configuration

Rules define **what** to measure and **how long** is allowed.

- **Rule type:** WorkflowTransition | EventProcessing | BackgroundJob | EventChainStall.
- **Target type:** workflow | event | job.
- **Target name:** Workflow definition ID or EntityType, EventType, or JobType/JobName; use `*` for “all”.
- **MaxDurationSeconds:** Hard limit; exceeding it is a **Breach**.
- **WarningThresholdSeconds** (optional): Below breach; exceeding it creates a **Warning**.
- **EscalationThresholdSeconds** (optional): Above breach; exceeding it creates **Critical** and triggers alerts.

**Example (API):**

```http
POST /api/sla/rules
Content-Type: application/json

{
  "ruleType": "WorkflowTransition",
  "targetType": "workflow",
  "targetName": "Order",
  "maxDurationSeconds": 300,
  "warningThresholdSeconds": 120,
  "escalationThresholdSeconds": 600,
  "enabled": true
}
```

---

## 4. Running evaluation

Evaluation does **not** run by itself; it runs when the **SLA Evaluation** background job runs.

- **Option A:** Enqueue a job of type `slaevaluation` (payload optional: `{ "companyId": "<guid>" }`) via your scheduler or API. The background job processor will run it and call the evaluation service.
- **Option B:** Call the evaluation service from a custom endpoint or scheduled task if you have one.

There is no built-in schedule; configure your scheduler (e.g. every 15–30 minutes) to enqueue `slaevaluation` if you want periodic checks.

---

## 5. Breach investigation flow

1. **Dashboard:** Open `/admin/sla-monitor`. Check **Open breaches** and **Critical** counts and the **Most common breached** list.
2. **Filter:** Use type (workflow/event/job), severity, and status to narrow the breach table.
3. **Trace link:** For each breach, click **Trace** to open Trace Explorer with the correct query (correlation ID, workflow job ID, event ID, or job run ID). Use the timeline to see where the delay or failure occurred.
4. **Lifecycle:** Use **Ack** to acknowledge and **Resolve** when the issue is addressed (requires `jobs.admin`). Resolved breaches are excluded from “open” counts but remain in the list for history.

---

## 6. Alert handling

- **When:** Alerts are sent when a **Critical** breach is recorded (severity = Critical), e.g. event chain stall or duration above EscalationThresholdSeconds.
- **Webhook:** If `SlaAlerts:WebhookUrl` is set, a JSON payload is POSTed with BreachId, CompanyId, Severity, TargetType, TargetId, CorrelationId, Title, DurationSeconds, DetectedAtUtc, and **TraceExplorerLink**. Use this to integrate with Slack, PagerDuty, or internal dashboards.
- **Email:** Optional; configure `SlaAlerts:EmailEnabled` and `EmailRecipients`; the current implementation logs that email would be sent; full email sending can be wired in SlaAlertSender.
- **Trace Explorer link:** Always use the **TraceExplorerLink** in the payload to open the exact trace for that breach.

---

## 7. Integration with Trace Explorer

- From a breach row, **Trace** opens `/admin/trace-explorer?correlationId=...` (or by eventId/workflowJobId/jobRunId).
- From Trace Explorer you can see the full chain: workflow transition, event emit/process, job start/complete. No duplicate data is stored; SLA Monitor only holds breach metadata and the link.

---

## 8. Troubleshooting

| Issue | Action |
|-------|--------|
| No breaches although delays exist | Ensure SLA rules exist and are **Enabled**; ensure evaluation job has run; check rule TargetName matches (e.g. EntityType "Order", or "*"). |
| Too many breaches | Tighten MaxDurationSeconds or narrow TargetName; or disable the rule temporarily. |
| Alerts not firing | Check SlaAlerts:WebhookUrl (and optional email settings); confirm breaches are **Critical** (e.g. EscalationThresholdSeconds set and exceeded). |
| Company filter too strict | SuperAdmin sees all companies; others only their CompanyId. Create rules with the intended CompanyId. |

---

## 9. References

- **Architecture:** `docs/SLA_INTELLIGENCE_ARCHITECTURE.md`
- **Data audit (timestamps and delays):** `docs/SLA_INTELLIGENCE_AUDIT.md`

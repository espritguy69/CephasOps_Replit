# Production Monitoring Alerts

Define alert rules so critical and warning conditions trigger notifications. Rules are platform-agnostic (Prometheus Alertmanager, Azure Monitor, PagerDuty, etc.); adapt expressions and thresholds to your observability stack.

---

## Critical Alerts

These should page or escalate immediately.

| Alert | Condition | Source / expression idea | Action |
|-------|-----------|---------------------------|--------|
| **Database unreachable** | Database health check Unhealthy | `/health/ready` or `/health` entry `database` status ≠ Healthy | Check PostgreSQL availability, connection string, network; failover if HA. |
| **Guardian stopped** | Guardian disabled in Production when it should run | `/health/platform` entry `guardian` status Degraded (e.g. “Guardian is disabled”) on a node that should run Guardian | Verify `ProductionRoles:RunGuardian` and `PlatformGuardian:Enabled`; restart or fix config. |
| **Job backlog critical** | Job queue dead-letter above threshold | `/health/platform` entry `jobbacklog` Unhealthy or `data.deadLetterCount` ≥ configured unhealthy threshold | Investigate dead-letter jobs; fix root cause (e.g. bad payload, missing dependency); consider replay. |
| **Critical tenant anomaly** | One or more critical anomalies in platform health | Platform health API / anomaly API: critical anomaly count > 0 | Review anomalies; contact tenant if needed; follow [INCIDENT_RESPONSE.md](INCIDENT_RESPONSE.md#tenant-data-incident). |
| **Event bus unhealthy** | Dispatcher not running or dead-letter above unhealthy threshold | `/health/platform` entry `eventbus` Unhealthy | Check event store dispatcher and DB; clear or replay dead-letter as per runbook. |
| **Redis down** (when used) | Redis health check Unhealthy | `/health/ready` or `/health/platform` entry `redis` Unhealthy | Check Redis server and network; rate limiting may fail open. |

---

## Warning Alerts

These should notify the team but not necessarily page.

| Alert | Condition | Source / expression idea | Action |
|-------|-----------|---------------------------|--------|
| **API latency spike** | Request duration p95 above threshold (e.g. 2s) | Prometheus: `http_server_request_duration_seconds` p95 > 2 | Check load, DB/Redis latency, slow endpoints; scale or optimize. |
| **Tenant storage spike** | Storage growth or usage above policy threshold | Tenant metrics / storage API or platform health storage warnings | Review tenant usage; consider notifications or quota. |
| **Rate-limit abuse** | High rate of rate-limit hits for a tenant or globally | Logs or metrics (rate-limit rejections); platform health when `RateLimitBreachCountLast24h` is exposed | Investigate tenant or integration; adjust limits or throttle. |
| **Worker crash loop** | Worker process or pod restarts repeatedly | Orchestrator (Kubernetes restart count, Docker restart policy) or process manager | Check logs and health; fix config or dependency (DB/Redis). |
| **Job backlog degraded** | Pending or dead-letter above degraded threshold | `/health/platform` entry `jobbacklog` Degraded | Monitor; consider scaling workers or investigating slow jobs. |
| **Event bus degraded** | Pending or dead-letter above degraded threshold | `/health/platform` entry `eventbus` Degraded | Monitor; consider increasing dispatcher throughput or fixing failing handlers. |

---

## Thresholds (reference)

- **Job backlog:** Configurable in `HealthChecks:JobBacklog` (e.g. `DeadLetterUnhealthyThreshold`, `PendingDegradedThreshold`). Use the same values in alert definitions where possible.
- **Event bus:** From `EventBusDispatcherOptions` (e.g. `PendingCountDegradedThreshold`, `DeadLetterUnhealthyThreshold`). Align alerts with these.
- **API latency:** Define p95/p99 and window (e.g. 5m) in your metrics backend.

## See also

- [HEALTH_CHECKS.md](HEALTH_CHECKS.md) — Health check semantics.
- [INCIDENT_RESPONSE.md](INCIDENT_RESPONSE.md) — How to respond to each type of incident.

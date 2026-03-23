# Operational Dashboards

Define dashboards for operations teams so platform and tenant health are visible at a glance. Metrics and health data come from the API health endpoints, Prometheus/OpenTelemetry, and (where available) platform analytics APIs.

## Data Sources

- **GET /health** and **GET /health/platform** — JSON with status and per-check data (e.g. `pendingCount`, `deadLetterCount`).
- **GET /metrics** — Prometheus scrape endpoint (when OpenTelemetry metrics are enabled).
- **Platform analytics APIs** — e.g. tenant health, anomaly counts, platform health summary (see existing platform/analytics controllers).

---

## 1. Platform Overview

**Purpose:** Single-screen view of platform health and load.

| Panel | Source | Notes |
|-------|--------|--------|
| **Active tenants** | Platform analytics / tenant health API | Count of tenants in “active” or healthy state |
| **Warning tenants** | Same | Tenants in Warning health status |
| **Critical anomalies** | Anomaly API / platform health | Count of critical tenant anomalies (e.g. last 24h) |
| **Job queue size** | `/health/platform` → `jobbacklog` entry `data.pendingCount` (and optionally `runningCount`) | Trend and current value |
| **API latency** | Prometheus: `http_server_request_duration_*` (ASP.NET Core instrumentation) | p50/p95/p99 by route or overall |
| **Event bus backlog** | `/health/platform` → `eventbus` entry `data.pendingCount` | Pending event store events |
| **Database / Redis / Guardian** | `/health/platform` entries | Status indicators (Healthy/Degraded/Unhealthy) |

---

## 2. Tenant Health

**Purpose:** Per-tenant and aggregate tenant view for support and success.

| Panel | Source | Notes |
|-------|--------|--------|
| **Tenant anomaly events** | Platform Guardian / anomaly API | List or count of anomalies by tenant and severity |
| **Rate-limit spikes** | Logs or metrics (if instrumented); platform health `RateLimitBreachCountLast24h` when available | Tenants hitting rate limits |
| **Storage growth** | Tenant metrics / storage APIs | Per-tenant or aggregate storage trend |
| **Job failures** | Platform health `FailedJobsLast24h`; JobRun/JobExecution APIs | Failed jobs per tenant or globally |

---

## 3. Infrastructure

**Purpose:** API and worker runtime health.

| Panel | Source | Notes |
|-------|--------|--------|
| **API response time** | Prometheus: ASP.NET Core request duration | By endpoint or service |
| **Worker CPU usage** | Host/container metrics (e.g. cAdvisor, node exporter) | For worker nodes |
| **Database connections** | PostgreSQL stats or connection pool metrics (if exposed) | Active connections, pool usage |
| **Slow queries** | PostgreSQL `pg_stat_statements` or app-level metrics (if added) | Count or top N slow queries |
| **Redis latency** | Redis INFO or custom metric (if exposed) | When Redis is used |

---

## Implementation Notes

- **Grafana:** Use Prometheus as a data source; add a JSON/HTTP data source or a small sidecar that periodically calls `/health/platform` and exposes key fields as metrics if you need them in Prometheus.
- **Alerting:** Base critical/warning alerts on the same metrics and health checks (see [ALERTING_RULES.md](ALERTING_RULES.md)).
- **Platform Guardian:** Use existing platform health and drift APIs to populate “Guardian status” and “drift/anomaly” panels.

## See also

- [HEALTH_CHECKS.md](HEALTH_CHECKS.md) — Health endpoint semantics and tags.
- [ALERTING_RULES.md](ALERTING_RULES.md) — Alert rules that complement these dashboards.

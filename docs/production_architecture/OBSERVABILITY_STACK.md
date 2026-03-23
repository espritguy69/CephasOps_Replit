# Observability Stack Integration

**Purpose:** Production observability for CephasOps—logging, metrics, tracing, tenant labeling, alerts, and dashboards.

---

## 1. Logging strategy

- **Current:** Serilog to console and file (logs/cephasops-.log, 7-day retention); Enrich.FromLogContext(); RequestLogContextMiddleware pushes CompanyId/TenantId into LogContext.
- **Production recommendation:**
  - **Structured logs** (JSON) to stdout; container/runtime captures and forwards to centralized aggregation (e.g. Loki, Elasticsearch, cloud log service).
  - **Retain** CompanyId and TenantId in every log event for tenant-scoped requests.
  - **Sensitive data:** Do not log passwords, tokens, or full PII in structured fields; redact or omit.
  - **Levels:** Info for normal operations; Warning for rate limit, validation, retries; Error for exceptions and failed jobs.
- **Data retention:** 30–90 days for search; archive or drop beyond that per policy.

---

## 2. Metrics strategy

- **Application metrics to expose (recommended):**
  - **Per-tenant (labeled):** API request count by tenant (or company), job execution count by tenant, job failure count by tenant, rate limit exceedance count by tenant.
  - **Platform:** Total active tenants, pending job count, stuck job reset count, Guardian anomaly count, platform health summary (warning/critical tenant count).
  - **Background jobs:** Job executions per type, failure rate, execution duration histogram.
  - **Event bus:** Dispatcher lag, processed count, failure count.
- **Instrumentation:** OpenTelemetry metrics, or Prometheus-style counters/gauges. Use **tenant_id** (or **company_id**) as a label only when cardinality is acceptable; avoid high-cardinality labels (e.g. user_id on every metric).
- **Export:** Prometheus scrape endpoint (e.g. /metrics) or OTLP exporter. Do not expose metrics endpoint publicly; use network policy or auth.

---

## 3. Tracing strategy

- **Use case:** Slow request paths, slow job execution, cross-service calls (if any). Optional for initial production.
- **Recommendation:** OpenTelemetry tracing with sampling (e.g. 10% or tail-based for errors). Propagate trace id in logs and responses (e.g. X-Correlation-ID).
- **Tenant:** Add tenant_id/company_id as span attribute when available so traces can be filtered by tenant.
- **Export:** OTLP to backend (Jaeger, Tempo, or cloud). Not required for MVP if cost-sensitive.

---

## 4. Tenant labeling guidance

- **Logs:** Include `TenantId` and `CompanyId` in LogContext for request-scoped logs; include in job execution logs.
- **Metrics:** Use label `tenant_id` or `company_id` for tenant-scoped metrics; keep cardinality bounded (one label value per tenant). Do not use tenant id as metric name.
- **Traces:** Add attribute `tenant.id` or `company.id` to spans when in tenant context.
- **Alerting:** Alerts can filter by tenant (e.g. “tenant X has > N failures”) or aggregate (“any tenant in Critical”).

---

## 5. Alert categories

| Category | Example | Severity |
|----------|---------|----------|
| **API availability** | Error rate > 5%, 5xx spike | Critical |
| **Latency** | p99 > threshold | Warning |
| **Job queue** | Pending job count > 500, backlog growth | Warning |
| **Job failures** | Failure rate per tenant or global spike | Warning / Critical |
| **Rate limit** | High 429 rate for a tenant (abuse or misconfiguration) | Info / Warning |
| **Guardian** | Critical drift, critical anomaly count > 0 | Warning |
| **Database** | Connection failures, replication lag | Critical |
| **Storage** | Disk or quota near full | Warning |

---

## 6. Dashboard recommendations

- **Platform ops:** Platform health (GET /api/platform/analytics/platform-health), tenant count, job queue depth, Guardian drift/anomaly summary, rate limit breach count.
- **Tenant health:** Tenant health list (GET /api/platform/analytics/tenant-health), anomalies (GET /api/platform/analytics/anomalies), performance health.
- **Infrastructure:** API request rate, error rate, latency (p50, p95, p99), DB connections, worker process health.
- **Jobs:** Pending vs running, failures by type, execution duration by job type.
- **Grafana:** Import or build dashboards from Prometheus metrics and log-derived metrics; use tenant_id filter where applicable.

---

## 7. Data retention guidance

| Data | Retention (suggested) |
|------|------------------------|
| Application logs | 30–90 days searchable; archive or drop after |
| Metrics (Prometheus) | 15–30 days raw; longer if downsampled |
| Traces | 7–14 days |
| TenantAnomalyEvents | 90 days (or per policy) |
| TenantMetricsDaily/Monthly | 13+ months for billing/reporting |
| Job execution history | Per table retention; e.g. 90 days for completed/failed |

---

## 8. Low-risk instrumentation additions

- **Structured logging:** Already present (Serilog, LogContext). Ensure all new code uses structured parameters (e.g. `{CompanyId}`, `{JobType}`).
- **Metrics hooks:** Add optional counters for: job_executions_total (labels: type, status, tenant_id optional), tenant_rate_limit_exceeded_total (label: company_id or tenant_id), guardian_anomalies_total (label: severity). Implement via OpenTelemetry or Prometheus.AspNetCore when adopted; no vendor lock-in required for this doc.
- **Health endpoint:** Existing /health/ready with database and event bus checks; use for readiness probe.

---

## 9. References

- [ENVIRONMENT_CONFIGURATION.md](ENVIRONMENT_CONFIGURATION.md) – Config for log level, metrics export, tracing.
- [PRODUCTION_RUNBOOKS.md](PRODUCTION_RUNBOOKS.md) – Incident response and checks.

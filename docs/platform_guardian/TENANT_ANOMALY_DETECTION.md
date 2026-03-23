# Tenant Anomaly Detection

**Purpose:** Platform Guardian service that evaluates per-tenant metrics and persists anomaly events for diagnostics and reporting.

---

## Service

**TenantAnomalyDetectionService** (ITenantAnomalyDetectionService)

- **RunDetectionAsync:** Runs under platform bypass; for each active tenant compares last 24h metrics to baseline (e.g. 7-day average or prior period). When thresholds are exceeded, persists **TenantAnomalyEvent** (Kind, Severity, Details). Deduplicates by not adding the same Kind for the same tenant within the last hour.
- **GetAnomaliesAsync:** Returns recent TenantAnomalyDto (since, tenantId, severity optional, take).

---

## Anomaly kinds and severity

| Kind | Trigger | Severity |
|------|---------|----------|
| **ApiSpike** | API calls last 24h ≥ multiple of 7-day average (Warning/Critical from options) | Warning, Critical |
| **StorageSpike** | Storage growth (current vs 7 days ago) ≥ fraction (Warning/Critical) | Warning, Critical |
| **JobFailureSpike** | Job failures last 24h ≥ threshold (Warning/Critical) | Warning, Critical |

Additional kinds (e.g. RateLimitBreach, ImpersonationEvent, FailedAuthSpike, ExportSpike) can be added when data sources are available (e.g. structured logs or counters).

---

## Severity levels

- **Info** – Informational only.
- **Warning** – Needs review; may indicate abuse or misconfiguration.
- **Critical** – High risk; immediate review recommended.

---

## Persistence

**TenantAnomalyEvents** table (platform-scoped; no tenant query filter):

- Id, TenantId, Kind, Severity, OccurredAtUtc, Details, ResolvedAtUtc.
- Indexes: TenantId, (TenantId, OccurredAtUtc), (Severity, OccurredAtUtc).

---

## Configuration: PlatformGuardian:AnomalyDetection

| Option | Default | Description |
|--------|---------|-------------|
| Enabled | true | Enable detection runs. |
| ApiSpikeWarningMultiple | 2.0 | API 24h ≥ this × 7d avg → Warning. |
| ApiSpikeCriticalMultiple | 5.0 | API 24h ≥ this × 7d avg → Critical. |
| JobFailureSpikeWarning | 10 | Job failures 24h ≥ this → Warning. |
| JobFailureSpikeCritical | 50 | Job failures 24h ≥ this → Critical. |
| StorageGrowthWarningFraction | 0.2 | Storage growth ≥ 20% → Warning. |
| StorageGrowthCriticalFraction | 0.5 | Storage growth ≥ 50% → Critical. |
| MaxEventsPerTenantPerRun | 10 | Cap events per tenant per run. |

---

## Endpoint

**GET /api/platform/analytics/anomalies**

- **Auth:** SuperAdmin, AdminTenantsView.
- **Query:** sinceUtc (default 7 days ago), tenantId, severity, take (default 500).
- **Returns:** Array of TenantAnomalyDto.

---

## Safety

- All reads/writes run under **TenantScopeExecutor.RunWithPlatformBypassAsync** (platform-only).
- No tenant context override; anomaly events are stored for platform operators only.

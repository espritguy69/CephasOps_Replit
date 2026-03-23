# Guardian Scheduling

**Purpose:** Scheduled execution of Platform Guardian checks (anomaly, drift, performance) without overloading the database or failing runtime.

---

## Hosted service

**PlatformGuardianHostedService**

- Runs in the background on a configurable interval (default 60 minutes; minimum 5).
- Each cycle:
  - **Anomaly detection:** Runs ITenantAnomalyDetectionService.RunDetectionAsync (when enabled). Persists TenantAnomalyEvents.
  - **Drift detection:** Runs IPlatformDriftDetectionService.DetectAsync (when enabled). Logs if critical drift found; does not write files unless ReportPath is implemented.
  - **Performance watchdog:** Calls IPerformanceWatchdogService.GetPerformanceHealthAsync (when enabled). Logs if tenants impacted or pending jobs high.
- Exceptions in a step are caught and logged; the service does not stop. Runtime is not failed because of drift or anomalies.

---

## Configuration: PlatformGuardian

| Option | Default | Description |
|--------|---------|-------------|
| Enabled | true | Enable the scheduled Guardian. |
| RunIntervalMinutes | 60 | Minutes between cycles. Minimum 5. |
| RunAnomalyDetection | true | Run anomaly detection in each cycle. |
| RunDriftDetection | true | Run drift detection in each cycle. |
| RunPerformanceWatchdog | true | Run performance watchdog in each cycle. |

---

## Production safety

- **Database load:** Anomaly detection queries TenantMetricsDaily and JobExecutions per tenant; drift is config-only; performance health uses tenant health and job count. Interval of 15–60 minutes is typically acceptable. Use RunIntervalMinutes >= 15 for high-tenant counts if needed.
- **No runtime failure:** Guardian never throws to the host; exceptions are logged and the loop continues.
- **Scoped services:** Each cycle uses a new scope; no long-lived DbContext.

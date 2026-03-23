# Platform Drift Report

**Purpose:** Platform Guardian compares current configuration and runtime values against an expected baseline for operational SaaS settings. Drift is reported, not enforced; runtime does not fail solely because drift exists.

---

## Areas checked

| Area | Section | Examples |
|------|---------|----------|
| Job orchestration | JobOrchestration:Worker | BatchSize, TenantJobFairnessEnabled, LeaseSeconds |
| Tenant rate limit | SaaS:TenantRateLimit | Enabled, RequestsPerMinute |
| Storage lifecycle | SaaS:StorageLifecycle | Enabled, ArchiveAfterDays |
| Platform Guardian | PlatformGuardian | Enabled, RunIntervalMinutes |

---

## Classification

| Classification | Meaning |
|----------------|--------|
| **Informational** | Acceptable drift; no action required (e.g. high limits for capacity). |
| **Warning** | Should be reviewed; may indicate misconfiguration or reduced safety (e.g. tenant fairness disabled, rate limit disabled). |
| **Critical** | Critical misconfiguration (e.g. lease too short leading to excessive stuck resets). |

---

## Service

**PlatformDriftDetectionService** (IPlatformDriftDetectionService)

- **DetectAsync:** Reads current config (IConfiguration), compares to embedded baseline rules, returns **PlatformDriftResultDto** (GeneratedAtUtc, Items, counts by classification). Does not throw.

---

## Endpoint

**GET /api/platform/analytics/drift**

- **Auth:** SuperAdmin, AdminTenantsView.
- **Returns:** PlatformDriftResultDto (list of drift items with Section, Key, Expected, Actual, Classification, Message).

---

## Machine-readable artifact

When **PlatformGuardian:DriftDetection:ReportPath** is set, the scheduled Guardian can write the report to that path (e.g. `tools/architecture/platform_guardian_report.json`). Schema: same as API response (GeneratedAtUtc, Items[], InformationalCount, WarningCount, CriticalCount).

---

## Safety

- Detection is read-only (config read). No changes to application behavior based on drift unless you implement additional automation.
- Do not fail runtime solely because drift exists; this is a detection/reporting layer.

# Environment and Configuration Architecture

**Purpose:** Standardize environment groups and config areas for local, dev, staging, and production; secrets vs non-secrets; validation expectations.

---

## 1. Environment groups

| Environment | Use | Typical config |
|-------------|-----|----------------|
| **Local** | Developer machine; optional seed data | appsettings.Development.json; ConnectionStrings:DefaultConnection; no strict validation. |
| **Dev** | Shared dev deployment | Similar to local; may use shared DB; optional stricter defaults. |
| **Staging** | Pre-production; mirrors production topology | Production-like; may use smaller scale; secrets from vault or env. |
| **Production** | Live tenants | Full HA; secrets from vault; startup validation for critical settings. |

---

## 2. Important config areas

| Area | Key paths | Secret? | Notes |
|------|-----------|---------|--------|
| **Database** | ConnectionStrings:DefaultConnection | Yes | Required in all envs. |
| **JWT / Auth** | Jwt:SecretKey, Jwt:Issuer, Jwt:Audience | Yes | Required for API. |
| **Job orchestration** | JobOrchestration:Worker (BatchSize, LeaseSeconds, MaxJobsPerTenantPerCycle, TenantJobFairnessEnabled) | No | Production: tenant fairness on; lease ≥ 60. |
| **Tenant rate limit** | SaaS:TenantRateLimit (Enabled, RequestsPerMinute, RequestsPerHour, Plans) | No | Production: Enabled true. |
| **Storage lifecycle** | SaaS:StorageLifecycle (Enabled, Interval, WarmAfterDays, ColdAfterDays, ArchiveAfterDays) | No | Production: sensible days. |
| **Guardian** | PlatformGuardian (Enabled, RunIntervalMinutes), PlatformGuardian:AnomalyDetection, DriftDetection | No | Production: Enabled true; interval ≥ 15. |
| **Production roles** | ProductionRoles (RunJobWorkers, RunGuardian, RunStorageLifecycle, RunMetricsAggregation, RunWatchdog, RunSchedulers, RunEventDispatcher, RunNotificationWorkers, RunIntegrationWorkers, RunEmailCleanup) | No | Set per node type (API vs worker). |
| **Signup** | Signup/self-service config | No | Trial days, allowlist if any. |
| **Subscriptions** | Billing plan slugs, defaults | No | Trial plan slug, limits. |
| **Billing provider** | External payment provider keys/URLs | Yes | If integrated. |
| **Support / impersonation** | Token TTL, audit | No | SuperAdmin only. |
| **Cache / Redis** | Redis connection (optional) | Yes (if used) | For distributed rate limit or cache. |
| **Observability** | Log level, OpenTelemetry/OTLP endpoint, Prometheus | No (endpoint may be internal) | Production: structured logs; metrics export. |
| **CORS** | Cors:AllowedOrigins | No | Production: specific origins. |

---

## 3. Required settings by environment

- **All:** ConnectionStrings:DefaultConnection, Jwt (SecretKey, Issuer, Audience).
- **Production:** In addition: CORS restricted; rate limit enabled; Guardian enabled (or explicitly disabled with reason); ProductionRoles set per node; no default/dev passwords.
- **Staging:** Same as production for critical paths; may relax Guardian interval or job batch size for cost.

---

## 4. Secret vs non-secret

- **Secrets:** Connection strings, JWT secret, Redis password, billing API keys, external webhook secrets. Store in environment variables, secret manager, or vault; never commit.
- **Non-secret:** Feature flags, limits, intervals, URLs (internal), log level. Can live in appsettings or config service; override by env where needed.

---

## 5. Safe defaults

- **JobOrchestration:Worker:TenantJobFairnessEnabled:** true. **LeaseSeconds:** ≥ 60.
- **SaaS:TenantRateLimit:Enabled:** true.
- **PlatformGuardian:Enabled:** true; **RunIntervalMinutes:** 60 (min 5).
- **ProductionRoles:** All true when unset (all-in-one); set explicitly for API-only or worker-only nodes.

---

## 6. Production-only settings

- **Strict CORS** (no wildcard in production).
- **Log level** at least Information; avoid Debug in production.
- **Startup validation** (see below): fail fast if critical production config is missing when ASPNETCORE_ENVIRONMENT=Production.

---

## 7. Validation expectations

- **Local/Dev:** Missing ConnectionStrings or Jwt can fail at first request; acceptable for dev.
- **Production:** Validate at startup: ConnectionStrings:DefaultConnection present; Jwt:SecretKey present and length sufficient; if Production roles are used, at least one of RunApi (implicit) or RunJobWorkers true so the process does something. Optional: validate CORS is not wildcard.
- **Config drift:** Use Platform Guardian drift detection (GET /api/platform/analytics/drift) to compare runtime config to baseline; act on Critical classification.

---

## 8. Startup validation (production)

Add a minimal startup check that runs when `ASPNETCORE_ENVIRONMENT == "Production"`:

- **Required:** ConnectionStrings:DefaultConnection not null/empty. Jwt:SecretKey not null/empty and length ≥ 16 (or your policy).
- **On failure:** Log error and exit with non-zero code so the host does not start with invalid config.
- **Do not** require optional settings (e.g. Redis); only block on clearly invalid production state.

Implementation: optional `ProductionStartupValidator` that runs in `WebApplication` after Build(), before Run(); or a hosted service that runs once and exits the host on failure. See optional implementation below.

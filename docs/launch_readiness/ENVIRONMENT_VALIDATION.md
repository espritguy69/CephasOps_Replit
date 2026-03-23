# Production Environment Validation

CephasOps runs **automated startup checks** when `ASPNETCORE_ENVIRONMENT=Production`. These ensure the application does not start with missing or invalid configuration and that critical dependencies are reachable.

## What Is Validated

### 1. Database connectivity

- **Config:** `ConnectionStrings:DefaultConnection` must be set and non-empty in Production.
- **Connectivity:** After the app is built, a one-off health check runs for the **database** check. If it fails (e.g. PostgreSQL unreachable), the process throws and exits. This prevents the app from serving traffic when the database is down.

### 2. Redis connectivity (when used)

- **Config:** If `ConnectionStrings:Redis` is set, it cannot be empty.
- **Connectivity:** When Redis is configured, a **redis** health check is registered. The same startup connectivity run executes it in Production. If Redis is unreachable, the process throws and exits so that rate limiting and any cache that depend on Redis are not used in a broken state.

### 3. Required secrets

- **JWT:** Either `Jwt:SecretKey` or `Jwt:Key` must be set in Production and at least **16 characters**. This ensures token signing is not using a default or weak secret.

### 4. Rate-limit config

- **Section:** `SaaS:TenantRateLimit`. If present, `RequestsPerMinute` and `RequestsPerHour` (when explicitly set) must be greater than zero.
- **Purpose:** Prevents misconfiguration that would disable or break per-tenant rate limiting.

### 5. Guardian config

- **Section:** `PlatformGuardian` (optional but recommended in Production).
- **Behaviour:** Not enforced at startup; the **Guardian health check** reports Degraded in Production if Guardian is disabled. Ensure `PlatformGuardian:Enabled` is `true` and `RunIntervalMinutes` is set (e.g. 60) on nodes that should run Guardian.

### 6. Worker role config

- **Section:** `ProductionRoles` (optional but recommended).
- **Behaviour:** Not enforced at startup. Use it to run API-only vs worker-only nodes (e.g. `RunJobWorkers`, `RunGuardian`, `RunSchedulers`). Document the intended roles per environment so orchestration (Docker/Kubernetes) sets the correct flags.

### 7. JWT config

- **Keys:** `Jwt:SecretKey` or `Jwt:Key` (see above). Optionally `Jwt:Issuer` and `Jwt:Audience` for strict validation.
- **Purpose:** Ensures authentication is configured and not using defaults in Production.

### 8. Syncfusion license (Production)

- **Config:** `SYNCFUSION_LICENSE_KEY` environment variable (or equivalent from your secure store).
- **Behaviour:** The application has an in-code fallback key for **development only**. In Production, set `SYNCFUSION_LICENSE_KEY` from a secure store (e.g. environment, Key Vault, or secrets manager) so the fallback is never used. Production use of the fallback is not validated at startup and is not recommended (license and compliance).

## Implementation Details

- **ProductionStartupValidator** (sync): Runs immediately after `app.Build()`. Validates config only; throws `InvalidOperationException` if any Production requirement fails.
- **StartupConnectivityValidator** (async): Runs after config validation. Executes health checks for **database** and **redis** (when registered). In Production, if any of these checks report Unhealthy, throws so the host exits before serving requests.

## Configuration Reference

| Item | Config path | Required in Production |
|------|-------------|-------------------------|
| Database | `ConnectionStrings:DefaultConnection` | Yes |
| Redis | `ConnectionStrings:Redis` | No (optional; validated when set) |
| JWT secret | `Jwt:SecretKey` or `Jwt:Key` | Yes (min 16 chars) |
| Rate limit | `SaaS:TenantRateLimit:*` | Validated when section exists |
| Guardian | `PlatformGuardian:*` | Recommended (health check reports if disabled) |
| Worker roles | `ProductionRoles:*` | Recommended (per-node role flags) |
| Syncfusion | `SYNCFUSION_LICENSE_KEY` (env) | Recommended in Production (do not rely on in-code fallback) |

## Non-Production

When `ASPNETCORE_ENVIRONMENT` is not `Production`, config validation and startup connectivity checks are **skipped**. Development and Testing can run with minimal or default config.

## See also

- [HEALTH_CHECKS.md](HEALTH_CHECKS.md) — Health endpoints and checks used at runtime.
- [GO_LIVE_CHECKLIST.md](GO_LIVE_CHECKLIST.md) — Pre-launch verification including env and secrets.

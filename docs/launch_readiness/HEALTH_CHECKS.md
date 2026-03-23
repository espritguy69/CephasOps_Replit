# Health Check Endpoints

CephasOps exposes three health endpoints for liveness, readiness, and platform operations. Use them for load balancers, orchestrators (Docker/Kubernetes), and operational dashboards.

## Endpoints

| Endpoint | Purpose | Use case |
|----------|---------|----------|
| **GET /health** | All registered health checks | Overall status; debugging |
| **GET /health/ready** | Checks tagged `ready` | Readiness probe (Kubernetes/Docker); “can this instance accept traffic?” |
| **GET /health/platform** | Checks tagged `platform` | Operations view: DB, event bus, Redis, Guardian, job queue |

All return JSON with `status` (`Healthy`, `Degraded`, `Unhealthy`), `totalDuration`, and an `entries` object keyed by check name, each with `status`, `description`, and optional `data`.

## Checks Included

### Database (`database`)

- **Tags:** `ready`, `database`, `platform`, `startup`
- **What it does:** Ensures the application can connect to PostgreSQL (`ApplicationDbContext.Database.CanConnectAsync`).
- **Healthy:** Connection OK. **Unhealthy:** Connection failed (e.g. DB down or wrong connection string).

### Event Bus (`eventbus`)

- **Tags:** `ready`, `eventbus`, `platform`
- **What it does:** Verifies the Event Store dispatcher is running and backlog/dead-letter are within thresholds (see `EventBusDispatcherOptions`).
- **Healthy:** Dispatcher running, pending and dead-letter below thresholds. **Degraded:** Pending or dead-letter above degraded threshold; expired leases. **Unhealthy:** Dispatcher not running or dead-letter above unhealthy threshold.

### Redis (`redis`)

- **Tags:** `ready`, `platform`, `cache`, `startup`
- **Registered only when** `ConnectionStrings:Redis` is set.
- **What it does:** Pings Redis (rate-limit store / cache). **Healthy:** Ping OK. **Unhealthy:** Connection or ping failed.

### Guardian (`guardian`)

- **Tags:** `platform`
- **What it does:** Verifies Platform Guardian configuration: `ProductionRoles:RunGuardian` and `PlatformGuardian:Enabled`. Does not verify “last run” timing.
- **Healthy:** Guardian enabled and configured. **Degraded:** In Production, Guardian disabled by role or config (instance may be API-only or intentionally disabled).

### Job backlog (`jobbacklog`)

- **Tags:** `platform`
- **What it does:** Uses `IJobExecutionQueryService` to get pending, running, failed-retry, and dead-letter counts. Compares to configurable thresholds.
- **Config:** `HealthChecks:JobBacklog` — `PendingDegradedThreshold` (default 1000), `DeadLetterUnhealthyThreshold` (default 100), `DeadLetterDegradedThreshold` (default 10).
- **Healthy:** Pending and dead-letter below thresholds. **Degraded:** Pending or dead-letter above degraded threshold. **Unhealthy:** Dead-letter above unhealthy threshold.

## Tag Summary

- **ready:** Database, Event Bus, Redis (if configured). Use for **readiness** (e.g. `/health/ready`).
- **platform:** Database, Event Bus, Redis, Guardian, Job backlog. Use for **platform/operations** view (`/health/platform`).
- **startup:** Database, Redis. Used internally for Production startup connectivity validation; not exposed as a separate endpoint.

## Example: Readiness probe

```yaml
# Kubernetes
readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 10
  timeoutSeconds: 5
```

## Example: Platform status

```bash
curl -s http://localhost:5000/health/platform | jq .
```

Use the `entries` object to drive operations dashboards (e.g. job backlog, event bus pending, Guardian status).

## See also

- [ENVIRONMENT_VALIDATION.md](ENVIRONMENT_VALIDATION.md) — Startup validation (database/Redis connectivity in Production).
- [OPERATIONS_DASHBOARDS.md](OPERATIONS_DASHBOARDS.md) — Suggested dashboards that consume health and metrics.

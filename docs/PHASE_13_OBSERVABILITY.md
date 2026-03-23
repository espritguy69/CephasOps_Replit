# Phase 13: Observability Platform

## Overview

Phase 13 adds health checks and an observability API endpoint. Correlation ID and trace explorer (Phase 8, 9) remain the primary tracing story.

## Implemented

### Health checks
- **Database**: `DatabaseHealthCheck` uses `ApplicationDbContext.CanConnectAsync()`. Tag: `ready`, `database`.
- **Event Bus**: Existing `EventBusHealthCheck` (tags: `ready`, `eventbus`).
- **Endpoint**: `GET /health` returns aggregated status (unchanged).

### API
- **GET /api/observability/diagnostics**: Returns HealthUrl (`/health`) and a short message. Requires `JobsView`. Use `/health` for full health status.

### Correlation and tracing (existing)
- Every request gets a correlation ID via `CorrelationIdMiddleware` (header `X-Correlation-Id`).
- Trace Explorer: `GET /api/trace/correlation/{id}`, EventStore lineage, JobRuns by correlation.
- Problem details include `correlationId` for errors.

## Usage

- **Liveness/readiness**: Call `GET /health`; use tags if you add a custom UI (e.g. `?tags=ready`).
- **Diagnostics**: Call `GET /api/observability/diagnostics` (authenticated, JobsView) for a pointer to health and correlation.
- **Metrics/alerting**: No new metrics aggregation or alerting sink in Phase 13; add when needed (e.g. Prometheus, Application Insights).

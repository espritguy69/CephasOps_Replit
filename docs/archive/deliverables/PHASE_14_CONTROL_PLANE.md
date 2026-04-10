# Phase 14: Operational Control Plane

## Overview

Phase 14 documents and exposes a single index endpoint for operator APIs. Existing controllers already provide event inspection, replay, command/job orchestration, integration management, and trace explorer.

## Implemented

### Control plane index
- **GET /api/admin/control-plane**: Returns a list of capability groups with base path and short description. Requires Admin/SuperAdmin and JobsView.
- Capabilities listed: Event store, Event replay, Rebuild, Command orchestration, Background jobs, Job orchestration, System workers, Integration, Trace, Operational trace, Observability, Tenants, Billing plans, Tenant subscriptions.

### Existing operator APIs (unchanged)
- **Event store**: `/api/event-store` — events, lineage, ledger.
- **Replay**: `/api/event-store/replay` — replay operations.
- **Rebuild**: `/api/event-store/rebuild` — rebuild operations.
- **Commands**: `/api/command-orchestration` — command diagnostics, processing log.
- **Background jobs**: `/api/background-jobs` — job runs, filters, retry.
- **Job orchestration**: `/api/job-orchestration` — definitions, execution.
- **System workers**: `/api/system/workers` — worker instances.
- **Integration**: `/api/integration` — connectors, outbound deliveries, inbound receipts, replay.
- **Trace**: `/api/trace`, `/api/operational-trace` — trace by correlation/event/job.
- **Observability**: `/api/observability`, `/health`.
- **Tenants**: `/api/tenants`.
- **Billing**: `/api/billing/plans`, `/api/billing/subscriptions`.

## Usage

- Call **GET /api/admin/control-plane** to discover operator endpoints and build dashboards or runbooks.
- All listed endpoints require appropriate permissions (JobsView, JobsAdmin, etc.) and company scoping for non–super-admin.

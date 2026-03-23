# Tenant Activity Timeline

**Date:** 2026-03-13  
**Purpose:** Chronological activity timeline per tenant for platform observability and audit.

## Overview

- **Entity:** **TenantActivityEvents** (TenantId, EventType, EntityType, EntityId, Description, UserId, TimestampUtc, MetadataJson).
- **Service:** `ITenantActivityService` / `TenantActivityService` (Application/Audit).
- **Recording:** `TenantActivityService.RecordAsync(tenantId, eventType, ...)` from tenant-scoped code.

## Example Event Types

- OrderCreated, NotificationSent, JobExecuted, IntegrationCall, OrderCompleted, Login, FeatureFlagChanged.

## API

- **GET /api/platform/tenants/{tenantId}/activity-timeline**  
  Returns last 100 events (or `take` up to 500) ordered by timestamp descending.  
  **Permission:** AdminTenantsView (platform admin only).

## Frontend

- Add an **Activity Timeline** panel to the tenant detail drawer in the platform observability dashboard; call the above endpoint and display events in reverse chronological order.

## Safety

- All events are tenant-scoped (TenantId). Timeline read runs under platform bypass; only the requested tenant’s events are returned.
- Recording must be called with a valid tenant id; no cross-tenant writes.

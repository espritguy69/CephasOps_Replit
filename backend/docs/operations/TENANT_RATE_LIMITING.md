# Tenant Rate Limiting

**Date:** 2026-03-13  
**Purpose:** Prevent a single tenant from overwhelming the platform (noisy-tenant resource exhaustion).

## Overview

Per-tenant request limits are enforced by **TenantRateLimitMiddleware** (API layer). Limits are configurable per plan via **TenantRateLimitOptions**.

## Behavior

- **Tenant resolution:** `ITenantProvider.CurrentTenantId` (company/tenant context from request).
- **Counters:** Per-tenant request counters maintained by **IRateLimitStore** (in-memory or Redis).
- **When exceeded:** HTTP **429 Too Many Requests** with body: `{"error":"Tenant request limit exceeded. Please retry later."}`
- **Log event:** `TenantRateLimitExceeded` with `TenantId`, `Endpoint`, `LimitType`.
- **Metrics:** Each 429 is recorded as **RateLimitExceeded** in TenantUsageRecords; aggregated into **TenantMetricsDaily.RateLimitExceededCount**.

## Suggested Limits (configurable)

| Resource           | Default (config) |
|--------------------|------------------|
| API requests/min   | 100 (per plan)   |
| API requests/hour  | 1000             |

Background jobs, notifications, and integration calls are not rate-limited by this middleware; they are constrained by job queues and operational guards.

## Safety

- No cross-tenant leakage: limits are keyed by tenant/company id.
- Unauthenticated or missing-tenant requests are not rate-limited (pass-through).
- Do not weaken tenant isolation; rate limiting is per-tenant only.

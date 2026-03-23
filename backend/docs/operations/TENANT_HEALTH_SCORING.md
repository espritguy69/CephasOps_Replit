# Tenant Health Scoring

**Date:** 2026-03-13  
**Purpose:** Automatically score tenant health from operational metrics for the platform observability dashboard.

## Overview

- **Service:** `ITenantHealthScoringService` / `TenantHealthScoringService`.
- **Storage:** **TenantMetricsDaily** extended with **HealthScore** (0–100) and **HealthStatus** (Healthy | Warning | Critical).

## Score Inputs (weights)

| Metric                 | Weight |
|------------------------|--------|
| Job failures           | 30%   |
| Notification failures | 20%   |
| Integration failures  | 20%   |
| API error rate         | 20%   |
| Activity drop          | 10%   |

## Score Scale

- **90–100** → Healthy  
- **70–89** → Warning  
- **&lt;70** → Critical  

## Execution

- Run after daily aggregation: **TenantMetricsAggregationHostedService** calls `ComputeAndStoreForAllTenantsAsync(yesterday)` after `AggregateDailyAsync`.
- Platform bypass only; reads JobExecutions, NotificationDispatches, OutboundIntegrationDeliveries, TenantMetricsDaily per tenant.

## Dashboard

- **Tenant operations overview** includes **HealthScore** and **HealthStatus** from the latest TenantMetricsDaily row.
- No tenant data exposed to other tenants; platform-only read.

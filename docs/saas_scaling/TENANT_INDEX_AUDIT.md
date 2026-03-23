# Tenant Query Index Audit

**Date:** 2026-03-13

Audit of tenant-scoped entities and indexes for multi-tenant query performance. Ensures common patterns (list by tenant, by tenant+date, by tenant+status, by tenant+user) are indexed.

---

## Scope

Entities with **CompanyId** or **TenantId** used in:

- Orders, Jobs (JobExecution, BackgroundJob), Files, Notifications, Inventory, Events (EventStoreEntry, ReplayOperation), Billing (TenantSubscription, TenantUsageRecord), Users, Workflow, SI App operations.

---

## Target index patterns

| Pattern | Use case |
|---------|----------|
| (CompanyId) | Filter by tenant |
| (CompanyId, CreatedAt) or (CompanyId, CreatedAtUtc) | List by tenant, newest first |
| (CompanyId, Status) | Filter by tenant and status |
| (CompanyId, UserId) | User-scoped data within tenant |

---

## Entity index summary

### Orders

| Entity | Existing indexes | New indexes | Notes |
|--------|------------------|-------------|--------|
| Order | (CompanyId, ServiceId) unique, (CompanyId, Status, AppointmentDate), (CompanyId, AssignedSiId, AppointmentDate), (CompanyId, PartnerId), (CompanyId, BuildingId), (Status) | **(CompanyId, CreatedAt)** | List orders by tenant by date; CreatedAt from CompanyScopedEntity |

### Jobs

| Entity | Existing indexes | New indexes | Notes |
|--------|------------------|-------------|--------|
| JobExecution | (Status, NextRunAtUtc), (CompanyId, Status) | **(CompanyId, CreatedAtUtc)** | List/fairness by tenant and time |
| BackgroundJob | (State, Priority, CreatedAt), (CompanyId, State, CreatedAt), (CompanyId) | — | Adequate |

### Files

| Entity | Existing indexes | New indexes | Notes |
|--------|------------------|-------------|--------|
| File | (CompanyId, Id), (CompanyId, EntityId, EntityType), (CreatedAt) | **(CompanyId, CreatedAt)** | List by tenant by date; File inherits CreatedAt |

### Notifications

| Entity | Existing indexes | New indexes | Notes |
|--------|------------------|-------------|--------|
| Notification | (UserId, CompanyId, Status), (CompanyId, Type, CreatedAt) | — | Adequate |
| NotificationDispatch | (CompanyId, Status), (Status, NextRetryAtUtc) | — | Adequate |

### Inventory (Materials, etc.)

| Entity | Existing indexes | New indexes | Notes |
|--------|------------------|-------------|--------|
| Material | (CompanyId, ItemCode), (CompanyId, Barcode), (CompanyId, Category), (CompanyId, IsActive) | — | Adequate |

### Events

| Entity | Existing indexes | New indexes | Notes |
|--------|------------------|-------------|--------|
| EventStoreEntry | (CompanyId, EventType, OccurredAtUtc), (Status, NextRetryAtUtc), (OccurredAtUtc) | — | Adequate |
| ReplayOperation | (CompanyId, RequestedAtUtc), (CompanyId, State, RequestedAtUtc) | — | Adequate |

### Billing

| Entity | Existing indexes | New indexes | Notes |
|--------|------------------|-------------|--------|
| TenantSubscription | (TenantId), (BillingPlanId), (Status) | — | Adequate |
| TenantUsageRecord | (TenantId, MetricKey, PeriodStartUtc) | — | Adequate |

### Users

| Entity | Existing indexes | New indexes | Notes |
|--------|------------------|-------------|--------|
| User | (CompanyId), (Email) unique, (IsActive) | **(CompanyId, IsActive)** | List active users by tenant |

### Workflow

| Entity | Existing indexes | New indexes | Notes |
|--------|------------------|-------------|--------|
| WorkflowDefinition | (CompanyId, EntityType, IsActive), (CompanyId, Name) | — | Adequate |
| SystemLog | (CompanyId, Category, CreatedAt) | — | Adequate |

---

## New indexes added (migration)

- **Order:** `IX_Orders_CompanyId_CreatedAt` on (CompanyId, CreatedAt)
- **JobExecution:** `IX_JobExecutions_CompanyId_CreatedAtUtc` on (CompanyId, CreatedAtUtc)
- **File:** `IX_Files_CompanyId_CreatedAt` on (CompanyId, CreatedAt)
- **User:** `IX_Users_CompanyId_IsActive` on (CompanyId, IsActive)

---

## Expected query improvements

- **Order list by tenant:** Queries filtering by CompanyId and ordering by CreatedAt use the new index; avoids full scan on Orders.
- **Job execution list/fairness:** (CompanyId, CreatedAtUtc) supports “pending jobs per tenant” and time-ordered processing.
- **File list by tenant:** List files by company and creation date uses index.
- **User list by tenant:** Active users per company use (CompanyId, IsActive).

---

## Maintenance

- Add new tenant-scoped entities and their common query patterns to this audit.
- After adding new list/dashboard endpoints, verify they use indexed columns in filters and order by.

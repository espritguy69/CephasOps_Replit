# Tenant & Platform Entities

**Date:** April 2026  
**Status:** Verified against code

---

## Tenant

**File:** `CephasOps.Domain/Tenants/Entities/Tenant.cs`  
**Base Class:** `BaseEntity`

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `Id` | Guid | No | Primary key |
| `Name` | string | No | Tenant display name |
| `Slug` | string | No | URL-safe identifier |
| `IsActive` | bool | No | Whether tenant is active |
| `CreatedAt` | DateTime | No | From BaseEntity |
| `UpdatedAt` | DateTime? | Yes | From BaseEntity |

**Note:** Tenant does NOT inherit from `CompanyScopedEntity` — it is the top-level boundary, not scoped within a company.

---

## TenantActivityEvent

**File:** `CephasOps.Domain/Tenants/Entities/TenantActivityEvent.cs`  
**Base Class:** `BaseEntity`

Records activity events for audit trail at the tenant level.

---

## TenantOnboardingProgress

**File:** `CephasOps.Domain/Tenants/Entities/TenantOnboardingProgress.cs`  
**Base Class:** `BaseEntity`

Tracks onboarding progress for new tenants (setup wizard completion, initial configuration steps).

---

## Standard Infrastructure Fields

Most entities inheriting from `CompanyScopedEntity` include these standard fields (not always listed in individual entity docs):

| Field | Type | Description |
|-------|------|-------------|
| `CompanyId` | Guid | Owning company (tenant isolation) |
| `IsDeleted` | bool | Soft-delete flag |
| `DeletedAt` | DateTime? | When soft-deleted |
| `DeletedByUserId` | Guid? | Who performed the soft-delete |
| `RowVersion` | byte[] | Concurrency token (optimistic locking) |
| `CreatedAt` | DateTime | Record creation timestamp |
| `UpdatedAt` | DateTime? | Last modification timestamp |

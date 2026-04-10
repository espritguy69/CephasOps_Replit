# Tenant Hierarchy: Tenant → Company → Department

**Date:** April 2026  
**Status:** Production — Implemented in Phase 11+

---

## Overview

CephasOps uses a three-level isolation hierarchy for multi-tenant SaaS:

```
Tenant (Platform Account)
  └── Company (Legal Entity)
       └── Department (Organizational Unit)
```

---

## 1. Tenant

**Entity:** `CephasOps.Domain/Tenants/Entities/Tenant.cs`  
**Base Class:** `BaseEntity`

| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Primary key |
| `Name` | string | Display name |
| `Slug` | string | URL-safe identifier |
| `IsActive` | bool | Active/suspended flag |

**Role:** Top-level SaaS customer account. A Tenant owns one or more Companies.

---

## 2. Company

**Entity:** `CephasOps.Domain/Companies/Entities/Company.cs`  
**Base Class:** `BaseEntity`

| Field | Type | Description |
|-------|------|-------------|
| `Id` | Guid | Primary key |
| `TenantId` | Guid? | Parent tenant (nullable for legacy compatibility) |
| `LegalName` | string | Legal company name |
| `Code` | string | Short code |
| `Status` | enum | Active/Suspended |

**Role:** A legal business entity. Historically the primary isolation unit (Phase 1–10). Now belongs to a Tenant.

**Relationship:** `Tenant (1) → Company (N)`

---

## 3. Department

**Entity:** `CephasOps.Domain/Departments/Entities/Department.cs`  
**Base Class:** `CompanyScopedEntity` (inherits `CompanyId`)

| Field | Type | Description |
|-------|------|-------------|
| `Name` | string | Department name |
| `Code` | string | Department code |
| `CostCentreId` | Guid? | Linked cost centre |
| `CompanyId` | Guid | Parent company (from `CompanyScopedEntity`) |

**Role:** Organizational sub-unit within a Company. Used for filtering views and reports.

**Relationship:** `Company (1) → Department (N)`

---

## 4. Data Scoping Model

### CompanyScopedEntity

**File:** `CephasOps.Domain/Common/CompanyScopedEntity.cs`

All business entities that need tenant isolation inherit from `CompanyScopedEntity`, which provides:
- `CompanyId` (Guid) — links to the owning Company
- `IsDeleted` (bool) — soft-delete flag
- `DeletedAt` (DateTime?) — when soft-deleted
- `DeletedByUserId` (Guid?) — who soft-deleted

### Global Query Filters

In `ApplicationDbContext.OnModelCreating()`, all `CompanyScopedEntity` subclasses receive automatic filters:

```
WHERE IsDeleted = false 
AND (TenantScope.CurrentTenantId IS NULL OR CompanyId = TenantScope.CurrentTenantId)
```

This ensures every database query is automatically scoped to the current tenant.

### TenantScope

**File:** `CephasOps.Infrastructure/Persistence/TenantScope.cs`

Uses `AsyncLocal<Guid?>` to hold the current tenant/company ID for the request lifetime. Set by `TenantGuardMiddleware` based on JWT claims.

### TenantSafetyGuard

**File:** `CephasOps.Infrastructure/Persistence/TenantSafetyGuard.cs`

Throws exception if sensitive operations are attempted without a valid tenant context, unless `PlatformBypass` is explicitly active (for system-wide operations like migrations).

---

## 5. Enforcement Layers

| Layer | Mechanism | Location |
|-------|-----------|----------|
| **Build-time** | Roslyn analyzers (`CEPHAS001`–`CEPHAS004`) | `analyzers/CephasOps.TenantSafetyAnalyzers/` |
| **CI** | `tenant-safety.yml` workflow blocks unsafe PRs | `.github/workflows/` |
| **Request** | `TenantGuardMiddleware` sets scope from JWT | Infrastructure |
| **Query** | Global query filters on all `CompanyScopedEntity` | `ApplicationDbContext` |
| **Write** | `SaveChanges` validates `CompanyId` matches scope | `ApplicationDbContext` |
| **Manual** | `TenantSafetyGuard.AssertTenantContext()` | Critical controllers |

---

## 6. Platform Bypass

For system-wide operations (migrations, reporting, maintenance), the `TenantScopeExecutor` provides:

- `RunWithTenantScopeAsync(companyId, ...)` — run within a specific tenant
- `RunWithPlatformBypassAsync(...)` — run across all tenants (disables safety checks)

Platform Bypass is logged and should only be used for administrative operations.

---

## 7. Historical Context

| Phase | Scoping Model |
|-------|---------------|
| Phase 1–10 | Flat `CompanyId` scoping — Company was the top-level boundary |
| Phase 11+ | `Tenant → Company → Department` hierarchy introduced |
| Current | `CompanyId` remains the primary query filter; `TenantId` provides the umbrella grouping |

> **Note:** Some older documentation may still describe the flat `CompanyId` model. This document reflects the current production architecture.

# Phase 11: Tenant Isolation Architecture

## Overview

Phase 11 introduces a formal **Tenant** as the top-level isolation boundary for multi-tenant SaaS. The hierarchy is:

**Tenant → Companies → Departments → Users → Roles**

Existing company-scoped data and behaviour remain; tenant is an optional parent so that:

- One tenant can have multiple companies (e.g. group companies).
- Billing, subscriptions, and platform features (Phase 12+) can be keyed by tenant.
- Event bus, command bus, integration bus, and workflows can be made tenant-aware in later phases.

## What Was Implemented

### 1. Tenant entity

- **Domain**: `Tenant` in `CephasOps.Domain.Tenants.Entities` with Id, Name, Slug, IsActive, CreatedAtUtc, UpdatedAtUtc.
- **Persistence**: Table `Tenants`, unique index on Slug, index on IsActive. EF configuration in `TenantConfiguration`.

### 2. Company–Tenant relationship

- **Domain**: `Company` has optional `TenantId` (nullable) and navigation `Tenant`.
- **Persistence**: `Companies.TenantId` FK to `Tenants.Id` with `ON DELETE SET NULL`. Index on `TenantId`.
- **Backward compatibility**: Existing companies have `TenantId = null` (legacy/on-prem style).

### 3. Tenant context

- **Application**: `ITenantContext` exposes `TenantId`, `TenantSlug`, and `IsTenantResolved`.
- **Resolution**: Implemented in API as `TenantContextService`: resolves tenant from current user’s company (`ICurrentUserService.CompanyId` → `Company.TenantId` → optional Tenant load for Slug). When user has no company or company has no tenant, context is unresolved (null).
- **Registration**: `ITenantContext` and `TenantContextService` registered in DI (Api) as scoped.

### 4. Company API and DTOs

- **CompanyDto**: Added `TenantId` and `TenantSlug` for display. Company list and get-by-id load `Tenant` and map these.
- **Create/Update company**: Do not yet accept `TenantId`; can be added in a follow-up so operators can assign a company to a tenant.

### 5. Tenant management API

- **Application**: `ITenantService` / `TenantService` with List, GetById, GetBySlug, Create, Update. DTOs: `TenantDto`, `CreateTenantRequest`, `UpdateTenantRequest`.
- **API**: `TenantsController` under `api/tenants` with:
  - GET list (optional `isActive`), GET by id, GET by slug, POST create, PUT update.
- **Permissions**: `AdminTenantsView`, `AdminTenantsEdit` in `PermissionCatalog`; controller uses `RequirePermission` and `Authorize(Roles = "SuperAdmin,Admin")`. Permissions are seeded with the rest of the catalog and assigned to Admin (admin.*) and SuperAdmin.

### 6. Migration

- **EF migration**: `Phase11_TenantIsolation` adds table `Tenants` and column `Companies.TenantId` with FK and indexes. No data migration; existing companies keep `TenantId = null`.

## Usage

- **Resolving current tenant**: Inject `ITenantContext` in application or API. Use `TenantId` / `TenantSlug` when building tenant-scoped queries, event metadata, or integration routing. When `IsTenantResolved` is false, treat as “no tenant” (e.g. legacy single-company or super-admin).
- **Managing tenants**: Use `api/tenants` (list, get, create, update) with an admin user. Create a tenant, then (when company API is extended) set `TenantId` on one or more companies.
- **JWT / company**: Tenant is derived from the current user’s company. Ensure `companyId` (or equivalent) is set in the token so `ICurrentUserService.CompanyId` is correct; then `ITenantContext` will reflect that company’s tenant.

## Limitations and follow-ups

- **Event bus / command bus / workflows / integration bus**: Not yet tenant-aware. Envelopes and commands do not carry TenantId; filters and routing do not use tenant. These can be extended in later phases to add TenantId to metadata and enforce tenant scoping.
- **Connector endpoints**: Phase 10 is company-scoped; tenant-scoped connectors (e.g. by TenantId) can be added when needed.
- **Company create/update**: DTOs and service do not yet accept `TenantId`; add when you want operators to assign companies to tenants via API.
- **TenantContextService**: Uses synchronous DB read on first property access; acceptable for request-scoped resolution. If needed, consider async resolution or per-request cache.

## Architecture rules

- Domain: Tenant and Company.TenantId + Tenant navigation; no dependency on Application/Infrastructure.
- Application: ITenantContext, tenant service and DTOs; uses existing ApplicationDbContext.
- Infrastructure: Tenant and Company EF configuration, migration.
- API: TenantContextService (resolves from ICurrentUserService + DbContext), TenantsController.

No circular dependencies. Tenant is an optional layer above existing company-based isolation.

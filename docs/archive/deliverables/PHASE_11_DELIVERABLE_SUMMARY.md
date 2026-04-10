# Phase 11 Deliverable Summary — Tenant Isolation

## Delivered

| Item | Description |
|------|-------------|
| **Tenant entity** | Domain `Tenant` (Id, Name, Slug, IsActive, CreatedAtUtc, UpdatedAtUtc). Table `Tenants`, unique Slug, index IsActive. |
| **Company–Tenant link** | `Company.TenantId` (nullable FK to Tenants), `Company.Tenant` navigation. Migration adds column + FK (SET NULL on delete). |
| **ITenantContext** | Application interface: TenantId, TenantSlug, IsTenantResolved. Resolved from current user’s company. |
| **TenantContextService** | Api implementation: uses ICurrentUserService.CompanyId → Company.TenantId → Tenant (for Slug). Sync, request-scoped. |
| **Tenant CRUD** | ITenantService / TenantService; TenantDto, CreateTenantRequest, UpdateTenantRequest. |
| **Tenants API** | GET /api/tenants, GET /api/tenants/{id}, GET /api/tenants/by-slug/{slug}, POST, PUT. Admin + AdminTenantsView/AdminTenantsEdit. |
| **Permissions** | AdminTenantsView, AdminTenantsEdit in PermissionCatalog; seeded and assigned to Admin (admin.*) and SuperAdmin. |
| **CompanyDto** | TenantId, TenantSlug added; CompanyService includes Tenant and maps them. |
| **Migration** | Phase11_TenantIsolation: Tenants table, Companies.TenantId + FK + indexes. |

## Not in scope (follow-ups)

- TenantId on event/command envelopes or workflow/connector scoping.
- Company create/update DTOs accepting TenantId.
- Async or cached tenant resolution.
- Tenant-specific feature flags or billing (Phase 12).

## Verification

- Build: `dotnet build` from backend/src/CephasOps.Api.
- Migration: `dotnet ef database update` (or apply idempotent script per AGENTS.md).
- API: Create tenant via POST /api/tenants; list via GET /api/tenants (as Admin/SuperAdmin).
- Tenant context: Log in with user whose company has TenantId set; inject ITenantContext and confirm TenantId/TenantSlug.

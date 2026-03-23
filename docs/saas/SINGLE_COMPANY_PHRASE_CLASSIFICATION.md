# Single-Company Phrase Classification — SaaS Alignment

**Date:** 2026-03-13  
**Purpose:** Classify documentation phrases that indicate single-company architecture and map them to tenant-scoped, platform-scoped, or shared reference data.

---

## Phrases Found and Classification

| Phrase | Occurrences (documents) | Should Refer To | Notes |
|--------|---------------------------|------------------|--------|
| "the company" | Multiple | Tenant-scoped: "the tenant's company" or "current company (CompanyId)" | Avoid ambiguous "the company" when multiple tenants exist. |
| "all users" | docs/02_modules (notifications, email_parser, escalation-rules, external_portals) | Tenant-scoped: "all users in the tenant" / "users with role in the company" | Clarify scope in each module. |
| "global admin" | docs/event-platform, platform, OPERATIONAL_*, JOB_OBSERVABILITY | Platform-scoped: "PlatformAdmin" or "platform admin" | Prefer "platform admin" for consistency. |
| "system admin" | frontend/README, email_parser SETUP | Platform-scoped or Tenant-scoped by context | Clarify: system = platform vs tenant settings admin. |
| "master data" | Many (SETTINGS_MODULE, MODULE_INVENTORY, inventory, materials, DATA_SEED, etc.) | Tenant-scoped or Shared: "tenant master data" vs "shared reference data" | Materials, categories, order types = tenant. Country list, etc. = shared. |
| "company data" | docs/02_modules/companies/WORKFLOW | Tenant-scoped | Correct; ensure "company" = one tenant's company. |
| "shared inventory" | docs/02_modules/inventory, materials WORKFLOW | Misleading in SaaS | Replace with "tenant-scoped inventory" or "material master data per tenant". |
| "admin can see all" | docs/02_modules/departments (SuperAdmin/Admin see all departments) | Platform admin: all tenants; Tenant admin: all departments in tenant | Split: PlatformAdmin vs TenantAdmin. |
| "global reports" | (implicit in "all data") | Platform-scoped: cross-tenant reports only for platform; tenant reports = tenant-scoped | Document in AUTHORIZATION_MATRIX. |

---

## Entity and Scope Classification Table

Aligned with the codebase: tenant-scoped entities have `CompanyId` and are subject to EF global query filters and TenantSafetyGuard.

| Module | Entity / Concept | Scope | Isolation Key | Notes |
|--------|-------------------|--------|----------------|--------|
| Orders | Order | Tenant | CompanyId | All order entities and related (OrderMaterialUsage, OrderPayoutSnapshot, etc.) |
| Inventory | Material, StockBalance, StockMovement, SerialisedItem, etc. | Tenant | CompanyId | Per-tenant inventory and movements |
| Billing | Invoice, InvoiceLineItem, BillingRatecard, Payment, SupplierInvoice | Tenant | CompanyId | Financial isolation per tenant |
| Companies | Company, Partner, PartnerGroup, Department, CostCentre | Tenant | CompanyId (Company is key; Tenant links to Company) | Tenant = billing/subscription boundary; Company = data boundary |
| Tenants | Tenant, TenantSubscription, TenantFeatureFlags, TenantOnboardingProgress | Platform | TenantId | Platform-scoped; no CompanyId filter |
| Users | User, DepartmentMembership | Tenant (User.CompanyId) | CompanyId | User belongs to one company (tenant) |
| Background jobs | BackgroundJob, JobExecution | Tenant (CompanyId on job) | CompanyId | Job runs under tenant scope |
| Events | EventStoreEntry, replay operations | Tenant (CompanyId on entry) | CompanyId | Dispatch/replay use TenantScopeExecutor |
| Notifications | Notification, NotificationDispatch, NotificationSetting | Tenant | CompanyId | Per-tenant notifications |
| Parser | ParseSession, ParsedOrderDraft, EmailAccount, EmailTemplate | Tenant | CompanyId | Templates and sessions per tenant |
| Settings | DocumentTemplate, MaterialTemplate, SlaProfile, TimeSlot, etc. | Tenant | CompanyId | Settings are tenant-scoped |
| Reference (shared) | Country list, verticals (if shared), system enums | Shared | none | Read-only; no tenant key |
| Audit / logs | AuditLog, RequestLogContext | Tenant or Platform | CompanyId in context | Logs include CompanyId for tenant requests |
| Files | File | Tenant | CompanyId | File storage scoped by company |

---

## Recommended Documentation Wording

- Use **"tenant"** for the subscription/billing boundary (Tenant entity).
- Use **"company"** or **"tenant's company"** for the data boundary (Company entity; CompanyId on entities).
- Use **"platform admin"** (or **PlatformAdmin**) for roles that can operate across tenants; **"tenant admin"** (or **TenantAdmin**) for admin within one tenant.
- Use **"tenant-scoped"** for data that is isolated by CompanyId; **"platform-scoped"** for data that is not (e.g. Tenant list, platform health); **"shared reference"** for read-only data with no tenant key.

---

*For remediation of specific documents see [REMEDIATION_CHECKLIST.md](REMEDIATION_CHECKLIST.md).*

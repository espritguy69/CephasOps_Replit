# Tenancy Model

**Date:** 2026-03-13  
**Purpose:** Define what a tenant is, how it relates to company and users, and how tenant context is used. Aligns with the implemented architecture (Tenant, Company, CompanyId, TenantScopeExecutor).

---

## 1. What Is a Tenant?

In CephasOps, a **tenant** is the **subscription and billing boundary**. It is represented by the **Tenant** entity (platform-scoped). Each tenant has:

- A unique **TenantId** (guid).
- Linkage to a **Company** (one-to-one in the current model: one tenant, one company).
- Subscription state (e.g. trial, active, suspended, cancelled).
- Optional feature flags and onboarding progress.

Tenants do **not** hold operational data directly; the **Company** and all **CompanyId**-scoped entities hold the data. So in practice, "tenant" and "tenant's company" are used together: **tenant** = billing/access boundary, **company** = data boundary (CompanyId on entities).

---

## 2. Relationship Between Tenant and Company

| Concept | Entity | Scope | Role |
|--------|--------|--------|------|
| Tenant | Tenant | Platform | Billing, subscription, feature flags, onboarding. |
| Company | Company | Data boundary | Operational data owner; all tenant-scoped entities have CompanyId. |

Current model: **one Tenant → one Company**. Provisioning creates both (Tenant + Company + default departments + admin user). All business data (orders, inventory, billing, users, etc.) is keyed by **CompanyId**, not TenantId, in the domain and persistence layers. API and job execution use **CompanyId** as the effective tenant context (TenantScope.CurrentTenantId is a CompanyId in the codebase).

---

## 3. User Membership Within Tenants

- Each **User** has a **CompanyId** (one company per user in the current model).
- Users gain access to departments via **DepartmentMembership** (department belongs to a company).
- **Tenant resolution** at request time: JWT may carry CompanyId; or X-Company-Id (SuperAdmin override); or resolution from user's department memberships. The resolved value is the **effective company (tenant context)** for the request.
- Users in one company **cannot** see or modify another company's data unless the code path is explicitly platform-wide (platform bypass) and authorised (e.g. platform admin only).

---

## 4. Tenant Admins vs Platform Admins

| Role type | Scope | Can do |
|-----------|--------|--------|
| **Platform admin** (e.g. SuperAdmin, platform-only roles) | Platform | Create/disable tenants; view tenant list; access platform health and control-plane; optionally impersonate tenant via X-Company-Id. |
| **Tenant admin** (e.g. Admin, TenantAdmin within a company) | One tenant (company) | Manage users, departments, and settings for **that company only**; cannot see other tenants' data. |

Platform admins must **not** bypass tenant isolation by default: cross-tenant operations use explicit platform bypass (e.g. TenantScopeExecutor.RunWithPlatformBypassAsync) in controlled code paths (provisioning, retention, schedulers), not in normal API request handling.

---

## 5. Entities That Must Always Include CompanyId

All **tenant-scoped** entities must have **CompanyId** (or be owned by an entity that has it). The codebase enforces this via:

- **CompanyScopedEntity** base type (Domain) and all derived entities.
- **User**, **BackgroundJob**, **JobExecution**, **OrderPayoutSnapshot**, **InboundWebhookReceipt** (explicitly tenant-scoped in TenantSafetyGuard).

Any new entity that represents tenant-owned data should:

- Inherit from **CompanyScopedEntity**, or
- Have a **CompanyId** and be registered in **TenantSafetyGuard.IsTenantScopedEntityType** and in EF global query filters.

Shared reference data (e.g. country list) has no CompanyId and is not filtered by tenant.

---

## 6. How Tenant Context Is Determined

- **API request:** TenantGuardMiddleware calls ITenantProvider.GetEffectiveCompanyIdAsync(); the result is set as TenantScope.CurrentTenantId for the request and cleared in finally. See [TENANT_RESOLUTION.md](TENANT_RESOLUTION.md).
- **Background job:** Job runner sets TenantScope from job.CompanyId (or platform bypass if null) via TenantScopeExecutor before executing the job delegate.
- **Event dispatch / replay:** Entry.CompanyId drives TenantScopeExecutor.RunWithTenantScopeOrBypassAsync.
- **Webhooks:** Request.CompanyId drives scope or platform bypass.

No manual setting of TenantScope or EnterPlatformBypass in runtime services; use **TenantScopeExecutor** only (except DatabaseSeeder and ApplicationDbContextFactory as documented in [KNOWN_BYPASSES_AND_GUARDS.md](KNOWN_BYPASSES_AND_GUARDS.md)).

---

## 7. Diagram (Conceptual)

```
┌─────────────────────────────────────────────────────────────────┐
│ PLATFORM                                                        │
│  Tenant (billing, subscription)  ──1:1──►  Company (data owner)   │
│       │                                    │                    │
│       │                                    │ CompanyId          │
│       │                                    ▼                    │
│       │  User (CompanyId) ──────────────────────────────────────┤
│       │  Department (CompanyId)                                 │
│       │  Order, Invoice, Material, ... (CompanyId)              │
└─────────────────────────────────────────────────────────────────┘
```

---

*See also: [DATA_ISOLATION_RULES.md](DATA_ISOLATION_RULES.md), [TENANT_RESOLUTION.md](TENANT_RESOLUTION.md), backend [TENANT_SAFETY_DEVELOPER_GUIDE.md](../../backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md).*

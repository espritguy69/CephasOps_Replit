# Authorization Matrix

**Date:** 2026-03-13  
**Purpose:** Define roles (PlatformAdmin, TenantAdmin, OperationsManager, Finance, Installer, Viewer, etc.) and what each can do; ensure platform admins cannot accidentally bypass tenant isolation.

---

## 1. Role Types

| Role (conceptual) | Scope | Description |
|--------------------|--------|-------------|
| **PlatformAdmin** (e.g. SuperAdmin) | Platform | Can manage tenants, view tenant list, access control-plane and platform health; can use X-Company-Id to act in a tenant context. Must use explicit platform APIs for cross-tenant operations; normal API remains tenant-scoped unless designed otherwise. |
| **TenantAdmin** (e.g. Admin within a company) | One tenant | Manage users, departments, and settings for that company only. Cannot see or change other tenants' data. |
| **OperationsManager** | One tenant | Operations, orders, workflow, installers, scheduling within the tenant. |
| **Finance** | One tenant | Billing, invoices, payments, P&amp;L within the tenant. |
| **Installer** (e.g. SI app user) | One tenant | Field operations, tasks, time slots assigned to that tenant. |
| **Viewer** | One tenant | Read-only access to tenant data. |

Exact role names and permissions are defined in the application (RBAC); this matrix describes the **intent** for SaaS alignment.

---

## 2. Capability Matrix

| Capability | PlatformAdmin | TenantAdmin | OperationsManager | Finance | Installer | Viewer |
|------------|---------------|-------------|-------------------|--------|-----------|--------|
| View tenant list | Yes | No | No | No | No | No |
| Create tenants (signup / admin) | Yes (admin path) / Signup (public) | No | No | No | No | No |
| Manage users (invite, roles, deactivate) | In context (X-Company-Id) or platform | Yes (own tenant) | Limited (e.g. own dept) | No | No | No |
| View orders | In context | Yes | Yes | Read as needed | Assigned only | Yes |
| Run reports | In context (tenant) or platform (aggregate) | Yes (tenant) | Yes (tenant) | Yes (tenant) | Limited | Yes (tenant) |
| Access billing / invoices | In context | Yes | Read as needed | Yes | No | Read if permitted |
| View audit logs | Platform-wide or per tenant | Own tenant | Own tenant | Own tenant | No | If permitted |
| Manage departments | In context | Yes | Limited | No | No | No |
| Manage settings / master data | In context | Yes | Limited | No | No | No |
| Background job / replay (admin) | Platform or in context | Own tenant (JobsAdmin) | Own tenant | No | No | No |

---

## 3. Platform Admin and Tenant Isolation

- **Platform admins must not bypass tenant isolation by default.** Normal API requests from a platform admin still resolve to a tenant (e.g. JWT CompanyId or X-Company-Id). Cross-tenant operations (e.g. list all tenants, create tenant, platform health) must be implemented as **separate control-plane or platform endpoints**, using **TenantScopeExecutor.RunWithPlatformBypassAsync** only where necessary and with proper authorization checks.
- **X-Company-Id:** When a platform admin sends X-Company-Id, they are acting **in that tenant's context**. All tenant-scoped data access in that request is then limited to that company. No accidental cross-tenant data return in the same request.
- **Audit:** Platform admin actions (especially tenant create/disable and cross-tenant access) should be audited with clear "platform" vs "tenant" scope in logs.

---

## 4. Tenant-Required vs Platform-Only Endpoints

- **Tenant-required:** Most API routes require a resolved tenant (TenantGuardMiddleware). If resolution fails → 403.
- **Platform-only:** Routes such as tenant list, provisioning (admin), platform health, control-plane are allowlisted or explicitly designed to run without tenant context and are restricted to platform admin (or unauthenticated for signup) with appropriate checks.

---

*See: [TENANCY_MODEL.md](TENANCY_MODEL.md), [TENANT_RESOLUTION.md](TENANT_RESOLUTION.md), [DATA_ISOLATION_RULES.md](DATA_ISOLATION_RULES.md).*

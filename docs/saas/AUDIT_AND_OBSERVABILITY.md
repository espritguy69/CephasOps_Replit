# Audit and Observability

**Date:** 2026-03-13  
**Purpose:** Audit logging scope (tenant vs platform), observability boundaries, and log context (CompanyId, TenantId) so that tenant data is not mixed in logs and platform operations are visible.

---

## 1. Audit Logging Scope

- **Tenant-scoped operations:** Audit logs should record the **CompanyId** (and optionally TenantId) so that each entry is attributable to a tenant. No cross-tenant data in a single audit record; if platform admin performs an action in a tenant context (X-Company-Id), the log should show that company.
- **Platform operations:** Tenant create/disable, provisioning, retention, scheduler runs, etc. run under platform bypass; logs should clearly indicate "platform" or "no tenant" and, where relevant, which tenant was affected (e.g. TenantId in provisioning logs).

---

## 2. Request Log Context

- **RequestLogContextMiddleware** pushes **CompanyId** and **TenantId** (from ITenantProvider.CurrentTenantId) into Serilog **LogContext** so that all request logs for that request carry tenant information. This enables log filtering by tenant and avoids leaking another tenant's data into log messages (ensure no tenant-scoped PII from other tenants is logged).

---

## 3. Observability Boundaries

- **Tenant-scoped:** Operations overview, reports, and dashboards that are exposed to tenant users must only show data for **that tenant**. APIs use tenant resolution and RequireCompanyId; event/store queries use scopeCompanyId when the caller is not a platform admin.
- **Platform-scoped:** Health endpoints (e.g. /health), job backlog summary, tenant list, control-plane, and platform metrics are platform-wide. Access to these should be restricted to platform admin or operational tooling; responses must not include other tenants' PII.

---

## 4. Event Store and Replay

- Event store query APIs accept **scopeCompanyId**. For non–platform admins, pass the user's CompanyId so results are filtered to that tenant. Implementations filter with `Where(e => e.CompanyId == scopeCompanyId)` when scopeCompanyId is set. See docs/event-platform/TENANT_SAFETY.md.

---

## 5. Verification and Reports

- **Operations overview** and **tenant-safety verification reports** (e.g. backend/docs/operations) provide evidence that tenant resolution, scope, and guards are correctly applied. Runbooks should reference tenant context when diagnosing tenant-specific issues.

---

*See: [TENANCY_MODEL.md](TENANCY_MODEL.md), [AUTHORIZATION_MATRIX.md](AUTHORIZATION_MATRIX.md), [DATA_ISOLATION_RULES.md](DATA_ISOLATION_RULES.md), backend [RequestLogContextMiddleware](../../backend/src/CephasOps.Api/Middleware/RequestLogContextMiddleware.cs).*

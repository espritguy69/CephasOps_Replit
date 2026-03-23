# Tenant Offboarding

**Date:** 2026-03-13  
**Purpose:** Data export, tenant disable/suspend, data retention, and offboarding flow so that tenant exit is controlled and compliant.

---

## 1. Data Export

- **Intent:** Allow tenant (or platform admin on their behalf) to export their data before or after termination (e.g. for portability or compliance).
- **Scope:** All tenant-scoped data (CompanyId = tenant's company): orders, invoices, users, materials, settings, etc. Export format and scope should be defined (e.g. CSV/JSON per entity type, or bulk dump). Access to export should be authorised (tenant admin or platform admin in context).
- **Implementation:** Export runs under tenant scope (or platform bypass with explicit CompanyId filter) so that only that tenant's data is included. No cross-tenant data in the export file.

---

## 2. Tenant Disable / Suspend

- **Disable:** Tenant state is set so that users cannot log in or use the API (e.g. subscription cancelled, tenant marked disabled). SubscriptionEnforcementMiddleware (or equivalent) returns 403 or redirects to billing.
- **Suspend:** Same idea; temporary block without full deletion. Data is retained; re-enable restores access.

---

## 3. Data Retention

- **Retention policy:** Define how long tenant data is kept after disable/cancel (e.g. 30/90 days). After that, data may be purged or anonymised per policy.
- **Platform jobs:** Retention/cleanup that touch tenant data (e.g. old notifications, events) must run under **TenantScopeExecutor.RunWithPlatformBypassAsync** and filter by tenant or run per-tenant so that only the intended tenant's data is deleted. See backend TENANT_SAFETY_DEVELOPER_GUIDE (retention services).

---

## 4. Offboarding Flow (Conceptual)

1. **Request:** Tenant or platform initiates offboarding (cancel subscription, request account closure).
2. **Export (optional):** Data export is made available to the tenant.
3. **Disable:** Tenant is suspended/disabled; login and API access blocked.
4. **Retention period:** Data retained for the defined period.
5. **Purge/anonymise:** After retention, tenant-scoped data is purged or anonymised per policy; Tenant/Company records may be soft-deleted or marked as closed.

---

## 5. References

- **Backend:** Tenant state (active/suspended/cancelled); SubscriptionEnforcementMiddleware; retention services (platform bypass, per-tenant or filtered).
- **Docs:** [BILLING_SUBSCRIPTIONS.md](BILLING_SUBSCRIPTIONS.md), [PROVISIONING_FLOW.md](PROVISIONING_FLOW.md), [DATA_ISOLATION_RULES.md](DATA_ISOLATION_RULES.md).

---

*See also: [TENANCY_MODEL.md](TENANCY_MODEL.md), [AUTHORIZATION_MATRIX.md](AUTHORIZATION_MATRIX.md).*

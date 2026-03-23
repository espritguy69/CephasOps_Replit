# Billing and Subscriptions

**Date:** 2026-03-13  
**Purpose:** Subscription model, tenant suspension/disable, feature flags, and linkage to provisioning. Describes how tenant access and capability follow subscription status.

---

## 1. Subscription Model

- Each **Tenant** is linked to a **TenantSubscription** (or equivalent) that describes plan, status, and validity (e.g. trial, active, cancelled, past-due, expired).
- Provisioning creates an initial subscription (e.g. trial) when the tenant is created. Billing and plan changes are handled by platform processes (not described in detail here; see product/billing docs).

---

## 2. Tenant State and Access

- **Active:** Tenant can use the platform within the limits of their plan and feature flags.
- **Suspended / Disabled:** Tenant access can be blocked or limited by **SubscriptionEnforcementMiddleware** (or equivalent): e.g. return 403 or redirect to billing when subscription is cancelled, past-due, or expired, or when the tenant is manually suspended.
- **Offboarding:** See [TENANT_OFFBOARDING.md](TENANT_OFFBOARDING.md) for data export and disable flows.

---

## 3. Feature Flags

- **TenantFeatureFlags** (or equivalent) allow the platform to enable/disable features per tenant (e.g. by plan or A/B). Provisioning initialises default flags for new tenants. Middleware or application code can check flags before allowing access to specific features.

---

## 4. Linkage to Provisioning

- During **provisioning** (see [PROVISIONING_FLOW.md](PROVISIONING_FLOW.md)), a default subscription is created and linked to the new tenant so that the tenant can use the system immediately (e.g. trial). No separate "billing signup" step is required for the minimal flow; billing upgrade/payment can be added as a separate process.

---

## 5. References

- **Backend:** SubscriptionEnforcementMiddleware; TenantSubscription; TenantFeatureFlags; CompanyProvisioningService (creates subscription linkage).
- **Docs:** [docs/architecture/SAAS_PHASE3_SUBSCRIPTION_ENFORCEMENT.md](../architecture/SAAS_PHASE3_SUBSCRIPTION_ENFORCEMENT.md), [PROVISIONING_FLOW.md](PROVISIONING_FLOW.md).

---

*See also: [TENANCY_MODEL.md](TENANCY_MODEL.md), [AUTHORIZATION_MATRIX.md](AUTHORIZATION_MATRIX.md).*

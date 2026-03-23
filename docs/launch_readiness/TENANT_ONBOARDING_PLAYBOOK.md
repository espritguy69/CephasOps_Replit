# Tenant Onboarding Playbook

Use this playbook to onboard the first (and subsequent) production tenants in a consistent, verifiable way.

---

## Prerequisites

- Platform is live and healthy (`/health/ready` and `/health/platform` Healthy where applicable).
- Signup/provisioning and billing flows are configured (trial plan, billing provider if applicable).
- Operations team has access to platform admin and (if needed) database/API for verification.

---

## Steps

### 1. Create tenant

- Use the **signup flow** or **platform admin** to create the tenant (company).
- Record: **Company ID**, **tenant name**, **admin email**, **chosen plan** (e.g. Trial).

### 2. Verify provisioning

- Confirm the company record exists and is in the expected state (e.g. Active).
- Confirm tenant-scoped data stores are created as per your provisioning logic (e.g. default settings, feature flags).
- If using separate DB schema or database per tenant, confirm creation and migrations.

### 3. Verify trial subscription

- When the tenant is on a trial plan, confirm:
  - Billing plan or subscription record exists and is Trial.
  - Trial end date (if applicable) is set.
  - Rate limits match trial plan (e.g. `SaaS:TenantRateLimit:Plans:Trial`).

### 4. Verify onboarding wizard

- Log in as the tenant admin (or use a test user).
- Complete (or skip) the **onboarding wizard** if the product has one.
- Confirm onboarding state is updated (e.g. `OnboardingProgress` or equivalent) and the tenant can access the main app.

### 5. Verify analytics tracking

- Confirm that tenant usage or analytics events are recorded (e.g. usage metrics, audit logs, or analytics pipeline).
- Check platform analytics or admin view: the new tenant appears in “active tenants” or equivalent.

### 6. Verify Guardian baseline

- After a short period (e.g. after first Guardian run), confirm:
  - No **critical** anomalies for the new tenant (platform health or anomaly API).
  - Guardian is running and optional drift report shows no unexpected issues for this tenant.
- If the tenant has no data yet, baseline may be minimal; document “baseline verified” once Guardian has run at least once.

---

## Checklist (summary)

- [ ] Tenant created (company + admin user).
- [ ] Provisioning verified (data stores, feature flags, schema if applicable).
- [ ] Trial subscription and rate limits verified.
- [ ] Onboarding wizard verified (state and access).
- [ ] Analytics/usage tracking verified.
- [ ] Guardian baseline verified (no critical anomalies after first run).

---

## Rollback

If onboarding fails or must be reverted:

- Disable or delete the tenant per your **tenant lifecycle** procedure (e.g. soft-delete, disable login).
- Remove or archive tenant data per retention and compliance policy.
- Document the reason and any follow-up (e.g. fix provisioning, then retry).

## See also

- [GO_LIVE_CHECKLIST.md](GO_LIVE_CHECKLIST.md) — Pre-launch checks before first tenant.
- [INCIDENT_RESPONSE.md](INCIDENT_RESPONSE.md) — If a tenant data or signup incident occurs.

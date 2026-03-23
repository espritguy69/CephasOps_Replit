# SaaS Multi-Tenant Documentation

**Date:** 2026-03-13  
**Purpose:** Central index for multi-tenant SaaS architecture documentation. Use this section for tenant model, isolation rules, provisioning, authorization, and operational runbooks.

This documentation aligns with the tenant architecture implemented in the codebase (TenantScopeExecutor, TenantGuardMiddleware, TenantSafetyGuard, EF global query filters). It does **not** describe aspirational features; it reflects current behaviour and safeguards.

---

## Core SaaS Documents

| Document | Purpose |
|----------|---------|
| [TENANCY_MODEL.md](TENANCY_MODEL.md) | What a tenant is; tenant–company relationship; user membership; tenant vs platform admins; entities that require CompanyId. |
| [TENANT_RESOLUTION.md](TENANT_RESOLUTION.md) | How tenant context is determined per request, job, event, and webhook; ITenantProvider; X-Company-Id; JWT. |
| [DATA_ISOLATION_RULES.md](DATA_ISOLATION_RULES.md) | Tenant-scoped vs platform-scoped vs shared reference data; safeguards (TenantScope, guards, EF filters, SaveChanges). |
| [PROVISIONING_FLOW.md](PROVISIONING_FLOW.md) | Tenant creation; company creation; department initialization; admin creation; subscription linkage; onboarding. |
| [AUTHORIZATION_MATRIX.md](AUTHORIZATION_MATRIX.md) | Roles (PlatformAdmin, TenantAdmin, OperationsManager, etc.); what each can do; platform vs tenant boundaries. |
| [BILLING_SUBSCRIPTIONS.md](BILLING_SUBSCRIPTIONS.md) | Subscription model; tenant suspension/disable; feature flags; linkage to provisioning. |
| [BACKGROUND_JOB_ISOLATION.md](BACKGROUND_JOB_ISOLATION.md) | Tenant-aware job execution; per-tenant fairness; TenantScopeExecutor; logging CompanyId; failure isolation. |
| [AUDIT_AND_OBSERVABILITY.md](AUDIT_AND_OBSERVABILITY.md) | Audit logging scope; tenant-scoped vs platform observability; log context (CompanyId, TenantId). |
| [TENANT_OFFBOARDING.md](TENANT_OFFBOARDING.md) | Data export; tenant disable/suspend; data retention; offboarding flow. |
| [KNOWN_BYPASSES_AND_GUARDS.md](KNOWN_BYPASSES_AND_GUARDS.md) | Documented platform bypasses (seeding, design-time, retention, schedulers); guards (TenantSafetyGuard, SiWorkflowGuard, etc.). |
| [SAAS_AUDIT_CHECKLIST.md](SAAS_AUDIT_CHECKLIST.md) | Checklist for verifying tenant safety when adding or changing code. |

---

## Audit and Remediation

| Document | Purpose |
|----------|---------|
| [DOCUMENTATION_AUDIT_TABLE.md](DOCUMENTATION_AUDIT_TABLE.md) | Phase 1 audit: all discovered docs, purpose, single-company assumptions, SaaS alignment, action needed. |
| [SINGLE_COMPANY_PHRASE_CLASSIFICATION.md](SINGLE_COMPANY_PHRASE_CLASSIFICATION.md) | Phase 2: phrases indicating single-company architecture; classification as tenant-scoped, platform-scoped, or shared. |
| [REMEDIATION_CHECKLIST.md](REMEDIATION_CHECKLIST.md) | Prioritised remediation tasks for documentation and architecture clarity. |
| [SAAS_AUDIT_FINAL_OUTPUT.md](SAAS_AUDIT_FINAL_OUTPUT.md) | Final output summary: audit table, SaaS structure, new docs, remediation checklist, updated transition audit. |

---

## Related Documentation (outside /docs/saas/)

- **Backend tenant safety (authoritative):**  
  [backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md](../../backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md),  
  [SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md](../../backend/docs/architecture/SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md),  
  [TENANT_SCOPE_EXECUTOR_COMPLETION.md](../../backend/docs/architecture/TENANT_SCOPE_EXECUTOR_COMPLETION.md)
- **Transition audit:**  
  [backend/docs/operations/MULTI_TENANT_TRANSITION_AUDIT.md](../../backend/docs/operations/MULTI_TENANT_TRANSITION_AUDIT.md)
- **Onboarding / operations:**  
  [docs/saas_operations/ONBOARDING_FLOW.md](../saas_operations/ONBOARDING_FLOW.md),  
  [docs/saas_scaling/TENANT_PROVISIONING_FLOW.md](../saas_scaling/TENANT_PROVISIONING_FLOW.md),  
  [docs/saas_scaling/JOB_ISOLATION.md](../saas_scaling/JOB_ISOLATION.md)

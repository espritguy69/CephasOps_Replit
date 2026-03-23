# SaaS Documentation Audit — Final Output

**Date:** 2026-03-13  
**Scope:** Documentation and architecture audit only. No application code, database schema, or migrations modified.

---

## 1. Documentation Audit Table

**Location:** [DOCUMENTATION_AUDIT_TABLE.md](DOCUMENTATION_AUDIT_TABLE.md)

- Scanned: **docs/**, **backend/docs/**, **README.md**, **AGENTS.md**, and related architecture, deployment, operations, security, and API documentation.
- For each document (representative set): **purpose**, **single-company assumptions**, **SaaS alignment**, **action needed**.
- Summary: README and MULTI_COMPANY_ARCHITECTURE have critical single-company wording; companies/auth/departments WORKFLOW need major rewrites; backend tenant-safety docs are already SaaS-aligned; docs/saas_* and remediation docs largely aligned.

---

## 2. SaaS Documentation Structure

**Location:** [README.md](README.md)

New section **docs/saas/** with:

| Document | Purpose |
|----------|---------|
| TENANCY_MODEL.md | Tenant vs company, user membership, tenant vs platform admins, entities with CompanyId. |
| TENANT_RESOLUTION.md | How tenant context is determined (request, job, event, webhook). |
| DATA_ISOLATION_RULES.md | Tenant/platform/shared data; safeguards; cross-tenant must fail safely. |
| PROVISIONING_FLOW.md | Tenant/company creation, departments, admin, subscription, feature flags, onboarding. |
| AUTHORIZATION_MATRIX.md | Roles; capability matrix; platform admin cannot bypass isolation. |
| BILLING_SUBSCRIPTIONS.md | Subscription model, suspend/disable, feature flags. |
| BACKGROUND_JOB_ISOLATION.md | Tenant-aware jobs, fairness, TenantScopeExecutor, logging, failure isolation. |
| AUDIT_AND_OBSERVABILITY.md | Audit scope, log context, observability boundaries. |
| TENANT_OFFBOARDING.md | Data export, disable/suspend, retention, offboarding. |
| KNOWN_BYPASSES_AND_GUARDS.md | Documented bypasses and guards. |
| SAAS_AUDIT_CHECKLIST.md | PR/verification checklist for tenant safety. |
| DOCUMENTATION_AUDIT_TABLE.md | Phase 1 audit table. |
| SINGLE_COMPANY_PHRASE_CLASSIFICATION.md | Phase 2 phrase and entity/scope classification. |
| REMEDIATION_CHECKLIST.md | Prioritised documentation remediation tasks. |

Existing docs (e.g. backend/docs/architecture tenant safety, docs/saas_operations, docs/saas_scaling) are linked from README where appropriate; no duplication of content.

---

## 3. New SaaS Documentation Files

All created under **docs/saas/** and aligned with the current tenant architecture in the codebase (TenantScopeExecutor, TenantGuardMiddleware, TenantSafetyGuard, EF global query filters, SaveChanges tenant-integrity). Content reflects **implemented** behaviour, not aspirational features.

---

## 4. Remediation Checklist

**Location:** [REMEDIATION_CHECKLIST.md](REMEDIATION_CHECKLIST.md)

**Priority 1 (Critical):** README deployment line; MULTI_COMPANY_ARCHITECTURE rewrite; companies WORKFLOW; auth WORKFLOW.  
**Priority 2 (High):** Departments “see all”; API overview tenant scoping; settings/master data wording; SaaS readiness X-Company-Id.  
**Priority 3 (Medium):** “Global admin” → “platform admin”; “all users” scope; frontend README; docs index link to docs/saas.  
**Priority 4 (Low):** Module inventory; inventory/materials “shared”; RBAC overview; runbooks.

---

## 5. Updated Transition Audit Report

**Location:** [backend/docs/operations/MULTI_TENANT_TRANSITION_AUDIT.md](../../backend/docs/operations/MULTI_TENANT_TRANSITION_AUDIT.md)

Added:

- **Documentation audit and SaaS docs (2026-03-13)** — Reference to docs/saas/ and risk classification (Critical/High/Medium/Low) for documentation findings.
- **Verification evidence** — Code remediations (FindAsync, SaveChanges, single-company removal, Email/SMS); documentation (docs/saas/ created); ongoing (checklist, remediation, _MigrationHelper, BillingRatecardService).
- **SaaS documentation index** — Pointer to docs/saas/README.md at end of report.

---

## 6. Alignment with Implemented Tenant Architecture

- **Tenant model:** Tenant = subscription boundary; Company = data boundary (CompanyId); one tenant → one company; users have CompanyId.
- **Resolution:** ITenantProvider (X-Company-Id, JWT, department fallback); TenantGuardMiddleware; scope set in Program.cs.
- **Isolation:** TenantScope; EF global query filters; TenantSafetyGuard on SaveChanges (tenant context + entity CompanyId check); TenantScopeExecutor only in runtime services.
- **Bypasses:** Only documented exceptions (seeder, design-time factory, retention, schedulers, provisioning, webhook/event with no company, job reap).
- **Jobs/events/webhooks:** TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(job.CompanyId / entry.CompanyId / request.CompanyId).

No code or schema was changed; documentation only.

---

*This file is the final output of the SaaS documentation audit. For entry point to all SaaS docs use [README.md](README.md).*

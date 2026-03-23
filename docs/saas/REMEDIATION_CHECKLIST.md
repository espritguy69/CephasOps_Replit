# SaaS Documentation Remediation Checklist

**Date:** 2026-03-13  
**Purpose:** Prioritised list of documentation and architecture documentation tasks to complete the single-company to multi-tenant SaaS documentation alignment.

---

## Priority 1 — Critical (misleading or blocking)

| # | Task | Location | Action |
|---|------|----------|--------|
| 1 | **README deployment/architecture** | README.md | Rewrite "Deployment: Single-company, multi-department" to reflect multi-tenant capability and point to docs/saas. |
| 2 | **MULTI_COMPANY_ARCHITECTURE** | docs/01_system/MULTI_COMPANY_ARCHITECTURE.md | Major rewrite: state current multi-tenant model (Tenant, Company, CompanyId, TenantScopeExecutor); remove "reference only for future"; align with backend tenant safety docs. |
| 3 | **Companies WORKFLOW single-company rule** | docs/02_modules/companies/WORKFLOW.md | Remove "Single-Company Mode", "CompanyId is Guid.Empty for all users"; describe tenant-scoped company creation and provisioning. |
| 4 | **Auth WORKFLOW single-company** | docs/02_modules/auth/WORKFLOW.md | Remove "Single-company mode: CompanyId is Guid.Empty for all users"; describe JWT CompanyId and tenant resolution. |

---

## Priority 2 — High (clear single-company assumptions)

| # | Task | Location | Action |
|---|------|----------|--------|
| 5 | **Departments "see all"** | docs/02_modules/departments/WORKFLOW.md | Clarify: Platform admin sees all tenants' departments; Tenant admin sees all departments in own tenant only. |
| 6 | **API overview tenant scoping** | docs/04_api/API_OVERVIEW.md | Add tenant scoping (RequireCompanyId, X-Company-Id); align with TENANT_RESOLUTION and AUTHORIZATION_MATRIX. |
| 7 | **Settings / master data "entire system"** | docs/02_modules/global_settings/SETTINGS_MODULE.md | Clarify tenant-scoped vs platform settings; "master data" per tenant vs shared reference. |
| 8 | **SaaS Architecture Readiness X-Company-Id** | docs/SaaS_ARCHITECTURE_READINESS_REPORT.md | Update with current guard behaviour (X-Company-Id only for authorised platform admin). |

---

## Priority 3 — Medium (terminology and consistency)

| # | Task | Location | Action |
|---|------|----------|--------|
| 9 | **"Global admin" → "Platform admin"** | docs/event-platform, docs/platform, OPERATIONAL_*, JOB_OBSERVABILITY_DESIGN | Replace "global admin" with "platform admin" or "PlatformAdmin" for consistency. |
| 10 | **"All users" scope** | docs/02_modules/notifications, email_parser, escalation-rules, external_portals | Clarify: "all users **in the tenant**" or "users with role **in the company**". |
| 11 | **Frontend README "System admins"** | frontend/README.md | Clarify: platform vs tenant admin (system = platform; tenant settings = tenant admin). |
| 12 | **Docs index link to SaaS** | docs/README.md, docs/00_QUICK_NAVIGATION.md | Add link to docs/saas/ and multi-tenant section. |

---

## Priority 4 — Low (nice-to-have and coverage)

| # | Task | Location | Action |
|---|------|----------|--------|
| 13 | **Module inventory master data** | docs/02_modules/MODULE_INVENTORY.md | Add note: "master data" is tenant-scoped unless stated as shared reference. |
| 14 | **Inventory/materials "shared"** | docs/02_modules/inventory, materials WORKFLOW | Replace "material master data shared" with "tenant-scoped material master data". |
| 15 | **RBAC overview** | docs/02_modules/rbac/OVERVIEW.md | Add PlatformAdmin vs TenantAdmin; tenant-scoped data for Settings & Master Data. |
| 16 | **Operational runbooks** | docs/saas_operations, docs/production_architecture | Ensure runbooks reference tenant context where relevant (e.g. diagnosing tenant-specific issues). |

---

## Architecture Documentation (already present; verify links)

| Doc | Status |
|-----|--------|
| backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md | Authoritative; link from docs/saas and README. |
| backend/docs/architecture/SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md | Authoritative; no change. |
| backend/docs/architecture/TENANT_SCOPE_EXECUTOR_COMPLETION.md | Authoritative; no change. |
| backend/docs/operations/MULTI_TENANT_TRANSITION_AUDIT.md | Update with link to docs/saas; add "Verification evidence" section. |
| docs/saas/* (new) | Created in this audit; link from DOCUMENTATION_AUDIT_TABLE and transition audit. |

---

## Verification Evidence (for transition audit)

After remediation:

- [ ] README and MULTI_COMPANY_ARCHITECTURE describe multi-tenant model and point to docs/saas.
- [ ] Companies and Auth WORKFLOW no longer state single-company mode or Guid.Empty for all users.
- [ ] API overview describes tenant resolution and RequireCompanyId.
- [ ] At least one pass over 02_modules replacing "all users" / "master data for entire system" with tenant-scoped wording.
- [ ] docs/saas/ README is the entry point for tenant model, isolation, provisioning, auth, jobs, audit, offboarding, bypasses, and checklist.

---

*This checklist is produced as part of the SaaS documentation audit. For the full audit table see [DOCUMENTATION_AUDIT_TABLE.md](DOCUMENTATION_AUDIT_TABLE.md).*

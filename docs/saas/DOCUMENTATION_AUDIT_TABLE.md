# Documentation Audit Table — Single-Company to Multi-Tenant SaaS

**Date:** 2026-03-13  
**Scope:** Repository-wide documentation scan. Purpose: identify single-company assumptions and SaaS alignment.

---

## Summary

| Category | Count | Single-Company Assumptions | SaaS Aligned | Action Needed |
|----------|-------|----------------------------|--------------|---------------|
| Root / README | 2 | 1 | 0 | 1 rewrite, 1 minor |
| /docs (top-level) | 40+ | 15+ | 10+ | Various |
| /docs/01_system | 12 | 2 | 1 | 1 major, 1 minor |
| /docs/02_modules | 80+ | 20+ | 30+ | Many minor/major |
| /docs/04_api | 5 | 1 | 3 | 1 minor |
| /docs/architecture | 15+ | 3 | 8+ | 2 major |
| /docs/saas_* | 25+ | 2 | 20+ | Minor updates |
| /backend/docs | 52 | 0 | 50+ | 2 minor |

---

## Audit Table (selected and representative)

| File | Purpose | Single-Company Assumptions | SaaS Alignment | Action Needed |
|------|----------|----------------------------|----------------|---------------|
| **README.md** | Project overview, quick start | Yes — "Single-company, multi-department", "Deployment: Single-company" | Partial | Rewrite deployment/architecture sentence to reflect multi-tenant capability |
| **AGENTS.md** | Agent instructions, DB, run commands | No | Yes | Minor: ensure "multi-tenant" mentioned where relevant |
| **docs/README.md** | Documentation index | Partial | Partial | Add link to docs/saas/ and multi-tenant section |
| **docs/01_system/MULTI_COMPANY_ARCHITECTURE.md** | System-level multi-company design | Yes — "single-company mode", "reference only for future" | Partial | Major rewrite: state current multi-tenant model; remove "reference only" |
| **docs/01_system/SYSTEM_OVERVIEW.md** | High-level system description | Unknown | Partial | Review for "the company", "all users"; align with tenant model |
| **docs/02_modules/companies/WORKFLOW.md** | Companies module workflow | Yes — "Single-Company Mode", "CompanyId is Guid.Empty for all users" | No | Major rewrite: tenant-scoped company creation; remove single-company rule |
| **docs/02_modules/auth/WORKFLOW.md** | Auth workflow | Yes — "Single-company mode: CompanyId is Guid.Empty for all users" | No | Rewrite: JWT CompanyId, tenant resolution |
| **docs/02_modules/departments/WORKFLOW.md** | Departments | Yes — "SuperAdmin/Admin can see all departments" | Partial | Clarify: platform admin sees all tenants; tenant admin sees own |
| **docs/02_modules/global_settings/SETTINGS_MODULE.md** | Settings, master data | Yes — "Master Data for the entire system" | Partial | Clarify tenant-scoped vs platform settings |
| **docs/02_modules/notifications/WORKFLOW.md** | Notifications | Partial — "Resolve by Role: all users with role" | Partial | Clarify: tenant-scoped "all users" (same company) |
| **docs/02_modules/email_parser/SPECIFICATION.md** | Email parser spec | Partial — "all users with that role" | Partial | Clarify tenant scope for recipients |
| **docs/02_modules/inventory/WORKFLOW.md** | Inventory | Partial — "Material master data shared" | Partial | Clarify tenant-scoped materials |
| **docs/02_modules/rbac/OVERVIEW.md** | RBAC | Partial — "Settings & Master Data" (global tone) | Partial | Add PlatformAdmin vs TenantAdmin; tenant-scoped data |
| **docs/04_api/API_OVERVIEW.md** | API principles, auth, RBAC | Partial — "Department Scoping" only | Partial | Add tenant scoping; RequireCompanyId; X-Company-Id |
| **docs/architecture/SAAS_PHASE3_SUBSCRIPTION_ENFORCEMENT.md** | Subscription enforcement | No | Yes | Minor update dates/links |
| **docs/event-platform/tenant-safety.md** | Event tenant safety | No | Yes | Keep; link to docs/saas |
| **docs/event-platform/TENANT_SAFETY.md** | Event store scopeCompanyId | No | Yes | Keep |
| **docs/platform/TENANT_SAFETY_VERIFICATION.md** | New API tenant verification | Partial — "global admin" | Partial | Align with AUTHORIZATION_MATRIX (PlatformAdmin) |
| **docs/JOB_OBSERVABILITY_DESIGN.md** | Job observability | Partial — "SuperAdmin can see all" | Partial | Align with platform vs tenant admin |
| **docs/OPERATIONAL_REPLAY_ENGINE_PHASE1.md** | Replay engine | No — "company-scoped for non–global admins" | Yes | Minor terminology (global → platform admin) |
| **docs/SaaS_ARCHITECTURE_READINESS_REPORT.md** | SaaS readiness | Partial — X-Company-Id security note | Partial | Update with current guard behaviour |
| **docs/remediation/SAAS_REMEDIATION_VERIFICATION.md** | Verification checklist | No | Yes | Link to docs/saas |
| **docs/launch_readiness/TENANT_ONBOARDING_PLAYBOOK.md** | Onboarding playbook | Partial — "company record" | Yes | Minor wording |
| **docs/saas_operations/ONBOARDING_FLOW.md** | Signup, first login, wizard | No | Yes | Minor: link to PROVISIONING_FLOW |
| **docs/saas_scaling/JOB_ISOLATION.md** | Job tenant fairness | No | Yes | Link to BACKGROUND_JOB_ISOLATION |
| **docs/saas_scaling/TENANT_PROVISIONING_FLOW.md** | Provisioning | No | Yes | Link to docs/saas PROVISIONING_FLOW |
| **backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md** | When to set scope, bypass, executor | No | Yes | None |
| **backend/docs/architecture/SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md** | Security diagram, guards | No | Yes | None |
| **backend/docs/architecture/TENANT_SCOPE_EXECUTOR_COMPLETION.md** | Executor standard | No | Yes | None |
| **backend/docs/architecture/EF_TENANT_SCOPE_SAFETY.md** | EF scope, filters, SaveChanges | No | Yes | None |
| **backend/docs/architecture/TENANT_QUERY_SAFETY_GUIDELINES.md** | Explicit company-scoped queries | No | Yes | None |
| **backend/docs/operations/MULTI_TENANT_TRANSITION_AUDIT.md** | Transition state, risks, verdict | No | Yes | Update with link to docs/saas; add verification evidence |
| **backend/docs/COMPANY_REMOVAL_COMPLETE.md** | Company removal summary | No | Yes | Historical; no change |
| **backend/docs/TENANT_GUARD_AUDIT_REPORT.md** | TenantGuard audit | No | Yes | None |
| **frontend/README.md** | Frontend setup | Partial — "System admins" | Partial | Clarify: platform vs tenant admin |

---

## Discovery Summary

- **Total .md files scanned:** 700+ (docs + backend/docs + root + frontend/infra).
- **Single-company assumptions:** Present in README, MULTI_COMPANY_ARCHITECTURE, companies/auth/departments WORKFLOW, several 02_modules and event/platform docs (e.g. "global admin", "all users", "master data for entire system").
- **Already SaaS-aligned:** Most of backend/docs/architecture (tenant safety), backend/docs/operations (transition audit, guards), docs/saas_operations, docs/saas_scaling (provisioning, jobs), remediation docs.
- **Action priorities:** (1) README deployment line, (2) MULTI_COMPANY_ARCHITECTURE and companies/auth WORKFLOW rewrites, (3) Module docs that say "all users" or "master data" without tenant scope, (4) API overview tenant scoping, (5) Terminology consistency (global admin → platform admin).

---

*This table is produced as part of the SaaS documentation audit. For remediation tasks see [REMEDIATION_CHECKLIST.md](REMEDIATION_CHECKLIST.md).*

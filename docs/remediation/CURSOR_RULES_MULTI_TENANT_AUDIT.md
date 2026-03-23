# Cursor Rules Multi-Tenant Audit Report

**Date:** 2026-03-13  
**Scope:** All Cursor rules, system prompts, and operational guidance in the CephasOps repository  
**Goal:** Discover and list rules that still assume single-company architecture vs. those already aligned with multi-tenant SaaS.  
**Task:** Read-only audit — no modifications applied.

---

## 1️⃣ List of All Cursor Rule Files

| File | Purpose | Status |
|------|---------|--------|
| `.cursorrules` | Root MCP/agent instructions: workflow, dev strategy, **Single Company Multi-Department Mode**, structure, backend/frontend rules, docs steward | **⚠️ NEEDS UPDATE** |
| `.cursor/rules/00_no_manual_scope.mdc` | Prevent manual tenant scope; require TenantScopeExecutor only | ✅ MULTI-TENANT SAFE |
| `.cursor/rules/01_cephas_precheck.mdc` | Architecture safety review before coding; tenant ownership, executor method, guards | ✅ MULTI-TENANT SAFE |
| `.cursor/rules/02_tenant_safety.mdc` | Tenant isolation, TenantScopeExecutor, TenantSafetyGuard, CompanyId propagation | ✅ MULTI-TENANT SAFE |
| `.cursor/rules/03_backend_workers.mdc` | Hosted services/workers must use TenantScopeExecutor; CompanyId source | ✅ MULTI-TENANT SAFE |
| `.cursor/rules/04_code_review_guard.mdc` | Code review for manual scope, missing executor, tenant-owned writes without CompanyId, guards | ✅ MULTI-TENANT SAFE |
| `.cursor/rules/05_docs_reference.mdc` | Point to tenant-safety architecture docs as primary references | ✅ MULTI-TENANT SAFE |
| `.cursor/rules/postgress.mdc` | PostgreSQL connection details; no tenant/company content | ✅ NEUTRAL / SAFE |
| `.cursor/rules/dotnet-version.mdc` | .NET 10 / net10.0 standard; no tenant/company content | ✅ NEUTRAL / SAFE |
| `.cursor/rules/ask.mdc` | Ask-mode: no code, layman terms; Syncfusion key; no tenant/company content | ✅ NEUTRAL / SAFE |
| `.cursor/rules/documentation-dates.mdc` | Use 2026 for doc dates; globs docs/**/*.md | ✅ NEUTRAL / SAFE |
| `.cursor/rules/architecture-guardrails.mdc` | Clean Architecture, no DbContext in controllers, idempotent jobs, etc.; no tenant wording | ✅ NEUTRAL / SAFE |
| `.cursor/rules/ef-migration-governance.mdc` | EF migration governance, counts, script-only manifest; no tenant/company content | ✅ NEUTRAL / SAFE |
| `.cursor/prompts/pre_coding_review.md` | Pre-coding: tenant/platform ownership, TenantScopeExecutor, CompanyId, guards | ✅ MULTI-TENANT SAFE |
| `.cursor/prompts/implementation_plan.md` | Plan: tenant scope, platform bypass, nullable-company path | ✅ MULTI-TENANT SAFE |
| `.cursor/prompts/final_review.md` | Final review: tenant safety, executor, guards, null-company, tests | ✅ MULTI-TENANT SAFE |
| `AGENTS.md` | Project overview, setup, run commands; describes "multi-company ISP operations management platform" | ✅ MULTI-TENANT SAFE |
| `cursor/CURSOR_ONBOARDING_PROMPT.md` | Master onboarding: follow multi-company boundaries, CompanyId filters, multi-company isolation | ✅ MULTI-TENANT SAFE |
| `cursor/BACKEND FEATURE DELTA PROMPT.md` | Backend delta: multi-company filters required | ✅ MULTI-TENANT SAFE |
| `cursor/FRONTEND FEATURE DELTA PROMPT.md` | Frontend delta: company/department context required | ✅ MULTI-TENANT SAFE |
| `cursor/ONBOARDING_SI.md` | SI onboarding: multi-company isolation, CompanyId | ✅ MULTI-TENANT SAFE |
| `cursor/CHECK_BACKEND_COMPLETION.md` | Backend completion checklist: multi-company context, CompanyId, no cross-company leaks | ✅ MULTI-TENANT SAFE |
| `cursor/SETTINGS_UI_RULESET.md` | Settings UI checklist; includes "No companyId references (multi-company removed)" | ⚠️ NEEDS REVIEW |
| Other `cursor/*.md` (SI_STRATEGY, DOCS_GOVERNANCE, FRONTEND_* etc.) | Various prompts/rulesets; not scanned for tenant wording | — |

*Note: `.cursor/cursor.json` references rule paths under `cursor/` (e.g. `cursor/CURSOR_ONBOARDING_PROMPT.md`). Those resolve to the repo-root `cursor/` folder.*

---

## 2️⃣ Rules That Still Assume Single-Company Architecture

### `.cursorrules` — **⚠️ SINGLE-TENANT ASSUMPTION**

**Section "0️⃣ Single Company, Multi-Department Mode"** explicitly enforces the old model:

- **"CephasOps now operates in single-company mode. All previous multi-company guidance is superseded."**
- **"Assume one global company context; do NOT implement company switching, company filters, or per-company isolation layers."**
- **"Where legacy docs mention CompanyId, treat it as the implicit root company identifier; keep fields for backward compatibility but do not add new user flows for multi-company management unless explicitly requested."**
- **"When docs reference 'multi-company' handling, reinterpret those steps for single-company with multiple departments unless a user story explicitly reintroduces multi-company requirements."**

**Why this is unsafe for multi-tenant SaaS:**

- Directly contradicts `.cursor/rules/00_no_manual_scope.mdc`, `01_cephas_precheck.mdc`, `02_tenant_safety.mdc`, `03_backend_workers.mdc`, and `04_code_review_guard.mdc`, which require tenant scope, TenantScopeExecutor, CompanyId propagation, and no bypass of TenantSafetyGuard.
- Instructs the agent to *reinterpret* multi-company guidance as single-company and to avoid company filters and per-company isolation — which undermines tenant isolation.
- "One global company context" and "do NOT implement company filters" conflict with tenant-scoped APIs, EF global query filters, and TenantGuardMiddleware behaviour.

**Risk:** Any agent or MCP that follows `.cursorrules` first may downgrade tenant safety or ignore TenantScopeExecutor/guard rules.

---

### `cursor/SETTINGS_UI_RULESET.md` — **⚠️ POSSIBLE SINGLE-TENANT ASSUMPTION**

- **Checklist item:** "No `companyId` references (multi-company removed)".

**Why it needs review:**

- In a multi-tenant SaaS, settings can be global (platform) or per-tenant (company-level). The phrase "multi-company removed" can be read as "we removed multi-company from the product" (single-tenant assumption) rather than "this specific UI has no company selector."
- If the intent is "settings UI must not expose raw companyId in this ruleset," it should be reworded so it does not imply product-wide single-company.

**Recommendation:** Clarify intent: either "Settings UI does not include a company switcher (tenant is from auth context)" or "No stray companyId in settings components; tenant comes from TenantProvider/context."

---

## 3️⃣ Rules Already SaaS / Multi-Tenant Safe

- **Tenant scope and executor:**  
  `00_no_manual_scope.mdc` — No manual `TenantScope.CurrentTenantId` or Enter/ExitPlatformBypass; runtime must use TenantScopeExecutor only (except DatabaseSeeder and ApplicationDbContextFactory).

- **Pre-coding and review:**  
  `01_cephas_precheck.mdc`, `04_code_review_guard.mdc` — Require tenant ownership, correct TenantScopeExecutor method, guards (TenantSafetyGuard, SiWorkflowGuard, FinancialIsolationGuard, EventStoreConsistencyGuard), and reject tenant-owned writes without CompanyId and unjustified platform bypass.

- **Tenant safety:**  
  `02_tenant_safety.mdc` — Tenant-owned writes inside valid tenant scope; TenantScopeExecutor as boundary; TenantSafetyGuard; CompanyId propagation for jobs/events/webhooks; reject silent fallback to platform bypass.

- **Workers and hosted services:**  
  `03_backend_workers.mdc` — Always use TenantScopeExecutor; explain where CompanyId comes from and whether execution is tenant- or platform-owned.

- **Documentation reference:**  
  `05_docs_reference.mdc` — Points to `backend/docs/architecture/` tenant-safety and executor docs as primary references.

- **Prompts:**  
  `pre_coding_review.md`, `implementation_plan.md`, `final_review.md` — All require tenant/platform ownership, TenantScopeExecutor method, CompanyId propagation, null-company behaviour, and tests.

- **Cursor onboarding and deltas:**  
  `cursor/CURSOR_ONBOARDING_PROMPT.md` — "Follow multi-company boundaries everywhere"; "Skip CompanyId filters" in NEVER list; "Use multi-company isolation for ALL data access"; entities and queries must include/filter by CompanyId.  
  `cursor/BACKEND FEATURE DELTA PROMPT.md`, `cursor/FRONTEND FEATURE DELTA PROMPT.md`, `cursor/ONBOARDING_SI.md`, `cursor/CHECK_BACKEND_COMPLETION.md` — Require multi-company filters, company/department context, or multi-company isolation; no cross-company data leaks.

- **AGENTS.md:**  
  Describes CephasOps as a "multi-company ISP operations management platform"; no single-company mandate.

- **Neutral rules (no tenant/company policy):**  
  `postgress.mdc`, `dotnet-version.mdc`, `ask.mdc`, `documentation-dates.mdc`, `architecture-guardrails.mdc`, `ef-migration-governance.mdc` — Do not contradict multi-tenant architecture.

---

## 4️⃣ Recommended Changes (Do Not Apply in This Audit)

1. **`.cursorrules`**
   - **Remove or replace** the entire section "0️⃣ Single Company, Multi-Department Mode" so it no longer states that CephasOps operates in single-company mode or that multi-company guidance is superseded.
   - **Align** with multi-tenant SaaS: e.g. "CephasOps is a multi-tenant SaaS platform. Respect tenant isolation, TenantScopeExecutor, and architecture rules in `.cursor/rules/` and backend/docs/architecture/. CompanyId and tenant scope are required for tenant-owned data and operations."
   - **Do not** instruct agents to reinterpret multi-company docs as single-company or to avoid company filters / per-company isolation.

2. **`cursor/SETTINGS_UI_RULESET.md`**
   - **Clarify** the checklist item "No `companyId` references (multi-company removed)" so it does not imply product-wide single-company. Options:
     - State that tenant context comes from auth/TenantProvider and settings UI does not add a company switcher, or
     - State that components in this ruleset must not hardcode or expose companyId inappropriately (tenant from context).

3. **Documentation referenced by rules**
   - `05_docs_reference.mdc` already points to tenant-safety docs; no change needed for the rule itself.
   - **Awareness:** Operational/architecture docs elsewhere (e.g. `docs/architecture/00_company-systems-overview.md`, `docs/03_business/STORYBOOK.md`, `docs/02_modules/companies/WORKFLOW.md`) still contain phrases like "Single Company", "single company mode", "the company". Updating those docs for multi-tenant language is a separate documentation task; this audit does not modify them.

4. **Priority**
   - **High:** Update `.cursorrules` so it no longer enforces single-company mode and does not conflict with `.cursor/rules/` tenant safety.
   - **Low:** Clarify `cursor/SETTINGS_UI_RULESET.md` checklist item to avoid ambiguity.

---

## Appendix: Operational Documentation with Single-Company Language (Awareness Only)

The following docs (outside `.cursor/rules/` and `cursor/`) contain "single company", "the company", or similar wording. They were not modified in this audit; they are listed so that future doc and rule updates can align them with multi-tenant messaging where appropriate.

| Doc | Example wording |
|-----|------------------|
| `docs/architecture/00_company-systems-overview.md` | "CephasOps (Single Company)", "Single Company Mode" |
| `docs/03_business/STORYBOOK.md` | "Single Company with Multiple Departments", "one global company", "this single company" |
| `docs/architecture/README.md` | "Single company with multiple departments" |
| `docs/02_modules/companies/WORKFLOW.md` | "Only a single company is allowed", "Single company always active" |
| `docs/overview/product_overview.md` | "Single company, multi-department" |
| `docs/07_frontend/ui/COMPANY_SETTINGS.md` | "Single company only (no dropdown)" |
| `docs/saas/PROVISIONING_FLOW.md` | "only a single company" (in context of CompanyService.CreateAsync) |
| `docs/operations/POST_COMPANY_MIGRATION_REGRESSION_AUDIT.md` | "Single-company legacy", "Only a single company is allowed" |
| Others (see grep results in audit) | Various "the company", "single company" references |

---

## Resolution Applied (2026-03-13)

The outdated single-company rule has been replaced with multi-tenant SaaS rules.

**Changes made:**

1. **`.cursorrules`**  
   - **Removed:** The entire section **"0️⃣ Single Company, Multi-Department Mode"** (single-company mode, one global company context, no company switching/filters, reinterpret multi-company as single-company).  
   - **Added:** Section **"0️⃣ Multi-Tenant SaaS Mode"** stating that CephasOps is a multi-tenant SaaS platform with tenant-isolated data; rules for TenantId/CompanyId filtering, cross-tenant read/write behaviour, TenantScope/ITenantProvider, safe failure on missing tenant context, and explicit TenantScopeExecutor bypass for platform-wide operations; and "Never assume" single company, global company access, optional companyId, or unrestricted data access.  
   - **Updated:** In "1️⃣ Global Workspace Rules", the bullet that instructed reinterpreting multi-company handling as single-company was replaced with: "Respect multi-tenant boundaries; follow .cursor/rules/ and backend/docs/architecture/ for tenant scope and isolation."

2. **`cursor/SETTINGS_UI_RULESET.md`**  
   - **Replaced:** The checklist item "No `companyId` references (multi-company removed)" with: "CompanyId should not be user-selectable in UI. Tenant context is derived from authentication. The system remains multi-tenant but does not allow manual company switching in the UI."

3. **Rules alignment verified**  
   - `.cursor/rules/architecture-guardrails.mdc`, `ef-migration-governance.mdc`, `dotnet-version.mdc`, and `.cursor/rules/00`–`05` remain unchanged and are compatible with multi-tenant SaaS. They already reference or enforce tenant-safe operations where applicable (TenantScopeExecutor, TenantSafetyGuard, CompanyId, backend/docs/architecture tenant-safety docs).

**End of audit. Resolution applied as above; application code and database schema were not modified.**

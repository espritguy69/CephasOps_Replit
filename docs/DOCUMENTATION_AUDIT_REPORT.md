# CephasOps Documentation Audit Report
**Date:** April 10, 2026  
**Auditor:** Architecture Audit Agent  
**Scope:** Full `/docs` validation against actual codebase, configs, scripts, database artifacts, deployment files, and project structure

---

## 1. DOCUMENTATION COVERAGE SUMMARY

### What /docs Covers Well
- **System Architecture**: `01_system/ARCHITECTURE_BOOK.md` and `TECHNICAL_ARCHITECTURE.md` accurately describe the Clean Architecture pattern, 4-layer structure, and project references
- **Tenant Safety**: `tenant_boundary_tests/` is comprehensive and truthful — all claims verified against analyzers, CI workflows, and controller implementations
- **Module Documentation**: `02_modules/` covers all 14 core modules with accurate service/controller/entity mappings
- **Frontend Strategy**: `07_frontend/FRONTEND_STRATEGY.md` correctly describes the shadcn/ui + Tailwind + React Query + Syncfusion stack
- **SI App**: Both PWA (`frontend-si/`) and Native (`si-mobile/`) are documented with accurate routing and feature descriptions
- **Infrastructure Scripts**: All referenced paths in deployment scripts exist; CI/CD workflow references are valid

### What Areas Are Weak or Missing
- **API Documentation**: Only ~50% of 132 controllers are documented in `04_api/`
- **Data Model**: Entity docs omit soft-delete fields, tenant hierarchy changes, and 10+ new entities
- **Root-Level Chaos**: 154 loose markdown files at `/docs/` root — most are transient audit/phase reports that should be in subdirectories
- **Deployment Docs**: `08_infrastructure/` is high-level; doesn't mirror the detailed native VPS steps in `deploy-vps-native.sh`
- **Supabase References**: 7 docs still reference Supabase despite migration to self-hosted PostgreSQL

---

## 2. FINDINGS BY SEVERITY

### CRITICAL

| # | Finding | Details |
|---|---------|---------|
| **F1** | **Tailwind version wrong in TECH_STACK.md** | Docs say `3.4.18`, actual `package.json` has `4.1.17` — completely different major version with breaking API changes |
| **F2** | **7 docs still reference Supabase** | Project migrated to self-hosted PostgreSQL. Files: `TECH_STACK.md`, `TECHNICAL_ARCHITECTURE.md`, `SYSTEM_OVERVIEW.md`, `MASTER_PRD.md`, `00_company-systems-overview.md`, `QUICK_COMMANDS.md`, `EMAIL_PARSER_E2E_TEST_GUIDE.md` |
| **F3** | **NotificationTemplate & ReportDefinition undocumented AND unimplemented** | Entities exist in Domain but have no DbSet, no migration, no table — service calls will crash at runtime. Docs don't mention this known gap |
| **F4** | **50%+ of API controllers undocumented** | 132 controllers exist; `04_api/` covers roughly 60. Missing: Analytics, GPON rates, SMS/WhatsApp, Master Data (Skills, Teams, Warehouses, Vendors, Verticals, TaxCodes), and more |
| **F5** | **Tenant hierarchy not in canonical docs** | Code has `Tenant → Company → Department` hierarchy. Core docs (`01_system/`, `05_data_model/`) still describe flat `CompanyId` scoping. Some phase docs (e.g., `PHASE_11_TENANT_ISOLATION.md`) reference it, but it's absent from the primary architecture references |

### HIGH

| # | Finding | Details |
|---|---------|---------|
| **F6** | **154 root-level docs are uncategorized** | 90% should be in subdirectories — Phase summaries, audit reports, runbooks all dumped at root |
| **F7** | **Data model docs in `05_data_model/` missing 10+ entities** | `Tenant`, `TenantActivityEvent`, `TenantOnboardingProgress`, `OperationalInsight`, `MigrationAudit`, `LedgerBalanceCache`, `StockByLocationSnapshot`, `MaterialPartner`, `BaseWorkRate`, `GponSiJobRate`, `OrderFinancialAlert`, `ExternalIdempotencyRecord` — all in code, not in `05_data_model/` (some may be referenced in module-specific or phase docs elsewhere) |
| **F8** | **Entity docs omit infrastructure fields** | Most `CompanyScopedEntity` subclasses now have `IsDeleted`, `DeletedAt`, `DeletedByUserId`, `RowVersion` — the `05_data_model/` entity docs don't describe these standard fields (note: not all entities inherit these — e.g., `Tenant` uses `BaseEntity`) |
| **F9** | **Order entity docs incomplete** | `orders_entities.md` omits Relocation fields (`OldAddress`, `RelocationType`), Network/VOIP fields (`NetworkPackage`, `VoipPassword`), and Splitter fields |
| **F10** | **RMA naming inconsistency** | Docs say `RmaTicket`, code uses `RmaRequest` and `RmaRequestItem` |
| **F11** | **SI App dual-implementation confusion** | `frontend-si/` (PWA) and `si-mobile/` (React Native) serve the same users but docs don't clearly distinguish which is which or why both exist |
| **F12** | **API_CONTRACTS_SUMMARY.md is misleading** | Suggests specific status endpoints (`/start-otw`, `/met`) but code uses single generic `POST /api/orders/{id}/status` |

### MEDIUM

| # | Finding | Details |
|---|---------|---------|
| **F13** | **Offline-first claim contradicted** | `SI_APP_STRATEGY.md` says "Offline-first core principle". `SI_APP_OVERVIEW.md` correctly says "not yet implemented". Contradictory |
| **F14** | **ExcelDataReader claimed removed** | `TECH_STACK.md` says ExcelDataReader replaced by Syncfusion. `CephasOps.Api.csproj` still has `ExcelDataReader 3.6.0` |
| **F15** | **Syncfusion version inconsistency** | Docs list `31.1.17`. Backend uses mix of `31.1.17` and `31.2.15`. Frontend packages range `31.1.17` to `31.2.16` |
| **F16** | **08_infrastructure/ lacks VPS deployment guide** | The actual `deploy-vps-native.sh` is comprehensive but has no matching documentation in `08_infrastructure/` |
| **F17** | **Duplicate module folders** | `docs/02_modules/department/` AND `docs/02_modules/departments/` both exist |
| **F18** | **QuestPDF location unclear** | `TECH_STACK.md` mentions QuestPDF but doesn't specify which project layer contains it |
| **F19** | **RBAC docs claim dedicated service** | Docs describe `IRbacService`. Code splits logic across `AuthService`, `PermissionAuthorizationHandler`, and `UserPermissionProvider` |
| **F20** | **KPI evaluation logic scattered** | Docs describe `KpiEvaluationService`. Actual logic embedded in `OrderService` and `PayrollService` |

### LOW

| # | Finding | Details |
|---|---------|---------|
| **F21** | **Redundant archive content** | `docs/archive/` has 50 files that overlap with root-level docs |
| **F22** | **`_templates/doc_template.md` exists but unused** | Template for new docs exists but most recent docs don't follow it |
| **F23** | **`00_QUICK_NAVIGATION.md` may be stale** | 255-line navigation file — needs validation against current doc structure |

---

## 3. FILE-BY-FILE AUDIT (Major Documents)

### 01_system/

| File | Purpose | Status | Action |
|------|---------|--------|--------|
| `ARCHITECTURE_BOOK.md` | Comprehensive architecture reference | **Accurate** | Minor: update Tenant hierarchy |
| `TECHNICAL_ARCHITECTURE.md` | Technical stack & patterns | **Outdated** | Fix: remove Supabase refs, update Tailwind version |
| `SYSTEM_OVERVIEW.md` | High-level system overview | **Outdated** | Fix: remove Supabase refs |
| `TECH_STACK.md` | Technology versions | **Outdated** | Fix: Tailwind 3→4, remove Supabase, clarify ExcelDataReader |
| `EMAIL_PIPELINE.md` | Email parsing flow | **Accurate** | None |
| `MULTI_COMPANY_ARCHITECTURE.md` | Multi-tenant design | **Incomplete** | Add: Tenant→Company→Department hierarchy |
| `ORDER_LIFECYCLE.md` | Order state machine | **Accurate** | None |
| `WORKFLOW_ENGINE.md` | Workflow engine design | **Accurate** | None |
| `SERVER_STATUS.md` | Deployment status | **Outdated** | Update with current VPS status |

### 02_modules/ (Spot Check)

| Module Folder | Status | Notes |
|---------------|--------|-------|
| `orders/` | **Accurate** | Missing: Relocation & Network fields |
| `billing/` | **Accurate** | MyInvois integration well-documented |
| `email_parser/` | **Accurate** | 11 docs — most thorough module |
| `inventory/` | **Accurate** | Missing: `LedgerBalanceCache`, `StockByLocationSnapshot` |
| `rbac/` | **Incomplete** | Claims dedicated service that doesn't exist |
| `workflow/` | **Accurate** | Well-maintained |
| `notifications/` | **Incomplete** | NotificationTemplate entity missing from DB |
| `department/` | **Duplicate** | Also exists as `departments/` |
| `partners/` | **Accurate** | None |
| `scheduler/` | **Accurate** | None |
| `rma/` | **Inaccurate** | Uses `RmaTicket` name, code uses `RmaRequest` |

### 04_api/

| File | Status | Notes |
|------|--------|-------|
| `API_CONTRACTS_SUMMARY.md` | **Outdated** | Covers ~30% of actual controllers, wrong endpoint paths |
| `PHASE2_SETTINGS_API.md` | **Accurate** | SLA, Automation, Approval — all verified |
| Other API docs | **Missing** | 70+ controllers have no documentation |

### 05_data_model/

| Status | Notes |
|--------|-------|
| **Partially Accurate** | Core entities correct. Missing: Tenant entities, soft-delete fields, 10+ new entities, infrastructure columns |

### 07_frontend/

| File | Status | Notes |
|------|--------|-------|
| `FRONTEND_STRATEGY.md` | **Accurate** | shadcn/ui + TanStack Query confirmed |
| `SI_APP_OVERVIEW.md` | **Accurate** | 1:1 match with `frontend-si/` code |
| `SI_APP_STRATEGY.md` | **Contradictory** | Claims offline-first which isn't implemented |
| `TAILWIND4_GAP_ANALYSIS.md` | **Accurate** | Correctly identifies migration path |

### 08_infrastructure/

| Status | Notes |
|--------|-------|
| **Incomplete** | High-level only. Missing: dedicated VPS deployment guide, systemd setup, nginx config documentation |

---

## 4. CODE-VS-DOCS MISMATCHES

| # | Doc Claim | Code Reality |
|---|-----------|-------------|
| 1 | Tailwind CSS 3.4.18 | Tailwind CSS 4.1.17 |
| 2 | PostgreSQL via Supabase | Self-hosted PostgreSQL on Debian 13 VPS |
| 3 | ExcelDataReader removed | Still in `CephasOps.Api.csproj` v3.6.0 |
| 4 | Flat `CompanyId` scoping | `Tenant → Company → Department` hierarchy |
| 5 | `RmaTicket` entity | `RmaRequest` + `RmaRequestItem` |
| 6 | `IRbacService` dedicated service | Split across `AuthService` + `PermissionAuthorizationHandler` |
| 7 | `KpiEvaluationService` standalone | Logic embedded in `OrderService`/`PayrollService` |
| 8 | Specific order status endpoints | Single generic `POST /api/orders/{id}/status` |
| 9 | `SettingsController` | Split into `GlobalSettingsController` + `IntegrationSettingsController` + entity-specific controllers |
| 10 | `NotificationTemplate` table exists | No DbSet, no migration, no table — runtime crash |
| 11 | `ReportDefinition` table exists | No DbSet, no migration, no table |
| 12 | Offline-first SI App | Not implemented — explicitly noted as future enhancement |
| 13 | `GET /api/auth/switch-company/{companyId}` | Commented out in `AuthController.cs` |

---

## 5. MISSING DOCUMENTATION

| Area | What's Missing |
|------|---------------|
| **VPS Deployment Guide** | Step-by-step guide matching `deploy-vps-native.sh` |
| **Tenant Hierarchy** | `Tenant → Company → Department` relationship doc |
| **New Entities** | 12 entities added without any documentation |
| **Master Data Controllers** | Skills, Teams, Warehouses, Vendors, Verticals, TaxCodes |
| **Messaging APIs** | SMS, WhatsApp, Messaging controllers |
| **Platform Analytics** | `PlatformAnalyticsController`, `OperationalInsightsController`, `ObservabilityController` |
| **GPON Rates** | `GponBaseWorkRatesController` |
| **Financial Alerts** | `FinancialAlertsController` |
| **Soft Delete Strategy** | How `IsDeleted`, `DeletedAt`, `RowVersion` work across all entities |
| **Idempotency** | `ExternalIdempotencyRecord` usage pattern |
| **Known Architecture Gaps** | Missing `.sln`, broken analyzer reference, Application→Infrastructure violation |

---

## 6. STRUCTURAL ISSUES

### Current Root Mess (154 files)
Files that should be relocated:

| Pattern | Count | Target |
|---------|-------|--------|
| `WORKFLOW_*` | 14 | `docs/02_modules/workflow/` |
| `SLA_*` | 3 | `docs/02_modules/sla-configuration/` |
| `UI_CONSISTENCY_*` | 3 | `docs/07_frontend/` |
| `TRACE_EXPLORER_*` | 3 | `docs/operations/` |
| `SERVICE_PROFILE_*` | 3 | `docs/02_modules/service_installer/` |
| `DISTRIBUTED_PLATFORM_*` | 15 | `docs/archive/distributed-platform/` |
| `PHASE_*_*` | 20+ | `docs/archive/deliverables/` |
| Module-specific audits | 30+ | Respective `docs/02_modules/` folders |
| Runbooks | 5+ | `docs/operations/` |

### What Should Remain at Root
- `00_QUICK_NAVIGATION.md` (updated)
- `DOCUMENTATION_AUDIT_REPORT.md` (this file)
- `COMPLETION_STATUS_REPORT.md` (if current)

---

## 7. RECOMMENDED DOCUMENTATION STRUCTURE

```
docs/
├── 00_QUICK_NAVIGATION.md          # Updated index
├── DOCUMENTATION_AUDIT_REPORT.md   # This audit
├── 01_system/                       # Architecture, tech stack, overview
├── 02_modules/                      # Per-module docs (orders, billing, etc.)
├── 03_business/                     # PRD, business rules
├── 04_api/                          # API contracts (needs major expansion)
├── 05_data_model/                   # Entity docs (needs update)
│   ├── entities/
│   └── relationships/
├── 06_ai/                           # AI/automation docs
├── 07_frontend/                     # Frontend strategy, SI apps
│   ├── si_app/
│   └── ui/
├── 08_infrastructure/               # Deployment, CI/CD, VPS guide
├── 09_operations/                   # Runbooks, maintenance, monitoring
│   ├── runbooks/                    # SLA, Trace, Event Bus runbooks
│   └── maintenance/                 # Fix scripts, data repair
├── 10_security/                     # Tenant safety, auth, secret mgmt
├── 11_known_gaps/                   # Architecture debt, known issues
├── archive/                         # Historical phase summaries
│   ├── deliverables/                # Phase 1-15 summaries
│   ├── distributed-platform/        # Platform migration docs
│   └── event_bus/
└── _templates/                      # Doc templates
```

---

## 8. FINAL VERDICT

### Status: PARTIALLY TRUSTWORTHY

**Explanation:**

The `/docs` folder contains genuinely valuable documentation. The system architecture docs (`01_system/`) and tenant safety docs are **highly accurate** and reflect real implementation. Module documentation (`02_modules/`) is **mostly accurate** for the 14 core modules.

However, the documentation is **unreliable** in these specific areas:
1. **Technology versions** — Tailwind major version wrong, Supabase references stale
2. **API coverage** — Over 70 controllers undocumented
3. **Data model** — Missing entities, wrong entity names, omitted fields
4. **Structural organization** — 154 root-level files make navigation nearly impossible
5. **Known gaps not disclosed** — Missing DbSets, broken analyzer, architecture violations not documented anywhere

**Bottom Line:** A developer relying solely on `/docs` would get the architectural intent right but would be misled on specific versions, API endpoints, entity shapes, and deployment details. The documentation needs a focused cleanup pass — not a rewrite — to become fully trustworthy.

### Priority Actions — REMEDIATION STATUS (April 2026)

| # | Action | Status |
|---|--------|--------|
| 1 | Fix `TECH_STACK.md` — Tailwind 4, remove Supabase, clarify ExcelDataReader | **DONE** |
| 2 | Relocate 100+ root files into proper subdirectories | **DONE** (154 → 3 root files) |
| 3 | Expand `04_api/` to cover all 132 controllers | **PARTIAL** — added `UNDOCUMENTED_CONTROLLERS.md` index |
| 4 | Update `05_data_model/` with missing entities and infrastructure fields | **DONE** — added `tenant_entities.md`, `operational_entities.md` |
| 5 | Add `11_known_gaps/` section documenting architecture debt | **DONE** — 3 gap files created |
| 6 | Create VPS deployment guide in `08_infrastructure/` | **DONE** |

### Additional Fixes Applied
- Fixed `TECHNICAL_ARCHITECTURE.md` — removed Supabase, added tenant hierarchy, noted Application→Infrastructure gap
- Fixed `SYSTEM_OVERVIEW.md` — replaced Supabase references
- Fixed `MASTER_PRD.md` — updated database description
- Fixed `QUICK_COMMANDS.md` — removed hardcoded Supabase credentials, added migration warning
- Fixed `EMAIL_PARSER_E2E_TEST_GUIDE.md` — removed Supabase reference
- Fixed `00_company-systems-overview.md` — updated database description in diagram
- Fixed `SI_APP_STRATEGY.md` — marked offline-first as NOT YET IMPLEMENTED
- Fixed `inventory_entities.md` — corrected RmaTicket → RmaRequest naming
- Merged duplicate `departments/` into `department/`
- Created `01_system/TENANT_HIERARCHY.md` — full tenant isolation documentation
- Rewrote `00_QUICK_NAVIGATION.md` with verification status legend

### Updated Verdict: MOSTLY TRUSTWORTHY

After remediation, `/docs` is now **mostly trustworthy**:
- Core architecture docs are **verified and corrected**
- Known gaps are **honestly documented** in `11_known_gaps/`
- Root-level clutter is **eliminated** (154 → 3 files)
- Stale Supabase references are **removed** from all active docs
- Remaining gap: API coverage still at ~50% (tracked in `UNDOCUMENTED_CONTROLLERS.md`)

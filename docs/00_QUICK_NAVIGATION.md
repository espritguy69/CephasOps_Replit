# CephasOps Documentation — Quick Navigation Guide

**Last Updated:** April 2026  
**Total Documents:** 750+ MD files across organized categories  
**Audit Status:** Remediated — see `DOCUMENTATION_AUDIT_REPORT.md`

---

## FOLDER STRUCTURE

```
/docs
├── 00_QUICK_NAVIGATION.md          ← This file
├── DOCUMENTATION_AUDIT_REPORT.md   ← Latest audit findings
├── README.md                        ← Start here
├── 01_system/                       ← System architecture, tech stack, tenant model [VERIFIED]
├── 02_modules/                      ← Feature modules (30+ modules) [VERIFIED]
├── 03_business/                     ← Business rules, PRD, policies [VERIFIED]
├── 04_api/                          ← API documentation [PARTIALLY VERIFIED — ~50% coverage]
├── 05_data_model/                   ← Database entities & relationships [UPDATED]
├── 06_ai/                           ← AI prompts & developer guides [VERIFIED]
├── 07_frontend/                     ← Frontend strategy, UI docs [VERIFIED]
├── 08_infrastructure/               ← DevOps, deployment, VPS guide [UPDATED]
├── 09_operations/                   ← Runbooks, monitoring, migrations
│   └── runbooks/                    ← Operational runbooks
├── 11_known_gaps/                   ← Known architecture & database risks [NEW]
├── architecture/                    ← Architecture intelligence, watchdog reports
├── event-platform/                  ← Event bus, domain events, connectors
├── launch_readiness/                ← Go-live checklists, completion status
├── saas/                            ← Multi-tenant SaaS model docs
├── saas_readiness/                  ← SaaS readiness assessments
├── tenant_boundary_tests/           ← Tenant safety test documentation
├── archive/                         ← Historical phase summaries and deliverables
│   ├── deliverables/                ← Phase 6-15 summaries
│   └── distributed-platform/       ← Distributed platform migration docs
└── _templates/                      ← Doc templates
```

---

## QUICK LINKS BY TOPIC

### Start Here
- [README](./README.md) — Project overview
- [Documentation Audit Report](./DOCUMENTATION_AUDIT_REPORT.md) — Latest audit findings and remediation status

### System Architecture [VERIFIED]
- [Architecture Book](./01_system/ARCHITECTURE_BOOK.md) — Comprehensive reference
- [Technical Architecture](./01_system/TECHNICAL_ARCHITECTURE.md) — Clean Architecture layers
- [System Overview](./01_system/SYSTEM_OVERVIEW.md) — End-to-end journey and diagrams
- [Tech Stack](./01_system/TECH_STACK.md) — Technology versions (audited April 2026)
- [Tenant Hierarchy](./01_system/TENANT_HIERARCHY.md) — Tenant → Company → Department model [NEW]
- [Multi-Company Architecture](./01_system/MULTI_COMPANY_ARCHITECTURE.md) — Multi-tenant design
- [Workflow Engine](./01_system/WORKFLOW_ENGINE.md) — State machine and guard conditions
- [Email Pipeline](./01_system/EMAIL_PIPELINE.md) — Email ingestion flow
- [Order Lifecycle](./01_system/ORDER_LIFECYCLE.md) — 17-status order workflow

### Known Gaps [NEW — Read These]
- [Architecture Gaps](./11_known_gaps/ARCHITECTURE_GAPS.md) — 9 unresolved architecture risks
- [Database Gaps](./11_known_gaps/DATABASE_GAPS.md) — 6 database/schema risks (includes critical missing DbSets)
- [Documentation Limitations](./11_known_gaps/DOCUMENTATION_LIMITATIONS.md) — 7 documentation coverage gaps

### Core Modules [VERIFIED]

#### Orders & Scheduling
- [Orders Module](./02_modules/orders/OVERVIEW.md)
- [Scheduler Module](./02_modules/scheduler/OVERVIEW.md)
- [Tasks Module](./02_modules/tasks/OVERVIEW.md)

#### Email Parser
- [Email Parser Overview](./02_modules/email_parser/OVERVIEW.md)
- [Email Parser Setup](./02_modules/email_parser/SETUP.md)

#### Financial
- [Rate Engine](./02_modules/rate_engine/RATE_ENGINE.md)
- [GPON Rate Cards](./02_modules/gpon/GPON_RATECARDS.md)
- [Billing Module](./02_modules/billing/OVERVIEW.md)
- [P&L Module](./02_modules/pnl/OVERVIEW.md)
- [Payroll Module](./02_modules/payroll/OVERVIEW.md)
- [Partner Module](./02_modules/partners/OVERVIEW.md)

#### Inventory & Assets
- [Inventory Module](./02_modules/inventory/OVERVIEW.md)
- [Reports Hub](./02_modules/reports_hub/OVERVIEW.md)
- [Splitters](./02_modules/splitters/OVERVIEW.md)

#### Organization
- [Department Module](./02_modules/department/OVERVIEW.md)
- [Companies Setup](./02_modules/global_settings/COMPANIES_SETUP.md)
- [RBAC Module](./02_modules/rbac/OVERVIEW.md)

#### Settings & Configuration
- [Global Settings](./02_modules/global_settings/GLOBAL_SETTINGS_MODULE.md)
- [Notifications](./02_modules/notifications/OVERVIEW.md)
- [Document Generation](./02_modules/document_generation/OVERVIEW.md)

### API Documentation [PARTIALLY VERIFIED]
- [API Contracts Summary](./04_api/API_CONTRACTS_SUMMARY.md) — Legacy high-level index
- [Phase 2 Settings API](./04_api/PHASE2_SETTINGS_API.md) — SLA, Automation, Approval APIs
- [Undocumented Controllers](./04_api/UNDOCUMENTED_CONTROLLERS.md) — Index of 30+ controllers needing docs [NEW]

### Data Model [UPDATED]
- [Data Model Index](./05_data_model/DATA_MODEL_INDEX.md)
- [Entities](./05_data_model/entities/) — Entity definitions
  - [Tenant Entities](./05_data_model/entities/tenant_entities.md) [NEW]
  - [Operational Entities](./05_data_model/entities/operational_entities.md) [NEW]
- [Relationships](./05_data_model/relationships/)

### Frontend [VERIFIED]
- [Frontend Strategy](./07_frontend/FRONTEND_STRATEGY.md) — shadcn/ui + Tailwind 4 + Syncfusion
- [SI App Overview](./07_frontend/si_app/SI_APP_STRATEGY.md) — Mobile SI app strategy

### Infrastructure [UPDATED]
- [VPS Deployment Guide](./08_infrastructure/VPS_DEPLOYMENT_GUIDE.md) — Full native deployment steps [NEW]
- [Testing Setup](./08_infrastructure/TESTING_SETUP.md)

### Operations
- [Runbooks](./09_operations/runbooks/) — Operational procedures
- [Migration Notes](./09_operations/) — Migration phase notes

### SaaS & Tenant Safety [VERIFIED]
- [Tenancy Model](./saas/TENANCY_MODEL.md)
- [Tenant Safety Guards](./tenant_boundary_tests/TENANT_SAFETY_GUARDS.md)
- [Tenant Safety CI](./tenant_boundary_tests/TENANT_SAFETY_CI_ENFORCEMENT.md)

### Architecture Intelligence
- [Codebase Intelligence Map](./architecture/CODEBASE_INTELLIGENCE_MAP.md) — Module/controller/service map
- [Architecture Watchdog](./architecture/ARCHITECTURE_WATCHDOG_REPORT.md)
- [Refactor Safety Report](./architecture/REFACTOR_SAFETY_REPORT.md)

---

## HOW TO FIND WHAT YOU NEED

| Question | Go To |
|----------|-------|
| "How does the system work?" | [System Overview](./01_system/SYSTEM_OVERVIEW.md) |
| "What's the tech stack?" | [Tech Stack](./01_system/TECH_STACK.md) |
| "How does tenant isolation work?" | [Tenant Hierarchy](./01_system/TENANT_HIERARCHY.md) |
| "What's broken or risky?" | [Known Gaps](./11_known_gaps/) |
| "How do I deploy to VPS?" | [VPS Deployment Guide](./08_infrastructure/VPS_DEPLOYMENT_GUIDE.md) |
| "I need a specific module's docs" | [02_modules/](./02_modules/) |
| "What APIs exist?" | [04_api/](./04_api/) |
| "How do I work on the frontend?" | [Frontend Strategy](./07_frontend/FRONTEND_STRATEGY.md) |

---

## VERIFICATION STATUS LEGEND

| Status | Meaning |
|--------|---------|
| **VERIFIED** | Audited against code in April 2026 — accurate |
| **UPDATED** | Corrected/expanded during April 2026 remediation |
| **PARTIALLY VERIFIED** | Some gaps remain — see Known Gaps |
| **NEW** | Created during April 2026 remediation |
| **ARCHIVED** | Moved to `archive/` — historical reference only |

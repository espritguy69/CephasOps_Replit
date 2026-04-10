# CephasOps Documentation - Complete Index

**Last Updated**: February 2026  
**Organization**: Modular structure (01_system through 08_infrastructure, architecture); plus source-of-truth–aligned folders (overview, business, integrations, operations, dev).  
**Doc status**: See [DOCS_STATUS.md](./DOCS_STATUS.md). **Docs map**: See [DOCS_MAP.md](./DOCS_MAP.md).

---

## 📋 **Source-of-Truth Aligned Docs** (Feb 2026)

| Document | Description |
|----------|-------------|
| [Docs Map](./DOCS_MAP.md) | Required doc set (A–P); existing vs missing; folder structure |
| [Docs Inventory](./DOCS_INVENTORY.md) | Inventory of existing docs with status (OK/OUTDATED/MISSING) |
| [Product Overview](./overview/product_overview.md) | Business type, core processes, tech stack, operating model |
| [Process Flows](./business/process_flows.md) | End-to-end main flow and side paths |
| [Department & RBAC](./business/department_rbac.md) | Department responsibilities and RBAC model |
| [Order Lifecycle Summary](./business/order_lifecycle_summary.md) | Pointer to ORDER_LIFECYCLE; status summary |
| [Reference Data Taxonomy](./business/reference_data_taxonomy.md) | Settings reference data: Part A (implemented from code/seeds), Part B (suggested only) |
| [SI App Journey](./business/si_app_journey.md) | Installer journey and data captured (GPS, photos, splitter, ONU) |
| [Docket Process](./business/docket_process.md) | Receive → verify → reject → upload |
| [Billing & MyInvois](./business/billing_myinvois_flow.md) | Invoice creation, PDF, e-invoice, rejection/reinvoice |
| [Inventory & Ledger Summary](./business/inventory_ledger_summary.md) | Ledger as source of truth; serialised lifecycle |
| [Payroll & Rate Overview](./business/payroll_rate_overview.md) | SI rate plans; payroll; partner rates |
| [P&L Boundaries](./business/pnl_boundaries.md) | P&L as analytics; not GL |
| [Integrations Overview](./integrations/overview.md) | Email, WhatsApp, SMS, MyInvois, OneDrive, partner portals |
| [Background Jobs](./operations/background_jobs.md) | Job types; hosted services; schedulers |
| [MyInvois Production Runbook](./operations/myinvois_production_runbook.md) | Credentials, submission, polling, rejection handling |
| [Partner Portal Manual Process](./operations/partner_portal_manual_process.md) | Docket/invoice submission to TIME (no API) |
| [Inventory & Ledger Ops Runbook](./operations/inventory_ledger_ops_runbook.md) | Ledger reconciliation, serial lifecycle, RMA |
| [Reporting & P&L Ops Runbook](./operations/reporting_pnl_ops_runbook.md) | P&L rebuild, Reports Hub, KPI, Dashboard |
| [Scope Not Handled](./operations/scope_not_handled.md) | Leads, statutory payroll, offline SI, partner API, etc. |
| [Developer Onboarding](./dev/onboarding.md) | Local setup, env vars, run scripts, architecture map |
| [API Surface Summary](./architecture/api_surface_summary.md) | Controllers grouped by module |
| [Data Model Overview](./architecture/data_model_overview.md) | Key entities and relationships |
| [Implementation Truth Inventory](./DOCS_IMPLEMENTATION_TRUTH_INVENTORY.md) | Schema, backend, frontend snapshot from code/EF (single source for doc alignment) |
| [Completion Status Report](./COMPLETION_STATUS_REPORT.md) | Feature completion matrix; backend/frontend/DB per module; top blockers |
| [Architecture Audit Report](./ARCHITECTURE_AUDIT_REPORT.md) | Code vs docs alignment; drift; module boundaries; verified architectural truths |
| [Go-Live Readiness Checklist (GPON)](./GO_LIVE_READINESS_CHECKLIST_GPON.md) | GPON production readiness checklist |
| [Roadmap to Completion](./ROADMAP_TO_COMPLETION.md) | Phased roadmap (Phase 0–5) to production-ready |
| [Docs Changelog](./CHANGELOG_DOCS.md) | Doc changes tied to source of truth |
| [Discrepancies](./_discrepancies.md) | Audit register: Closed, Open – Must Fix, Accepted Gaps, Deferred |
| [Codebase Intelligence Map](./architecture/CODEBASE_INTELLIGENCE_MAP.md) | Architecture intelligence hub: modules, controllers, services, entities, workers, integrations, flows, hotspots |
| [Codebase Intelligence Report](./CODEBASE_INTELLIGENCE_REPORT.md) | Summary of intelligence layer (Level 13); artifacts; findings; governance updates |
| [Refactor Safety Report](./REFACTOR_SAFETY_REPORT.md) | Level 14 refactor safety audit: coupling, fragility, safe/danger zones, worker risks, sequence plan |
| [Architecture Watchdog Report](./ARCHITECTURE_WATCHDOG_REPORT.md) | Level 15 watchdog: drift, sprawl, dependency leaks, worker coupling, module boundaries |
| [Architecture Governance Systems](./architecture/ARCHITECTURE_GOVERNANCE_SYSTEMS.md) | Index: Change Impact Predictor, Architecture Policy Engine, Auto Documentation Sync, Risk Dashboard, Self-Maintaining Architecture, Portal, Governance logs |

### **Architecture watchdog (Level 15)**

| Document | Description |
|----------|-------------|
| [Service Sprawl Watch](./architecture/service_sprawl_watch.md) | Oversized or centralizing services |
| [Controller Sprawl Watch](./architecture/controller_sprawl_watch.md) | Controller families growing too broad |
| [Dependency Leak Watch](./architecture/dependency_leak_watch.md) | Hidden links, cycles, cross-domain leakage |
| [Worker Coupling Watch](./architecture/worker_coupling_watch.md) | Worker coupling and risk trend |
| [Module Boundary Regression](./architecture/module_boundary_regression.md) | Module boundary status: stable / drifting / high risk |

### **Refactor safety (architecture)**

| Document | Description |
|----------|-------------|
| [High Coupling Modules](./architecture/high_coupling_modules.md) | Modules ranked by coupling risk |
| [Hidden Dependencies](./architecture/hidden_dependencies.md) | Service/service and DbContext cross-access; GetRequiredService |
| [Module Fragility Map](./architecture/module_fragility_map.md) | Per-module fragility (size, coupling, workers, criticality) |
| [Safe Refactor Zones](./architecture/safe_refactor_zones.md) | Lower-risk refactor areas |
| [Refactor Danger Zones](./architecture/refactor_danger_zones.md) | High-risk refactor areas |
| [Refactor Sequence Plan](./architecture/refactor_sequence_plan.md) | Suggested refactor order (least → most critical) |
| [Worker Dependency Risks](./architecture/worker_dependency_risks.md) | Worker service dependencies and hidden coupling |

### **Codebase intelligence (relationship maps)**

| Document | Description |
|----------|-------------|
| [Controller → Service Map](./architecture/controller_service_map.md) | Controllers and main services by domain |
| [Module Dependency Map](./architecture/module_dependency_map.md) | Upstream/downstream dependencies per module |
| [Background Worker Map](./architecture/background_worker_map.md) | Hosted services and job types |
| [Integration Map](./architecture/integration_map.md) | External and internal integrations |
| [Entity Domain Map](./architecture/entity_domain_map.md) | Entities grouped by business area |

---

## 📚 **Quick Navigation**

- [Getting Started](#getting-started)
- [System Architecture](#system-architecture)
- [Feature Modules](#feature-modules)
- [Business Rules](#business-rules)
- [API Documentation](#api-documentation)
- [Data Model](#data-model)
- [AI & Development](#ai--development)
- [Frontend](#frontend)
- [Infrastructure](#infrastructure)
- [Archives](#archives)

---

## 🚀 **Getting Started**

| Document | Description |
|----------|-------------|
| [README.md](./README.md) | Main documentation index |
| [Quick Navigation](./00_QUICK_NAVIGATION.md) | Quick links to all major docs |
| [Quick Start Guide](./06_ai/QUICK_START.md) | Get started in 5 minutes |
| [Developer Guide](./06_ai/DEVELOPER_GUIDE.md) | Complete developer onboarding |
| [Developer Handbook](./06_ai/DEV_HANDBOOK.md) | Development best practices |

---

## 🏗️ **System Architecture**

**Location**: `01_system/`

| Document | Description |
|----------|-------------|
| [System Overview](./01_system/SYSTEM_OVERVIEW.md) | High-level system architecture |
| [Architecture Book](./01_system/ARCHITECTURE_BOOK.md) | Detailed architectural decisions |
| [Order Lifecycle](./01_system/ORDER_LIFECYCLE.md) | Complete order workflow |
| [Email Pipeline](./01_system/EMAIL_PIPELINE.md) | Email processing flow |
| [Workflow Engine](./01_system/WORKFLOW_ENGINE.md) | Workflow state machine |
| [Multi-Company Architecture](./01_system/MULTI_COMPANY_ARCHITECTURE.md) | Multi-tenant design |
| [Server Status](./01_system/SERVER_STATUS.md) | Infrastructure status |

---

## 📦 **Feature Modules**

**Location**: `02_modules/`

### Email Parser
**Folder**: `email_parser/`

| Document | Description |
|----------|-------------|
| [Overview](./02_modules/email_parser/OVERVIEW.md) | Email parser introduction |
| [Setup Guide](./02_modules/email_parser/SETUP.md) | Email account configuration |
| [Specification](./02_modules/email_parser/SPECIFICATION.md) | Parsing rules and logic |
| [Workflow](./02_modules/email_parser/WORKFLOW.md) | Email processing workflow |
| [Building Matching](./02_modules/email_parser/BUILDING_MATCHING.md) | Auto-building resolution |

### Orders
**Folder**: `orders/`

| Document | Description |
|----------|-------------|
| [Overview](./02_modules/orders/OVERVIEW.md) | Orders module specification |
| [Service ID Rules](./02_modules/orders/SERVICE_ID_RULES.md) | Service ID type detection and auto-selection rules |

### Departments
**Folder**: `department/`

| Document | Description |
|----------|-------------|
| [Overview](./02_modules/department/OVERVIEW.md) | Department module specification |
| [Filtering](./02_modules/department/FILTERING.md) | Department filtering logic |

### Rate Engine & GPON
**Folder**: `rate_engine/`, `gpon/`

| Document | Description |
|----------|-------------|
| [Rate Engine Overview](./02_modules/rate_engine/RATE_ENGINE.md) | Universal rate engine |
| [GPON Overview](./02_modules/gpon/GPON_RATECARDS.md) | GPON rate cards |

### Billing & Payroll
**Folder**: `billing/`, `payroll/`, `pnl/`

| Document | Description |
|----------|-------------|
| [Billing Overview](./02_modules/billing/OVERVIEW.md) | Billing & tax module |
| [Payroll Overview](./02_modules/payroll/OVERVIEW.md) | Payroll processing |
| [P&L Overview](./02_modules/pnl/OVERVIEW.md) | Profit & loss tracking |

### Inventory & Materials
**Folder**: `inventory/`, `materials/`, `splitters/`

| Document | Description |
|----------|-------------|
| [Inventory Overview](./02_modules/inventory/OVERVIEW.md) | Inventory & RMA |
| [Materials Overview](./02_modules/materials/OVERVIEW.md) | Material templates |
| [Splitters Overview](./02_modules/splitters/OVERVIEW.md) | Splitter management |

### Reports Hub
**Folder**: `reports_hub/`

| Document | Description |
|----------|-------------|
| [Reports Hub Overview](./02_modules/reports_hub/OVERVIEW.md) | Search, run, and export key reports (orders, materials, stock, ledger, scheduler) |

### Scheduler & Tasks
**Folder**: `scheduler/`, `tasks/`, `workflow/`

| Document | Description |
|----------|-------------|
| [Scheduler Overview](./02_modules/scheduler/OVERVIEW.md) | Job scheduling |
| [Tasks Overview](./02_modules/tasks/OVERVIEW.md) | Task management |

### Partners & Service Installers
**Folder**: `partners/`, `service_installer/`

| Document | Description |
|----------|-------------|
| [Partners Overview](./02_modules/partners/OVERVIEW.md) | Partner/telco management |
| [Service Installer Overview](./02_modules/service_installer/OVERVIEW.md) | SI app module |

### Documents & Notifications
**Folder**: `document_generation/`, `notifications/`

| Document | Description |
|----------|-------------|
| [Document Generation Overview](./02_modules/document_generation/OVERVIEW.md) | Template-based docs |
| [Document Templates](./02_modules/document_generation/DOCUMENT_TEMPLATES_MODULE.md) | Template management |
| [Notifications Overview](./02_modules/notifications/OVERVIEW.md) | Notification system |

### Settings & Infrastructure
**Folder**: `global_settings/`, `background_jobs/`, `kpi/`, `rbac/`

| Document | Description |
|----------|-------------|
| [Global Settings Overview](./02_modules/global_settings/OVERVIEW.md) | System settings |
| [Background Jobs Overview](./02_modules/background_jobs/OVERVIEW.md) | Background processing |
| [KPI Profiles Overview](./02_modules/kpi/OVERVIEW.md) | KPI management |
| [RBAC Overview](./02_modules/rbac/OVERVIEW.md) | Role-based access control |

---

## 📋 **Business Rules**

**Location**: `03_business/`

| Document | Description |
|----------|-------------|
| [Business Policies](./03_business/BUSINESS_POLICIES.md) | Core business rules |
| [Storybook](./03_business/STORYBOOK.md) | GPON business scenarios |
| [Use Cases](./03_business/USE_CASES.md) | User scenarios |
| [User Flows](./03_business/USER_FLOWS.md) | User journey maps |

---

## 🔌 **API Documentation**

**Location**: `04_api/`

| Document | Description |
|----------|-------------|
| [API Contracts Summary](./04_api/API_CONTRACTS_SUMMARY.md) | API endpoint reference |

---

## 🗄️ **Data Model**

**Location**: `05_data_model/`

| Folder | Description |
|--------|-------------|
| [Data Model Index](./05_data_model/DATA_MODEL_INDEX.md) | Database schema overview |
| [Reference Types & Relationships](./05_data_model/REFERENCE_TYPES_AND_RELATIONSHIPS.md) | Departments, building types, order types, order categories, installation methods, splitter types—lists and how they relate |
| [entities/](./05_data_model/entities/) | 27 entity definition files |
| [relationships/](./05_data_model/relationships/) | 17 relationship files |

---

## 🤖 **AI & Development**

**Location**: `06_ai/`

| Document | Description |
|----------|-------------|
| [Quick Start](./06_ai/QUICK_START.md) | Get started in 5 minutes |
| [Developer Guide](./06_ai/DEVELOPER_GUIDE.md) | Developer onboarding |
| [Developer Handbook](./06_ai/DEV_HANDBOOK.md) | Development practices |
| [Cursor Master Script](./06_ai/CURSOR_MASTER_SCRIPT.md) | AI assistant prompts |
| [Implementation Notes](./06_ai/IMPLEMENTATION_NOTES_FOR_DEVELOPERS_AND_CURSOR_AI.md) | AI guidelines |
| [.NET 10 Upgrade Guide](./06_ai/DOTNET_10_UPGRADE_GUIDE.md) | Upgrade instructions |
| [Testing Guides](./06_ai/) | Email parser testing, invoice testing |

---

## 🎨 **Frontend**

**Location**: `07_frontend/`

| Document | Description |
|----------|-------------|
| [Frontend Strategy](./07_frontend/FRONTEND_STRATEGY.md) | Frontend architecture |
| [SI App Strategy](./07_frontend/si_app/SI_APP_STRATEGY.md) | Service Installer app |
| [UI Components](./07_frontend/ui/) | 11 UI component docs |
| [Storybook](./07_frontend/storybook/) | 7 storybook guides |
| [Checklists](./07_frontend/) | Configuration, workflow, improvements |

**Architecture Documentation:**
| Document | Description |
|----------|-------------|
| [Frontend Component Library](./architecture/ui/storybook.md) | Complete UI component library & screen documentation (Storybook-style) |

---

## 🔧 **Infrastructure**

**Location**: `08_infrastructure/`

| Document | Description |
|----------|-------------|
| [Fast Restart Guide](./08_infrastructure/FAST_RESTART_GUIDE.md) | Development speed optimization |
| [Testing Setup](./08_infrastructure/TESTING_SETUP.md) | Test environment setup |
| [Versioning Strategy](./08_infrastructure/VERSIONING_STRATEGY.md) | Version management |
| [Background Jobs Infrastructure](./08_infrastructure/background_jobs_infrastructure.md) | Job processing setup |

---

## 📦 **Archives**

| Document | Description |
|----------|-------------|
| [archive/](./archive/README.md) | Superseded and one-time deliverable docs (event bus phase history, deliverables, evidence). **Current** event bus: [PHASE_8_PLATFORM_EVENT_BUS.md](./PHASE_8_PLATFORM_EVENT_BUS.md), [EVENT_BUS_OPERATIONS_RUNBOOK.md](./EVENT_BUS_OPERATIONS_RUNBOOK.md). |
| **99_appendix/** | Reserved for reference material. For current specs use the numbered folders (01–08) and [00_QUICK_NAVIGATION](./00_QUICK_NAVIGATION.md). |

---

## 📁 **Folder Structure**

```
docs/
├── _INDEX.md (this file)
├── README.md
├── 00_QUICK_NAVIGATION.md
│
├── 01_system/ (9 files)
│   └── System architecture and flows
│
├── 02_modules/ (22 subfolders, ~36 files)
│   ├── email_parser/
│   │   ├── OVERVIEW.md
│   │   ├── SETUP.md
│   │   ├── SPECIFICATION.md
│   │   ├── WORKFLOW.md
│   │   └── BUILDING_MATCHING.md
│   ├── department/
│   │   ├── OVERVIEW.md
│   │   └── FILTERING.md
│   ├── orders/
│   │   └── OVERVIEW.md
│   ├── rate_engine/
│   │   └── OVERVIEW.md
│   ├── ... (18 more module folders)
│   └── Each module has focused, smaller files
│
├── 03_business/ (10 files)
├── 04_api/ (2 files)
├── 05_data_model/ (46 files)
├── 06_ai/ (22 files)
├── 07_frontend/ (22 files)
└── 08_infrastructure/ (11 files)
```

---

## 🎯 **Documentation Principles**

### File Naming:
- `OVERVIEW.md` - What the module is
- `SETUP.md` - How to configure/install
- `SPECIFICATION.md` - Detailed rules/logic
- `WORKFLOW.md` - Process flows
- Descriptive names for specific topics

### Size:
- ✅ Small focused files (<500 lines)
- ✅ Single responsibility per file
- ✅ Easy to navigate and maintain

### Organization:
- ✅ Modular subfolders by domain
- ✅ Related docs grouped together
- ✅ Clear folder hierarchy

---

## 🔍 **How to Find Documentation**

### By Task:

**"I want to set up email parsing"**  
→ `02_modules/email_parser/SETUP.md`

**"I want to understand how orders work"**  
→ `02_modules/orders/OVERVIEW.md`

**"I want to configure rates"**  
→ `02_modules/rate_engine/OVERVIEW.md`

**"I'm a new developer"**  
→ `06_ai/QUICK_START.md`

**"I want to see the database schema"**  
→ `05_data_model/DATA_MODEL_INDEX.md`

### By Category:
- **System/Architecture** → `01_system/`
- **Features/Modules** → `02_modules/{module_name}/`
- **Business Rules** → `03_business/`
- **API** → `04_api/`
- **Database** → `05_data_model/`
- **Development** → `06_ai/`
- **Frontend** → `07_frontend/`
- **DevOps** → `08_infrastructure/`

---

## ✅ **Documentation Status**

- ✅ **Organized**: Modular structure with subfolders
- ✅ **Focused**: Small files, single responsibility
- ✅ **Complete**: All features documented
- ✅ **Current**: Latest information included
- ✅ **Navigable**: Multiple navigation aids
- ✅ **Production-Ready**: Professional documentation

**Total Files**: 200+ organized documentation files

---

**Need help navigating?** Start with [00_QUICK_NAVIGATION.md](./00_QUICK_NAVIGATION.md)


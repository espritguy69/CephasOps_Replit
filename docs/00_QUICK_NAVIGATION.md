# 🗺️ CephasOps Documentation - Quick Navigation Guide

**Last Updated**: February 2026  
**Total Documents**: 200+ MD files; source-of-truth–aligned docs in overview/, business/, integrations/, operations/, dev/, architecture/

---

## 📂 **FOLDER STRUCTURE**

```
/docs
├── README.md ← Start here
├── 00_QUICK_NAVIGATION.md ← This file
├── DOCS_MAP.md ← Required doc set (A–P); existing vs missing
├── DOCS_INVENTORY.md ← Doc inventory and status
├── overview/ ← Product overview (business type, processes, tech stack)
├── business/ ← Process flows, department/RBAC, order, SI, docket, billing, payroll, P&L
├── integrations/ ← Email, WhatsApp, SMS, MyInvois, OneDrive
├── operations/ ← Background jobs; scope not handled
├── dev/ ← Developer onboarding (setup, run, env)
├── architecture/ ← API surface summary; data model overview (+ existing)
├── 01_system/ ← System architecture & flows
├── 02_modules/ ← Feature modules (30+ modules)
├── 03_business/ ← Business rules & policies  
├── 04_api/ ← API documentation
├── 05_data_model/ ← Database entities & relationships
├── 06_ai/ ← AI prompts & developer guides
├── 07_frontend/ ← Frontend docs & UI
├── 08_infrastructure/ ← DevOps & infrastructure
├── 99_appendix/ ← Reference material
└── archive/ ← Superseded / one-time deliverable docs (see archive/README.md)
```

---

## 🎯 **QUICK LINKS BY TOPIC**

### **Source-of-Truth Aligned (Feb 2026)**
- 📋 [Docs Map](./DOCS_MAP.md) – Required docs and mapping
- 📋 [Docs Inventory](./DOCS_INVENTORY.md) – Doc inventory and status
- 📐 [Implementation Truth Inventory](./DOCS_IMPLEMENTATION_TRUTH_INVENTORY.md) – Schema, backend, frontend from code/EF (for doc alignment)
- 🏛️ [Architecture Audit Report](./ARCHITECTURE_AUDIT_REPORT.md) – Code vs docs alignment; drift; module boundaries
- 🧠 [Codebase Intelligence Map](./architecture/CODEBASE_INTELLIGENCE_MAP.md) – Architecture intelligence hub: modules, controllers, services, entities, workers, integrations, flows, hotspots
- 📋 [Codebase Intelligence Report](./CODEBASE_INTELLIGENCE_REPORT.md) – Summary of intelligence layer and findings
- 🛡️ [Refactor Safety Report](./REFACTOR_SAFETY_REPORT.md) – Coupling, fragility, safe/danger zones, worker risks, refactor sequence
- 🔍 [Architecture Watchdog Report](./ARCHITECTURE_WATCHDOG_REPORT.md) – Drift, sprawl, leaks, boundary regression
- 📐 [Architecture Governance Systems](./architecture/ARCHITECTURE_GOVERNANCE_SYSTEMS.md) – Change Impact Predictor, Policy Engine, Doc Sync, Risk Dashboard, Self-Maintaining Architecture, Portal, Governance logs
- 📊 [Completion Status Report](./COMPLETION_STATUS_REPORT.md) – Feature completion matrix; top blockers
- ✅ [Go-Live Readiness Checklist (GPON)](./GO_LIVE_READINESS_CHECKLIST_GPON.md) – Production readiness
- 🗺️ [Roadmap to Completion](./ROADMAP_TO_COMPLETION.md) – Phased plan to production-ready
- 📄 [Product Overview](./overview/product_overview.md) – Business type and core processes
- 🔄 [Process Flows](./business/process_flows.md) – End-to-end and side paths
- 📋 [Order Lifecycle Summary](./business/order_lifecycle_summary.md) – Status flow; pointer to canonical lifecycle spec
- 🏢 [Department & RBAC](./business/department_rbac.md) – Responsibilities and access
- 📊 [Reference Data Taxonomy](./business/reference_data_taxonomy.md) – Settings reference data (implemented vs suggested)
- 📱 [SI App Journey](./business/si_app_journey.md) – Installer flow and data captured
- 💰 [Billing & MyInvois](./business/billing_myinvois_flow.md) – Invoicing and e-invoice
- ⚙️ [Background Jobs](./operations/background_jobs.md) – Schedulers and job types
- 🏢 [Multi-tenant SaaS docs](./saas/README.md) – Tenancy model, isolation, provisioning, authorization, jobs, audit, offboarding, bypasses, checklist

### **Codebase intelligence (architecture maps)**
- 🧠 [Codebase Intelligence Map](./architecture/CODEBASE_INTELLIGENCE_MAP.md) – Hub: repo shape, runtime, domains, flows, hotspots, governance links
- 🗺️ [Controller → Service Map](./architecture/controller_service_map.md) – Controllers and main services by domain
- 🗺️ [Module Dependency Map](./architecture/module_dependency_map.md) – Upstream/downstream per module
- 🗺️ [Background Worker Map](./architecture/background_worker_map.md) – Hosted services and job types
- 🗺️ [Integration Map](./architecture/integration_map.md) – Email, WhatsApp, SMS, MyInvois, OneDrive, event store, job orchestration
- 🗺️ [Entity Domain Map](./architecture/entity_domain_map.md) – Entities grouped by business area

### **Refactor safety (Level 14)**
- 🛡️ [Refactor Safety Report](./REFACTOR_SAFETY_REPORT.md) – Coupling, fragility, safe/danger zones, worker risks, sequence plan
- 🗺️ [High Coupling Modules](./architecture/high_coupling_modules.md) – Modules ranked by coupling risk
- 🗺️ [Hidden Dependencies](./architecture/hidden_dependencies.md) – Runtime resolution and cross-domain DbContext
- 🗺️ [Module Fragility Map](./architecture/module_fragility_map.md) – Per-module fragility
- 🗺️ [Safe Refactor Zones](./architecture/safe_refactor_zones.md) – Lower-risk areas
- 🗺️ [Refactor Danger Zones](./architecture/refactor_danger_zones.md) – High-risk areas
- 🗺️ [Refactor Sequence Plan](./architecture/refactor_sequence_plan.md) – Suggested refactor order
- 🗺️ [Worker Dependency Risks](./architecture/worker_dependency_risks.md) – Worker coupling and risks

### **Architecture watchdog (Level 15)**
- 🔍 [Architecture Watchdog Report](./ARCHITECTURE_WATCHDOG_REPORT.md) – Drift, sprawl, leaks, boundaries, refactor-risk check
- 📋 [Service Sprawl Watch](./architecture/service_sprawl_watch.md) – Oversized or centralizing services
- 📋 [Controller Sprawl Watch](./architecture/controller_sprawl_watch.md) – Controller families growing too broad
- 📋 [Dependency Leak Watch](./architecture/dependency_leak_watch.md) – Hidden links, cycles, cross-domain leakage
- 📋 [Worker Coupling Watch](./architecture/worker_coupling_watch.md) – Worker coupling and risk trend
- 📋 [Module Boundary Regression](./architecture/module_boundary_regression.md) – Module boundary status

### **Engineering Maturity Model (Levels 1–5)**
- 📐 [Architecture Watchdog Summary](./architecture/architecture_watchdog_summary.md) – **Start here:** index of all maturity & watchdog artifacts
- 🛡️ [Architecture Guardrails](./architecture/architecture_guardrails.md) – Rules that protect the system; controller/service/migration guardrails and automated checks
- 📄 [Level 1 – Code & Feature Integrity](./engineering/level1_code_integrity.md) – Controllers, services, validation, transactions, job safety
- 🗺️ [Level 2 – Service Dependency Graph](./architecture/service_dependency_graph.md) – Central services, hotspots, split boundaries
- 📊 [Level 2 – Service Sprawl Analysis](./architecture/service_sprawl_analysis.md) – P1/P2/P3 risk ranking, split recommendations
- 🏛️ [Level 3 – Architecture Integrity Audit](./architecture/architecture_integrity_audit.md) – Clean Architecture violations, boundary leaks
- ⚡ [Level 4 – Reliability & Production Safety](./operations/reliability_audit.md) – Jobs, idempotency, retries, transactions, MyInvois
- 📋 [Level 5 – Migration Integrity Watch](./operations/migration_integrity_watch.md) – EF chain safety, schema drift, operational cautions
- 📋 [Level 5 – System Evolution Risk](./operations/system_evolution_risk.md) – Long-term platform risks, scaling, control documents

- 📄 [MyInvois Production Runbook](./operations/myinvois_production_runbook.md) – Credentials, submission, polling, rejection handling
- 📤 [Partner Portal Manual Process](./operations/partner_portal_manual_process.md) – Docket/invoice submission to TIME (no API)
- 📦 [Inventory & Ledger Ops Runbook](./operations/inventory_ledger_ops_runbook.md) – Ledger reconciliation, serial lifecycle, RMA
- 📊 [Reporting & P&L Ops Runbook](./operations/reporting_pnl_ops_runbook.md) – P&L rebuild, Reports Hub, KPI, Dashboard
- 🚫 [Scope Not Handled](./operations/scope_not_handled.md) – What the system does not do yet
- 👨‍💻 [Developer Onboarding](./dev/onboarding.md) – Setup, run, env, next steps

### **Getting Started**
- 📖 [README](./README.md) - Start here!
- 🚀 [Quick Start Guide](./06_ai/QUICK_START.md)
- 👨‍💻 [Developer Guide](./06_ai/DEVELOPER_GUIDE.md)
- 📚 [Developer Handbook](./06_ai/DEV_HANDBOOK.md)

---

### **System Architecture**
- 🏗️ [Architecture Book](./01_system/ARCHITECTURE_BOOK.md)
- 📊 [System Overview](./01_system/SYSTEM_OVERVIEW.md)
- 🔄 [Order Lifecycle](./business/order_lifecycle_summary.md) – Status flow; canonical: [order_lifecycle_and_statuses](./business/order_lifecycle_and_statuses.md)
- 🏢 [Multi-Company Architecture](./01_system/MULTI_COMPANY_ARCHITECTURE.md)
- ⚙️ [Workflow Engine](./01_system/WORKFLOW_ENGINE.md)
- 📧 [Email Pipeline](./01_system/EMAIL_PIPELINE.md)

---

### **Core Modules**

#### **Orders & Scheduling**
- 📦 [Orders Module](./02_modules/orders/OVERVIEW.md)
- 📅 [Scheduler Module](./02_modules/scheduler/OVERVIEW.md)
- ✅ [Tasks Module](./02_modules/tasks/OVERVIEW.md)

#### **Email Parser**
- 📧 [Email Parser Overview](./02_modules/email_parser/OVERVIEW.md) ⭐ Main parser spec
- 📧 [Email Parser Setup](./02_modules/email_parser/SETUP.md)
- 📧 [Background Jobs Module](./02_modules/background_jobs/OVERVIEW.md)

#### **Financial**
- 💰 [Rate Engine](./02_modules/rate_engine/RATE_ENGINE.md)
- 💵 [GPON Rate Cards](./02_modules/gpon/GPON_RATECARDS.md)
- 💼 [Partner Module](./02_modules/partners/OVERVIEW.md)
- 🧾 [Billing Module](./02_modules/billing/OVERVIEW.md)
- 📊 [P&L Module](./02_modules/pnl/OVERVIEW.md)
- 💸 [Payroll Module](./02_modules/payroll/OVERVIEW.md)

#### **Inventory & Assets**
- 📦 [Inventory Module](./02_modules/inventory/OVERVIEW.md)
- 📊 [Reports Hub](./02_modules/reports_hub/OVERVIEW.md) – Search, run, and export key reports (orders, materials, stock, ledger, scheduler)
- 🔧 [Splitters](./02_modules/splitters/OVERVIEW.md)
- 📋 [Material Templates](./02_modules/materials/MATERIAL_TEMPLATES_MODULE.md)

#### **Organization**
- 🏢 [Department Module](./02_modules/department/OVERVIEW.md)
- 🏢 [Department Filtering](./02_modules/department/FILTERING.md)
- 🏢 [Companies Setup](./02_modules/global_settings/COMPANIES_SETUP.md)

#### **Documents & Settings**
- 📄 [Document Generation](./02_modules/document_generation/OVERVIEW.md)
- 📝 [Document Templates](./02_modules/document_generation/DOCUMENT_TEMPLATES_MODULE.md)
- ⚙️ [Global Settings](./02_modules/global_settings/GLOBAL_SETTINGS_MODULE.md)
- 🔔 [Notifications](./02_modules/notifications/OVERVIEW.md)
- 🔐 [RBAC Module](./02_modules/rbac/OVERVIEW.md)

---

### **Business Rules**
- 📋 [Business Policies](./03_business/BUSINESS_POLICIES.md)
- 📖 [Storybook](./03_business/STORYBOOK.md) - GPON business rules
- 🎭 [Use Cases](./03_business/USE_CASES.md)
- 🔄 [User Flows](./03_business/USER_FLOWS.md)

---

### **Data Model**
- 📊 [Data Model Index](./05_data_model/DATA_MODEL_INDEX.md)
- 📊 [Data Model Summary](./05_data_model/DATA_MODEL_SUMMARY.md)
- 📋 [Reference Types & Relationships](./05_data_model/REFERENCE_TYPES_AND_RELATIONSHIPS.md) – Departments, building/order/installation types and how they relate
- 📁 [Entities](./05_data_model/entities/) - 27 entity files
- 🔗 [Relationships](./05_data_model/relationships/) - 17 relationship files

---

### **Frontend**
- 🎨 [Frontend Strategy](./07_frontend/FRONTEND_STRATEGY.md)
- 📱 [SI App Strategy](./07_frontend/si_app/SI_APP_STRATEGY.md)
- 🧩 [UI Components](./07_frontend/ui/) - 11 component docs
- 📚 [Storybook](./07_frontend/storybook/) - 7 storybook files
- 📖 [Frontend Component Library](./architecture/ui/storybook.md) - Complete UI component library documentation
- ✅ [Frontend Checklist](./07_frontend/FRONTEND_WORKFLOW_CHECKLIST.md)

---

### **Infrastructure**
- 🧪 [Testing Setup](./08_infrastructure/TESTING_SETUP.md)
- 📌 [Versioning Strategy](./08_infrastructure/VERSIONING_STRATEGY.md)
- 🔄 [Versioning Quick Start](./08_infrastructure/VERSIONING_QUICK_START.md)

---

### **AI & Development**
- 🤖 [Cursor Master Script](./06_ai/CURSOR_MASTER_SCRIPT.md)
- 🎓 [Cursor Onboarding](./06_ai/CURSOR_ONBOARDING.md)
- 🔧 [Implementation Notes](./06_ai/IMPLEMENTATION_NOTES_FOR_DEVELOPERS_AND_CURSOR_AI.md)
- ⬆️ [.NET 10 Upgrade Guide](./06_ai/DOTNET_10_UPGRADE_GUIDE.md)
- 📝 [Parser Input Examples](./06_ai/PARSER_INPUT_EXAMPLES.md)

---

## 🔍 **HOW TO FIND WHAT YOU NEED**

### **"I want to understand how the system works"**
→ Start with: [01_system/SYSTEM_OVERVIEW.md](./01_system/SYSTEM_OVERVIEW.md)

### **"I want to implement a new feature"**
→ Check: [02_modules/](./02_modules/) for the relevant module spec

### **"I want to understand the business rules"**
→ Read: [03_business/BUSINESS_POLICIES.md](./03_business/BUSINESS_POLICIES.md)

### **"I want to integrate with the API"**
→ See: [04_api/API_CONTRACTS_SUMMARY.md](./04_api/API_CONTRACTS_SUMMARY.md)

### **"I want to understand the database"**
→ Browse: [05_data_model/](./05_data_model/)

### **"I'm a new developer joining the team"**
→ Start with: [06_ai/QUICK_START.md](./06_ai/QUICK_START.md)

### **"I want to work on the frontend"**
→ Check: [07_frontend/FRONTEND_STRATEGY.md](./07_frontend/FRONTEND_STRATEGY.md)

### **"I need to deploy or set up infrastructure"**
→ See: [08_infrastructure/](./08_infrastructure/)


---

## 📌 **FOLDER DESCRIPTIONS**

| Folder | Purpose | File Count |
|--------|---------|------------|
| **01_system/** | System architecture, flows, lifecycle | 9 files |
| **02_modules/** | Feature module specifications | 31 files |
| **03_business/** | Business rules, policies, use cases | 10 files |
| **04_api/** | API documentation | 2 files |
| **05_data_model/** | Database schema & entities | 46 files |
| **06_ai/** | AI prompts & developer guides | 15 files |
| **07_frontend/** | Frontend architecture & UI | 22 files |
| **08_infrastructure/** | DevOps & infrastructure | 5 files |

**Total**: 159 organized documentation files + 1 README.md at root = **169 files**

---

## 🎉 **DOCUMENTATION IS NOW PRODUCTION-READY!**

Everything is organized, deduplicated, and easy to navigate! 🚀


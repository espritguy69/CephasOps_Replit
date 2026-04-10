# 📘 CephasOps Documentation – Master Index

**Status:** Active. **Last doc reorg:** April 2026. See [DOCUMENTATION_AUDIT_REPORT.md](./DOCUMENTATION_AUDIT_REPORT.md) for the latest audit and remediation status.

Welcome to the complete documentation library for **CephasOps**, covering:

- System architecture  
- Module specifications  
- Business policies  
- API contracts  
- Data models  
- Parser logic  
- Frontend specifications  
- Infrastructure  
- Background jobs  
- **Multi-tenant SaaS** (tenancy model, isolation, provisioning, authorization, jobs, audit) → **[saas/README.md](./saas/README.md)**  
- Appendix materials  

This folder is the *single source of truth* for everything related to CephasOps.

**Navigation:** [00_QUICK_NAVIGATION.md](./00_QUICK_NAVIGATION.md) (quick links by topic) · [DOCUMENTATION_AUDIT_REPORT.md](./DOCUMENTATION_AUDIT_REPORT.md) (audit findings) · [11_known_gaps/](./11_known_gaps/) (known architecture & database risks) · [architecture/CODEBASE_INTELLIGENCE_MAP.md](./architecture/CODEBASE_INTELLIGENCE_MAP.md) (architecture intelligence hub) · [architecture/ARCHITECTURE_WATCHDOG_REPORT.md](./architecture/ARCHITECTURE_WATCHDOG_REPORT.md) (architecture watchdog) · [architecture/architecture_watchdog_summary.md](./architecture/architecture_watchdog_summary.md) (engineering maturity index).

---

# 🚀 Where to Start

If you're new to the system, read the files in this order:

### **1. SYSTEM_OVERVIEW.md** (`01_system/SYSTEM_OVERVIEW.md`)
A high-level overview of CephasOps — purpose, goals, and core capabilities.

### **2. ARCHITECTURE_BOOK.md** (`01_system/ARCHITECTURE_BOOK.md`)
The full system architecture book covering:
- System design  
- Modules  
- Data flows  
- Integrations  
- Background jobs  

### **3. 05_data_model/README.md**
The **canonical data model**:
- Entities  
- Relationships  
- Data modeling standards  
- Cross-module dependencies  

### **4. 02_modules/**  
Read module-specific specs:
- Orders  
- Scheduler  
- SI App  
- Inventory  
- Billing  
- Payroll  
- P&L  
- Parser  
- Settings  
- RBAC  
- Background jobs  

### **5. 04_api/**
All endpoint definitions:
- API blueprint  
- Endpoint contracts  
- Request/response formats  
- Validation  
- Errors  

### **6. 07_frontend/**
Frontend behavior, UI flows, and component library.

---

# 📚 Folder Overview

## **01_system/**
System-level documentation:
- System overview
- Domain philosophy
- Lifecycles
- Workflow engine
- Order lifecycle
- Scenario walkthroughs

## **02_modules/**
Detailed module specifications:
- Purpose  
- Domain logic  
- API endpoints  
- Events  
- Validation rules  
- Integration behavior  

Each module matches the backend module structure.

## **03_business/**
Business policy documentation:
- KPI rules  
- Costing logic  
- Rate plans  
- SLA & KPI envelopes  
- Business flow diagrams  
- SOP references  

## **04_api/**
Backend API specs:
- API blueprint  
- REST design rules  
- Pagination & filtering  
- Authentication & headers  
- Error response standards  

## **05_data_model/**
Canonical, final source for entity and relationship definitions:
- Entities  
- Foreign keys  
- Cross-module diagrams  
- Normalisation rules  
- Metadata usage  
- Multi-company isolation  

> Cursor must always reference this folder before generating backend code.

## **06_ai/**
AI-related logic:
- Email parser pipelines  
- Parsing templates  
- AI-driven extraction workflow  
- Snapshot cleanup logic  
- Parser edge cases  

## **07_frontend/**
Frontend specifications for:
- Admin Web Portal  
- Scheduler  
- SI App (mobile-first)  
- Inventory UI  
- Reports Hub (search, run, filters, export)  
- Billing UI  
- Payroll UI  
- P&L dashboards  

Includes:
- UI_FLOWS.md  
- COMPONENT_LIBRARY.md  
- UX_STANDARDS.md  
- Module screen specs  

## **08_infrastructure/**
System infrastructure & devops:
- Background job specs  
- Build & release pipelines  
- Storage structure  
- Snapshot lifecycle  
- e-Invoice integration flows  
- Logging & monitoring  

**Operational Replay:** See [archive/OPERATIONAL_REPLAY_ENGINE_PHASE1.md](./archive/OPERATIONAL_REPLAY_ENGINE_PHASE1.md) and [archive/OPERATIONAL_REPLAY_ENGINE_PHASE2.md](./archive/OPERATIONAL_REPLAY_ENGINE_PHASE2.md) for the replay engine documentation.

**Source-of-truth aligned:** Consolidated views live in [overview/](./overview/), [business/](./business/), [operations/](./operations/), [integrations/](./integrations/), [dev/](./dev/), and [architecture/](./architecture/).

**Archive:** Superseded or one-time deliverable docs are in [archive/](./archive/) (see [archive/README.md](./archive/README.md)). Use numbered folders (01–08) and `architecture/` for current specs.

**Note:** The `99_appendix/` folder is reserved for reference material.

---

# 🧠 How AI & Cursor Should Use This Folder

Cursor AI should strictly follow this loading order:

### **1. Load `ARCHITECTURE_BOOK.md`**
To understand the entire system before writing any code.

### **2. Load `05_data_model/DATA_MODEL_SUMMARY.md`**
This defines:
- All entities  
- Multi-company rules  
- Data types  
- Normalisation  

### **3. Load the module spec under `02_modules/`**
This defines the:
- Business requirements  
- API expectations  
- Domain behavior  

### **4. Load `04_api/`**
To generate:
- Controllers  
- DTOs  
- API routing  

### **5. Load `07_frontend/`**
To generate:
- UI screens  
- Forms  
- Components  

### **6. Use `08_infrastructure/`**
For:
- Background jobs  
- Snapshot cleanup  
- Parser scheduling  
- P&L rebuild tasks  

AI must verify *all* entities against `05_data_model` before creating:
- Repositories  
- Services  
- EF Core configs  
- Controllers  
- Migrations  

---

# 🧵 Change Management Requirements

Whenever a developer changes:
- An entity  
- A relationship  
- A business rule  
- A workflow  
- A parser config  
- An API contract  

They **must** update the corresponding file under `/docs`.

Only then can:
- Backend  
- Frontend  
- AI-generated code  
- Database migrations  

Remain consistent.

---

# 🔍 Versioning

Every major change must be:

1. Documented in the correct file  
2. Linked in the CHANGELOG  
3. Reflected in module specs  
4. Approved before merging  

---

# 📬 Contact

Maintained by  
**CephasOps Architecture & Engineering Team**

---


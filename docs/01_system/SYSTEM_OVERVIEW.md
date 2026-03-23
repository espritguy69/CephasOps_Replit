# CephasOps – System Overview

**Version:** 1.0  
**Date:** December 2025  
**Status:** Production System

---

## 1. End-to-End Journey

### 1.1 Order Lifecycle Journey

```
┌─────────────────────────────────────────────────────────────────┐
│                    ORDER LIFECYCLE JOURNEY                      │
└─────────────────────────────────────────────────────────────────┘

1. EMAIL INGESTION
   Partner Email → Email Server (POP3/IMAP/O365)
   ↓
   EmailIngestionService fetches email
   ↓
   EmailMessage entity created

2. EMAIL CLASSIFICATION
   EmailClassificationService analyzes email
   ↓
   Classifies: Activation / Modification / Assurance / Reschedule / MRA
   ↓
   Routes to appropriate parser template

3. PARSING
   ParserTemplate extracts structured data
   ↓
   Excel/PDF/HTML parsing
   ↓
   ParsedOrderDraft created
   ↓
   Admin reviews in ParseSessionReviewPage

4. ORDER CREATION
   Admin approves ParsedOrderDraft
   ↓
   OrderService creates Order
   ↓
   Status: "Pending"
   ↓
   Order appears in scheduler

5. SCHEDULING
   Admin assigns SI and time slot
   ↓
   ScheduledSlot created
   ↓
   Status: "Assigned"
   ↓
   SI receives notification

6. FIELD INSTALLATION (SI App)
   SI opens job in mobile app
   ↓
   Status: "OnTheWay" (GPS captured)
   ↓
   Status: "MetCustomer" (GPS + photo)
   ↓
   Installation completed
   ↓
   Serial numbers scanned
   ↓
   Photos uploaded
   ↓
   Checklist completed
   ↓
   Status: "OrderCompleted"

7. DOCKET MANAGEMENT
   SI submits docket
   ↓
   Admin reviews and uploads
   ↓
   Status: "DocketsReceived"
   ↓
   Admin validates and QA checks
   ↓
   Status: "DocketsVerified"
   ↓
   Admin uploads to partner portal
   ↓
   Status: "DocketsUploaded"
   ↓
   Status: "ReadyForInvoice"

8. INVOICING
   Finance creates invoice
   ↓
   Invoice PDF generated
   ↓
   Status: "Invoiced"
   ↓
   Finance submits to partner portal (e-Invoice/MyInvois)
   ↓
   Status: "SubmittedToPortal"
   ↓
   Partner processes payment
   ↓
   Finance records payment
   ↓
   Status: "Completed"

9. PAYROLL & P&L
   PayrollService calculates SI earnings
   ↓
   PnlService calculates profit/loss
   ↓
   Reports generated for directors
```

### 1.2 System Context Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         EXTERNAL SYSTEMS                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  Partner Email Servers  │  Partner Portals  │  Payment Gateways  │
│  (POP3/IMAP/O365)       │  (TIME X Portal)  │  (Future)          │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        CEPHASOPS SYSTEM                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────┐         ┌──────────────┐                     │
│  │  Admin Portal │         │   SI App     │                     │
│  │  (React Web)  │         │  (React PWA) │                     │
│  └──────┬───────┘         └──────┬───────┘                     │
│         │                         │                              │
│         └──────────┬───────────────┘                              │
│                   │                                              │
│                   ▼                                              │
│         ┌──────────────────────┐                                │
│         │   Backend API         │                                │
│         │  (ASP.NET Core 10)    │                                │
│         └──────────┬────────────┘                                │
│                    │                                             │
│         ┌───────────┴────────────┐                               │
│         │                        │                               │
│         ▼                        ▼                               │
│  ┌──────────────┐        ┌──────────────┐                      │
│  │  Application │        │   Domain     │                      │
│  │   Services   │        │   Entities   │                      │
│  └──────┬───────┘        └──────┬───────┘                      │
│         │                        │                               │
│         └──────────┬─────────────┘                               │
│                    │                                             │
│                    ▼                                             │
│         ┌──────────────────────┐                                │
│         │   Infrastructure     │                                │
│         │  (EF Core, Email,     │                                │
│         │   File Storage)       │                                │
│         └──────────┬────────────┘                                │
│                    │                                             │
│                    ▼                                             │
│         ┌──────────────────────┐                                │
│         │   PostgreSQL          │                                │
│         │   (Supabase)          │                                │
│         └──────────────────────┘                                │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. Component Overview

### 2.1 Backend Components

#### API Layer (`CephasOps.Api`)
- **Purpose:** HTTP endpoints, authentication, request/response handling
- **Key Features:**
  - JWT authentication
  - CORS configuration
  - Swagger/OpenAPI documentation
  - Request validation
  - Error handling middleware

#### Application Layer (`CephasOps.Application`)
- **Purpose:** Business logic, use cases, orchestration
- **Key Modules:**
  - Orders, Scheduler, Inventory, Billing, Payroll, P&L
  - Parser, Workflow, Notifications, Settings
  - Auth, Files, Assets, RMA, Tasks

#### Domain Layer (`CephasOps.Domain`)
- **Purpose:** Entities, value objects, domain rules
- **Key Features:**
  - Business invariants
  - Domain events
  - Entity relationships
  - Enum definitions

#### Infrastructure Layer (`CephasOps.Infrastructure`)
- **Purpose:** External integrations, persistence, file storage
- **Key Features:**
  - EF Core configurations
  - PostgreSQL provider
  - Email connectivity (MailKit)
  - File storage
  - Background job processing

### 2.2 Frontend Components

#### Admin Portal (`/frontend`)
- **Technology:** React + TypeScript + Vite
- **UI Library:** shadcn/ui + Tailwind CSS
- **State Management:** TanStack Query + Context API
- **Key Features:**
  - Dashboard and analytics
  - Order management
  - Scheduler calendar
  - Settings management
  - Reports and KPIs

#### SI App (`/frontend-si`)
- **Technology:** React + TypeScript (PWA)
- **UI Library:** Mobile-optimized components
- **Key Features:**
  - Job list and details
  - Status transitions
  - Photo upload
  - Serial number scanning
  - GPS tracking

---

## 3. Module Boundaries

### 3.1 Core Modules

| Module | Purpose | Key Entities | Dependencies |
|--------|---------|--------------|--------------|
| **Orders** | Order lifecycle management | Order, OrderStatusLog, OrderReschedule | Workflow, Scheduler, Inventory |
| **Workflow** | Business rule enforcement | WorkflowDefinition, GuardCondition, SideEffect | Orders, Settings |
| **Scheduler** | SI assignment and scheduling | ScheduledSlot, SiAvailability, SiLeaveRequest | Orders, ServiceInstallers |
| **Inventory** | Materials and stock management | Material, StockBalance, StockMovement, SerialisedItem | Orders, RMA |
| **Billing** | Invoicing and payment tracking | Invoice, InvoiceLineItem, Payment | Orders, Partners |
| **Payroll** | SI earnings calculation | PayrollPeriod, PayrollRun, JobEarningRecord | Orders, ServiceInstallers, Rates |
| **P&L** | Profit & loss reporting | PnlPeriod, PnlFact, PnlDetailPerOrder | Orders, Billing, Payroll |
| **Parser** | Email parsing and order creation | ParseSession, ParsedOrderDraft, ParserTemplate | Orders, Email |
| **Settings** | System configuration | GlobalSettings, ParserTemplate, WorkflowDefinition | All modules |

### 3.2 Supporting Modules

| Module | Purpose | Key Entities |
|--------|---------|--------------|
| **Auth** | Authentication and authorization | User, Role, Permission |
| **Files** | File storage and management | File |
| **Notifications** | In-app and email notifications | Notification, NotificationSetting |
| **Assets** | Asset management and depreciation | Asset, AssetType, AssetDepreciation |
| **RMA** | Return material authorization | RmaRequest, RmaRequestItem |
| **Tasks** | Task management | TaskItem |
| **Buildings** | Building and infrastructure management | Building, Splitter, SplitterPort |

---

## 4. Lifecycle Diagrams

### 4.1 Order Status Lifecycle

**Complete Order Workflow (17 Statuses):**

**Main Flow (12 Statuses):**
```
Pending → Assigned → OnTheWay → MetCustomer → OrderCompleted 
  → DocketsReceived → DocketsVerified → DocketsUploaded 
  → ReadyForInvoice → Invoiced → SubmittedToPortal → Completed
```

**Side States (5 Statuses):**
- `Blocker` - Can occur from Assigned, OnTheWay, or MetCustomer
- `ReschedulePendingApproval` - Can occur from Assigned or Blocker
- `Rejected` - Order rejected
- `Cancelled` - Order cancelled (terminal)
- `Reinvoice` - Payment rejected, needs resubmission

**Visual Flow Diagram:**
```
                    ┌─────────┐
                    │ Pending │
                    └────┬────┘
                         │
                         ▼
                    ┌──────────┐
                    │ Assigned │
                    └────┬─────┘
                         │
            ┌────────────┼────────────┐
            │            │            │
            ▼            ▼            ▼
      ┌──────────┐ ┌──────────┐ ┌──────────────┐
      │OnTheWay  │ │ Blocker  │ │Reschedule    │
      └────┬─────┘ └────┬─────┘ │Pending      │
           │            │        │Approval     │
           ▼            │        └────┬─────────┘
      ┌──────────┐      │             │
      │MetCustomer│     │             │
      └────┬─────┘      │             │
           │            │             │
           │            ▼             │
           │      ┌──────────┐       │
           │      │ Resolved │       │
           │      └────┬─────┘       │
           │           │             │
           └───────────┴─────────────┘
                         │
                         ▼
                  ┌──────────────┐
                  │OrderCompleted│
                  └──────┬───────┘
                         │
                         ▼
                  ┌──────────────┐
                  │DocketsReceived│
                  └──────┬───────┘
                         │
                         ▼
                  ┌──────────────┐
                  │DocketsVerified│
                  └──────┬───────┘
                         │
                         ▼
                  ┌──────────────┐
                  │DocketsUploaded│
                  └──────┬───────┘
                         │
                         ▼
                  ┌──────────────┐
                  │ReadyForInvoice│
                  └──────┬───────┘
                         │
                         ▼
                    ┌──────────┐
                    │ Invoiced │
                    └────┬─────┘
                         │
                         ▼
                  ┌──────────────┐
                  │SubmittedToPortal│
                  └──────┬───────┘
                         │
            ┌────────────┼────────────┐
            │            │            │
            ▼            ▼            ▼
      ┌──────────┐ ┌──────────┐ ┌──────────┐
      │Completed │ │ Reinvoice│ │ Cancelled│
      └──────────┘ └────┬─────┘ └──────────┘
                        │
                        └──────────────┐
                                       │
                                       ▼
                                  ┌──────────┐
                                  │ Invoiced │
                                  └──────────┘
```

**Note:** All status values use PascalCase (e.g., `OnTheWay`, `MetCustomer`, `DocketsVerified`, `SubmittedToPortal`).  
**Reference:** See `docs/05_data_model/WORKFLOW_STATUS_REFERENCE.md` for complete status definitions.

### 4.2 Data Flow Diagram

```
┌──────────────┐
│ Partner Email│
└──────┬───────┘
       │
       ▼
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│Email Ingestion│────▶│Classification│────▶│   Parsing    │
└──────┬───────┘     └──────┬───────┘     └──────┬───────┘
       │                    │                     │
       ▼                    ▼                     ▼
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│EmailMessage  │     │EmailCategory │     │ParsedOrder   │
│   Entity     │     │   (Settings) │     │   Draft      │
└──────────────┘     └──────────────┘     └──────┬───────┘
                                                   │
                                                   ▼
                                          ┌──────────────┐
                                          │Order Creation │
                                          │   (Admin)     │
                                          └──────┬───────┘
                                                 │
                                                 ▼
                                          ┌──────────────┐
                                          │    Order     │
                                          │   Entity     │
                                          └──────┬───────┘
                                                 │
                    ┌────────────────────────────┼────────────────────────────┐
                    │                            │                            │
                    ▼                            ▼                            ▼
            ┌──────────────┐            ┌──────────────┐            ┌──────────────┐
            │  Scheduler   │            │  Inventory   │            │   Workflow   │
            │   Module     │            │   Module     │            │    Engine    │
            └──────┬───────┘            └──────┬───────┘            └──────┬───────┘
                   │                           │                           │
                   ▼                           ▼                           ▼
            ┌──────────────┐            ┌──────────────┐            ┌──────────────┐
            │ScheduledSlot │            │StockMovement │            │StatusLog     │
            └──────────────┘            └──────────────┘            └──────────────┘
                   │                           │                           │
                   └───────────────────────────┴───────────────────────────┘
                                                 │
                                                 ▼
                                          ┌──────────────┐
                                          │OrderCompleted│
                                          └──────┬───────┘
                                                 │
                    ┌────────────────────────────┼────────────────────────────┐
                    │                            │                            │
                    ▼                            ▼                            ▼
            ┌──────────────┐            ┌──────────────┐            ┌──────────────┐
            │   Billing    │            │   Payroll    │            │     P&L      │
            │   Module     │            │   Module     │            │    Module    │
            └──────┬───────┘            └──────┬───────┘            └──────┬───────┘
                   │                           │                           │
                   ▼                           ▼                           ▼
            ┌──────────────┐            ┌──────────────┐            ┌──────────────┐
            │   Invoice    │            │PayrollRun    │            │  PnlPeriod   │
            └──────────────┘            └──────────────┘            └──────────────┘
```

---

## 5. Integration Points

### 5.1 External Integrations

| Integration | Type | Status | Purpose |
|-------------|------|--------|---------|
| **Partner Email Servers** | POP3/IMAP/O365 | ✅ Active | Email ingestion |
| **Partner Portals** | Manual (TIME X Portal) | ✅ Active | Invoice submission |
| **Payment Gateways** | API | 📋 Future | Payment processing |
| **Accounting Software** | API | 📋 Future | Financial sync |

### 5.2 Internal Integrations

| Integration | Type | Status | Purpose |
|-------------|------|--------|---------|
| **Email Parser → Orders** | Domain Event | ✅ Active | Order creation |
| **Orders → Scheduler** | Service Call | ✅ Active | SI assignment |
| **Orders → Inventory** | Service Call | ✅ Active | Material usage |
| **Orders → Billing** | Service Call | ✅ Active | Invoice creation |
| **Orders → Payroll** | Background Job | ✅ Active | SI earnings |
| **Billing + Payroll → P&L** | Background Job | ✅ Active | P&L calculation |

---

## 6. System Characteristics

### 6.1 Scalability
- **Horizontal Scaling:** API layer can be scaled independently
- **Database:** PostgreSQL supports high concurrency
- **Caching:** Memory cache for settings and frequently accessed data
- **Background Jobs:** Asynchronous processing for heavy operations

### 6.2 Reliability
- **Audit Trails:** All status changes and critical operations are logged
- **Data Integrity:** Foreign key constraints and validation rules
- **Error Handling:** Comprehensive error handling and logging
- **Soft Delete:** Critical entities support soft delete for data recovery

### 6.3 Security
- **Authentication:** JWT-based authentication
- **Authorization:** Role-based access control (RBAC)
- **Data Isolation:** Company-scoped data filtering
- **Audit Logging:** All sensitive operations are logged

### 6.4 Maintainability
- **Clean Architecture:** Separation of concerns
- **Domain-Driven Design:** Business logic in domain layer
- **Settings-Driven:** Configuration without code changes
- **Documentation:** Comprehensive documentation in `/docs`

---

## 7. Repository Structure

**Updated from:** `bizops_docs/CURRENT_STATE.md`

This section provides a high-level overview of the CephasOps repository structure.

### 7.1 High-Level Structure

```
CephasOps/
├── backend/                    # .NET 10 Backend (Clean Architecture)
│   ├── src/
│   │   ├── CephasOps.Api/      # ASP.NET Core Web API (Controllers, Program.cs)
│   │   ├── CephasOps.Application/  # Use cases, services, DTOs
│   │   ├── CephasOps.Domain/   # Entities, value objects, domain logic
│   │   └── CephasOps.Infrastructure/  # EF Core, PostgreSQL, external services
│   ├── tests/                  # Unit tests
│   ├── migrations/             # SQL migration scripts
│   └── scripts/                # PowerShell scripts (build, migrate, seed)
│
├── frontend/                   # React Admin Portal (TypeScript)
│   ├── src/
│   │   ├── api/                # API client modules (80+ files, mostly .ts)
│   │   ├── components/         # React components (auth, charts, layout, ui, etc.)
│   │   ├── contexts/           # React contexts (Auth, Department, Notification, Theme)
│   │   ├── hooks/              # Custom hooks (34 files, mostly .ts)
│   │   ├── pages/              # Page components (orders, inventory, settings, etc.)
│   │   ├── routes/             # Route definitions
│   │   ├── types/              # TypeScript type definitions (38 files)
│   │   └── utils/              # Utility functions
│   └── package.json            # React 18, Vite, Syncfusion v31.1.17, TanStack Query
│
├── frontend-si/                # React Service Installer App (TypeScript)
│   ├── src/
│   │   ├── api/                # API client (orders, workflow, photos, etc.)
│   │   ├── components/         # Mobile-first components
│   │   ├── pages/              # SI-specific pages (jobs, materials, earnings)
│   │   └── types/              # TypeScript types
│   └── package.json            # React 18, Vite, TanStack Query (no Syncfusion)
│
├── docs/                       # Technical documentation
│   ├── 01_system/              # Architecture, system overview
│   ├── 02_modules/             # Module specifications
│   ├── 03_business/            # Business processes, use cases
│   ├── 04_api/                 # API contracts
│   ├── 05_data_model/          # Entity definitions, relationships
│   ├── 06_ai/                  # AI/developer guides
│   ├── 07_frontend/            # Frontend patterns, UI guides
│   └── 08_infrastructure/      # Deployment, sync, versioning
│
├── environments/                # Environment variable examples
├── cursor-guides/              # Pattern reference examples
└── scripts/                    # Root-level scripts (start-all, sync-pc, etc.)
```

### 7.2 Backend Structure

#### Clean Architecture Layers

**CephasOps.Domain** (168 .cs files)
- **Purpose**: Pure business logic, no dependencies
- **Key Areas**:
  - `Assets/` - Asset management entities
  - `Billing/` - Invoices, payments, e-invoice
  - `Buildings/` - Building, blocks, splitters, network topology
  - `Companies/` - Company, Partner, PartnerGroup, CostCentre
  - `Departments/` - Department, DepartmentMembership, MaterialAllocation
  - `Inventory/` - Material, SerialisedItem, StockBalance, StockMovement
  - `Orders/` - Order, OrderMaterialUsage, OrderStatusLog, OrderBlocker
  - `Parser/` - EmailAccount, EmailMessage, ParseSession, ParserTemplate
  - `Settings/` - 27+ setting entities (GlobalSetting, Team, Vendor, etc.)
  - `Users/` - User, Role, Permission entities
  - `Workflow/` - Workflow engine entities
  - `Common/` - BaseEntity, CompanyScopedEntity (soft delete, concurrency)

**CephasOps.Application** (Organized by domain)
- **Purpose**: Use cases, services, DTOs
- **Key Modules**:
  - `Admin/`, `Agent/`, `Assets/`, `Auth/`, `Billing/`
  - `Buildings/`, `Companies/`, `Departments/`, `Files/`
  - `Inventory/`, `Notifications/`, `Orders/`, `Parser/`
  - `Payroll/`, `Pnl/`, `RMA/`, `Scheduler/`, `ServiceInstallers/`
  - `Settings/` (70 files - largest module)
  - `Tasks/`, `Workflow/`, `Rates/`

**CephasOps.Infrastructure**
- **Purpose**: EF Core, PostgreSQL, external integrations
- **Key Areas**:
  - `Persistence/` - 286 files (EF configurations, DbContext, seeders)
  - `Security/` - Encryption services
  - `External/` - External API integrations

### 7.3 Frontend Structure

#### Admin Portal (`frontend/`)
- **Framework**: React 18 + TypeScript
- **Build Tool**: Vite
- **UI Library**: Syncfusion Essential Studio Enterprise Edition
- **State Management**: TanStack Query
- **Styling**: Tailwind CSS + shadcn/ui components
- **Structure**: Modular API clients, reusable components, page-based routing

#### Service Installer App (`frontend-si/`)
- **Framework**: React 18 + TypeScript
- **Build Tool**: Vite
- **UI Library**: shadcn/ui (no Syncfusion)
- **State Management**: TanStack Query
- **Styling**: Tailwind CSS v4.0
- **Structure**: Mobile-first, PWA-capable, simplified for field use

---

**Document Status:** This overview reflects the current production system architecture as of December 2025.

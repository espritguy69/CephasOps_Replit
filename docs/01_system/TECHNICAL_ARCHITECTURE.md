# CephasOps – Technical Architecture Overview

**Version:** 1.0  
**Date:** December 2025  
**Status:** Production System

---

## 1. Architecture Principles

### 1.1 Clean Architecture

CephasOps follows **Clean Architecture** principles with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                        API Layer                             │
│  (CephasOps.Api) - Controllers, DTOs, Authentication        │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                         │
│  (CephasOps.Application) - Services, Use Cases, DTOs       │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│                      Domain Layer                            │
│  (CephasOps.Domain) - Entities, Value Objects, Rules        │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│                  Infrastructure Layer                        │
│  (CephasOps.Infrastructure) - EF Core, Email, Storage       │
└─────────────────────────────────────────────────────────────┘
```

**Dependency Rule:**
- **Domain** → No dependencies (pure business logic)
- **Application** → Depends on Domain only
- **Infrastructure** → Depends on Domain only
- **API** → Depends on Application, Infrastructure, and Domain

### 1.2 Domain-Driven Design (DDD)

- **Entities:** Rich domain models with business logic
- **Value Objects:** Immutable objects (e.g., ServiceId, Money)
- **Aggregates:** Order (root), Invoice (root), PayrollPeriod (root)
- **Domain Events:** OrderCompleted, InvoiceCreated, PaymentReceived
- **Repositories:** Abstraction for data access (via EF Core)

### 1.3 CQRS Pattern

- **Commands:** Mutations (CreateOrder, UpdateOrderStatus, CreateInvoice)
- **Queries:** Read operations (GetOrders, GetInvoice, GetPnlSummary)
- **Handlers:** Separate command and query handlers
- **DTOs:** Data transfer objects for API contracts

---

## 2. Layer Details

### 2.1 Domain Layer (`CephasOps.Domain`)

**Purpose:** Core business logic, entities, and domain rules.

**Structure:**
```
Domain/
├── Orders/
│   ├── Entities/ (Order, OrderStatusLog, OrderReschedule, etc.)
│   ├── Enums/ (OrderStatus, BlockerCategory, etc.)
│   └── Common/ (Value Objects)
├── Workflow/
│   └── Entities/ (WorkflowDefinition, GuardCondition, SideEffect)
├── Inventory/
│   ├── Entities/ (Material, StockBalance, SerialisedItem, etc.)
│   └── Enums/ (StockStatus, etc.)
├── Billing/
│   ├── Entities/ (Invoice, Payment, etc.)
│   └── Enums/ (PaymentMethod, PaymentType, etc.)
├── Common/
│   ├── BaseEntity.cs (Id, CreatedAt, UpdatedAt)
│   └── CompanyScopedEntity.cs (CompanyId, soft delete)
└── ... (other modules)
```

**Key Principles:**
- No dependencies on Application or Infrastructure
- Business rules enforced in entities
- Immutable value objects where appropriate
- Domain events for side effects

### 2.2 Application Layer (`CephasOps.Application`)

**Purpose:** Use cases, business orchestration, DTOs.

**Structure:**
```
Application/
├── Orders/
│   ├── Services/ (IOrderService, OrderService)
│   └── DTOs/ (OrderDto, CreateOrderDto, etc.)
├── Workflow/
│   ├── Services/ (IWorkflowService, WorkflowService)
│   ├── Validators/ (Guard condition validators)
│   └── Executors/ (Side effect executors)
├── Parser/
│   ├── Services/ (IEmailIngestionService, IParserService)
│   └── DTOs/ (ParsedOrderDraftDto, etc.)
└── ... (other modules)
```

**Key Principles:**
- Services contain business logic
- DTOs for data transfer
- Validation in services or validators
- Dependency injection for repositories

### 2.3 Infrastructure Layer (`CephasOps.Infrastructure`)

**Purpose:** External integrations, persistence, file storage.

**Structure:**
```
Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs
│   ├── Configurations/ (EF Core entity configurations)
│   ├── Migrations/ (EF Core migrations)
│   └── Seeders/ (Database seeders)
└── Services/ (Email, File Storage, etc.)
```

**Key Technologies:**
- **EF Core 10:** ORM for PostgreSQL
- **Npgsql:** PostgreSQL provider
- **MailKit:** Email connectivity
- **Syncfusion:** Document generation (PDF, Excel, Word)

### 2.4 API Layer (`CephasOps.Api`)

**Purpose:** HTTP endpoints, authentication, request/response handling.

**Structure:**
```
Api/
├── Controllers/ (OrderController, InvoiceController, etc.)
├── Converters/ (JSON converters)
├── Program.cs (Dependency injection, middleware)
└── Program.Swagger.cs (Swagger configuration)
```

**Key Features:**
- JWT authentication
- CORS configuration
- Swagger/OpenAPI documentation
- Request validation
- Error handling middleware

---

## 3. Entity and Aggregate Explanation

### 3.1 Core Aggregates

#### Order Aggregate
- **Root:** `Order`
- **Children:** `OrderStatusLog`, `OrderReschedule`, `OrderDocket`, `OrderMaterialUsage`
- **Invariants:**
  - Status transitions must be valid
  - Service ID must be unique per partner
  - Material usage must match stock availability

#### Invoice Aggregate
- **Root:** `Invoice`
- **Children:** `InvoiceLineItem`, `Payment`
- **Invariants:**
  - Invoice total = sum of line items
  - Payment amount ≤ invoice balance
  - Cannot modify after submission

#### PayrollPeriod Aggregate
- **Root:** `PayrollPeriod`
- **Children:** `PayrollRun`, `JobEarningRecord`
- **Invariants:**
  - Cannot modify after finalization
  - Earnings must match order completions

### 3.2 Entity Relationships

```
Company (1) ──< (N) Order
Order (1) ──< (N) OrderStatusLog
Order (1) ──< (N) OrderMaterialUsage
Order (1) ──< (1) Invoice
Invoice (1) ──< (N) InvoiceLineItem
Invoice (1) ──< (N) Payment
Order (N) ──> (1) ServiceInstaller
Order (N) ──> (1) Partner
Order (N) ──> (1) Building
```

---

## 4. Request Pipeline

### 4.1 HTTP Request Flow

```
1. HTTP Request
   ↓
2. CORS Middleware
   ↓
3. Authentication Middleware (JWT)
   ↓
4. Authorization (Role/Permission Check)
   ↓
5. Controller Action
   ↓
6. Application Service
   ↓
7. Domain Logic / Repository
   ↓
8. EF Core / Database
   ↓
9. Response DTO
   ↓
10. JSON Serialization
   ↓
11. HTTP Response
```

### 4.2 Error Handling

- **Validation Errors:** Return 400 Bad Request with error details
- **Authorization Errors:** Return 401 Unauthorized or 403 Forbidden
- **Not Found:** Return 404 Not Found
- **Business Rule Violations:** Return 400 with business error message
- **Server Errors:** Return 500 Internal Server Error (logged)

---

## 5. Logging, Validation, and Notification Pipelines

### 5.1 Logging

- **Framework:** Microsoft.Extensions.Logging
- **Structured Logging:** JSON format for production
- **Log Levels:** Trace, Debug, Information, Warning, Error, Critical
- **Key Log Points:**
  - Status transitions
  - Workflow validations
  - Email ingestion
  - Invoice submissions
  - Payment processing

### 5.2 Validation

- **Input Validation:** FluentValidation (if used) or manual validation
- **Domain Validation:** Business rules in domain entities
- **Workflow Validation:** Guard conditions in workflow engine
- **Validation Errors:** Returned as structured error responses

### 5.3 Notification Pipeline

```
Event Occurs (e.g., Order Assigned)
   ↓
NotificationService.CreateNotificationAsync()
   ↓
Resolve Recipients (Role-based, Department-based, VIP)
   ↓
Create Notification Entity
   ↓
Send In-App Notification
   ↓
Send Email (if configured)
   ↓
Future: SMS/WhatsApp (infrastructure ready)
```

---

## 6. Background Jobs

### 6.1 Background Job Processor

**Service:** `BackgroundJobProcessorService` (BackgroundService)

**Job Types:**
- **Email Ingest:** Fetch emails from configured accounts
- **P&L Rebuild:** Recalculate P&L for a period
- **Notification Retention:** Archive/delete old notifications
- **Document Generation:** Generate PDFs/Excel files

**Job States:**
- Queued → Running → Succeeded / Failed

**Retry Logic:**
- Automatic retry on transient failures
- Max retry attempts configured

---

## 7. Important Architectural Decisions & Tradeoffs

### 7.1 Multi-Tenant SaaS Architecture

**Decision:** Operate as a multi-tenant SaaS platform with per-company data isolation.

**Rationale:**
- Enables onboarding multiple companies without code changes
- Per-company data isolation via CompanyId scoping on all entities
- Department-based filtering within each tenant

**Implementation:**
- All queries scoped by CompanyId via EF Core global query filters
- Company-aware RBAC controls module visibility per tenant
- Shared infrastructure with logical data separation

### 7.2 Settings-Driven Configuration

**Decision:** All business rules configurable via settings (no hardcoding).

**Rationale:**
- Add new partners without code changes
- Department-specific workflows
- Future-proof extensibility

**Tradeoff:**
- More complex settings management
- Requires careful validation

### 7.3 Workflow Engine

**Decision:** Centralized workflow engine for all status transitions.

**Rationale:**
- Enforce business rules consistently
- Audit trail for all changes
- Prevent invalid transitions

**Tradeoff:**
- Additional complexity
- Performance overhead (minimal)

### 7.4 PostgreSQL with EF Core

**Decision:** PostgreSQL as primary database with EF Core ORM.

**Rationale:**
- Open-source, cost-effective
- Strong ACID guarantees
- Rich feature set
- EF Core provides type safety

**Tradeoff:**
- Learning curve for EF Core
- Migration management required

### 7.5 React Frontend

**Decision:** React + TypeScript for admin portal, React PWA for SI app.

**Rationale:**
- Modern, maintainable
- Large ecosystem
- Type safety with TypeScript
- PWA for mobile installers

**Tradeoff:**
- Separate codebase for SI app
- Future: Consider unified codebase

---

## 8. Architecture Diagrams

### 8.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Frontend (React)                         │
│  Admin Portal + SI App                                      │
└───────────────────────┬─────────────────────────────────────┘
                        │ HTTP/REST
                        ▼
┌─────────────────────────────────────────────────────────────┐
│                    API Layer (ASP.NET Core)                 │
│  Controllers, Authentication, Middleware                    │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│                Application Layer                            │
│  Services, Use Cases, DTOs                                  │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│                  Domain Layer                               │
│  Entities, Value Objects, Business Rules                    │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│              Infrastructure Layer                           │
│  EF Core, Email, File Storage, Background Jobs              │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│                  PostgreSQL (Supabase)                      │
└─────────────────────────────────────────────────────────────┘
```

### 8.2 Data Flow Diagram

```
User Action → Controller → Service → Repository → EF Core → PostgreSQL
                                                              ↓
                                                         Response
                                                              ↓
User Action ← JSON Response ← DTO ← Entity ← Database Query
```

### 8.3 Module Interaction

```
Orders Module
    ├──→ Workflow Engine (status transitions)
    ├──→ Scheduler (SI assignment)
    ├──→ Inventory (material usage)
    ├──→ Billing (invoice creation)
    └──→ Payroll (SI earnings)

Workflow Engine
    ├──→ Orders (status validation)
    └──→ Settings (workflow definitions)

Parser Module
    └──→ Orders (order creation)

Billing Module
    ├──→ Orders (invoice linking)
    └──→ P&L (revenue calculation)

Payroll Module
    ├──→ Orders (earnings calculation)
    └──→ P&L (labour cost calculation)
```

---

**Document Status:** This architecture overview reflects the current production system as of December 2025.


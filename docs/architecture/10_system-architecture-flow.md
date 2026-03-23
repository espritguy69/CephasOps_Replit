# System Architecture Flow

**File:** `docs/architecture/10_system-architecture-flow.md`  
**Purpose:** Technical architecture showing layers, components, and data flow

---

## Diagram: Clean Architecture Layers & Data Flow

```mermaid
flowchart TB
    subgraph Frontend["🌐 Frontend Layer"]
        direction LR
        AdminUI["Admin Portal<br/>(React + TypeScript + Vite)"]
        SIAppUI["SI App<br/>(React PWA + TypeScript)"]
    end
    
    subgraph APILayer["🔌 API Layer (CephasOps.Api)"]
        direction TB
        Controllers["Controllers<br/>(REST Endpoints)"]
        Middleware["Middleware<br/>(Auth, CORS, Validation)"]
        DTOs["DTOs<br/>(Request/Response)"]
    end
    
    subgraph ApplicationLayer["⚙️ Application Layer (CephasOps.Application)"]
        direction TB
        Services["Services<br/>(OrderService, InvoiceService, etc.)"]
        UseCases["Use Cases<br/>(CreateOrder, UpdateStatus)"]
        Validators["Validators<br/>(Business Rules)"]
    end
    
    subgraph DomainLayer["🏛️ Domain Layer (CephasOps.Domain)"]
        direction TB
        Entities["Entities<br/>(Order, Invoice, Material)"]
        ValueObjects["Value Objects<br/>(ServiceId, Money)"]
        DomainEvents["Domain Events<br/>(OrderCompleted, etc.)"]
        BusinessRules["Business Rules<br/>(Invariants, Policies)"]
    end
    
    subgraph InfrastructureLayer["🔧 Infrastructure Layer (CephasOps.Infrastructure)"]
        direction TB
        EFCore["EF Core<br/>(Data Access)"]
        EmailService["Email Service<br/>(MailKit)"]
        FileStorage["File Storage<br/>(S3/MinIO)"]
        BackgroundWorkers["Background Workers<br/>(Email Fetcher, Parser)"]
    end
    
    subgraph Database["💾 Database Layer"]
        PostgreSQL["PostgreSQL<br/>(Primary DB)"]
        Cache["Cache<br/>(In-Memory)"]
    end
    
    subgraph ExternalIntegrations["🔗 External Integrations"]
        EmailServers["Email Servers<br/>(POP3/IMAP/O365)"]
        PartnerPortals["Partner Portals<br/>(TIME X Portal)"]
        ObjectStorage["Object Storage<br/>(Files, Snapshots)"]
    end
    
    %% Frontend to API
    AdminUI -->|HTTP/REST<br/>JWT Auth| Controllers
    SIAppUI -->|HTTP/REST<br/>JWT Auth| Controllers
    
    %% API Layer flow
    Controllers --> Middleware
    Middleware --> DTOs
    DTOs --> Services
    
    %% Application Layer flow
    Services --> UseCases
    UseCases --> Validators
    Services --> Entities
    
    %% Domain Layer (no dependencies)
    Entities --> ValueObjects
    Entities --> DomainEvents
    Entities --> BusinessRules
    
    %% Infrastructure dependencies
    Services -.->|Interface| EFCore
    Services -.->|Interface| EmailService
    Services -.->|Interface| FileStorage
    BackgroundWorkers --> EmailService
    BackgroundWorkers --> Services
    
    %% Infrastructure to Database
    EFCore --> PostgreSQL
    Services --> Cache
    
    %% External integrations
    EmailService <--> EmailServers
    FileStorage --> ObjectStorage
    Services -.->|Manual| PartnerPortals
    
    %% Background workers process events
    BackgroundWorkers --> EFCore
    DomainEvents -.->|Triggers| BackgroundWorkers
    
    %% Styling
    classDef frontend fill:#E3F2FD,stroke:#1976D2,stroke-width:2px
    classDef api fill:#FFF3E0,stroke:#F57C00,stroke-width:2px
    classDef application fill:#F3E5F5,stroke:#7B1FA2,stroke-width:2px
    classDef domain fill:#E8F5E9,stroke:#388E3C,stroke-width:2px
    classDef infrastructure fill:#FFF8E1,stroke:#FBC02D,stroke-width:2px
    classDef database fill:#ECEFF1,stroke:#455A64,stroke-width:2px
    classDef external fill:#FCE4EC,stroke:#C2185B,stroke-width:2px
    
    class Frontend frontend
    class APILayer api
    class ApplicationLayer application
    class DomainLayer domain
    class InfrastructureLayer infrastructure
    class Database database
    class ExternalIntegrations external
```

---

## Module Dependencies Flow

```mermaid
flowchart LR
    subgraph CoreModules["📦 Core Business Modules"]
        Orders["Orders"]
        Workflow["Workflow Engine"]
        Scheduler["Scheduler"]
        Inventory["Inventory"]
        Billing["Billing"]
        Payroll["Payroll"]
        PNL["P&L"]
    end
    
    subgraph SupportingModules["🛠️ Supporting Modules"]
        Parser["Email Parser"]
        Auth["Auth & RBAC"]
        Files["Files"]
        Notifications["Notifications"]
        Settings["Settings"]
    end
    
    %% Order dependencies
    Orders --> Workflow
    Orders --> Scheduler
    Orders --> Inventory
    Orders --> Billing
    Orders --> Payroll
    
    %% Workflow dependencies
    Workflow --> Settings
    
    %% Parser creates orders
    Parser --> Orders
    
    %% Financial dependencies
    Billing --> PNL
    Payroll --> PNL
    
    %% Supporting services
    Orders --> Notifications
    Orders --> Files
    All[All Modules] --> Auth
    All --> Settings
    
    %% Styling
    classDef core fill:#FFE5B4,stroke:#FF8C00,stroke-width:2px
    classDef support fill:#E0F7FA,stroke:#0097A7,stroke-width:2px
    
    class Orders,Workflow,Scheduler,Inventory,Billing,Payroll,PNL core
    class Parser,Auth,Files,Notifications,Settings support
```

---

## Request Flow: User Action → Database

```mermaid
sequenceDiagram
    participant U as User (Admin/SI)
    participant F as Frontend
    participant API as API Layer
    participant App as Application Service
    participant D as Domain Entity
    participant I as Infrastructure
    participant DB as PostgreSQL
    
    U->>F: User Action (e.g., Update Order Status)
    F->>API: HTTP POST /api/orders/{id}/status
    API->>API: JWT Authentication
    API->>API: Request Validation
    API->>App: Call OrderService.UpdateStatus()
    App->>App: Validate Business Rules
    App->>D: Load Order Entity
    D->>D: Check Status Transition Validity
    D->>D: Apply Business Rules
    D->>D: Raise Domain Event (StatusChanged)
    App->>I: Save via Repository
    I->>DB: EF Core SaveChanges()
    DB-->>I: Success
    I-->>App: Entity Updated
    App->>App: Trigger Side Effects (Notifications, etc.)
    App-->>API: Return DTO
    API-->>F: JSON Response
    F-->>U: Update UI
```

---

## Architecture Principles

### Dependency Rule
- **Domain**: No dependencies (pure business logic)
- **Application**: Depends only on Domain
- **Infrastructure**: Depends only on Domain
- **API**: Depends on Application, Infrastructure, and Domain

### Clean Architecture Benefits
- **Testability**: Domain logic can be tested without infrastructure
- **Independence**: Can swap databases, frameworks, or UI technologies
- **Business Focus**: Core business logic isolated from technical details

---

**Related Diagrams:**
- [Company & Systems Overview](./00_company-systems-overview.md) - High-level system view
- [Email to Order Workflow](./20_workflow_email_to_order.md) - Email processing details
- [Order Lifecycle](./21_workflow_order_lifecycle.md) - Complete order journey


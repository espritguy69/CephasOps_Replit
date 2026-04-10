# Company & Systems Overview

**File:** `docs/architecture/00_company-systems-overview.md`  
**Purpose:** High-level view showing company structure, main systems, and external services

---

## Diagram: Company → Systems → External Services

```mermaid
flowchart TB
    subgraph Company["🏢 CephasOps (SaaS Platform — Per-Tenant View)"]
        direction TB
        GPON["📊 GPON Department<br/>(Active)"]
        CWO["🚧 CWO Department<br/>(Future)"]
        NWO["🌐 NWO Department<br/>(Future)"]
    end
    
    subgraph InternalSystems["💻 CephasOps Internal Systems"]
        direction LR
        AdminPortal["🖥️ Admin Portal<br/>(React Web)"]
        SIApp["📱 SI App<br/>(React PWA)"]
        BackendAPI["⚙️ Backend API<br/>(ASP.NET Core 10)"]
        EmailParser["📧 Email Parser<br/>(Background Worker)"]
        BackgroundJobs["⏰ Background Jobs<br/>(Scheduler, P&L, Cleanup)"]
    end
    
    subgraph Databases["💾 Data Storage"]
        PostgreSQL["🐘 PostgreSQL 16<br/>(Self-hosted VPS)"]
        FileStorage["📁 File Storage<br/>(Snapshots, Docs)"]
    end
    
    subgraph ExternalSystems["🌐 External Systems"]
        direction TB
        PartnerEmail["📬 Partner Email Servers<br/>(POP3/IMAP/O365)"]
        PartnerPortal["🔗 Partner Portals<br/>(TIME X Portal)"]
        PaymentGateway["💳 Payment Gateways<br/>(Future)"]
    end
    
    subgraph Partners["🤝 ISP Partners"]
        direction LR
        TIME["TIME"]
        Digi["Digi"]
        Celcom["Celcom"]
        UMobile["U-Mobile"]
    end
    
    %% Company to Departments
    Company --> GPON
    Company --> CWO
    Company --> NWO
    
    %% Departments to Systems
    GPON --> AdminPortal
    GPON --> SIApp
    GPON --> BackendAPI
    GPON --> EmailParser
    
    %% System connections
    AdminPortal -->|HTTP/REST| BackendAPI
    SIApp -->|HTTP/REST| BackendAPI
    EmailParser -->|Process| BackendAPI
    BackgroundJobs -->|Process| BackendAPI
    
    %% Backend to Storage
    BackendAPI -->|EF Core| PostgreSQL
    BackendAPI -->|Store| FileStorage
    
    %% External integrations
    PartnerEmail -->|Fetch| EmailParser
    BackendAPI -.->|Manual Upload| PartnerPortal
    BackendAPI -.->|Future| PaymentGateway
    
    %% Partners send emails
    Partners -->|Send Orders| PartnerEmail
    
    %% Styling
    classDef active fill:#90EE90,stroke:#006400,stroke-width:2px
    classDef future fill:#FFE4B5,stroke:#FF8C00,stroke-width:2px
    classDef external fill:#E0E0E0,stroke:#666,stroke-width:2px
    classDef system fill:#B0E0E6,stroke:#4682B4,stroke-width:2px
    
    class GPON active
    class CWO,NWO future
    class ExternalSystems,Partners external
    class InternalSystems,Databases system
```

---

## Key Relationships

### Company Structure
- **Multi-Tenant SaaS**: CephasOps operates as a SaaS platform with per-company data isolation and multiple departments per tenant
- **Departments**: Functional units (GPON active, CWO/NWO future)
- **Branches**: Physical locations for organizational structure

### Internal Systems
- **Admin Portal**: Web-based React app for operations teams
- **SI App**: Mobile-optimized PWA for field installers
- **Backend API**: ASP.NET Core 10 with Clean Architecture
- **Email Parser**: Background worker that processes partner emails
- **Background Jobs**: Async processing (scheduling, P&L calculations, cleanup)

### External Systems
- **Partner Email Servers**: Source of order emails (POP3/IMAP/O365)
- **Partner Portals**: TIME X Portal for invoice/docket submission (manual)
- **Payment Gateways**: Future integration for automated payments

### Data Storage
- **PostgreSQL 16**: Primary database (self-hosted on Debian 13 VPS)
- **File Storage**: Object storage for snapshots, documents, photos

---

**Related Diagrams:**
- [System Architecture](./10_system-architecture-flow.md) - Technical layer details
- [Email to Order Workflow](./20_workflow_email_to_order.md) - Email processing flow
- [Order Lifecycle](./21_workflow_order_lifecycle.md) - Complete order journey


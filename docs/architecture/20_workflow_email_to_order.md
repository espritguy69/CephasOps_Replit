# Workflow: Email to Order Creation

**File:** `docs/architecture/20_workflow_email_to_order.md`  
**Purpose:** Complete flow from partner email to order creation in CephasOps

---

## Diagram: Email Pipeline Flow

```mermaid
flowchart TD
    Start([Partner Sends Email]) --> EmailServer[Email Server<br/>POP3/IMAP/O365]
    
    EmailServer --> Ingestion[Email Ingestion Worker<br/>Runs every 60 seconds]
    
    Ingestion --> Classify{Email Classification<br/>Partner + Intent Detection}
    
    Classify -->|Activation| ActParser[Activation Parser]
    Classify -->|Modification| ModParser[Modification Parser]
    Classify -->|Assurance| AssParser[Assurance Parser]
    Classify -->|Reschedule| ResParser[Reschedule Parser]
    Classify -->|Low Confidence| ReviewQueue[Parser Review Queue]
    
    ActParser --> Extract[Data Extraction]
    ModParser --> Extract
    AssParser --> Extract
    ResParser --> Extract
    
    Extract -->|Excel/PDF| ExcelParser[Excel/PDF Parser]
    Extract -->|Body Text| BodyParser[Body Parser<br/>TTKT/Approval NLP]
    
    ExcelParser --> Normalize[Data Normalization<br/>Phone, Address, Date]
    BodyParser --> Normalize
    
    Normalize --> Resolver{Order Resolver<br/>New or Update?}
    
    Resolver -->|New Order| CreateDraft[Create ParsedOrderDraft]
    Resolver -->|Update Existing| UpdateOrder[Update Existing Order]
    
    CreateDraft --> Review[Admin Review<br/>ParseSessionReviewPage]
    
    Review -->|Approve| CreateOrder[OrderService.CreateFromDraft]
    Review -->|Reject| RejectDraft[Mark Draft as Rejected]
    Review -->|Edit| EditDraft[Edit Draft Fields]
    
    CreateOrder --> OrderCreated[Order Created<br/>Status: Pending]
    
    UpdateOrder --> OrderUpdated[Order Updated<br/>Status/Details Changed]
    
    OrderCreated --> Notify[Send Notifications]
    OrderUpdated --> Notify
    
    Notify --> End([Order Ready for Scheduling])
    
    %% Styling
    classDef process fill:#E3F2FD,stroke:#1976D2,stroke-width:2px
    classDef decision fill:#FFF9C4,stroke:#F57F17,stroke-width:2px
    classDef system fill:#F3E5F5,stroke:#7B1FA2,stroke-width:2px
    classDef result fill:#E8F5E9,stroke:#388E3C,stroke-width:2px
    
    class Ingestion,Extract,Normalize,CreateOrder,UpdateOrder process
    class Classify,Resolver,Review decision
    class EmailServer,ExcelParser,BodyParser system
    class OrderCreated,OrderUpdated,End result
```

---

## Sequence Diagram: Email Processing

```mermaid
sequenceDiagram
    participant P as Partner (TIME/Digi)
    participant ES as Email Server
    participant EW as Email Worker
    participant CS as Classification Service
    participant PS as Parser Service
    participant NS as Normalization Service
    participant OR as Order Resolver
    participant OS as Order Service
    participant DB as PostgreSQL
    participant Admin as Admin User
    
    P->>ES: Send Email with Excel/PDF
    EW->>ES: Poll Email (every 60s)
    ES-->>EW: Email + Attachments
    
    EW->>DB: Store EmailMessage
    EW->>CS: Classify Email
    
    CS->>CS: Detect Partner (TIME/Digi/Celcom)
    CS->>CS: Detect Intent (Activation/Modification)
    CS-->>EW: Classification Result
    
    EW->>PS: Select Parser Template
    PS->>PS: Extract Excel/PDF Data
    PS->>PS: Map to Order Fields
    PS-->>EW: Raw Extracted Data
    
    EW->>NS: Normalize Data
    NS->>NS: Fix Phone Numbers
    NS->>NS: Standardize Address
    NS->>NS: Parse Dates
    NS-->>EW: Normalized Data
    
    EW->>OR: Resolve Order (New/Update)
    OR->>DB: Check Existing Orders
    alt New Order
        OR-->>EW: Create Draft
        EW->>DB: Create ParsedOrderDraft
        EW-->>Admin: Notification: Draft Ready for Review
        Admin->>OS: Approve Draft
        OS->>DB: Create Order (Status: Pending)
        OS-->>Admin: Order Created
    else Update Existing
        OR-->>EW: Update Order
        EW->>OS: Update Order Details
        OS->>DB: Update Order
        OS-->>Admin: Order Updated
    end
```

---

## Department Routing Flow

```mermaid
flowchart LR
    Email[Incoming Email] --> Mailbox{Email Account<br/>Configuration}
    
    Mailbox -->|GPON Department| GPONDept[GPON Department<br/>Active]
    Mailbox -->|CWO Department| CWODept[CWO Department<br/>Future]
    Mailbox -->|NWO Department| NWODept[NWO Department<br/>Future]
    
    GPONDept --> GPONParser[GPON Parser Templates<br/>TIME/Digi/Celcom]
    CWODept --> CWOParser[CWO Parser Templates<br/>Future]
    NWODept --> NWOParser[NWO Parser Templates<br/>Future]
    
    GPONParser --> GPONWorkflow[GPON Workflow<br/>17 Status Lifecycle]
    CWOParser --> CWOWorkflow[CWO Workflow<br/>Future]
    NWOParser --> NWOWorkflow[NWO Workflow<br/>Future]
    
    GPONWorkflow --> Order[Order Created<br/>Department: GPON]
    
    %% Styling
    classDef active fill:#90EE90,stroke:#006400,stroke-width:2px
    classDef future fill:#FFE4B5,stroke:#FF8C00,stroke-width:2px
    
    class GPONDept,GPONParser,GPONWorkflow,Order active
    class CWODept,CWOParser,CWOWorkflow,NWODept,NWOParser,NWOWorkflow future
```

---

## Key Components

### Email Ingestion
- **Frequency**: Every 60 seconds (configurable per mailbox)
- **Protocols**: IMAP, POP3, Microsoft Graph (O365)
- **Storage**: Raw email stored in `email_raw` table

### Classification
- **Partner Detection**: TIME, Digi, Celcom, U-Mobile
- **Intent Detection**: Activation, Modification, Assurance, Reschedule
- **Confidence Score**: < 0.75 → Review Queue

### Parsing
- **Excel Parser**: Handles TIME/Digi/Celcom formats
- **PDF Parser**: Converts PDF → Excel → Parse
- **Body Parser**: Extracts TTKT, approval text from email body
- **NLP**: Date/time extraction, approval detection

### Normalization
- **Phone**: `+60122334455` → `0122334455`
- **Address**: Extract unit/block/floor, standardize format
- **Date/Time**: Convert to `YYYY-MM-DD HH:mm`
- **Partner ID**: Map to internal Partner entity

### Order Resolution
- **Matching**: Service ID, Partner Order ID, Customer + Address
- **New Order**: Create ParsedOrderDraft → Admin Review → Create Order
- **Update**: Update existing order with new information

---

**Related Diagrams:**
- [Company & Systems Overview](./00_company-systems-overview.md) - System context
- [System Architecture](./10_system-architecture-flow.md) - Technical details
- [Order Lifecycle](./21_workflow_order_lifecycle.md) - After order creation


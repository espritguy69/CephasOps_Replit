# RMA – System Workflow Diagram

**Date:** December 12, 2025  
**Purpose:** End-to-end workflow representation for the RMA module, covering RMA request creation, approval workflow, shipment tracking, and closure

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         RMA MODULE SYSTEM                                │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   RMA REQUESTS         │      │   RMA ITEMS            │
        │  (Faulty Device Returns)│      │  (Serialized Items)    │
        ├───────────────────────┤      ├───────────────────────┤
        │ • Requested            │      │ • SerialisedItemId     │
        │ • Pending (Approval)   │      │ • OriginalOrderId      │
        │ • Approved             │      │ • Result (Repaired/    │
        │ • InTransit            │      │   Replaced/Credited/   │
        │ • Closed               │      │   Scrapped)            │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   APPROVAL WORKFLOW    │      │   INVENTORY INTEGRATION │
        │  (If Required)         │      │  (Stock Movements)      │
        └───────────────────────┘      └───────────────────────┘
```

---

## Complete Workflow: RMA Lifecycle

```
[STEP 1: RMA INITIATION]
         |
         v
[Faulty Device Detected]
  - Usually in Assurance orders
  - Device not working
  - Customer complaint
         |
         v
┌────────────────────────────────────────┐
│ CREATE RMA REQUEST                       │
│ POST /api/rma/requests                  │
└────────────────────────────────────────┘
         |
         v
CreateRmaRequestDto {
  PartnerId: TIME
  Reason: "Device faulty - no signal"
  Items: [
    {
      SerialisedItemId: "serial-123"
      OriginalOrderId: "order-456"
      Notes: "Device failed after 2 months"
    }
  ]
}
         |
         v
┌────────────────────────────────────────┐
│ CHECK APPROVAL WORKFLOW                  │
│ ApprovalWorkflowService.GetEffectiveWorkflow()│
└────────────────────────────────────────┘
         |
         v
[Check if Approval Required]
  ApprovalWorkflow.find(
    CompanyId = Cephas
    WorkflowType = "RMA"
    EntityType = "RmaRequest"
    PartnerId = TIME
  )
         |
    ┌────┴────┐
    |         |
    v         v
[REQUIRED] [NOT REQUIRED]
   |            |
   |            v
   |       InitialStatus = "Requested"
   |
   v
InitialStatus = "Pending"
ApprovalWorkflowId = "workflow-123"
         |
         v
┌────────────────────────────────────────┐
│ CREATE RMA REQUEST                       │
└────────────────────────────────────────┘
         |
         v
RmaRequest {
  Id: "rma-789"
  CompanyId: Cephas
  PartnerId: TIME
  RequestDate: 2025-12-12
  Reason: "Device faulty - no signal"
  Status: "Pending" | "Requested"
  ApprovalWorkflowId: "workflow-123" (if approval required)
  Items: [
    {
      SerialisedItemId: "serial-123"
      OriginalOrderId: "order-456"
      Notes: "Device failed after 2 months"
    }
  ]
}
         |
         v
[If Approval Required]
         |
         v
[Wait for Approval Workflow Completion]
         |
         v
[Approval Workflow Executes]
  - Approval steps completed
  - Status updated to "Approved"
         |
         v
[STEP 2: UPDATE SERIALIZED ITEM STATUS]
         |
         v
┌────────────────────────────────────────┐
│ UPDATE INVENTORY STATUS                  │
│ InventoryService.UpdateSerializedItem()  │
└────────────────────────────────────────┘
         |
         v
SerializedItem {
  SerialNumber: "SN001"
  Status: "FaultyInWarehouse"
  CurrentLocationId: RMA_Holding_Area
}
         |
         v
[STEP 3: ATTACH MRA DOCUMENT]
         |
         v
[Partner Sends MRA Email]
  - Email Parser detects MRA type
  - Extracts RMA number
  - Downloads PDF attachment
         |
         v
┌────────────────────────────────────────┐
│ ATTACH MRA DOCUMENT                      │
│ PUT /api/rma/requests/{id}               │
└────────────────────────────────────────┘
         |
         v
UpdateRmaRequestDto {
  RmaNumber: "RMA-2025-00123" (from partner)
  MraDocumentId: "file-456" (PDF file ID)
}
         |
         v
[Update RMA Request]
  RmaRequest {
    RmaNumber: "RMA-2025-00123"
    MraDocumentId: "file-456"
  }
         |
         v
[STEP 4: SHIPMENT TO PARTNER]
         |
         v
[Admin: Prepare Shipment]
  - Package devices
  - Print MRA PDF
         |
         v
┌────────────────────────────────────────┐
│ UPDATE RMA STATUS TO INTRANSIT            │
│ PUT /api/rma/requests/{id}               │
└────────────────────────────────────────┘
         |
         v
UpdateRmaRequestDto {
  Status: "InTransit"
}
         |
         v
[Update RMA Request]
  RmaRequest {
    Status: "InTransit"
  }
         |
         v
┌────────────────────────────────────────┐
│ CREATE STOCK MOVEMENT                     │
│ InventoryService.CreateMovement()        │
└────────────────────────────────────────┘
         |
         v
StockMovement {
  MovementType: "RMAOutbound"
  FromLocationId: RMA_Holding_Area
  ToLocationId: InTransitToPartner
  MaterialId: "ONU-HG8240H"
  Quantity: 1
  SerialNumber: "SN001"
  PartnerId: TIME
  RmaRequestId: "rma-789"
}
         |
         v
[Update SerializedItem]
  SerializedItem {
    SerialNumber: "SN001"
    Status: "InTransitToPartner"
  }
         |
         v
[STEP 5: RMA CLOSURE]
         |
         v
[Partner Processes RMA]
  Options:
    - Repaired device returned
    - Replacement device sent
    - Credit note issued
    - Warranty void, scrap
         |
         v
┌────────────────────────────────────────┐
│ UPDATE RMA RESULT                         │
│ PUT /api/rma/requests/{id}/close         │
└────────────────────────────────────────┘
         |
         v
[For each RMA Item]
         |
         v
[Update RMA Request Item]
  RmaRequestItem {
    Result: "Replaced" | "Repaired" | "Credited" | "Scrapped"
  }
         |
         v
[Based on Result]
         |
    ┌────┴────┐
    |         |
    v         v
[REPAIRED/REPLACED] [CREDITED/SCRAPPED]
   |                      |
   |                      v
   |                 [If Credited]
   |                      |
   |                      v
   |                 [Record Credit Note in Finance]
   |                      |
   |                      v
   |                 [If Scrapped]
   |                      |
   |                      v
   |                 [Record Scrap Loss in PNL]
   |
   v
[If Repaired/Replaced]
         |
         v
[Create RMA Inbound Movement]
         |
         v
StockMovement {
  MovementType: "RMAInbound"
  FromLocationId: InTransitToPartner
  ToLocationId: Main_Warehouse
  MaterialId: "ONU-HG8240H"
  Quantity: 1
  SerialNumber: "SN999" (replacement) or "SN001" (repaired)
  PartnerId: TIME
  RmaRequestId: "rma-789"
}
         |
         v
[If Replacement]
         |
         v
[Create New SerializedItem]
  SerializedItem {
    SerialNumber: "SN999"
    MaterialId: "ONU-HG8240H"
    CurrentLocationId: Main_Warehouse
    Status: "InWarehouse"
    ReceivedFromRMA: true
    OriginalRmaRequestId: "rma-789"
  }
         |
         v
[If Repaired]
         |
         v
[Update Existing SerializedItem]
  SerializedItem {
    SerialNumber: "SN001"
    Status: "InWarehouse"
    CurrentLocationId: Main_Warehouse
  }
         |
         v
[Update Old SerializedItem]
  SerializedItem {
    SerialNumber: "SN001" (if replaced)
    Status: "RMAClosed"
    ClosedDate: 2025-12-20
  }
         |
         v
[Update RMA Request]
  RmaRequest {
    Status: "Closed"
    ClosedDate: 2025-12-20
  }
         |
         v
[RMA Closed]
```

---

## Email Parser Integration Flow

```
[Partner Sends MRA Email]
         |
         v
┌────────────────────────────────────────┐
│ EMAIL PARSER DETECTS MRA TYPE             │
│ EmailIngestionService.ClassifyEmail()     │
└────────────────────────────────────────┘
         |
         v
[Email Classification]
  EmailType: "MRA" | "RMA"
         |
         v
┌────────────────────────────────────────┐
│ EXTRACT RMA DATA                         │
│ ParserService.ExtractRmaData()            │
└────────────────────────────────────────┘
         |
         v
[Extract from Email/PDF]
  - RMA Number: "RMA-2025-00123"
  - Service ID: "TBBN1234567"
  - Serial Numbers: ["SN001", "SN002"]
  - Partner: TIME
         |
         v
[Download PDF Attachment]
         |
         v
┌────────────────────────────────────────┐
│ CREATE OR UPDATE RMA REQUEST               │
└────────────────────────────────────────┘
         |
         v
[If RMA Request Exists]
  - Match by Service ID or Serial Number
  - Update with RMA Number
  - Attach MRA Document
         |
         v
[If RMA Request Does Not Exist]
  - Create new RMA Request
  - Link to Order (by Service ID)
  - Attach MRA Document
```

---

## Approval Workflow Integration Flow

```
[RMA Request Created]
         |
         v
┌────────────────────────────────────────┐
│ CHECK APPROVAL WORKFLOW                  │
│ ApprovalWorkflowService.GetEffectiveWorkflow()│
└────────────────────────────────────────┘
         |
         v
[Resolution Chain]
  1. Partner-specific workflow
  2. Department-specific workflow
  3. Company default workflow
  4. No workflow (auto-approved)
         |
    ┌────┴────┐
    |         |
    v         v
[WORKFLOW FOUND] [NO WORKFLOW]
   |              |
   |              v
   |         [Status: "Requested"]
   |         [No approval needed]
   |
   v
[Status: "Pending"]
ApprovalWorkflowId = "workflow-123"
         |
         v
[Wait for Approval Steps]
         |
         v
[Approval Workflow Executes]
  - Step 1: Manager Approval
  - Step 2: Director Approval (if required)
         |
         v
[All Steps Approved]
         |
         v
[Update RMA Request]
  RmaRequest {
    Status: "Approved"
  }
         |
         v
[RMA Ready for Shipment]
```

---

## Entities Involved

### RmaRequest Entity
```
RmaRequest
├── Id (Guid)
├── CompanyId (Guid)
├── PartnerId (Guid)
├── RmaNumber (string?, from partner)
├── RequestDate (DateTime)
├── Reason (string)
├── Status (string: Requested, Pending, Approved, InTransit, Closed)
├── MraDocumentId (Guid?, file reference)
├── ApprovalWorkflowId (Guid?)
├── ShippedDate (DateTime?)
├── ClosedDate (DateTime?)
├── Items (RmaRequestItem[])
└── CreatedAt, UpdatedAt
```

### RmaRequestItem Entity
```
RmaRequestItem
├── Id (Guid)
├── CompanyId (Guid)
├── RmaRequestId (Guid)
├── SerialisedItemId (Guid)
├── OriginalOrderId (Guid)
├── Notes (string?)
├── Result (string?: Repaired, Replaced, Credited, Scrapped)
└── CreatedAt, UpdatedAt
```

---

## API Endpoints Involved

### RMA Requests
- `GET /api/rma/requests` - List RMA requests with filters
- `GET /api/rma/requests/{id}` - Get RMA request details
- `GET /api/rma/requests/by-order/{orderId}` - Get RMAs for an order
- `POST /api/rma/requests` - Create RMA request
- `PUT /api/rma/requests/{id}` - Update RMA request
- `DELETE /api/rma/requests/{id}` - Delete RMA request
- `POST /api/rma/requests/{id}/close` - Close RMA request with results

---

## Module Rules & Validations

### RMA Request Creation Rules
- Partner must exist
- At least one item (SerialisedItemId) required
- OriginalOrderId should reference valid order
- Serialized item must exist and be in valid state for RMA

### Approval Workflow Rules
- Approval workflow checked on creation
- If workflow exists, status set to "Pending"
- If no workflow, status set to "Requested"
- Approval steps must complete before status changes to "Approved"

### Shipment Rules
- Status must be "Approved" or "Requested" before InTransit
- MRA document should be attached (recommended)
- RMA number from partner should be recorded

### Closure Rules
- Status must be "InTransit" before closure
- All items must have Result set
- Result options: Repaired, Replaced, Credited, Scrapped
- Stock movements created based on result

### Inventory Integration Rules
- Serialized items moved to RMA_Holding_Area on creation
- Status updated to "FaultyInWarehouse"
- On shipment: Status → "InTransitToPartner"
- On closure:
  - Repaired: Status → "InWarehouse"
  - Replaced: New SerializedItem created, old one → "RMAClosed"
  - Credited: Status → "RMAClosed", credit note recorded
  - Scrapped: Status → "Scrapped", scrap loss recorded

---

## Integration Points

### Orders Module
- RMA requests linked to orders via OriginalOrderId
- Assurance orders trigger RMA workflows
- Order material replacements tracked in RMA items

### Inventory Module
- Serialized items status updated throughout RMA lifecycle
- Stock movements created for RMA outbound and inbound
- RMA holding area managed as stock location

### Email Parser Module
- MRA emails detected and parsed
- RMA data extracted from emails/PDFs
- RMA requests created/updated from parsed data

### Approval Workflows Module
- Approval workflows checked on RMA creation
- Approval steps executed before RMA can proceed
- Status updated based on approval results

### Billing Module
- Credit notes recorded for credited RMAs
- Financial impact tracked

### PNL Module
- Scrap losses recorded for scrapped RMAs
- Cost impact on PNL calculations

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/02_modules/inventory/WAREHOUSE_INVENTORY_FLOW.md` - Inventory workflow
- `docs/02_modules/email_parser/EMAIL_SETUP_AND_PARSE_FLOW.md` - Email parser flow


# Inventory – System Workflow Diagram

**Date:** December 12, 2025  
**Purpose:** End-to-end workflow representation for the Inventory module

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    WAREHOUSE & INVENTORY SYSTEM                          │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   STOCK LOCATIONS       │      │   STOCK MOVEMENTS      │
        │  (Warehouse, SI, etc.)  │      │  (GRN, Issue, Return)  │
        ├───────────────────────┤      ├───────────────────────┤
        │ • Main Warehouse        │      │ • GRN (Goods Received)│
        │ • Sub-warehouses        │      │ • Issue to SI          │
        │ • SI In-Hand Stock      │      │ • Return from SI       │
        │ • RMA Holding Area       │      │ • Install at Customer  │
        │ • Customer Site         │      │ • RMA Outbound         │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   SERIALIZED ITEMS     │      │   NON-SERIALIZED       │
        │  (ONU, Router, etc.)    │      │  (Cable, Connector)    │
        └───────────────────────┘      └───────────────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │      RMA WORKFLOW              │
                    │  (Faulty Device Returns)       │
                    └───────────────────────────────┘
```

---

## Complete Flow: Goods Receipt to Customer Installation

```
[STEP 1: GOODS RECEIPT FROM PARTNER]
         |
         v
┌────────────────────────────────────────┐
│ PARTNER SHIPMENT RECEIVED                │
│ - Physical stock arrives                │
│ - Delivery note/Excel/PDF provided       │
└────────────────────────────────────────┘
         |
         v
┌────────────────────────────────────────┐
│ CREATE GRN (GOODS RECEIPT NOTE)          │
│ POST /api/inventory/grn                  │
└────────────────────────────────────────┘
         |
         v
GRN {
  PartnerId: TIME
  DeliveryNoteNumber: "DN-2025-00123"
  ReceivedDate: 2025-12-10
  WarehouseId: Main_Warehouse
  Items: [
    { MaterialId: "ONU-HG8240H", Quantity: 100 },
    { MaterialId: "PATCHCORD-6M", Quantity: 500 }
  ]
}
         |
         v
[For Serialized Items]
         |
         v
┌────────────────────────────────────────┐
│ CAPTURE SERIAL NUMBERS                   │
└────────────────────────────────────────┘
         |
         v
[For each Serialized Item]
  SerializedItem {
    MaterialId: "ONU-HG8240H"
    SerialNumber: "SN123456789"
    CompanyId: Cephas
    CurrentLocationId: Main_Warehouse
    Status: "InWarehouse"
  }
         |
         v
[For Non-Serialized Items]
         |
         v
┌────────────────────────────────────────┐
│ UPDATE STOCK BALANCE                     │
└────────────────────────────────────────┘
         |
         v
StockBalance {
  MaterialId: "PATCHCORD-6M"
  StockLocationId: Main_Warehouse
  Quantity: 500 (increased)
}
         |
         v
┌────────────────────────────────────────┐
│ CREATE STOCK MOVEMENT                    │
└────────────────────────────────────────┘
         |
         v
StockMovement {
  MovementType: "GRN"
  FromLocationId: null (Partner)
  ToLocationId: Main_Warehouse
  MaterialId: "ONU-HG8240H"
  Quantity: 100
  OrderId: null
  ServiceInstallerId: null
  PartnerId: TIME
  Remarks: "GRN DN-2025-00123"
}
         |
         v
[STEP 2: ISSUE STOCK TO SI]
         |
         v
┌────────────────────────────────────────┐
│ SI PREPARES FOR JOBS                     │
│ Admin/Warehouse issues materials         │
└────────────────────────────────────────┘
         |
         v
┌────────────────────────────────────────┐
│ CREATE ISSUE TO SI TRANSACTION            │
│ POST /api/inventory/movements             │
└────────────────────────────────────────┘
         |
         v
Issue Request {
  FromLocationId: Main_Warehouse
  ToLocationId: SI_InHand (SI-123)
  Items: [
    { MaterialId: "ONU-HG8240H", Quantity: 5, Serials: ["SN001", "SN002", ...] },
    { MaterialId: "PATCHCORD-6M", Quantity: 20 }
  ]
  ServiceInstallerId: "SI-123"
  OrderId: null (generic standby stock)
}
         |
         v
[Validate Stock Availability]
         |
    ┌────┴────┐
    |         |
    v         v
[AVAILABLE] [INSUFFICIENT]
   |            |
   |            v
   |       [Reject Issue]
   |       [Error: Insufficient stock]
   |
   v
[For Serialized Items]
  Update SerializedItem {
    SerialNumber: "SN001"
    CurrentLocationId: SI_InHand (SI-123)
    Status: "WithSI"
  }
         |
         v
[For Non-Serialized Items]
  Update StockBalance {
    MaterialId: "PATCHCORD-6M"
    StockLocationId: Main_Warehouse
    Quantity: 480 (decreased from 500)
  }
  Create StockBalance {
    MaterialId: "PATCHCORD-6M"
    StockLocationId: SI_InHand (SI-123)
    Quantity: 20 (increased)
  }
         |
         v
┌────────────────────────────────────────┐
│ CREATE STOCK MOVEMENT                    │
└────────────────────────────────────────┘
         |
         v
StockMovement {
  MovementType: "IssueToSI"
  FromLocationId: Main_Warehouse
  ToLocationId: SI_InHand (SI-123)
  MaterialId: "ONU-HG8240H"
  Quantity: 5
  ServiceInstallerId: "SI-123"
  Remarks: "Issued for field operations"
}
         |
         v
[STEP 3: INSTALLATION / JOB COMPLETION]
         |
         v
[Order Status: OrderCompleted]
  OrderId: "order-456"
  ServiceInstallerId: "SI-123"
         |
         v
┌────────────────────────────────────────┐
│ SI MARKS MATERIALS USED                  │
│ (Via SI App or Admin)                    │
└────────────────────────────────────────┘
         |
         v
[Materials Used]
  - ONU-HG8240H x 1 (Serial: SN001)
  - PATCHCORD-6M x 2
         |
         v
┌────────────────────────────────────────┐
│ CREATE INSTALLATION MOVEMENT              │
│ POST /api/inventory/movements             │
└────────────────────────────────────────┘
         |
         v
[For Serialized Items]
         |
         v
Update SerializedItem {
  SerialNumber: "SN001"
  CurrentLocationId: CustomerSite
  Status: "InstalledAtCustomer"
  LastOrderId: "order-456"
  LastServiceId: "TBBN1234567"
}
         |
         v
[For Non-Serialized Items]
         |
         v
Update StockBalance {
  MaterialId: "PATCHCORD-6M"
  StockLocationId: SI_InHand (SI-123)
  Quantity: 18 (decreased from 20)
}
         |
         v
Create StockMovement {
  MovementType: "InstallAtCustomer"
  FromLocationId: SI_InHand (SI-123)
  ToLocationId: CustomerSite
  MaterialId: "ONU-HG8240H"
  Quantity: 1
  SerialNumber: "SN001"
  OrderId: "order-456"
  ServiceInstallerId: "SI-123"
  Remarks: "Installed at customer premises"
}
         |
         v
[STEP 4: UNUSED MATERIAL RETURN]
         |
         v
[SI Returns Unused Materials]
  - PATCHCORD-6M x 18 (unused)
         |
         v
┌────────────────────────────────────────┐
│ CREATE RETURN FROM SI TRANSACTION        │
│ POST /api/inventory/movements             │
└────────────────────────────────────────┘
         |
         v
Return Request {
  FromLocationId: SI_InHand (SI-123)
  ToLocationId: Main_Warehouse
  Items: [
    { MaterialId: "PATCHCORD-6M", Quantity: 18 }
  ]
  ServiceInstallerId: "SI-123"
  Reason: "Unused materials"
}
         |
         v
[Update Stock Balances]
  SI Stock: 18 → 0
  Warehouse Stock: 480 → 498
         |
         v
Create StockMovement {
  MovementType: "ReturnFromSI"
  FromLocationId: SI_InHand (SI-123)
  ToLocationId: Main_Warehouse
  MaterialId: "PATCHCORD-6M"
  Quantity: 18
  ServiceInstallerId: "SI-123"
  Remarks: "Return unused materials"
}
```

---

## Goods Receipt (GRN) Flow

```
[Partner Shipment Arrives]
  - Physical stock received
  - Delivery note provided
  - Excel/PDF document (optional)
         |
         v
┌────────────────────────────────────────┐
│ CREATE GRN RECORD                        │
│ POST /api/inventory/grn                  │
└────────────────────────────────────────┘
         |
         v
GRN Form:
  - Partner: TIME
  - Delivery Note Number: "DN-2025-00123"
  - Received Date: 2025-12-10
  - Warehouse: Main_Warehouse
  - Items: [
      { Material: "ONU-HG8240H", Quantity: 100 },
      { Material: "PATCHCORD-6M", Quantity: 500 }
    ]
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE GRN DATA                        │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[VALID] [INVALID]
   |         |
   |         v
   |    [Show Errors]
   |    [Fix and Retry]
   |
   v
[For each Item in GRN]
         |
         v
[Check if Material is Serialized]
         |
    ┌────┴────┐
    |         |
    v         v
[SERIALIZED] [NON-SERIALIZED]
   |              |
   |              v
   |         [Update Stock Balance]
   |             StockBalance {
   |               MaterialId: "PATCHCORD-6M"
   |               StockLocationId: Main_Warehouse
   |               Quantity: 500 (increased)
   |             }
   |
   v
[Capture Serial Numbers]
         |
         v
[For each Serialized Item]
         |
         v
┌────────────────────────────────────────┐
│ CREATE SERIALIZED ITEM                   │
└────────────────────────────────────────┘
         |
         v
SerializedItem {
  MaterialId: "ONU-HG8240H"
  SerialNumber: "SN123456789"
  CompanyId: Cephas
  CurrentLocationId: Main_Warehouse
  Status: "InWarehouse"
  ReceivedDate: 2025-12-10
  PartnerId: TIME
  GrnId: "grn-123"
}
         |
         v
[Create Stock Movement]
         |
         v
StockMovement {
  MovementType: "GRN"
  FromLocationId: null (Partner)
  ToLocationId: Main_Warehouse
  MaterialId: "ONU-HG8240H"
  Quantity: 100
  PartnerId: TIME
  GrnId: "grn-123"
  Remarks: "GRN DN-2025-00123"
  CreatedByUserId: "warehouse-user"
}
         |
         v
[GRN Complete]
         |
         v
[Stock Available in Warehouse]
```

---

## Issue Stock to SI Flow

```
[SI Prepares for Jobs]
  ServiceInstallerId: "SI-123"
  Orders Assigned: [order-456, order-457, order-458]
         |
         v
┌────────────────────────────────────────┐
│ CHECK MATERIAL REQUIREMENTS              │
└────────────────────────────────────────┘
         |
         v
[For each Assigned Order]
  Order Materials:
    - ONU-HG8240H x 1
    - PATCHCORD-6M x 2
    - PATCHCORD-10M x 1
         |
         v
[Aggregate Requirements]
  Total Needed:
    - ONU-HG8240H x 3
    - PATCHCORD-6M x 6
    - PATCHCORD-10M x 3
         |
         v
┌────────────────────────────────────────┐
│ CHECK STOCK AVAILABILITY                 │
└────────────────────────────────────────┘
         |
         v
[For each Material]
  StockBalance.find(
    MaterialId = "ONU-HG8240H"
    StockLocationId = Main_Warehouse
  )
         |
    ┌────┴────┐
    |         |
    v         v
[AVAILABLE] [INSUFFICIENT]
   |            |
   |            v
   |       [Show Warning]
   |       [Suggest Alternatives]
   |       [Allow Partial Issue]
   |
   v
[For Serialized Items]
         |
         v
[Select Specific Serials]
  SerializedItem.find(
    MaterialId = "ONU-HG8240H"
    Status = "InWarehouse"
    CurrentLocationId = Main_Warehouse
    LIMIT 3
  )
         |
         v
Selected Serials: ["SN001", "SN002", "SN003"]
         |
         v
┌────────────────────────────────────────┐
│ CREATE ISSUE TO SI TRANSACTION           │
│ POST /api/inventory/movements             │
└────────────────────────────────────────┘
         |
         v
[For Serialized Items]
         |
         v
[Update SerializedItem Status]
  For each Serial:
    SerializedItem {
      SerialNumber: "SN001"
      CurrentLocationId: SI_InHand (SI-123)
      Status: "WithSI"
      AssignedToSIId: "SI-123"
    }
         |
         v
[For Non-Serialized Items]
         |
         v
[Update Stock Balances]
  Warehouse Stock: 500 → 494 (decreased by 6)
  SI Stock: 0 → 6 (increased by 6)
         |
         v
┌────────────────────────────────────────┐
│ CREATE STOCK MOVEMENT                    │
└────────────────────────────────────────┘
         |
         v
StockMovement {
  MovementType: "IssueToSI"
  FromLocationId: Main_Warehouse
  ToLocationId: SI_InHand (SI-123)
  MaterialId: "ONU-HG8240H"
  Quantity: 3
  SerialNumbers: ["SN001", "SN002", "SN003"]
  ServiceInstallerId: "SI-123"
  OrderIds: ["order-456", "order-457", "order-458"]
  Remarks: "Issued for assigned orders"
}
         |
         v
[Stock Issued to SI]
         |
         v
[SI Can Now Use Materials in Jobs]
```

---

## Installation / Job Completion Flow

```
[Order Status: OrderCompleted]
  OrderId: "order-456"
  ServiceInstallerId: "SI-123"
         |
         v
┌────────────────────────────────────────┐
│ SI CONFIRMS MATERIALS USED                │
│ (Via SI App or Admin)                    │
└────────────────────────────────────────┘
         |
         v
[Materials Used Confirmation]
  - ONU-HG8240H x 1 (Serial: SN001)
  - PATCHCORD-6M x 2
  - PATCHCORD-10M x 1
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE MATERIALS USED                  │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[VALID] [INVALID]
   |         |
   |         v
   |    [Show Errors]
   |    [Require Correction]
   |
   v
Checks:
  ✓ Serial numbers match SI's assigned stock
  ✓ Quantities match planned materials
  ✓ Materials are from SI's in-hand stock
         |
         v
[For Serialized Items]
         |
         v
┌────────────────────────────────────────┐
│ UPDATE SERIALIZED ITEM STATUS             │
└────────────────────────────────────────┘
         |
         v
SerializedItem {
  SerialNumber: "SN001"
  CurrentLocationId: CustomerSite
  Status: "InstalledAtCustomer"
  LastOrderId: "order-456"
  LastServiceId: "TBBN1234567"
  InstalledAt: 2025-12-12
  InstalledBySIId: "SI-123"
}
         |
         v
[For Non-Serialized Items]
         |
         v
┌────────────────────────────────────────┐
│ UPDATE STOCK BALANCES                     │
└────────────────────────────────────────┘
         |
         v
[Decrease SI Stock]
  StockBalance {
    MaterialId: "PATCHCORD-6M"
    StockLocationId: SI_InHand (SI-123)
    Quantity: 4 → 2 (decreased by 2)
  }
         |
         v
[Record Usage in OrderMaterial]
         |
         v
OrderMaterial {
  OrderId: "order-456"
  MaterialId: "ONU-HG8240H"
  PlannedQuantity: 1
  ActualQuantity: 1
  SerialNumber: "SN001"
  IsPlanned: false
  IsUsed: true
  UsedAt: 2025-12-12
}
         |
         v
┌────────────────────────────────────────┐
│ CREATE STOCK MOVEMENT                    │
└────────────────────────────────────────┘
         |
         v
StockMovement {
  MovementType: "InstallAtCustomer"
  FromLocationId: SI_InHand (SI-123)
  ToLocationId: CustomerSite
  MaterialId: "ONU-HG8240H"
  Quantity: 1
  SerialNumber: "SN001"
  OrderId: "order-456"
  ServiceInstallerId: "SI-123"
  Remarks: "Installed at customer premises"
}
         |
         v
[Materials Installed]
         |
         v
[Stock Movements Recorded]
```

---

## Unused Material Return Flow

```
[SI Has Unused Materials]
  ServiceInstallerId: "SI-123"
  Unused Materials:
    - PATCHCORD-6M x 18
    - PATCHCORD-10M x 5
         |
         v
┌────────────────────────────────────────┐
│ CREATE RETURN FROM SI TRANSACTION         │
│ POST /api/inventory/movements             │
└────────────────────────────────────────┘
         |
         v
Return Request {
  FromLocationId: SI_InHand (SI-123)
  ToLocationId: Main_Warehouse
  Items: [
    { MaterialId: "PATCHCORD-6M", Quantity: 18 },
    { MaterialId: "PATCHCORD-10M", Quantity: 5 }
  ]
  ServiceInstallerId: "SI-123"
  Reason: "Unused materials from completed jobs"
}
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE RETURN                         │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[VALID] [INVALID]
   |         |
   |         v
   |    [Show Errors]
   |    [Require Correction]
   |
   v
Checks:
  ✓ SI has the materials in stock
  ✓ Quantities match SI's current stock
  ✓ Materials are in good condition (for return)
         |
         v
[For Non-Serialized Items]
         |
         v
[Update Stock Balances]
  SI Stock: 18 → 0 (decreased)
  Warehouse Stock: 480 → 498 (increased)
         |
         v
[For Serialized Items (if any)]
         |
         v
[Update SerializedItem Status]
  SerializedItem {
    SerialNumber: "SNXXX"
    CurrentLocationId: Main_Warehouse
    Status: "InWarehouse"
    AssignedToSIId: null
  }
         |
         v
┌────────────────────────────────────────┐
│ CREATE STOCK MOVEMENT                    │
└────────────────────────────────────────┘
         |
         v
StockMovement {
  MovementType: "ReturnFromSI"
  FromLocationId: SI_InHand (SI-123)
  ToLocationId: Main_Warehouse
  MaterialId: "PATCHCORD-6M"
  Quantity: 18
  ServiceInstallerId: "SI-123"
  Remarks: "Return unused materials"
}
         |
         v
[Materials Returned to Warehouse]
         |
         v
[Stock Updated]
```

---

## RMA Workflow Flow

```
[STEP 1: RMA INITIATION]
         |
         v
[Faulty Device Detected]
  - Usually in Assurance jobs
  - Device not working
  - Customer complaint
         |
         v
┌────────────────────────────────────────┐
│ PARTNER SENDS MRA EMAIL                  │
│ (Email Parser detects MRA type)         │
└────────────────────────────────────────┘
         |
         v
[Email Parser Extracts]
  - Service ID / Ticket ID
  - Device Serial(s)
  - RMA Number (if present)
  - MRA PDF attachment
         |
         v
┌────────────────────────────────────────┐
│ CREATE RMA REQUEST                       │
│ POST /api/rma/requests                   │
└────────────────────────────────────────┘
         |
         v
RMARequest {
  PartnerId: TIME
  RmaNumber: "RMA-2025-00123" (from partner)
  RequestDate: 2025-12-12
  Reason: "Device faulty - no signal"
  Status: "Requested"
  MraDocumentId: [File ID of PDF]
  Items: [
    {
      SerializedItemId: "serial-123"
      SerialNumber: "SN001"
      OriginalOrderId: "order-456"
      Notes: "Device failed after 2 months"
    }
  ]
}
         |
         v
[Update SerializedItem Status]
  SerializedItem {
    SerialNumber: "SN001"
    Status: "FaultyInWarehouse"
    CurrentLocationId: RMA_Holding_Area
  }
         |
         v
[STEP 2: RMA SHIPMENT TO PARTNER]
         |
         v
┌────────────────────────────────────────┐
│ PREPARE SHIPMENT                         │
│ - Print MRA PDF                          │
│ - Package devices                        │
└────────────────────────────────────────┘
         |
         v
┌────────────────────────────────────────┐
│ CREATE RMA SHIPMENT TRANSACTION           │
│ POST /api/inventory/movements             │
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
  RmaRequestId: "rma-123"
  Remarks: "Shipped to partner for RMA"
}
         |
         v
[Update SerializedItem Status]
  SerializedItem {
    SerialNumber: "SN001"
    Status: "InTransitToPartner"
  }
         |
         v
[Update RMA Request]
  RMARequest {
    Status: "InTransit"
    ShippedDate: 2025-12-15
  }
         |
         v
[STEP 3: RMA CLOSURE]
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
│ PUT /api/rma/requests/{id}                 │
└────────────────────────────────────────┘
         |
         v
[If Repaired/Replaced]
         |
         v
RMARequestItem {
  Result: "Replaced"
  ReplacementSerialNumber: "SN999" (new device)
  ClosedDate: 2025-12-20
}
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
  SerialNumber: "SN999" (replacement)
  PartnerId: TIME
  RmaRequestId: "rma-123"
  Remarks: "Replacement device received"
}
         |
         v
[Create New SerializedItem]
  SerializedItem {
    SerialNumber: "SN999"
    MaterialId: "ONU-HG8240H"
    CurrentLocationId: Main_Warehouse
    Status: "InWarehouse"
    ReceivedFromRMA: true
    OriginalRmaRequestId: "rma-123"
  }
         |
         v
[Update Old SerializedItem]
  SerializedItem {
    SerialNumber: "SN001"
    Status: "RMAClosed"
    ClosedDate: 2025-12-20
  }
         |
         v
[If Credited]
         |
         v
RMARequestItem {
  Result: "Credited"
  CreditAmount: RM 50.00
  ClosedDate: 2025-12-20
}
         |
         v
[Update SerializedItem]
  SerializedItem {
    SerialNumber: "SN001"
    Status: "RMAClosed"
    ClosedDate: 2025-12-20
  }
         |
         v
[Record Credit Note in Finance Module]
         |
         v
[If Scrapped]
         |
         v
RMARequestItem {
  Result: "Scrapped"
  ClosedDate: 2025-12-20
}
         |
         v
[Update SerializedItem]
  SerializedItem {
    SerialNumber: "SN001"
    Status: "Scrapped"
    ClosedDate: 2025-12-20
  }
         |
         v
[Record Scrap Loss in PNL Module]
         |
         v
[RMA Closed]
```

---

## Stock Balance Tracking Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    STOCK BALANCE CALCULATION                             │
└─────────────────────────────────────────────────────────────────────────┘

[For Non-Serialized Items]
───────────────────────────

StockBalance {
  MaterialId: "PATCHCORD-6M"
  StockLocationId: Main_Warehouse
  Quantity: 500
}

When Movement Occurs:
  MovementType: "IssueToSI"
  Quantity: 20
  FromLocationId: Main_Warehouse
  ToLocationId: SI_InHand (SI-123)

Update:
  Main_Warehouse: 500 → 480 (decreased)
  SI_InHand (SI-123): 0 → 20 (increased)

[For Serialized Items]
──────────────────────

SerializedItem {
  SerialNumber: "SN001"
  MaterialId: "ONU-HG8240H"
  CurrentLocationId: Main_Warehouse
  Status: "InWarehouse"
}

When Movement Occurs:
  MovementType: "IssueToSI"
  SerialNumber: "SN001"
  FromLocationId: Main_Warehouse
  ToLocationId: SI_InHand (SI-123)

Update:
  SerializedItem.CurrentLocationId: Main_Warehouse → SI_InHand (SI-123)
  SerializedItem.Status: "InWarehouse" → "WithSI"
  SerializedItem.AssignedToSIId: null → "SI-123"

Stock Balance (for reporting):
  Count(SerializedItem WHERE Status = "InWarehouse") = Available Quantity
```

---

## Inventory States for Serialized Items

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    SERIALIZED ITEM STATUS FLOW                           │
└─────────────────────────────────────────────────────────────────────────┘

[InWarehouse]
  │
  ├─→ [WithSI] (when issued to SI)
  │     │
  │     ├─→ [InstalledAtCustomer] (when installed)
  │     │     │
  │     │     └─→ [FaultyInWarehouse] (if faulty, returned)
  │     │           │
  │     │           └─→ [RMARequested] (when RMA created)
  │     │                 │
  │     │                 └─→ [InTransitToPartner] (when shipped)
  │     │                       │
  │     │                       └─→ [RMAClosed] (when resolved)
  │     │
  │     └─→ [InWarehouse] (if returned unused)
  │
  └─→ [Scrapped] (if damaged beyond repair)

STATUS DEFINITIONS:
───────────────────
InWarehouse: Item in warehouse, available for issue
WithSI: Item assigned to SI, in SI's possession
InstalledAtCustomer: Item installed at customer site
FaultyInWarehouse: Item returned as faulty, in RMA holding area
RMARequested: RMA request created, waiting for shipment
InTransitToPartner: Item shipped to partner for RMA
RMAClosed: RMA resolved (repaired/replaced/credited/scrapped)
Scrapped: Item scrapped, no longer usable
```

---

## Key Components

### StockLocation Entity
```
StockLocation
├── Id (Guid)
├── CompanyId (Guid)
├── Name (string)
├── Type (enum: Warehouse, SI, RMA, CustomerSite)
├── LinkedServiceInstallerId (Guid?, if Type = SI)
├── LinkedBuildingId (Guid?, if Type = CustomerSite)
├── IsActive (bool)
└── CreatedAt, UpdatedAt
```

### StockBalance Entity
```
StockBalance
├── Id (Guid)
├── MaterialId (Guid)
├── StockLocationId (Guid)
├── Quantity (decimal)
└── LastUpdatedAt (DateTime)
```

### SerializedItem Entity
```
SerializedItem
├── Id (Guid)
├── MaterialId (Guid)
├── SerialNumber (string, unique)
├── CompanyId (Guid)
├── CurrentLocationId (Guid)
├── Status (enum: InWarehouse, WithSI, InstalledAtCustomer, 
│           FaultyInWarehouse, RMARequested, InTransitToPartner, 
│           RMAClosed, Scrapped)
├── AssignedToSIId (Guid?)
├── LastOrderId (Guid?)
├── LastServiceId (string?)
├── ReceivedDate (DateTime?)
├── InstalledAt (DateTime?)
├── PartnerId (Guid?)
├── GrnId (Guid?)
└── Notes (string?)
```

### StockMovement Entity
```
StockMovement
├── Id (Guid)
├── CompanyId (Guid)
├── MovementType (enum: GRN, IssueToSI, ReturnFromSI, 
│                  InstallAtCustomer, ReturnFaulty, 
│                  RMAOutbound, RMAInbound, Adjust)
├── FromLocationId (Guid?)
├── ToLocationId (Guid)
├── MaterialId (Guid)
├── Quantity (decimal)
├── SerialNumber (string?, for serialized items)
├── OrderId (Guid?)
├── ServiceInstallerId (Guid?)
├── PartnerId (Guid?)
├── GrnId (Guid?)
├── RmaRequestId (Guid?)
├── Remarks (string?)
├── CreatedByUserId (Guid)
└── CreatedAt (DateTime)
```

### RMARequest Entity
```
RMARequest
├── Id (Guid)
├── CompanyId (Guid)
├── PartnerId (Guid)
├── RmaNumber (string?, from partner)
├── RequestDate (DateTime)
├── Reason (string)
├── Status (enum: Requested, InTransit, Closed)
├── MraDocumentId (Guid?, file reference)
├── ShippedDate (DateTime?)
├── ClosedDate (DateTime?)
└── Items (RMARequestItem[])
```

---

## Integration Points

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    INVENTORY MODULE INTEGRATION                         │
└─────────────────────────────────────────────────────────────────────────┘

1. ORDERS MODULE
   ┌─────────────────────────────────────┐
   │ Material usage on order completion   │
   │ Stock availability checks             │
   │ Material cost tracking                │
   └─────────────────────────────────────┘

2. MATERIALS MODULE
   ┌─────────────────────────────────────┐
   │ Material master data shared          │
   │ Material templates for stock issue   │
   └─────────────────────────────────────┘

3. EMAIL PARSER MODULE
   ┌─────────────────────────────────────┐
   │ MRA email detection and parsing       │
   │ RMA request creation from emails      │
   └─────────────────────────────────────┘

4. PNL MODULE
   ┌─────────────────────────────────────┐
   │ Material costs from inventory         │
   │ Cost of goods sold calculation        │
   └─────────────────────────────────────┘

5. BILLING MODULE
   ┌─────────────────────────────────────┐
   │ Material costs for chargeable items    │
   │ Inventory valuation                    │
   └─────────────────────────────────────┘

6. SERVICE INSTALLERS MODULE
   ┌─────────────────────────────────────┐
   │ SI stock tracking                     │
   │ Material assignment to SIs            │
   └─────────────────────────────────────┘
```

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/02_modules/inventory/OVERVIEW.md` - Inventory Module Overview
- `docs/02_modules/materials/MATERIAL_MANAGEMENT_FLOW.md` - Material Management Flow
- `docs/02_modules/orders/OVERVIEW.md` - Orders Module Overview


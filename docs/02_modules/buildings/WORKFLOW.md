# Buildings – System Workflow Diagram

**Date:** December 12, 2025  
**Purpose:** End-to-end workflow representation for the Buildings module, covering building creation, splitter management, port allocation, material defaults, and infrastructure tracking

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         BUILDINGS MODULE SYSTEM                          │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   BUILDINGS             │      │   BUILDING TYPES       │
        │  (Physical Locations)   │      │  (High-Rise, Terrace)  │
        ├───────────────────────┤      ├───────────────────────┤
        │ • Name/Code            │      │ • Name                 │
        │ • Address              │      │ • Code                 │
        │ • Building Type         │      │ • Description          │
        │ • Installation Method   │      └───────────────────────┘
        │ • Contacts             │
        │ • Rules                │
        └───────────────────────┘
                    │
                    ▼
        ┌───────────────────────┐
        │   SPLITTERS            │
        │  (Port Management)     │
        ├───────────────────────┤
        │ • Splitter ID          │
        │ • Port Allocation      │
        │ • Standby Port Rules   │
        └───────────────────────┘
                    │
                    ▼
        ┌───────────────────────┐
        │   MATERIAL DEFAULTS    │
        │  (Per-Order-Type)      │
        └───────────────────────┘
```

---

## Complete Workflow: Building Management

```
[STEP 1: CREATE BUILDING]
         |
         v
┌────────────────────────────────────────┐
│ CREATE BUILDING                         │
│ POST /api/buildings                     │
└────────────────────────────────────────┘
         |
         v
CreateBuildingDto {
  Name: "Tower A, Block B"
  Code: "TOWER-A-B"
  AddressLine1: "123 Main Street"
  City: "Kuala Lumpur"
  State: "Selangor"
  Postcode: "50000"
  PropertyType: "High-Rise"
  BuildingTypeId: "building-type-1"
  InstallationMethodId: "method-1"
  DepartmentId: "dept-123"
  Latitude: 3.1390
  Longitude: 101.6869
}
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE BUILDING                        │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[VALID] [INVALID]
   |         |
   |         v
   |    [Reject Creation]
   |    [Error: Duplicate building name]
   |
   v
Checks:
  ✓ Name is unique per company
  ✓ Building Type exists (if provided)
  ✓ Installation Method exists (if provided)
  ✓ Department exists (if provided)
  ✓ Address is valid
         |
         v
┌────────────────────────────────────────┐
│ CREATE BUILDING                         │
└────────────────────────────────────────┘
         |
         v
Building {
  Id: "building-123"
  CompanyId: Cephas
  Name: "Tower A, Block B"
  Code: "TOWER-A-B"
  AddressLine1: "123 Main Street"
  City: "Kuala Lumpur"
  State: "Selangor"
  Postcode: "50000"
  PropertyType: "High-Rise"
  BuildingTypeId: "building-type-1"
  InstallationMethodId: "method-1"
  DepartmentId: "dept-123"
  Latitude: 3.1390
  Longitude: 101.6869
  IsActive: true
}
         |
         v
┌────────────────────────────────────────┐
│ AUTO-CREATE STOCK LOCATION                │
│ LocationAutoCreateService.CreateLocationForBuilding()│
└────────────────────────────────────────┘
         |
         v
StockLocation {
  Id: "location-456"
  CompanyId: Cephas
  Name: "building-123 - Tower A, Block B"
  Type: "CustomerSite"
  LinkedBuildingId: "building-123"
}
         |
         v
[STEP 2: ADD BUILDING CONTACTS]
         |
         v
┌────────────────────────────────────────┐
│ CREATE BUILDING CONTACT                  │
│ POST /api/buildings/{id}/contacts        │
└────────────────────────────────────────┘
         |
         v
BuildingContact {
  BuildingId: "building-123"
  Role: "Security Guard"
  Name: "Encik Ali"
  Phone: "0123456789"
  Email: "security@building.com"
  IsPrimary: true
  IsActive: true
}
         |
         v
[STEP 3: ADD BUILDING RULES]
         |
         v
┌────────────────────────────────────────┐
│ CREATE BUILDING RULES                    │
│ POST /api/buildings/{id}/rules            │
└────────────────────────────────────────┘
         |
         v
BuildingRules {
  BuildingId: "building-123"
  AccessRules: "Security card required, register at guardhouse"
  InstallationRules: "No drilling after 6 PM, lift access required"
  OtherNotes: "MDF room on 2nd floor, key from management"
}
         |
         v
[STEP 4: ADD SPLITTERS]
         |
         v
┌────────────────────────────────────────┐
│ CREATE SPLITTER                          │
│ POST /api/buildings/{id}/splitters       │
└────────────────────────────────────────┘
         |
         v
CreateSplitterDto {
  BuildingId: "building-123"
  SplitterTypeId: "splitter-type-1-32"
  PhysicalLocation: "MDF OLT 1_0/11/8"
  TotalPorts: 32
}
         |
         v
┌────────────────────────────────────────┐
│ CREATE SPLITTER                          │
└────────────────────────────────────────┘
         |
         v
Splitter {
  Id: "splitter-789"
  BuildingId: "building-123"
  SplitterTypeId: "splitter-type-1-32"
  PhysicalLocation: "MDF OLT 1_0/11/8"
  TotalPorts: 32
  IsActive: true
}
         |
         v
[Create Splitter Ports]
  For i = 1 to 32:
    SplitterPort {
      SplitterId: "splitter-789"
      PortNumber: i
      Status: "Standby" (if i = 32) | "Available"
      IsStandby: (i == 32)
    }
         |
         v
[STEP 5: ALLOCATE PORT TO ORDER]
         |
         v
[Order Completed]
  Order {
    SplitterId: "splitter-789"
    PortNumber: 8
  }
         |
         v
┌────────────────────────────────────────┐
│ UPDATE SPLITTER PORT                     │
│ PUT /api/splitters/{id}/ports/{port}     │
└────────────────────────────────────────┘
         |
         v
[Check Port Status]
         |
    ┌────┴────┐
    |         |
    v         v
[PORT 32] [OTHER PORT]
   |            |
   |            v
   |       [Check if Available]
   |            |
   |       ┌────┴────┐
   |       |         |
   |       v         v
   |   [AVAILABLE] [IN USE]
   |       |            |
   |       |            v
   |       |       [Error: Port already in use]
   |       |
   |       v
   |   [Update Port]
   |       SplitterPort {
   |         Status: "Used"
   |         ServiceId: "TBBN1234567"
   |         OrderId: "order-456"
   |       }
   |
   v
[Standby Port 32 - Requires Approval]
         |
         v
[Check Approval]
  IF ApprovalAttachmentId IS NULL:
    → Error: "Standby port 32 requires partner approval"
         |
         v
[Update Port with Approval]
  SplitterPort {
    PortNumber: 32
    Status: "Used"
    ServiceId: "TBBN1234567"
    OrderId: "order-456"
    ApprovalAttachmentId: "file-123" (approval document)
  }
         |
         v
[STEP 6: ADD MATERIAL DEFAULTS]
         |
         v
┌────────────────────────────────────────┐
│ CREATE BUILDING DEFAULT MATERIAL         │
│ POST /api/buildings/{id}/default-materials│
└────────────────────────────────────────┘
         |
         v
CreateBuildingDefaultMaterialDto {
  BuildingId: "building-123"
  OrderTypeId: ACTIVATION
  MaterialId: "FIBER-50M"
  DefaultQuantity: 2
  Notes: "This building always needs extra cable"
}
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE DEFAULT MATERIAL                 │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[VALID] [INVALID]
   |         |
   |         v
   |    [Reject: Material must be non-serialized]
   |
   v
Checks:
  ✓ Building exists
  ✓ Order Type exists
  ✓ Material exists and is active
  ✓ Material is non-serialized
  ✓ Quantity > 0
  ✓ No duplicate (BuildingId, OrderTypeId, MaterialId)
         |
         v
┌────────────────────────────────────────┐
│ CREATE BUILDING DEFAULT MATERIAL         │
└────────────────────────────────────────┘
         |
         v
BuildingDefaultMaterial {
  Id: "default-123"
  BuildingId: "building-123"
  OrderTypeId: ACTIVATION
  MaterialId: "FIBER-50M"
  DefaultQuantity: 2
  IsActive: true
  Notes: "This building always needs extra cable"
}
         |
         v
[When Order Created for This Building]
         |
         v
[Check Order Type]
         |
    ┌────┴────┐
    |         |
    v         v
[ACTIVATION] [OTHER]
   |            |
   |            v
   |       [No Default Materials Applied]
   |       [Materials section available for manual addition]
   |
   v
[Building Default Materials Applied (Activation Orders Only)]
  OrderMaterial {
    MaterialId: "FIBER-50M"
    PlannedQuantity: 2 (from Building Default)
    Source: "BuildingDefault"
  }
```

---

## Splitter Port Management Flow

```
[Order Completed with Splitter Details]
         |
         v
┌────────────────────────────────────────┐
│ ALLOCATE SPLITTER PORT                   │
│ PUT /api/splitters/{id}/ports/{port}     │
└────────────────────────────────────────┘
         |
         v
Port Allocation Request {
  SplitterId: "splitter-789"
  PortNumber: 8
  ServiceId: "TBBN1234567"
  OrderId: "order-456"
  ApprovalAttachmentId: null (if port 32)
}
         |
         v
[Get Splitter and Port]
  Splitter.find(Id = "splitter-789")
  SplitterPort.find(
    SplitterId = "splitter-789"
    PortNumber = 8
  )
         |
         v
[Check Port Status]
         |
    ┌────┴────┐
    |         |
    v         v
[AVAILABLE] [IN USE]
   |            |
   |            v
   |       [Error: Port already in use]
   |
   v
[Check if Port 32 (Standby)]
         |
    ┌────┴────┐
    |         |
    v         v
[PORT 32] [OTHER PORT]
   |            |
   |            v
   |       [Update Port]
   |           SplitterPort {
   |             Status: "Used"
   |             ServiceId: "TBBN1234567"
   |             OrderId: "order-456"
   |           }
   |
   v
[Standby Port - Check Approval]
         |
         v
[If ApprovalAttachmentId IS NULL]
  → Error: "Standby port 32 requires partner approval. Please attach approval document."
         |
         v
[If ApprovalAttachmentId IS NOT NULL]
         |
         v
[Update Port with Approval]
  SplitterPort {
    PortNumber: 32
    Status: "Used"
    ServiceId: "TBBN1234567"
    OrderId: "order-456"
    ApprovalAttachmentId: "file-123"
    UsedAt: 2025-12-15
  }
         |
         v
[Port Allocated]
```

---

## Building Matching Flow

```
[Email Parser Extracts Address]
         |
         v
┌────────────────────────────────────────┐
│ MATCH BUILDING FROM ADDRESS              │
│ BuildingMatchingService.MatchBuilding()  │
└────────────────────────────────────────┘
         |
         v
[Parse Address]
  ParsedAddress {
    AddressLine1: "123 Main Street"
    City: "Kuala Lumpur"
    State: "Selangor"
    Postcode: "50000"
    BuildingName: "Tower A"
  }
         |
         v
[Step 1: Exact Match by Address]
  Building.find(
    AddressLine1 = "123 Main Street"
    City = "Kuala Lumpur"
    Postcode = "50000"
  )
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NOT FOUND]
   |            |
   |            v
   |       [Step 2: Fuzzy Match by Building Name]
   |           Building.find(
   |             Name CONTAINS "Tower A"
   |             City = "Kuala Lumpur"
   |           )
   |
   v
[Return Matched Building]
  Building {
    Id: "building-123"
    Name: "Tower A, Block B"
    MatchScore: 100 (exact match)
  }
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NOT FOUND]
   |            |
   |            v
   |       [Step 3: Fuzzy Match by Postcode + Street]
   |           Building.find(
   |             Postcode = "50000"
   |             AddressLine1 SIMILAR TO "Main Street"
   |           )
   |
   v
[Return Matched Building]
  Building {
    Id: "building-123"
    Name: "Tower A, Block B"
    MatchScore: 85 (fuzzy match)
  }
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NO MATCH]
   |            |
   |            v
   |       [Return null]
   |       [Building not found]
   |
   v
[Building Matched]
```

---

## Building Merge Flow (admin tool)

Admin tool to merge duplicate buildings: list similar buildings, preview reassignments, then merge (orders and parsed drafts reassigned to target, source soft-deleted).

```
[Admin: Settings > Buildings > Merge duplicates]
         |
         v
┌────────────────────────────────────────┐
│ 1. SELECT SOURCE BUILDING               │
│    (building to be merged away)         │
│    GET /api/buildings (list)            │
└────────────────────────────────────────┘
         |
         v
┌────────────────────────────────────────┐
│ 2. GET MERGE CANDIDATES                 │
│    GET /api/buildings/merge-candidates  │
│    ?buildingId={sourceId}               │
└────────────────────────────────────────┘
         |
         v
[Similar buildings returned (name/address/postcode); if none, user can still pick any building as target]
         |
         v
┌────────────────────────────────────────┐
│ 3. SELECT TARGET BUILDING              │
│    (building to keep)                   │
└────────────────────────────────────────┘
         |
         v
┌────────────────────────────────────────┐
│ 4. GET MERGE PREVIEW                   │
│    GET /api/buildings/merge-preview     │
│    ?sourceBuildingId=&targetBuildingId=│
└────────────────────────────────────────┘
         |
         v
BuildingMergePreviewDto {
  SourceBuildingName, TargetBuildingName
  OrdersToReassignCount, ParsedDraftsToReassignCount
  OrderIdsToReassign
}
         |
         v
┌────────────────────────────────────────┐
│ 5. CONFIRM & MERGE                     │
│    POST /api/buildings/merge            │
│    Body: { sourceBuildingId, targetBuildingId } │
└────────────────────────────────────────┘
         |
         v
[Backend: Reassign orders and parsed drafts to target; set source IsActive=false, IsDeleted=true, DeletedAt, DeletedByUserId]
         |
         v
BuildingMergeResultDto {
  OrdersMovedCount, ParsedDraftsReassignedCount
  SourceSoftDeleted, Message
}
```

**UI:** Settings > Buildings page has a "Merge duplicates" button; Settings index has a "Merge buildings" card. Both link to **Building Merge** page at `/settings/buildings-merge`.

---

## Entities Involved

### Building Entity
```
Building
├── Id (Guid)
├── CompanyId (Guid)
├── Name (string)
├── Code (string?)
├── AddressLine1 (string)
├── AddressLine2 (string?)
├── City (string)
├── State (string)
├── Postcode (string)
├── Area (string?)
├── Latitude (decimal?)
├── Longitude (decimal?)
├── PropertyType (string)
├── BuildingTypeId (Guid?)
├── InstallationMethodId (Guid?)
├── DepartmentId (Guid?)
├── RfbAssignedDate (DateTime?)
├── FirstOrderDate (DateTime?)
├── Notes (string?)
├── IsActive (bool)
└── CreatedAt, UpdatedAt
```

### BuildingContact Entity
```
BuildingContact
├── Id (Guid)
├── BuildingId (Guid)
├── Role (string)
├── Name (string)
├── Phone (string?)
├── Email (string?)
├── Remarks (string?)
├── IsPrimary (bool)
├── IsActive (bool)
└── CreatedAt, UpdatedAt
```

### BuildingRules Entity
```
BuildingRules
├── Id (Guid)
├── BuildingId (Guid)
├── AccessRules (string?)
├── InstallationRules (string?)
├── OtherNotes (string?)
└── CreatedAt, UpdatedAt
```

### Splitter Entity
```
Splitter
├── Id (Guid)
├── BuildingId (Guid)
├── SplitterTypeId (Guid)
├── PhysicalLocation (string)
├── TotalPorts (int)
├── IsActive (bool)
└── CreatedAt, UpdatedAt
```

### SplitterPort Entity
```
SplitterPort
├── Id (Guid)
├── SplitterId (Guid)
├── PortNumber (int)
├── Status (string: Available, Used, Reserved, Standby)
├── ServiceId (string?)
├── OrderId (Guid?)
├── ApprovalAttachmentId (Guid?, required for port 32)
├── UsedAt (DateTime?)
└── CreatedAt, UpdatedAt
```

### BuildingDefaultMaterial Entity
```
BuildingDefaultMaterial
├── Id (Guid)
├── BuildingId (Guid)
├── OrderTypeId (Guid)
├── MaterialId (Guid)
├── DefaultQuantity (decimal)
├── IsActive (bool)
├── Notes (string?)
└── CreatedAt, UpdatedAt
```

---

## API Endpoints Involved

### Buildings
- `GET /api/buildings` - List buildings with filters
- `GET /api/buildings/{id}` - Get building details
- `POST /api/buildings` - Create building
- `PUT /api/buildings/{id}` - Update building
- `DELETE /api/buildings/{id}` - Delete building (soft delete)

### Building Merge (admin tool)
- `GET /api/buildings/merge-candidates?buildingId={id}` - Get similar buildings that could be merge targets for a given building
- `GET /api/buildings/merge-preview?sourceBuildingId={id}&targetBuildingId={id}` - Preview merge: orders and parsed drafts to be reassigned
- `POST /api/buildings/merge` - Merge source building into target: reassign orders and parsed drafts to target, soft-delete source (body: `{ sourceBuildingId, targetBuildingId }`)

### Building Contacts
- `GET /api/buildings/{id}/contacts` - Get building contacts
- `POST /api/buildings/{id}/contacts` - Create contact
- `PUT /api/buildings/{id}/contacts/{contactId}` - Update contact
- `DELETE /api/buildings/{id}/contacts/{contactId}` - Delete contact

### Building Rules
- `GET /api/buildings/{id}/rules` - Get building rules
- `POST /api/buildings/{id}/rules` - Create/update rules
- `DELETE /api/buildings/{id}/rules` - Delete rules

### Splitters
- `GET /api/buildings/{id}/splitters` - Get building splitters
- `POST /api/buildings/{id}/splitters` - Create splitter
- `PUT /api/splitters/{id}` - Update splitter
- `PUT /api/splitters/{id}/ports/{port}` - Allocate port
- `GET /api/splitters/{id}/ports` - Get splitter ports

### Building Default Materials
- `GET /api/buildings/{id}/default-materials` - Get default materials
- `POST /api/buildings/{id}/default-materials` - Create default material
- `PUT /api/buildings/{id}/default-materials/{id}` - Update default material
- `DELETE /api/buildings/{id}/default-materials/{id}` - Delete default material

---

## Module Rules & Validations

### Building Creation Rules
- Name must be unique per company
- Address must include at least AddressLine1
- City and State required
- Postcode must be valid format
- Building Type must exist (if provided)
- Installation Method must exist (if provided)
- Department must exist (if provided)

### Splitter Rules
- Splitter must belong to a building
- Splitter Type must exist
- Total Ports must match Splitter Type
- Ports auto-created on splitter creation
- Port 32 is reserved as standby (for 1:32 splitters)

### Port Allocation Rules
- Port must be Available before allocation
- Port 32 (standby) requires ApprovalAttachmentId
- Port cannot be allocated if already Used
- Service ID and Order ID recorded on allocation
- Port status: Available → Used

### Building Default Material Rules
- Material must be non-serialized
- Material must exist and be active
- Order Type must exist
- Quantity must be > 0
- Unique constraint: (BuildingId, OrderTypeId, MaterialId)
- **Defaults applied automatically only for Activation order types** (Activation, FTTH, FTTO)
- **Other order types**: Default materials are not auto-loaded; materials section remains available for manual addition when needed

### Building Matching Rules
- Exact match by full address (AddressLine1 + City + Postcode)
- Fuzzy match by building name + city
- Fuzzy match by postcode + street name
- Match score calculated (100 = exact, < 100 = fuzzy)
- Multiple matches return best match

---

## Integration Points

### Orders Module
- Orders linked to buildings via BuildingId
- Building defaults applied on order creation
- Splitter and port allocated on order completion

### Inventory Module
- Stock location auto-created for building (CustomerSite type)
- Material defaults from building applied to orders
- Serialized items tracked at building location

### Email Parser Module
- Building matching from parsed addresses
- Address normalization for matching
- Building auto-creation (optional, if enabled)

### Materials Module
- Material templates may consider building type
- Building defaults override template items
- Material availability checked

### Scheduler Module
- Building location (GPS) used for route optimization
- Building contacts used for coordination
- Building rules displayed to SI

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/02_modules/buildings/OVERVIEW.md` - Buildings module overview
- `docs/02_modules/orders/WORKFLOW.md` - Orders workflow
- `docs/02_modules/materials/MATERIAL_MANAGEMENT_FLOW.md` - Material management flow


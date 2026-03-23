# Materials – System Workflow Diagram

**Date:** December 12, 2025  
**Purpose:** End-to-end workflow representation for the Materials module

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    MATERIAL MANAGEMENT SYSTEM                             │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   MATERIAL MASTER       │      │   MATERIAL TEMPLATES    │
        │  (Item Catalogue)       │      │  (Default Kits)        │
        ├───────────────────────┤      ├───────────────────────┤
        │ • Material Code         │      │ • Order Type based      │
        │ • Description           │      │ • Building Type based   │
        │ • Category              │      │ • Partner specific      │
        │ • Serialized/Non-Serial│      │ • Material Items        │
        │ • Unit of Measure       │      │ • Quantities            │
        │ • Default Cost          │      │ • IsDefault flag        │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │   BUILDING DEFAULT MATERIALS   │
                    │  (Per-Building Overrides)      │
                    └───────────────────────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │   ORDER MATERIAL POPULATION    │
                    │  (Auto-apply to Orders)        │
                    └───────────────────────────────┘
```

---

## Complete Flow: Material Setup to Order Population

```
[STEP 1: CREATE MATERIAL MASTER]
         |
         v
┌────────────────────────────────────────┐
│ CREATE MATERIAL                         │
│ POST /api/materials                     │
└────────────────────────────────────────┘
         |
         v
Material {
  MaterialCode: "ONU-HG8240H"
  Description: "Huawei ONU HG8240H"
  Category: "ONU"
  IsSerialized: true
  PartnerProvided: true
  UnitOfMeasure: "UNIT"
  DefaultCost: RM 50.00
  CompanyId: Cephas
  IsActive: true
}
         |
         v
[STEP 2: CREATE MATERIAL TEMPLATE]
         |
         v
┌────────────────────────────────────────┐
│ CREATE MATERIAL TEMPLATE                │
│ POST /api/material-templates            │
└────────────────────────────────────────┘
         |
         v
MaterialTemplate {
  Name: "TIME Prelaid High-Rise Kit"
  CompanyId: Cephas
  OrderType: "ACTIVATION"
  BuildingTypeId: HighRise
  PartnerId: TIME (optional)
  IsDefault: true
  IsActive: true
}
         |
         v
[STEP 3: ADD TEMPLATE ITEMS]
         |
         v
┌────────────────────────────────────────┐
│ ADD MATERIAL TEMPLATE ITEMS             │
│ POST /api/material-templates/{id}/items │
└────────────────────────────────────────┘
         |
         v
MaterialTemplateItem {
  MaterialTemplateId: [Template ID]
  MaterialId: "ONU-HG8240H"
  Quantity: 1
  UnitOfMeasure: "UNIT"
  IsSerialised: true
  Notes: "Standard ONU for activation"
}
         |
         v
[Add More Items]
  - Patchcord 6m x 2
  - Patchcord 10m x 1
  - UPC Connector x 1
  - APC Connector x 1
         |
         v
[STEP 4: CREATE BUILDING DEFAULT MATERIALS]
         |
         v
┌────────────────────────────────────────┐
│ CREATE BUILDING DEFAULT MATERIAL        │
│ POST /api/buildings/{id}/default-materials│
└────────────────────────────────────────┘
         |
         v
BuildingDefaultMaterial {
  BuildingId: "building-123"
  OrderTypeId: ACTIVATION
  MaterialId: "FIBER-50M"
  DefaultQuantity: 2
  IsActive: true
  Notes: "This building always needs extra cable"
}
         |
         v
[STEP 5: ORDER CREATION TRIGGERS MATERIAL POPULATION]
         |
         v
[Order Created]
  Order {
    OrderTypeId: ACTIVATION
    PartnerId: TIME
    BuildingId: "building-123"
    BuildingTypeId: HighRise
    CompanyId: Cephas
  }
         |
         v
┌────────────────────────────────────────┐
│ RESOLVE MATERIAL TEMPLATE               │
│ MaterialTemplateService.GetEffectiveTemplate()│
└────────────────────────────────────────┘
         |
         v
[Template Resolution Priority]
         |
    ┌────┴────┐
    |         |
    v         v
[Priority 1: Partner-Specific Template]
  MaterialTemplate.find(
    CompanyId = Cephas
    PartnerId = TIME
    OrderType = ACTIVATION
    BuildingTypeId = HighRise
    IsActive = true
  )
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND]  [NOT FOUND]
   |            |
   |            v
   |       [Priority 2: Default Template]
   |           MaterialTemplate.find(
   |             CompanyId = Cephas
   |             PartnerId = null
   |             OrderType = ACTIVATION
   |             BuildingTypeId = HighRise
   |             IsDefault = true
   |             IsActive = true
   |           )
   |            |
   |       ┌────┴────┐
   |       |         |
   |       v         v
   |   [FOUND]  [NOT FOUND]
   |       |            |
   |       |            v
   |       |       [Priority 3: Template without Building Type]
   |       |           MaterialTemplate.find(
   |       |             CompanyId = Cephas
   |       |             PartnerId = TIME (or null)
   |       |             OrderType = ACTIVATION
   |       |             BuildingTypeId = null
   |       |             IsActive = true
   |       |           )
   |       |            |
   |       |       ┌────┴────┐
   |       |       |         |
   |       |       v         v
   |       |   [FOUND]  [NO TEMPLATE]
   |       |       |            |
   |       |       |            v
   |       |       |       [No materials auto-populated]
   |       |       |       [Manual selection required]
   |       |       |            |
   |       └───────┴────────────┘
   |               |
   └───────────────┘
         |
         v
[Template Selected: "TIME Prelaid High-Rise Kit"]
         |
         v
┌────────────────────────────────────────┐
│ APPLY TEMPLATE ITEMS TO ORDER            │
└────────────────────────────────────────┘
         |
         v
[For each MaterialTemplateItem]
         |
         v
Create OrderMaterial {
  OrderId: [Order ID]
  MaterialId: "ONU-HG8240H"
  PlannedQuantity: 1
  ActualQuantity: null
  IsPlanned: true
  IsSerialised: true
  Source: "MaterialTemplate"
}
         |
         v
[Repeat for all Template Items]
  - ONU x 1
  - Patchcord 6m x 2
  - Patchcord 10m x 1
  - UPC Connector x 1
  - APC Connector x 1
         |
         v
┌────────────────────────────────────────┐
│ APPLY BUILDING DEFAULT MATERIALS        │
│ (Overrides Template Items)               │
└────────────────────────────────────────┘
         |
         v
[Query Building Default Materials]
  BuildingDefaultMaterial.find(
    BuildingId = "building-123"
    OrderTypeId = ACTIVATION
    IsActive = true
  )
         |
         v
[For each Building Default Material]
         |
         v
[Check if Material Already Exists in Order]
         |
    ┌────┴────┐
    |         |
    v         v
[EXISTS] [NOT EXISTS]
   |            |
   |            v
   |       [Create New OrderMaterial]
   |           OrderMaterial {
   |             OrderId: [Order ID]
   |             MaterialId: "FIBER-50M"
   |             PlannedQuantity: 2 (from Building Default)
   |             IsPlanned: true
   |             Source: "BuildingDefault"
   |           }
   |
   v
[Update Existing OrderMaterial]
  OrderMaterial.PlannedQuantity = BuildingDefault.DefaultQuantity
  OrderMaterial.Source = "BuildingDefault" (overrides Template)
         |
         v
[STEP 6: FINAL MATERIAL LIST]
         |
         v
Order Materials:
  1. ONU-HG8240H x 1 (from Template)
  2. Patchcord 6m x 2 (from Template)
  3. Patchcord 10m x 1 (from Template)
  4. UPC Connector x 1 (from Template)
  5. APC Connector x 1 (from Template)
  6. FIBER-50M x 2 (from Building Default, overrides if existed in template)
         |
         v
[Materials Ready for Order]
         |
         v
[User can manually add/remove materials]
```

---

## Material Master Creation Flow

```
[Admin: Settings → Materials → New Material]
         |
         v
┌────────────────────────────────────────┐
│ MATERIAL CREATION FORM                   │
└────────────────────────────────────────┘
         |
         v
Form Fields:
  - Material Code: "ONU-HG8240H" (unique per company)
  - Description: "Huawei ONU HG8240H"
  - Category: [ONU | Router | Cable | Connector | ...]
  - Is Serialized: true/false
  - Partner Provided: true/false
  - Unit of Measure: [UNIT | METER | PIECE | ...]
  - Default Cost: RM 50.00
  - Company: Cephas
  - Is Active: true
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE MATERIAL DATA                   │
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
Checks:
  ✓ Material Code unique per company
  ✓ Description not empty
  ✓ Category valid
  ✓ Unit of Measure valid
  ✓ Default Cost >= 0
         |
         v
┌────────────────────────────────────────┐
│ CREATE MATERIAL                         │
│ POST /api/materials                     │
└────────────────────────────────────────┘
         |
         v
Material Created {
  Id: "material-123"
  MaterialCode: "ONU-HG8240H"
  Description: "Huawei ONU HG8240H"
  Category: "ONU"
  IsSerialized: true
  PartnerProvided: true
  UnitOfMeasure: "UNIT"
  DefaultCost: RM 50.00
  CompanyId: Cephas
  IsActive: true
  CreatedAt: 2025-12-12
}
         |
         v
[Material Ready for Use]
```

---

## Material Template Creation Flow

```
[Admin: Settings → Material Templates → New Template]
         |
         v
┌────────────────────────────────────────┐
│ MATERIAL TEMPLATE CREATION FORM          │
└────────────────────────────────────────┘
         |
         v
Form Fields:
  - Name: "TIME Prelaid High-Rise Kit"
  - Company: Cephas
  - Order Type: ACTIVATION
  - Building Type: High-Rise (optional)
  - Partner: TIME (optional)
  - Is Default: true
  - Is Active: true
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE TEMPLATE DATA                   │
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
Checks:
  ✓ Name not empty
  ✓ Order Type valid
  ✓ Building Type valid (if provided)
  ✓ Partner valid (if provided)
  ✓ IsDefault: Only one default per (Company, OrderType, BuildingType, Partner)
         |
         v
┌────────────────────────────────────────┐
│ CREATE MATERIAL TEMPLATE                │
│ POST /api/material-templates            │
└────────────────────────────────────────┘
         |
         v
MaterialTemplate Created {
  Id: "template-123"
  Name: "TIME Prelaid High-Rise Kit"
  CompanyId: Cephas
  OrderType: "ACTIVATION"
  BuildingTypeId: HighRise
  PartnerId: TIME
  IsDefault: true
  IsActive: true
}
         |
         v
[STEP 2: ADD MATERIAL ITEMS TO TEMPLATE]
         |
         v
┌────────────────────────────────────────┐
│ ADD MATERIAL TEMPLATE ITEMS              │
│ POST /api/material-templates/{id}/items  │
└────────────────────────────────────────┘
         |
         v
[For each Material in Kit]
         |
         v
MaterialTemplateItem {
  MaterialTemplateId: "template-123"
  MaterialId: "ONU-HG8240H"
  Quantity: 1
  UnitOfMeasure: "UNIT"
  IsSerialised: true (from Material)
  Notes: "Standard ONU for activation"
}
         |
         v
[Add Multiple Items]
  Item 1: ONU-HG8240H x 1
  Item 2: PATCHCORD-6M x 2
  Item 3: PATCHCORD-10M x 1
  Item 4: UPC-CONNECTOR x 1
  Item 5: APC-CONNECTOR x 1
         |
         v
[Template Complete]
         |
         v
[Template Ready for Use]
```

---

## Material Template Resolution Flow

```
[Order Created/Updated]
  Order {
    CompanyId: Cephas
    PartnerId: TIME
    OrderTypeId: ACTIVATION
    BuildingTypeId: HighRise
  }
         |
         v
┌────────────────────────────────────────┐
│ RESOLVE MATERIAL TEMPLATE                 │
│ MaterialTemplateService.GetEffectiveTemplate()│
└────────────────────────────────────────┘
         |
         v
[Priority 1: Partner-Specific Template]
  MaterialTemplate.find(
    CompanyId = Cephas
    PartnerId = TIME
    OrderType = ACTIVATION
    BuildingTypeId = HighRise
    IsActive = true
  )
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND]  [NOT FOUND]
   |            |
   |            v
   |       [Priority 2: Default Template (No Partner)]
   |           MaterialTemplate.find(
   |             CompanyId = Cephas
   |             PartnerId = null
   |             OrderType = ACTIVATION
   |             BuildingTypeId = HighRise
   |             IsDefault = true
   |             IsActive = true
   |           )
   |            |
   |       ┌────┴────┐
   |       |         |
   |       v         v
   |   [FOUND]  [NOT FOUND]
   |       |            |
   |       |            v
   |       |       [Priority 3: Template without Building Type]
   |       |           MaterialTemplate.find(
   |       |             CompanyId = Cephas
   |       |             PartnerId = TIME (or null)
   |       |             OrderType = ACTIVATION
   |       |             BuildingTypeId = null
   |       |             IsActive = true
   |           )
   |       |            |
   |       |       ┌────┴────┐
   |       |       |         |
   |       |       v         v
   |       |   [FOUND]  [NO TEMPLATE]
   |       |       |            |
   |       |       |            v
   |       |       |       [Return null]
   |       |       |       [No auto-population]
   |       |       |            |
   |       └───────┴────────────┘
   |               |
   └───────────────┘
         |
         v
[Template Selected]
  MaterialTemplate {
    Id: "template-123"
    Name: "TIME Prelaid High-Rise Kit"
    Items: [
      { MaterialId: "ONU-HG8240H", Quantity: 1 },
      { MaterialId: "PATCHCORD-6M", Quantity: 2 },
      { MaterialId: "PATCHCORD-10M", Quantity: 1 },
      { MaterialId: "UPC-CONNECTOR", Quantity: 1 },
      { MaterialId: "APC-CONNECTOR", Quantity: 1 }
    ]
  }
         |
         v
[Return Template]
```

---

## Building Default Material Flow

```
[Admin: Buildings → Building Detail → Default Materials Tab]
         |
         v
┌────────────────────────────────────────┐
│ CREATE BUILDING DEFAULT MATERIAL        │
│ POST /api/buildings/{id}/default-materials│
└────────────────────────────────────────┘
         |
         v
Form Fields:
  - Building: "ROYCE RESIDENCE" (pre-selected)
  - Order Type: ACTIVATION
  - Material: [Select from Materials list]
  - Default Quantity: 2
  - Notes: "This building always needs extra cable"
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE BUILDING DEFAULT                │
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
Checks:
  ✓ Building exists
  ✓ Order Type valid
  ✓ Material exists and is active
  ✓ Material is non-serialized (for building defaults)
  ✓ Quantity > 0
  ✓ No duplicate (BuildingId, OrderTypeId, MaterialId)
         |
         v
┌────────────────────────────────────────┐
│ CREATE BUILDING DEFAULT MATERIAL        │
└────────────────────────────────────────┘
         |
         v
BuildingDefaultMaterial Created {
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
[Building Default Ready]
         |
         v
[When Order Created for This Building]
         |
         v
┌────────────────────────────────────────┐
│ APPLY BUILDING DEFAULT MATERIALS         │
│ BuildingDefaultMaterialService.GetMaterialsForOrder()│
└────────────────────────────────────────┘
         |
         v
[Query Building Defaults]
  BuildingDefaultMaterial.find(
    BuildingId = "building-123"
    OrderTypeId = ACTIVATION
    IsActive = true
  )
         |
         v
[For each Building Default]
         |
         v
[Check if Material Already in Order]
  (from Template or manual)
         |
    ┌────┴────┐
    |         |
    v         v
[EXISTS] [NOT EXISTS]
   |            |
   |            v
   |       [Create OrderMaterial]
   |           OrderMaterial {
   |             OrderId: [Order ID]
   |             MaterialId: "FIBER-50M"
   |             PlannedQuantity: 2
   |             Source: "BuildingDefault"
   |           }
   |
   v
[Update Existing OrderMaterial]
  OrderMaterial.PlannedQuantity = BuildingDefault.DefaultQuantity
  OrderMaterial.Source = "BuildingDefault"
         |
         v
[Building Defaults Applied]
```

---

## Order Material Population Flow

```
[Order Created or Building Assigned]
  Order {
    Id: "order-456"
    CompanyId: Cephas
    PartnerId: TIME
    OrderTypeId: ACTIVATION
    BuildingId: "building-123"
    BuildingTypeId: HighRise
  }
         |
         v
┌────────────────────────────────────────┐
│ STEP 1: RESOLVE MATERIAL TEMPLATE         │
│ MaterialTemplateService.GetEffectiveTemplate()│
└────────────────────────────────────────┘
         |
         v
[Template Resolution]
  (see Material Template Resolution Flow above)
         |
    ┌────┴────┐
    |         |
    v         v
[TEMPLATE FOUND] [NO TEMPLATE]
   |                  |
   |                  v
   |             [Skip to Step 2]
   |
   v
[Apply Template Items]
         |
         v
[For each MaterialTemplateItem]
         |
         v
Create OrderMaterial {
  OrderId: "order-456"
  MaterialId: [from Template Item]
  PlannedQuantity: [from Template Item.Quantity]
  ActualQuantity: null
  IsPlanned: true
  IsSerialised: [from Material]
  Source: "MaterialTemplate"
  CreatedAt: 2025-12-12
}
         |
         v
[Template Items Applied]
  - ONU-HG8240H x 1
  - PATCHCORD-6M x 2
  - PATCHCORD-10M x 1
  - UPC-CONNECTOR x 1
  - APC-CONNECTOR x 1
         |
         v
┌────────────────────────────────────────┐
│ STEP 2: APPLY BUILDING DEFAULT MATERIALS │
│ BuildingDefaultMaterialService.GetMaterialsForOrder()│
└────────────────────────────────────────┘
         |
         v
[Query Building Defaults]
  BuildingDefaultMaterial.find(
    BuildingId = "building-123"
    OrderTypeId = ACTIVATION
    IsActive = true
  )
         |
         v
[Building Defaults Found]
  - FIBER-50M x 2
         |
         v
[For each Building Default]
         |
         v
[Check if Material Already in Order]
  OrderMaterial.find(
    OrderId = "order-456"
    MaterialId = "FIBER-50M"
  )
         |
    ┌────┴────┐
    |         |
    v         v
[EXISTS] [NOT EXISTS]
   |            |
   |            v
   |       [Create OrderMaterial]
   |           OrderMaterial {
   |             OrderId: "order-456"
   |             MaterialId: "FIBER-50M"
   |             PlannedQuantity: 2
   |             Source: "BuildingDefault"
   |           }
   |
   v
[Update Existing OrderMaterial]
  OrderMaterial.PlannedQuantity = 2 (from Building Default)
  OrderMaterial.Source = "BuildingDefault" (overrides Template)
         |
         v
[Building Defaults Applied]
         |
         v
┌────────────────────────────────────────┐
│ STEP 3: FINAL MATERIAL LIST               │
└────────────────────────────────────────┘
         |
         v
Order Materials (Final):
  1. ONU-HG8240H x 1 (from Template)
  2. PATCHCORD-6M x 2 (from Template)
  3. PATCHCORD-10M x 1 (from Template)
  4. UPC-CONNECTOR x 1 (from Template)
  5. APC-CONNECTOR x 1 (from Template)
  6. FIBER-50M x 2 (from Building Default)
         |
         v
[Materials Ready]
         |
         v
[User can manually add/remove materials]
```

---

## Material Template Priority Examples

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    TEMPLATE RESOLUTION EXAMPLES                          │
└─────────────────────────────────────────────────────────────────────────┘

EXAMPLE 1: Partner-Specific Template (Highest Priority)
────────────────────────────────────────────────────────
Order:
  CompanyId: Cephas
  PartnerId: TIME
  OrderType: ACTIVATION
  BuildingTypeId: HighRise

Resolution:
  Priority 1: MaterialTemplate.find(
    CompanyId = Cephas
    PartnerId = TIME
    OrderType = ACTIVATION
    BuildingTypeId = HighRise
  )
  → FOUND: "TIME Prelaid High-Rise Kit"
  → Use this template

EXAMPLE 2: Default Template (No Partner-Specific)
──────────────────────────────────────────────────
Order:
  CompanyId: Cephas
  PartnerId: Celcom
  OrderType: ACTIVATION
  BuildingTypeId: HighRise

Resolution:
  Priority 1: MaterialTemplate.find(
    CompanyId = Cephas
    PartnerId = Celcom
    OrderType = ACTIVATION
    BuildingTypeId = HighRise
  )
  → NOT FOUND

  Priority 2: MaterialTemplate.find(
    CompanyId = Cephas
    PartnerId = null
    OrderType = ACTIVATION
    BuildingTypeId = HighRise
    IsDefault = true
  )
  → FOUND: "Standard Prelaid High-Rise Kit"
  → Use this template

EXAMPLE 3: Template without Building Type (Fallback)
────────────────────────────────────────────────────
Order:
  CompanyId: Cephas
  PartnerId: TIME
  OrderType: ACTIVATION
  BuildingTypeId: Terrace (no template for Terrace)

Resolution:
  Priority 1: MaterialTemplate.find(
    CompanyId = Cephas
    PartnerId = TIME
    OrderType = ACTIVATION
    BuildingTypeId = Terrace
  )
  → NOT FOUND

  Priority 2: MaterialTemplate.find(
    CompanyId = Cephas
    PartnerId = null
    OrderType = ACTIVATION
    BuildingTypeId = Terrace
    IsDefault = true
  )
  → NOT FOUND

  Priority 3: MaterialTemplate.find(
    CompanyId = Cephas
    PartnerId = TIME
    OrderType = ACTIVATION
    BuildingTypeId = null
  )
  → FOUND: "TIME Activation General Kit"
  → Use this template
```

---

## Key Components

### Material Entity
```
Material
├── Id (Guid)
├── MaterialCode (string, unique per company)
├── Description (string)
├── Category (string: ONU, Router, Cable, Connector, etc.)
├── IsSerialized (bool)
├── PartnerProvided (bool)
├── UnitOfMeasure (string: UNIT, METER, PIECE, etc.)
├── DefaultCost (decimal)
├── CompanyId (Guid)
├── IsActive (bool)
└── CreatedAt, UpdatedAt
```

### MaterialTemplate Entity
```
MaterialTemplate
├── Id (Guid)
├── Name (string)
├── CompanyId (Guid)
├── OrderType (string: ACTIVATION, ASSURANCE, etc.)
├── BuildingTypeId (Guid?)
├── PartnerId (Guid?)
├── IsDefault (bool)
├── IsActive (bool)
├── CreatedAt, CreatedByUserId
└── UpdatedAt, UpdatedByUserId
```

### MaterialTemplateItem Entity
```
MaterialTemplateItem
├── Id (Guid)
├── MaterialTemplateId (Guid)
├── MaterialId (Guid)
├── Quantity (decimal)
├── UnitOfMeasure (string)
├── IsSerialised (bool, mirrors Material)
├── Notes (string?)
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

## Integration Points

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    MATERIAL MODULE INTEGRATION                           │
└─────────────────────────────────────────────────────────────────────────┘

1. ORDERS MODULE
   ┌─────────────────────────────────────┐
   │ Material population on order create   │
   │ Material templates applied            │
   │ Building defaults applied             │
   └─────────────────────────────────────┘

2. INVENTORY MODULE
   ┌─────────────────────────────────────┐
   │ Material master data shared          │
   │ Stock availability checks            │
   │ Material cost tracking                │
   └─────────────────────────────────────┘

3. BUILDINGS MODULE
   ┌─────────────────────────────────────┐
   │ Building default materials           │
   │ Building type for template matching   │
   └─────────────────────────────────────┘

4. PNL MODULE
   ┌─────────────────────────────────────┐
   │ Material costs from DefaultCost      │
   │ Cost allocation per material         │
   └─────────────────────────────────────┘

5. SETTINGS MODULE
   ┌─────────────────────────────────────┐
   │ Material master data management       │
   │ Template configuration                │
   └─────────────────────────────────────┘
```

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/02_modules/materials/OVERVIEW.md` - Materials Module Overview
- `docs/02_modules/materials/MATERIAL_TEMPLATES_MODULE.md` - Material Templates Specification
- `docs/02_modules/inventory/MATERIAL_POPULATION_RULES.md` - Material Population Rules
- `docs/02_modules/inventory/OVERVIEW.md` - Inventory Module Overview


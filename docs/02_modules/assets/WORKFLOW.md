# Assets – System Workflow Diagram

**Date:** December 12, 2025  
**Purpose:** End-to-end workflow representation for the Assets module

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    ASSET MANAGEMENT SYSTEM                               │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   ASSET TYPES          │      │   ASSET LIFECYCLE      │
        │  (Master Data)          │      │  (Status Tracking)     │
        ├───────────────────────┤      ├───────────────────────┤
        │ • Vehicle              │      │ • Acquired             │
        │ • Tool                 │      │ • In Use               │
        │ • Equipment            │      │ • Under Maintenance    │
        │ • Device               │      │ • Retired              │
        │ • Property             │      │ • Disposed             │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   ASSET TRACKING       │      │   ASSET OPERATIONS    │
        │  (Assignment, Location)│      │  (Maintenance, Depr)  │
        └───────────────────────┘      └───────────────────────┘
```

---

## Complete Flow: Asset Lifecycle

```
[STEP 1: ASSET ACQUISITION]
         |
         v
┌────────────────────────────────────────┐
│ CREATE ASSET                            │
│ POST /api/assets                        │
└────────────────────────────────────────┘
         |
         v
Asset {
  AssetTag: "VEH-001"
  AssetTypeId: Vehicle
  Name: "Toyota Hilux"
  SerialNumber: "ABC123456"
  PurchaseDate: 2025-01-15
  PurchasePrice: RM 120,000.00
  Supplier: "Toyota Dealer"
  WarrantyExpiryDate: 2028-01-15
  UsefulLifeMonths: 60
  CompanyId: Cephas
  DepartmentId: GPON
  CostCentreId: ISP_Operations
  Status: "Acquired"
  AssignedToUserId: null
  Location: "Warehouse"
}
         |
         v
[Asset Created]
         |
         v
[STEP 2: ASSET ASSIGNMENT]
         |
         v
┌────────────────────────────────────────┐
│ ASSIGN ASSET TO USER/DEPARTMENT         │
│ PUT /api/assets/{id}/assign             │
└────────────────────────────────────────┘
         |
         v
Asset {
  Status: "In Use"
  AssignedToUserId: "user-si-123"
  Location: "Field"
  AssignedAt: 2025-01-20
}
         |
         v
[Asset Assigned]
         |
         v
[STEP 3: ASSET USAGE TRACKING]
         |
         v
[Asset Used in Operations]
  - Vehicle: Used for field visits
  - Tool: Used in installations
  - Equipment: Used in jobs
         |
         v
[Track Usage]
  - Usage hours
  - Mileage (for vehicles)
  - Job assignments
         |
         v
[STEP 4: MAINTENANCE SCHEDULING]
         |
         v
┌────────────────────────────────────────┐
│ SCHEDULE MAINTENANCE                    │
│ POST /api/assets/{id}/maintenance        │
└────────────────────────────────────────┘
         |
         v
[Maintenance Due]
  - Scheduled maintenance (every 10,000 km)
  - Preventive maintenance (monthly)
  - Warranty service
         |
         v
┌────────────────────────────────────────┐
│ CREATE MAINTENANCE RECORD                │
└────────────────────────────────────────┘
         |
         v
AssetMaintenance {
  AssetId: "asset-123"
  MaintenanceType: "Scheduled"
  ScheduledDate: 2025-06-15
  PerformedDate: null
  Cost: null
  Description: "10,000 km service"
  Status: "Scheduled"
}
         |
         v
[STEP 5: MAINTENANCE PERFORMED]
         |
         v
┌────────────────────────────────────────┐
│ UPDATE MAINTENANCE RECORD                │
│ PUT /api/assets/{id}/maintenance/{id}    │
└────────────────────────────────────────┘
         |
         v
AssetMaintenance {
  Status: "Completed"
  PerformedDate: 2025-06-15
  Cost: RM 500.00
  Description: "10,000 km service completed"
  Notes: "Oil change, filter replacement"
  NextMaintenanceDate: 2025-12-15
}
         |
         v
[Update Asset Status]
  Asset.Status = "In Use" (if was "Under Maintenance")
         |
         v
[STEP 6: DEPRECIATION CALCULATION]
         |
         v
┌────────────────────────────────────────┐
│ CALCULATE DEPRECIATION                  │
│ (Monthly/Yearly)                       │
└────────────────────────────────────────┘
         |
         v
[Depreciation Method: Straight Line]
  PurchasePrice: RM 120,000.00
  UsefulLifeMonths: 60
  MonthlyDepreciation = PurchasePrice / UsefulLifeMonths
  MonthlyDepreciation = RM 120,000 / 60 = RM 2,000.00
         |
         v
[For each Month]
         |
         v
AssetDepreciation {
  AssetId: "asset-123"
  Period: "2025-01"
  DepreciationAmount: RM 2,000.00
  AccumulatedDepreciation: RM 2,000.00
  OpeningBookValue: RM 120,000.00
  ClosingBookValue: RM 118,000.00
  CalculatedAt: 2025-02-01
}
         |
         v
[Update Asset]
  Asset.AccumulatedDepreciation = RM 2,000.00
  Asset.CurrentBookValue = RM 118,000.00
         |
         v
[STEP 7: ASSET RETIREMENT/DISPOSAL]
         |
         v
┌────────────────────────────────────────┐
│ DISPOSE ASSET                           │
│ POST /api/assets/{id}/dispose           │
└────────────────────────────────────────┘
         |
         v
[Disposal Reasons]
  - End of useful life
  - Damage beyond repair
  - Obsolete
  - Sale
  - Scrap
         |
         v
┌────────────────────────────────────────┐
│ CREATE DISPOSAL RECORD                  │
└────────────────────────────────────────┘
         |
         v
AssetDisposal {
  AssetId: "asset-123"
  DisposalDate: 2029-01-15
  DisposalMethod: "Sale"
  DisposalAmount: RM 30,000.00
  Reason: "End of useful life"
  Buyer: "Used Car Dealer"
  Notes: "Sold after 4 years of service"
}
         |
         v
[Update Asset]
  Asset.Status = "Disposed"
  Asset.DisposedAt: 2029-01-15
         |
         v
[Calculate Gain/Loss]
  BookValue: RM 40,000.00 (after 60 months depreciation)
  DisposalAmount: RM 30,000.00
  Loss: RM 10,000.00
```

---

## Asset Acquisition Flow

```
[Admin: Assets → New Asset]
         |
         v
┌────────────────────────────────────────┐
│ ASSET ACQUISITION FORM                   │
└────────────────────────────────────────┘
         |
         v
Form Fields:
  - Asset Type: [Vehicle | Tool | Equipment | Device | Property]
  - Asset Tag: "VEH-001" (auto-generated or manual)
  - Name: "Toyota Hilux"
  - Serial Number: "ABC123456"
  - Purchase Date: 2025-01-15
  - Purchase Price: RM 120,000.00
  - Supplier: "Toyota Dealer"
  - Supplier Invoice ID: "INV-2025-001"
  - Warranty Expiry Date: 2028-01-15
  - Useful Life (Months): 60
  - Company: Cephas
  - Department: GPON
  - Cost Centre: ISP_Operations
  - Initial Location: "Warehouse"
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE ASSET DATA                     │
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
┌────────────────────────────────────────┐
│ CREATE ASSET                            │
│ POST /api/assets                        │
└────────────────────────────────────────┘
         |
         v
Asset Created {
  Id: "asset-123"
  AssetTag: "VEH-001"
  AssetTypeId: Vehicle
  Name: "Toyota Hilux"
  SerialNumber: "ABC123456"
  PurchaseDate: 2025-01-15
  PurchasePrice: RM 120,000.00
  Supplier: "Toyota Dealer"
  SupplierInvoiceId: "INV-2025-001"
  WarrantyExpiryDate: 2028-01-15
  UsefulLifeMonths: 60
  CompanyId: Cephas
  DepartmentId: GPON
  CostCentreId: ISP_Operations
  Status: "Acquired"
  Location: "Warehouse"
  AssignedToUserId: null
  CurrentBookValue: RM 120,000.00
  AccumulatedDepreciation: RM 0.00
}
         |
         v
[Asset Ready for Assignment]
```

---

## Asset Assignment Flow

```
[Asset: Status = "Acquired" or "In Use"]
         |
         v
┌────────────────────────────────────────┐
│ ASSIGN ASSET                            │
│ PUT /api/assets/{id}/assign             │
└────────────────────────────────────────┘
         |
         v
Assignment Options:
  - Assign to User (Service Installer)
  - Assign to Department
  - Assign to Location
  - Unassign (return to warehouse)
         |
         v
[If Assign to User]
         |
         v
Assignment Data {
  AssignedToUserId: "user-si-123"
  AssignedAt: 2025-01-20
  Location: "Field"
  Notes: "Assigned to SI for field operations"
}
         |
         v
[Update Asset]
  Asset.Status = "In Use"
  Asset.AssignedToUserId = "user-si-123"
  Asset.Location = "Field"
  Asset.AssignedAt = 2025-01-20
         |
         v
[If Unassign]
         |
         v
[Update Asset]
  Asset.Status = "Available"
  Asset.AssignedToUserId = null
  Asset.Location = "Warehouse"
  Asset.AssignedAt = null
         |
         v
[Asset Assignment Complete]
```

---

## Maintenance Flow

```
[Asset: Status = "In Use"]
         |
         v
┌────────────────────────────────────────┐
│ MAINTENANCE TRIGGER                      │
└────────────────────────────────────────┘
         |
    ┌────┴────┬──────────────┐
    |         |              |
    v         v              v
[Scheduled] [Preventive] [Breakdown]
   |            |              |
   |            |              |
   v            v              v
[Time-based] [Mileage] [On-demand]
   |            |              |
   |            |              |
   └────────────┴──────────────┘
         |
         v
┌────────────────────────────────────────┐
│ CREATE MAINTENANCE RECORD                │
│ POST /api/assets/{id}/maintenance        │
└────────────────────────────────────────┘
         |
         v
AssetMaintenance {
  AssetId: "asset-123"
  MaintenanceType: "Scheduled"
  ScheduledDate: 2025-06-15
  PerformedDate: null
  Cost: null
  Description: "10,000 km service"
  ServiceProvider: null
  Notes: null
  Status: "Scheduled"
  NextMaintenanceDate: 2025-12-15
  NextMaintenanceMileage: 20000
}
         |
         v
[Update Asset Status]
  Asset.Status = "Under Maintenance" (if asset unavailable)
  OR
  Asset.Status = "In Use" (if maintenance doesn't require asset downtime)
         |
         v
[Maintenance Due Date Arrives]
         |
         v
┌────────────────────────────────────────┐
│ PERFORM MAINTENANCE                      │
│ PUT /api/assets/{id}/maintenance/{id}    │
└────────────────────────────────────────┘
         |
         v
[Update Maintenance Record]
  AssetMaintenance {
    Status: "Completed"
    PerformedDate: 2025-06-15
    Cost: RM 500.00
    ServiceProvider: "Authorized Service Center"
    Notes: "Oil change, filter replacement, tire rotation"
    NextMaintenanceDate: 2025-12-15
    NextMaintenanceMileage: 20000
  }
         |
         v
[Update Asset]
  Asset.Status = "In Use"
  Asset.LastMaintenanceDate: 2025-06-15
  Asset.NextMaintenanceDate: 2025-12-15
         |
         v
[Maintenance Complete]
```

---

## Depreciation Flow

```
[Asset: PurchaseDate = 2025-01-15]
         |
         v
[Monthly Depreciation Job]
  Runs: First day of each month
         |
         v
┌────────────────────────────────────────┐
│ CALCULATE DEPRECIATION                  │
│ DepreciationService.CalculateMonthly() │
└────────────────────────────────────────┘
         |
         v
[For each Asset]
  WHERE:
    Status != "Disposed"
    PurchaseDate <= Current Month
         |
         v
[Get Asset Details]
  Asset {
    PurchasePrice: RM 120,000.00
    UsefulLifeMonths: 60
    AccumulatedDepreciation: RM 10,000.00
    CurrentBookValue: RM 110,000.00
  }
         |
         v
[Calculate Monthly Depreciation]
  DepreciationMethod: "StraightLine"
  MonthlyDepreciation = PurchasePrice / UsefulLifeMonths
  MonthlyDepreciation = RM 120,000 / 60 = RM 2,000.00
         |
         v
[Check if Fully Depreciated]
  RemainingMonths = UsefulLifeMonths - (Months Since Purchase)
  IF RemainingMonths <= 0:
    MonthlyDepreciation = Remaining Book Value
         |
         v
[Create Depreciation Entry]
         |
         v
AssetDepreciation {
  AssetId: "asset-123"
  Period: "2025-02"
  DepreciationAmount: RM 2,000.00
  AccumulatedDepreciation: RM 12,000.00
  OpeningBookValue: RM 110,000.00
  ClosingBookValue: RM 108,000.00
  DepreciationMethod: "StraightLine"
  CalculatedAt: 2025-02-01
}
         |
         v
[Update Asset]
  Asset.AccumulatedDepreciation = RM 12,000.00
  Asset.CurrentBookValue = RM 108,000.00
  Asset.LastDepreciationDate = 2025-02-01
         |
         v
[Save to Database]
         |
         v
[Repeat for all Assets]
```

---

## Asset Disposal Flow

```
[Asset: Status = "In Use" or "Retired"]
         |
         v
┌────────────────────────────────────────┐
│ DISPOSE ASSET                           │
│ POST /api/assets/{id}/dispose           │
└────────────────────────────────────────┘
         |
         v
Disposal Form:
  - Disposal Date: 2029-01-15
  - Disposal Method: [Sale | Scrap | Donation | Write-off]
  - Disposal Amount: RM 30,000.00
  - Reason: "End of useful life"
  - Buyer/Recipient: "Used Car Dealer"
  - Notes: "Sold after 4 years of service"
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE DISPOSAL                        │
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
[Get Current Book Value]
  Asset.CurrentBookValue: RM 40,000.00
  Asset.AccumulatedDepreciation: RM 80,000.00
         |
         v
[Calculate Gain/Loss]
  BookValue: RM 40,000.00
  DisposalAmount: RM 30,000.00
  Gain/Loss = DisposalAmount - BookValue
  Gain/Loss = RM 30,000 - RM 40,000 = RM -10,000 (Loss)
         |
         v
┌────────────────────────────────────────┐
│ CREATE DISPOSAL RECORD                  │
└────────────────────────────────────────┘
         |
         v
AssetDisposal {
  AssetId: "asset-123"
  DisposalDate: 2029-01-15
  DisposalMethod: "Sale"
  DisposalAmount: RM 30,000.00
  BookValueAtDisposal: RM 40,000.00
  GainLoss: RM -10,000.00
  Reason: "End of useful life"
  Buyer: "Used Car Dealer"
  Notes: "Sold after 4 years of service"
  CreatedByUserId: "admin-user"
}
         |
         v
[Update Asset]
  Asset.Status = "Disposed"
  Asset.DisposedAt = 2029-01-15
  Asset.DisposalId = "disposal-123"
         |
         v
[If Assigned]
  Asset.AssignedToUserId = null
  Asset.Location = "Disposed"
         |
         v
[Asset Disposed]
```

---

## Asset Status Transitions

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    ASSET STATUS FLOW                                    │
└─────────────────────────────────────────────────────────────────────────┘

[Acquired]
  │
  ├─→ [In Use] (when assigned)
  │     │
  │     ├─→ [Under Maintenance] (when maintenance scheduled)
  │     │     │
  │     │     └─→ [In Use] (when maintenance completed)
  │     │
  │     ├─→ [Retired] (when end of useful life)
  │     │     │
  │     │     └─→ [Disposed] (when disposed)
  │     │
  │     └─→ [Disposed] (direct disposal)
  │
  └─→ [Disposed] (immediate disposal)

STATUS DEFINITIONS:
───────────────────
Acquired: Asset purchased, not yet assigned
In Use: Asset assigned and actively used
Under Maintenance: Asset in maintenance, temporarily unavailable
Retired: Asset reached end of useful life, no longer in use
Disposed: Asset sold, scrapped, or written off
```

---

## Key Components

### Asset Entity
```
Asset
├── Id (Guid)
├── AssetTag (string, unique per company)
├── AssetTypeId (Guid)
├── Name (string)
├── SerialNumber (string?)
├── PurchaseDate (DateTime)
├── PurchasePrice (decimal)
├── Supplier (string?)
├── SupplierInvoiceId (Guid?)
├── WarrantyExpiryDate (DateTime?)
├── UsefulLifeMonths (int)
├── CompanyId (Guid)
├── DepartmentId (Guid?)
├── CostCentreId (Guid?)
├── Status (enum: Acquired, InUse, UnderMaintenance, Retired, Disposed)
├── AssignedToUserId (Guid?)
├── Location (string)
├── AssignedAt (DateTime?)
├── CurrentBookValue (decimal)
├── AccumulatedDepreciation (decimal)
├── LastMaintenanceDate (DateTime?)
├── NextMaintenanceDate (DateTime?)
├── DisposedAt (DateTime?)
├── DisposalId (Guid?)
└── CreatedAt, UpdatedAt
```

### AssetMaintenance Entity
```
AssetMaintenance
├── Id (Guid)
├── AssetId (Guid)
├── MaintenanceType (enum: Scheduled, Preventive, Breakdown, Warranty)
├── ScheduledDate (DateTime)
├── PerformedDate (DateTime?)
├── Cost (decimal?)
├── Description (string)
├── ServiceProvider (string?)
├── Notes (string?)
├── Status (enum: Scheduled, InProgress, Completed, Cancelled)
├── NextMaintenanceDate (DateTime?)
├── NextMaintenanceMileage (int?)
└── CreatedAt, UpdatedAt
```

### AssetDepreciation Entity
```
AssetDepreciation
├── Id (Guid)
├── AssetId (Guid)
├── Period (string: "YYYY-MM")
├── DepreciationAmount (decimal)
├── AccumulatedDepreciation (decimal)
├── OpeningBookValue (decimal)
├── ClosingBookValue (decimal)
├── DepreciationMethod (enum: StraightLine, DecliningBalance)
├── CalculatedAt (DateTime)
└── CreatedAt
```

### AssetDisposal Entity
```
AssetDisposal
├── Id (Guid)
├── AssetId (Guid)
├── DisposalDate (DateTime)
├── DisposalMethod (enum: Sale, Scrap, Donation, WriteOff)
├── DisposalAmount (decimal)
├── BookValueAtDisposal (decimal)
├── GainLoss (decimal)
├── Reason (string)
├── Buyer (string?)
├── Notes (string?)
├── CreatedByUserId (Guid)
└── CreatedAt
```

---

## Integration Points

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    ASSET MODULE INTEGRATION                              │
└─────────────────────────────────────────────────────────────────────────┘

1. PROCUREMENT MODULE
   ┌─────────────────────────────────────┐
   │ Asset purchase from SupplierInvoice │
   │ Link asset to procurement record    │
   └─────────────────────────────────────┘

2. INVENTORY MODULE
   ┌─────────────────────────────────────┐
   │ Track asset usage in jobs            │
   │ Asset assignment to orders           │
   └─────────────────────────────────────┘

3. FINANCE MODULE
   ┌─────────────────────────────────────┐
   │ Depreciation affects PNL             │
   │ Disposal gain/loss reporting          │
   └─────────────────────────────────────┘

4. USERS MODULE
   ┌─────────────────────────────────────┐
   │ Asset assignment to users            │
   │ Track user-assigned assets            │
   └─────────────────────────────────────┘

5. DEPARTMENTS MODULE
   ┌─────────────────────────────────────┐
   │ Asset assignment to departments      │
   │ Department asset inventory           │
   └─────────────────────────────────────┘
```

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/02_modules/pnl/PNL_CALCULATION_FLOW.md` - PNL & Depreciation
- `docs/02_modules/inventory/OVERVIEW.md` - Inventory & Asset Usage


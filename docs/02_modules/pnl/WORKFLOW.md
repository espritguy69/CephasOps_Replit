# PNL – System Workflow Diagram

**Date:** December 12, 2025  
**Purpose:** End-to-end workflow representation for the PNL module

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    PNL CALCULATION SYSTEM                                │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   REVENUE SOURCE       │      │   COST SOURCES        │
        │  (Billing/Invoices)    │      │  (Materials, Labour)  │
        ├───────────────────────┤      ├───────────────────────┤
        │ • Invoice Total        │      │ • Material Cost        │
        │ • Partner billing      │      │ • SI Labour Cost       │
        │ • Period aggregation   │      │ • Overhead allocation  │
        │ • Company/Partner      │      │ • Period aggregation   │
        │   breakdown            │      │ • Company/Partner      │
        └───────────────────────┘      │   breakdown            │
                    │                  └───────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │      PNL CALCULATION          │
                    │  (Revenue - Costs = Profit)   │
                    └───────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   PNL FACT TABLE       │      │   PNL DETAILS         │
        │  (Aggregated Summary)  │      │  (Per Order Details)  │
        └───────────────────────┘      └───────────────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │      PNL REPORTS               │
                    │  (Dashboard, Analytics)       │
                    └───────────────────────────────┘
```

---

## Complete Flow: Data Sources to PNL Report

```
[PNL Calculation Trigger]
  Trigger: Periodic (daily/monthly) OR On-demand
  Period: 2025-11 (November)
  CompanyId: Cephas
         |
         v
┌────────────────────────────────────────┐
│ STEP 1: FETCH REVENUE DATA              │
│ (From Billing Module)                   │
└────────────────────────────────────────┘
         |
         v
[Query Invoices]
  WHERE:
    CompanyId = Cephas
    InvoiceDate BETWEEN PeriodStart AND PeriodEnd
    Status = "Invoiced" OR "Completed"
         |
         v
[For each Invoice]
         |
         v
Invoice {
  Id: "invoice-123"
  CompanyId: Cephas
  PartnerId: TIME
  InvoiceDate: 2025-11-15
  TotalAmount: RM 159.00
  SubTotal: RM 150.00
  TaxAmount: RM 9.00
  LineItems: [
    {
      OrderId: "order-456"
      Description: "GPON Activation FTTH"
      Quantity: 1
      UnitPrice: RM 150.00
      Total: RM 150.00
    }
  ]
}
         |
         v
[Extract Revenue]
  Revenue = Invoice.TotalAmount (or SubTotal, configurable)
  Period = Invoice.InvoiceDate (monthly)
  PartnerId = Invoice.PartnerId
  OrderId = Invoice.LineItems[0].OrderId
         |
         v
┌────────────────────────────────────────┐
│ STEP 2: FETCH MATERIAL COST             │
│ (From Inventory Module)                 │
└────────────────────────────────────────┘
         |
         v
[Query Material Usage per Order]
  WHERE:
    OrderId IN (completed orders in period)
         |
         v
[For each Order]
         |
         v
Material Usage {
  OrderId: "order-456"
  Materials: [
    {
      MaterialId: "material-onu"
      MaterialCode: "ONU"
      Quantity: 1
      UnitCost: RM 50.00
      TotalCost: RM 50.00
    },
    {
      MaterialId: "material-patchcord"
      MaterialCode: "PATCHCORD_6M"
      Quantity: 2
      UnitCost: RM 5.00
      TotalCost: RM 10.00
    }
  ]
}
         |
         v
[Calculate Material Cost]
  MaterialCost = Sum(Material.TotalCost)
  Example: RM 50.00 + RM 10.00 = RM 60.00
         |
         v
┌────────────────────────────────────────┐
│ STEP 3: FETCH LABOUR COST                │
│ (From Payroll Module)                   │
└────────────────────────────────────────┘
         |
         v
[Query JobEarningRecords]
  WHERE:
    OrderId IN (completed orders in period)
    Period = "2025-11"
         |
         v
[For each Order]
         |
         v
JobEarningRecord {
  OrderId: "order-456"
  ServiceInstallerId: "SI-123"
  BaseRate: RM 100.00
  KpiAdjustment: +RM 10.00
  FinalPay: RM 110.00
  Period: "2025-11"
}
         |
         v
[Calculate Labour Cost]
  LabourCost = Sum(JobEarningRecord.FinalPay)
  Example: RM 110.00
         |
         v
┌────────────────────────────────────────┐
│ STEP 4: FETCH OVERHEAD COSTS            │
│ (From OverheadEntry)                    │
└────────────────────────────────────────┘
         |
         v
[Query OverheadEntries]
  WHERE:
    CompanyId = Cephas
    Period = "2025-11"
         |
         v
[For each OverheadEntry]
         |
         v
OverheadEntry {
  CompanyId: Cephas
  CostCentreId: "ISP_Operations"
  Period: "2025-11"
  Amount: RM 5,000.00
  Description: "Rent, Utilities"
  AllocationBasis: "Revenue" (or "Jobs", "Fixed")
}
         |
         v
[Calculate Overhead Allocation]
  Strategy: By Revenue Proportion
  TotalRevenue = Sum(Invoice.TotalAmount)
  OverheadAllocated = (OrderRevenue / TotalRevenue) × TotalOverhead
         |
         v
Example:
  TotalRevenue: RM 100,000.00
  TotalOverhead: RM 5,000.00
  OrderRevenue: RM 150.00
  OverheadAllocated = (150 / 100000) × 5000 = RM 7.50
         |
         v
┌────────────────────────────────────────┐
│ STEP 5: CALCULATE PNL PER ORDER          │
└────────────────────────────────────────┘
         |
         v
[For each Completed Order in Period]
         |
         v
PNL Calculation:
  OrderId: "order-456"
  Revenue: RM 150.00 (from Invoice)
  MaterialCost: RM 60.00
  LabourCost: RM 110.00
  OverheadAllocated: RM 7.50
         |
         v
  DirectCosts = MaterialCost + LabourCost
  DirectCosts = RM 60.00 + RM 110.00 = RM 170.00
         |
         v
  GrossProfit = Revenue - DirectCosts
  GrossProfit = RM 150.00 - RM 170.00 = RM -20.00
         |
         v
  NetProfit = GrossProfit - OverheadAllocated
  NetProfit = RM -20.00 - RM 7.50 = RM -27.50
         |
         v
  GrossMargin% = (GrossProfit / Revenue) × 100
  GrossMargin% = (-20 / 150) × 100 = -13.33%
         |
         v
  NetMargin% = (NetProfit / Revenue) × 100
  NetMargin% = (-27.50 / 150) × 100 = -18.33%
         |
         v
┌────────────────────────────────────────┐
│ STEP 6: CREATE PNL DETAIL PER ORDER      │
└────────────────────────────────────────┘
         |
         v
PnlDetailPerOrder {
  Id: "pnl-detail-789"
  OrderId: "order-456"
  CompanyId: Cephas
  PartnerId: TIME
  Period: "2025-11"
  OrderType: "ACTIVATION"
  RevenueAmount: RM 150.00
  MaterialCost: RM 60.00
  LabourCost: RM 110.00
  OverheadAllocated: RM 7.50
  GrossProfit: RM -20.00
  NetProfit: RM -27.50
  GrossMargin%: -13.33%
  NetMargin%: -18.33%
  KpiResult: "OnTime"
  RescheduledCount: 0
}
         |
         v
[Save to Database]
         |
         v
┌────────────────────────────────────────┐
│ STEP 7: AGGREGATE TO PNL FACT            │
└────────────────────────────────────────┘
         |
         v
[Aggregate by Dimensions]
  Dimensions:
    - Company
    - Partner (optional)
    - Vertical (optional)
    - Cost Centre (optional)
    - Period
    - Order Type (optional)
         |
         v
[For each Dimension Combination]
         |
         v
Aggregation Example:
  Company: Cephas
  Partner: TIME
  Period: "2025-11"
  OrderType: "ACTIVATION"
         |
         v
  Sum all PnlDetailPerOrder matching:
    CompanyId = Cephas
    PartnerId = TIME
    Period = "2025-11"
    OrderType = "ACTIVATION"
         |
         v
Aggregated Values:
  TotalRevenue = Sum(RevenueAmount) = RM 50,000.00
  TotalMaterialCost = Sum(MaterialCost) = RM 20,000.00
  TotalLabourCost = Sum(LabourCost) = RM 25,000.00
  TotalOverheadAllocated = Sum(OverheadAllocated) = RM 2,500.00
  TotalGrossProfit = RM 50,000 - RM 20,000 - RM 25,000 = RM 5,000.00
  TotalNetProfit = RM 5,000 - RM 2,500 = RM 2,500.00
  JobsCount = Count(Orders) = 500
         |
         v
┌────────────────────────────────────────┐
│ STEP 8: CREATE PNL FACT                  │
└────────────────────────────────────────┘
         |
         v
PnlFact {
  Id: "pnl-fact-123"
  CompanyId: Cephas
  PartnerId: TIME
  VerticalId: ISP
  CostCentreId: ISP_Operations
  Period: "2025-11"
  OrderType: "ACTIVATION"
  RevenueAmount: RM 50,000.00
  DirectMaterialCost: RM 20,000.00
  DirectLabourCost: RM 25,000.00
  IndirectCost: RM 2,500.00
  GrossProfit: RM 5,000.00
  NetProfit: RM 2,500.00
  GrossMargin%: 10.00%
  NetMargin%: 5.00%
  JobsCount: 500
  OrdersCompletedCount: 500
  ReschedulesCount: 50
  AssuranceJobsCount: 20
  LastRecalculatedAt: 2025-12-01 00:00:00
}
         |
         v
[Save to Database]
         |
         v
┌────────────────────────────────────────┐
│ STEP 9: GENERATE PNL REPORTS             │
└────────────────────────────────────────┘
         |
         v
[Available Reports]
         |
    ┌────┴────┬──────────────┐
    |         |              |
    v         v              v
[Summary] [Partner] [Order Detail]
   |         |              |
   |         |              |
   v         v              v
[Company   [Per Partner  [Drill-down
 Overview] Profitability] to Orders]
```

---

## Revenue Calculation Flow

```
[Invoice Created]
  Status: "Invoiced"
  InvoiceDate: 2025-11-15
         |
         v
┌────────────────────────────────────────┐
│ DETERMINE REVENUE AMOUNT                 │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[Use Total] [Use SubTotal]
   |            |
   |            |
   └────────────┘
         |
         v
Revenue Amount = Invoice.TotalAmount
  OR
Revenue Amount = Invoice.SubTotal
  (Configurable per company)
         |
         v
[Map to Period]
  Period = Format(InvoiceDate, "YYYY-MM")
  Example: 2025-11-15 → "2025-11"
         |
         v
[Link to Order]
  OrderId = Invoice.LineItems[0].OrderId
         |
         v
[Extract Dimensions]
  CompanyId = Invoice.CompanyId
  PartnerId = Invoice.PartnerId
  OrderType = Order.OrderTypeId
  CostCentreId = Order.CostCentreId (if set)
         |
         v
[Revenue Data Ready]
  Revenue: RM 150.00
  Period: "2025-11"
  OrderId: "order-456"
  CompanyId: Cephas
  PartnerId: TIME
```

---

## Cost Calculation Flow

```
[Order Completed]
  OrderId: "order-456"
  Status: "OrderCompleted"
  CompletedAt: 2025-11-15
         |
         v
┌────────────────────────────────────────┐
│ MATERIAL COST CALCULATION                 │
└────────────────────────────────────────┘
         |
         v
[Query Material Usage]
  OrderMaterialUsage.find(OrderId = "order-456")
         |
         v
Material Usage Records:
  [
    { MaterialCode: "ONU", Quantity: 1, UnitCost: RM 50.00, TotalCost: RM 50.00 },
    { MaterialCode: "PATCHCORD_6M", Quantity: 2, UnitCost: RM 5.00, TotalCost: RM 10.00 }
  ]
         |
         v
MaterialCost = Sum(TotalCost)
MaterialCost = RM 50.00 + RM 10.00 = RM 60.00
         |
         v
┌────────────────────────────────────────┐
│ LABOUR COST CALCULATION                 │
└────────────────────────────────────────┘
         |
         v
[Query JobEarningRecord]
  JobEarningRecord.find(OrderId = "order-456")
         |
         v
JobEarningRecord {
  OrderId: "order-456"
  ServiceInstallerId: "SI-123"
  BaseRate: RM 100.00
  KpiAdjustment: +RM 10.00
  FinalPay: RM 110.00
}
         |
         v
LabourCost = FinalPay
LabourCost = RM 110.00
         |
         v
[If Multiple SIs]
  LabourCost = Sum(All JobEarningRecord.FinalPay)
         |
         v
┌────────────────────────────────────────┐
│ OVERHEAD ALLOCATION                      │
└────────────────────────────────────────┘
         |
         v
[Query OverheadEntries]
  OverheadEntry.find(
    CompanyId = Cephas
    Period = "2025-11"
  )
         |
         v
OverheadEntries:
  [
    { CostCentreId: "ISP_Operations", Amount: RM 5,000.00 },
    { CostCentreId: "Warehouse", Amount: RM 2,000.00 }
  ]
         |
         v
[Allocation Strategy: Revenue Proportion]
  TotalRevenue = Sum(All Invoice.TotalAmount in period)
  OrderRevenue = Invoice.TotalAmount for this order
  OverheadAllocated = (OrderRevenue / TotalRevenue) × TotalOverhead
         |
         v
Example:
  TotalRevenue: RM 100,000.00
  TotalOverhead: RM 5,000.00
  OrderRevenue: RM 150.00
  OverheadAllocated = (150 / 100000) × 5000 = RM 7.50
         |
         v
[Cost Data Ready]
  MaterialCost: RM 60.00
  LabourCost: RM 110.00
  OverheadAllocated: RM 7.50
  TotalCost: RM 177.50
```

---

## PNL Aggregation Flow

```
[PNL Calculation Triggered]
  Period: "2025-11"
  CompanyId: Cephas
         |
         v
┌────────────────────────────────────────┐
│ FETCH ALL PNL DETAILS                    │
│ PnlDetailPerOrder.find(                 │
│   CompanyId = Cephas                    │
│   Period = "2025-11"                    │
│ )                                       │
└────────────────────────────────────────┘
         |
         v
[Group by Dimensions]
         |
    ┌────┴────┬──────────────┐
    |         |              |
    v         v              v
[By Partner] [By OrderType] [By CostCentre]
   |              |              |
   |              |              |
   └──────────────┴──────────────┘
         |
         v
[For each Dimension Combination]
         |
         v
Example Group:
  CompanyId: Cephas
  PartnerId: TIME
  OrderType: ACTIVATION
  Period: "2025-11"
         |
         v
[Filter PnlDetailPerOrder]
  WHERE:
    CompanyId = Cephas
    PartnerId = TIME
    OrderType = ACTIVATION
    Period = "2025-11"
         |
         v
[Aggregate Values]
  RevenueAmount = Sum(RevenueAmount)
  MaterialCost = Sum(MaterialCost)
  LabourCost = Sum(LabourCost)
  OverheadAllocated = Sum(OverheadAllocated)
  JobsCount = Count(OrderId)
  ReschedulesCount = Sum(RescheduledCount)
         |
         v
[Calculate Derived Values]
  DirectCosts = MaterialCost + LabourCost
  GrossProfit = RevenueAmount - DirectCosts
  NetProfit = GrossProfit - OverheadAllocated
  GrossMargin% = (GrossProfit / RevenueAmount) × 100
  NetMargin% = (NetProfit / RevenueAmount) × 100
         |
         v
[Create PnlFact]
  PnlFact {
    CompanyId: Cephas
    PartnerId: TIME
    OrderType: ACTIVATION
    Period: "2025-11"
    RevenueAmount: RM 50,000.00
    DirectMaterialCost: RM 20,000.00
    DirectLabourCost: RM 25,000.00
    IndirectCost: RM 2,500.00
    GrossProfit: RM 5,000.00
    NetProfit: RM 2,500.00
    GrossMargin%: 10.00%
    NetMargin%: 5.00%
    JobsCount: 500
  }
         |
         v
[Save to Database]
         |
         v
[Repeat for all Dimension Combinations]
```

---

## PNL Report Generation Flow

```
[User Requests PNL Report]
  Filters:
    - CompanyId: Cephas
    - Period: "2025-11"
    - PartnerId: TIME (optional)
    - OrderType: ACTIVATION (optional)
         |
         v
┌────────────────────────────────────────┐
│ QUERY PNL FACTS                         │
│ PnlFact.find(                           │
│   CompanyId = Cephas                    │
│   Period = "2025-11"                    │
│   PartnerId = TIME (if filter)          │
│   OrderType = ACTIVATION (if filter)    │
│ )                                       │
└────────────────────────────────────────┘
         |
         v
[Retrieve PnlFact Records]
         |
         v
┌────────────────────────────────────────┐
│ FORMAT REPORT DATA                       │
└────────────────────────────────────────┘
         |
    ┌────┴────┬──────────────┐
    |         |              |
    v         v              v
[Summary] [Partner] [Order Detail]
   |         |              |
   |         |              |
   v         v              v
[Company   [Per Partner  [Drill-down
 Overview] Profitability] to Orders]
         |
         v
[Generate Report]
  Format: JSON (API) OR HTML/PDF (UI)
         |
         v
[Return to User]
```

---

## Key Components

### PnlFact Entity
```
PnlFact
├── Id (Guid)
├── CompanyId (Guid)
├── PartnerId (Guid?)
├── VerticalId (Guid?)
├── CostCentreId (Guid?)
├── Period (string: "YYYY-MM")
├── OrderType (string?)
├── RevenueAmount (decimal)
├── DirectMaterialCost (decimal)
├── DirectLabourCost (decimal)
├── IndirectCost (decimal)
├── GrossProfit (decimal)
├── NetProfit (decimal)
├── GrossMargin% (decimal)
├── NetMargin% (decimal)
├── JobsCount (int)
├── OrdersCompletedCount (int)
├── ReschedulesCount (int)
├── AssuranceJobsCount (int)
├── LastRecalculatedAt (DateTime)
└── CreatedAt, UpdatedAt
```

### PnlDetailPerOrder Entity
```
PnlDetailPerOrder
├── Id (Guid)
├── OrderId (Guid)
├── CompanyId (Guid)
├── PartnerId (Guid)
├── Period (string: "YYYY-MM")
├── OrderType (string)
├── RevenueAmount (decimal)
├── MaterialCost (decimal)
├── LabourCost (decimal)
├── OverheadAllocated (decimal)
├── GrossProfit (decimal)
├── NetProfit (decimal)
├── GrossMargin% (decimal)
├── NetMargin% (decimal)
├── KpiResult (string)
├── RescheduledCount (int)
└── CreatedAt, UpdatedAt
```

### OverheadEntry Entity
```
OverheadEntry
├── Id (Guid)
├── CompanyId (Guid)
├── CostCentreId (Guid?)
├── VerticalId (Guid?)
├── Period (string: "YYYY-MM")
├── Amount (decimal)
├── Description (string)
├── Source (enum: Manual, Imported, System)
├── AllocationBasis (string?)
└── CreatedAt, UpdatedAt
```

---

## Integration Points

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    PNL MODULE INTEGRATION                                │
└─────────────────────────────────────────────────────────────────────────┘

1. BILLING MODULE
   ┌─────────────────────────────────────┐
   │ Revenue from Invoices                │
   │ Invoice.TotalAmount or SubTotal      │
   └─────────────────────────────────────┘

2. INVENTORY MODULE
   ┌─────────────────────────────────────┐
   │ Material costs per order              │
   │ Material usage and unit costs         │
   └─────────────────────────────────────┘

3. PAYROLL MODULE
   ┌─────────────────────────────────────┐
   │ Labour costs from JobEarningRecords   │
   │ SI payments per order                │
   └─────────────────────────────────────┘

4. ORDERS MODULE
   ┌─────────────────────────────────────┐
   │ Order metadata (OrderType, Partner)   │
   │ Order completion status              │
   └─────────────────────────────────────┘

5. SETTINGS MODULE
   ┌─────────────────────────────────────┐
   │ Cost Centre configuration            │
   │ Company/Vertical/Partner data        │
   └─────────────────────────────────────┘
```

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/02_modules/pnl/OVERVIEW.md` - PNL Module Overview
- `docs/02_modules/billing/INVOICE_RATE_CALCULATION_FLOW.md` - Invoice & Revenue
- `docs/02_modules/payroll/OVERVIEW.md` - Payroll & Labour Costs


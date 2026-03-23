# Billing – System Workflow Diagram

**Date:** December 12, 2025  
**Purpose:** End-to-end workflow representation for the Billing module

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    INVOICE & RATE CALCULATION SYSTEM                     │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   PARTNER RATE CARDS   │      │   SI RATE CARDS       │
        │  (Revenue - What we    │      │  (Payout - What we     │
        │   charge partners)     │      │   pay installers)      │
        ├───────────────────────┤      ├───────────────────────┤
        │ • BillingRatecard     │      │ • GponSiJobRate        │
        │ • PartnerGroupId      │      │ • InstallerType        │
        │ • PartnerId (opt)      │      │ • SiLevel              │
        │ • OrderTypeId          │      │ • OrderTypeId          │
        │ • InstallationMethodId│      │ • InstallationTypeId   │
        │ • Amount (RM)          │      │ • InstallationMethodId│
        │                       │      │ • DefaultRateAmount    │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │      RATE RESOLUTION          │
                    │  (Priority-based lookup)      │
                    └───────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   INVOICE GENERATION   │      │   PAYROLL CALCULATION │
        │  (Partner billing)    │      │  (SI earnings)        │
        └───────────────────────┘      └───────────────────────┘
```

---

## Complete Flow: Order → Invoice → Payment

```
[Order Created]
    OrderType: ACTIVATION
    InstallationType: FTTH
    InstallationMethod: PRELAID
    PartnerId: TIME
    ServiceInstallerId: SI-123
         |
         v
[Order Status: OrderCompleted]
         |
         v
[Order Status: DocketsUploaded]
         |
         v
[Order Status: ReadyForInvoice]
         |
         v
┌────────────────────────────────────────┐
│ STEP 1: RESOLVE PARTNER RATE            │
│ (What we charge the partner)            │
└────────────────────────────────────────┘
         |
         v
[Rate Resolution Process]
    (see Partner Rate Resolution below)
         |
         v
[Partner Rate: RM 150.00]
         |
         v
┌────────────────────────────────────────┐
│ STEP 2: GENERATE INVOICE                │
│ - Invoice Number: INV-CEPHAS-2025-00123 │
│ - Invoice Date: 2025-12-12              │
│ - Due Date: 2025-12-12 + 45 days        │
│ - Line Item:                            │
│   Description: "GPON Activation FTTH"   │
│   Quantity: 1                           │
│   Unit Price: RM 150.00                  │
│   Subtotal: RM 150.00                    │
│   Tax (SST 6%): RM 9.00                  │
│   Total: RM 159.00                       │
└────────────────────────────────────────┘
         |
         v
[Invoice Status: Invoiced]
         |
         v
[Invoice uploaded to TIME portal]
         |
         v
[MyInvois e-Invoice submitted]
         |
         v
[Payment Received]
         |
         v
[Invoice Status: Completed]
         |
         v
┌────────────────────────────────────────┐
│ STEP 3: RESOLVE SI RATE                 │
│ (What we pay the service installer)     │
└────────────────────────────────────────┘
         |
         v
[SI Rate Resolution Process]
    (see SI Rate Resolution below)
         |
         v
[SI Rate: RM 80.00]
         |
         v
┌────────────────────────────────────────┐
│ STEP 4: CALCULATE PAYROLL               │
│ - JobEarningRecord created              │
│ - BaseRate: RM 80.00                    │
│ - KPI Adjustment: +RM 10.00 (OnTime)    │
│ - FinalPay: RM 90.00                    │
└────────────────────────────────────────┘
         |
         v
[Payroll Run Generated]
         |
         v
[P&L Updated]
    Revenue: RM 150.00
    Cost: RM 90.00
    Margin: RM 60.00
```

---

## Partner Rate Card Resolution Flow

```
[Order Ready for Invoice]
    CompanyId: Cephas
    DepartmentId: GPON
    PartnerId: TIME
    PartnerGroupId: TIME
    OrderTypeId: ACTIVATION
    InstallationTypeId: FTTH
    InstallationMethodId: PRELAID
         |
         v
┌────────────────────────────────────────┐
│ PARTNER RATE RESOLUTION (Priority)     │
└────────────────────────────────────────┘
         |
         v
[Step 1: Most Specific Match]
    BillingRatecard.find(
        CompanyId = Cephas
        DepartmentId = GPON
        PartnerId = TIME (specific partner)
        OrderTypeId = ACTIVATION
        InstallationMethodId = PRELAID
    )
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND]  [NOT FOUND]
   |         |
   |         v
   |    [Step 2: Partner Group Match]
   |        BillingRatecard.find(
   |            CompanyId = Cephas
   |            DepartmentId = GPON
   |            PartnerGroupId = TIME (group level)
   |            PartnerId = null
   |            OrderTypeId = ACTIVATION
   |            InstallationMethodId = PRELAID
   |        )
   |         |
   |    ┌────┴────┐
   |    |         |
   |    v         v
   | [FOUND]  [NOT FOUND]
   |    |         |
   |    |         v
   |    |    [Step 3: Department Default]
   |    |        BillingRatecard.find(
   |    |            CompanyId = Cephas
   |    |            DepartmentId = GPON
   |    |            PartnerGroupId = null
   |    |            PartnerId = null
   |    |            OrderTypeId = ACTIVATION
   |    |            InstallationMethodId = PRELAID
   |    |        )
   |    |         |
   |    |    ┌────┴────┐
   |    |    |         |
   |    |    v         v
   |    | [FOUND]  [NO RATE]
   |    |    |         |
   |    |    |         v
   |    |    |    [BLOCK INVOICE]
   |    |    |    [Error: No rate card found]
   |    |    |         |
   |    └────┴─────────┘
   |         |
   └─────────┘
         |
         v
[Partner Rate Selected]
    Amount: RM 150.00
         |
         v
[Use for Invoice Line Item]
```

---

## Service Installer Rate Resolution Flow

```
[Order Completed]
    ServiceInstallerId: SI-123
    InstallerType: SUBCON
    SiLevel: SENIOR
    OrderTypeId: ACTIVATION
    InstallationTypeId: FTTH
    InstallationMethodId: PRELAID
         |
         v
┌────────────────────────────────────────┐
│ SI RATE RESOLUTION (Priority)          │
└────────────────────────────────────────┘
         |
         v
[Step 1: Custom Rate Override]
    GponSiCustomRate.find(
        InstallerId = SI-123
        DepartmentId = GPON
        OrderTypeId = ACTIVATION
        InstallationTypeId = FTTH
        InstallationMethodId = PRELAID
    )
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND]  [NOT FOUND]
   |         |
   |         v
   |    [Step 2: Default SI Rate]
   |        GponSiJobRate.find(
   |            InstallerType = SUBCON
   |            SiLevel = SENIOR
   |            DepartmentId = GPON
   |            OrderTypeId = ACTIVATION
   |            InstallationTypeId = FTTH
   |            InstallationMethodId = PRELAID
   |        )
   |         |
   |    ┌────┴────┐
   |    |         |
   |    v         v
   | [FOUND]  [NOT FOUND]
   |    |         |
   |    |         v
   |    |    [Step 3: Fallback to Employee Rate]
   |    |        GponSiJobRate.find(
   |    |            InstallerType = EMPLOYEE
   |    |            SiLevel = SENIOR
   |    |            ... (same other criteria)
   |    |        )
   |    |         |
   |    |    ┌────┴────┐
   |    |    |         |
   |    |    v         v
   |    | [FOUND]  [NO RATE]
   |    |    |         |
   |    |    |         v
   |    |    |    [Use GlobalSetting Default]
   |    |    |    [Or: BLOCK PAYROLL]
   |    |    |         |
   |    └────┴─────────┘
   |         |
   └─────────┘
         |
         v
[SI Rate Selected]
    CustomRate: RM 200.00 (if custom exists)
    OR
    DefaultRate: RM 100.00 (if no custom)
         |
         v
[Apply KPI Adjustments]
         |
    ┌────┴────┐
    |         |
    v         v
[OnTime] [Late/Exceeded]
   |         |
   |         v
   |    [Apply Penalty]
   |    FinalPay = BaseRate - Penalty
   |
   v
[Apply Bonus]
    FinalPay = BaseRate + Bonus
         |
         v
[Store in JobEarningRecord]
```

---

## Invoice Generation Flow

```
[Order Status: ReadyForInvoice]
         |
         v
┌────────────────────────────────────────┐
│ PREREQUISITE CHECKS                     │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[ALL PASS] [ANY FAIL]
   |         |
   |         v
   |    [BLOCK INVOICE]
   |    [Set InvoiceBlocked = true]
   |
   v
Checks:
  ✓ Order status = OrderCompleted
  ✓ Dockets uploaded
  ✓ Serial numbers validated
  ✓ Photos uploaded
  ✓ No unresolved blocker
  ✓ Materials cost confirmed
  ✓ Partner billing rules satisfied
         |
         v
┌────────────────────────────────────────┐
│ RESOLVE PARTNER RATE                    │
│ (see Partner Rate Resolution above)      │
└────────────────────────────────────────┘
         |
         v
[Partner Rate: RM 150.00]
         |
         v
┌────────────────────────────────────────┐
│ CALCULATE INVOICE AMOUNTS                │
└────────────────────────────────────────┘
         |
         v
Invoice Line Item:
  Description: "GPON Activation FTTH - Prelaid"
  Quantity: 1
  Unit Price: RM 150.00
  Subtotal: RM 150.00
         |
         v
[Apply Tax (SST)]
    Company: Cephas Sdn. Bhd
    Tax Status: SST-Registered
    Tax Rate: 6%
    Tax Amount: RM 150.00 × 6% = RM 9.00
         |
         v
[Calculate Total]
    Subtotal: RM 150.00
    Tax (SST): RM 9.00
    Total: RM 159.00
         |
         v
┌────────────────────────────────────────┐
│ CREATE INVOICE                          │
└────────────────────────────────────────┘
         |
         v
Invoice {
  InvoiceNumber: "INV-CEPHAS-2025-00123"
  InvoiceDate: 2025-12-12
  DueDate: 2025-12-12 + 45 days
  PartnerId: TIME
  OrderId: [Order ID]
  SubTotal: RM 150.00
  TaxAmount: RM 9.00
  TotalAmount: RM 159.00
  Status: "Draft"
}
         |
         v
[Generate Invoice PDF]
         |
         v
[Submit to MyInvois]
         |
         v
[Receive e-Invoice Response]
    UUID: [from MyInvois]
    QR Code: [generated]
    Status: "Validated"
         |
         v
[Update Invoice]
    Status: "Invoiced"
    MyInvoisUuid: [UUID]
    MyInvoisQrCode: [QR Code]
    PortalUploadDate: 2025-12-12
         |
         v
[Invoice Locked]
    (Immutable after e-Invoice submission)
```

---

## Service Installer Payroll Calculation Flow

```
[Payroll Run Created]
    Period: 2025-11 (November)
    CompanyId: Cephas
         |
         v
[Fetch Completed Orders]
    Status = OrderCompleted
    CompletedAt between PeriodStart and PeriodEnd
         |
         v
[For each Order]
         |
         v
┌────────────────────────────────────────┐
│ RESOLVE SI RATE                          │
│ (see SI Rate Resolution above)           │
└────────────────────────────────────────┘
         |
         v
[SI Rate: RM 100.00 (Base)]
         |
         v
┌────────────────────────────────────────┐
│ CHECK KPI RESULT                         │
└────────────────────────────────────────┘
         |
    ┌────┴────┬──────────────┐
    |         |              |
    v         v              v
[OnTime] [Late] [ExceededSla]
   |         |              |
   |         |              |
   v         v              v
[Apply    [Apply        [Apply
 Bonus]   Penalty]      Higher
   |         |          Penalty]
   |         |              |
   |         |              |
   └─────────┴──────────────┘
         |
         v
[Calculate Final Pay]
    BaseRate: RM 100.00
    KPI Adjustment: +RM 10.00 (OnTime bonus)
    FinalPay: RM 110.00
         |
         v
┌────────────────────────────────────────┐
│ CREATE JobEarningRecord                 │
└────────────────────────────────────────┘
         |
         v
JobEarningRecord {
  OrderId: [Order ID]
  ServiceInstallerId: SI-123
  OrderTypeId: ACTIVATION
  OrderTypeCode: "ACTIVATION"
  BaseRate: RM 100.00
  KpiAdjustment: +RM 10.00
  FinalPay: RM 110.00
  Period: "2025-11"
  Status: "Draft"
}
         |
         v
[Repeat for all Orders in Period]
         |
         v
[Aggregate per SI]
         |
         v
┌────────────────────────────────────────┐
│ CREATE PayrollLine                      │
└────────────────────────────────────────┘
         |
         v
PayrollLine {
  PayrollRunId: [Run ID]
  ServiceInstallerId: SI-123
  TotalJobs: 25
  TotalBasePay: RM 2,500.00
  TotalKpiAdjustments: +RM 250.00
  NetPay: RM 2,750.00
}
         |
         v
[Generate Payroll Summary]
         |
         v
[Export for Bank Transfer]
```

---

## Rate Resolution Priority Comparison

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    PARTNER RATE RESOLUTION PRIORITY                      │
└─────────────────────────────────────────────────────────────────────────┘

Priority 1: Partner-Specific Rate (Most Specific)
    BillingRatecard
    - PartnerId = TIME (specific)
    - PartnerGroupId = TIME
    - DepartmentId = GPON
    - OrderTypeId = ACTIVATION
    - InstallationMethodId = PRELAID
    → Rate: RM 150.00

Priority 2: Partner Group Rate
    BillingRatecard
    - PartnerId = null
    - PartnerGroupId = TIME (group level)
    - DepartmentId = GPON
    - OrderTypeId = ACTIVATION
    - InstallationMethodId = PRELAID
    → Rate: RM 150.00 (default for TIME group)

Priority 3: Department Default Rate
    BillingRatecard
    - PartnerId = null
    - PartnerGroupId = null
    - DepartmentId = GPON
    - OrderTypeId = ACTIVATION
    - InstallationMethodId = PRELAID
    → Rate: RM 140.00 (GPON default)

┌─────────────────────────────────────────────────────────────────────────┐
│                    SI RATE RESOLUTION PRIORITY                           │
└─────────────────────────────────────────────────────────────────────────┘

Priority 1: Custom Rate Override (Highest Priority)
    GponSiCustomRate
    - InstallerId = SI-123 (specific SI)
    - DepartmentId = GPON
    - OrderTypeId = ACTIVATION
    - InstallationTypeId = FTTH
    - InstallationMethodId = PRELAID
    → Rate: RM 200.00 (special deal)

Priority 2: Default SI Rate by Type & Level
    GponSiJobRate
    - InstallerType = SUBCON
    - SiLevel = SENIOR
    - DepartmentId = GPON
    - OrderTypeId = ACTIVATION
    - InstallationTypeId = FTTH
    - InstallationMethodId = PRELAID
    → Rate: RM 100.00 (default for SUBCON SENIOR)

Priority 3: Employee Rate (Fallback)
    GponSiJobRate
    - InstallerType = EMPLOYEE
    - SiLevel = SENIOR
    - ... (same criteria)
    → Rate: RM 70.00 (employee rate)

Priority 4: Global Setting (Last Resort)
    GlobalSetting.KpiPrelaidDefaultHours
    → Use default value or block payroll
```

---

## Rate Card Data Model

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    PARTNER RATE CARD (BillingRatecard)                   │
└─────────────────────────────────────────────────────────────────────────┘

BillingRatecard
├── Id (Guid)
├── CompanyId (Guid)
├── DepartmentId (Guid?) - GPON, NWO, CWO
├── PartnerGroupId (Guid?) - TIME, CELCOM_DIGI, U_MOBILE
├── PartnerId (Guid?) - Specific partner override
├── OrderTypeId (Guid?) - ACTIVATION, ASSURANCE, etc.
├── ServiceCategory (string?) - FTTH, FTTO, FTTR, FTTC
├── InstallationMethodId (Guid?) - PRELAID, NON_PRELAID, etc.
├── BuildingType (string?) - Legacy field
├── Amount (decimal) - Rate in RM
├── TaxRate (decimal) - 0, 0.06 (SST)
├── Description (string?)
├── IsActive (bool)
├── ValidFrom (DateTime?)
└── ValidTo (DateTime?)

INDEXES:
  - (CompanyId, DepartmentId, PartnerGroupId, PartnerId, OrderTypeId, InstallationMethodId)
  - (CompanyId, DepartmentId, PartnerGroupId, OrderTypeId, InstallationMethodId)

┌─────────────────────────────────────────────────────────────────────────┐
│                    SI RATE CARD (GponSiJobRate)                          │
└─────────────────────────────────────────────────────────────────────────┘

GponSiJobRate
├── Id (Guid)
├── CompanyId (Guid)
├── InstallerType (enum) - EMPLOYEE, SUBCON
├── SiLevel (string) - JUNIOR, SENIOR, SUPERVISOR
├── DepartmentId (Guid) - GPON
├── OrderTypeId (Guid) - ACTIVATION, ASSURANCE, etc.
├── InstallationTypeId (Guid) - FTTH, FTTO, FTTR, FTTC
├── InstallationMethodId (Guid) - PRELAID, NON_PRELAID, etc.
├── DefaultRateAmount (decimal) - Rate in RM
├── Currency (string) - MYR
├── IsActive (bool)
├── ValidFrom (DateTime?)
└── ValidTo (DateTime?)

INDEXES:
  - (CompanyId, InstallerType, SiLevel, DepartmentId, OrderTypeId, InstallationTypeId, InstallationMethodId)

┌─────────────────────────────────────────────────────────────────────────┐
│                    SI CUSTOM RATE (GponSiCustomRate)                     │
└─────────────────────────────────────────────────────────────────────────┘

GponSiCustomRate
├── Id (Guid)
├── CompanyId (Guid)
├── InstallerId (Guid) - Specific SI
├── DepartmentId (Guid) - GPON
├── OrderTypeId (Guid) - ACTIVATION, ASSURANCE, etc.
├── InstallationTypeId (Guid) - FTTH, FTTO, etc.
├── InstallationMethodId (Guid) - PRELAID, NON_PRELAID, etc.
├── CustomRateAmount (decimal) - Override rate in RM
├── Currency (string) - MYR
├── IsActive (bool)
├── ValidFrom (DateTime?)
└── ValidTo (DateTime?)

INDEXES:
  - (CompanyId, InstallerId, DepartmentId, OrderTypeId, InstallationTypeId, InstallationMethodId)
```

---

## Complete Example: Order to Payment

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    EXAMPLE: COMPLETE FLOW                               │
└─────────────────────────────────────────────────────────────────────────┘

ORDER DETAILS:
  OrderId: ORD-001
  Partner: TIME (PartnerGroup: TIME)
  OrderType: ACTIVATION
  InstallationType: FTTH
  InstallationMethod: PRELAID
  ServiceInstaller: SI-123 (SUBCON, SENIOR)
  CompletedAt: 2025-11-15 14:30:00

STEP 1: PARTNER RATE RESOLUTION
────────────────────────────────
Lookup: BillingRatecard
  CompanyId: Cephas
  DepartmentId: GPON
  PartnerGroupId: TIME
  PartnerId: TIME (specific)
  OrderTypeId: ACTIVATION
  InstallationMethodId: PRELAID

Result: RM 150.00

STEP 2: INVOICE GENERATION
──────────────────────────
Invoice {
  InvoiceNumber: "INV-CEPHAS-2025-00123"
  InvoiceDate: 2025-11-15
  DueDate: 2025-12-30 (45 days)
  PartnerId: TIME
  LineItems: [
    {
      Description: "GPON Activation FTTH - Prelaid"
      Quantity: 1
      UnitPrice: RM 150.00
      Subtotal: RM 150.00
      TaxRate: 6%
      TaxAmount: RM 9.00
      Total: RM 159.00
    }
  ]
  SubTotal: RM 150.00
  TaxAmount: RM 9.00
  TotalAmount: RM 159.00
}

STEP 3: SI RATE RESOLUTION
──────────────────────────
Lookup 1: GponSiCustomRate
  InstallerId: SI-123
  DepartmentId: GPON
  OrderTypeId: ACTIVATION
  InstallationTypeId: FTTH
  InstallationMethodId: PRELAID
  → NOT FOUND

Lookup 2: GponSiJobRate
  InstallerType: SUBCON
  SiLevel: SENIOR
  DepartmentId: GPON
  OrderTypeId: ACTIVATION
  InstallationTypeId: FTTH
  InstallationMethodId: PRELAID
  → FOUND: RM 100.00

STEP 4: KPI EVALUATION
───────────────────────
Job Duration: 2 hours
Target Duration: 2 hours (Prelaid)
Result: OnTime

KPI Adjustment: +RM 10.00 (OnTime bonus)

STEP 5: PAYROLL CALCULATION
────────────────────────────
JobEarningRecord {
  OrderId: ORD-001
  ServiceInstallerId: SI-123
  OrderTypeId: ACTIVATION
  BaseRate: RM 100.00
  KpiAdjustment: +RM 10.00
  FinalPay: RM 110.00
  Period: "2025-11"
}

STEP 6: P&L CALCULATION
───────────────────────
Revenue: RM 150.00 (from invoice)
Cost: RM 110.00 (SI payout)
Margin: RM 40.00
Margin %: 26.67%

FINAL SUMMARY:
  Partner charged: RM 159.00 (including SST)
  SI paid: RM 110.00
  Company margin: RM 40.00
```

---

## Rate Lookup Examples

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    PARTNER RATE LOOKUP EXAMPLES                         │
└─────────────────────────────────────────────────────────────────────────┘

EXAMPLE 1: TIME Activation, FTTH, Prelaid
───────────────────────────────────────────
Order:
  Partner: TIME (PartnerGroup: TIME)
  OrderType: ACTIVATION
  InstallationType: FTTH
  InstallationMethod: PRELAID

Resolution:
  1. Try: PartnerId = TIME, PartnerGroupId = TIME
     → Match: RM 150.00
  2. If not found → Try: PartnerGroupId = TIME, PartnerId = null
     → Match: RM 150.00 (TIME group default)
  3. If not found → Try: PartnerGroupId = null, PartnerId = null
     → Match: RM 140.00 (GPON department default)

EXAMPLE 2: Celcom Activation, FTTO, Non-Prelaid
────────────────────────────────────────────────
Order:
  Partner: Celcom Fibre (PartnerGroup: CELCOM_DIGI)
  OrderType: ACTIVATION
  InstallationType: FTTO
  InstallationMethod: NON_PRELAID

Resolution:
  1. Try: PartnerId = Celcom Fibre
     → NOT FOUND
  2. Try: PartnerGroupId = CELCOM_DIGI, PartnerId = null
     → Match: RM 500.00 (Celcom/Digi group rate)
  3. If not found → Try: Department default
     → Match: RM 480.00 (GPON default for FTTO Non-Prelaid)

┌─────────────────────────────────────────────────────────────────────────┐
│                    SI RATE LOOKUP EXAMPLES                              │
└─────────────────────────────────────────────────────────────────────────┘

EXAMPLE 1: SUBCON SENIOR, Activation, FTTH, Prelaid
────────────────────────────────────────────────────
SI:
  InstallerId: SI-123
  InstallerType: SUBCON
  SiLevel: SENIOR
  OrderType: ACTIVATION
  InstallationType: FTTH
  InstallationMethod: PRELAID

Resolution:
  1. Try: GponSiCustomRate for SI-123
     → NOT FOUND
  2. Try: GponSiJobRate (SUBCON, SENIOR)
     → Match: RM 100.00
  3. If not found → Try: GponSiJobRate (EMPLOYEE, SENIOR)
     → Match: RM 70.00 (fallback)

EXAMPLE 2: Custom Rate Override
────────────────────────────────
SI:
  InstallerId: SI-456 (Special subcon)
  InstallerType: SUBCON
  SiLevel: SENIOR
  OrderType: ACTIVATION
  InstallationType: FTTH
  InstallationMethod: SDU

Resolution:
  1. Try: GponSiCustomRate for SI-456
     → FOUND: RM 200.00 (special deal)
  2. Use custom rate (ignores default rates)
     → Final: RM 200.00
```

---

## Integration Points

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    RATE SYSTEM INTEGRATION                              │
└─────────────────────────────────────────────────────────────────────────┘

1. ORDERS MODULE
   ┌─────────────────────────────────────┐
   │ When: Order status = ReadyForInvoice│
   │ Action: Resolve partner rate         │
   │ Result: Invoice line item amount     │
   │ Impact: Invoice total calculated    │
   └─────────────────────────────────────┘
                    │
                    v
   [BillingRatecard lookup]

2. BILLING MODULE
   ┌─────────────────────────────────────┐
   │ When: Invoice generation             │
   │ Action: Use resolved partner rate    │
   │ Result: Invoice created with amount  │
   │ Impact: Partner billed correctly    │
   └─────────────────────────────────────┘
                    │
                    v
   [Invoice.TotalAmount = Rate + Tax]

3. PAYROLL MODULE
   ┌─────────────────────────────────────┐
   │ When: Payroll run created           │
   │ Action: Resolve SI rate per order    │
   │ Result: JobEarningRecord created    │
   │ Impact: SI earnings calculated      │
   └─────────────────────────────────────┘
                    │
                    v
   [GponSiJobRate or GponSiCustomRate lookup]

4. P&L MODULE
   ┌─────────────────────────────────────┐
   │ When: P&L calculation               │
   │ Action: Compare revenue vs cost      │
   │ Revenue: From Invoice (partner rate) │
   │ Cost: From Payroll (SI rate)         │
   │ Result: Margin calculated            │
   │ Impact: Profitability tracked        │
   └─────────────────────────────────────┘
                    │
                    v
   [Margin = PartnerRate - SiRate]
```

---

## Rate Card Configuration Structure

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    PARTNER RATE CARD CONFIGURATION                       │
└─────────────────────────────────────────────────────────────────────────┘

BillingRatecard Examples:

ROW 1: TIME Activation FTTH Prelaid
  CompanyId: Cephas
  DepartmentId: GPON
  PartnerGroupId: TIME
  PartnerId: null (group-level)
  OrderTypeId: ACTIVATION
  InstallationMethodId: PRELAID
  Amount: RM 150.00
  TaxRate: 6% (SST)

ROW 2: TIME Activation FTTH Non-Prelaid
  CompanyId: Cephas
  DepartmentId: GPON
  PartnerGroupId: TIME
  PartnerId: null
  OrderTypeId: ACTIVATION
  InstallationMethodId: NON_PRELAID
  Amount: RM 330.00
  TaxRate: 6%

ROW 3: Celcom/Digi Activation FTTH Prelaid
  CompanyId: Cephas
  DepartmentId: GPON
  PartnerGroupId: CELCOM_DIGI
  PartnerId: null
  OrderTypeId: ACTIVATION
  InstallationMethodId: PRELAID
  Amount: RM 145.00
  TaxRate: 6%

┌─────────────────────────────────────────────────────────────────────────┐
│                    SI RATE CARD CONFIGURATION                            │
└─────────────────────────────────────────────────────────────────────────┘

GponSiJobRate Examples:

ROW 1: SUBCON SENIOR Activation FTTH Prelaid
  CompanyId: Cephas
  InstallerType: SUBCON
  SiLevel: SENIOR
  DepartmentId: GPON
  OrderTypeId: ACTIVATION
  InstallationTypeId: FTTH
  InstallationMethodId: PRELAID
  DefaultRateAmount: RM 100.00

ROW 2: SUBCON JUNIOR Activation FTTH Prelaid
  CompanyId: Cephas
  InstallerType: SUBCON
  SiLevel: JUNIOR
  DepartmentId: GPON
  OrderTypeId: ACTIVATION
  InstallationTypeId: FTTH
  InstallationMethodId: PRELAID
  DefaultRateAmount: RM 80.00

ROW 3: EMPLOYEE SENIOR Activation FTTH Prelaid
  CompanyId: Cephas
  InstallerType: EMPLOYEE
  SiLevel: SENIOR
  DepartmentId: GPON
  OrderTypeId: ACTIVATION
  InstallationTypeId: FTTH
  InstallationMethodId: PRELAID
  DefaultRateAmount: RM 70.00

┌─────────────────────────────────────────────────────────────────────────┐
│                    SI CUSTOM RATE EXAMPLES                               │
└─────────────────────────────────────────────────────────────────────────┘

GponSiCustomRate Examples:

ROW 1: Special Subcon - High Rate
  CompanyId: Cephas
  InstallerId: SI-456 (Mohan - special subcon)
  DepartmentId: GPON
  OrderTypeId: ACTIVATION
  InstallationTypeId: FTTH
  InstallationMethodId: SDU
  CustomRateAmount: RM 200.00 (higher than default)

ROW 2: Special Subcon - Lower Rate
  CompanyId: Cephas
  InstallerId: SI-789 (New subcon - training rate)
  DepartmentId: GPON
  OrderTypeId: ACTIVATION
  InstallationTypeId: FTTH
  InstallationMethodId: PRELAID
  CustomRateAmount: RM 60.00 (lower than default for training)
```

---

## Key Takeaways

1. **Two Separate Rate Systems:**
   - Partner Rate Cards (BillingRatecard) → What we charge partners
   - SI Rate Cards (GponSiJobRate/GponSiCustomRate) → What we pay installers

2. **Priority-Based Resolution:**
   - Partner: PartnerId → PartnerGroupId → Department Default
   - SI: CustomRate → DefaultRate by Type/Level → Employee Rate → Global Setting

3. **Invoice Calculation:**
   - Uses Partner Rate Card
   - Adds Tax (SST) if applicable
   - Generates e-Invoice via MyInvois

4. **Payroll Calculation:**
   - Uses SI Rate Card (custom or default)
   - Applies KPI adjustments (bonus/penalty)
   - Aggregates per SI per period

5. **P&L Integration:**
   - Revenue = Partner Rate (from invoice)
   - Cost = SI Rate (from payroll)
   - Margin = Revenue - Cost

6. **Rate Flexibility:**
   - Partner rates can vary by PartnerGroup, Partner, Department, OrderType, InstallationMethod
   - SI rates can vary by InstallerType, SiLevel, and can have custom overrides per SI

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/02_modules/billing/OVERVIEW.md` - Billing Module Specification
- `docs/02_modules/gpon/GPON_RATECARDS.md` - GPON Rate Cards Details
- `docs/02_modules/rate_engine/RATE_ENGINE.md` - Universal Rate Engine
- `docs/02_modules/payroll/OVERVIEW.md` - Payroll Module Specification


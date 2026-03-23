# Rates – System Workflow Diagram

**Date:** December 12, 2025  
**Purpose:** End-to-end workflow representation for the Rate Engine module, covering rate resolution, rate card management, and payout calculation

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         RATE ENGINE SYSTEM                               │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   REVENUE RATES        │      │   PAYOUT RATES         │
        │  (Partner Rate Cards) │      │  (SI Payout Rates)     │
        ├───────────────────────┤      ├───────────────────────┤
        │ • Partner Rate Cards   │      │ • SI Level Rates       │
        │ • Group Rate Cards     │      │ • Custom SI Rates      │
        │ • Default Rates        │      │ • Rate Resolution      │
        │ • Effective Dates      │      │ • KPI Adjustments      │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   RATE RESOLUTION      │      │   RATE CALCULATION     │
        │  (Priority Chain)      │      │  (Final Amounts)       │
        └───────────────────────┘      └───────────────────────┘
```

---

## Complete Workflow: Rate Resolution

```
[STEP 1: RATE RESOLUTION REQUEST]
         |
         v
[Order Completed]
  Order {
    OrderTypeId: "activation-type-id"
    InstallationTypeId: "prelaid-type-id"
    InstallationMethodId: "method-id"
    PartnerId: TIME
    PartnerGroupId: "time-group-id"
    AssignedSiId: "SI-123"
    SiLevel: "Senior"
  }
         |
         v
┌────────────────────────────────────────┐
│ RESOLVE GPON RATES                       │
│ RateEngineService.ResolveGponRatesAsync()│
└────────────────────────────────────────┘
         |
         v
GponRateResolutionRequest {
  OrderTypeId: "activation-type-id"
  InstallationTypeId: "prelaid-type-id"
  InstallationMethodId: "method-id"
  PartnerGroupId: "time-group-id"
  PartnerId: TIME
  ServiceInstallerId: "SI-123"
  SiLevel: "Senior"
  ReferenceDate: 2025-12-12
}
         |
         v
[STEP 2: RESOLVE REVENUE RATE]
         |
         v
┌────────────────────────────────────────┐
│ RESOLVE REVENUE RATE                     │
│ RateEngineService.ResolveGponRevenueRateInternalAsync()│
└────────────────────────────────────────┘
         |
         v
[Resolution Priority]
         |
         v
[Priority 1: Partner-Specific Rate]
  GponPartnerJobRate.find(
    OrderTypeId = "activation-type-id"
    InstallationTypeId = "prelaid-type-id"
    InstallationMethodId = "method-id"
    PartnerId = TIME
    EffectiveFrom <= ReferenceDate
    EffectiveTo >= ReferenceDate (or null)
    IsActive = true
  )
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NOT FOUND]
   |            |
   |            v
   |       [Priority 2: Partner Group Rate]
   |           GponPartnerJobRate.find(
   |             OrderTypeId = "activation-type-id"
   |             InstallationTypeId = "prelaid-type-id"
   |             InstallationMethodId = "method-id"
   |             PartnerGroupId = "time-group-id"
   |             PartnerId = null
   |             EffectiveFrom <= ReferenceDate
   |             EffectiveTo >= ReferenceDate (or null)
   |             IsActive = true
   |           )
   |            |
   |       ┌────┴────┐
   |       |         |
   |       v         v
   |   [FOUND] [NOT FOUND]
   |       |            |
   |       |            v
   |       |       [Priority 3: Default Rate]
   |       |           GponPartnerJobRate.find(
   |       |             OrderTypeId = "activation-type-id"
   |       |             InstallationTypeId = "prelaid-type-id"
   |       |             InstallationMethodId = "method-id"
   |       |             PartnerId = null
   |       |             PartnerGroupId = null
   |       |             EffectiveFrom <= ReferenceDate
   |       |             EffectiveTo >= ReferenceDate (or null)
   |       |             IsActive = true
   |       |           )
   |       |            |
   |       |       ┌────┴────┐
   |       |       |         |
   |       |       v         v
   |       |   [FOUND] [NO RATE]
   |       |       |            |
   |       |       |            v
   |       |       |       [Return null]
   |       |       |       [No revenue rate]
   |       |       |            |
   |       └───────┴────────────┘
   |               |
   └───────────────┘
         |
         v
[Revenue Rate Found]
  GponPartnerJobRate {
    Id: "rate-123"
    RevenueAmount: 150.00
    PartnerId: TIME
  }
         |
         v
[STEP 3: RESOLVE PAYOUT RATE]
         |
         v
┌────────────────────────────────────────┐
│ RESOLVE PAYOUT RATE                      │
│ RateEngineService.ResolveGponPayoutRateAsync()│
└────────────────────────────────────────┘
         |
         v
[Priority 1: Custom SI Rate]
  GponSiCustomRate.find(
    ServiceInstallerId = "SI-123"
    OrderTypeId = "activation-type-id"
    InstallationTypeId = "prelaid-type-id"
    InstallationMethodId = "method-id"
    PartnerGroupId = "time-group-id"
    EffectiveFrom <= ReferenceDate
    EffectiveTo >= ReferenceDate (or null)
    IsActive = true
  )
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NOT FOUND]
   |            |
   |            v
   |       [Priority 2: Default SI Rate by Level]
   |           GponSiJobRate.find(
   |             OrderTypeId = "activation-type-id"
   |             InstallationTypeId = "prelaid-type-id"
   |             InstallationMethodId = "method-id"
   |             SiLevel = "Senior"
   |             PartnerGroupId = "time-group-id"
   |             EffectiveFrom <= ReferenceDate
   |             EffectiveTo >= ReferenceDate (or null)
   |             IsActive = true
   |           )
   |            |
   |       ┌────┴────┐
   |       |         |
   |       v         v
   |   [FOUND] [NOT FOUND]
   |       |            |
   |       |            v
   |       |       [Priority 3: Default SI Rate (No Group)]
   |       |           GponSiJobRate.find(
   |       |             OrderTypeId = "activation-type-id"
   |       |             InstallationTypeId = "prelaid-type-id"
   |       |             InstallationMethodId = "method-id"
   |       |             SiLevel = "Senior"
   |       |             PartnerGroupId = null
   |       |             EffectiveFrom <= ReferenceDate
   |       |             EffectiveTo >= ReferenceDate (or null)
   |       |             IsActive = true
   |       |           )
   |       |            |
   |       |       ┌────┴────┐
   |       |       |         |
   |       |       v         v
   |       |   [FOUND] [NO RATE]
   |       |       |            |
   |       |       |            v
   |       |       |       [Return null]
   |       |       |       [No payout rate]
   |       |       |            |
   |       └───────┴────────────┘
   |               |
   └───────────────┘
         |
         v
[Payout Rate Found]
  GponSiJobRate {
    Id: "payout-rate-456"
    PayoutAmount: 80.00
    SiLevel: "Senior"
  }
         |
         v
[STEP 4: RETURN RATE RESOLUTION RESULT]
         |
         v
┌────────────────────────────────────────┐
│ RATE RESOLUTION RESULT                    │
└────────────────────────────────────────┘
         |
         v
GponRateResolutionResult {
  Success: true
  RevenueAmount: 150.00
  RevenueSource: "GponPartnerJobRate"
  RevenueRateId: "rate-123"
  PayoutAmount: 80.00
  PayoutSource: "GponSiJobRate"
  PayoutRateId: "payout-rate-456"
  ResolutionSteps: [
    "Starting revenue rate resolution...",
    "Revenue rate found: 150.00 MYR from GponPartnerJobRate",
    "Starting payout rate resolution...",
    "No custom rate found for SI, checking default payout rates",
    "Default payout rate found: 80.00 MYR from GponSiJobRate"
  ]
}
```

---

## Custom Rate Creation Workflow

```
[STEP 1: CREATE CUSTOM SI RATE]
         |
         v
┌────────────────────────────────────────┐
│ CREATE CUSTOM RATE                        │
│ POST /api/rates/custom-si-rates          │
└────────────────────────────────────────┘
         |
         v
CreateGponSiCustomRateDto {
  ServiceInstallerId: "SI-123"
  OrderTypeId: "activation-type-id"
  InstallationTypeId: "prelaid-type-id"
  InstallationMethodId: "method-id"
  PartnerGroupId: "time-group-id"
  CustomPayoutAmount: 100.00
  EffectiveFrom: 2025-01-01
  EffectiveTo: null
  IsActive: true
}
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE CUSTOM RATE                     │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[VALID] [INVALID]
   |         |
   |         v
   |    [Return Validation Errors]
   |
   v
Checks:
  ✓ ServiceInstallerId exists
  ✓ OrderTypeId exists
  ✓ InstallationTypeId exists
  ✓ CustomPayoutAmount > 0
  ✓ EffectiveFrom <= EffectiveTo (if both provided)
         |
         v
┌────────────────────────────────────────┐
│ CREATE CUSTOM RATE RECORD                 │
└────────────────────────────────────────┘
         |
         v
GponSiCustomRate {
  Id: Guid.NewGuid()
  CompanyId: Cephas
  ServiceInstallerId: "SI-123"
  OrderTypeId: "activation-type-id"
  InstallationTypeId: "prelaid-type-id"
  InstallationMethodId: "method-id"
  PartnerGroupId: "time-group-id"
  CustomPayoutAmount: 100.00
  EffectiveFrom: 2025-01-01
  EffectiveTo: null
  IsActive: true
  CreatedAt: DateTime.UtcNow
  UpdatedAt: DateTime.UtcNow
}
         |
         v
[Save to Database]
  _context.GponSiCustomRates.Add(customRate)
  await _context.SaveChangesAsync()
         |
         v
[Custom Rate Active]
  → Takes priority over default SI rates
```

---

## Rate Card Creation Workflow

```
[STEP 1: CREATE PARTNER RATE CARD]
         |
         v
┌────────────────────────────────────────┐
│ CREATE RATE CARD                          │
│ POST /api/billing-ratecards              │
└────────────────────────────────────────┘
         |
         v
CreateBillingRatecardDto {
  PartnerId: TIME
  OrderTypeId: "activation-type-id"
  RevenueAmount: 150.00
  EffectiveFrom: 2025-01-01
  EffectiveTo: null
  IsActive: true
}
         |
         v
[Create Rate Card]
  BillingRatecard {
    Id: Guid.NewGuid()
    CompanyId: Cephas
    PartnerId: TIME
    OrderTypeId: "activation-type-id"
    RevenueAmount: 150.00
    EffectiveFrom: 2025-01-01
    EffectiveTo: null
    IsActive: true
  }
         |
         v
[Save Rate Card]
  _context.BillingRatecards.Add(ratecard)
  await _context.SaveChangesAsync()
         |
         v
[Rate Card Available for Resolution]
```

---

## Rate Resolution Priority Examples

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    RATE RESOLUTION EXAMPLES                             │
└─────────────────────────────────────────────────────────────────────────┘

EXAMPLE 1: Custom SI Rate (Highest Priority)
────────────────────────────────────────────
Request:
  ServiceInstallerId: "SI-123"
  OrderTypeId: "activation-type-id"
  SiLevel: "Senior"

Resolution:
  Priority 1: GponSiCustomRate.find(
    ServiceInstallerId = "SI-123"
    OrderTypeId = "activation-type-id"
  )
  → FOUND: CustomPayoutAmount = 100.00
  → Use: 100.00 MYR

EXAMPLE 2: Default SI Rate by Level
─────────────────────────────────────
Request:
  ServiceInstallerId: "SI-456" (no custom rate)
  OrderTypeId: "activation-type-id"
  SiLevel: "Senior"

Resolution:
  Priority 1: GponSiCustomRate.find(...)
  → NOT FOUND

  Priority 2: GponSiJobRate.find(
    OrderTypeId = "activation-type-id"
    SiLevel = "Senior"
  )
  → FOUND: PayoutAmount = 80.00
  → Use: 80.00 MYR

EXAMPLE 3: Partner-Specific Revenue Rate
──────────────────────────────────────────
Request:
  PartnerId: TIME
  OrderTypeId: "activation-type-id"

Resolution:
  Priority 1: GponPartnerJobRate.find(
    PartnerId = TIME
    OrderTypeId = "activation-type-id"
  )
  → FOUND: RevenueAmount = 150.00
  → Use: 150.00 MYR

EXAMPLE 4: Partner Group Revenue Rate (Fallback)
─────────────────────────────────────────────────
Request:
  PartnerId: TIME (no partner-specific rate)
  PartnerGroupId: "time-group-id"
  OrderTypeId: "activation-type-id"

Resolution:
  Priority 1: GponPartnerJobRate.find(
    PartnerId = TIME
    OrderTypeId = "activation-type-id"
  )
  → NOT FOUND

  Priority 2: GponPartnerJobRate.find(
    PartnerGroupId = "time-group-id"
    PartnerId = null
    OrderTypeId = "activation-type-id"
  )
  → FOUND: RevenueAmount = 140.00
  → Use: 140.00 MYR
```

---

## Entities Involved

### GponPartnerJobRate Entity
```
GponPartnerJobRate
├── Id (Guid)
├── CompanyId (Guid)
├── PartnerId (Guid?)
├── PartnerGroupId (Guid?)
├── OrderTypeId (Guid)
├── InstallationTypeId (Guid)
├── InstallationMethodId (Guid?)
├── RevenueAmount (decimal)
├── EffectiveFrom (DateTime)
├── EffectiveTo (DateTime?)
├── IsActive (bool)
└── CreatedAt, UpdatedAt
```

### GponSiJobRate Entity
```
GponSiJobRate
├── Id (Guid)
├── CompanyId (Guid)
├── OrderTypeId (Guid)
├── InstallationTypeId (Guid)
├── InstallationMethodId (Guid?)
├── SiLevel (string)
├── PartnerGroupId (Guid?)
├── PayoutAmount (decimal)
├── EffectiveFrom (DateTime)
├── EffectiveTo (DateTime?)
├── IsActive (bool)
└── CreatedAt, UpdatedAt
```

### GponSiCustomRate Entity
```
GponSiCustomRate
├── Id (Guid)
├── CompanyId (Guid)
├── ServiceInstallerId (Guid)
├── OrderTypeId (Guid)
├── InstallationTypeId (Guid)
├── InstallationMethodId (Guid?)
├── PartnerGroupId (Guid?)
├── CustomPayoutAmount (decimal)
├── EffectiveFrom (DateTime)
├── EffectiveTo (DateTime?)
├── IsActive (bool)
└── CreatedAt, UpdatedAt
```

---

## API Endpoints Involved

### Rate Resolution
- `POST /api/rates/resolve-gpon` - Resolve GPON rates
  - Request: `GponRateResolutionRequest`
  - Response: `GponRateResolutionResult`

- `GET /api/rates/revenue` - Get revenue rate
  - Query params: `orderTypeId`, `installationTypeId`, `partnerId`, etc.
  - Response: `decimal?`

- `GET /api/rates/payout` - Get payout rate
  - Query params: `orderTypeId`, `installationTypeId`, `serviceInstallerId`, `siLevel`, etc.
  - Response: `decimal?`

### Rate Card Management
- `GET /api/billing-ratecards` - List rate cards
- `GET /api/billing-ratecards/{id}` - Get rate card
- `POST /api/billing-ratecards` - Create rate card
- `PUT /api/billing-ratecards/{id}` - Update rate card
- `DELETE /api/billing-ratecards/{id}` - Delete rate card

### Custom SI Rates
- `GET /api/rates/custom-si-rates` - List custom SI rates
- `GET /api/rates/custom-si-rates/{id}` - Get custom SI rate
- `POST /api/rates/custom-si-rates` - Create custom SI rate
- `PUT /api/rates/custom-si-rates/{id}` - Update custom SI rate
- `DELETE /api/rates/custom-si-rates/{id}` - Delete custom SI rate

---

## Module Rules & Validations

### Revenue Rate Rules
- Resolution priority: Partner → Partner Group → Default
- Effective dates must be valid (EffectiveFrom <= EffectiveTo)
- Only one active rate per combination at a time
- Partner-specific rates override group rates

### Payout Rate Rules
- Resolution priority: Custom SI Rate → Default SI Rate by Level
- Custom rates take highest priority
- SiLevel required for default rate lookup
- Effective dates must be valid

### Rate Card Rules
- RevenueAmount must be > 0
- EffectiveFrom is required
- EffectiveTo can be null (indefinite)
- Only one active rate per (Partner/Group, OrderType, InstallationType) at a time

### Custom Rate Rules
- CustomPayoutAmount must be > 0
- ServiceInstallerId must exist
- Custom rates override all default rates
- Effective dates must be valid

---

## Integration Points

### Billing Module
- Revenue rates used for invoice generation
- Rate cards linked to partners
- Invoice amounts calculated from resolved rates

### Payroll Module
- Payout rates used for SI earnings calculation
- Custom rates applied per SI
- KPI adjustments applied on top of rates

### Orders Module
- Rate resolution triggered on order completion
- Rates stored in JobEarningRecord
- Rate resolution logged for audit

### Partners Module
- Partner-specific rate cards
- Partner group rate fallback
- Partner rate card management

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/02_modules/rate_engine/RATE_ENGINE.md` - Rate engine specification
- `docs/02_modules/billing/INVOICE_RATE_CALCULATION_FLOW.md` - Rate usage in billing


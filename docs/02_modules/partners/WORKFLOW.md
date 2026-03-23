# Partners – System Workflow Diagram

**Date:** December 12, 2025  
**Purpose:** End-to-end workflow representation for the Partners module, covering partner creation, partner group management, and partner-specific configurations

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        PARTNERS MODULE SYSTEM                            │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   PARTNER CREATION     │      │   PARTNER GROUPS        │
        │  (Setup, Validation)   │      │  (Grouping, Hierarchy)  │
        ├───────────────────────┤      ├───────────────────────┤
        │ • Create Partner       │      │ • Create Partner Group  │
        │ • Link to Group        │      │ • Assign Partners       │
        │ • Set Active Status    │      │ • Group-Level Settings  │
        │ • Configure Settings   │      │ • Group Rate Cards      │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   PARTNER CONFIG       │      │   PARTNER INTEGRATION  │
        │  (Rate Cards, Settings)│      │  (Email, API)          │
        └───────────────────────┘      └───────────────────────┘
```

---

## Complete Workflow: Partner Setup

```
[STEP 1: CREATE PARTNER GROUP]
         |
         v
┌────────────────────────────────────────┐
│ CREATE PARTNER GROUP REQUEST              │
│ POST /api/partner-groups                 │
└────────────────────────────────────────┘
         |
         v
CreatePartnerGroupDto {
  Name: "TIME Group"
  Code: "TIME"
  Description: "TIME partners group"
  IsActive: true
}
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE PARTNER GROUP                    │
│ PartnerGroupService.CreateAsync()         │
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
  ✓ Name not empty
  ✓ Code not empty
  ✓ Code unique per company
         |
         v
┌────────────────────────────────────────┐
│ CREATE PARTNER GROUP RECORD               │
└────────────────────────────────────────┘
         |
         v
PartnerGroup {
  Id: Guid.NewGuid()
  CompanyId: Cephas
  Name: "TIME Group"
  Code: "TIME"
  Description: "TIME partners group"
  IsActive: true
  CreatedAt: DateTime.UtcNow
  UpdatedAt: DateTime.UtcNow
}
         |
         v
[Save to Database]
  _context.PartnerGroups.Add(group)
  await _context.SaveChangesAsync()
         |
         v
[STEP 2: CREATE PARTNER]
         |
         v
┌────────────────────────────────────────┐
│ CREATE PARTNER REQUEST                    │
│ POST /api/partners                       │
└────────────────────────────────────────┘
         |
         v
CreatePartnerDto {
  Name: "TIME dotCom Sdn. Bhd."
  Code: "TIME"
  PartnerGroupId: "group-123"
  ContactPerson: "John Doe"
  ContactEmail: "john@time.com"
  ContactPhone: "03-12345678"
  IsActive: true
}
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE PARTNER DATA                     │
│ PartnerService.CreateAsync()              │
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
  ✓ Name not empty
  ✓ Code not empty
  ✓ Code unique per company
  ✓ PartnerGroupId exists (if provided)
         |
         v
┌────────────────────────────────────────┐
│ CREATE PARTNER RECORD                     │
└────────────────────────────────────────┘
         |
         v
Partner {
  Id: Guid.NewGuid()
  CompanyId: Cephas
  PartnerGroupId: "group-123"
  Name: "TIME dotCom Sdn. Bhd."
  Code: "TIME"
  ContactPerson: "John Doe"
  ContactEmail: "john@time.com"
  ContactPhone: "03-12345678"
  IsActive: true
  CreatedAt: DateTime.UtcNow
  UpdatedAt: DateTime.UtcNow
}
         |
         v
[Save to Database]
  _context.Partners.Add(partner)
  await _context.SaveChangesAsync()
         |
         v
[STEP 3: CONFIGURE PARTNER RATE CARD]
         |
         v
┌────────────────────────────────────────┐
│ CREATE RATE CARD                          │
│ POST /api/billing-ratecards              │
└────────────────────────────────────────┘
         |
         v
CreateBillingRatecardDto {
  PartnerId: partner.Id
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
    PartnerId: partner.Id
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
[STEP 4: CONFIGURE PARTNER WORKFLOW]
         |
         v
┌────────────────────────────────────────┐
│ CREATE PARTNER-SPECIFIC WORKFLOW           │
│ POST /api/workflow-definitions            │
└────────────────────────────────────────┘
         |
         v
CreateWorkflowDefinitionDto {
  Name: "TIME Order Workflow"
  EntityType: "Order"
  PartnerId: partner.Id
  IsActive: true
}
         |
         v
[Create Workflow]
  WorkflowDefinition {
    Id: Guid.NewGuid()
    CompanyId: Cephas
    PartnerId: partner.Id
    EntityType: "Order"
    Name: "TIME Order Workflow"
    IsActive: true
  }
         |
         v
[Partner Configuration Complete]
```

---

## Partner Resolution in Orders

```
[Order Created]
  Order {
    ServiceId: "TBBN1234567"
    PartnerId: null (not yet set)
  }
         |
         v
┌────────────────────────────────────────┐
│ AUTO-DETECT PARTNER FROM SERVICE ID       │
│ OrderService.DetectPartnerFromServiceId()  │
└────────────────────────────────────────┘
         |
         v
[Check Service ID Format]
  ServiceId: "TBBN1234567"
         |
         v
[TBBN Format Detected]
  Pattern: TBBN[A-Z]?\d+[A-Z]?
  → TIME partner
         |
         v
[Find TIME Partner]
  Partner.find(
    Code = "TIME"
    IsActive = true
  )
         |
         v
[Set Partner on Order]
  Order.PartnerId = timePartner.Id
         |
         v
[Partner-Specific Workflow Applied]
  WorkflowDefinition.find(
    EntityType = "Order"
    PartnerId = timePartner.Id
  )
         |
         v
[Partner-Specific Rate Card Applied]
  BillingRatecard.find(
    PartnerId = timePartner.Id
    OrderTypeId = order.OrderTypeId
  )
```

---

## Partner Group Hierarchy

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    PARTNER GROUP STRUCTURE                              │
└─────────────────────────────────────────────────────────────────────────┘

PartnerGroup: "TIME Group"
  │
  ├─→ Partner: "TIME dotCom Sdn. Bhd."
  │     │
  │     ├─→ Rate Cards
  │     ├─→ Workflow Definitions
  │     └─→ Email Accounts
  │
  └─→ Partner: "TIME Fibre"
        │
        ├─→ Rate Cards
        ├─→ Workflow Definitions
        └─→ Email Accounts

[Group-Level Settings]
  - Group rate cards (fallback)
  - Group workflow definitions (fallback)
  - Group email parsing rules
```

---

## Entities Involved

### PartnerGroup Entity
```
PartnerGroup
├── Id (Guid)
├── CompanyId (Guid)
├── Name (string)
├── Code (string, unique per company)
├── Description (string?)
├── IsActive (bool)
├── CreatedAt (DateTime)
└── UpdatedAt (DateTime)
```

### Partner Entity
```
Partner
├── Id (Guid)
├── CompanyId (Guid)
├── PartnerGroupId (Guid?)
├── Name (string)
├── Code (string, unique per company)
├── ContactPerson (string?)
├── ContactEmail (string?)
├── ContactPhone (string?)
├── IsActive (bool)
├── CreatedAt (DateTime)
└── UpdatedAt (DateTime)
```

---

## API Endpoints Involved

### Partner Groups
- `GET /api/partner-groups` - List partner groups
- `GET /api/partner-groups/{id}` - Get partner group details
- `POST /api/partner-groups` - Create partner group
- `PUT /api/partner-groups/{id}` - Update partner group
- `DELETE /api/partner-groups/{id}` - Delete partner group

### Partners
- `GET /api/partners` - List partners (optional: `?partnerGroupId=...`)
- `GET /api/partners/{id}` - Get partner details
- `POST /api/partners` - Create partner
- `PUT /api/partners/{id}` - Update partner
- `DELETE /api/partners/{id}` - Delete partner

---

## Module Rules & Validations

### Partner Group Rules
- Name is required
- Code is required and must be unique per company
- Partner groups can have multiple partners
- Group-level settings act as fallback

### Partner Rules
- Name is required
- Code is required and must be unique per company
- PartnerGroupId optional (partners can be ungrouped)
- Partner code used for auto-detection from Service ID

### Partner Resolution Rules
- TBBN format → TIME partner
- Partner-specific Service ID formats → specific partner
- Auto-detection happens on order creation
- Manual partner selection always allowed

---

## Integration Points

### Orders Module
- Orders linked to partners
- Partner auto-detection from Service ID
- Partner-specific workflows applied

### Billing Module
- Partner rate cards for revenue calculation
- Partner-specific invoice templates
- Partner payment terms

### Workflow Module
- Partner-specific workflow definitions
- Partner-level guard conditions
- Partner-level side effects

### Email Parser Module
- Partner email accounts
- Partner-specific parsing rules
- Partner email templates

### Rate Engine Module
- Partner rate resolution
- Partner group rate fallback
- Partner-specific custom rates

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/02_modules/partners/OVERVIEW.md` - Partners module overview
- `docs/05_data_model/relationships/company_partner_relationships.md` - Relationship diagrams


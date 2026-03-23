# Companies – System Workflow Diagram

**Date:** December 12, 2025  
**Purpose:** End-to-end workflow representation for the Companies module, covering company creation, configuration, and SaaS multi-tenant operations

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        COMPANIES MODULE SYSTEM                           │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   COMPANY CREATION     │      │   COMPANY CONFIG       │
        │  (Setup, Validation)   │      │  (Settings, Locale)     │
        ├───────────────────────┤      ├───────────────────────┤
        │ • Create Company       │      │ • Locale Settings       │
        │ • Validate Uniqueness   │      │ • Timezone              │
        │ • Set Defaults          │      │ • Date/Time Format      │
        │ • SaaS Tenant Mode      │      │ • Currency              │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   COMPANY CONTEXT      │      │   COMPANY DATA         │
        │  (Current Company)      │      │  (Scoped Entities)     │
        └───────────────────────┘      └───────────────────────┘
```

---

## Complete Workflow: Company Setup

```
[STEP 1: COMPANY CREATION]
         |
         v
┌────────────────────────────────────────┐
│ CREATE COMPANY REQUEST                    │
│ POST /api/companies                      │
└────────────────────────────────────────┘
         |
         v
CreateCompanyDto {
  LegalName: "Cephas Sdn. Bhd."
  ShortName: "Cephas"
  Vertical: "ISP"
  RegistrationNo: "123456-A"
  TaxId: "TAX123456"
  Address: "123 Main Street, KL"
  Phone: "03-12345678"
  Email: "info@cephas.com"
  DefaultTimezone: "Asia/Kuala_Lumpur"
  DefaultDateFormat: "dd/MM/yyyy"
  DefaultTimeFormat: "hh:mm a"
  DefaultCurrency: "MYR"
  DefaultLocale: "en-MY"
  IsActive: true
}
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE COMPANY CREATION                    │
│ CompanyService.CreateCompanyAsync()        │
└────────────────────────────────────────┘
         |
         v
[Check Existing Companies]
  Company.count()
         |
    ┌────┴────┐
    |         |
    v         v
[COUNT = 0] [COUNT > 0]
   |            |
   |            v
   |       [Throw InvalidOperationException]
   |       "A company with this ShortName already exists."
   |
   v
┌────────────────────────────────────────┐
│ VALIDATE SHORT NAME UNIQUENESS            │
└────────────────────────────────────────┘
         |
         v
[Check Duplicate Short Name]
  Company.find(ShortName = "Cephas")
         |
    ┌────┴────┐
    |         |
    v         v
[NOT FOUND] [FOUND]
   |            |
   |            v
   |       [Throw InvalidOperationException]
   |       "A company with short name 'Cephas' already exists."
   |
   v
┌────────────────────────────────────────┐
│ NORMALIZE VALUES                          │
└────────────────────────────────────────┘
         |
         v
[Normalize Required Fields]
  LegalName: Trim, normalize whitespace
  ShortName: Trim, normalize whitespace
  Vertical: Trim, normalize whitespace
         |
         v
[Normalize Optional Fields]
  RegistrationNo: Trim or null
  TaxId: Trim or null
  Address: Trim or null
  Phone: Trim or null
  Email: Trim or null
         |
         v
┌────────────────────────────────────────┐
│ CREATE COMPANY RECORD                     │
└────────────────────────────────────────┘
         |
         v
Company {
  Id: Guid.NewGuid()
  LegalName: "Cephas Sdn. Bhd."
  ShortName: "Cephas"
  Vertical: "ISP"
  RegistrationNo: "123456-A"
  TaxId: "TAX123456"
  Address: "123 Main Street, KL"
  Phone: "03-12345678"
  Email: "info@cephas.com"
  IsActive: true
  DefaultTimezone: "Asia/Kuala_Lumpur"
  DefaultDateFormat: "dd/MM/yyyy"
  DefaultTimeFormat: "hh:mm a"
  DefaultCurrency: "MYR"
  DefaultLocale: "en-MY"
  CreatedAt: DateTime.UtcNow
  UpdatedAt: DateTime.UtcNow
}
         |
         v
[Save to Database]
  _context.Set<Company>().Add(company)
  await _context.SaveChangesAsync()
         |
         v
[STEP 2: COMPANY UPDATE]
         |
         v
┌────────────────────────────────────────┐
│ UPDATE COMPANY                           │
│ PUT /api/companies/{id}                 │
└────────────────────────────────────────┘
         |
         v
UpdateCompanyDto {
  ShortName: "CephasOps" (changed)
  Email: "contact@cephasops.com" (changed)
}
         |
         v
[Get Existing Company]
  Company.find(Id = companyId)
         |
         v
[Validate Short Name Change]
  If ShortName changed:
    Check for duplicates
    Normalize new value
         |
         v
[Update Company]
  company.ShortName = "CephasOps"
  company.Email = "contact@cephasops.com"
  company.UpdatedAt = DateTime.UtcNow
         |
         v
[Save Changes]
  await _context.SaveChangesAsync()
         |
         v
[STEP 3: COMPANY CONTEXT USAGE]
         |
         v
[System Uses Company Context]
  ICurrentUserService.CompanyId
    → Returns tenant CompanyId from user context
         |
         v
[Query Company-Scoped Data]
  Orders.find(CompanyId = Guid.Empty or actual company ID)
         |
         v
[Company Settings Applied]
  Date Format: "dd/MM/yyyy"
  Time Format: "hh:mm a"
  Currency: "MYR"
  Timezone: "Asia/Kuala_Lumpur"
```

---

## Multi-Tenant Company Rules

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    MULTI-TENANT COMPANY RULES                            │
└─────────────────────────────────────────────────────────────────────────┘

[Rule 1: Company Provisioning]
  New companies created via tenant provisioning workflow
  → Enforced in CreateCompanyAsync()

[Rule 2: CompanyId Context]
  ICurrentUserService.CompanyId returns tenant-scoped CompanyId
  → All entities use explicit company context

[Rule 3: Tenant Isolation]
  Users are scoped to their assigned company
  → Company context enforced via EF Core global query filters

[Rule 4: Company-Scoped Queries]
  Queries use:
    - CompanyId = SuperAdmin's selected context (for SuperAdmin)
    - CompanyId = actualCompanyId (for regular users)
    - All entities are company-scoped
```

---

## Company Locale Settings

```
[Company Locale Configuration]
  Company {
    DefaultTimezone: "Asia/Kuala_Lumpur"
    DefaultDateFormat: "dd/MM/yyyy"
    DefaultTimeFormat: "hh:mm a"
    DefaultCurrency: "MYR"
    DefaultLocale: "en-MY"
  }
         |
         v
[Frontend Uses Locale Settings]
  Date Display: 15/12/2025 (dd/MM/yyyy)
  Time Display: 02:30 PM (hh:mm a)
  Currency Display: RM 100.00 (MYR)
  Locale: en-MY
         |
         v
[Backend Uses Locale Settings]
  Date Parsing: "15/12/2025" → DateTime
  Currency Formatting: 100.00 → "RM 100.00"
  Timezone Conversion: UTC → Asia/Kuala_Lumpur
```

---

## Entities Involved

### Company Entity
```
Company
├── Id (Guid)
├── LegalName (string)
├── ShortName (string, unique)
├── Vertical (string: ISP, Barbershop, Travel)
├── RegistrationNo (string?)
├── TaxId (string?)
├── Address (string?)
├── Phone (string?)
├── Email (string?)
├── IsActive (bool)
├── DefaultTimezone (string)
├── DefaultDateFormat (string)
├── DefaultTimeFormat (string)
├── DefaultCurrency (string)
├── DefaultLocale (string)
├── CreatedAt (DateTime)
└── UpdatedAt (DateTime)
```

---

## API Endpoints Involved

### Company Management
- `GET /api/companies` - List companies (scoped by user role and tenant context)
- `GET /api/companies/{id}` - Get company details
- `POST /api/companies` - Create company (only if no company exists)
- `PUT /api/companies/{id}` - Update company
- `DELETE /api/companies/{id}` - Delete company (soft delete)

---

## Module Rules & Validations

### Company Creation Rules
- Company ShortName must be globally unique across all tenants
- ShortName must be unique
- LegalName is required
- Vertical is required
- Locale settings have defaults if not provided

### Company Update Rules
- ShortName change must maintain uniqueness
- All fields optional in update (only provided fields updated)
- IsActive can be toggled
- Locale settings can be updated

### Multi-Tenant Isolation Rules
- CompanyId context is tenant-scoped for all users
- Users operate within their assigned company context
- All entities explicitly scoped to tenant CompanyId
- SuperAdmin can view data across all tenants

---

## Integration Points

### All Modules
- All modules use company context for data scoping
- Company locale settings affect date/time/currency formatting
- Company vertical affects available features

### Departments Module
- Departments belong to company
- Department filtering works with company context

### Orders Module
- Orders belong to company
- Company settings affect order display

### Billing Module
- Invoices belong to company
- Company tax settings used for invoicing
- Company currency used for amounts

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/02_modules/global_settings/COMPANIES_SETUP.md` - Company setup guide
- `docs/02_modules/global_settings/MULTI_COMPANY_MODULE.md` - Multi-company documentation (historical)


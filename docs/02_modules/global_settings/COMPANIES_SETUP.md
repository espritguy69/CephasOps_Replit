
# COMPANIES_SETUP.md
Full Architecture Documentation — Multi-Company Setup for CephasOps

---

## 1. Purpose

This module defines how CephasOps supports **multiple companies**, each with different:

- Branding
- Vertical (ISP / Barbershop / Travel / Trading)
- Modules enabled
- Templates
- Partners
- Employees
- Materials
- Payroll configurations
- Tax profiles
- Invoice sequences

This ensures CephasOps can run operations for:

1. **Cephas Sdn. Bhd (ISP)**
2. **Cephas Trading & Services (ISP)**
3. **Kingsman Classic Services (Barbershop & Spa)**
4. **Menorah Travel & Tours Sdn Bhd (Travel)**

…and future companies without code changes.

---

## 2. Company Profile Structure

Each company has a configurable profile stored in:

```
company_settings
company_branding
company_integrations
company_modules
company_ratecards
company_tax_profiles
```

### 2.1 Core Fields

```
CompanyId
CompanyName
BusinessNature (ISP, Barbershop, Travel, Mixed)
ROCNumber
SSTNumber
GSTNumber (optional future)
Email
Phone
Address
Website
Logo
PrimaryColour
SecondaryColour
Timezone
Currency
```

### 2.2 Branding

Each company can upload:

- Logo
- Letterhead template
- Footer block
- Digital stamp/signature
- Branch-specific branding (optional)

---

## 3. Vertical / Business-Type Configuration

### 3.1 ISP (Cephas, Cephas Trading)

Modules enabled:

- Orders
- Scheduler
- Inventory
- RMA
- Billing
- SI App
- Splitters
- Payroll
- P&L
- Tax & eInvoice
- Email Parser

Partners linked:

- TIME
- Celcom
- Digi
- U-Mobile

Special rules:

- Unique Service ID patterns (TBBN…, CELCOM…, DIGI…)
- KPI matrices for SI
- Assurance URLs (TTKT / AWO links)
- BOQ material presets per building type

### 3.1 ISP Vertical (Current Phase: GPON Only)

CephasOps is currently operating as **single-company, single-vertical**, focused 
on the **GPON Department** under the ISP vertical.

Only the following are active:

- Orders (GPON lifecycle)
- Scheduler (GPON SI assignments)
- Inventory (GPON materials)
- Splitters
- Email Parser (GPON-exclusive)
- Billing
- RMA (ISP)
- Payroll
- P&L

Active Partner Groups (GPON):
- TIME (Activation / Modification / Assurance)
- TIME–Digi HSBB
- TIME–Celcom HSBB

Existing Parser Templates are scoped ONLY for **GPON** and are fully defined in:
`Settings → Parser → Parser Templates`.

### Future-Ready Vertical Expansion
CWO and NWO (under TIME Group but separate departments) will adopt the same 
architecture later by simply adding:

- New Parser Templates
- New Email Accounts
- New Partner Groups
- Their own lifecycle specs

No architectural changes needed.

### 3.2 Barbershop (Kingsman)

Modules enabled:

- POS (future)
- Scheduling (appointments)
- Product inventory
- Payroll
- P&L
- Receipt templates

Special rules:

- GST/SST rules for services vs products
- Kingsman-branded receipts & invoices

### 3.3 Travel (Menorah Travel)

Modules enabled:

- Itinerary builder (future)
- Travel receipts
- Customer management
- P&L
- Invoice templates

Special rules:

- Zero-rated exports
- Mixed tax items
- Customer-specific itineraries

---

## 4. Module Access By Company

Each company can choose which modules are active.

Example config:

```
{
  "companyId": 1,
  "modules": {
    "Orders": true,
    "Scheduler": true,
    "Inventory": true,
    "RMA": true,
    "Billing": true,
    "SIApp": true,
    "SplitterTracking": true,
    "POS": false,
    "Itinerary": false
  }
}
```

---

## 5. Company-Specific Rate Cards

### 5.1 ISP

Rate cards for:

- FTTH Activation
- Modification
- Assurance
- FTTR / FTTC / SDU / RDF POLE

Per partner (TIME, Celcom, Digi).

### 5.2 Barbershop

Kingsman price list:

- Haircut
- Beard
- Coloring
- Packages

### 5.3 Travel

Package price breakdowns.

---

## 6. Payroll Configurations Per Company

Each company defines:

- Allowances
- OT Rules
- Public holidays based on region
- SI performance-based pay (Cephas ISP)
- Commission structure (Kingsman)
- Travel agent commission (Menorah)

Stored under:

```
company_payroll_settings
```

---

## 7. Tax Profiles Per Company

Each company sets:

- SST/GST status
- Tax codes per item
- Invoice prefixes
- LHDN eInvoice credentials (if applicable)
- Rounding rules

---

## 8. Integrations

Different company = different integration credentials.

Examples:

### ISP (Cephas)
- TIME partner portal
- TTKT/ AWO upload links
- RMA return emails

### Kingsman
- POS device integration
- Payment gateways

### Menorah
- Airline APIs (future)
- Hotel booking vendors

---

## 9. Document Templates Per Company

Stored per-company at:

```
/templates/{companySlug}/
```

Includes:

- Invoice templates
- BOQ templates
- Receipt templates
- RMA forms
- Letterheads
- Travel itineraries

---

## 10. Company Selector in UI

Directors see all companies.

Normal users only see companies where they have **roles assigned**.

UI displays:

```
[Cephas ISP] [Cephas Trading] [Kingsman] [Menorah]
```

Switching company:
- Reloads modules
- Reloads permissions
- Reloads rate cards
- Reloads branding

---

## 11. Data Isolation Rules

### Hard rule:
> No cross-company data mixing.

Orders of Cephas Trading cannot appear in Cephas ISP.

Each company has its own:

- Orders
- SI list
- Inventory
- Billing records
- Invoices
- RMA workflows
- Payroll
- P&L

Shared database OK → but companyId is mandatory in all tables.

---

## 12. API Specification (Documentation Only)

```
GET  /api/companies
POST /api/companies
GET  /api/companies/{id}
PUT  /api/companies/{id}
GET  /api/companies/{id}/modules
PUT  /api/companies/{id}/modules
GET  /api/companies/{id}/branding
PUT  /api/companies/{id}/branding
```

---

## 13. Summary

This module ensures CephasOps can scale to:

- Multiple companies
- Multiple verticals
- Multiple partner ecosystems
- Independent branding / tax / payroll / templates / inventory

All without modifying the core system.

Ready for Cursor to implement.

# Reference Data Taxonomy (Settings)

**Related:** [Order lifecycle and statuses](order_lifecycle_and_statuses.md) | [Department & RBAC](department_rbac.md) | [Process flows](process_flows.md)

**Source of truth:** docs/_source/Codebase_Summary_SourceOfTruth.md; docs/_source/Business_Processes_SourceOfTruth.md.

---

## Governance

> **Part A** reflects system truth extracted from code and seeds.  
> **Part B** contains recommendations only and does not represent current system behaviour.

---

## Part A — Implemented Reference Data (Authoritative)

Values below are taken only from backend code and DB seed scripts. No values are invented.

---

### Order Types (Implemented)

| Value | Code | Source | Notes |
|-------|------|--------|-------|
| Activation | ACTIVATION | backend/src/CephasOps.Infrastructure/Persistence/Migrations/20250106_SeedAllReferenceData.sql; DatabaseSeeder.cs (SeedDefaultOrderTypesAsync) | New installation + activation of service |
| Modification Indoor | MODIFICATION_INDOOR | Same | Indoor modification of existing service |
| Modification Outdoor | MODIFICATION_OUTDOOR | Same | Outdoor modification of existing service |
| Assurance | ASSURANCE | Same | Fault repair and troubleshooting |
| Value Added Service | VALUE_ADDED_SERVICE | Same | Additional services beyond standard installation/repair |

---

### Order Categories (Implemented)

| Value | Code | Source | Notes |
|-------|------|--------|-------|
| FTTH | FTTH | backend/src/CephasOps.Infrastructure/Persistence/Migrations/20250106_SeedAllReferenceData.sql; DatabaseSeeder.cs (SeedDefaultOrderCategoriesAsync) | Fibre to the Home |
| FTTO | FTTO | Same | Fibre to the Office |
| FTTR | FTTR | Same | Fibre to the Room |
| FTTC | FTTC | Same | Fibre to the Curb |

**Note:** The API route `api/installation-types` serves Order Categories (InstallationTypesController uses OrderCategoryService). Domain entity was historically named InstallationType; renamed to OrderCategory.

---

### Building Types (Implemented)

| Value | Code | Source | Notes |
|-------|------|--------|-------|
| Condominium | CONDO | 20250106_SeedAllReferenceData.sql; DatabaseSeeder.cs (SeedDefaultBuildingTypesAsync) | High-rise residential |
| Apartment | APARTMENT | Same | Multi-unit residential |
| Service Apartment | SERVICE_APT | Same | Serviced residential units |
| Flat | FLAT | Same | Low-rise residential units |
| Terrace House | TERRACE | Same | Row houses |
| Semi-Detached | SEMI_DETACHED | Same | Semi-detached houses |
| Bungalow | BUNGALOW | Same | Single-story detached house |
| Townhouse | TOWNHOUSE | Same | Multi-story attached houses |
| Office Tower | OFFICE_TOWER | Same | High-rise office building |
| Office Building | OFFICE | Same | Low to mid-rise office building |
| Shop Office | SHOP_OFFICE | Same | Mixed shop and office building |
| Shopping Mall | MALL | Same | Retail shopping complex |
| Hotel | HOTEL | Same | Hotel or resort building |
| Mixed Development | MIXED | Same | Mixed residential and commercial |
| Industrial | INDUSTRIAL | Same | Industrial or warehouse building |
| Warehouse | WAREHOUSE | Same | Storage or warehouse facility |
| Educational | EDUCATIONAL | Same | School or educational institution |
| Government | GOVERNMENT | Same | Government building |
| Other | OTHER | Same | Other building type |

---

### Departments (Implemented)

| Value | Code | Source | Notes |
|-------|------|--------|-------|
| GPON | GPON | 20250106_SeedAllReferenceData.sql (Step 6); DatabaseSeeder.cs (SeedGponDepartmentAsync) | GPON Operations Department. Only department seeded. |

**Note:** Departments are configurable via Settings; no other fixed values in repo.

---

### Installation Types (Implemented)

**Implementation note:** In code, “Installation Types” are exposed via the **Order Category** entity and API route `api/installation-types`. There is no separate InstallationType table; the controller delegates to OrderCategoryService. The **implemented values** are therefore the same as **Order Categories** (FTTH, FTTO, FTTR, FTTC). See “Order Categories (Implemented)” above.

---

### Installation Categories / Methods (Implemented)

These are the **InstallationMethod** entity values (site conditions: Prelaid, Non-prelaid, SDU/RDF). They affect rates and building classification. **Skills** have a category named “InstallationMethods” (Aerial, Underground, etc.); that is a skill category, not this reference table.

| Value | Code | Source | Notes |
|-------|------|--------|-------|
| Prelaid | PRELAID | backend/src/CephasOps.Infrastructure/Persistence/Migrations/AddInstallationMethodsTable.sql; 20241127_AddDepartmentIdToInstallationMethods.sql | Fibre already laid; tap into existing infrastructure. Category FTTH. |
| Non-prelaid (MDU / old building) | NON_PRELAID | Same | Multi-dwelling / old buildings; full infrastructure build. Category FTTH. |
| SDU / RDF Pole | SDU_RDF | Same | Single dwelling units and pole-based installations. Category FTTH. |

---

### Partners (Implemented)

**Implemented as configurable; no fixed values in repo.** The Partners table and API exist (Settings → Partners); no seed data for Partner rows in 20250106_SeedAllReferenceData.sql or DatabaseSeeder.cs. Partners are created and maintained via UI/API.

**Partner–Category labels (locked rule):** Labels such as **TIME-FTTH**, **TIME-FTTO** are **derived for display only** from `Partner.Code` and `OrderCategory.Code` (e.g. `TIME` + `-` + `FTTH`). They are **not persisted** and there are **no composite partner rows** (e.g. no “TIME-FTTH” row in the Partners table). See [Taxonomy trace: Installation Type, Method, Partners](../architecture/taxonomy_trace_installation_partner.md).

---

## Part B — Suggested / Recommended Reference Data (Not Implemented)

The following are **suggestions only**. They are not present in code or seeds and must not be treated as current system behaviour.

---

### Order Types — Suggested (not implemented)

| Suggested value | Code (example) | Why suggested |
|-----------------|----------------|----------------|
| Termination | TERMINATION | Parser templates reference ORDER_TYPE TERMINATION for TIME termination emails; no matching Order Type is seeded. Needed if termination work orders are created as orders. |
| Relocation | RELOCATION | Parser templates reference RELOCATION for TIME relocation emails; not in seeded Order Types. Supports move-of-service workflows. |
| General | GENERAL | TIME General (Fallback) parser uses OrderTypeCode GENERAL; not in seeded list. Would allow catch-all order type. |

---

### Order Categories — Suggested (not implemented)

| Suggested value | Code (example) | Why suggested |
|----------------|----------------|----------------|
| (None beyond FTTH, FTTO, FTTR, FTTC) | — | Current set aligns with GPON fibre categories. Future CWO/NWO may introduce department-specific categories. |

---

### Building Types — Suggested (not implemented)

| Suggested value | Code (example) | Why suggested |
|----------------|----------------|----------------|
| (None) | — | Implemented list already covers residential, commercial, mixed, industrial, and other. Add only if new building classifications are required by ops or partners. |

---

### Departments — Suggested (not implemented)

| Suggested value | Code (example) | Why suggested |
|----------------|----------------|----------------|
| CWO | CWO | Business docs reference CWO/NWO as future departments; lifecycle doc states CWO/NWO will define their own workflows when activated. |
| NWO | NWO | Same as above; future department for separate workflow and RBAC. |
| Finance | FINANCE | Some docs reference Finance as a department for payment matching and reporting; may be a sub-team or separate department depending on org design. |

---

### Installation Types — Suggested (not implemented)

Installation Types in the system are Order Categories (FTTH, FTTO, FTTR, FTTC). No additional installation-type values suggested beyond those. Any new fibre categories would be added as Order Categories and exposed via the same API.

---

### Installation Categories / Methods — Suggested (not implemented)

| Suggested value | Code (example) | Why suggested |
|----------------|----------------|----------------|
| FTTO-specific method | e.g. FTTO_OFFICE | If FTTO orders require a distinct installation method for rate/billing (e.g. office riser vs home). |
| Hybrid / Mixed | HYBRID | For sites that combine prelaid and non-prelaid segments; ops clarity and rate resolution. |

---

### Partners — Suggested (not implemented)

| Suggested value | Why suggested |
|----------------|----------------|
| TIME | Primary partner in parser templates and business docs (TIME portal, TIME email patterns). Add via Settings when going live. |
| CelcomDigi | Referenced in parser (Celcom HSBB). Add when processing Celcom work orders. |
| U Mobile | Referenced in business/process docs. Add when partnering. |

Partners are not seeded; add all partners via Settings/API based on actual contracts and email/portal configuration.

---

## Summary

- **Part A:** Order Types (5), Order Categories (4), Building Types (19), Departments (1 seeded), Installation Types (= Order Categories, 4), Installation Methods (3), Partners (0 seeded; configurable only).
- **Part B:** Suggested additions for Termination/Relocation/General order types, CWO/NWO/Finance departments, optional installation methods, and TIME/CelcomDigi/U Mobile as partners to be added when required. None of these are in code or seeds.

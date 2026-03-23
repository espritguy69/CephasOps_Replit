# Reference Types and Relationships

**Purpose:** Single place to record and plan all reference/lookup types (departments, building types, order types, order categories, installation methods, splitter types) and how they relate to each other.

**Source of truth:** The authoritative default lists are seeded in `backend/src/CephasOps.Infrastructure/Persistence/DatabaseSeeder.cs`. This document is the planning and record view; when adding or changing seeded types, update both the seeder and this doc.

---

## 1. Types overview

| Type | Description | Seeded by | Settings UI |
|------|-------------|-----------|-------------|
| **Department** | Functional unit (e.g. GPON Operations). Scopes access and owns reference data. | `SeedGponDepartmentAsync` | Settings → Departments |
| **Building type** | Classification of a building (Residential, Commercial, etc.). | `SeedDefaultBuildingTypesAsync` | Settings → Building Types |
| **Order type** | Kind of job (Activation, Modification, Assurance, etc.). | `SeedDefaultOrderTypesAsync` | Settings → Job Types |
| **Order category** | Fibre type (FTTH, FTTO, FTTR, FTTC). | `SeedDefaultOrderCategoriesAsync` | Settings → Order Categories |
| **Installation method** | How installation is done (e.g. Aerial, Underground). Entity exists; no default seed in seeder—manage via API/Settings. | — | Settings → Installation Methods |
| **Splitter type** | Splitter port configuration (1:8, 1:12, 1:32). | `SeedDefaultSplitterTypesAsync` | Settings → Splitter Types |

---

## 2. Default seeded values

### 2.1 Departments

| Name | Code | Description |
|------|------|-------------|
| GPON | GPON | GPON Operations Department |

Additional departments can be created via Settings → Departments.

### 2.2 Building types

| Name | Code | Description |
|------|------|-------------|
| Condominium | CONDO | High-rise residential building |
| Apartment | APARTMENT | Multi-unit residential building |
| Service Apartment | SERVICE_APT | Serviced residential units |
| Flat | FLAT | Low-rise residential units |
| Terrace House | TERRACE | Row houses |
| Semi-Detached | SEMI_DETACHED | Semi-detached houses |
| Bungalow | BUNGALOW | Single-story detached house |
| Townhouse | TOWNHOUSE | Multi-story attached houses |
| Office Tower | OFFICE_TOWER | High-rise office building |
| Office Building | OFFICE | Low to mid-rise office building |
| Shop Office | SHOP_OFFICE | Mixed shop and office building |
| Shopping Mall | MALL | Retail shopping complex |
| Hotel | HOTEL | Hotel or resort building |
| Mixed Development | MIXED | Mixed residential and commercial |
| Industrial | INDUSTRIAL | Industrial or warehouse building |
| Warehouse | WAREHOUSE | Storage or warehouse facility |
| Educational | EDUCATIONAL | School or educational institution |
| Government | GOVERNMENT | Government building |
| Other | OTHER | Other building type |

### 2.3 Order types (job types)

| Name | Code | Description |
|------|------|-------------|
| Activation | ACTIVATION | New installation + activation of service |
| Modification Indoor | MODIFICATION_INDOOR | Indoor modification of existing service |
| Modification Outdoor | MODIFICATION_OUTDOOR | Outdoor modification of existing service |
| Assurance | ASSURANCE | Fault repair and troubleshooting |
| Value Added Service | VALUE_ADDED_SERVICE | Additional services beyond standard installation/repair |

### 2.4 Order categories

| Name | Code | Description |
|------|------|-------------|
| FTTH | FTTH | Fibre to the Home |
| FTTO | FTTO | Fibre to the Office |
| FTTR | FTTR | Fibre to the Room |
| FTTC | FTTC | Fibre to the Curb |

### 2.5 Installation methods

The **InstallationMethod** entity is department-scoped and used on Orders, Buildings, and rate cards. There is **no default seed** in `DatabaseSeeder`; records are created via Settings → Installation Methods (or API). For planning, the Skills seed uses an "InstallationMethods" category with similar concepts:

| Concept (from Skills category) | Code | Description |
|---------------------------------|------|-------------|
| Aerial installation (pole-to-building) | AERIAL_INSTALL | Aerial fiber from pole to building |
| Underground/conduit installation | UNDERGROUND_INSTALL | Underground and conduit-based fiber |
| Indoor cable routing | INDOOR_ROUTING | Routing fiber within buildings |
| Wall penetration and patching | WALL_PENETRATION | Penetrating walls and patching for cable |
| Cable management and labeling | CABLE_MANAGEMENT | Cable management and labeling practices |
| Weatherproofing | WEATHERPROOFING | Weatherproofing outdoor installations |

### 2.6 Splitter types

| Name | Code | TotalPorts | StandbyPortNumber | Description |
|------|------|------------|-------------------|-------------|
| 1:8 | 1_8 | 8 | — | 1:8 Splitter (8 ports) |
| 1:12 | 1_12 | 12 | — | 1:12 Splitter (12 ports) |
| 1:32 | 1_32 | 32 | 32 | 1:32 Splitter (32 ports, port 32 standby) |

---

## 3. How they relate

- **Department**  
  - Is the scope for RBAC and for most reference data.  
  - Owns: Order types, Order categories, Building types, Splitter types, Installation methods (all are department-scoped where applicable).  
  - One company has many departments; each reference type row may have an optional `DepartmentId`.

- **Building**  
  - Has one **Building type** (optional).  
  - May have one **Installation method** (optional).  
  - Belongs to a company (and optionally department for scoping).

- **Order**  
  - Has one **Order type** (required).  
  - Has one **Order category** (optional).  
  - Has one **Installation method** (optional; site condition / prelaid vs non-prelaid etc.).  
  - Links to a **Building** (which has Building type and optionally Installation method).  
  - Department is resolved from context (e.g. assigned SI or default department) for scoping.

- **Rates (billing, payroll)**  
  - Partner job rates and SI rate plans can be keyed by Order type, Order category, and Installation method.  
  - Building type is used in some rate or matching logic where relevant.

```
Company
  └── Department (e.g. GPON)
        ├── Order types (Activation, Modification Indoor/Outdoor, Assurance, Value Added Service)
        ├── Order categories (FTTH, FTTO, FTTR, FTTC)
        ├── Building types (Condominium, Apartment, …)
        ├── Installation methods (managed via Settings; no default seed)
        └── Splitter types (1:8, 1:12, 1:32)

Order ──► Order type (required)
     ──► Order category (optional)
     ──► Installation method (optional)
     ──► Building ──► Building type (optional)
                  ──► Installation method (optional)
```

---

## 4. Planning: adding or changing types

1. **Backend:** Add or adjust the corresponding seed method in `DatabaseSeeder.cs` (for types that are seeded). For Installation methods, use the Settings API or UI (no seeder).
2. **Migrations:** If the entity schema changes, add a migration as usual.
3. **This doc:** Update the relevant table in §2 and any relationship note in §3 so the doc stays the single record for planning and reference.

---

## 5. See also

- [Department module](../02_modules/department/OVERVIEW.md) — Department purpose and scope
- [Department filtering](../02_modules/department/FILTERING.md) — How department scope is applied
- [Orders overview](../02_modules/orders/OVERVIEW.md) — Order lifecycle and order types in use
- [RBAC (department scope)](../RBAC_MATRIX_REPORT.md) — Which endpoints are department-scoped
- [Data model index](./DATA_MODEL_INDEX.md) — Full schema and entity list

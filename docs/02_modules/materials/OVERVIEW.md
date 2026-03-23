# DEPARTMENT_MATERIAL_ALLOCATIONS_MODULE.md

## Department Material Allocations Module – Functional Specification

This module defines how **material costs and revenue are allocated to departments and cost centres** within each company. It bridges the gap between:

- Global `MaterialTemplate` (what the material is)
- `CompanyMaterialRate` (company-specific pricing)
- `Department` (which team owns the work)
- Cost centre codes (for P&L and financial reporting)

---

## 1. Purpose

When materials are used in orders, the system needs to:

- Determine which **department** owns the material cost
- Assign a **cost centre code** for financial reporting
- **Split allocations** when multiple departments share responsibility for a material

The `DepartmentMaterialCostCenter` entity provides this mapping, enabling accurate P&L reporting at the department and cost centre level.

---

## 2. Scope

### Included

- Maintaining per-department cost centre mappings per material template
- Validating allocation percentages (must total 100% when multiple active rows exist)
- Providing department allocation DTOs for P&L and Billing services
- Integrating with GlobalSettings (CostCenterStrictMode, DefaultCostCenterCode)
- Resolving cost centres with fallback chain: DepartmentMaterialCostCenter → CompanyMaterialRate → GlobalSetting

### Not Included

- Material pricing (belongs to Company Material Rates module)
- Department definitions (belongs to Department module)
- Material template definitions (belongs to Material Templates module)
- UI layout (belongs in `07_frontend/ui`)

---

## 3. Key Concepts

### 3.1 DepartmentMaterialCostCenter

Maps `(CompanyId, DepartmentId, MaterialTemplateId)` to:

- `CostCenterCode` (e.g. `ISP_OPS`, `WAREHOUSE`, `FINANCE`)
- `AllocationPercent` (0–100, or null for 100%)

**Example:**

For material template `ONT001` in company `Cephas`:

- Department `OPS`: 70% → Cost Centre `ISP_OPS`
- Department `WAREHOUSE`: 30% → Cost Centre `WAREHOUSE`

When an order in `OPS` department uses `ONT001`, 70% of the cost goes to `ISP_OPS` cost centre, 30% to `WAREHOUSE`.

### 3.2 Allocation Rules

1. **Single Department:**
   - One row with `AllocationPercent = null` → treated as 100%
   - One row with `AllocationPercent = 100` → 100% to that department

2. **Multiple Departments:**
   - Multiple rows must have `AllocationPercent` values that sum to 100% (±0.01 tolerance)
   - If any row has `AllocationPercent = null`, it's treated as 100% and others are ignored

3. **No Mapping:**
   - Falls back to `CompanyMaterialRate.DefaultCostCenterCode` (if present)
   - Falls back to `GlobalSetting.DefaultCostCenterCode` (if present)
   - If `CostCenterStrictMode = true` and still no mapping → throw configuration error
   - If `CostCenterStrictMode = false` → allow unassigned but log warning

---

## 4. Responsibilities

### 4.1 CRUD for Department Material Cost Centres

- Create/Update/Delete `DepartmentMaterialCostCenter` for `(CompanyId, DepartmentId, MaterialTemplateId)`
- Validate allocation percentages sum to 100% when multiple rows exist
- Ensure no duplicate active rows for same `(CompanyId, DepartmentId, MaterialTemplateId)`
- Soft-delete (IsActive = false) for historical tracking

### 4.2 Cost Centre Resolution

Given:
- `companyId`
- `materialTemplateId`
- `departmentId` (from order's `currentDepartmentId`)

Resolve:
- `CostCenterCode`
- `AllocationPercent`

**Resolution Order:**

1. Look up `DepartmentMaterialCostCenter` for exact match: `(CompanyId, DepartmentId, MaterialTemplateId, IsActive = true)`
2. If found, return that cost centre and allocation percent
3. If multiple rows exist for same `(CompanyId, MaterialTemplateId)` but different `DepartmentId`:
   - Return all allocations (for proportional splitting)
   - Validate sum of percentages ≈ 100%
4. If no mapping found:
   - Check `CompanyMaterialRate.DefaultCostCenterCode` for `(CompanyId, MaterialTemplateId)`
   - If present, use `departmentId` (or null if not applicable) with that cost centre and 100%
5. If still not found:
   - Check `GlobalSetting.DefaultCostCenterCode`
   - If present, use `departmentId` (or null) with that cost centre and 100%
6. If still not found:
   - If `CostCenterStrictMode = true` → throw configuration error
   - If `CostCenterStrictMode = false` → return null cost centre, log warning

### 4.3 Validation Rules

- `CostCenterCode` must not be empty
- `AllocationPercent` must be between 0 and 100 (or null)
- When multiple rows exist for same `(CompanyId, MaterialTemplateId)`:
  - Sum of `AllocationPercent` must be 100 (±0.01 tolerance)
  - No duplicate `DepartmentId` values
- Unique constraint: `(CompanyId, DepartmentId, MaterialTemplateId, IsActive = true)`

---

## 5. Integration with Other Modules

### 5.1 P&L Module

**PnlOrderDetailBuilder** uses `DepartmentMaterialCostCenter` to:

- Allocate material costs to correct cost centres
- Split costs proportionally when multiple departments share materials
- Set `CostCentreId` on `PnlOrderDetail` records

**Example Flow:**

1. Order has `currentDepartmentId = OPS`
2. Order uses material template `ONT001`
3. Lookup `DepartmentMaterialCostCenter` for `(Company, OPS, ONT001)`
4. Find: 70% → `ISP_OPS`, 30% → `WAREHOUSE`
5. Material cost = RM 280
6. Allocate: RM 196 to `ISP_OPS`, RM 84 to `WAREHOUSE`
7. Create `PnlOrderDetail` records (or aggregate) with correct cost centre

### 5.2 Billing Module

**BillingService** (optionally) uses `DepartmentMaterialCostCenter` to:

- Attach cost centre codes to invoice lines
- Track revenue by department and cost centre
- Support department-level billing reports

### 5.3 Department Module

- Provides department list for cost centre mapping UI
- Validates `departmentId` exists and belongs to `companyId`
- Ensures department is active before allowing cost centre assignment

### 5.4 Company Material Rates Module

- `CompanyMaterialRate.DefaultCostCenterCode` acts as fallback
- Used when no `DepartmentMaterialCostCenter` mapping exists
- Provides company-level default cost centre per material

---

## 6. Backend Services

### 6.1 DepartmentMaterialCostCenterService

**Responsibilities:**

- CRUD operations for cost centre mappings
- Validation of allocation percentages
- Cost centre resolution with fallback chain

**Example Methods:**

```csharp
Task<IReadOnlyList<DepartmentMaterialCostCenterDto>> GetForMaterialAsync(
    Guid companyId,
    Guid materialTemplateId,
    CancellationToken cancellationToken = default);

Task UpsertForMaterialAsync(
    UpsertDepartmentMaterialCostCentersCommand command,
    CancellationToken cancellationToken = default);

Task<DepartmentCostCenterAllocation?> ResolveCostCenterAsync(
    Guid companyId,
    Guid materialTemplateId,
    Guid? departmentId,
    CancellationToken cancellationToken = default);
```

### 6.2 DepartmentCostCenterResolver (Utility)

**Responsibilities:**

- Implements the full resolution chain
- Handles fallback logic
- Integrates with GlobalSettingsService

**Usage:**

Called by P&L builder, billing service, and other finance modules when they need to determine cost centre for a material.

---

## 7. API Endpoints

**Base path:** `/api/companies/{companyId}/materials/{materialTemplateId}/department-cost-centers`

### 7.1 Get Allocations

```
GET /api/companies/{companyId}/materials/{materialTemplateId}/department-cost-centers
```

**Response:**

```json
[
  {
    "departmentId": "guid",
    "departmentCode": "OPS",
    "departmentName": "Operations",
    "costCenterCode": "ISP_OPS",
    "allocationPercent": 70.0,
    "isActive": true,
    "notes": null
  },
  {
    "departmentId": "guid",
    "departmentCode": "WH",
    "departmentName": "Warehouse",
    "costCenterCode": "WAREHOUSE",
    "allocationPercent": 30.0,
    "isActive": true,
    "notes": null
  }
]
```

### 7.2 Upsert Allocations

```
PUT /api/companies/{companyId}/materials/{materialTemplateId}/department-cost-centers
```

**Request Body:**

```json
[
  {
    "departmentId": "guid",
    "costCenterCode": "ISP_OPS",
    "allocationPercent": 70.0,
    "isActive": true,
    "notes": "Primary allocation to OPS"
  },
  {
    "departmentId": "guid",
    "costCenterCode": "WAREHOUSE",
    "allocationPercent": 30.0,
    "isActive": true,
    "notes": "Secondary allocation to Warehouse"
  }
]
```

**Validation:**

- No duplicate `departmentId` values
- If multiple rows, sum of `allocationPercent` must be 100 (±0.01)
- `costCenterCode` must not be empty

---

## 8. Error Handling & Logging

**Missing Cost Centre:**

- Logged with `companyId`, `materialTemplateId`, `departmentId`
- If `CostCenterStrictMode = true`, operation fails with configuration error
- If `CostCenterStrictMode = false`, warning logged, operation continues with null cost centre

**Invalid Allocation Percentages:**

- Validation error returned immediately
- Operation fails before database write

**Conflicting Definitions:**

- Unique constraint prevents duplicate active rows
- If violation occurs, operation fails with clear error message

---

## 9. Dependencies

**Depends On:**

- **Department Module** – for department validation and lists
- **Material Templates Module** – for material template validation
- **Company Material Rates Module** – for fallback `DefaultCostCenterCode`
- **Global Settings Module** – for `DefaultCostCenterCode` and `CostCenterStrictMode`
- **P&L Module** – for consumption of cost centre allocations
- **Billing Module** – (optional) for revenue allocation

---

## 10. Non-Goals

This module does **not**:

- Define material pricing (Company Material Rates module)
- Define departments (Department module)
- Define cost centre master data (Settings/Finance module)
- Calculate P&L (P&L module)
- Generate invoices (Billing module)

**Its purpose is:**

> To provide a clean mapping layer that connects materials, departments, and cost centres, enabling accurate financial allocation and reporting.

---

## 11. Summary

- `DepartmentMaterialCostCenter` maps materials to cost centres per department
- Allocation percentages enable proportional cost/revenue splitting
- Resolution chain provides safe fallbacks when mappings are missing
- Integration with P&L and Billing ensures accurate financial reporting
- Validation ensures data integrity and prevents configuration errors


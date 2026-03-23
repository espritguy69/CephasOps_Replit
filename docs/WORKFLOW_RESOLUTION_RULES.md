# Workflow Resolution Rules (Locked)

**Date:** 2026-03-08  
**Purpose:** Canonical specification for how the effective workflow definition is resolved. Used by `WorkflowDefinitionsService.GetEffectiveWorkflowDefinitionAsync` and all Order workflow callers.

---

## Resolution priority

The effective workflow is the **first** match in this order:

1. **Partner-specific:** `EntityType` + `CompanyId` + `PartnerId` (match) + `IsActive`
2. **Department-specific:** `EntityType` + `CompanyId` + `DepartmentId` (match), with `PartnerId == null` and `IsActive`
3. **Order-type-specific:** `EntityType` + `CompanyId` + `OrderTypeCode` (match), with `PartnerId == null`, `DepartmentId == null`, and `IsActive`
4. **General:** `EntityType` + `CompanyId` + `PartnerId == null` + `DepartmentId == null` + `OrderTypeCode == null` + `IsActive`

Only **one** active workflow must exist per exact scope. Ambiguous active definitions for the same scope are invalid and must be rejected (validation or clear error).

---

## Context for Orders

For `EntityType = "Order"`:

- **PartnerId:** from `Order.PartnerId`
- **DepartmentId:** from `Order.DepartmentId`
- **OrderTypeCode:** from the order’s OrderType:
  - If the selected OrderType has a **parent** (`ParentOrderTypeId != null`), use the **parent’s** `Code` (e.g. MODIFICATION_OUTDOOR → `"MODIFICATION"`, STANDARD → `"ASSURANCE"`).
  - Otherwise use the OrderType’s own `Code` (e.g. ACTIVATION → `"ACTIVATION"`).

Examples:

| Order’s OrderType (leaf) | Resolved OrderTypeCode |
|--------------------------|------------------------|
| MODIFICATION_OUTDOOR     | MODIFICATION           |
| INDOOR                   | MODIFICATION           |
| STANDARD / REPULL        | ASSURANCE              |
| ACTIVATION               | ACTIVATION             |
| VALUE_ADDED_SERVICE (or subtype) | VALUE_ADDED_SERVICE (or parent code) |

---

## Backward compatibility

- New parameters and columns are **nullable**. Existing workflows with `PartnerId = null`, `DepartmentId = null`, and `OrderTypeCode = null` remain valid and act as the **general** fallback.
- Callers that do not pass `departmentId` or `orderTypeCode` still resolve correctly: the engine resolves them from the Order when `EntityType = "Order"`.

---

## Database constraint

A unique partial index on `WorkflowDefinitions` enforces at most one active workflow per scope at the database level: `(CompanyId, EntityType, COALESCE(PartnerId::text, ''), COALESCE(DepartmentId::text, ''), COALESCE(OrderTypeCode, '')) WHERE IsActive = true`. This prevents duplicate active scopes even from concurrent inserts or other application paths.

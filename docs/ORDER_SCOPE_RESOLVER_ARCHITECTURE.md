# Order Scope Resolver — Architecture and Refactor Plan

**Date:** 2026-03-08  
**Goal:** Centralize resolution of order scope context (PartnerId, DepartmentId, OrderTypeCode) via a shared `IEffectiveScopeResolver` and refactor `WorkflowEngineService` to use it. No rate resolution changes, no schema changes, full backward compatibility.

**Implementation status:** Done. `EffectiveScopeResolver` implemented, `WorkflowEngineService` refactored to use `IEffectiveScopeResolver`, DI registered in `Program.cs`. The three private resolve methods were removed from `WorkflowEngineService`.

**See also:** CephasOps now has a shared **order pricing context** resolver (`IOrderPricingContextResolver` / `OrderPricingContext`) that centralizes derivation of all pricing-driving fields from an Order (PartnerId, DepartmentId, OrderTypeId, OrderTypeCode, ParentOrderTypeCode, OrderCategoryId, InstallationMethodId, PartnerGroupId). Workflow engine uses it for Order entities. See `docs/ORDER_PRICING_CONTEXT_AUDIT.md`.

---

## 1. Where scope is currently resolved

### 1.1 WorkflowEngineService

**File:** `CephasOps.Application/Workflow/Services/WorkflowEngineService.cs`

**Logic (three private methods):**

- **PartnerId:** When `entityType` is `"Order"`, loads `Orders` by `entityId`, returns `order.PartnerId`. Otherwise null.
- **DepartmentId:** When `entityType` is `"Order"`, loads `Orders` by `entityId`, returns `order.DepartmentId`. Otherwise null.
- **OrderTypeCode:** When `entityType` is `"Order"`, loads `Orders` → `OrderTypeId`, then loads `OrderTypes.Include(ParentOrderType)`; returns `ParentOrderType.Code` when subtype, else `OrderType.Code`.

**Used in:**

- `ExecuteTransitionAsync` — DTO may supply scope; otherwise resolved from entity (lines 59–61).
- `GetAllowedTransitionsAsync` — resolved from entity (198–200).
- `CanTransitionAsync` — resolved from entity (232–234).

**Queries:** Up to four DB round-trips per call (Order for PartnerId, Order for DepartmentId, Order for OrderTypeId, OrderType with Parent).

---

### 1.2 OrderService

**File:** `CephasOps.Application/Orders/Services/OrderService.cs`

**Logic:**

- **PartnerId / DepartmentId:** Taken directly from the order entity already loaded (`orderEntity.PartnerId`, `orderEntity.DepartmentId`). No separate resolution.
- **OrderTypeCode:** Private method `ResolveOrderTypeCodeForWorkflowAsync(Guid orderTypeId)` — loads `OrderTypes.Include(ParentOrderType)`, returns parent code or own `Code`. Same rule as WorkflowEngineService.

**Used in:** `ApplyStatusChangeAsync` when building workflow context and calling `GetEffectiveWorkflowDefinitionAsync` and `ExecuteTransitionAsync` with a DTO that already has PartnerId, DepartmentId, OrderTypeCode set (so WorkflowEngineService does not resolve again when DTO is provided).

**Duplicate:** Only the **OrderTypeCode (parent code)** logic is duplicated between WorkflowEngineService and OrderService.

---

### 1.3 BillingService

**File:** `CephasOps.Application/Billing/Services/BillingService.cs`

**Logic:** Loads order with `Include(Partner)`, `Include(OrderCategory)`. Uses `order.PartnerId`, `order.DepartmentId`, `order.OrderTypeId` directly. Does **not** resolve OrderTypeCode (parent); uses OrderTypeId for BillingRatecard match.

**Conclusion:** No shared “scope resolver” call needed for billing in this change.

---

### 1.4 RateEngineService

**File:** `CephasOps.Application/Rates/Services/RateEngineService.cs`

**Logic:** Does not load orders. Callers (e.g. PayrollService) build `GponRateResolutionRequest` from the order they already have (`order.PartnerId`, `partner?.GroupId`, `order.OrderTypeId`, etc.).

**Conclusion:** No change to rate resolution; no use of IEffectiveScopeResolver in this deliverable.

---

## 2. Common logic to centralize

| Output           | Source                    | Rule                                                                 |
|-----------------|---------------------------|----------------------------------------------------------------------|
| **PartnerId**   | Order                     | `order.PartnerId`                                                    |
| **DepartmentId**| Order                     | `order.DepartmentId`                                                 |
| **OrderTypeCode** | Order.OrderTypeId → OrderType | Parent’s `Code` when `ParentOrderTypeId` set and parent loaded, else own `Code` |

OrderTypeCode rule (from `WORKFLOW_RESOLUTION_RULES.md`): for workflow scope use the **parent** order type code when the selected type is a subtype (e.g. MODIFICATION_OUTDOOR → `"MODIFICATION"`); otherwise use the order type’s own `Code`.

---

## 3. Proposed interface and DTO

### 3.1 EffectiveOrderScope (DTO)

```csharp
namespace CephasOps.Application.Common.DTOs;

/// <summary>
/// Resolved order scope context for workflow and other scope-based resolution.
/// Aligns with WORKFLOW_RESOLUTION_RULES.md: Partner → Department → Order Type → General.
/// </summary>
public class EffectiveOrderScope
{
    public Guid? PartnerId { get; set; }
    public Guid? DepartmentId { get; set; }
    /// <summary>Parent order type code when subtype (e.g. MODIFICATION), else own Code (e.g. ACTIVATION).</summary>
    public string? OrderTypeCode { get; set; }
}
```

### 3.2 IEffectiveScopeResolver

```csharp
namespace CephasOps.Application.Common.Interfaces;

/// <summary>
/// Resolves order scope context (PartnerId, DepartmentId, OrderTypeCode) for use in
/// workflow resolution and other scope-based lookups. OrderTypeCode uses parent code when subtype.
/// </summary>
public interface IEffectiveScopeResolver
{
    /// <summary>
    /// Resolve scope from an entity (e.g. Order). When entityType is "Order", loads the order
    /// and returns PartnerId, DepartmentId, and OrderTypeCode (parent code when subtype).
    /// For non-Order entity types, returns null.
    /// </summary>
    Task<EffectiveOrderScope?> ResolveFromEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve OrderTypeCode for scope: parent's Code when OrderType is a subtype, else own Code.
    /// </summary>
    Task<string?> GetOrderTypeCodeForScopeAsync(Guid orderTypeId, CancellationToken cancellationToken = default);
}
```

- `ResolveFromEntityAsync`: supports WorkflowEngineService (entityType + entityId only).
- `GetOrderTypeCodeForScopeAsync`: allows OrderService (or others) to resolve only OrderTypeCode when they already have the order and only need the code.

---

## 4. Implementation sketch

- **EffectiveScopeResolver** (new class in `CephasOps.Application/Common/Services/` or `Workflow/Services/`):
  - Depends on `ApplicationDbContext`.
  - **ResolveFromEntityAsync:** if entityType != "Order", return null. Else: one query to load Order by Id projecting `PartnerId`, `DepartmentId`, `OrderTypeId`; if order null return null; then call internal **GetOrderTypeCodeForScopeAsync(orderTypeId)** and return new EffectiveOrderScope { order.PartnerId, order.DepartmentId, orderTypeCode }.
  - **GetOrderTypeCodeForScopeAsync:** load OrderType by orderTypeId with `Include(ot => ot.ParentOrderType)`; return `ParentOrderType?.Code ?? OrderType.Code` (with null checks).

- **Single-query optimization (optional):** For `ResolveFromEntityAsync` we can do one query that joins Order → OrderType → ParentOrderType and projects PartnerId, DepartmentId, and the resolved code in one round-trip. Left as an implementation detail.

---

## 5. Refactor plan

### 5.1 WorkflowEngineService

- Add constructor dependency on `IEffectiveScopeResolver`.
- In `ExecuteTransitionAsync`: if DTO already has PartnerId, DepartmentId, OrderTypeCode, use them; otherwise call `_scopeResolver.ResolveFromEntityAsync(dto.EntityType, dto.EntityId, ct)`. If result is null (non-Order or not found), keep current behaviour (treat as no scope / existing fallback). Otherwise set partnerId, departmentId, orderTypeCode from result.
- In `GetAllowedTransitionsAsync` and `CanTransitionAsync`: replace the three `Resolve*` calls with one `_scopeResolver.ResolveFromEntityAsync(entityType, entityId, ct)`. If null, pass nulls to `GetEffectiveWorkflowDefinitionAsync` (same as today for non-Order).
- Remove private methods: `ResolvePartnerIdForEntityAsync`, `ResolveDepartmentIdForOrderAsync`, `ResolveOrderTypeCodeForOrderAsync`.

### 5.2 OrderService (optional, not in minimal change)

- Can later inject `IEffectiveScopeResolver` and replace `ResolveOrderTypeCodeForWorkflowAsync` with `_scopeResolver.GetOrderTypeCodeForScopeAsync(orderEntity.OrderTypeId, ct)` to avoid duplicate logic. Not required for this deliverable.

### 5.3 BillingService / RateEngineService

- No changes in this deliverable.

### 5.4 DI

- Register `IEffectiveScopeResolver` → `EffectiveScopeResolver` (Scoped) in `Program.cs` before `WorkflowEngineService` registration.

---

## 6. Backward compatibility and safety

- Behaviour for Orders: same PartnerId, DepartmentId, and OrderTypeCode as today; workflow resolution and transitions unchanged.
- Non-Order entity types: `ResolveFromEntityAsync` returns null; callers pass nulls to `GetEffectiveWorkflowDefinitionAsync` as today.
- No database or API contract changes; no change to rate resolution or billing logic.
- New interface and implementation are additive; only WorkflowEngineService is refactored to use them.

---

## 7. Files to add/change (minimal)

| Action | File |
|--------|------|
| Add | `CephasOps.Application/Common/DTOs/EffectiveOrderScope.cs` |
| Add | `CephasOps.Application/Common/Interfaces/IEffectiveScopeResolver.cs` |
| Add | `CephasOps.Application/Common/Services/EffectiveScopeResolver.cs` |
| Edit | `CephasOps.Application/Workflow/Services/WorkflowEngineService.cs` (use resolver; remove three private methods) |
| Edit | `CephasOps.Api/Program.cs` (register `IEffectiveScopeResolver` → `EffectiveScopeResolver`) |

No changes to OrderService, BillingService, RateEngineService, or schema in this deliverable.

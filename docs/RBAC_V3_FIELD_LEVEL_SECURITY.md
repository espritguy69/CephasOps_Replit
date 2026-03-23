# RBAC v3 — Field-Level Security

Field-level permission control for sensitive operational data. This allows hiding or protecting specific fields without redesigning the existing RBAC system.

## Overview

- **JWT authentication** is unchanged.
- **Existing RBAC permissions** (module.action) continue to work.
- **Department scope** logic is unchanged.
- **Rollout** is incremental: fields are hidden when the user lacks the corresponding field-level permission; no authorization errors are thrown for hidden fields.

---

## Phase 1 — Sensitive Field Audit

| Module   | Field(s)                                                                 | Current exposure                                                                 | Recommended permission   |
|----------|---------------------------------------------------------------------------|-----------------------------------------------------------------------------------|--------------------------|
| Orders   | RevenueAmount, PayoutAmount, ProfitAmount                                 | Optional on list/detail when `includeProfitability=true`; exposed in API         | `orders.view.price`      |
| Payroll  | TotalAmount (run), TotalPay, Adjustments, NetPay (line), BaseRate, KpiAdjustment, FinalPay (job earning), SI rate plan amounts | All returned to users with `payroll.view`                                         | `payroll.view.payout`    |
| Payroll  | Editing payout amounts, SI rate plans                                     | Requires `payroll.run` or rate plan endpoints                                     | `payroll.edit.payout`    |
| Rates    | BaseWorkRateDto.Amount; GponRateResolutionResult (RevenueAmount, PayoutAmount, margin); Partner/SI rate amounts | Exposed to users with `rates.view` / `rates.edit`                                 | `rates.view.amounts`     |
| Inventory| DefaultCost (MaterialDto)                                                 | Returned on material list/detail to users with `inventory.view`                   | `inventory.view.cost`    |
| Inventory| Editing material cost                                                     | Update material endpoint                                                          | `inventory.edit.cost`    |
| Reports  | Financial columns in report run results (revenue, payout, profit, cost)   | Depends on report definition; export can include financial data                   | `reports.view.financial` |

---

## Field-Level Permissions (module.action.field)

Naming convention: **module.action.field**.

| Permission                | Description                                      |
|---------------------------|--------------------------------------------------|
| `orders.view.price`       | View order revenue, payout, profit on orders     |
| `payroll.view.payout`     | View payroll amounts (run total, line pay, job earnings, SI rate plan amounts) |
| `payroll.edit.payout`     | Edit payroll payout amounts and SI rate plans    |
| `rates.view.amounts`      | View rate amounts (revenue, payout, base work rate) |
| `inventory.view.cost`     | View material default cost                       |
| `inventory.edit.cost`     | Edit material default cost                       |
| `reports.view.financial`  | View financial columns in report run/export      |

---

## Backend Behaviour

### Hiding vs denying

- **Do not** throw authorization errors for missing field-level permissions.
- **Do** remove or mask the field value (e.g. set to `null`) before returning the DTO when the user lacks the permission.

### SuperAdmin

- **SuperAdmin** always sees all fields (bypass applied in the same way as for endpoint permissions).

### Response filtering

- Before returning DTOs, the API calls a **field-level security filter** that:
  1. If the current user is SuperAdmin, returns the DTO unchanged.
  2. Otherwise, checks the user’s permissions (from `IUserPermissionProvider`).
  3. For each sensitive field, if the user does not have the corresponding field-level permission, the field is set to `null` (or omitted) on the response.

Example (conceptual):

```csharp
if (!await _fieldLevelSecurity.HasPermissionAsync(PermissionCatalog.OrdersViewPrice))
{
    dto.RevenueAmount = null;
    dto.PayoutAmount = null;
    dto.ProfitAmount = null;
}
```

Filtering is applied in controllers (or a shared response pipeline) after building the DTO and before returning it.

### Where filtering is applied

- **Orders**: OrderDto (RevenueAmount, PayoutAmount, ProfitAmount) in list and get-by-id when profitability is included.
- **Payroll**: PayrollRunDto (TotalAmount), PayrollLineDto (TotalPay, Adjustments, NetPay), JobEarningRecordDto (BaseRate, KpiAdjustment, FinalPay), SiRatePlanDto (all rate fields).
- **Rates**: BaseWorkRateDto (Amount), GponRateResolutionResult (RevenueAmount, PayoutAmount, GrossMargin, MarginPercentage, etc.), partner/SI rate DTOs (RevenueAmount, PayoutAmount, CustomPayoutAmount).
- **Inventory**: MaterialDto (DefaultCost).
- **Reports**: Report run result columns that are marked as financial (when supported by report definition).

---

## Frontend Behaviour

- The frontend receives the same DTO shape; hidden fields are `null` or omitted.
- Use **user permissions** (from auth/me) to decide whether to show columns or sections:
  - **Orders**: Show “Selling Price” / Revenue / Payout / Profit columns only if the user has `orders.view.price`.
  - **Payroll**: Show “Payout Amount”, “Total Pay”, “Net Pay”, “Rate”, “Final Pay” only if the user has `payroll.view.payout`.
  - **Inventory**: Show “Cost” column only if the user has `inventory.view.cost`.
  - **Rates**: Show amount columns only if the user has `rates.view.amounts`.
  - **Reports**: Show financial columns only if the user has `reports.view.financial`.

If a field is missing from the response (or null), the UI should remain stable (e.g. show “—” or hide the column).

---

## Adding New Field-Level Permissions

1. **Add the constant** to `PermissionCatalog` (e.g. `OrdersViewPrice = "orders.view.price"`) and include it in `AllOrdered` and the appropriate `ByModule` group.
2. **Seed**: New permissions are seeded by `SeedPermissionsAsync` (all `PermissionCatalog.All`). Assign to Admin/SuperAdmin in `SeedRolePermissionsAsync` as needed (e.g. Admin gets `orders.*`, `payroll.*`, etc.).
3. **Backend filtering**: In the controller or filter service, after building the DTO, call the field-level security helper for that entity and permission; set the sensitive property to `null` when the user lacks the permission.
4. **Frontend**: Where the field is displayed, check `user.permissions` for the new permission and hide the column or value when missing.

---

## Safety

- **SuperAdmin** always sees everything (bypass in field-level filter).
- **Admin** defaults include the same module prefixes as today; new field-level permissions under `orders.*`, `payroll.*`, `rates.*`, `inventory.*`, `reports.*` are assigned to Admin by the seeder so existing workflows remain unchanged.
- Existing endpoint-level permissions (e.g. `orders.view`, `payroll.view`) are unchanged; they control access to the endpoint. Field-level permissions only control visibility of specific fields within that response.

---

## Tests

- Fields are hidden when the user does not have the corresponding field-level permission.
- Fields are visible when the user has the permission.
- SuperAdmin bypass: all fields are visible for SuperAdmin regardless of field-level permissions.

# Workflow Resolution Extension Audit (Department & Order Type)

**Date:** 2025-03-08  
**Purpose:** Assess the safest way to extend workflow resolution to support Department and Order Type. **Audit only — no code changes.**

**Implemented (2026-03-08):** The extension has been implemented as recommended. Resolution priority: Partner → Department → OrderType → General. WorkflowDefinition has `OrderTypeCode` (string?, nullable). For Orders, OrderTypeCode is resolved as parent order type code when the selected type is a subtype. See `docs/WORKFLOW_RESOLUTION_RULES.md`, `WorkflowDefinitionsService.GetEffectiveWorkflowDefinitionAsync`, `WorkflowEngineService` (ResolveDepartmentIdForOrderAsync, ResolveOrderTypeCodeForOrderAsync), and tests in `WorkflowDefinitionsServiceResolutionTests.cs`. API GET `/api/workflow-definitions/effective` accepts optional `departmentId`, `orderTypeCode`. Validation prevents multiple active workflows per scope; ambiguous definitions throw.

---

## 1. Current resolution flow (pre-extension)

### 1.1 Call chain

1. **Order status change (e.g. Order Detail / API)**  
   - `OrderService.UpdateOrderStatusAsync` loads order, validates blocker if needed, then:
     - Calls `_workflowDefinitionsService.GetEffectiveWorkflowDefinitionAsync(orderCompanyId, "Order", orderEntity.PartnerId)` to check a workflow exists (throws if null).
     - Builds `ExecuteTransitionDto` with `PartnerId = orderEntity.PartnerId`, calls `_workflowEngineService.ExecuteTransitionAsync(companyId, executeDto, userId)`.

2. **WorkflowEngineService**  
   - **ExecuteTransitionAsync:**  
     - `partnerId = dto.PartnerId ?? await ResolvePartnerIdForEntityAsync(dto.EntityType, dto.EntityId, ct)`.  
     - Calls `_workflowDefinitionsService.GetEffectiveWorkflowDefinitionAsync(companyId, dto.EntityType, partnerId, ct)`.  
     - Then: get current status → find transition → create job → validate guards → run side effects → update status.
   - **GetAllowedTransitionsAsync:**  
     - `partnerId = await ResolvePartnerIdForEntityAsync(entityType, entityId, ct)`.  
     - Calls `GetEffectiveWorkflowDefinitionAsync(companyId, entityType, partnerId, ct)`.
   - **CanTransitionAsync:**  
     - Same: resolve partnerId from entity for Order, then `GetEffectiveWorkflowDefinitionAsync(companyId, entityType, partnerId, ct)`.

3. **WorkflowDefinitionsService.GetEffectiveWorkflowDefinitionAsync**  
   - **Signature:** `(Guid companyId, string entityType, Guid? partnerId = null, CancellationToken ct)`.  
   - **Logic:**  
     - Base query: CompanyId (or no filter if Guid.Empty) + EntityType + IsActive.  
     - If `partnerId.HasValue`: get first where PartnerId == partnerId.  
     - If still null: get first where PartnerId == null.  
     - Return definition or null.

### 1.2 Where resolution is used

| Caller | Method | PartnerId source |
|--------|--------|------------------|
| OrderService | GetEffectiveWorkflowDefinitionAsync | orderEntity.PartnerId (pre-check) |
| OrderService | ExecuteTransitionAsync (via DTO) | executeDto.PartnerId = orderEntity.PartnerId |
| WorkflowEngineService | ExecuteTransitionAsync | dto.PartnerId ?? ResolvePartnerIdForEntityAsync |
| WorkflowEngineService | GetAllowedTransitionsAsync | ResolvePartnerIdForEntityAsync (Order only) |
| WorkflowEngineService | CanTransitionAsync | ResolvePartnerIdForEntityAsync (Order only) |
| WorkflowDefinitionsController | GetEffectiveWorkflowDefinition (GET /effective) | Query param partnerId |
| WorkflowEngineServiceTests | Mocks | It.IsAny<Guid?>() for partner |

### 1.3 Data model today

- **WorkflowDefinition:** CompanyId, EntityType, Name, Description, IsActive, **PartnerId?**, **DepartmentId?**. No OrderTypeId or OrderTypeCode.
- **Order:** OrderTypeId (Guid, leaf), DepartmentId (Guid?), PartnerId (Guid).
- **OrderType:** Id, Code, ParentOrderTypeId, DepartmentId, etc. Parents: ACTIVATION, MODIFICATION, ASSURANCE, VALUE_ADDED_SERVICE. Children e.g. MODIFICATION_INDOOR, MODIFICATION_OUTDOOR.

DepartmentId and Order Type are **not** used in resolution today.

---

## 2. Files that would need change

### 2.1 Backend – resolution and DTOs

| File | Change |
|------|--------|
| `backend/src/CephasOps.Application/Workflow/Services/IWorkflowDefinitionsService.cs` | Extend `GetEffectiveWorkflowDefinitionAsync` with `Guid? departmentId = null` and `string? orderTypeCode = null` (or `Guid? orderTypeId` if using leaf/parent id). |
| `backend/src/CephasOps.Application/Workflow/Services/WorkflowDefinitionsService.cs` | Implement new resolution priority in `GetEffectiveWorkflowDefinitionAsync`: partner → department → order type → general. Add queries for DepartmentId and OrderTypeCode (or OrderTypeId) with correct fallbacks. |
| `backend/src/CephasOps.Application/Workflow/DTOs/WorkflowJobDto.cs` (ExecuteTransitionDto) | Add optional `DepartmentId?` and `OrderTypeCode?` (or `OrderTypeId?`) so callers can pass them. |
| `backend/src/CephasOps.Application/Workflow/Services/WorkflowEngineService.cs` | For Order: resolve DepartmentId and OrderTypeCode (or OrderTypeId) when not on DTO (same pattern as ResolvePartnerIdForEntityAsync). Pass both into `GetEffectiveWorkflowDefinitionAsync` in ExecuteTransitionAsync, GetAllowedTransitionsAsync, CanTransitionAsync. Add helpers e.g. `ResolveDepartmentIdForOrderAsync`, `ResolveOrderTypeCodeForOrderAsync` (or resolve parent code from OrderTypeId). |
| `backend/src/CephasOps.Application/Orders/Services/OrderService.cs` | When calling `GetEffectiveWorkflowDefinitionAsync` and when building ExecuteTransitionDto, pass order’s DepartmentId and order type (code or id) so engine uses same resolution. |

### 2.2 Backend – domain and persistence (if Order Type added to WorkflowDefinition)

| File | Change |
|------|--------|
| `backend/src/CephasOps.Domain/Workflow/Entities/WorkflowDefinition.cs` | Add `OrderTypeCode` (string?) or `OrderTypeId` (Guid?) if workflow definitions are to be scoped by order type. (DepartmentId already exists.) |
| `backend/src/CephasOps.Application/Workflow/DTOs/WorkflowDefinitionDto.cs` | Add OrderTypeCode/OrderTypeId to DTOs and Create/Update DTOs. |
| `backend/src/CephasOps.Infrastructure/Persistence/Configurations/Workflow/WorkflowDefinitionConfiguration.cs` | If new column: property and index. |
| New migration | Add column to WorkflowDefinitions if OrderTypeCode or OrderTypeId is added. |

### 2.3 Backend – other callers of workflow execution

| File | Change |
|------|--------|
| `backend/src/CephasOps.Application/Parser/Services/EmailIngestionService.cs` | When building ExecuteTransitionDto for Order, set DepartmentId and OrderTypeCode (or resolve from order) so resolution stays consistent. |
| `backend/src/CephasOps.Application/Billing/Services/InvoiceSubmissionService.cs` | Same: set DepartmentId and order type on ExecuteTransitionDto when transitioning orders. |
| `backend/src/CephasOps.Application/Scheduler/Services/SchedulerService.cs` | Same for all five ExecuteTransitionDto usages that affect orders. |

### 2.4 API and frontend

| File | Change |
|------|--------|
| `backend/src/CephasOps.Api/Controllers/WorkflowDefinitionsController.cs` | GET `/api/workflow-definitions/effective`: add optional query params e.g. `departmentId`, `orderTypeCode` (or `orderTypeId`) and pass to service. |
| `frontend/src/api/workflowDefinitions.ts` | EffectiveWorkflowParams: add `departmentId?`, `orderTypeCode?` (or `orderTypeId?`). |
| `frontend/src/types/workflowDefinitions.ts` | Same on EffectiveWorkflowParams; WorkflowDefinition/Create/Update types: add orderTypeCode/orderTypeId if backend adds it. |

### 2.5 Tests

| File | Change |
|------|--------|
| `backend/tests/CephasOps.Application.Tests/Workflow/WorkflowEngineServiceTests.cs` | Mocks for `GetEffectiveWorkflowDefinitionAsync`: add `It.IsAny<Guid?>()` for departmentId and `It.IsAny<string?>()` (or Guid?) for order type. New tests for department/order-type resolution and fallback. |

### 2.6 Docs, seeds, settings pages, and APIs to update

| Item | Update |
|------|--------|
| **docs/01_system/WORKFLOW_ENGINE.md** | §2.5.2 Resolution Priority: add department and order-type steps; §2.5.3 implement department in resolution; §2.5.9 add DepartmentId and OrderTypeCode to “resolved at transition time”; resolution example with all four levels. |
| **docs/01_system/WORKFLOW_ENGINE_FLOW.md** | Workflow Activation Resolution diagram: add department and order-type steps; note “Department and Order Type now used in resolution”. |
| **docs/02_modules/workflow/WORKFLOW_ACTIVATION_RULES.md** | §2.1 Resolution Priority: add Priority 2 (department) and 3 (order type), renumber general; §2.2 Logic: implement new parameters and queries; document OrderTypeCode (parent code) and uniqueness. |
| **docs/WORKFLOW_AUDIT_SUMMARY.md** | §3 / §4: state DepartmentId and OrderTypeCode (or parent code) are used in resolution; §7 source of truth; §8 remove “DepartmentId not used” gap. |
| **docs/workflow-seeding-runbook.md** | Optional: how to seed department- or order-type-specific workflows; resolution order reminder. |
| **backend/scripts/postgresql-seeds/07_gpon_order_workflow.sql** | No change if keeping one general workflow; optional: add example of department-scoped workflow for same company. |
| **scripts/deploy/workflow/*.sql** | No change for general seed; optional scripts to add department/order-type definitions. |
| **Frontend: Workflow Definitions page** | `frontend/src/pages/workflow/WorkflowDefinitionsPage.tsx`: form and list filters for DepartmentId and OrderTypeCode (dropdown or code input). |
| **Frontend: types** | `frontend/src/types/workflowDefinitions.ts`: WorkflowDefinition, Create, Update, EffectiveWorkflowParams with departmentId, orderTypeCode. |
| **API: GET /api/workflow-definitions/effective** | Add optional query params departmentId, orderTypeCode; pass to GetEffectiveWorkflowDefinitionAsync. |

### 2.7 Summary

- **No new parameters (department/order type only in engine from entity):**  
  WorkflowDefinitionsService, WorkflowEngineService (resolve from order + pass to service), OrderService (pass from order), EmailIngestionService, InvoiceSubmissionService, SchedulerService, ExecuteTransitionDto, controller effective endpoint, frontend effective API/types, tests.
- **If WorkflowDefinition gains Order Type:**  
  Domain entity, DTOs, EF configuration, one migration, and workflow definitions UI (form + list) to set Order Type.

---

## 3. Recommended future priority

Use this order (most specific to least):

1. **Partner-specific** — EntityType + CompanyId + PartnerId + IsActive (DepartmentId null, OrderType null).
2. **Department-specific** — EntityType + CompanyId + PartnerId = null + DepartmentId + IsActive (OrderType null).
3. **Order-type-specific** — EntityType + CompanyId + PartnerId = null + DepartmentId = null + OrderTypeCode (or OrderTypeId) + IsActive.
4. **General** — EntityType + CompanyId + PartnerId = null + DepartmentId = null + OrderType null + IsActive.

Rationale:

- Partner overrides department and order type (partner-specific process wins).
- Department applies when no partner workflow exists (e.g. GPON vs CWO vs NWO).
- Order type applies when no department workflow exists (e.g. different flow for MODIFICATION vs ACTIVATION).
- General remains the fallback for existing and new deployments.

Optional variant: **partner > department+order type > general** (single “department + order type” step). That would require defining how DepartmentId and OrderType combine (e.g. both must match, or one can be null). The four-step order above is simpler and matches the “partner → department → order type → general” description in the doc.

---

## 4. Leaf type vs parent type (Order Type)

### 4.1 Options

- **Leaf OrderTypeId (Guid):**  
  Match workflow by the order’s exact OrderTypeId (e.g. MODIFICATION_INDOOR, MODIFICATION_OUTDOOR).  
  - Pros: Most precise.  
  - Cons: More workflows to maintain (one per leaf); any new leaf type needs a new workflow or falls back to general.

- **Parent / order-type code (string):**  
  Match by parent Order Type code (e.g. MODIFICATION) or by the order type’s own Code (leaf code).  
  - Pros: Fewer workflows (e.g. one for MODIFICATION for both INDOOR and OUTDOOR); aligns with ApprovalWorkflow/EscalationRule which use `string? OrderType`; stable if leaf codes are renamed.  
  - Cons: Need a single rule for “order type” (e.g. always parent code, or configurable).

### 4.2 Recommendation

Use **order type as a code (string)**, and support **parent code** for resolution:

- Store on WorkflowDefinition: **OrderTypeCode** (string?, e.g. `"MODIFICATION"`, `"ACTIVATION"`).
- At resolution time for an order: from Order.OrderTypeId load OrderType, then take **parent code if ParentOrderTypeId is not null, else own Code**. Use that string in the “order-type-specific” step.
- So one workflow can cover all MODIFICATION (INDOOR/OUTDOOR) by setting OrderTypeCode = `"MODIFICATION"`; optionally allow leaf codes (e.g. `"MODIFICATION_OUTDOOR"`) for finer control.

This keeps workflow definitions small in number, matches existing settings (ApprovalWorkflow, EscalationRule), and avoids tying workflow to Guid FKs that can change.

---

## 5. Risks

### 5.1 Ambiguous workflow matches

- **Multiple active workflows for same scope:**  
  If two definitions tie on (EntityType, CompanyId, PartnerId, DepartmentId, OrderTypeCode), the first returned (e.g. by CreatedAt) wins. Risk of misconfiguration and hard-to-debug behaviour.  
  **Mitigation:** Document “at most one active per (EntityType, PartnerId, DepartmentId, OrderTypeCode)” and consider a unique index or validation on create/update.

### 5.2 Performance

- **Extra queries:**  
  Resolving DepartmentId and OrderTypeCode for Order (when not on DTO) adds one or two lookups (Order + OrderType) per resolution.  
  **Mitigation:** Single query that projects PartnerId, DepartmentId, OrderTypeId; then one OrderType query by id to get (Code, ParentOrderTypeId) or join Order → OrderType and optionally ParentOrderType to get codes. Indexes on Order(Id), OrderType(Id), OrderType(ParentOrderTypeId).

### 5.3 Migration impact

- **Existing DB:**  
  If WorkflowDefinition gets OrderTypeCode (or OrderTypeId), new column is nullable; existing rows stay “general” (PartnerId null, DepartmentId null, OrderType null). No data migration needed for existing workflow definitions.  
- **Seeds:**  
  Current seeds create one “Order Workflow” with no PartnerId/DepartmentId; they do not set OrderTypeCode. After extension, that definition remains the general fallback. New seeds or runbooks can add department/order-type-specific definitions later.

### 5.4 UI / admin complexity

- **Workflow definitions UI:**  
  If DepartmentId and OrderTypeCode are added, the create/edit form and list filters should expose Department and Order Type. Admins must understand resolution order (partner > department > order type > general) to avoid confusion.  
  **Mitigation:** Short help text and docs; optional “effective workflow” preview by (entityType, partnerId, departmentId, orderTypeCode).

### 5.5 Backward compatibility

- **API:**  
  New query params (departmentId, orderTypeCode) and DTO fields (DepartmentId, OrderTypeCode) should be optional. Callers that do not send them get current behaviour (engine resolves from order for Order entity).  
- **Service signature:**  
  Add optional parameters with defaults (e.g. `Guid? departmentId = null`, `string? orderTypeCode = null`) so existing call sites keep working; only Order-related call sites need to pass or resolve department and order type.

---

## 6. Safest implementation plan

1. **Design and docs**  
   - Confirm resolution order (e.g. partner → department → order type → general) and “order type = parent code (with fallback to leaf code)” in WORKFLOW_ENGINE.md and WORKFLOW_ACTIVATION_RULES.md.  
   - Document uniqueness rule and validation for workflow definitions.

2. **Backend – resolution only (no new WorkflowDefinition column)**  
   - Extend `GetEffectiveWorkflowDefinitionAsync` with optional `departmentId` and `orderTypeCode`.  
   - Implement the four-step priority; for “order type” step, filter by OrderTypeCode (e.g. `WorkflowDefinition.OrderTypeCode == orderTypeCode`).  
   - Do **not** add OrderTypeCode to WorkflowDefinition yet; only implement the resolution branch that uses it when `orderTypeCode != null` and no definition has OrderTypeCode in the DB. So this step is “scaffolding” only, or add the column in the same phase (see below).

3. **Backend – WorkflowDefinition and DTOs**  
   - Add `OrderTypeCode` (string?, max length) to WorkflowDefinition entity and DTOs/Create/Update.  
   - Add migration; keep column nullable.  
   - No change to DepartmentId (already on entity and DTOs).

4. **Backend – engine and callers**  
   - Add `ResolveDepartmentIdForOrderAsync` and `ResolveOrderTypeCodeForOrderAsync` (or one method returning (DepartmentId, OrderTypeCode) from Order + OrderType). Use parent code when order type has parent.  
   - ExecuteTransitionDto: add optional DepartmentId and OrderTypeCode.  
   - In ExecuteTransitionAsync, GetAllowedTransitionsAsync, CanTransitionAsync: for Order, resolve department and order type when not on DTO; pass to GetEffectiveWorkflowDefinitionAsync.  
   - OrderService: pass order.DepartmentId and resolved order type code when calling GetEffectiveWorkflowDefinitionAsync and when building ExecuteTransitionDto.  
   - EmailIngestionService, InvoiceSubmissionService, SchedulerService: set DepartmentId and OrderTypeCode on ExecuteTransitionDto from the order.

5. **API and frontend**  
   - GET effective: add optional departmentId, orderTypeCode (or orderTypeId and resolve code in API).  
   - Frontend types and effective API: add same optional params.  
   - Workflow definitions UI: add Department and Order Type (code dropdown or text) to create/edit and filters; optional in first iteration.

6. **Tests and validation**  
   - Unit tests for GetEffectiveWorkflowDefinitionAsync with all four levels and fallbacks.  
   - WorkflowEngineService tests: resolve order type code from order; mock GetEffectiveWorkflowDefinitionAsync with departmentId and orderTypeCode.  
   - Integration test: order with PartnerId + DepartmentId + OrderTypeId → correct workflow by priority.

7. **Seeds and runbooks**  
   - Leave existing seed as-is (general workflow).  
   - Document in workflow-seeding runbook how to add department- or order-type-specific definitions and how resolution order works.

This keeps existing behaviour (general workflow) unchanged, adds resolution in a clear order, and avoids breaking callers or the UI until they are updated to pass or display the new fields.

---

**End of audit.**

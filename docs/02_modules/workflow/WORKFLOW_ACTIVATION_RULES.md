# Workflow Activation Rules

**Last Updated:** 2025-03-08  
**Status:** ✅ Implemented

## Overview

This document describes the rules and logic for activating and resolving workflow definitions in CephasOps. The workflow engine uses a hierarchical resolution system to determine which workflow definition applies to a given entity.

**Order workflow at runtime:** Orders do **not** store WorkflowDefinitionId; workflow is resolved at **transition time**. For Order entities, the engine uses the same **resolution context** (PartnerId, DepartmentId, OrderTypeCode) for execution and for allowed-transitions. Context is taken from `ExecuteTransitionDto` when set by callers, or resolved from the order (and OrderType for OrderTypeCode). **Resolution order:** (1) Partner-specific, (2) Department-specific, (3) OrderType-specific (by **parent** order type code when the selected type is a subtype, e.g. MODIFICATION_OUTDOOR → "MODIFICATION"), (4) General (all null). See `docs/WORKFLOW_RESOLUTION_RULES.md`. Order Category and Installation Method are not used for workflow selection.

---

## 1. Workflow Definition Activation

### 1.1 IsActive Flag

**Entity:** `WorkflowDefinition`

**Field:** `IsActive` (boolean, default: `true`)

**Purpose:** Controls whether a workflow definition is available for use.

**Behavior:**
- When `IsActive = false`, the workflow definition is **not** considered during resolution
- Only active workflows are returned by `GetEffectiveWorkflowDefinitionAsync()`
- Inactive workflows are preserved in the database (for history/audit)

### 1.2 Workflow Transition Activation

**Entity:** `WorkflowTransition`

**Field:** `IsActive` (boolean, default: `true`)

**Purpose:** Controls whether a specific transition within a workflow is allowed.

**Behavior:**
- When `IsActive = false`, the transition is **not** available for execution
- Only active transitions are considered when validating status changes
- Inactive transitions are preserved (for history/audit)

### 1.3 Activation Rules

**Rule 1: Workflow Definition Must Be Active**
```
IF WorkflowDefinition.IsActive = false
THEN WorkflowDefinition is NOT considered during resolution
```

**Rule 2: Transitions Must Be Active**
```
IF WorkflowTransition.IsActive = false
THEN Transition is NOT available for execution
```

**Rule 3: Both Must Be Active**
```
For a transition to be executable:
  - WorkflowDefinition.IsActive = true
  - WorkflowTransition.IsActive = true
```

---

## 2. Workflow Resolution Logic

### 2.1 Resolution Priority

When resolving which workflow definition applies to an entity, the system uses the following priority order (see `docs/WORKFLOW_RESOLUTION_RULES.md`):

**Priority 1: Partner-Specific Workflow**
- Matches: EntityType + CompanyId + PartnerId + IsActive. Only one active workflow per (EntityType, CompanyId, PartnerId, DepartmentId, OrderTypeCode) scope is allowed.

**Priority 2: Department-Specific Workflow** (PartnerId must be null on definition)
- Matches: EntityType + CompanyId + DepartmentId + IsActive, with PartnerId = null.

**Priority 3: Order-Type-Specific Workflow** (PartnerId and DepartmentId null on definition)
- Matches: EntityType + CompanyId + OrderTypeCode + IsActive, with PartnerId = null, DepartmentId = null. For Orders, OrderTypeCode is the **parent** order type code when the selected OrderType is a subtype (e.g. MODIFICATION_OUTDOOR → "MODIFICATION", STANDARD → "ASSURANCE").

**Priority 4: General Workflow**
- Matches: EntityType + CompanyId + PartnerId = null + DepartmentId = null + OrderTypeCode = null + IsActive. Existing workflows with no scope remain valid (backward compatible).

### 2.2 Resolution Method

**Service:** `IWorkflowDefinitionsService`

**Method:** `GetEffectiveWorkflowDefinitionAsync(companyId, entityType, partnerId, departmentId, orderTypeCode, cancellationToken)`

**Implementation:** `WorkflowDefinitionsService.GetEffectiveWorkflowDefinitionAsync()`

**Code Location:** `backend/src/CephasOps.Application/Workflow/Services/WorkflowDefinitionsService.cs`

**Logic:** Resolution order: (1) Partner-specific (EntityType + CompanyId + PartnerId + active), (2) Department-specific (PartnerId null, DepartmentId match), (3) OrderType-specific (PartnerId and DepartmentId null, OrderTypeCode match), (4) General (all null + active). If multiple active definitions exist for the same scope, the service throws. See `docs/WORKFLOW_RESOLUTION_RULES.md` and the method’s XML comments.

### 2.3 Transition Resolution

When executing a workflow transition, the engine uses the same **context** (partnerId, departmentId, orderTypeCode) for resolution as for the initial lookup: for Order entities, `ExecuteTransitionDto` may set PartnerId, DepartmentId, OrderTypeCode; when not provided, the engine resolves them from the order (and OrderType for OrderTypeCode: parent code when subtype). This context is passed to `GetEffectiveWorkflowDefinitionAsync()` and used in GetAllowedTransitionsAsync and CanTransitionAsync so the same workflow is used everywhere.

When executing a workflow transition:

**Service:** `IWorkflowEngineService`

**Method:** `ExecuteTransitionAsync()`

**Code Location:** `backend/src/CephasOps.Application/Workflow/Services/WorkflowEngineService.cs`

**Logic:**
```csharp
// 1. Resolve context (partnerId, departmentId, orderTypeCode from DTO or from Order)
// 2. Get effective workflow definition (priority: Partner → Department → OrderType → General)
var workflowDefinition = await _workflowDefinitionsService
    .GetEffectiveWorkflowDefinitionAsync(companyId, entityType, partnerId, departmentId, orderTypeCode, cancellationToken);

if (workflowDefinition == null)
{
    throw new InvalidOperationException(
        $"No active workflow definition found for entity type '{entityType}'.");
}

// 2. Find allowed transition (must be active)
var transition = workflowDefinition.Transitions
    .FirstOrDefault(t => t.IsActive  // ← Only active transitions
        && (t.FromStatus == null || t.FromStatus == currentStatus)
        && t.ToStatus == dto.TargetStatus);

if (transition == null)
{
    throw new InvalidOperationException(
        $"No allowed transition found from '{currentStatus}' to '{dto.TargetStatus}'.");
}
```

---

## 3. Activation Scenarios

### 3.1 Activating a New Workflow

**Scenario:** Create a new workflow definition for "Order" entity type.

**Steps:**
1. Create `WorkflowDefinition` with `IsActive = true`
2. Create `WorkflowTransition` records with `IsActive = true`
3. System immediately uses this workflow for new orders

**Code:**
```csharp
var workflow = new WorkflowDefinition
{
    Id = Guid.NewGuid(),
    CompanyId = companyId,
    Name = "Standard Order Workflow",
    EntityType = "Order",
    IsActive = true, // ← Active by default
    // ...
};
```

### 3.2 Deactivating a Workflow

**Scenario:** Temporarily disable a workflow without deleting it.

**Steps:**
1. Set `WorkflowDefinition.IsActive = false`
2. System stops using this workflow immediately
3. Existing orders using this workflow continue (workflow is resolved at creation time)

**Code:**
```csharp
workflow.IsActive = false;
workflow.UpdatedAt = DateTime.UtcNow;
await _context.SaveChangesAsync();
```

### 3.3 Deactivating a Specific Transition

**Scenario:** Disable a specific status transition (e.g., "Blocked → Completed" should not be allowed).

**Steps:**
1. Set `WorkflowTransition.IsActive = false`
2. Transition is immediately unavailable
3. Other transitions in the workflow remain active

**Code:**
```csharp
transition.IsActive = false;
transition.UpdatedAt = DateTime.UtcNow;
await _context.SaveChangesAsync();
```

### 3.4 Replacing a Workflow

**Scenario:** Replace an existing workflow with a new version.

**Steps:**
1. Create new `WorkflowDefinition` with `IsActive = true`
2. Set old `WorkflowDefinition.IsActive = false`
3. New orders use the new workflow
4. Old orders continue with their original workflow (workflow is resolved at creation time)

**Best Practice:** Use versioning or effective dates for workflow definitions.

---

## 4. Validation Rules

### 4.1 Workflow Definition Validation

**Rule:** At least one active workflow definition must exist per entity type.

**Validation:**
```csharp
var activeWorkflows = await _context.WorkflowDefinitions
    .Where(wd => wd.CompanyId == companyId
        && wd.EntityType == entityType
        && wd.IsActive)
    .CountAsync();

if (activeWorkflows == 0)
{
    throw new InvalidOperationException(
        $"Cannot deactivate workflow: No other active workflow exists for entity type '{entityType}'.");
}
```

### 4.2 Transition Validation

**Rule:** At least one active transition must exist per workflow definition.

**Validation:**
```csharp
var activeTransitions = workflowDefinition.Transitions
    .Count(t => t.IsActive);

if (activeTransitions == 0)
{
    throw new InvalidOperationException(
        "Cannot deactivate all transitions: At least one active transition is required.");
}
```

### 4.3 Initial State Validation

**Rule:** At least one transition with `FromStatus = null` (initial state) must be active.

**Validation:**
```csharp
var initialTransitions = workflowDefinition.Transitions
    .Where(t => t.IsActive && t.FromStatus == null)
    .Count();

if (initialTransitions == 0)
{
    throw new InvalidOperationException(
        "Cannot deactivate initial transitions: At least one initial transition is required.");
}
```

---

## 5. API Endpoints

### 5.1 Get Workflow Definitions

```http
GET /api/workflow-definitions?entityType=Order&isActive=true
```

**Query Parameters:**
- `entityType` - Filter by entity type
- `isActive` - Filter by active status

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "name": "Standard Order Workflow",
      "entityType": "Order",
      "isActive": true,
      "transitions": [
        {
          "id": "guid",
          "fromStatus": null,
          "toStatus": "Pending",
          "isActive": true
        }
      ]
    }
  ]
}
```

### 5.2 Update Workflow Definition

```http
PUT /api/workflow-definitions/{id}
Content-Type: application/json

{
  "isActive": false
}
```

### 5.3 Update Workflow Transition

```http
PUT /api/workflow-definitions/{id}/transitions/{transitionId}
Content-Type: application/json

{
  "isActive": false
}
```

---

## 6. Best Practices

### 6.1 Workflow Management

- **Test before activating:** Create test workflow, verify transitions work correctly
- **Version workflows:** Use naming convention (e.g., "Order Workflow v2")
- **Document changes:** Use `Description` field to document workflow changes
- **Deactivate, don't delete:** Preserve workflow history by setting `IsActive = false`

### 6.2 Transition Management

- **Gradual rollout:** Deactivate old transitions gradually, activate new ones
- **Monitor usage:** Check which transitions are being used before deactivating
- **Backup plan:** Keep old workflow active until new one is verified

### 6.3 Resolution Strategy

- **Partner-specific first:** Use partner-specific workflows for partner-specific requirements
- **Department-specific:** Use department workflows for department-specific processes
- **General fallback:** Always have a general workflow as fallback

---

## 7. Troubleshooting

### 7.1 "No active workflow definition found"

**Causes:**
1. All workflows for entity type are inactive
2. No workflow exists for entity type
3. CompanyId mismatch

**Solution:**
1. Check `WorkflowDefinitions` table: `SELECT * FROM WorkflowDefinitions WHERE EntityType = 'Order' AND IsActive = true`
2. Activate a workflow or create a new one
3. Verify CompanyId matches

### 7.2 "No allowed transition found"

**Causes:**
1. Transition is inactive (`IsActive = false`)
2. Transition doesn't exist for current → target status
3. Workflow definition is inactive

**Solution:**
1. Check `WorkflowTransitions` table: `SELECT * FROM WorkflowTransitions WHERE WorkflowDefinitionId = @id AND IsActive = true`
2. Verify transition exists: `FromStatus = @current AND ToStatus = @target`
3. Activate transition or create new one

### 7.3 Workflow Not Resolving Correctly

**Causes:**
1. Multiple active workflows with same priority
2. PartnerId/DepartmentId mismatch
3. Resolution order issue

**Solution:**
1. Check resolution priority (partner-specific > general)
2. Verify PartnerId/DepartmentId matches
3. Review `GetEffectiveWorkflowDefinitionAsync()` logic

---

## 8. Related Documentation

- `docs/01_system/WORKFLOW_ENGINE.md` - Workflow engine overview
- `docs/02_modules/workflow/OVERVIEW.md` - Workflow module overview
- `docs/05_data_model/entities/workflow_entities.md` - Workflow entities documentation

---

## 9. Code References

**Service Implementation:**
- `backend/src/CephasOps.Application/Workflow/Services/WorkflowDefinitionsService.cs`
- `backend/src/CephasOps.Application/Workflow/Services/WorkflowEngineService.cs`

**Entity Definitions:**
- `backend/src/CephasOps.Domain/Workflow/Entities/WorkflowDefinition.cs`
- `backend/src/CephasOps.Domain/Workflow/Entities/WorkflowTransition.cs`

**API Controllers:**
- `backend/src/CephasOps.Api/Controllers/WorkflowDefinitionsController.cs`

---

**End of Document**


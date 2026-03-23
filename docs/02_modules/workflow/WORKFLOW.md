# Workflow Engine – System Workflow Diagram

**Date:** December 12, 2025  
**Purpose:** End-to-end workflow representation for the Workflow Engine module, covering workflow definitions, transitions, guard conditions, and side effects

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      WORKFLOW ENGINE SYSTEM                              │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   WORKFLOW DEFINITIONS  │      │   TRANSITION EXECUTION │
        │  (Configuration)        │      │  (State Changes)       │
        ├───────────────────────┤      ├───────────────────────┤
        │ • Define Workflows      │      │ • Validate Guards       │
        │ • Define Transitions    │      │ • Execute Side Effects  │
        │ • Define Guard Conditions│     │ • Update Entity Status  │
        │ • Define Side Effects   │      │ • Create Workflow Jobs  │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   GUARD VALIDATORS      │      │   SIDE EFFECT EXECUTORS│
        │  (Validation Registry)  │      │  (Action Registry)     │
        └───────────────────────┘      └───────────────────────┘
```

---

## Complete Workflow: Transition Execution

```
[STEP 1: TRANSITION REQUEST]
         |
         v
┌────────────────────────────────────────┐
│ EXECUTE TRANSITION REQUEST                │
│ POST /api/workflow/execute                │
└────────────────────────────────────────┘
         |
         v
ExecuteTransitionDto {
  EntityType: "Order"
  EntityId: "order-456"
  TargetStatus: "Assigned"
  Payload: {
    assignedSiId: "SI-123"
    appointmentDate: "2025-12-15"
  }
}
         |
         v
┌────────────────────────────────────────┐
│ GET EFFECTIVE WORKFLOW DEFINITION         │
│ WorkflowDefinitionsService.GetEffectiveWorkflowDefinitionAsync()│
└────────────────────────────────────────┘
         |
         v
[Resolution Priority]
  1. Partner-specific workflow
  2. Default workflow (no partner)
         |
         v
WorkflowDefinition {
  Id: "workflow-123"
  EntityType: "Order"
  CompanyId: Cephas
  PartnerId: TIME (optional)
  Transitions: [
    {
      FromStatus: "Pending"
      ToStatus: "Assigned"
      GuardConditions: [...]
      SideEffects: [...]
    }
  ]
}
         |
         v
[STEP 2: GET CURRENT ENTITY STATUS]
         |
         v
┌────────────────────────────────────────┐
│ GET CURRENT STATUS                       │
│ WorkflowEngineService.GetCurrentEntityStatusAsync()│
└────────────────────────────────────────┘
         |
         v
[Entity-Specific Status Retrieval]
  For Order:
    Order.find(Id = entityId)
    → Status: "Pending"
         |
         v
[STEP 3: FIND ALLOWED TRANSITION]
         |
         v
┌────────────────────────────────────────┐
│ MATCH TRANSITION                         │
└────────────────────────────────────────┘
         |
         v
[Find Transition]
  Transition.find(
    FromStatus = "Pending" (or null)
    ToStatus = "Assigned"
    IsActive = true
  )
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NOT FOUND]
   |            |
   |            v
   |       [Throw InvalidOperationException]
   |       "No allowed transition found"
   |
   v
[STEP 4: CREATE WORKFLOW JOB]
         |
         v
┌────────────────────────────────────────┐
│ CREATE WORKFLOW JOB                       │
└────────────────────────────────────────┘
         |
         v
WorkflowJob {
  Id: Guid.NewGuid()
  CompanyId: Cephas
  WorkflowDefinitionId: "workflow-123"
  EntityType: "Order"
  EntityId: "order-456"
  CurrentStatus: "Pending"
  TargetStatus: "Assigned"
  State: "Pending"
  PayloadJson: "{ assignedSiId: 'SI-123', ... }"
  InitiatedByUserId: "user-123"
  CreatedAt: DateTime.UtcNow
}
         |
         v
[Save Job]
  _context.WorkflowJobs.Add(job)
  await _context.SaveChangesAsync()
         |
         v
[STEP 5: START JOB EXECUTION]
         |
         v
[Update Job State]
  job.State = "Running"
  job.StartedAt = DateTime.UtcNow
  await _context.SaveChangesAsync()
         |
         v
[STEP 6: VALIDATE GUARD CONDITIONS]
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE GUARD CONDITIONS                 │
│ WorkflowEngineService.ValidateGuardConditionsAsync()│
└────────────────────────────────────────┘
         |
         v
[For each Guard Condition]
  GuardCondition {
    DefinitionId: "guard-123"
    Type: "RequiredFields"
    Configuration: {
      fields: ["AssignedSiId", "AppointmentDate"]
    }
  }
         |
         v
[Resolve Validator from Registry]
  GuardConditionValidatorRegistry.GetValidator("RequiredFields")
         |
         v
[Execute Validator]
  RequiredFieldsValidator.Validate(
    entityType: "Order",
    entityId: "order-456",
    configuration: { fields: [...] }
  )
         |
         v
[Check Required Fields]
  Order {
    AssignedSiId: "SI-123" ✓
    AppointmentDate: 2025-12-15 ✓
  }
         |
    ┌────┴────┐
    |         |
    v         v
[PASS] [FAIL]
   |            |
   |            v
   |       [Throw ValidationException]
   |       "Guard condition failed: RequiredFields"
   |
   v
[All Guards Pass]
         |
         v
[STEP 7: EXECUTE SIDE EFFECTS]
         |
         v
┌────────────────────────────────────────┐
│ EXECUTE SIDE EFFECTS                      │
│ WorkflowEngineService.ExecuteSideEffectsAsync()│
└────────────────────────────────────────┘
         |
         v
[For each Side Effect]
  SideEffect {
    DefinitionId: "sideeffect-123"
    Type: "CreateScheduledSlot"
    Configuration: {
      serviceInstallerId: "{payload.assignedSiId}"
      date: "{payload.appointmentDate}"
    }
  }
         |
         v
[Resolve Executor from Registry]
  SideEffectExecutorRegistry.GetExecutor("CreateScheduledSlot")
         |
         v
[Execute Side Effect]
  CreateScheduledSlotExecutor.Execute(
    entityType: "Order",
    entityId: "order-456",
    payload: { assignedSiId: "SI-123", ... },
    configuration: { ... }
  )
         |
         v
[Create Scheduled Slot]
  ScheduledSlot {
    OrderId: "order-456"
    ServiceInstallerId: "SI-123"
    Date: 2025-12-15
  }
         |
         v
[All Side Effects Executed]
         |
         v
[STEP 8: UPDATE ENTITY STATUS]
         |
         v
┌────────────────────────────────────────┐
│ UPDATE ENTITY STATUS                      │
│ WorkflowEngineService.UpdateEntityStatusAsync()│
└────────────────────────────────────────┘
         |
         v
[Entity-Specific Status Update]
  For Order:
    Order.Status = "Assigned"
    await _context.SaveChangesAsync()
         |
         v
[Create Status Log]
  OrderStatusLog {
    OrderId: "order-456"
    FromStatus: "Pending"
    ToStatus: "Assigned"
    ChangedByUserId: "user-123"
    ChangedAt: DateTime.UtcNow
  }
         |
         v
[STEP 9: SEND NOTIFICATIONS]
         |
         v
[Fire and Forget Notification]
  Task.Run(async () => {
    await _notificationHandler.HandleAsync(
      entityId: "order-456",
      newStatus: "Assigned",
      companyId: Cephas
    )
  })
         |
         v
[STEP 10: COMPLETE JOB]
         |
         v
[Update Job State]
  job.State = "Succeeded"
  job.CompletedAt = DateTime.UtcNow
  await _context.SaveChangesAsync()
         |
         v
[Return Workflow Job DTO]
  WorkflowJobDto {
    Id: job.Id
    State: "Succeeded"
    CurrentStatus: "Pending"
    TargetStatus: "Assigned"
    CompletedAt: DateTime.UtcNow
  }
```

---

## Workflow Definition Creation

```
[STEP 1: CREATE WORKFLOW DEFINITION]
         |
         v
┌────────────────────────────────────────┐
│ CREATE WORKFLOW DEFINITION                 │
│ POST /api/workflow-definitions            │
└────────────────────────────────────────┘
         |
         v
CreateWorkflowDefinitionDto {
  Name: "GPON Order Workflow"
  EntityType: "Order"
  Description: "Standard workflow for GPON orders"
  IsActive: true
  PartnerId: TIME (optional)
  DepartmentId: GPON (optional)
}
         |
         v
┌────────────────────────────────────────┐
│ CREATE DEFINITION RECORD                   │
└────────────────────────────────────────┘
         |
         v
WorkflowDefinition {
  Id: Guid.NewGuid()
  CompanyId: Cephas
  Name: "GPON Order Workflow"
  EntityType: "Order"
  Description: "Standard workflow for GPON orders"
  IsActive: true
  PartnerId: TIME
  DepartmentId: GPON
  CreatedAt: DateTime.UtcNow
  CreatedByUserId: "user-123"
}
         |
         v
[Save Definition]
  _context.WorkflowDefinitions.Add(definition)
  await _context.SaveChangesAsync()
         |
         v
[STEP 2: ADD TRANSITIONS]
         |
         v
┌────────────────────────────────────────┐
│ CREATE TRANSITION                         │
│ POST /api/workflow-definitions/{id}/transitions│
└────────────────────────────────────────┘
         |
         v
CreateTransitionDto {
  FromStatus: "Pending"
  ToStatus: "Assigned"
  DisplayOrder: 1
  AllowedRoles: ["Admin", "Manager"]
  IsActive: true
}
         |
         v
[Create Transition]
  WorkflowTransition {
    Id: Guid.NewGuid()
    WorkflowDefinitionId: definition.Id
    FromStatus: "Pending"
    ToStatus: "Assigned"
    DisplayOrder: 1
    AllowedRoles: ["Admin", "Manager"]
    IsActive: true
  }
         |
         v
[STEP 3: ADD GUARD CONDITIONS]
         |
         v
┌────────────────────────────────────────┐
│ CREATE GUARD CONDITION                    │
│ POST /api/workflow-definitions/{id}/transitions/{transitionId}/guards│
└────────────────────────────────────────┘
         |
         v
CreateGuardConditionDto {
  GuardConditionDefinitionId: "guard-123"
  Configuration: {
    fields: ["AssignedSiId", "AppointmentDate"]
  }
  IsActive: true
}
         |
         v
[Create Guard Condition]
  GuardCondition {
    Id: Guid.NewGuid()
    TransitionId: transition.Id
    GuardConditionDefinitionId: "guard-123"
    ConfigurationJson: "{ fields: [...] }"
    IsActive: true
  }
         |
         v
[STEP 4: ADD SIDE EFFECTS]
         |
         v
┌────────────────────────────────────────┐
│ CREATE SIDE EFFECT                         │
│ POST /api/workflow-definitions/{id}/transitions/{transitionId}/side-effects│
└────────────────────────────────────────┘
         |
         v
CreateSideEffectDto {
  SideEffectDefinitionId: "sideeffect-123"
  Configuration: {
    serviceInstallerId: "{payload.assignedSiId}"
    date: "{payload.appointmentDate}"
  }
  ExecutionOrder: 1
  IsActive: true
}
         |
         v
[Create Side Effect]
  SideEffect {
    Id: Guid.NewGuid()
    TransitionId: transition.Id
    SideEffectDefinitionId: "sideeffect-123"
    ConfigurationJson: "{ ... }"
    ExecutionOrder: 1
    IsActive: true
  }
         |
         v
[Workflow Definition Complete]
```

---

## Guard Condition Validation Flow

```
[Guard Condition Validation]
  GuardCondition {
    Type: "RequiredFields"
    Configuration: {
      fields: ["AssignedSiId", "AppointmentDate"]
    }
  }
         |
         v
┌────────────────────────────────────────┐
│ RESOLVE VALIDATOR                         │
│ GuardConditionValidatorRegistry.GetValidator()│
└────────────────────────────────────────┘
         |
         v
[Get Validator by Type]
  Validator = registry["RequiredFields"]
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NOT FOUND]
   |            |
   |            v
   |       [Throw InvalidOperationException]
   |       "Validator not found: RequiredFields"
   |
   v
┌────────────────────────────────────────┐
│ EXECUTE VALIDATOR                         │
│ RequiredFieldsValidator.Validate()        │
└────────────────────────────────────────┘
         |
         v
[Get Entity]
  Order = await _context.Orders.FindAsync(entityId)
         |
         v
[Check Required Fields]
  For each field in configuration.fields:
    Check if Order.{field} is not null/empty
         |
         v
[Validation Result]
  All fields present: ✓
  Missing fields: ✗
         |
    ┌────┴────┐
    |         |
    v         v
[PASS] [FAIL]
   |            |
   |            v
   |       [Throw ValidationException]
   |       "Required field missing: AppointmentDate"
   |
   v
[Guard Condition Passed]
```

---

## Side Effect Execution Flow

```
[Side Effect Execution]
  SideEffect {
    Type: "CreateScheduledSlot"
    Configuration: {
      serviceInstallerId: "{payload.assignedSiId}"
      date: "{payload.appointmentDate}"
    }
  }
         |
         v
┌────────────────────────────────────────┐
│ RESOLVE EXECUTOR                          │
│ SideEffectExecutorRegistry.GetExecutor()  │
└────────────────────────────────────────┘
         |
         v
[Get Executor by Type]
  Executor = registry["CreateScheduledSlot"]
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NOT FOUND]
   |            |
   |            v
   |       [Throw InvalidOperationException]
   |       "Executor not found: CreateScheduledSlot"
   |
   v
┌────────────────────────────────────────┐
│ RESOLVE CONFIGURATION VARIABLES            │
└────────────────────────────────────────┘
         |
         v
[Replace Placeholders]
  serviceInstallerId: "{payload.assignedSiId}"
    → "SI-123"
  date: "{payload.appointmentDate}"
    → "2025-12-15"
         |
         v
┌────────────────────────────────────────┐
│ EXECUTE SIDE EFFECT                       │
│ CreateScheduledSlotExecutor.Execute()     │
└────────────────────────────────────────┘
         |
         v
[Create Scheduled Slot]
  ScheduledSlot {
    OrderId: entityId
    ServiceInstallerId: "SI-123"
    Date: "2025-12-15"
  }
         |
         v
[Save Changes]
  await _context.SaveChangesAsync()
         |
         v
[Side Effect Executed]
```

---

## Get Allowed Transitions Flow

```
[Get Allowed Transitions Request]
  GET /api/workflow/allowed-transitions?entityType=Order&entityId=order-456&currentStatus=Pending
         |
         v
┌────────────────────────────────────────┐
│ GET EFFECTIVE WORKFLOW DEFINITION         │
│ WorkflowDefinitionsService.GetEffectiveWorkflowDefinitionAsync()│
└────────────────────────────────────────┘
         |
         v
[Get Current User Roles]
  userRoles = ["Admin", "Manager"]
         |
         v
┌────────────────────────────────────────┐
│ FILTER TRANSITIONS                        │
│ WorkflowEngineService.GetAllowedTransitionsAsync()│
└────────────────────────────────────────┘
         |
         v
[Filter Transitions]
  Transitions.where(
    IsActive = true
    FromStatus = "Pending" (or null)
    AllowedRoles contains user role (or empty)
  )
         |
         v
[Return Allowed Transitions]
  [
    {
      FromStatus: "Pending"
      ToStatus: "Assigned"
      DisplayOrder: 1
      AllowedRoles: ["Admin", "Manager"]
    },
    {
      FromStatus: "Pending"
      ToStatus: "Cancelled"
      DisplayOrder: 2
      AllowedRoles: ["Admin"]
    }
  ]
```

---

## Entities Involved

### WorkflowDefinition Entity
```
WorkflowDefinition
├── Id (Guid)
├── CompanyId (Guid)
├── Name (string)
├── EntityType (string)
├── Description (string?)
├── IsActive (bool)
├── PartnerId (Guid?)
├── DepartmentId (Guid?)
├── CreatedAt (DateTime)
├── CreatedByUserId (Guid)
├── UpdatedAt (DateTime)
└── UpdatedByUserId (Guid)
```

### WorkflowTransition Entity
```
WorkflowTransition
├── Id (Guid)
├── WorkflowDefinitionId (Guid)
├── FromStatus (string?)
├── ToStatus (string)
├── DisplayOrder (int)
├── AllowedRoles (List<string>)
├── IsActive (bool)
├── GuardConditions (List<GuardCondition>)
└── SideEffects (List<SideEffect>)
```

### GuardCondition Entity
```
GuardCondition
├── Id (Guid)
├── TransitionId (Guid)
├── GuardConditionDefinitionId (Guid)
├── ConfigurationJson (string)
├── IsActive (bool)
└── ExecutionOrder (int)
```

### SideEffect Entity
```
SideEffect
├── Id (Guid)
├── TransitionId (Guid)
├── SideEffectDefinitionId (Guid)
├── ConfigurationJson (string)
├── ExecutionOrder (int)
└── IsActive (bool)
```

### WorkflowJob Entity
```
WorkflowJob
├── Id (Guid)
├── CompanyId (Guid)
├── WorkflowDefinitionId (Guid)
├── EntityType (string)
├── EntityId (Guid)
├── CurrentStatus (string)
├── TargetStatus (string)
├── State (enum: Pending, Running, Succeeded, Failed)
├── PayloadJson (string?)
├── InitiatedByUserId (Guid)
├── StartedAt (DateTime?)
├── CompletedAt (DateTime?)
├── LastError (string?)
└── CreatedAt (DateTime)
```

---

## API Endpoints Involved

### Workflow Definitions
- `GET /api/workflow-definitions` - List workflow definitions
- `GET /api/workflow-definitions/{id}` - Get workflow definition
- `POST /api/workflow-definitions` - Create workflow definition
- `PUT /api/workflow-definitions/{id}` - Update workflow definition
- `DELETE /api/workflow-definitions/{id}` - Delete workflow definition

### Transitions
- `GET /api/workflow-definitions/{id}/transitions` - List transitions
- `POST /api/workflow-definitions/{id}/transitions` - Create transition
- `PUT /api/workflow-definitions/{id}/transitions/{transitionId}` - Update transition
- `DELETE /api/workflow-definitions/{id}/transitions/{transitionId}` - Delete transition

### Guard Conditions
- `GET /api/workflow-definitions/{id}/transitions/{transitionId}/guards` - List guard conditions
- `POST /api/workflow-definitions/{id}/transitions/{transitionId}/guards` - Create guard condition
- `PUT /api/workflow-definitions/{id}/transitions/{transitionId}/guards/{guardId}` - Update guard condition
- `DELETE /api/workflow-definitions/{id}/transitions/{transitionId}/guards/{guardId}` - Delete guard condition

### Side Effects
- `GET /api/workflow-definitions/{id}/transitions/{transitionId}/side-effects` - List side effects
- `POST /api/workflow-definitions/{id}/transitions/{transitionId}/side-effects` - Create side effect
- `PUT /api/workflow-definitions/{id}/transitions/{transitionId}/side-effects/{sideEffectId}` - Update side effect
- `DELETE /api/workflow-definitions/{id}/transitions/{transitionId}/side-effects/{sideEffectId}` - Delete side effect

### Workflow Execution
- `POST /api/workflow/execute` - Execute transition
- `GET /api/workflow/allowed-transitions` - Get allowed transitions
- `GET /api/workflow/jobs` - List workflow jobs
- `GET /api/workflow/jobs/{id}` - Get workflow job

---

## Module Rules & Validations

### Workflow Definition Rules
- EntityType must be valid (Order, Invoice, etc.)
- Only one active workflow per (CompanyId, EntityType, PartnerId, DepartmentId)
- Workflow definitions are company-scoped
- Partner-specific workflows take precedence over default

### Transition Rules
- FromStatus can be null (matches any status)
- ToStatus is required
- Transitions must be unique per workflow definition
- AllowedRoles empty means all roles can transition

### Guard Condition Rules
- Guard conditions executed in ExecutionOrder
- All guard conditions must pass for transition to proceed
- Guard condition failures throw ValidationException
- Guard validators registered in GuardConditionValidatorRegistry

### Side Effect Rules
- Side effects executed in ExecutionOrder
- Side effects can fail without blocking transition (configurable)
- Side effect executors registered in SideEffectExecutorRegistry
- Configuration supports payload variable substitution

### Workflow Job Rules
- Jobs created for every transition execution
- Job state tracks execution progress
- Failed jobs store error message in LastError
- Jobs are immutable after completion

---

## Integration Points

### Orders Module
- Order status transitions go through workflow engine
- Guard conditions validate order data
- Side effects create scheduled slots, send notifications

### Scheduler Module
- Side effects create/update scheduled slots
- Transition execution triggers calendar updates

### Notifications Module
- Side effects send notifications on status changes
- Notification handlers triggered asynchronously

### Billing Module
- Invoice status transitions use workflow engine
- Guard conditions validate invoice data
- Side effects trigger payment processing

### Settings Module
- Guard condition definitions configured in settings
- Side effect definitions configured in settings
- Workflow definitions are settings-driven

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/02_modules/workflow/WORKFLOW_ACTIVATION_RULES.md` - Workflow activation rules
- `docs/01_system/WORKFLOW_ENGINE_FLOW.md` - Detailed workflow engine flow


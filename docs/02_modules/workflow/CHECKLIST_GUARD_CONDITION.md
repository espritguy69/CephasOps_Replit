# Checklist Guard Condition Configuration

This document explains how to configure workflow transitions to require checklist completion.

## Overview

The `checklistCompleted` guard condition validates that all required checklist items (main steps and sub-steps) are completed before allowing a status transition.

## Guard Condition Definition

The guard condition is automatically seeded with the following configuration:

- **Key**: `checklistCompleted`
- **Name**: Checklist Completed
- **Description**: All required checklist items must be completed before transitioning
- **Entity Type**: Order
- **Validator Type**: `ChecklistCompletedValidator`
- **Config**: `{}` (uses current order status by default)

### Optional Configuration

You can configure the validator to check a specific status code:

```json
{
  "statusCode": "MetCustomer"
}
```

If `statusCode` is not specified, it defaults to the current order status.

## Adding to Workflow Transitions

To require checklist completion for a specific transition, add `checklistCompleted: true` to the transition's `GuardConditionsJson`.

### Example: Require Checklist for MetCustomer → OrderCompleted

```json
{
  "fromStatus": "MetCustomer",
  "toStatus": "OrderCompleted",
  "guardConditions": {
    "checklistCompleted": true,
    "photosRequired": true,
    "serialsValidated": true
  }
}
```

### Example: Require Checklist for Specific Status

If you want to check checklist for a different status than the current one:

```json
{
  "fromStatus": "Assigned",
  "toStatus": "OnTheWay",
  "guardConditions": {
    "checklistCompleted": {
      "statusCode": "Assigned"
    }
  }
}
```

## Common Use Cases

### 1. Field Work Statuses

Require checklist completion before moving from field work statuses:

- `Assigned` → `OnTheWay`: Require "Call customer" checklist
- `MetCustomer` → `OrderCompleted`: Require "Installation steps" checklist

### 2. Documentation Statuses

Require checklist completion before moving to documentation statuses:

- `OrderCompleted` → `DocketsReceived`: Require "Completion verification" checklist

### 3. Billing Statuses

Require checklist completion before moving to billing statuses:

- `DocketsUploaded` → `ReadyForInvoice`: Require "Billing verification" checklist

## Validation Behavior

The validator checks:

1. **Main Steps with No Sub-Steps**: Requires the main step's answer = Yes
2. **Main Steps with Sub-Steps**: 
   - Requires all required sub-steps to have answer = Yes
   - Optionally requires the main step's own answer = Yes if it's also marked required
3. **Sub-Steps**: Requires answer = Yes

If any required item is incomplete, the transition is blocked with an error message listing all incomplete items.

## Error Messages

When validation fails, the error message includes:

```
Guard condition 'checklistCompleted' is required but not met for Order {OrderId}.
```

The validation service also provides detailed error messages listing specific incomplete items:

- "Required step 'Call customer' is not completed."
- "Required sub-step 'Confirm phone number' under 'Call customer' is not completed."

## UI Integration

The checklist validation is automatically integrated into:

1. **Order Detail Page**: Shows checklist completion status
2. **Workflow Transition Buttons**: Validates before allowing transition
3. **Status Change Modals**: Displays validation errors if checklist incomplete

## Testing

To test checklist validation:

1. Create a checklist for a status with required items
2. Create an order in that status
3. Attempt to transition without completing the checklist
4. Verify the transition is blocked with appropriate error message
5. Complete all required checklist items
6. Verify the transition now succeeds


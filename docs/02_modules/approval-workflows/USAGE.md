# Approval Workflows Usage Guide

This guide explains how to configure and use approval workflows in CephasOps for multi-step approvals.

## Overview

Approval workflows define multi-step approval processes for entities like reschedule requests and RMA requests. They support parallel and sequential approval steps with timeout and escalation.

## Key Concepts

### Workflow Types
- **Reschedule**: For order reschedule requests
- **RMA**: For RMA (Return Material Authorization) requests
- **Custom**: For other entity types

### Approval Steps
- **Sequential**: Steps execute one after another
- **Parallel**: Steps execute simultaneously
- **Timeout**: Automatic escalation if step not completed in time
- **Escalation**: Move to next approver if current approver doesn't respond

## Configuration Steps

### 1. Create Approval Workflow

1. Navigate to **Settings > Approval Workflows**
2. Click **Create Approval Workflow**
3. Fill in the form:

   **Basic Information:**
   - **Name**: e.g., "Reschedule Approval Workflow"
   - **Description**: Optional description
   - **Workflow Type**: e.g., "Reschedule"
   - **Entity Type**: e.g., "OrderReschedule"
   - **Is Active**: Check to activate

   **Scope (Specificity):**
   - **Partner**: Optional - specific partner
   - **Department**: Optional - specific department
   - **Order Type**: Optional - specific order type
   - **Value Threshold**: Optional - minimum value to trigger workflow

4. Click **Save**

### 2. Add Approval Steps

1. Click **Add Step** on the workflow
2. Fill in step details:

   **Step Information:**
   - **Step Name**: e.g., "Manager Approval"
   - **Step Order**: Sequential order (1, 2, 3...)
   - **Is Parallel**: Check if this step runs in parallel with others
   - **Approval Type**: "Role" or "User"
   - **Approver Role**: If approval type is "Role"
   - **Approver User ID**: If approval type is "User"
   - **Timeout Minutes**: Minutes before escalation (optional)
   - **Escalate To**: Role or user to escalate to if timeout (optional)
   - **Is Required**: Check if this step is required

3. Click **Save Step**

### 3. Configure Step Sequence

**Sequential Steps:**
- Step 1: Manager Approval (Order: 1)
- Step 2: Director Approval (Order: 2)
- Step 3: Final Approval (Order: 3)

**Parallel Steps:**
- Step 1: Manager Approval (Order: 1, Is Parallel: Yes)
- Step 2: Finance Approval (Order: 1, Is Parallel: Yes)
- Step 3: Final Approval (Order: 2) - Waits for both parallel steps

## Example Scenarios

### Scenario 1: Simple Reschedule Approval

**Workflow:**
- Name: "Reschedule Approval"
- Workflow Type: Reschedule
- Entity Type: OrderReschedule

**Steps:**
1. Manager Approval (Role: Manager, Timeout: 60 minutes)

**Result:**
- Reschedule requests require manager approval
- If manager doesn't approve within 60 minutes, request escalates

### Scenario 2: Multi-Step RMA Approval

**Workflow:**
- Name: "High-Value RMA Approval"
- Workflow Type: RMA
- Entity Type: RmaRequest
- Value Threshold: 1000

**Steps:**
1. Manager Approval (Role: Manager, Order: 1)
2. Director Approval (Role: Director, Order: 2)
3. Finance Approval (Role: Finance, Order: 3)

**Result:**
- RMA requests over $1000 require three sequential approvals
- Each step must be approved before moving to next

### Scenario 3: Parallel Approval

**Workflow:**
- Name: "Complex Reschedule Approval"
- Workflow Type: Reschedule

**Steps:**
1. Manager Approval (Role: Manager, Order: 1, Is Parallel: Yes)
2. Operations Approval (Role: Operations, Order: 1, Is Parallel: Yes)
3. Final Approval (Role: Director, Order: 2)

**Result:**
- Manager and Operations approve in parallel
- After both approve, Director gives final approval

## How Approval Workflows Work

### 1. Workflow Resolution

When an entity is created (e.g., reschedule request), the system:
1. Finds applicable workflow based on:
   - Workflow type
   - Entity type
   - Partner (if specified)
   - Department (if specified)
   - Order type (if specified)
   - Value threshold (if specified)

2. Sets entity status to "PendingApproval" if workflow found
3. Sets `ApprovalWorkflowId` on entity

### 2. Step Execution

1. **First Step**: Automatically initiated when entity is created
2. **Sequential Steps**: Wait for previous step to complete
3. **Parallel Steps**: Execute simultaneously
4. **Timeout**: If step times out, escalates to next approver
5. **Completion**: When all required steps are approved, entity status changes to "Approved"

### 3. Approval Actions

Approvers can:
- **Approve**: Move to next step
- **Reject**: Stop workflow, set entity status to "Rejected"
- **Request Changes**: Return to previous step with comments

## Integration Points

### Reschedule Requests

When a reschedule request is created via `EmailSendingService.SendRescheduleRequestAsync`:
- System checks for applicable approval workflow
- If found, sets status to "PendingApproval"
- Approval steps are initiated

### RMA Requests

When an RMA request is created via `RMAService.CreateRmaRequestAsync`:
- System checks for applicable approval workflow
- If found, sets status to "PendingApproval"
- Approval steps are initiated

## Best Practices

1. **Start Simple**: Begin with single-step approvals
2. **Use Specific Scopes**: Narrow scope to avoid unintended matches
3. **Set Timeouts**: Always set timeouts to prevent stuck approvals
4. **Configure Escalation**: Set escalation targets for timeouts
5. **Test Workflows**: Test with sample requests before production
6. **Document Steps**: Use clear step names and descriptions
7. **Monitor Approvals**: Regularly review pending approvals

## Troubleshooting

### Workflow Not Being Applied

- Check if workflow is **Is Active**
- Verify entity matches workflow scope
- Check if more specific workflow exists

### Steps Not Executing

- Verify step order is correct
- Check if previous steps are completed (for sequential)
- Review approval logs

### Timeouts Not Working

- Verify timeout minutes are set
- Check escalation targets are configured
- Review timeout logs

## Related Documentation

- [SLA Configuration Usage Guide](../sla-configuration/USAGE.md)
- [Business Hours Usage Guide](../business-hours/USAGE.md)
- [Phase 2 Settings API Documentation](../../04_api/PHASE2_SETTINGS_API.md)


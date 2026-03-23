# Automation Rules Usage Guide

This guide explains how to configure and use automation rules in CephasOps to automate order management tasks.

## Overview

Automation rules automatically execute actions when specific conditions are met. They help reduce manual work and ensure consistent processes.

## Supported Rule Types

### 1. Auto-Assign
Automatically assign orders to service installers based on role or availability.

### 2. Auto-Escalate
Automatically escalate orders to managers or specific users.

### 3. Auto-Notify
Automatically send notifications to roles or users when orders reach specific statuses.

### 4. Auto-Status-Change
Automatically change order status based on conditions.

## Configuration Steps

### 1. Create Automation Rule

1. Navigate to **Settings > Automation Rules**
2. Click **Create Automation Rule**
3. Fill in the form:

   **Basic Information:**
   - **Name**: e.g., "Auto-Assign New Orders"
   - **Description**: Optional description
   - **Rule Type**: Select from dropdown (auto-assign, auto-escalate, auto-notify, auto-status-change)
   - **Priority**: Higher priority rules execute first
   - **Is Active**: Check to activate

   **Scope:**
   - **Entity Type**: e.g., "Order"
   - **Current Status**: Status that triggers the rule (e.g., "Pending")
   - **Partner**: Optional - specific partner
   - **Department**: Optional - specific department
   - **Order Type**: Optional - specific order type

   **Action Configuration:**
   - **Target Role**: Role to assign/escalate/notify (e.g., "Installer")
   - **Target User**: Optional - specific user ID
   - **Target Status**: For auto-status-change, the target status
   - **Action Config JSON**: Advanced configuration (optional)

4. Click **Save**

### 2. Configure Action Details

#### Auto-Assign Configuration

```json
{
  "assignToRole": "Installer",
  "loadBalance": true,
  "considerAvailability": true
}
```

#### Auto-Escalate Configuration

```json
{
  "escalateToRole": "Manager",
  "escalationReason": "Order pending for too long"
}
```

#### Auto-Notify Configuration

```json
{
  "notifyRoles": "Manager,Supervisor",
  "notificationTitle": "Order Status Changed",
  "notificationMessage": "Order {ServiceId} has changed status to {Status}"
}
```

#### Auto-Status-Change Configuration

```json
{
  "targetStatus": "Assigned",
  "reason": "Auto-assigned via automation rule"
}
```

## Example Scenarios

### Scenario 1: Auto-Assign New Orders

**Configuration:**
- Name: "Auto-Assign New Orders"
- Rule Type: auto-assign
- Entity Type: Order
- Current Status: Pending
- Target Role: Installer

**Result:**
- When an order reaches "Pending" status, it's automatically assigned to an available installer with the "Installer" role

### Scenario 2: Auto-Escalate Blocked Orders

**Configuration:**
- Name: "Escalate Blocked Orders"
- Rule Type: auto-escalate
- Entity Type: Order
- Current Status: Blocker
- Target Role: Manager

**Result:**
- When an order is set to "Blocker" status, it's automatically escalated to managers

### Scenario 3: Auto-Notify on Completion

**Configuration:**
- Name: "Notify on Order Completion"
- Rule Type: auto-notify
- Entity Type: Order
- Current Status: Completed
- Target Role: Manager,Supervisor

**Result:**
- When an order is completed, notifications are sent to all managers and supervisors

### Scenario 4: Auto-Status-Change (example: Assigned → OnTheWay)

**Note:** **InProgress** is not a valid order status. Order statuses and transitions are defined in [WORKFLOW_STATUS_REFERENCE.md](../../05_data_model/WORKFLOW_STATUS_REFERENCE.md) and seeded in 07_gpon_order_workflow.sql. Use only canonical statuses (e.g. Pending, Assigned, OnTheWay, MetCustomer, OrderCompleted, …).

**Configuration (example):**
- Name: "Auto-Status to OnTheWay when SI starts"
- Rule Type: auto-status-change
- Entity Type: Order
- Current Status: Assigned
- Target Status: OnTheWay

**Result:**
- When an order is in Assigned and the rule triggers, status can change to OnTheWay (if that transition is allowed by the workflow)

## Rule Execution Order

Rules are executed in priority order (higher priority first). Multiple rules can execute for the same order status change.

## Best Practices

1. **Start Simple**: Begin with basic rules and add complexity gradually
2. **Test Thoroughly**: Test rules with sample orders before activating
3. **Use Specific Scopes**: Narrow scope (partner, department, order type) to avoid unintended matches
4. **Set Appropriate Priorities**: Higher priority for critical rules
5. **Monitor Execution**: Review logs to ensure rules execute as expected
6. **Document Rules**: Use descriptions to explain rule purpose
7. **Disable Before Editing**: Disable rules before making changes to avoid conflicts

## Troubleshooting

### Rule Not Executing

- Check if rule is **Is Active**
- Verify order matches rule scope (status, partner, department, order type)
- Check rule priority (lower priority rules may be overridden)
- Review application logs for errors

### Auto-Assign Not Working

- Verify target role exists and has users
- Check if users with role are service installers
- Ensure service installers are active

### Notifications Not Being Sent

- Verify notification service is running
- Check user notification preferences
- Review notification logs

## Related Documentation

- [SLA Configuration Usage Guide](../sla-configuration/USAGE.md)
- [Escalation Rules Usage Guide](../escalation-rules/USAGE.md)
- [Phase 2 Settings API Documentation](../../04_api/PHASE2_SETTINGS_API.md)


# Escalation Rules Usage Guide

This guide explains how to configure and use escalation rules in CephasOps.

## Overview

Escalation rules automatically escalate orders to managers or specific users based on time, status, or conditions. They help ensure critical issues are addressed promptly.

## Trigger Types

### 1. Time-Based
Escalates after order has been in a status for a specified duration.

### 2. Status-Based
Escalates immediately when order reaches a specific status.

### 3. Condition-Based
Escalates based on custom conditions (priority, VIP status, etc.).

## Configuration Steps

### 1. Create Escalation Rule

1. Navigate to **Settings > Escalation Rules**
2. Click **Create Escalation Rule**
3. Fill in the form:

   **Basic Information:**
   - **Name**: e.g., "Escalate Blocked Orders"
   - **Description**: Optional description
   - **Trigger Type**: Select from dropdown (time-based, status-based, condition-based)
   - **Priority**: Higher priority rules execute first
   - **Is Active**: Check to activate

   **Scope:**
   - **Entity Type**: e.g., "Order"
   - **Current Status**: Status that triggers escalation (e.g., "Blocker")
   - **Partner**: Optional - specific partner
   - **Department**: Optional - specific department
   - **Order Type**: Optional - specific order type

   **Trigger Configuration:**
   - **Trigger Delay Minutes**: For time-based, minutes before escalation
   - **Trigger Conditions JSON**: For condition-based, custom conditions

   **Escalation Action:**
   - **Target Role**: Role to escalate to (e.g., "Manager")
   - **Target User ID**: Optional - specific user to escalate to
   - **Target Status**: Optional - change order status on escalation

4. Click **Save**

### 2. Configure Trigger Conditions (Condition-Based)

For condition-based triggers, use JSON configuration:

```json
{
  "priority": "High",
  "isVip": true,
  "rescheduleCount": { "gte": 2 }
}
```

## Example Scenarios

### Scenario 1: Time-Based Escalation

**Configuration:**
- Name: "Escalate Pending Orders After 2 Hours"
- Trigger Type: time-based
- Entity Type: Order
- Current Status: Pending
- Trigger Delay Minutes: 120
- Target Role: Manager

**Result:**
- Orders in "Pending" status for 2 hours are escalated to managers
- Notifications are sent to all users with "Manager" role

### Scenario 2: Status-Based Escalation

**Configuration:**
- Name: "Escalate Blocked Orders"
- Trigger Type: status-based
- Entity Type: Order
- Current Status: Blocker
- Target Role: Manager
- Target Status: Escalated

**Result:**
- Orders set to "Blocker" status are immediately escalated
- Order status changes to "Escalated"
- Managers are notified

### Scenario 3: Condition-Based Escalation

**Configuration:**
- Name: "Escalate High-Priority VIP Orders"
- Trigger Type: condition-based
- Entity Type: Order
- Current Status: Pending
- Trigger Conditions JSON:
  ```json
  {
    "priority": "High",
    "isVip": true
  }
  ```
- Target Role: Director

**Result:**
- High-priority VIP orders in "Pending" status are escalated to directors

### Scenario 4: Multi-Level Escalation

**Configuration:**
- Rule 1: Escalate after 1 hour → Manager
- Rule 2: Escalate after 4 hours → Director
- Rule 3: Escalate after 8 hours → CEO

**Result:**
- Progressive escalation as time passes
- Each level gets notified at appropriate time

## How Escalation Works

### 1. Rule Evaluation

When order status changes:
1. System finds applicable escalation rules
2. Evaluates trigger conditions
3. Executes escalation actions

### 2. Escalation Actions

**Escalate to Role:**
- Finds all users with target role
- Sends notifications to all users
- Logs escalation in order history

**Escalate to User:**
- Sends notification to specific user
- Logs escalation in order history

**Change Status:**
- Changes order status to target status
- Triggers workflow validation
- Executes side effects

### 3. Time-Based Calculation

For time-based triggers:
- Calculates time since order entered current status
- Uses business hours if configured
- Excludes weekends and holidays

## Integration Points

### Order Status Changes

Escalation rules are checked after order status changes:
- System evaluates all applicable rules
- Executes matching rules
- Sends notifications

### Business Hours

Time-based escalation respects business hours:
- Only counts time during business hours
- Excludes weekends and holidays
- Uses department-specific hours if available

## Best Practices

1. **Set Appropriate Delays**: Don't escalate too quickly or too slowly
2. **Use Specific Scopes**: Narrow scope to avoid unintended escalations
3. **Configure Notifications**: Ensure escalation targets receive notifications
4. **Test Rules**: Test with sample orders before production
5. **Monitor Escalations**: Review escalation logs regularly
6. **Avoid Duplicate Escalations**: Use conditions to prevent multiple escalations
7. **Document Rules**: Use descriptions to explain escalation logic

## Troubleshooting

### Escalation Not Triggering

- Check if rule is **Is Active**
- Verify order matches rule scope
- For time-based, check elapsed time calculation
- Review application logs

### Notifications Not Being Sent

- Verify notification service is running
- Check user notification preferences
- Verify target role has users
- Review notification logs

### Time Calculation Incorrect

- Verify business hours are configured
- Check timezone settings
- Review time calculation logs

## Related Documentation

- [SLA Configuration Usage Guide](../sla-configuration/USAGE.md)
- [Business Hours Usage Guide](../business-hours/USAGE.md)
- [Automation Rules Usage Guide](../automation-rules/USAGE.md)
- [Phase 2 Settings API Documentation](../../04_api/PHASE2_SETTINGS_API.md)


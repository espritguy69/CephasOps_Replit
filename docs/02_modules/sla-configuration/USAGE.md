# SLA Configuration Usage Guide

This guide explains how to configure and use SLA (Service Level Agreement) profiles in CephasOps.

## Overview

SLA profiles define response and resolution time limits for orders. The system automatically tracks SLA compliance and sends notifications when breaches occur.

## Key Concepts

### Response SLA
Time limit for responding to an order (e.g., moving from "Pending" to "Assigned").

### Resolution SLA
Time limit for completing an order (e.g., moving from "Assigned" to "Completed").

### Business Hours Exclusion
SLA calculation can exclude non-business hours, weekends, and public holidays.

### VIP Orders
Special SLA profiles can be configured for VIP orders (orders from VIP email addresses).

## Configuration Steps

### 1. Create Business Hours (Required for Business Hours Exclusion)

Before creating SLA profiles that exclude non-business hours, configure business hours:

1. Navigate to **Settings > Business Hours & Holidays**
2. Click **Create Business Hours**
3. Configure:
   - **Name**: e.g., "Standard Business Hours"
   - **Department**: Optional (leave blank for company-wide)
   - **Timezone**: e.g., "Asia/Kuala_Lumpur"
   - **Day-specific hours**: Set start/end times for each day
   - **Is Default**: Check if this is the default configuration
   - **Is Active**: Check to activate

4. Add public holidays:
   - Click **Add Holiday**
   - Enter holiday name, date, and type
   - Check **Is Recurring** for annual holidays

### 2. Create SLA Profile

1. Navigate to **Settings > SLA Configuration**
2. Click **Create SLA Profile**
3. Fill in the form:

   **Basic Information:**
   - **Name**: e.g., "TIME Activation SLA"
   - **Description**: Optional description
   - **Is Active**: Check to activate

   **Scope (Specificity):**
   - **Partner**: Optional - specific partner (e.g., TIME)
   - **Order Type**: Optional - specific order type (e.g., "Activation")
   - **Department**: Optional - specific department
   - **Is VIP Only**: Check if this applies only to VIP orders

   **Response SLA:**
   - **Response SLA Minutes**: e.g., 60 (1 hour)
   - **Response SLA From Status**: e.g., "Pending"
   - **Response SLA To Status**: e.g., "Assigned"

   **Resolution SLA:**
   - **Resolution SLA Minutes**: e.g., 1440 (24 hours)
   - **Resolution SLA From Status**: e.g., "Assigned"
   - **Resolution SLA To Status**: e.g., "Completed"

   **Business Hours:**
   - **Exclude Non-Business Hours**: Check to exclude non-business hours
   - **Exclude Weekends**: Automatically handled by business hours
   - **Exclude Holidays**: Automatically handled by public holidays

   **Notifications:**
   - **Notify On Breach**: Check to send notifications when SLA is breached

4. Click **Save**

### 3. Set Default Profile (Optional)

If multiple profiles match an order, the most specific one is used. You can set a default profile as a fallback:

1. Find the profile you want to set as default
2. Click **Set as Default**

## How SLA Resolution Works

The system uses a specificity-based resolution:

1. **Most Specific**: Partner + Order Type + Department + VIP
2. **Less Specific**: Partner + Order Type + Department
3. **Less Specific**: Partner + Order Type
4. **Less Specific**: Partner
5. **Less Specific**: Order Type + Department
6. **Less Specific**: Order Type
7. **Default**: Company-wide default profile

## Automatic SLA Tracking

SLA tracking happens automatically when:

1. **Order status changes** - The system:
   - Determines effective SLA profile
   - Calculates elapsed time (excluding non-business hours if configured)
   - Checks if SLA is breached
   - Updates order with breach information (`KpiBreachedAt`, `KpiCategory`)
   - Sends notifications if configured

2. **Business hours calculation** - If `ExcludeNonBusinessHours` is enabled:
   - Only counts time during business hours
   - Excludes weekends (if not configured as business days)
   - Excludes public holidays

## Example Scenarios

### Scenario 1: Standard Activation SLA

**Configuration:**
- Name: "Standard Activation SLA"
- Order Type: "Activation"
- Response SLA: 60 minutes (Pending → Assigned)
- Resolution SLA: 1440 minutes (Assigned → Completed)
- Exclude Non-Business Hours: Yes

**Result:**
- Orders of type "Activation" have 1 hour to be assigned
- Orders have 24 business hours to be completed
- Only business hours are counted

### Scenario 2: VIP Order SLA

**Configuration:**
- Name: "VIP Activation SLA"
- Order Type: "Activation"
- Is VIP Only: Yes
- Response SLA: 30 minutes (Pending → Assigned)
- Resolution SLA: 720 minutes (Assigned → Completed)

**Result:**
- VIP orders have faster SLA (30 minutes response, 12 hours resolution)
- Only applies to orders from VIP email addresses

### Scenario 3: Partner-Specific SLA

**Configuration:**
- Name: "TIME Activation SLA"
- Partner: TIME
- Order Type: "Activation"
- Response SLA: 45 minutes
- Resolution SLA: 1200 minutes

**Result:**
- Only TIME activation orders use this SLA
- Other partners use less specific or default profiles

## Monitoring SLA Breaches

### View Breached Orders

1. Navigate to **Orders**
2. Filter by:
   - **KPI Category**: "SlaBreach"
   - **Status**: Any status

### SLA Breach Notifications

When an SLA is breached and `NotifyOnBreach` is enabled:

- Notifications are sent to:
  - Department members (if order has department)
  - Users with "Manager" role
- Notification includes:
  - Order details
  - SLA type (Response/Resolution)
  - Elapsed time vs. limit
  - Link to order

## Best Practices

1. **Start with Default Profile**: Create a company-wide default SLA profile first
2. **Add Specific Profiles**: Create partner/order type specific profiles as needed
3. **Configure Business Hours**: Essential for accurate SLA calculation
4. **Set Up Holidays**: Add all public holidays to ensure accurate calculation
5. **Enable Notifications**: Enable breach notifications for critical SLAs
6. **Test with Sample Orders**: Create test orders to verify SLA calculation
7. **Monitor Breaches**: Regularly review breached orders to identify issues

## Troubleshooting

### SLA Not Being Applied

- Check if profile is **Is Active**
- Verify order matches profile scope (partner, order type, department)
- Check if more specific profile exists (specificity resolution)

### Business Hours Not Being Excluded

- Verify business hours are configured
- Check if `ExcludeNonBusinessHours` is enabled on SLA profile
- Ensure business hours are active and effective for the date range

### Notifications Not Being Sent

- Verify `NotifyOnBreach` is enabled
- Check notification service is running
- Verify users have notification preferences configured

## Related Documentation

- [Business Hours & Holidays Usage Guide](../business-hours/USAGE.md)
- [Automation Rules Usage Guide](../automation-rules/USAGE.md)
- [Phase 2 Settings API Documentation](../../04_api/PHASE2_SETTINGS_API.md)


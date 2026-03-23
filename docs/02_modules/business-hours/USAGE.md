# Business Hours & Holidays Usage Guide

This guide explains how to configure business hours and public holidays in CephasOps.

## Overview

Business hours define when your organization operates. They are used for:
- SLA calculation (excluding non-business hours)
- Escalation rule time-based triggers
- Automation rule scheduling

## Configuration Steps

### 1. Create Business Hours

1. Navigate to **Settings > Business Hours & Holidays**
2. Click **Create Business Hours**
3. Fill in the form:

   **Basic Information:**
   - **Name**: e.g., "Standard Business Hours"
   - **Description**: Optional description
   - **Department**: Optional - leave blank for company-wide
   - **Timezone**: e.g., "Asia/Kuala_Lumpur"
   - **Is Default**: Check if this is the default configuration
   - **Is Active**: Check to activate

   **Day-Specific Hours:**
   - **Monday**: Start time (e.g., "09:00") and End time (e.g., "17:00")
   - **Tuesday**: Start and End times
   - **Wednesday**: Start and End times
   - **Thursday**: Start and End times
   - **Friday**: Start and End times
   - **Saturday**: Start and End times (or leave blank for closed)
   - **Sunday**: Start and End times (or leave blank for closed)

   **Effective Dates:**
   - **Effective From**: Optional - start date
   - **Effective To**: Optional - end date

4. Click **Save**

### 2. Add Public Holidays

1. In **Business Hours & Holidays** page, go to **Public Holidays** section
2. Click **Add Holiday**
3. Fill in the form:

   **Holiday Information:**
   - **Name**: e.g., "New Year's Day"
   - **Holiday Date**: Date of the holiday
   - **Holiday Type**: e.g., "National", "State", "Religious"
   - **State**: Optional - specific state (for state holidays)
   - **Is Recurring**: Check if this holiday repeats annually
   - **Description**: Optional description
   - **Is Active**: Check to activate

4. Click **Save**

## Business Hours Resolution

The system uses a specificity-based resolution:

1. **Department-Specific**: Business hours for specific department
2. **Company-Wide Default**: Default business hours (Is Default = true)
3. **Fallback**: Always open (if no configuration exists)

## Example Configurations

### Standard Business Hours (Monday-Friday)

**Configuration:**
- Monday: 09:00 - 17:00
- Tuesday: 09:00 - 17:00
- Wednesday: 09:00 - 17:00
- Thursday: 09:00 - 17:00
- Friday: 09:00 - 17:00
- Saturday: (blank - closed)
- Sunday: (blank - closed)

### Extended Business Hours (Including Saturday)

**Configuration:**
- Monday-Friday: 09:00 - 17:00
- Saturday: 09:00 - 13:00
- Sunday: (blank - closed)

### Department-Specific Hours

**Configuration:**
- Name: "Support Team Hours"
- Department: Support
- Monday-Friday: 08:00 - 20:00
- Saturday-Sunday: 10:00 - 16:00

## Public Holidays

### National Holidays

Add all national public holidays:
- New Year's Day (January 1, Recurring: Yes)
- Chinese New Year (Date varies, Recurring: Yes)
- Hari Raya (Date varies, Recurring: Yes)
- Merdeka Day (August 31, Recurring: Yes)
- Christmas (December 25, Recurring: Yes)

### State Holidays

For state-specific holidays:
- Set **State** field to specific state
- Only applies to that state's operations

### Recurring Holidays

For holidays that repeat annually:
- Check **Is Recurring**
- System automatically applies holiday on same date each year

## Integration with SLA

When SLA profiles have `ExcludeNonBusinessHours` enabled:

1. **Time Calculation**: Only counts time during business hours
2. **Weekend Exclusion**: Automatically excludes weekends if not configured as business days
3. **Holiday Exclusion**: Automatically excludes public holidays

**Example:**
- Order created: Friday 16:00
- Business hours end: 17:00
- Next business day: Monday 09:00
- SLA calculation: Only counts 1 hour on Friday, then resumes Monday 09:00

## Integration with Escalation Rules

Time-based escalation rules use business hours:

- **Time-Based Triggers**: Only count time during business hours
- **Weekend Handling**: Weekends don't count toward escalation time
- **Holiday Handling**: Holidays don't count toward escalation time

## Best Practices

1. **Set Default First**: Create company-wide default business hours
2. **Add All Holidays**: Include all public holidays for accurate calculation
3. **Use Recurring**: Mark annual holidays as recurring
4. **Department Overrides**: Create department-specific hours only when needed
5. **Effective Dates**: Use effective dates for temporary changes (e.g., holiday schedules)
6. **Timezone**: Always set correct timezone
7. **Test Calculations**: Verify business hours calculation with sample dates

## Troubleshooting

### Business Hours Not Being Applied

- Check if business hours are **Is Active**
- Verify effective dates (Effective From/To)
- Check if department-specific hours exist (takes precedence)

### Holidays Not Being Excluded

- Verify holiday is **Is Active**
- Check holiday date matches
- For recurring holidays, verify date format

### Timezone Issues

- Ensure timezone is set correctly
- Verify timezone format (e.g., "Asia/Kuala_Lumpur")
- Check server timezone settings

## Related Documentation

- [SLA Configuration Usage Guide](../sla-configuration/USAGE.md)
- [Escalation Rules Usage Guide](../escalation-rules/USAGE.md)
- [Phase 2 Settings API Documentation](../../04_api/PHASE2_SETTINGS_API.md)


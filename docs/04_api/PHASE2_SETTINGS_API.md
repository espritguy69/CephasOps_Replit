# Phase 2 Settings API Documentation

This document provides comprehensive API documentation for all Phase 2 Settings modules: SLA Configuration, Automation Rules, Approval Workflows, Business Hours & Holidays, Escalation Rules, Guard Condition Definitions, and Side Effect Definitions.

## Base URL

All endpoints are prefixed with `/api/` and require authentication via JWT Bearer token.

## Common Response Format

All endpoints follow the standard CephasOps response envelope:

```json
{
  "success": true,
  "message": "optional status message",
  "data": {},
  "errors": []
}
```

## Authentication

All endpoints require the `Authorization: Bearer <token>` header. Some endpoints require specific roles (SuperAdmin, Admin).

---

## 1. SLA Profiles API

**Base Path:** `/api/sla-profiles`

### Get SLA Profiles

**GET** `/api/sla-profiles`

**Query Parameters:**
- `orderType` (string, optional): Filter by order type
- `partnerId` (Guid, optional): Filter by partner
- `departmentId` (Guid, optional): Filter by department
- `isActive` (bool, optional): Filter by active status

**Response:** `200 OK` - List of `SlaProfileDto`

### Get SLA Profile by ID

**GET** `/api/sla-profiles/{id}`

**Response:** `200 OK` - `SlaProfileDto` or `404 Not Found`

### Get Effective SLA Profile

**GET** `/api/sla-profiles/effective`

**Query Parameters:**
- `partnerId` (Guid, optional)
- `orderType` (string, required)
- `departmentId` (Guid, optional)
- `isVip` (bool, default: false)
- `effectiveDate` (DateTime, optional)

**Response:** `200 OK` - `SlaProfileDto` or `404 Not Found`

### Create SLA Profile

**POST** `/api/sla-profiles`

**Authorization:** SuperAdmin, Admin

**Request Body:** `CreateSlaProfileDto`

**Response:** `201 Created` - `SlaProfileDto`

### Update SLA Profile

**PUT** `/api/sla-profiles/{id}`

**Authorization:** SuperAdmin, Admin

**Request Body:** `UpdateSlaProfileDto`

**Response:** `200 OK` - `SlaProfileDto` or `404 Not Found`

### Delete SLA Profile

**DELETE** `/api/sla-profiles/{id}`

**Authorization:** SuperAdmin, Admin

**Response:** `204 No Content` or `404 Not Found`

### Set as Default

**POST** `/api/sla-profiles/{id}/set-default`

**Authorization:** SuperAdmin, Admin

**Response:** `200 OK` - `SlaProfileDto`

---

## 2. Automation Rules API

**Base Path:** `/api/automation-rules`

### Get Automation Rules

**GET** `/api/automation-rules`

**Query Parameters:**
- `ruleType` (string, optional): Filter by rule type (auto-assign, auto-escalate, auto-notify, auto-status-change)
- `entityType` (string, optional): Filter by entity type (Order, RmaRequest, etc.)
- `isActive` (bool, optional): Filter by active status

**Response:** `200 OK` - List of `AutomationRuleDto`

### Get Automation Rule by ID

**GET** `/api/automation-rules/{id}`

**Response:** `200 OK` - `AutomationRuleDto` or `404 Not Found`

### Get Applicable Rules

**GET** `/api/automation-rules/applicable`

**Query Parameters:**
- `entityType` (string, required)
- `currentStatus` (string, optional)
- `partnerId` (Guid, optional)
- `departmentId` (Guid, optional)
- `orderType` (string, optional)

**Response:** `200 OK` - List of `AutomationRuleDto`

### Create Automation Rule

**POST** `/api/automation-rules`

**Authorization:** SuperAdmin, Admin

**Request Body:** `CreateAutomationRuleDto`

**Response:** `201 Created` - `AutomationRuleDto`

### Update Automation Rule

**PUT** `/api/automation-rules/{id}`

**Authorization:** SuperAdmin, Admin

**Request Body:** `UpdateAutomationRuleDto`

**Response:** `200 OK` - `AutomationRuleDto` or `404 Not Found`

### Delete Automation Rule

**DELETE** `/api/automation-rules/{id}`

**Authorization:** SuperAdmin, Admin

**Response:** `204 No Content` or `404 Not Found`

### Toggle Active Status

**POST** `/api/automation-rules/{id}/toggle-active`

**Authorization:** SuperAdmin, Admin

**Response:** `200 OK` - `AutomationRuleDto`

---

## 3. Approval Workflows API

**Base Path:** `/api/approval-workflows`

### Get Approval Workflows

**GET** `/api/approval-workflows`

**Query Parameters:**
- `workflowType` (string, optional): Filter by workflow type (Reschedule, RMA, etc.)
- `entityType` (string, optional): Filter by entity type
- `isActive` (bool, optional): Filter by active status

**Response:** `200 OK` - List of `ApprovalWorkflowDto`

### Get Approval Workflow by ID

**GET** `/api/approval-workflows/{id}`

**Response:** `200 OK` - `ApprovalWorkflowDto` or `404 Not Found`

### Get Effective Workflow

**GET** `/api/approval-workflows/effective`

**Query Parameters:**
- `workflowType` (string, required)
- `entityType` (string, required)
- `partnerId` (Guid, optional)
- `departmentId` (Guid, optional)
- `orderType` (string, optional)
- `value` (decimal, optional): Value threshold

**Response:** `200 OK` - `ApprovalWorkflowDto` or `404 Not Found`

### Create Approval Workflow

**POST** `/api/approval-workflows`

**Authorization:** SuperAdmin, Admin

**Request Body:** `CreateApprovalWorkflowDto`

**Response:** `201 Created` - `ApprovalWorkflowDto`

### Update Approval Workflow

**PUT** `/api/approval-workflows/{id}`

**Authorization:** SuperAdmin, Admin

**Request Body:** `UpdateApprovalWorkflowDto`

**Response:** `200 OK` - `ApprovalWorkflowDto` or `404 Not Found`

### Delete Approval Workflow

**DELETE** `/api/approval-workflows/{id}`

**Authorization:** SuperAdmin, Admin

**Response:** `204 No Content` or `404 Not Found`

---

## 4. Business Hours & Holidays API

**Base Path:** `/api/business-hours`

### Get Business Hours

**GET** `/api/business-hours`

**Query Parameters:**
- `departmentId` (Guid, optional): Filter by department
- `isActive` (bool, optional): Filter by active status

**Response:** `200 OK` - List of `BusinessHoursDto`

### Get Business Hours by ID

**GET** `/api/business-hours/{id}`

**Response:** `200 OK` - `BusinessHoursDto` or `404 Not Found`

### Get Effective Business Hours

**GET** `/api/business-hours/effective`

**Query Parameters:**
- `departmentId` (Guid, optional)
- `effectiveDate` (DateTime, optional)

**Response:** `200 OK` - `BusinessHoursDto` or `404 Not Found`

### Check if Business Hours

**GET** `/api/business-hours/check`

**Query Parameters:**
- `dateTime` (DateTime, required)
- `departmentId` (Guid, optional)

**Response:** `200 OK` - `{ "isBusinessHours": true/false }`

### Create Business Hours

**POST** `/api/business-hours`

**Authorization:** SuperAdmin, Admin

**Request Body:** `CreateBusinessHoursDto`

**Response:** `201 Created` - `BusinessHoursDto`

### Update Business Hours

**PUT** `/api/business-hours/{id}`

**Authorization:** SuperAdmin, Admin

**Request Body:** `UpdateBusinessHoursDto`

**Response:** `200 OK` - `BusinessHoursDto` or `404 Not Found`

### Delete Business Hours

**DELETE** `/api/business-hours/{id}`

**Authorization:** SuperAdmin, Admin

**Response:** `204 No Content` or `404 Not Found`

### Get Public Holidays

**GET** `/api/business-hours/holidays`

**Query Parameters:**
- `year` (int, optional): Filter by year
- `isActive` (bool, optional): Filter by active status

**Response:** `200 OK` - List of `PublicHolidayDto`

### Create Public Holiday

**POST** `/api/business-hours/holidays`

**Authorization:** SuperAdmin, Admin

**Request Body:** `CreatePublicHolidayDto`

**Response:** `201 Created` - `PublicHolidayDto`

### Update Public Holiday

**PUT** `/api/business-hours/holidays/{id}`

**Authorization:** SuperAdmin, Admin

**Request Body:** `UpdatePublicHolidayDto`

**Response:** `200 OK` - `PublicHolidayDto` or `404 Not Found`

### Delete Public Holiday

**DELETE** `/api/business-hours/holidays/{id}`

**Authorization:** SuperAdmin, Admin

**Response:** `204 No Content` or `404 Not Found`

---

## 5. Escalation Rules API

**Base Path:** `/api/escalation-rules`

### Get Escalation Rules

**GET** `/api/escalation-rules`

**Query Parameters:**
- `triggerType` (string, optional): Filter by trigger type (time-based, status-based, condition-based)
- `entityType` (string, optional): Filter by entity type
- `isActive` (bool, optional): Filter by active status

**Response:** `200 OK` - List of `EscalationRuleDto`

### Get Escalation Rule by ID

**GET** `/api/escalation-rules/{id}`

**Response:** `200 OK` - `EscalationRuleDto` or `404 Not Found`

### Get Applicable Rules

**GET** `/api/escalation-rules/applicable`

**Query Parameters:**
- `entityType` (string, required)
- `currentStatus` (string, required)
- `partnerId` (Guid, optional)
- `departmentId` (Guid, optional)
- `orderType` (string, optional)

**Response:** `200 OK` - List of `EscalationRuleDto`

### Create Escalation Rule

**POST** `/api/escalation-rules`

**Authorization:** SuperAdmin, Admin

**Request Body:** `CreateEscalationRuleDto`

**Response:** `201 Created` - `EscalationRuleDto`

### Update Escalation Rule

**PUT** `/api/escalation-rules/{id}`

**Authorization:** SuperAdmin, Admin

**Request Body:** `UpdateEscalationRuleDto`

**Response:** `200 OK` - `EscalationRuleDto` or `404 Not Found`

### Delete Escalation Rule

**DELETE** `/api/escalation-rules/{id}`

**Authorization:** SuperAdmin, Admin

**Response:** `204 No Content` or `404 Not Found`

---

## 6. Guard Condition Definitions API

**Base Path:** `/api/guard-condition-definitions`

### Get Guard Condition Definitions

**GET** `/api/guard-condition-definitions`

**Query Parameters:**
- `entityType` (string, optional): Filter by entity type
- `isActive` (bool, optional): Filter by active status

**Response:** `200 OK` - List of `GuardConditionDefinitionDto`

### Get Guard Condition Definition by ID

**GET** `/api/guard-condition-definitions/{id}`

**Response:** `200 OK` - `GuardConditionDefinitionDto` or `404 Not Found`

### Create Guard Condition Definition

**POST** `/api/guard-condition-definitions`

**Authorization:** SuperAdmin, Admin

**Request Body:** `CreateGuardConditionDefinitionDto`

**Response:** `201 Created` - `GuardConditionDefinitionDto`

### Update Guard Condition Definition

**PUT** `/api/guard-condition-definitions/{id}`

**Authorization:** SuperAdmin, Admin

**Request Body:** `UpdateGuardConditionDefinitionDto`

**Response:** `200 OK` - `GuardConditionDefinitionDto` or `404 Not Found`

### Delete Guard Condition Definition

**DELETE** `/api/guard-condition-definitions/{id}`

**Authorization:** SuperAdmin, Admin

**Response:** `204 No Content` or `404 Not Found`

---

## 7. Side Effect Definitions API

**Base Path:** `/api/side-effect-definitions`

### Get Side Effect Definitions

**GET** `/api/side-effect-definitions`

**Query Parameters:**
- `entityType` (string, optional): Filter by entity type
- `isActive` (bool, optional): Filter by active status

**Response:** `200 OK` - List of `SideEffectDefinitionDto`

### Get Side Effect Definition by ID

**GET** `/api/side-effect-definitions/{id}`

**Response:** `200 OK` - `SideEffectDefinitionDto` or `404 Not Found`

### Create Side Effect Definition

**POST** `/api/side-effect-definitions`

**Authorization:** SuperAdmin, Admin

**Request Body:** `CreateSideEffectDefinitionDto`

**Response:** `201 Created` - `SideEffectDefinitionDto`

### Update Side Effect Definition

**PUT** `/api/side-effect-definitions/{id}`

**Authorization:** SuperAdmin, Admin

**Request Body:** `UpdateSideEffectDefinitionDto`

**Response:** `200 OK` - `SideEffectDefinitionDto` or `404 Not Found`

### Delete Side Effect Definition

**DELETE** `/api/side-effect-definitions/{id}`

**Authorization:** SuperAdmin, Admin

**Response:** `204 No Content` or `404 Not Found`

---

## Error Responses

All endpoints may return the following error responses:

- **400 Bad Request**: Invalid request data
- **401 Unauthorized**: Missing or invalid authentication token
- **403 Forbidden**: Insufficient permissions
- **404 Not Found**: Resource not found
- **500 Internal Server Error**: Server error

Error response format:

```json
{
  "success": false,
  "message": "Error description",
  "errors": ["Detailed error messages"]
}
```

---

## Integration Notes

### SLA Integration

SLA profiles are automatically applied when order status changes. The system:
1. Determines effective SLA profile based on order context
2. Calculates elapsed time (excluding non-business hours if configured)
3. Tracks SLA breaches
4. Sends notifications if configured

### Automation Rules Integration

Automation rules are executed after order status changes. Supported actions:
- **auto-assign**: Automatically assign order to service installer
- **auto-escalate**: Escalate order to role/user
- **auto-notify**: Send notifications to roles/users
- **auto-status-change**: Automatically change order status

### Approval Workflows Integration

Approval workflows are checked when:
- Creating reschedule requests (EmailSendingService)
- Creating RMA requests (RMAService)

If an applicable workflow is found, the entity status is set to "PendingApproval".

### Business Hours Integration

Business hours are used in:
- SLA calculation (exclude non-business hours)
- Escalation rule time-based triggers
- Automation rule scheduling

### Escalation Rules Integration

Escalation rules are checked after order status changes. They can:
- Escalate to a role (notify all users with that role)
- Escalate to a specific user
- Change order status automatically

---

## See Also

- [SLA Configuration Usage Guide](../02_modules/sla-configuration/USAGE.md)
- [Automation Rules Usage Guide](../02_modules/automation-rules/USAGE.md)
- [Approval Workflows Usage Guide](../02_modules/approval-workflows/USAGE.md)
- [Business Hours Usage Guide](../02_modules/business-hours/USAGE.md)
- [Escalation Rules Usage Guide](../02_modules/escalation-rules/USAGE.md)


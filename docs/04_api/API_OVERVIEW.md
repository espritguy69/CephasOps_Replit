# CephasOps – API Overview

**Version:** 1.0  
**Date:** December 2025  
**Status:** Production System

---

## 1. API Principles

### 1.1 RESTful Conventions
- **Resource-based URLs:** `/api/orders`, `/api/invoices`
- **HTTP Methods:** GET (read), POST (create), PUT (update), DELETE (delete)
- **JSON Payloads:** All requests and responses use JSON
- **Status Codes:** Standard HTTP status codes (200, 201, 400, 401, 403, 404, 500)

### 1.2 Authentication
- **JWT Bearer Token:** All authenticated endpoints require `Authorization: Bearer <token>`
- **Token Expiration:** Tokens expire after configured time
- **Refresh Token:** Use `/api/auth/refresh` to refresh expired tokens

### 1.3 Authorization
- **Role-Based Access Control (RBAC):** Endpoints check user roles
- **Permission-Based:** Fine-grained permissions for specific operations
- **Department Scoping:** Data filtered by user's active department

### 1.4 Standard Response Envelope
All API responses follow this structure:
```json
{
  "success": true,
  "message": "Optional status message",
  "data": {},
  "errors": []
}
```

### 1.5 Error Response Format
```json
{
  "success": false,
  "message": "Error description",
  "errors": [
    {
      "field": "fieldName",
      "message": "Error message"
    }
  ]
}
```

---

## 2. Authentication & User Context

### 2.1 Authentication Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/auth/login` | Login and get JWT token |
| POST | `/api/auth/refresh` | Refresh expired token |
| GET | `/api/auth/me` | Get current user info |

### 2.2 User Context
- **Current User:** Available via `ICurrentUserService`
- **Company ID:** Automatically injected from user context
- **Department ID:** Automatically injected from active department
- **User ID:** Available for audit logging

---

## 3. Standard Request/Response Conventions

### 3.1 Pagination
```json
{
  "page": 1,
  "pageSize": 20,
  "totalCount": 100,
  "totalPages": 5,
  "data": []
}
```

### 3.2 Filtering
- **Query Parameters:** `?status=Pending&partnerId=xxx&fromDate=2025-01-01`
- **Common Filters:** status, partnerId, assignedSiId, buildingId, dateRange

### 3.3 Sorting
- **Query Parameters:** `?sortBy=createdAt&sortOrder=desc`
- **Default:** Usually by `createdAt` descending

### 3.4 Field Selection
- **Future:** Support `?fields=id,name,status` for field selection

---

## 4. Error Schema

### 4.1 Validation Errors (400)
```json
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    {
      "field": "email",
      "message": "Email is required"
    }
  ]
}
```

### 4.2 Authorization Errors (401/403)
```json
{
  "success": false,
  "message": "Unauthorized" // or "Forbidden"
}
```

### 4.3 Not Found Errors (404)
```json
{
  "success": false,
  "message": "Resource not found"
}
```

### 4.4 Server Errors (500)
```json
{
  "success": false,
  "message": "An error occurred while processing your request"
}
```

---

## 5. Key API Groups

### 5.1 Orders API

**Base Path:** `/api/orders`

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/orders` | List orders (with filters) |
| GET | `/api/orders/{id}` | Get order details |
| POST | `/api/orders` | Create order |
| PUT | `/api/orders/{id}` | Update order |
| POST | `/api/orders/{id}/assign` | Assign SI |
| POST | `/api/orders/{id}/transition` | Status transition |
| GET | `/api/orders/{orderId}/checklist` | Get checklist |
| POST | `/api/orders/{orderId}/checklist/answers` | Submit checklist answers |

### 5.2 Workflow API

**Base Path:** `/api/workflow` and `/api/workflow-definitions`

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/workflow-definitions` | List workflows |
| GET | `/api/workflow-definitions/{id}` | Get workflow |
| POST | `/api/workflow-definitions` | Create workflow |
| PUT | `/api/workflow-definitions/{id}` | Update workflow |
| POST | `/api/workflow/{orderId}/transition` | Execute transition |
| GET | `/api/workflow/guard-conditions` | List guard conditions |
| GET | `/api/workflow/side-effects` | List side effects |

### 5.3 Scheduler API

**Base Path:** `/api/scheduler`

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/scheduler/slots` | Get schedule slots |
| POST | `/api/scheduler/slots` | Create schedule slot |
| PUT | `/api/scheduler/slots/{id}` | Update slot |
| GET | `/api/scheduler/unassigned` | Get unassigned orders |
| GET | `/api/scheduler/si-availability/{siId}` | Get SI availability |
| POST | `/api/scheduler/si-leave` | Create leave request |
| POST | `/api/scheduler/block-order` | Block order |

### 5.4 Inventory API

**Base Path:** `/api/inventory`

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/inventory/materials` | List materials |
| GET | `/api/inventory/stock` | Get stock levels |
| POST | `/api/inventory/movement` | Record stock movement |
| GET | `/api/inventory/serial/{serialNo}` | Lookup serial |
| POST | `/api/inventory/serial/assign` | Assign serial to order |

### 5.5 Billing API

**Base Path:** `/api/billing`

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/billing/invoices` | List invoices |
| GET | `/api/billing/invoices/{id}` | Get invoice |
| POST | `/api/billing/invoices` | Create invoice |
| POST | `/api/billing/invoices/{id}/submit` | Submit to portal |
| POST | `/api/billing/payments` | Record payment |
| GET | `/api/billing/ageing` | Ageing report |

### 5.6 Parser API

**Base Path:** `/api/parser` and `/api/emails`

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/email/ingest` | Trigger email ingestion |
| GET | `/api/emails` | List emails |
| GET | `/api/parser/sessions` | List parse sessions |
| GET | `/api/parser/sessions/{id}` | Get session details |
| POST | `/api/parser/sessions/{id}/approve` | Approve draft |
| POST | `/api/parser/sessions/{id}/reject` | Reject draft |

### 5.7 Payroll API

**Base Path:** `/api/payroll`

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/payroll/periods` | List payroll periods |
| GET | `/api/payroll/runs` | List payroll runs |
| POST | `/api/payroll/runs` | Create payroll run |
| GET | `/api/payroll/earnings` | Get SI earnings |
| POST | `/api/payroll/runs/{id}/finalise` | Finalize payroll |

### 5.8 P&L API

**Base Path:** `/api/pnl`

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/pnl/summary` | Get P&L summary |
| GET | `/api/pnl/orders` | Get per-order P&L |
| GET | `/api/pnl/drilldown` | P&L drilldown |
| GET | `/api/pnl/overheads` | List overheads |
| POST | `/api/pnl/overheads` | Create overhead |
| POST | `/api/pnl/rebuild` | Rebuild P&L |

### 5.9 Settings API

**Base Path:** `/api/settings/*` and entity-specific endpoints

| Category | Endpoints |
|----------|-----------|
| **Partners** | `/api/partners`, `/api/partner-groups` |
| **Departments** | `/api/departments` |
| **Service Installers** | `/api/service-installers` |
| **Materials** | `/api/materials`, `/api/material-categories` |
| **Buildings** | `/api/buildings`, `/api/splitters` |
| **Workflow** | `/api/workflow-definitions`, `/api/order-statuses` |
| **Templates** | `/api/document-templates`, `/api/email-templates` |
| **System** | `/api/global-settings`, `/api/kpi-profiles` |

### 5.10 Notifications API

**Base Path:** `/api/notifications`

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/notifications/my` | Get user notifications |
| GET | `/api/notifications/my/unread-count` | Unread count |
| POST | `/api/notifications/{id}/read` | Mark as read |

---

## 6. Endpoint Lifecycle Diagrams

### 6.1 Order Creation Flow

```
POST /api/orders
    ↓
Validate Request (DTO validation)
    ↓
Check Permissions (Create Order)
    ↓
OrderService.CreateOrderAsync()
    ↓
Validate Business Rules
    ↓
Create Order Entity
    ↓
Save to Database
    ↓
Return OrderDto (201 Created)
```

### 6.2 Status Transition Flow

```
POST /api/workflow/{orderId}/transition
    ↓
Validate Request
    ↓
Check Permissions (Transition Order)
    ↓
WorkflowService.ExecuteTransitionAsync()
    ↓
Validate Guard Conditions
    ↓
Execute Side Effects
    ↓
Update Order Status
    ↓
Create OrderStatusLog
    ↓
Return Success (200 OK)
```

### 6.3 Invoice Creation Flow

```
POST /api/billing/invoices
    ↓
Validate Request
    ↓
Check Permissions (Create Invoice)
    ↓
BillingService.CreateInvoiceAsync()
    ↓
Validate Order Status (ReadyForInvoice)
    ↓
Calculate Invoice Amounts
    ↓
Generate Invoice PDF
    ↓
Create Invoice Entity
    ↓
Update Order Status
    ↓
Return InvoiceDto (201 Created)
```

---

## 7. API Client Integration

### 7.1 Frontend API Client

**Location:** `frontend/src/api/client.ts`

**Features:**
- Base URL configuration
- JWT token injection
- Department ID injection
- Error handling
- Response envelope handling

### 7.2 API Module Structure

**Pattern:** One module per domain
- `orders.ts` - Order API calls
- `billing.ts` - Billing API calls
- `scheduler.ts` - Scheduler API calls
- etc.

### 7.3 TanStack Query Integration

**Pattern:** Hooks for data fetching
- `useOrders()` - Fetch orders
- `useCreateOrder()` - Create order mutation
- `useUpdateOrder()` - Update order mutation

---

## 8. API Versioning

### 8.1 Current Status
- **No Versioning:** All endpoints under `/api/*`
- **Future:** Consider `/api/v1/*` for versioning

### 8.2 Breaking Changes
- **Policy:** Avoid breaking changes
- **Communication:** Document all changes
- **Deprecation:** Deprecate before removing

---

## 9. Rate Limiting & Throttling

### 9.1 Current Status
- **No Rate Limiting:** Currently not implemented
- **Future:** Add rate limiting for production

### 9.2 Recommendations
- **Per-User Limits:** Limit requests per user
- **Per-Endpoint Limits:** Different limits per endpoint
- **IP-Based Limits:** Prevent abuse

---

## 10. API Documentation

### 10.1 Swagger/OpenAPI
- **Endpoint:** `/swagger` (development)
- **OpenAPI Spec:** Available at `/swagger/v1/swagger.json`
- **Interactive UI:** Swagger UI for testing

### 10.2 Documentation Standards
- **XML Comments:** Document all endpoints
- **Request/Response Examples:** Include in Swagger
- **Error Codes:** Document all possible errors

---

## 11. API Response Envelope Migration Status

**Updated from:** `API_RESPONSE_ENVELOPE_MIGRATION.md`

**✅ STATUS: COMPLETE - ALL ENDPOINTS MIGRATED**  
**Date:** December 2025  
**Completion Date:** December 2025

### Migration Overview

All API endpoints have been migrated to use the standard `ApiResponse<T>` envelope format:

```json
{
  "success": true,
  "message": "Optional status message",
  "data": {},
  "errors": []
}
```

### Infrastructure Created

✅ **Backend Infrastructure:**
- `backend/src/CephasOps.Api/Common/ApiResponse.cs` - Response envelope class
- `backend/src/CephasOps.Api/Common/ControllerExtensions.cs` - Helper extension methods

✅ **Frontend Infrastructure:**
- Both `frontend/src/api/client.ts` and `frontend-si/src/api/client.ts` updated to automatically unwrap `ApiResponse<T>` envelopes
- Backward compatible: handles both envelope format and legacy direct responses

### Migration Status

**Total Controllers Migrated:** 78 controllers  
**Total Endpoints Migrated:** 500+ endpoints  
**Status:** All API endpoints now use the standard `ApiResponse<T>` envelope

### Migration Pattern

**Backend Changes:**
1. Add using statement: `using CephasOps.Api.Common;`
2. Update return types: `ActionResult<ApiResponse<OrderDto>>`
3. Update ProducesResponseType attributes
4. Use extension methods: `return this.Success(order, "Order retrieved successfully");`
5. Error responses: `return this.BadRequest<OrderDto>(ex.Message);`

**Frontend Changes:**
✅ Already Complete: Both frontend API clients automatically unwrap `ApiResponse<T>` envelopes. No changes needed in individual API modules.

### Excluded Endpoints

File download endpoints intentionally return binary data directly (not wrapped in `ApiResponse<T>`):
- Export endpoints (Excel, PDF, templates)
- File download endpoints
- These return `FileContentResult` directly, which is the correct approach for binary data.

### Benefits Achieved

1. **Consistency:** All API responses follow the same structure
2. **Error Handling:** Standardized error responses make frontend error handling easier
3. **Type Safety:** Strongly-typed response envelopes improve developer experience
4. **Documentation:** Swagger documentation accurately reflects response structure
5. **Maintainability:** Centralized response handling makes future changes easier

---

**Document Status:** This API overview reflects the current production system as of December 2025.


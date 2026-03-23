# System Logging – Entities & Relationships (Full Production)

## 1. Overview
Centralised event logging for:
- Orders
- WorkOrders
- Background jobs
- API calls
- Security events
- Email parser events
- User actions

---

## 2. Entities

### 2.1 `SystemLog`
Universal log entry.

Fields:
- `id` (PK)
- `tenant_id` (FK, nullable)
- `company_id` (FK, nullable)
- `level` (INFO / WARN / ERROR / SECURITY / DEBUG)
- `category` (Order, WorkOrder, Parser, Auth, JobRunner, Material, Docket)
- `message`
- `context_json`
- `entity_type` (Order / WorkOrder / User / EmailMessage / ParseSession / JobRun)
- `entity_id`
- `created_at`
- `created_by_user_id` (FK, nullable)

---

### 2.2 `AuditEvent`
Tracks security-sensitive events.

Fields:
- `id` (PK)
- `tenant_id`, `company_id`
- `user_id` (FK → User)
- `event_type` (Login / Logout / PermissionChange / DataExport / AdminAction)
- `ip_address`
- `user_agent`
- `details_json`
- `created_at`

---

### 2.3 `ApiRequestLog`
Tracks incoming/outgoing API calls.

Fields:
- `id` (PK)
- `tenant_id`, `company_id`
- `method` (GET / POST / etc.)
- `url`
- `request_headers_json`
- `request_body_json`
- `response_status`
- `response_body_json`
- `duration_ms`
- `created_at`


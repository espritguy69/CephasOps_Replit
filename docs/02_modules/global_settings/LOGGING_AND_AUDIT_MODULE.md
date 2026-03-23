# LOGGING_AND_AUDIT_MODULE.md
System Logging & Audit – Full Backend Specification

**Implementation status (Feb 2026):** AuditLog entity, table, `IAuditLogService`/`AuditLogService`, and `GET /api/logs/audit` are implemented. Order status changes via WorkflowEngine write to AuditLog (Action=StatusChanged, FieldChangesJson). SystemLog table exists; optional LoggingService for SystemLog and ApiRequestLog are not yet implemented.

---

## 1. Purpose

Provide structured logging and audit trails for:

- Debugging and production support
- Security / compliance (who changed what, when)
- Partner / TIME audits

This is complementary to application-level logging (Serilog, etc.) and **persists important events in the database**.

---

## 2. Data Model

### 2.1 SystemLog

General-purpose system events.

Fields:

- `Id`
- `Timestamp`
- `Level` (enum: Trace, Debug, Info, Warn, Error, Fatal)
- `Category` (string: Parser, Orders, Scheduler, Inventory, Billing, Payroll, PnL, Settings, Auth)
- `Message`
- `DetailsJson` (optional structured payload)
- `CorrelationId` (for tracing)
- `CompanyId` (nullable – system-wide events may not have company)
- `UserId` (nullable)
- `ServiceInstallerId` (nullable)
- `ExceptionType` (nullable)
- `StackTrace` (nullable)

Indexes:

- `(Timestamp DESC)`
- `(CompanyId, Timestamp DESC)`
- `(Category, Timestamp DESC)`

---

### 2.2 AuditLog

Represents **business-level changes** to important entities.

Fields:

- `Id`
- `Timestamp`
- `CompanyId`
- `UserId` (or `System`)
- `EntityType` (string: Order, Invoice, Material, KpiProfile, GlobalSetting, CompanySetting, etc.)
- `EntityId`
- `Action` (Created, Updated, Deleted, StatusChanged, Login, Logout)
- `FieldChangesJson` (array of `{ field, oldValue, newValue }`)
- `Channel` (AdminWeb, SIApp, Api, BackgroundJob)
- `IpAddress` (optional)
- `MetadataJson` (extra context)

Indexes:

- `(CompanyId, EntityType, EntityId)`
- `(CompanyId, Timestamp DESC)`
- `(UserId, Timestamp DESC)`

---

### 2.3 ApiRequestLog (optional v1, but designed now)

Fields:

- `Id`
- `Timestamp`
- `CompanyId` (if derivable)
- `UserId`
- `HttpMethod`
- `Path`
- `StatusCode`
- `DurationMs`
- `IpAddress`
- `UserAgent`
- `RequestBodyTruncated` (optional, sanitized)
- `ResponseBodyTruncated` (optional, sanitized)
- `CorrelationId`

---

## 3. Middleware & Services

### 3.1 LoggingService

Wrapper used by other modules to write `SystemLog` and `AuditLog`.

Methods:

- `Task LogSystemAsync(level, category, message, details, companyId, userId, correlationId)`
- `Task LogAuditAsync(companyId, userId, entityType, entityId, action, fieldChanges, channel, metadata)`

FieldChanges can be inferred via EF ChangeTracker or passed explicitly.

---

### 3.2 API Middleware

- Automatically logs API requests into `ApiRequestLog` (or external log sink).
- Adds `CorrelationId` header if missing.

---

## 4. Integration

- Settings changes → AuditLog
- GlobalSettings changes → AuditLog
- Order status changes → AuditLog
- WorkflowEngine denials → SystemLog (Warning/Error)
- Parser failures → SystemLog (Error)
- Background job runs → SystemLog

---

## 5. Security / Access

- Logs are read-only through API.
- Access restricted to:
  - SuperAdmin
  - Technical support roles
  - Selected audit roles

APIs:

- `/api/logs/system`
- `/api/logs/audit`
- Filters: `companyId`, `entityType`, `userId`, `dateFrom`, `dateTo`, `level`, `category`.


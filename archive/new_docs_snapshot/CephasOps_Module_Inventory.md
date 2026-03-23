# CephasOps – Module Inventory Summary

**Version:** 1.0  
**Date:** December 2025  
**Status:** Production System

---

## Module Overview

This document provides a comprehensive inventory of all modules in CephasOps, including their purpose, entities, services, API endpoints, workflows, and dependencies.

---

## 1. Workflow Engine Module

### Purpose
Enforces business rules and validates status transitions across all order lifecycles.

### Major Entities
- `WorkflowDefinition` - Workflow configuration per department
- `WorkflowTransition` - Allowed status transitions
- `GuardCondition` - Validation rules
- `SideEffect` - Automated actions on transitions
- `GuardConditionDefinition` - Guard condition templates
- `SideEffectDefinition` - Side effect templates

### Key Application Services
- `IWorkflowService` / `WorkflowService` - Workflow execution
- `IWorkflowDefinitionService` - Workflow configuration
- `IGuardConditionValidator` - Validation logic
- `ISideEffectExecutor` - Side effect execution

### Key API Endpoints
- `GET /api/workflow-definitions` - List workflows
- `GET /api/workflow-definitions/{id}` - Get workflow details
- `POST /api/workflow-definitions` - Create workflow
- `PUT /api/workflow-definitions/{id}` - Update workflow
- `POST /api/workflow/{orderId}/transition` - Execute transition
- `GET /api/workflow/guard-conditions` - List guard conditions
- `GET /api/workflow/side-effects` - List side effects

### Important Workflows
- Order status transitions (Pending → Assigned → Completed)
- Checklist validation
- Splitter usage validation
- Material requirement validation
- Department transfer rules

### Dependencies
- **Orders Module:** Status transitions
- **Settings Module:** Workflow definitions
- **Inventory Module:** Material validation

---

## 2. Order Engine Module

### Purpose
Manages the complete order lifecycle from creation to completion.

### Major Entities
- `Order` - Central order entity
- `OrderStatusLog` - Status change history
- `OrderReschedule` - Reschedule requests
- `OrderDocket` - Docket files
- `OrderMaterialUsage` - Material consumption
- `OrderBlocker` - Blocker records
- `OrderStatusChecklistItem` - Checklist items per status
- `OrderStatusChecklistAnswer` - Checklist answers
- `OrderType` - Order type definitions
- `InstallationType` - Installation type definitions

### Key Application Services
- `IOrderService` / `OrderService` - Order CRUD and lifecycle
- `IOrderStatusChecklistService` - Checklist management
- `IOrderRescheduleService` - Reschedule handling

### Key API Endpoints
- `GET /api/orders` - List orders (with filters)
- `GET /api/orders/{id}` - Get order details
- `POST /api/orders` - Create order
- `PUT /api/orders/{id}` - Update order
- `POST /api/orders/{id}/assign` - Assign SI
- `POST /api/orders/{id}/transition` - Status transition
- `GET /api/orders/{orderId}/checklist` - Get checklist
- `POST /api/orders/{orderId}/checklist/answers` - Submit answers

### Important Workflows
- Order creation (from parser or manual)
- Status transitions (via workflow engine)
- SI assignment
- Material usage tracking
- Docket upload and validation
- Blocker resolution

### Dependencies
- **Workflow Engine:** Status transitions
- **Scheduler Module:** SI assignment
- **Inventory Module:** Material usage
- **Billing Module:** Invoice creation
- **Parser Module:** Order creation

---

## 3. Rate Engine Module

### Purpose
Calculates SI payouts and partner billing rates.

### Major Entities
- `RateCard` - Rate card definitions
- `RateCardLine` - Rate card line items
- `GponSiJobRate` - SI job rates
- `GponPartnerJobRate` - Partner job rates
- `GponSiCustomRate` - Custom SI rates
- `CustomRate` - Custom rate overrides

### Key Application Services
- `IRateService` / `RateService` - Rate calculation
- `IBillingRatecardService` - Billing rate cards

### Key API Endpoints
- `GET /api/rates` - List rates
- `GET /api/rates/si/{siId}` - Get SI rates
- `GET /api/rates/partner/{partnerId}` - Get partner rates
- `POST /api/rates` - Create rate
- `PUT /api/rates/{id}` - Update rate
- `GET /api/billing-ratecards` - List billing rate cards

### Important Workflows
- SI rate calculation (base + bonus - penalty)
- Partner billing rate lookup
- Custom rate override application
- KPI-based bonus/penalty calculation

### Dependencies
- **Orders Module:** Order type and completion data
- **ServiceInstallers Module:** SI level and details
- **Partners Module:** Partner configuration
- **Payroll Module:** Earnings calculation

---

## 4. KPI Engine Module

### Purpose
Measures and tracks performance metrics for orders, SIs, and departments.

### Major Entities
- `KpiProfile` - KPI profile definitions
- KPI calculations (derived from orders, not stored)

### Key Application Services
- `IKpiProfileService` - KPI profile management
- KPI calculation logic (in OrderService, PayrollService)

### Key API Endpoints
- `GET /api/kpi-profiles` - List KPI profiles
- `GET /api/kpi-profiles/{id}` - Get KPI profile
- `POST /api/kpi-profiles` - Create KPI profile
- `PUT /api/kpi-profiles/{id}` - Update KPI profile
- `GET /api/orders/kpi` - Get order KPIs
- `GET /api/si/{siId}/kpi` - Get SI KPIs

### Important Workflows
- On-time completion tracking
- Docket quality metrics
- SI performance scoring
- Department-level KPIs

### Dependencies
- **Orders Module:** Order completion data
- **ServiceInstallers Module:** SI performance
- **Settings Module:** KPI profile definitions

---

## 5. GPON Module

### Purpose
GPON-specific configurations and deployments.

### Major Entities
- GPON deployment configurations (via DTOs)

### Key Application Services
- `IGponDeploymentService` - GPON deployment management

### Key API Endpoints
- GPON-specific endpoints (if any)

### Important Workflows
- GPON department configuration
- GPON-specific workflow definitions

### Dependencies
- **Orders Module:** GPON orders
- **Settings Module:** GPON configurations

---

## 6. Materials Module

### Purpose
Manages material catalog, stock levels, and material movements.

### Major Entities
- `Material` - Material catalog
- `MaterialCategory` - Material categories
- `StockBalance` - Stock levels by location
- `StockMovement` - Stock movement history
- `SerialisedItem` - Serialized item tracking
- `StockLocation` - Storage locations (warehouse, SI bag, customer, RMA)

### Key Application Services
- `IInventoryService` - Inventory management
- `IMaterialService` - Material catalog
- `IStockMovementService` - Stock movements

### Key API Endpoints
- `GET /api/inventory/materials` - List materials
- `GET /api/inventory/stock` - Get stock levels
- `POST /api/inventory/movement` - Record stock movement
- `GET /api/inventory/serial/{serialNo}` - Lookup serial
- `POST /api/inventory/serial/assign` - Assign serial to order

### Important Workflows
- Material allocation to SI
- Stock movement tracking
- Serial number assignment
- RMA material return

### Dependencies
- **Orders Module:** Material usage
- **RMA Module:** Material returns
- **Buildings Module:** Building default materials

---

## 7. Finance Module

### Purpose
Manages billing, invoicing, payments, and financial reporting.

### Major Entities
- `Invoice` - Invoice records
- `InvoiceLineItem` - Invoice line items
- `Payment` - Payment records
- `SupplierInvoice` - Supplier invoices
- `SupplierInvoiceLineItem` - Supplier invoice lines

### Key Application Services
- `IBillingService` - Invoice creation and management
- `IPaymentService` - Payment processing
- `ISupplierInvoiceService` - Supplier invoice management
- `IInvoiceSubmissionService` - Portal submission

### Key API Endpoints
- `GET /api/billing/invoices` - List invoices
- `GET /api/billing/invoices/{id}` - Get invoice
- `POST /api/billing/invoices` - Create invoice
- `POST /api/billing/invoices/{id}/submit` - Submit to portal
- `POST /api/billing/payments` - Record payment
- `GET /api/billing/ageing` - Ageing report
- `GET /api/accounting/supplier-invoices` - List supplier invoices

### Important Workflows
- Invoice creation from orders
- Portal submission
- Payment recording
- Invoice rejection handling
- Supplier invoice management

### Dependencies
- **Orders Module:** Order completion data
- **Partners Module:** Partner billing rules
- **P&L Module:** Revenue calculation

---

## 8. Approval Workflows Module

### Purpose
Manages approval workflows for various operations.

### Major Entities
- `ApprovalWorkflow` - Approval workflow definitions
- Approval records (stored in related entities)

### Key Application Services
- `IApprovalWorkflowService` - Approval workflow management

### Key API Endpoints
- `GET /api/approval-workflows` - List workflows
- `POST /api/approval-workflows` - Create workflow
- `POST /api/approval-workflows/{id}/approve` - Approve
- `POST /api/approval-workflows/{id}/reject` - Reject

### Important Workflows
- Reschedule approvals
- Override approvals
- Invoice approvals

### Dependencies
- **Orders Module:** Reschedule requests
- **Workflow Engine:** Override approvals
- **Users Module:** Approver roles

---

## 9. Notifications Module

### Purpose
Manages in-app and email notifications.

### Major Entities
- `Notification` - Notification records
- `NotificationSetting` - User notification preferences
- `NotificationTemplate` - Notification templates
- `EmailTemplate` - Email templates
- `SmsTemplate` - SMS templates
- `WhatsAppTemplate` - WhatsApp templates

### Key Application Services
- `INotificationService` / `NotificationService` - Notification creation and sending
- `IEmailSendingService` - Email sending
- `INotificationTemplateService` - Template management

### Key API Endpoints
- `GET /api/notifications/my` - Get user notifications
- `GET /api/notifications/my/unread-count` - Unread count
- `POST /api/notifications/{id}/read` - Mark as read
- `POST /api/email/send` - Send email
- `GET /api/notification-templates` - List templates

### Important Workflows
- Order assignment notifications
- Status change notifications
- VIP email notifications
- System alerts

### Dependencies
- **Orders Module:** Order events
- **Users Module:** User preferences
- **Settings Module:** Notification templates

---

## 10. RMA Module

### Purpose
Manages Return Material Authorization (RMA) requests.

### Major Entities
- `RmaRequest` - RMA request records
- `RmaRequestItem` - RMA line items

### Key Application Services
- `IRmaService` / `RmaService` - RMA management

### Key API Endpoints
- `GET /api/rma` - List RMA requests
- `GET /api/rma/{id}` - Get RMA details
- `POST /api/rma` - Create RMA request
- `PUT /api/rma/{id}` - Update RMA
- `POST /api/rma/{id}/close` - Close RMA

### Important Workflows
- RMA request creation
- Faulty item tracking
- MRA document linkage
- RMA status workflow

### Dependencies
- **Inventory Module:** Serialized items
- **Orders Module:** Order linkage
- **Files Module:** MRA documents

---

## 11. Parser Module

### Purpose
Email ingestion, classification, and parsing into structured order data.

### Major Entities
- `EmailAccount` - Email account configurations
- `EmailMessage` - Email records
- `ParseSession` - Parsing session records
- `ParsedOrderDraft` - Parsed order drafts
- `ParserTemplate` - Parser template definitions
- `ParserRule` - Parser rules
- `VipEmail` - VIP email addresses
- `VipGroup` - VIP email groups
- `EmailTemplate` - Email templates

### Key Application Services
- `IEmailIngestionService` / `EmailIngestionService` - Email fetching
- `IParserService` / `ParserService` - Email parsing
- `IParserTemplateService` - Template management
- `IEmailClassificationService` - Email classification

### Key API Endpoints
- `POST /api/email/ingest` - Trigger email ingestion
- `GET /api/emails` - List emails
- `GET /api/parser/sessions` - List parse sessions
- `GET /api/parser/sessions/{id}` - Get session details
- `POST /api/parser/sessions/{id}/approve` - Approve draft
- `POST /api/parser/sessions/{id}/reject` - Reject draft
- `GET /api/parser-templates` - List templates

### Important Workflows
- Email fetching (POP3/IMAP/O365)
- Email classification
- Excel/PDF/HTML parsing
- Order draft creation
- Duplicate detection
- VIP email notifications

### Dependencies
- **Orders Module:** Order creation
- **Settings Module:** Parser templates
- **Notifications Module:** VIP notifications

---

## 12. Scheduler Module

### Purpose
Manages SI assignments, availability, and scheduling.

### Major Entities
- `ScheduledSlot` - Schedule slot records
- `SiAvailability` - SI availability records
- `SiLeaveRequest` - SI leave requests

### Key Application Services
- `ISchedulerService` / `SchedulerService` - Scheduling logic

### Key API Endpoints
- `GET /api/scheduler/slots` - Get schedule slots
- `POST /api/scheduler/slots` - Create schedule slot
- `PUT /api/scheduler/slots/{id}` - Update slot
- `GET /api/scheduler/unassigned` - Get unassigned orders
- `GET /api/scheduler/si-availability/{siId}` - Get SI availability
- `POST /api/scheduler/si-leave` - Create leave request
- `POST /api/scheduler/block-order` - Block order

### Important Workflows
- SI assignment
- Schedule slot management
- Availability tracking
- Leave management
- Order blocking

### Dependencies
- **Orders Module:** Order assignment
- **ServiceInstallers Module:** SI details

---

## 13. Settings Module

### Purpose
System-wide configuration and master data management.

### Major Entities
- `GlobalSettings` - Global system settings
- `Company` - Company profiles
- `Partner` - Partner configurations
- `PartnerGroup` - Partner group definitions
- `Department` - Department definitions
- `Vertical` - Vertical/industry definitions
- `ServiceInstaller` - SI profiles
- `BuildingType` - Building type definitions
- `InstallationMethod` - Installation method definitions
- `SplitterType` - Splitter type definitions
- `Splitter` - Splitter records
- `MaterialCategory` - Material categories
- `OrderType` - Order type definitions
- `OrderStatus` - Order status definitions (via settings)
- `TimeSlot` - Time slot definitions
- `BusinessHours` - Business hours configuration
- `EscalationRule` - Escalation rules
- `SlaProfile` - SLA profile definitions
- `AutomationRule` - Automation rules
- `DocumentTemplate` - Document templates
- `KpiProfile` - KPI profile definitions
- `PnlType` - P&L type definitions
- `AssetType` - Asset type definitions
- `CostCentre` - Cost center definitions
- `Team` - Team definitions
- `Role` - Role definitions
- `ProductType` - Product type definitions
- `ServicePlan` - Service plan definitions
- `Brand` - Brand definitions
- `Vendor` - Vendor definitions
- `Warehouse` - Warehouse definitions
- `Bin` - Bin/location definitions
- `PaymentTerm` - Payment term definitions
- `TaxCode` - Tax code definitions
- `ReportDefinition` - Report definitions

### Key Application Services
- 40+ settings services (one per entity type)

### Key API Endpoints
- Settings endpoints under `/api/settings/*` and entity-specific endpoints

### Important Workflows
- System configuration
- Master data management
- Workflow definition management
- Parser template management

### Dependencies
- **All Modules:** Settings drive behavior across all modules

---

## 14. Assets Module

### Purpose
Asset management, depreciation, and maintenance tracking.

### Major Entities
- `Asset` - Asset records
- `AssetType` - Asset type definitions
- `AssetDepreciation` - Depreciation records
- `AssetDisposal` - Disposal records
- `AssetMaintenance` - Maintenance records

### Key Application Services
- `IAssetService` / `AssetService` - Asset management
- `IAssetTypeService` - Asset type management
- `IDepreciationService` - Depreciation calculation

### Key API Endpoints
- `GET /api/assets` - List assets
- `GET /api/assets/{id}` - Get asset details
- `POST /api/assets` - Create asset
- `PUT /api/assets/{id}` - Update asset
- `GET /api/assets/depreciation` - Depreciation report
- `GET /api/assets/maintenance` - Maintenance schedule

### Important Workflows
- Asset registration
- Depreciation calculation
- Maintenance scheduling
- Asset disposal

### Dependencies
- **Settings Module:** Asset type definitions

---

## 15. Buildings Module

### Purpose
Building and infrastructure management, including splitters.

### Major Entities
- `Building` - Building records
- `BuildingType` - Building type definitions
- `BuildingBlock` - Building blocks
- `BuildingContact` - Building contacts
- `BuildingDefaultMaterial` - Default materials per building
- `BuildingRules` - Building-specific rules
- `BuildingSplitter` - Building splitter assignments
- `Splitter` - Splitter records
- `SplitterType` - Splitter type definitions
- `SplitterPort` - Splitter port records
- `Infrastructure` - Infrastructure records
- `HubBox` - Hub box records
- `Pole` - Pole records
- `Street` - Street records
- `InstallationMethod` - Installation method definitions

### Key Application Services
- `IBuildingService` / `BuildingService` - Building management
- `ISplitterService` / `SplitterService` - Splitter management
- `IInfrastructureService` - Infrastructure management
- `IBuildingMatchingService` - Building matching logic

### Key API Endpoints
- `GET /api/buildings` - List buildings
- `GET /api/buildings/{id}` - Get building details
- `POST /api/buildings` - Create building
- `PUT /api/buildings/{id}` - Update building
- `GET /api/splitters` - List splitters
- `GET /api/splitters/{id}/ports` - Get splitter ports

### Important Workflows
- Building registration
- Splitter port management
- Building matching (for orders)
- Default material assignment

### Dependencies
- **Orders Module:** Building assignment
- **Inventory Module:** Default materials
- **Settings Module:** Building types, installation methods

---

## 16. Tasks Module

### Purpose
Task management and tracking.

### Major Entities
- `TaskItem` - Task records

### Key Application Services
- `ITaskService` / `TaskService` - Task management

### Key API Endpoints
- `GET /api/tasks` - List tasks
- `GET /api/tasks/{id}` - Get task details
- `POST /api/tasks` - Create task
- `PUT /api/tasks/{id}` - Update task
- `GET /api/tasks/my` - Get my tasks
- `GET /api/tasks/department/{departmentId}` - Get department tasks

### Important Workflows
- Task creation
- Task assignment
- Task status tracking
- Kanban board management

### Dependencies
- **Users Module:** Task assignment
- **Departments Module:** Department tasks

---

## 17. Payroll Module

### Purpose
SI earnings calculation and payroll management.

### Major Entities
- `PayrollPeriod` - Payroll period records
- `PayrollRun` - Payroll run records
- `JobEarningRecord` - Individual job earnings
- `PayrollLine` - Payroll line items
- `SiRatePlan` - SI rate plan definitions

### Key Application Services
- `IPayrollService` / `PayrollService` - Payroll calculation

### Key API Endpoints
- `GET /api/payroll/periods` - List payroll periods
- `GET /api/payroll/runs` - List payroll runs
- `POST /api/payroll/runs` - Create payroll run
- `GET /api/payroll/earnings` - Get SI earnings
- `POST /api/payroll/runs/{id}/finalise` - Finalize payroll

### Important Workflows
- Payroll period creation
- Earnings calculation (base + bonus - penalty)
- Payroll run finalization
- Payment tracking

### Dependencies
- **Orders Module:** Order completion data
- **Rates Module:** SI rate cards
- **ServiceInstallers Module:** SI details
- **P&L Module:** Labour cost calculation

---

## 18. P&L Module

### Purpose
Profit & Loss calculation and reporting.

### Major Entities
- `PnlPeriod` - P&L period records
- `PnlFact` - Aggregated P&L facts
- `PnlDetailPerOrder` - Per-order P&L details
- `PnlType` - P&L type definitions
- `OverheadEntry` - Overhead entries

### Key Application Services
- `IPnlService` / `PnlService` - P&L calculation

### Key API Endpoints
- `GET /api/pnl/summary` - Get P&L summary
- `GET /api/pnl/orders` - Get per-order P&L
- `GET /api/pnl/drilldown` - P&L drilldown
- `GET /api/pnl/overheads` - List overheads
- `POST /api/pnl/overheads` - Create overhead
- `POST /api/pnl/rebuild` - Rebuild P&L

### Important Workflows
- P&L calculation (revenue - costs)
- Overhead allocation
- Period-based reporting
- Department/cost center breakdown

### Dependencies
- **Billing Module:** Revenue data
- **Payroll Module:** Labour costs
- **Inventory Module:** Material costs
- **Orders Module:** Order completion data

---

## 19. Auth Module

### Purpose
Authentication and authorization.

### Major Entities
- `User` - User accounts
- `Role` - Role definitions
- `Permission` - Permission definitions
- `UserRole` - User-role assignments
- `RolePermission` - Role-permission assignments

### Key Application Services
- `IAuthService` / `AuthService` - Authentication
- `ICurrentUserService` - Current user context

### Key API Endpoints
- `POST /api/auth/login` - Login
- `POST /api/auth/refresh` - Refresh token
- `GET /api/auth/me` - Get current user
- `GET /api/users` - List users
- `GET /api/rbac/roles` - List roles
- `GET /api/rbac/permissions` - List permissions

### Important Workflows
- JWT token generation
- Role-based access control
- Permission checking
- User session management

### Dependencies
- **All Modules:** Authorization for all operations

---

## 20. Files Module

### Purpose
File storage and management.

### Major Entities
- `File` - File metadata records

### Key Application Services
- `IFileService` / `FileService` - File management

### Key API Endpoints
- `POST /api/files/upload` - Upload file
- `GET /api/files/{id}` - Download file
- `DELETE /api/files/{id}` - Delete file

### Important Workflows
- File upload
- File download
- File deletion
- File metadata management

### Dependencies
- **All Modules:** File storage for documents, photos, dockets

---

## 21. Documents Module

### Purpose
Document generation (PDF, Excel, Word).

### Major Entities
- Document generation (via services, not entities)

### Key Application Services
- `ISyncfusionBoqGenerator` - BOQ generation
- `ISyncfusionInvoiceGenerator` - Invoice PDF generation
- `IDocumentGenerationService` - Generic document generation

### Key API Endpoints
- `POST /api/documents/generate` - Generate document
- `GET /api/documents/{id}` - Get generated document

### Important Workflows
- Invoice PDF generation
- BOQ Excel generation
- Document template rendering

### Dependencies
- **Billing Module:** Invoice data
- **Orders Module:** Order data
- **Settings Module:** Document templates

---

## 22. Admin Module

### Purpose
System administration and health monitoring.

### Major Entities
- System health (via DTOs)

### Key Application Services
- `IAdminService` / `AdminService` - Admin operations

### Key API Endpoints
- `GET /api/admin/health` - System health
- `GET /api/admin/migrations` - Migration status
- `POST /api/admin/cache/flush` - Flush cache

### Important Workflows
- System health monitoring
- Migration status checking
- Cache management

### Dependencies
- **Infrastructure:** Database, services

---

## 23. Agent Module

### Purpose
Agent mode operations (payment rejection handling, etc.).

### Major Entities
- Agent operations (via DTOs)

### Key Application Services
- `IAgentModeService` / `AgentModeService` - Agent operations

### Key API Endpoints
- `POST /api/agent/payment-rejection` - Handle payment rejection
- `GET /api/agent/status` - Get agent status

### Important Workflows
- Payment rejection handling
- Order status updates (Reinvoice)

### Dependencies
- **Orders Module:** Order status updates
- **Billing Module:** Payment data

---

## Module Dependency Graph

```
Settings Module (drives all)
    │
    ├──→ Workflow Engine
    ├──→ Parser Module
    ├──→ Orders Module
    ├──→ Scheduler Module
    ├──→ Inventory Module
    ├──→ Billing Module
    └──→ All other modules

Orders Module (central hub)
    ├──→ Workflow Engine (status transitions)
    ├──→ Scheduler (SI assignment)
    ├──→ Inventory (material usage)
    ├──→ Billing (invoice creation)
    ├──→ Payroll (SI earnings)
    └──→ P&L (profit calculation)

Workflow Engine
    ├──→ Orders (status validation)
    └──→ Settings (workflow definitions)

Parser Module
    └──→ Orders (order creation)

Billing + Payroll + Inventory
    └──→ P&L (financial calculation)
```

---

**Document Status:** This module inventory reflects the current production system as of December 2025.


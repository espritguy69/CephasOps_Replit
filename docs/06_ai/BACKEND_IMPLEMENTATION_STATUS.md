# Backend Implementation Status Audit

This document compares documented modules/features against actual backend implementation.

**Last Updated:** 2026-02
**Auditor:** AI Assistant  
**Note:** CephasOps runs in **single-company mode** (multiple departments, one company). Many APIs accept `companyId` as `Guid.Empty` for global/single-company context.

---

## ✅ Fully Implemented Modules

### 1. Orders Module ✅
- **Status:** Complete
- **Entities:** Order, OrderStatusLog, OrderReschedule, OrderBlocker, OrderDocket, OrderMaterialUsage, OrderTag, OrderTagAssignment
- **Controllers:** OrdersController, OrderStatusLogsController, OrderReschedulesController, OrderBlockersController, OrderDocketsController, OrderMaterialUsagesController
- **Services:** ✅ Implemented
- **Documentation:** `docs/02_modules/orders/OVERVIEW.md`

### 2. Inventory & RMA Module ✅
- **Status:** Complete
- **Entities:** Material, StockLocation, StockBalance, SerialisedItem, StockMovement, RmaTicket, RmaItem
- **Controllers:** MaterialsController, StockLocationsController, StockMovementsController, SerialisedItemsController, RmaController
- **Services:** ✅ Implemented
- **Documentation:** `docs/02_modules/inventory/OVERVIEW.md`

### 3. Billing & Tax & e-Invoice Module ✅
- **Status:** Complete
- **Entities:** Invoice, InvoiceLine, CreditNote, CreditNoteLine, Payment, PaymentAllocation, BillingRatecard, PartnerAccount, EInvoiceSubmission
- **Controllers:** InvoicesController, CreditNotesController, PaymentsController, RatecardsController, EInvoiceController
- **Services:** ✅ Implemented
- **Documentation:** `docs/02_modules/billing/OVERVIEW.md`

### 4. Scheduler Module ✅
- **Status:** Complete
- **Entities:** ScheduledSlot, SiAvailability, SiLeaveRequest, CalendarDayCapacity
- **Controllers:** SchedulerController
- **Services:** ✅ Implemented
- **Documentation:** `docs/02_modules/scheduler/OVERVIEW.md`

### 5. Payroll Module ✅
- **Status:** Complete
- **Entities:** PayrollPeriod, PayrollRun, PayrollItem, SiRatePlan, SiRatePlanRule
- **Controllers:** PayrollPeriodsController, PayrollRunsController, PayrollItemsController, SiRatePlansController
- **Services:** ✅ Implemented
- **Documentation:** `docs/02_modules/payroll/OVERVIEW.md`

### 6. P&L Module ✅
- **Status:** Complete
- **Entities:** PnlOrderDetail, PnlFact, OverheadEntry
- **Controllers:** PnlOrdersController, PnlSummaryController, PnlRebuildController, OverheadsController
- **Services:** ✅ Implemented
- **Documentation:** `docs/02_modules/pnl/OVERVIEW.md`

### 7. RBAC Module ✅
- **Status:** Complete
- **Entities:** Role, Permission, UserRole, UserCompany, RolePermission
- **Controllers:** RolesController, PermissionsController, UserAccessController
- **Services:** ✅ Implemented
- **Documentation:** `docs/02_modules/rbac/OVERVIEW.md`

### 8. Service Installer App (SI App) Module ✅
- **Status:** Complete
- **Entities:** SiJobSession, SiJobEvent, SiPhoto, SiDeviceScan, SiLocationPing
- **Controllers:** SiAppController
- **Services:** ✅ Implemented
- **Documentation:** `docs/02_modules/service_installer/OVERVIEW.md`

### 9. Email Parser Module ✅
- **Status:** Complete
- **Entities:** EmailAccount, EmailMessage, EmailAttachment, ParseSession, ParsedOrderDraft, ParserTemplate, SnapshotRecord
- **Controllers:** EmailAccountsController, EmailMessagesController, ParserTemplatesController, ParseSessionsController, ParsedOrderDraftsController
- **Services:** ✅ Implemented
- **Documentation:** `docs/02_modules/email_parser/OVERVIEW.md`, `docs/02_modules/email_parser/SETUP.md`, `docs/02_modules/email_parser/SPECIFICATION.md`
- **Health:** `GET /api/admin/health` includes `emailParser` (status, last poll times). See SETUP.md § Monitoring.

### 10. Identity Module ✅
- **Status:** Complete
- **Entities:** User
- **Controllers:** UsersController
- **Services:** ✅ Implemented

### 11. Workflow Engine Module ✅
- **Status:** Complete
- **Entities:** WorkflowDefinition, WorkflowTransition, WorkflowJob
- **Controllers:** OrderWorkflowController (status + lifecycle endpoints)
- **Services:** WorkflowEngine service, guard validators, domain events, transition logging
- **Documentation:** `docs/01_system/WORKFLOW_ENGINE.md`

### 12. Document Templates Module ✅
- **Status:** Complete
- **Entities:** DocumentTemplate, DocumentTemplateVariable, GeneratedDocument, StoredFile
- **Controllers:** DocumentTemplatesController, DocumentsController
- **Services:** Template CRUD, HTML templating, file storage, document generation for invoices/dockets/RMA/PO/quotation/BOQ/delivery order/payment receipt/generic
- **Invoice integration:** BillingController uses IDocumentGenerationService for invoice PDF (`GET /api/billing/invoices/{id}/pdf`) and HTML preview (`GET /api/billing/invoices/{id}/preview-html`). Default invoice template seeded on startup. Optional `?templateId=` on both endpoints.
- **Documentation:** `docs/02_modules/document_generation/DOCUMENT_TEMPLATES_MODULE.md`, `docs/02_modules/document_generation/OVERVIEW.md`

### 13. Background Jobs Infrastructure ✅
- **Status:** Complete
- **Entities:** BackgroundJob, BackgroundJobExecution
- **Worker:** Hosted scheduler (Cron-based) with persistent registry + execution history
- **Jobs:** Email ingestion poller, Snapshot cleanup, P&L rebuild, Scheduler compliance/cleanup
- **Services:** BackgroundJobProcessor, handlers, execution logging, DI-hosted workers
- **Documentation:** `docs/08_infrastructure/background_jobs_infrastructure.md`

### 14. Splitters Management Module ✅
- **Status:** Complete
- **Entities:** Splitter, SplitterPort, SplitterUsageLog, OrderSplitterAllocation
- **Controllers:** `SplittersController` (`/api/splitters`, `/api/splitters/{id}/ports`, `/api/splitter-usage`)
- **Services:** Splitter CRUD, auto port generation, port assignment & release, standby approval enforcement, usage logging
- **Integration:** Orders + SI App (port validation and locking during workflow transitions)
- **Documentation:** `docs/02_modules/splitters/OVERVIEW.md`

### 15. Global Settings Module ✅
- **Status:** Complete
- **Entities:** GlobalSetting
- **Controllers:** GlobalSettingsController
- **Services:** Global settings CRUD + usage in settings services
- **Documentation:** `docs/02_modules/global_settings/GLOBAL_SETTINGS_MODULE.md`

### 16. KPI Profile Module ✅
- **Status:** Complete
- **Entities:** KpiProfile
- **Controllers:** KpiProfilesController
- **Services:** KPI profile CRUD, effective profile lookup, KPI evaluation
- **Documentation:** `docs/05_data_model/entities/settings_entities.md`

### 17. Material Templates Module ✅
- **Status:** Complete
- **Entities:** MaterialTemplate, MaterialTemplateItem
- **Controllers:** MaterialTemplatesController
- **Services:** Template CRUD, effective template lookup, template cloning
- **Documentation:** `docs/02_modules/materials/MATERIAL_TEMPLATES_MODULE.md`

---

## ⚠️ Partially Implemented Modules

### 1. Settings Module ⚠️
- **Status:** Partially Complete
- **What exists:** Company, CompanySetting, Partner, ServiceInstaller, Region, CostCentre, Building/BuildingType entities, plus `CompanySettingsController`
- **Still missing:** module toggles, branding/profile tables described in `docs/02_modules/global_settings/SETTINGS_MODULE.md`
- **Impact:** MEDIUM – Branding and module enablement still require manual setup

### 2. Multi-Company & Companies Setup ⚠️
- **Status:** App runs in **single-company mode**. Core scoping via `CompanyScopedEntity`; many APIs accept `companyId = Guid.Empty` for single-company context.
- **Missing (if multi-company restored):** Company branding, module enablement flags, per-company template management per `docs/02_modules/global_settings/COMPANIES_SETUP.md`, `docs/02_modules/global_settings/MULTI_COMPANY_MODULE.md`
- **Impact:** LOW for current single-company deployment

### 3. ~~Email Ingestion Worker~~ ✅ (Updated)
- **Status:** **Complete.** Background scheduler (`EmailIngestionSchedulerService`), job processor (`BackgroundJobProcessorService`), and full POP3/IMAP polling via `EmailIngestionService` are implemented. Attachment download, ParseSession creation, and draft/order creation are in place.
- **Health:** Parser health exposed at `GET /api/admin/health` under `emailParser`.

---

## ✅ Audit Logging (implemented Feb 2026)

- **Status:** Implemented (core audit trail).
- **Entities:** AuditLog (table + migration); SystemLog / DataChangeLog / ApiRequestLog optional for future.
- **Features:** Audit trails (who did what, when); order status changes → AuditLog; `GET /api/logs/audit` (filterable, paged).
- **Documentation:** `docs/02_modules/global_settings/LOGGING_AND_AUDIT_MODULE.md`; IMPROVEMENT_AND_EVOLUTION_PLAN.md §5.

---

## ❌ Missing or Incomplete Modules

None. (System Logging & Audit core is done; optional SystemLog/ApiRequestLog/DataChangeLog can be added later if needed.)

---

## Summary Statistics

| Category | Count |
|----------|-------|
| ✅ Fully Implemented Modules | 19 |
| ⚠️ Partially Implemented | 2 |
| ❌ Not Implemented | 0 |
| **Total Documented Modules** | **21** |
| **Implementation Rate** | **90%** (19/21 fully complete) |

---

## Priority Implementation Queue

### 🔴 CRITICAL Priority

- (None; Email Ingestion and Audit logging are done.)

### 🟡 Optional / Future

- SystemLog, ApiRequestLog, DataChangeLog — add if required for compliance or diagnostics.

---

## Recommendations

### Immediate Actions (as of Jan 2026):

1. **Secrets management** – Move all credentials out of appsettings/env examples (see production-readiness review).
2. **CI/CD** – Add tests, Docker build, and deployment to pipelines.

### Technical Debt:

- Status changes should go through WorkflowEngine (currently may bypass rules)
- Email credentials: stored on EmailAccount (encrypted optional); CompanySetting pattern also documented in SETUP.md
- Email ingestion: ✅ Implemented (scheduler + POP3/IMAP + jobs)

---

## Notes

- ✅ Document generation + PDF storage via `/api/document-templates` + `/api/documents`.
- ✅ CompanySetting and EmailAccount credential storage (encryption supported).
- ✅ Most core CRUD operations complete; single-company mode is default.
- ✅ Postgres partial-index filters fixed (migration `20251120061706_FixPostgreSQLFilterSyntax`).
- ❌ Workflow enforcement: status changes may bypass WorkflowEngine in some paths.
- ✅ Background jobs: scheduler + EmailIngest handler + POP3/IMAP ingestion implemented.
- ✅ System health: `GET /api/admin/health` includes database and email parser health.

Update this audit after each major implementation milestone.


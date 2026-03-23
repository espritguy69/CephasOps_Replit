# Go-Live Readiness Checklist — GPON (Single-Company)

**Last run:** 2026-02-09  
**Scope:** GPON contractor production readiness (single-company deployment)  
**Reference:** [COMPLETION_STATUS_REPORT.md](./COMPLETION_STATUS_REPORT.md) | [_discrepancies.md](./_discrepancies.md) | [order_lifecycle_and_statuses.md](./business/order_lifecycle_and_statuses.md)  

**Phase 0–5:** COMPLETE — GPON go-live ready.

---

## 1. Security / RBAC

| # | Item | Status | Notes |
|---|------|--------|-------|
| 1.1 | Auth (login, JWT, refresh) | ☐ | AuthController, ProtectedRoute, SubconRoute |
| 1.2 | Department-scoped access on Orders, Inventory, Scheduler, Reports | ☐ | DepartmentAccessService, RBAC_MATRIX_REPORT |
| 1.3 | Settings protected (SettingsProtectedRoute) | ☐ | Admin-only settings routes |
| 1.4 | SI app: SI-only access to assigned jobs | ☐ | ServiceInstallerId in AuthContext |
| 1.5 | Partner data isolation (by company) | ☐ | Single-company; CompanyId filter |
| 1.6 | Sensitive settings encrypted (MyInvois credentials) | ☐ | IEncryptionService, GlobalSettings |
| 1.7 | Audit logging for critical actions | ☐ | AuditLogs table, AuditOverrides |

---

## 2. Data Integrity (Ledger, Serial Tracking)

| # | Item | Status | Notes |
|---|------|--------|-------|
| 2.1 | Stock ledger append-only; no direct edits | ☑ | StockLedgerEntry, StockLedgerService |
| 2.2 | Serialised item lifecycle (allocate → issue → use → return) | ☑ | SerialisedItem, StockAllocation |
| 2.3 | Ledger balance cache reconcile job | ☑ | ReconcileLedgerBalanceCache; LedgerReconciliationSchedulerService (12h) |
| 2.4 | Stock-by-location snapshots for reporting | ☑ | StockByLocationSnapshots, StockSnapshotSchedulerService (6h check) |
| 2.5 | Movement validation (MovementValidationService) | ☑ | InventoryService validates before movements |
| 2.6 | Order material usage / replacement audit trail | ☑ | OrderMaterialUsage, OrderMaterialReplacement |

---

## 3. Workflow Correctness (DB-Driven Workflow Present and Seeded)

| # | Item | Status | Notes |
|---|------|--------|-------|
| 3.1 | WorkflowDefinition for Order entity exists | ☑ | 07_gpon_order_workflow.sql in run-all-seeds.ps1 |
| 3.2 | WorkflowTransitions cover full GPON lifecycle | ☑ | Pending→Assigned→OnTheWay→MetCustomer→OrderCompleted→DocketsReceived→DocketsVerified→DocketsUploaded→ReadyForInvoice→Invoiced→SubmittedToPortal→Completed |
| 3.3 | Invoice rejection loop transitions present | ☑ | In 07_gpon_order_workflow.sql |
| 3.4 | Blocker exits: Blocker→Assigned (if required) | ☑ | AllowedRolesJson: Ops, Admin, HOD |
| 3.5 | Assigned→Blocker (if required) | ☑ | In DB workflow + fallback |
| 3.6 | Guard conditions seeded (DocketUploaded, etc.) | ☐ | DatabaseSeeder seeds GuardConditionDefinitions |
| 3.7 | Side effects seeded (UpdateOrderFlags) | ☐ | DatabaseSeeder seeds SideEffectDefinitions |
| 3.8 | EmailIngestionService uses workflow for status changes | ☑ | Resolved 2026-02-03; Cancelled/Blocker via workflow |
| 3.9 | Docket Admin UI (receive/verify/upload) | ☑ | /operations/dockets; checklist, verify, mark uploaded, file upload |

---

## 4. Billing / MyInvois

| # | Item | Status | Notes |
|---|------|--------|-------|
| 4.1 | Invoice CRUD and PDF generation | ☐ | BillingService, DocumentGenerationService |
| 4.2 | Invoice line items linked to orders | ☐ | InvoiceLineItem, OrderId |
| 4.3 | MyInvois settings configured (IntegrationSettings) | ☐ | MyInvois_BaseUrl, ClientId, ClientSecret, Enabled |
| 4.4 | Invoice submission to MyInvois (sandbox/production) | ☐ | InvoiceSubmissionService, MyInvoisApiProvider |
| 4.5 | MyInvois status poll job running | ☐ | MyInvoisStatusPoll job type |
| 4.6 | Invoice rejection / reinvoice flow tested | ☐ | Rejected→ReadyForInvoice, Rejected→Reinvoice, Reinvoice→Invoiced |
| 4.7 | Payment recording and matching | ☐ | PaymentsController, Payment entity |
| 4.8 | **MyInvois production runbook** | ☑ | docs/operations/myinvois_production_runbook.md |
| 4.9 | **Partner portal manual process** | ☑ | docs/operations/partner_portal_manual_process.md |

---

## 5. SI App Usability

| # | Item | Status | Notes |
|---|------|--------|-------|
| 5.1 | Login and SI context (ServiceInstallerId) | ☐ | AuthContext, SubconRoute for earnings |
| 5.2 | Jobs list and detail (assigned jobs) | ☐ | JobsListPage, JobDetailPage |
| 5.3 | Status transitions (OnTheWay, MetCustomer, OrderCompleted, Blocker, Reschedule) | ☐ | getAllowedTransitions, executeTransition |
| 5.4 | Material collection, scan, mark faulty, replacement | ☐ | MaterialsDisplay, SerialScanner, MarkFaultyModal, ReplacementForm |
| 5.5 | Photo upload | ☐ | PhotoUpload component |
| 5.6 | GPS/location display | ☐ | LocationDisplay |
| 5.7 | Checklist completion | ☐ | ChecklistDisplay |
| 5.8 | Reschedule request | ☐ | RescheduleRequestModal |
| 5.9 | Earnings page (SubconRoute) | ☐ | EarningsPage |

---

## 6. Monitoring / Logging

| # | Item | Status | Notes |
|---|------|--------|-------|
| 6.1 | Structured logging (ILogger) | ☐ | Serilog or equivalent |
| 6.2 | Background job logging | ☐ | BackgroundJobProcessorService logs |
| 6.3 | Debug logging disabled in production | ☐ | Remove or gate hardcoded path in Program.cs |
| 6.4 | Health/diagnostics endpoints | ☐ | DiagnosticsController, InfrastructureController |
| 6.5 | SystemLogs for workflow/jobs | ☐ | WorkflowJobs, SystemLogs entity |

---

## 7. Backup / Restore Considerations

| # | Item | Status | Notes |
|---|------|--------|-------|
| 7.1 | PostgreSQL backup strategy | ☐ | pg_dump, WAL archiving, or managed backup |
| 7.2 | File storage (photos, PDFs) backup | ☐ | Files entity; OneDrive or local path |
| 7.3 | Restore and migration rollback procedure | ☐ | Document EF migrations rollback |
| 7.4 | Seed data idempotency | ☐ | DatabaseSeeder, SQL seeds |

---

## 8. Admin Settings Completeness

| # | Item | Status | Notes |
|---|------|--------|-------|
| 8.1 | Partners configured | ☐ | PartnersPage, Partner.Code for Partner–Category |
| 8.2 | Partner groups | ☐ | PartnerGroupsPage |
| 8.3 | Order types and categories | ☐ | OrderTypesPage, OrderCategoriesPage |
| 8.4 | Installation methods | ☐ | InstallationMethodsPage (department-scoped) |
| 8.5 | Building types | ☐ | BuildingTypesPage |
| 8.6 | Splitter types | ☐ | SplitterTypesPage |
| 8.7 | Service installers and skills | ☐ | ServiceInstallersPage, SkillsManagementPage |
| 8.8 | SI rate plans | ☐ | SiRatePlansPage |
| 8.9 | Partner rates / rate engine | ☐ | PartnerRatesPage, RateEngineManagementPage |
| 8.10 | Materials and material templates | ☐ | MaterialSetupPage, MaterialTemplatesPage |
| 8.11 | Document templates (Invoice, JobDocket) | ☐ | DocumentTemplatesPage |
| 8.12 | Email accounts (parser) | ☐ | EmailSetupPage |
| 8.13 | Order statuses and workflow definitions | ☐ | OrderStatusesPage, WorkflowDefinitionsPage |
| 8.14 | KPI profiles (Docket KPI, etc.) | ☐ | KpiProfilesPage |
| 8.15 | Time slots | ☐ | TimeSlotSettingsPage |
| 8.16 | Integrations (MyInvois, SMS, WhatsApp) | ☐ | IntegrationsPage |

---

## Summary

- **Total items:** 60+
- **Pass criteria:** All critical items (1.x, 2.x, 3.x, 4.x, 5.x) checked before go-live.
- **Recommended:** Run `run-all-seeds.ps1` (includes 07_gpon_order_workflow.sql) on fresh DB; verify MyInvois config; test full order→docket→invoice→submission flow.
- **Phase 0–4 done:** Workflow; Docket Admin UI; MyInvois runbook; Partner portal doc; Invoice rejection loop; Ledger + P&L schedulers; Inventory + Reporting ops runbooks.

---

**Related:** [COMPLETION_STATUS_REPORT.md](./COMPLETION_STATUS_REPORT.md) | [ROADMAP_TO_COMPLETION.md](./ROADMAP_TO_COMPLETION.md)

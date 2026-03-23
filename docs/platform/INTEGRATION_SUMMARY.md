# Full Operations Platform — Integration Summary

**Mission:** Integrate remaining operational modules and formalize CephasOps as a complete ISP contractor operations platform without breaking existing behavior.

---

## A. Module Architecture Map

Created **docs/platform/MODULE_ARCHITECTURE_MAP.md** with:

- **Lead Management:** Parsed drafts, parser; no separate lead entity.
- **Work Orders:** IOrderService, OrderService, OrdersController; OrderCreatedEvent, OrderStatusChangedEvent, OrderAssignedEvent, OrderCompletedEvent.
- **Scheduling:** ISchedulerService, SchedulerController, WorkflowEngineService; workflow/order status and assignment.
- **Installer Workforce:** ServiceInstallers, Tasks, OrderAssignedOperationsHandler; SI app.
- **Inventory:** IInventoryService, IStockLedgerService, OrderMaterialUsageService; InventoryController.
- **Billing:** IBillingService, TenantSubscription, BillingPlansController, InvoiceSubmissionsController.
- **Payroll:** PayrollController, IOrderPayoutSnapshotService, rates, PnL.
- **Integration Bus:** IOutboundIntegrationBus, InboundWebhookRuntime, IntegrationEventForwardingHandler; IntegrationController, WebhooksController.
- **Event Platform:** IEventStore, IEventBus, EventStoreDispatcherHostedService, replay, observability; docs/event-platform/.
- **Automation:** AutomationRule, IAutomationRuleService, OrderCompletedAutomationHandler; AutomationRulesController.
- **Reporting:** ReportsController, ReportRegistry; department/company scope.
- **SaaS:** Tenants, TenantSubscription, BillingPlans; TenantSubscriptionsController.
- **Control Plane:** ControlPlaneController (capability index); event-store, job-orchestration, integration, trace, observability, tenants, billing.

Module boundaries and event flow (who emits what, who consumes) are documented in the map.

---

## B. Domain Events Integrated

| Event | When emitted | Handlers |
|-------|----------------|----------|
| **OrderCreatedEvent** | OrderService.CreateOrderAsync and CreateFromParsedDraftAsync (after save); optional IEventBus. | IntegrationEventForwardingHandler. |
| **OrderCompletedEvent** | WorkflowEngineService when status → OrderCompleted or Completed (same transaction as workflow). | IntegrationEventForwardingHandler, OrderCompletedAutomationHandler. |
| **OrderStatusChangedEvent** | (Existing) WorkflowEngineService. | Ledger, notifications, integration forwarding. |
| **OrderAssignedEvent** | (Existing) WorkflowEngineService when status → Assigned. | OrderAssignedOperationsHandler, integration forwarding. |

**Added in follow-up:** **InvoiceGeneratedEvent** — emitted from BillingService.CreateInvoiceAsync (optional IEventBus); type and legacy name registered; forwarded via IntegrationEventForwardingHandler; allowed for replay.

**Added (autonomous follow-up):** **MaterialIssuedEvent** — emitted from OrderMaterialUsageService.RecordMaterialUsageAsync after save (optional IEventBus; CompanyId from order or parameter); **MaterialReturnedEvent** — emitted from StockLedgerService.ReturnAsync after save (optional IEventBus; CompanyId from parameter); **PayrollCalculatedEvent** — emitted from PayrollService.CreatePayrollRunAsync after SaveChangesAsync (optional IEventBus). All three registered in EventTypeRegistry and EventReplayPolicy; IntegrationEventForwardingHandler forwards them to the outbound integration bus.

---

## C. Automation Engine

- **Existing:** AutomationRule entity (TriggerType, TriggerStatus, ActionType), IAutomationRuleService, AutomationRulesController; GetApplicableRulesAsync(companyId, entityType, currentStatus, ...).
- **Added:** **OrderCompletedAutomationHandler** (IDomainEventHandler&lt;OrderCompletedEvent&gt;): on OrderCompletedEvent, loads order (tenant-scoped), skips if order.InvoiceId already set, gets rules with TriggerType=StatusChange, TriggerStatus=OrderCompleted, then for each rule with ActionType **GenerateInvoice** calls IBillingService.BuildInvoiceLinesFromOrdersAsync and CreateInvoiceAsync, sets order.InvoiceId. Idempotent and tenant-safe.
- **Rule example:** WHEN OrderCompleted THEN GenerateInvoice → create rule with EntityType=Order, TriggerType=StatusChange, TriggerStatus=OrderCompleted, ActionType=GenerateInvoice.
- Documented in **docs/platform/AUTOMATION_ENGINE.md**.

---

## D. Intelligence Engine (Field Operations Intelligence)

- **Added:** **OperationalInsight** entity (CompanyId, Type, PayloadJson, OccurredAtUtc, EntityType, EntityId) and table **OperationalInsights**; **OrderCompletedInsightHandler** (IDomainEventHandler&lt;OrderCompletedEvent&gt;) writes an insight of type "OrderCompleted" with payload { OrderId, WorkflowJobId }. Idempotent handler; tenant-scoped by event CompanyId. Indexes on (CompanyId, OccurredAtUtc) and (CompanyId, Type). Idempotent SQL: **backend/scripts/apply-OperationalInsights-And-FeatureFlags.sql**.
- **Query API:** GET **/api/observability/insights** (companyId?, type?, fromUtc?, toUtc?, page, pageSize); tenant-scoped; **feature-gated** by FeatureFlagKeys.OperationalInsights (tenant from Company; 403 if disabled). Returns items + total.
- **Future:** Additional insight types and handlers (installer productivity anomaly, inventory discrepancy, job backlog prediction, SLA breach risk).

---

## E. Reporting Platform

- **Existing:** ReportsController, ReportRegistry, IOrderService, IInventoryService, ISchedulerService, etc.; run report by key with department/company scope; 403 when user has no access.
- **Tenant safety:** All report runs filter by tenant/department (IDepartmentAccessService, ICurrentUserService). No code changes in this pass; documented that reports must always filter by tenant.

---

## F. SaaS Feature Flags

- **Added:** **IFeatureFlagService**, **BillingPlanFeature** and **TenantFeatureFlag** entities/tables, **PlanBasedFeatureFlagService** (Scoped). **FeatureFlagKeys** static class: Automation, Reports, MultiDepartment, OperationalInsights, Integration, EventReplay (use with BillingPlanFeatures/TenantFeatureFlags).

---

## G. Control Plane Integration

- **Existing:** ControlPlaneController GET /api/admin/control-plane returns capability list: event store, event replay, rebuild, command orchestration, background jobs, job orchestration, system workers, integration, trace, operational trace, observability, observability insights, tenants, billing plans, tenant subscriptions.
- **Added:** **Tenant diagnostics** GET **/api/admin/control-plane/tenant-diagnostics** (optional **?companyId=**): TenantDiagnosticsDto with Links (EventStore, Integration, ObservabilityEvents, **ObservabilityInsights**).

---

## H. Tenant Safety Verification

- **Verified:** Order creation and event emission use CompanyId from context; event store and replay use scopeCompanyId; automation rules and handler are company-scoped; reporting and billing use company/tenant. Documented in **docs/platform/TENANT_SAFETY_VERIFICATION.md**.

---

## I. Documentation Updates

| Document | Content |
|----------|---------|
| **docs/platform/MODULE_ARCHITECTURE_MAP.md** | Module map: orders, scheduling, inventory, billing, payroll, integration, event platform, automation, reporting, SaaS, control plane; boundaries and event flow. |
| **docs/platform/AUTOMATION_ENGINE.md** | Automation model, event-driven execution (OrderCompleted → GenerateInvoice), rule configuration, tenant safety. |
| **docs/platform/TENANT_SAFETY_VERIFICATION.md** | Rule (CompanyId == current tenant), verification summary by area, event publishing and handler recommendations. |
| **docs/platform/INTEGRATION_SUMMARY.md** | This report. |

Existing **docs/event-platform/** (event architecture, lifecycle, handlers, replay, tenant safety) unchanged.

---

## J. Build/Test Status

- **Build:** Full solution builds successfully (0 errors). StockLedgerService: replaced undefined GetMaterialAsync with _context.Materials.FindAsync in ReceiveAsync and AllocateAsync.
- **Tests:** CephasOps.Application.Tests — 770 passed, 7 skipped, 0 failed. CephasOps.Api.Tests — 86 passed, 2 skipped, 0 failed. Recommended: E2E for order creation, workflow→OrderCompleted, automation, and invoice creation when feasible.
- **Schema:** EF migration **AddOperationalInsightsAndFeatureFlags** adds OperationalInsights, BillingPlanFeatures, TenantFeatureFlags (included in idempotent script from `dotnet ef migrations script`). Standalone **backend/scripts/apply-OperationalInsights-And-FeatureFlags.sql** remains for SQL-only environments.

---

## K. Platform Integration Checklist (Completed)

| Item | Status |
|------|--------|
| MaterialIssuedEvent | ✅ OrderMaterialUsageService; forwarded |
| MaterialReturnedEvent | ✅ StockLedgerService.ReturnAsync; forwarded |
| PayrollCalculatedEvent | ✅ PayrollService.CreatePayrollRunAsync; forwarded |
| Control plane tenant diagnostics | ✅ GET /api/admin/control-plane/tenant-diagnostics + links |
| Field Ops Intelligence | ✅ OperationalInsight + OrderCompletedInsightHandler (idempotent) + GET /api/observability/insights (gated by OperationalInsights feature) |
| PlanFeature / FeatureFlag | ✅ BillingPlanFeature, TenantFeatureFlag, PlanBasedFeatureFlagService, FeatureFlagKeys |

All items from the original platform integration list are implemented. Insights API returns 403 when the tenant does not have the OperationalInsights feature (plan or TenantFeatureFlag). Future work: additional insight types.

---

**Summary:** CephasOps has a clear module map; OrderCreated, OrderCompleted, InvoiceGenerated, MaterialIssued, MaterialReturned, and PayrollCalculated events (with emission and forwarding); event-driven automation (OrderCompleted → GenerateInvoice); OperationalInsight and OrderCompletedInsightHandler (idempotent); plan-based feature flags (BillingPlanFeature, TenantFeatureFlag, PlanBasedFeatureFlagService, FeatureFlagKeys); control plane with tenant diagnostics and observability insights; GET /api/observability/insights. Existing behavior is preserved; new code is additive and tenant-scoped.

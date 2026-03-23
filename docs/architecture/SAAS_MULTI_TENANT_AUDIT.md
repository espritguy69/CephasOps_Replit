# SaaS Multi-Tenant Phase — Architecture Audit

## 1. Executive Summary

CephasOps currently operates as a **single-company** system with existing **company-scoped** data and optional **Tenant** (Phase 11). This audit supports the conversion to a **SaaS multi-tenant** platform with strict tenant isolation, tenant-aware auth, and SaaS billing foundation.

**Key finding:** **Company** already represents the operational company and is the natural **tenant root**. Reuse Company as the tenant boundary; avoid parallel Tenant and Company concepts for operational data. Tenant entity is retained for **billing/subscription** (TenantSubscription) only; operational isolation key is **CompanyId** everywhere.

---

## 2. Backend Audit

### 2.1 Domain Entities

| Area | Entity | Has CompanyId? | Base | Notes |
|------|--------|----------------|------|-------|
| **Companies** | Company | N/A (root) | - | Has optional TenantId. Add Code, Status, SubscriptionId for SaaS. |
| **Tenants** | Tenant | N/A | - | Phase 11; use for billing only. 1:1 with Company for simplicity. |
| **Users** | User | **No** | - | **Missing.** Add CompanyId (primary tenant) for login/JWT. |
| **Departments** | Department | Yes | CompanyScopedEntity | CompanyId nullable. |
| **Orders** | Order, OrderType, OrderCategory, OrderStatusLog, OrderReschedule, OrderBlocker, OrderDocket, OrderMaterialUsage, OrderMaterialReplacement, OrderNonSerialisedReplacement, OrderStatusChecklistItem, OrderStatusChecklistAnswer | Yes | CompanyScopedEntity | All tenant-owned. |
| **Buildings** | Building, BuildingType, InstallationMethod, BuildingContact, BuildingRules, BuildingBlock, BuildingSplitter, Street, HubBox, Pole, Splitter, SplitterType, SplitterPort, BuildingDefaultMaterial | Yes | CompanyScopedEntity | Tenant-owned. |
| **ServiceInstallers** | ServiceInstaller, ServiceInstallerContact, Skill, ServiceInstallerSkill | Yes | CompanyScopedEntity | Tenant-owned. |
| **Tasks** | TaskItem | Yes | CompanyScopedEntity | Tenant-owned. |
| **Scheduler** | ScheduledSlot, SiAvailability, SiLeaveRequest | Yes | CompanyScopedEntity | Tenant-owned. |
| **Parser** | ParseSession, ParsedOrderDraft, ParsedMaterialAlias, EmailMessage, EmailAttachment, ParserRule, EmailAccount, VipEmail, VipGroup, ParserTemplate, EmailTemplate, ParserReplayRun | Yes | CompanyScopedEntity | Tenant-owned. |
| **Notifications** | Notification, NotificationDispatch, NotificationSetting | Yes | CompanyScopedEntity | Tenant-owned. |
| **Inventory** | Material, MaterialPartner, MaterialCategory, MaterialVertical, MaterialTag, MaterialAttribute, StockLocation, StockBalance, StockMovement, StockLedgerEntry, StockAllocation, LedgerBalanceCache, StockByLocationSnapshot, SerialisedItem, MovementType, LocationType, DeliveryOrder, DeliveryOrderItem | Yes (MaterialPartner BaseEntity) | CompanyScopedEntity / BaseEntity | MaterialPartner has CompanyId via config. Tenant-owned. |
| **RMA** | RmaRequest, RmaRequestItem | Yes | CompanyScopedEntity | Tenant-owned. |
| **Billing** | Invoice, InvoiceLineItem, InvoiceSubmissionHistory, BillingRatecard, SupplierInvoice, SupplierInvoiceLineItem, Payment | Yes | CompanyScopedEntity | Tenant-owned. |
| **Payroll** | PayrollPeriod, PayrollRun, PayrollLine, JobEarningRecord, SiRatePlan | Yes | CompanyScopedEntity | Tenant-owned. |
| **Rates** | RateCard, RateCardLine, CustomRate, RateGroup, OrderTypeSubtypeRateGroup, BaseWorkRate, RateModifier, ServiceProfile, OrderCategoryServiceProfile, GponPartnerJobRate, GponSiJobRate, GponSiCustomRate, OrderPayoutSnapshot, PayoutSnapshotRepairRun, PayoutAnomalyReview, PayoutAnomalyAlert, PayoutAnomalyAlertRun | Yes | CompanyScopedEntity | Tenant-owned. |
| **P&L** | PnlPeriod, PnlFact, PnlDetailPerOrder, OverheadEntry, PnlType, OrderFinancialAlert | Yes | CompanyScopedEntity | Tenant-owned. |
| **Departments** | MaterialAllocation, DepartmentMembership | Yes | CompanyScopedEntity | Tenant-owned. |
| **Assets** | AssetType, Asset, AssetMaintenance, AssetDepreciation, AssetDisposal | Yes | CompanyScopedEntity | Tenant-owned. |
| **Settings** | GlobalSetting, MaterialTemplate, DocumentTemplate, GeneratedDocument, KpiProfile, TimeSlot, SmsTemplate, WhatsAppTemplate, SmsGateway, CustomerPreference, TaxCode, PaymentTerm, Vendor, Bin, Brand, ServicePlan, ProductType, Team, CostCentre, SlaProfile, AutomationRule, ApprovalWorkflow, ApprovalStep, BusinessHours, PublicHoliday, EscalationRule, GuardConditionDefinition, SideEffectDefinition | Yes (some BaseEntity) | CompanyScopedEntity / BaseEntity | Tenant-owned where CompanyId present. |
| **Workflow** | WorkflowDefinition, WorkflowTransition, WorkflowJob, WorkflowInstance, WorkflowStepRecord, WorkflowTransitionHistoryEntry, **BackgroundJob**, JobExecution, JobDefinition, JobRun, SystemLog | **BackgroundJob: No** | CompanyScopedEntity / plain | **BackgroundJob missing CompanyId.** JobExecution has CompanyId. |
| **Events** | EventStoreEntry, EventStoreAttemptHistory, LedgerEntry, EventProcessingLog, CommandProcessingLog, ReplayExecutionLock, ReplayOperation, ReplayOperationEvent, RebuildOperation, RebuildExecutionLock, WorkerInstance | Yes / ScopeCompanyId | - | Tenant-scoped. |
| **Integration** | ConnectorDefinition, ConnectorEndpoint, OutboundIntegrationDelivery, OutboundIntegrationAttempt, InboundWebhookReceipt, ExternalIdempotencyRecord | Yes | - | Tenant-owned. |
| **SLA** | SlaRule, SlaBreach | Yes | CompanyScopedEntity | Tenant-owned. |
| **Audit** | AuditOverride, AuditLog | Yes | CompanyScopedEntity | Tenant-owned. |
| **Procurement** | Supplier, PurchaseOrder, PurchaseOrderItem | Yes | CompanyScopedEntity | Tenant-owned. |
| **Sales** | Quotation, QuotationItem | Yes | CompanyScopedEntity | Tenant-owned. |
| **Projects** | Project, BoqItem | Yes | CompanyScopedEntity | Tenant-owned. |
| **Files** | File | Yes | CompanyScopedEntity | Tenant-owned. |
| **Companies** | Partner, PartnerGroup, Vertical, CostCentre, CompanyDocument | Yes | CompanyScopedEntity | Tenant-owned. |
| **Billing (SaaS)** | BillingPlan, TenantSubscription, TenantUsageRecord, TenantInvoice | TenantId (not CompanyId) | - | Global (BillingPlan) or Tenant key (TenantSubscription.TenantId). |

**Entities missing CompanyId (tenant key):**

- **User** — add **CompanyId** (primary company for JWT/tenant context).
- **BackgroundJob** — add **CompanyId** so job execution is tenant-scoped.

**Base entity:** `CompanyScopedEntity` has **nullable** `CompanyId`; comment says "nullable since company feature removed". After backfill, make **non-null** and enforce via global query filter.

### 2.2 DbContext

- **ApplicationDbContext** applies configurations from assembly; no tenant filter applied yet.
- **Soft-delete** filter is applied to all CompanyScopedEntity (IsDeleted == false).
- **No** global filter on CompanyId.
- DbContext is registered with options only; no ITenantProvider injection yet.

### 2.3 Controllers

- Controllers use **ICurrentUserService** and **IDepartmentAccessService** for user and department scope.
- **No** CompaniesController found in audit (company API may exist under different name; Tenant/Company services exist).
- Controllers do not inject DbContext (per architecture guardrails); they use Application services.

### 2.4 Services

- **DepartmentAccessService**: Resolves departments by **UserId** (DepartmentMemberships). Does **not** filter by CompanyId; effectively single-company. For multi-tenant, department access must be within current tenant (CompanyId).
- **CurrentUserService**: Exposes **UserId**, **CompanyId** (from JWT "companyId" or "company_id"), **Email**, **Roles**, **IsSuperAdmin**, **ServiceInstallerId**. When no company claim, returns **Guid.Empty** (single-company bypass).
- **TenantContextService**: Resolves **TenantId** from **CompanyId** → Company.TenantId → Tenant. Used for billing/tenant slug.

### 2.5 Background Jobs

- **BackgroundJob** entity: **no CompanyId**; jobs are not tenant-scoped.
- **JobExecution** has CompanyId (from snapshot).
- Job processors run without explicit tenant context; must become tenant-aware (job carries CompanyId; execution restores context).

### 2.6 Auth Pipeline

- **JWT**: Issuer, audience, signing key configured in Program.cs.
- **AuthService.LoginAsync**: Passes **companyId: null** to GenerateJwtToken ("Company feature removed").
- **GenerateJwtToken**: Accepts companyId; adds claim "companyId" when present. **company_id** also supported by CurrentUserService.
- **User** entity: No CompanyId; cannot derive tenant from user at login. Must add User.CompanyId or resolve from first Department → CompanyId.

### 2.7 RBAC

- **PermissionAuthorizationHandler**: Uses ClaimTypes.NameIdentifier (userId), ClaimTypes.Role (roles).
- **PermissionCatalog**: Admin, tenant, billing permissions exist (AdminTenantsView, AdminTenantsEdit, AdminBillingPlansView, etc.).
- Department RBAC is user/department-based; must be constrained to current tenant.

### 2.8 Reports / Billing

- Reports and billing services use ApplicationDbContext and department/company scope via services; no central tenant filter. **Reporting is a high-risk leak** if any query omits CompanyId filter.

---

## 3. Tenant Model Decision

| Aspect | Decision |
|--------|----------|
| **Tenant root** | **Company** is the operational tenant root. All tenant-owned records have **CompanyId**. |
| **Tenant entity** | Keep **Tenant** for SaaS billing only (TenantSubscription, slug, plan). 1:1 with Company for simplicity: one Company = one Tenant. |
| **Isolation key** | **CompanyId** on every tenant-owned entity. |
| **Auth** | JWT must include **company_id** (and user_id, roles). User belongs to one primary Company (User.CompanyId). |
| **Default tenant** | Single legacy company: **Name "Cephas", Code "CEPHAS"**. All existing data backfilled to this company. |

---

## 4. Table Classification

### Tenant-owned (must have CompanyId, non-null after backfill)

All entities inheriting **CompanyScopedEntity** plus **User** (CompanyId), **BackgroundJob** (CompanyId), and event/job tables that already have CompanyId or ScopeCompanyId.

### Global platform tables (no CompanyId)

- **Tenants** (tenant registry for billing)
- **BillingPlans** (plan definitions)
- **__EFMigrationsHistory**
- System logs / migration history (if any)
- Reference enums (if stored as tables)

---

## 5. Gaps to Address

1. **User.CompanyId** — add and backfill from first DepartmentMembership → Department.CompanyId.
2. **BackgroundJob.CompanyId** — add; set when enqueuing from current tenant context; restore context when executing.
3. **Company** — add **Code**, **Status** (or use IsActive), **SubscriptionId** (optional) for SaaS.
4. **JWT** — set **company_id** at login/refresh from User.CompanyId (or resolved company).
5. **Default tenant** — create Company "Cephas" Code "CEPHAS"; backfill all NULL CompanyId to this company; resolve default in TenantProvider when JWT has no company.
6. **Global query filter** — apply CompanyId == CurrentTenantId for all CompanyScopedEntity (and User, BackgroundJob) when tenant is resolved.
7. **ITenantProvider** — central resolution: JWT company_id → X-Company-Id header → default company (CEPHAS).
8. **DepartmentAccessService** — ensure department list is restricted to current tenant (Department.CompanyId == CurrentTenantId).
9. **Subscription / Plan / Feature** — add PlanFeature, Feature, UsageMetric if not present; module flags (orders, parser, inventory, etc.) for entitlements.
10. **Documentation** — update architecture, data model, RBAC, background jobs, integration docs.

---

## 6. References

- Phase 11: `docs/PHASE_11_TENANT_ISOLATION.md`
- Architecture guardrails: `docs/architecture/architecture_guardrails.md`
- Entity domain map: `docs/architecture/entity_domain_map.md`

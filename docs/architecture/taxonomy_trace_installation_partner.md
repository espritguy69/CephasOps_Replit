# Taxonomy Trace: Installation Type, Installation Method, Partners

**Related:** [Reference data taxonomy](../business/reference_data_taxonomy.md) | [Order lifecycle and statuses](../business/order_lifecycle_and_statuses.md) | [Department & RBAC](../business/department_rbac.md)

**Definitions (ops):**
- **Installation Type** = Order Category assigned by partner (e.g. TIME): FTTH, FTTO, FTTR, FTTC.
- **Installation Method** = How the job is executed: e.g. Pole installation, Prelaid, Non-prelaid.
- **Partners** = Partner entity + category variants used in billing/ops views (e.g. TIME-FTTH, TIME-FTTO); in code, Partner is a single entity; “partner+category” is derived (Partner row + Order.OrderCategoryId).

**Locked rule:** Partner–Category labels (e.g. **TIME-FTTH**) are derived from **Partner.Code** and **OrderCategory.Code** for **display only** and are **not persisted**. Do not create composite partner rows (e.g. TIME-FTTH as a row in Partners). The label is computed in the backend mapping/projection layer and exposed as `derivedPartnerCategoryLabel` on Order and related DTOs.

This document traces where each is **stored** and **used** across backend and frontend. File paths are authoritative.

---

## 1. Data model (tables / fields)

### 1.1 Order Category (Installation Type)

| Location | Detail |
|----------|--------|
| **Table** | `OrderCategories` |
| **Config** | `backend/src/CephasOps.Infrastructure/Persistence/Configurations/Orders/OrderCategoryConfiguration.cs` – `ToTable("OrderCategories")` |
| **Entity** | `backend/src/CephasOps.Domain/Orders/Entities/OrderCategory.cs` – Id, CompanyId, DepartmentId, Name, Code, Description, DisplayOrder, IsActive, CreatedAt, UpdatedAt |
| **Order FK** | `Orders.OrderCategoryId` (nullable, FK to OrderCategories) – `backend/src/CephasOps.Domain/Orders/Entities/Order.cs` (OrderCategoryId); `backend/src/CephasOps.Infrastructure/Persistence/Configurations/Orders/OrderConfiguration.cs` (HasOne OrderCategory, FK OrderCategoryId) |
| **Other FKs** | GponPartnerJobRates.OrderCategoryId, GponSiJobRate.OrderCategoryId, GponSiCustomRate.OrderCategoryId, PnlDetailPerOrder (denormalised string `OrderCategory`), JobEarningRecord.OrderCategoryId |

**Note:** There is **no** separate `InstallationTypes` table. The API route `api/installation-types` is served by **InstallationTypesController**, which delegates to **OrderCategoryService** and reads/writes **OrderCategories**. So “Installation Type” in the UI = Order Category (same table).

### 1.2 Installation Method (Pole / Prelaid / Non-prelaid / SDU-RDF)

| Location | Detail |
|----------|--------|
| **Table** | `InstallationMethods` |
| **Config** | `backend/src/CephasOps.Infrastructure/Persistence/Configurations/Buildings/InstallationMethodConfiguration.cs` (not in grep; entity is in Domain.Buildings) – snapshot: `ApplicationDbContextModelSnapshot.cs` shows `modelBuilder.Entity("CephasOps.Domain.Buildings.Entities.InstallationMethod").ToTable("InstallationMethods")` |
| **Entity** | `backend/src/CephasOps.Domain/Buildings/Entities/InstallationMethod.cs` – Id, CompanyId, DepartmentId, Name, Code, Category, Description, IsActive, DisplayOrder, CreatedAt, UpdatedAt |
| **Order FK** | `Orders.InstallationMethodId` (nullable) – Order.cs; OrderConfiguration.cs (HasOne InstallationMethod, FK InstallationMethodId) |
| **Building FK** | `Buildings.InstallationMethodId` (nullable) – `backend/src/CephasOps.Domain/Buildings/Entities/Building.cs` |
| **Rate / other FKs** | BillingRatecards.InstallationMethodId, GponPartnerJobRates.InstallationMethodId, GponSiJobRate.InstallationMethodId, GponSiCustomRate.InstallationMethodId, SiRatePlans.InstallationMethodId, MaterialTemplates.InstallationMethodId, ParsedOrderDraft (no InstallationMethodId; parser sets Order.InstallationMethodId from building or null) |

**Seeded values (implemented):**
- From `backend/src/CephasOps.Infrastructure/Persistence/Migrations/AddInstallationMethodsTable.sql`: **Prelaid** (PRELAID), **Non-prelaid (MDU / old building)** (NON_PRELAID), **SDU / RDF Pole** (SDU_RDF).
- From `backend/src/CephasOps.Infrastructure/Persistence/Migrations/20241127_AddDepartmentIdToInstallationMethods.sql`: same three (idempotent insert by Code).

**“Pole installation”:** There is **no** literal value named “Pole installation”. The closest is **SDU / RDF Pole** (Code `SDU_RDF`), described as “Single dwelling units and pole-based installations. Pole accessories, aerial cable, termination box, and basic house kit required.” So “Pole installation” in ops should map to **SDU_RDF** where a single method is needed.

### 1.3 Partners

| Location | Detail |
|----------|--------|
| **Table** | `Partners` |
| **Config** | `backend/src/CephasOps.Infrastructure/Persistence/Configurations/Companies/PartnerConfiguration.cs` – `ToTable("Partners")` |
| **Entity** | `backend/src/CephasOps.Domain/Companies/Entities/Partner.cs` – Id, CompanyId, Name, **Code** (optional, for derived label), PartnerType, GroupId, DepartmentId, BillingAddress, ContactName, ContactEmail, ContactPhone, IsActive |
| **Order FK** | `Orders.PartnerId` (required) – Order.cs, OrderConfiguration.cs; index `CompanyId, PartnerId` |
| **Other FKs** | Invoices.PartnerId, BillingRatecards.PartnerId, ParserTemplate.PartnerId, ParsedOrderDraft.PartnerId, Material.PartnerId (legacy), MaterialPartners.PartnerId, RmaRequest.PartnerId, DocumentTemplate.PartnerId, SlaProfile.PartnerId, KpiProfile.PartnerId, PnlFact.PartnerId, GponPartnerJobRate.PartnerId, WorkflowDefinition.PartnerId, etc. |

**Partner–category composite (TIME-FTTH, TIME-FTTO, etc.):**
- **Not** stored as separate partner rows. There are no rows like “TIME-FTTH” or “TIME-FTTO” in `Partners`.
- **Derived** at display/filter time: **Partner** (one row per partner, e.g. “TIME”) + **Order.OrderCategoryId** (FTTH, FTTO, etc.). So “TIME-FTTH” = Partner.Name “TIME” + OrderCategory.Name “FTTH” for that order.
- **Billing/rates:** GponPartnerJobRates and BillingRatecards are keyed by PartnerId (or PartnerGroupId) + OrderTypeId + OrderCategoryId + InstallationMethodId; partner name + category name are shown in UI by resolving IDs to names (e.g. RateEngineManagementPage, PartnerRatesPage).

---

## 2. APIs (endpoints)

### 2.1 Order Category (Installation Type)

| Endpoint | Controller | Service | File |
|----------|------------|---------|------|
| GET /api/order-categories | OrderCategoriesController | IOrderCategoryService | backend/src/CephasOps.Api/Controllers/OrderCategoriesController.cs |
| GET /api/order-categories/{id} | OrderCategoriesController | IOrderCategoryService | same |
| POST /api/order-categories | OrderCategoriesController | IOrderCategoryService | same |
| PUT /api/order-categories/{id} | OrderCategoriesController | IOrderCategoryService | same |
| DELETE /api/order-categories/{id} | OrderCategoriesController | IOrderCategoryService | same |
| GET /api/installation-types | InstallationTypesController | IOrderCategoryService (same data) | backend/src/CephasOps.Api/Controllers/InstallationTypesController.cs |
| GET /api/installation-types/{id} | InstallationTypesController | IOrderCategoryService | same |
| POST /api/installation-types | InstallationTypesController | IOrderCategoryService | same |
| PUT /api/installation-types/{id} | InstallationTypesController | IOrderCategoryService | same |
| DELETE /api/installation-types/{id} | InstallationTypesController | IOrderCategoryService | same |

**Service implementation:** `backend/src/CephasOps.Application/Orders/Services/OrderCategoryService.cs`. Registered in `backend/src/CephasOps.Api/Program.cs` (e.g. `AddScoped<IOrderCategoryService, OrderCategoryService>()`).

### 2.2 Installation Method

| Endpoint | Controller | Service | File |
|----------|------------|---------|------|
| GET /api/installation-methods | InstallationMethodsController | IInstallationMethodService | backend/src/CephasOps.Api/Controllers/InstallationMethodsController.cs |
| GET /api/installation-methods/{id} | InstallationMethodsController | IInstallationMethodService | same |
| POST /api/installation-methods | InstallationMethodsController | IInstallationMethodService | same |
| PUT /api/installation-methods/{id} | InstallationMethodsController | IInstallationMethodService | same |
| DELETE /api/installation-methods/{id} | InstallationMethodsController | IInstallationMethodService | same |

**Service:** `backend/src/CephasOps.Application/Buildings/Services/InstallationMethodService.cs`. Registered in Program.cs.

### 2.3 Partners

| Endpoint | Controller | File |
|----------|------------|------|
| GET /api/partners | PartnersController | backend/src/CephasOps.Api/Controllers/PartnersController.cs (or under Settings; path from frontend api/partners) |
| CRUD partners | (PartnersController or Settings) | Used by frontend `frontend/src/api/partners.ts` |

**Order API and partner:**
- GET /api/orders, GET /api/orders/paged: support query **partnerId** (Guid). `backend/src/CephasOps.Api/Controllers/OrdersController.cs` (GetOrders, GetOrdersPaged); `backend/src/CephasOps.Application/Orders/Services/OrderService.cs` (GetOrdersAsync, GetOrdersPagedAsync – filter by PartnerId).
- GET /api/orders/{id}: returns OrderDto with **PartnerId**; no PartnerName in OrderService.MapToOrderDto. Scheduler and other DTOs add PartnerName where needed.

---

## 3. Frontend (pages / components)

### 3.1 Order Category (Installation Type)

| Place | Usage | File |
|-------|--------|------|
| Settings – Order Categories | CRUD Order Categories (api/order-categories) | frontend/src/pages/settings/OrderCategoriesPage.tsx |
| Settings – Installation Types | CRUD “Installation Types” (api/installation-types) – same data as Order Categories | frontend/src/pages/settings/InstallationTypesPage.tsx, frontend/src/api/installationTypes.ts |
| Order create | OrderCategoryId not in CreateOrderPage form; Installation Method is (installationMethodId + installationMethod name from Building or dropdown). Order Category can be set via API in create payload but no dropdown found on create form. | frontend/src/pages/orders/CreateOrderPage.tsx |
| Rate Engine / Partner rates | Order Category (orderCategoryIds, orderCategoryId) for partner rates, SI rates, custom rates; filters and table columns orderCategoryName | frontend/src/pages/settings/RateEngineManagementPage.tsx |
| SI Rate Plans | installationMethodId / installationMethodName; order categories not in SiRatePlansPage (SI rates keyed by OrderType + OrderCategory + InstallationMethod in backend) | frontend/src/pages/settings/SiRatePlansPage.tsx |
| PnL drilldown | orderCategory displayed (label “Order Category”) | frontend/src/pages/pnl/PnlDrilldownPage.tsx |
| Orders list grid | Columns show orderType; no orderCategory column in OrdersListPage grid. | frontend/src/pages/orders/OrdersListPage.tsx |
| Order detail | Shows orderType, partnerName/partnerGroup; no explicit Order Category or Installation Method section in snippet. | frontend/src/pages/orders/OrderDetailPage.tsx |

**API usage:** Order categories loaded via `getOrderCategories` (api/order-categories) or `getInstallationTypes` (api/installation-types). RateEngineManagementPage uses installation types response as “order categories” for rate combos.

### 3.2 Installation Method

| Place | Usage | File |
|-------|--------|------|
| Buildings list | Filter by installationMethodId; dropdown from getInstallationMethods | frontend/src/pages/buildings/BuildingsListPage.tsx, frontend/src/api/installationMethods.ts |
| Order create | installationMethodId + installationMethod (name); optional; can be set from selected building’s installationMethodId; getInstallationMethods for dropdown | frontend/src/pages/orders/CreateOrderPage.tsx |
| SI Rate Plans | installationMethodId, installationMethodName in form and table | frontend/src/pages/settings/SiRatePlansPage.tsx |
| Rate Engine Management | installationMethodIds, installationMethodId for partner/SI/custom rates; filters and combo; column installationMethodName | frontend/src/pages/settings/RateEngineManagementPage.tsx |
| Orders list / detail | No Installation Method column or field in OrdersListPage; Order detail shows partner/order type, not installation method in snippet. | — |

**Display of “Pole”:** UI shows Installation Method **name** from API (e.g. “SDU / RDF Pole”). So “Pole installation” in ops is shown as **SDU / RDF Pole** wherever installation method name is displayed (buildings filter, order create, rate screens). There is no separate “Pole installation” value in InstallationMethods.

### 3.3 Partners

| Place | Usage | File |
|-------|--------|------|
| Order create | Partner required; dropdown from getPartners({ isActive: true }); partnerId in payload | frontend/src/pages/orders/CreateOrderPage.tsx |
| Order detail | Displays partnerName or partnerGroup | frontend/src/pages/orders/OrderDetailPage.tsx |
| Orders list filters | OrderFilters use **hardcoded** PARTNER constant (TIME, TIMEDIGI, TIMECELCOM, TIMEUMOBILE, DIRECT) – not from Partners API | frontend/src/components/orders/OrderFilters.tsx, frontend/src/constants/orders.ts |
| Orders list grid | Columns use order data; no explicit partner column in snippet; filter is by partner (constant list). | frontend/src/pages/orders/OrdersListPage.tsx |
| Invoices list | Partner filter and column; options from getPartners(); partnerId filter, partnerName column | frontend/src/pages/billing/InvoicesListPage.tsx |
| Invoice create/edit | Partner select from getPartners | frontend/src/pages/billing (create flow), InvoiceEditPage |
| Parser templates | Partner Pattern (email pattern, e.g. *@time.com.my); not PartnerId – template has PartnerPattern, ParserTemplate.PartnerId in backend | frontend/src/features/email/ParserTemplatesPage.tsx |
| Settings – Partners | CRUD partners | frontend/src/pages/settings/PartnersPage.tsx |
| Settings – Partner groups | Partner groups | frontend/src/pages/settings/PartnerGroupsPage.tsx |
| Settings – Partner rates | Partner + Order Type + Order Category + Installation Method for billing rates | frontend/src/pages/settings/PartnerRatesPage.tsx |
| Rate Engine | Partner/PartnerGroup + Order Category + Installation Method for partner and SI rates | frontend/src/pages/settings/RateEngineManagementPage.tsx |
| Scheduler | Order cards / slots can show PartnerName (from scheduler API DTO) | backend SchedulerService returns PartnerName; frontend scheduler components |
| Reports / PnL | Partner filter / dimension; PartnerName in PnL DTOs | frontend/src/pages/pnl/PnlDrilldownPage.tsx; backend PnlService sets PartnerName from Partner lookup |
| Dashboard | Orders-by-partner chart | frontend/src/components/dashboard/OrdersByPartnerChart.tsx, frontend/src/components/charts/OrdersByPartnerChart.tsx |
| RMA | Partner on RMA; partner name from lookup | frontend/src/components/rma/CreateRmaModal.tsx; backend RMAService PartnerName |
| Workflow definitions | Workflow can be partner-specific (PartnerId on WorkflowDefinition) | frontend/src/pages/workflow/WorkflowDefinitionsPage.tsx |

**Partner + category derivation:** Everywhere “partner + category” (e.g. TIME-FTTH) is needed, it is **derived**: Partner name (from Partners table) + Order Category name (from OrderCategories) for that order or rate row. No composite table or hardcoded list of “TIME-FTTH” etc. in backend. Frontend order filters use a **hardcoded** list of partner **labels** (TIME, TIMEDIGI, etc.) in `frontend/src/constants/orders.ts` (PARTNER), which is not the same as Partners API and can get out of sync.

---

## 4. How partner + category is derived

- **Partners** table: one row per partner (e.g. TIME, Celcom). No “TIME-FTTH” rows.
- **Order** has PartnerId + OrderCategoryId. Display “Partner + Category” = Partner.Name + OrderCategory.Name (e.g. “TIME” + “FTTH”).
- **Rates (GponPartnerJobRates, BillingRatecards):** Keyed by PartnerId (or PartnerGroupId), OrderTypeId, OrderCategoryId, InstallationMethodId. UI resolves IDs to names (partnerName, orderCategoryName, installationMethodName) for tables and filters.
- **Parser:** ParserTemplate has PartnerPattern (email) and optional PartnerId; no OrderCategory on template; order type/category come from OrderTypeCode and order creation logic. ParsedOrderDraft has PartnerId only.
- **Billing / Invoices:** Invoice has PartnerId; partner name from Partner lookup (BillingService). No “partner+category” on invoice; category is on the order(s) behind the invoice.
- **Frontend order filters:** Partner filter uses **constants** PARTNER (TIME, TIMEDIGI, etc.) from `frontend/src/constants/orders.ts`, not Partners API – so filter values are not driven by DB.

---

## 5. Gaps and clarifications

| Gap | Detail |
|-----|--------|
| **“Pole installation” not a distinct method** | Only **SDU / RDF Pole** (SDU_RDF) exists in InstallationMethods. “Pole installation” in ops should be mapped to SDU_RDF for display and rate keying. |
| **Order list/detail: no Order Category or Installation Method in UI** | Orders list and order detail do not show Order Category or Installation Method columns/fields in the traced components. Backend OrderDto has OrderCategoryId and InstallationMethodId; display names are not populated in OrderService.MapToOrderDto for the main Orders API. |
| **Order filters use hardcoded partner list** | OrderFilters.tsx uses PARTNER from constants/orders.ts (TIME, TIMEDIGI, TIMECELCOM, TIMEUMOBILE, DIRECT), not getPartners(). Filtering by partner in Orders API uses partnerId (Guid), so frontend would need to map these labels to Partner IDs or switch to API-driven partner list. |
| **Partner + category composite** | Correctly implemented as **derived** (Partner + OrderCategory), not as separate partner rows or hardcoded “TIME-FTTH” list in backend. Billing/ops views that need “partner+category” should resolve PartnerId + OrderCategoryId to names. |
| **Order create: Order Category** | CreateOrderPage sends orderTypeId, buildingId, partnerId, installationMethodId; CreateOrderDto includes OrderCategoryId and InstallationMethodId. If the create form does not expose Order Category dropdown, OrderCategoryId may be null unless set by backend from building or other logic. |
| **ParsedOrderDraft** | Has PartnerId and OrderTypeCode; no OrderCategoryId. Order created from draft gets OrderCategoryId from CreateOrderFromDraftDto or defaulting logic (e.g. parser or backend). |

---

## 6. Seed and reference summary

| Type | Seed file(s) | Implemented values |
|------|--------------|--------------------|
| Order Category | 20250106_SeedAllReferenceData.sql; DatabaseSeeder.SeedDefaultOrderCategoriesAsync | FTTH, FTTO, FTTR, FTTC |
| Installation Method | AddInstallationMethodsTable.sql; 20241127_AddDepartmentIdToInstallationMethods.sql | Prelaid (PRELAID), Non-prelaid (NON_PRELAID), SDU / RDF Pole (SDU_RDF) |
| Partners | No seed for Partner rows | Configurable only; no fixed list in repo |

All paths above are under the repo root (backend/ or frontend/ as stated). No code was modified; this is a trace document only.

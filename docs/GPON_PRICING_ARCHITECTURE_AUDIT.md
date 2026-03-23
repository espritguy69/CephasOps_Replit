# GPON Pricing Model — Architecture Audit & Layered Design

**Classification:** Critical financial subsystem (SI payroll, partner billing, P&L, operational reporting)  
**Mode:** Audit first, propose second. No implementation until architecture is approved.  
**Date:** 2026-03-08

---

## Executive Summary

The GPON pricing subsystem must evolve from a **flat grid** (one row per OrderType × OrderCategory × InstallationMethod × SiLevel × PartnerGroup) to a **layered payout resolution** model to avoid rate table explosion when adding installer tiers (Junior, Senior, Specialist) and to support work-based pricing shared across order types (e.g. Assurance + VAS).

**Confirmed current behaviour (verified in code):**

- Payout resolution priority: **1. GponSiCustomRate → 2. GponSiJobRate.**
- GponSiJobRate lookup uses: OrderTypeId, OrderCategoryId, InstallationMethodId, SiLevel, PartnerGroupId.
- SiLevel is stored on **ServiceInstaller.SiLevel** (enum Junior/Senior); payroll passes `SiLevel.ToString()` into the rate engine.
- **SiRatePlan is NOT used for base job payout;** it is used only for KPI adjustments (OnTimeBonus, LatePenalty) in payroll.

**Target:** Three layers — **SI Override** (evolve GponSiCustomRate) → **SI Tier Rate** (multiplier/fixed per tier) → **Base Work Rate** (by RateGroup, Category, Method) → **Legacy GponSiJobRate** (migration fallback). Full backward compatibility until validation is complete. No deletion of legacy tables until cutover is signed off.

---

## 1. Current-State Assessment

### 1.1 Verified assumptions (code references)

| Assumption | Verification | Location |
|------------|--------------|----------|
| Payout priority: GponSiCustomRate then GponSiJobRate | Confirmed | `RateEngineService.cs` lines 73–91 (custom first), 96–118 (then default payout) |
| GponSiJobRate lookup dimensions | Confirmed | `ResolveGponPayoutRateInternalAsync`: OrderTypeId, OrderCategoryId, InstallationMethodId, SiLevel, PartnerGroupId (lines 460–498) |
| GponSiCustomRate lookup dimensions | Confirmed | `ResolveGponCustomRateAsync`: ServiceInstallerId, OrderTypeId, OrderCategoryId, InstallationMethodId, PartnerGroupId (lines 448–457) |
| SiLevel on ServiceInstaller | Confirmed | `ServiceInstaller.cs`: `public InstallerLevel SiLevel` (Junior/Senior enum) |
| Payroll uses RateEngineService for base payout | Confirmed | `PayrollService.cs` lines 241–251: `ResolveGponRatesAsync` with OrderTypeId, OrderCategoryId, InstallationMethodId, PartnerGroupId, ServiceInstallerId, SiLevel |
| SiRatePlan not used for base job payout | Confirmed | `PayrollService.cs`: base = `rateResult.PayoutAmount` (line 254); SiRatePlan used only for `OnTimeBonus` / `LatePenalty` (lines 266–278) |
| PartnerGroupId from Order | Confirmed | `PayrollService.cs` line 233: `partnerGroupId = partner?.GroupId` (Partner.GroupId) |

### 1.2 Current payout resolution path (end-to-end)

```
Order (OrderTypeId, OrderCategoryId, InstallationMethodId, PartnerId, AssignedSiId)
       │
       ▼
PayrollService.CreatePayrollRunAsync (or OrderProfitabilityService / API resolve)
       │
       ├── Load ServiceInstaller → SiLevel (default Junior)
       ├── Load Partner → PartnerGroupId
       │
       ▼
IRateEngineService.ResolveGponRatesAsync(GponRateResolutionRequest)
       │
       ├── [1] ResolveGponCustomRateAsync(ServiceInstallerId, OrderTypeId, OrderCategoryId, InstallationMethodId, PartnerGroupId)
       │         → GponSiCustomRates table
       │         → If found: return CustomPayoutAmount, stop.
       │
       └── [2] ResolveGponPayoutRateInternalAsync(OrderTypeId, OrderCategoryId, InstallationMethodId, SiLevel, PartnerGroupId)
                 → GponSiJobRates table
                 → Prefer PartnerGroupId match, then PartnerGroupId null (default)
                 → Return PayoutAmount or null
```

### 1.3 Data tables involved (payout only)

| Table | Role | Key dimensions |
|-------|------|----------------|
| **GponSiCustomRates** | Per-SI payout override (highest priority) | CompanyId, ServiceInstallerId, OrderTypeId, OrderCategoryId, InstallationMethodId?, PartnerGroupId? |
| **GponSiJobRates** | Default payout by job + level | CompanyId, OrderTypeId, OrderCategoryId, InstallationMethodId?, SiLevel, PartnerGroupId? |
| **SiRatePlans** | KPI bonus/penalty only (not base rate) | ServiceInstallerId, DepartmentId?, InstallationMethodId?; columns OnTimeBonus, LatePenalty, etc. |
| **ServiceInstallers** | Source of SiLevel | SiLevel (InstallerLevel enum) |
| **Partners** | Source of PartnerGroupId for order | GroupId → PartnerGroup |
| **Orders** | Input to resolution | OrderTypeId, OrderCategoryId, InstallationMethodId, PartnerId, AssignedSiId |

### 1.4 Where duplication occurs

- **GponSiJobRates:** One row per (OrderTypeId, OrderCategoryId, InstallationMethodId, **SiLevel**, PartnerGroupId). Every installer level (Junior, Senior, and any future tier) multiplies the grid. Example: Activation + FTTH + Aerial + Junior + TIME and Activation + FTTH + Aerial + Senior + TIME are two separate rows. Adding “Specialist” adds another full matrix of rows.

### 1.5 Where overrides occur

- **GponSiCustomRates:** One row per (ServiceInstallerId, OrderTypeId, OrderCategoryId, InstallationMethodId, PartnerGroupId) **only where an exception is needed**. This already avoids cloning the entire grid; the design must evolve this table, not remove it.

### 1.6 Consumers of payout resolution

| Consumer | Entry point | Use of payout |
|----------|-------------|----------------|
| **Payroll** | `PayrollService.CreatePayrollRunAsync` | Base payout per order → JobEarningRecord.BaseRate; + KPI from SiRatePlan → FinalPay |
| **P&L / profitability** | `OrderProfitabilityService.GetOrderProfitabilityAsync` | Payout amount and source for per-order profit |
| **API / Rate calculator** | `RatesController.ResolveRates` (POST `/api/rates/resolve`) | Returns revenue + payout for a given request (UI calculator) |

---

## 2. Target Architecture

### 2.1 Entity definitions

#### RateGroup (new)

- **Purpose:** Group order types so multiple types (e.g. Assurance, VAS) share the same pricing definition.
- **Fields:** Id, CompanyId, Code (e.g. "INSTALL", "SERVICE"), Name, IsActive, DisplayOrder.
- **Mapping:** Junction **RateGroupOrderType** (RateGroupId, OrderTypeId). Each OrderType belongs to at most one RateGroup for payout.

#### BaseWorkRate (new) — Layer 1

- **Purpose:** Work cost independent of installer. One row can serve all order types in a RateGroup.
- **Fields:** Id, CompanyId, RateGroupId, PartnerGroupId?, OrderCategoryId, InstallationMethodId?, OrderTypeId? (optional subtype override), BaseRate (decimal), Currency, ValidFrom, ValidTo, IsActive.
- **Key:** (RateGroupId, PartnerGroupId, OrderCategoryId, InstallationMethodId, OrderTypeId). OrderTypeId null = group default; set = subtype override.
- **Index:** (RateGroupId, PartnerGroupId, OrderCategoryId, InstallationMethodId) for lookup performance.

#### SITier (new)

- **Purpose:** Reference data for installer tiers (Junior, Senior, Specialist).
- **Fields:** Id, CompanyId, Code (e.g. "Junior", "Senior", "Specialist"), Name, IsActive, DisplayOrder.
- **ServiceInstaller:** Add **SITierId** (Guid?, FK). Keep SiLevel for backward compatibility; resolve tier from SITierId or fallback SiLevel → SITier by code.

#### SITierRate (new) — Layer 2

- **Purpose:** Apply tier logic to base rate: multiplier, fixed amount, or delta.
- **Fields:** Id, CompanyId, RateGroupId, OrderCategoryId, InstallationMethodId?, OrderTypeId?, SITierId, PartnerGroupId?, PayoutAmount? (fixed override), Multiplier? (e.g. 0.8, 1.0, 1.2), DeltaAmount? (optional), Currency, ValidFrom, ValidTo, IsActive.
- **Index:** (SITierId, RateGroupId), (RateGroupId, OrderCategoryId, InstallationMethodId, SITierId) for resolution.

#### SIOverrideRate — Layer 3 (evolve GponSiCustomRate)

- **Recommendation:** **Do NOT remove GponSiCustomRate.** Evolve it as the SI Override layer. Optionally add RateGroupId later for group-keyed overrides; initially keep keying by (ServiceInstallerId, OrderTypeId, OrderCategoryId, InstallationMethodId, PartnerGroupId) so behaviour is unchanged.
- **No schema change required for Phase 1;** same table, same resolution priority (first).

### 2.2 Relationship summary

- RateGroup ← RateGroupOrderType → OrderType (many-to-many; each OrderType at most one group).
- BaseWorkRate → RateGroup, OrderCategory, InstallationMethod, PartnerGroup; optional OrderType (subtype).
- SITierRate → RateGroup, SITier, OrderCategory, InstallationMethod, PartnerGroup; optional OrderType.
- ServiceInstaller → SITier (nullable).
- GponSiCustomRate (unchanged) → ServiceInstaller, OrderType, OrderCategory, InstallationMethod, PartnerGroup.

### 2.3 Activation without subtype

- Orders store **OrderTypeId** only (parent or leaf subtype). For “Activation” without subtype, the order’s OrderTypeId is the parent Activation type.
- RateGroup “INSTALL” can include Activation (and optionally Modification). BaseWorkRate rows with **OrderTypeId = null** define the group default; one row per (RateGroupId, OrderCategoryId, InstallationMethodId, PartnerGroupId) covers Activation. No subtype required.

### 2.4 Shared pricing for Assurance + VAS

- One RateGroup “SERVICE” with Order Types Assurance and VAS. BaseWorkRate keyed by (RateGroupId, OrderCategoryId, InstallationMethodId, OrderTypeId=null, PartnerGroupId) gives one pricing definition for both; no duplicated rows per order type.

### 2.5 Partner-specific payout

- BaseWorkRate and SITierRate both include **PartnerGroupId**. Resolution uses the same partner hierarchy as today (partner-specific then default). Preserved.

### 2.6 Performance requirements

- **Indexes (recommended):**
  - **BaseWorkRate:** (RateGroupId, PartnerGroupId, OrderCategoryId, InstallationMethodId), and (CompanyId, IsActive). Include OrderTypeId in composite if subtype lookups are frequent.
  - **SITierRate:** (SITierId, RateGroupId), (RateGroupId, OrderCategoryId, InstallationMethodId, SITierId), (CompanyId, IsActive).
  - **SI Override (GponSiCustomRate):** existing (ServiceInstallerId, OrderTypeId, OrderCategoryId, InstallationMethodId) — already present.
- **Caching:** Rate resolution is called per order in payroll and per order in P&L. Recommend in-memory caching for reference data: RateGroup list, RateGroupOrderType mapping, SITier list. Invalidate on configurable TTL or when RateGroup/SITier CRUD is invoked. Do not cache per-order resolution result (order dimensions vary).

---

## 3. Resolution Logic

### 3.1 Exact priority (payout)

1. **SI Override (Layer 3)**  
   GponSiCustomRate: (ServiceInstallerId, OrderTypeId, OrderCategoryId, InstallationMethodId, PartnerGroupId).  
   If found and valid (IsActive, ValidFrom/ValidTo) → return CustomPayoutAmount, **stop**.

2. **Resolve RateGroup**  
   From Order.OrderTypeId → RateGroupOrderType → RateGroupId. If no group, **fallback to legacy** (step 4).

3. **Base Work Rate (Layer 1)**  
   Lookup BaseWorkRate by (RateGroupId, OrderCategoryId, InstallationMethodId, OrderTypeId, PartnerGroupId); if not found, then (RateGroupId, OrderCategoryId, InstallationMethodId, null, PartnerGroupId).  
   BaseAmount = BaseRate. If not found → **fallback to legacy** (step 4).

4. **SI Tier Rate (Layer 2)**  
   SI’s tier from ServiceInstaller.SITierId (or SiLevel → SITier code).  
   Lookup SITierRate by (RateGroupId, OrderCategoryId, InstallationMethodId, OrderTypeId?, SITierId, PartnerGroupId); fallback OrderTypeId null.  
   - If row has **PayoutAmount** → return PayoutAmount.  
   - Else if row has **Multiplier** → return BaseAmount × Multiplier.  
   - Else if row has **DeltaAmount** → return BaseAmount + DeltaAmount.  
   - Else → return BaseAmount.

5. **Legacy fallback (migration)**  
   If no RateGroup or no BaseWorkRate for context: resolve **GponSiJobRate** as today (OrderTypeId, OrderCategoryId, InstallationMethodId, SiLevel, PartnerGroupId). Return PayoutAmount or null.

### 3.2 Example: Activation, Aerial, FTTH, Junior, Partner TIME

- **Inputs:** OrderTypeId = Activation, OrderCategoryId = FTTH, InstallationMethodId = Aerial, SiLevel = "Junior", PartnerGroupId = TIME, ServiceInstallerId = some SI (no custom rate).
- **Step 1:** No GponSiCustomRate for (SI, Activation, FTTH, Aerial, TIME) → continue.
- **Step 2:** OrderType Activation → RateGroup INSTALL (from RateGroupOrderType).
- **Step 3:** BaseWorkRate(INSTALL, FTTH, Aerial, null, TIME) → e.g. BaseRate = 100.
- **Step 4:** SITier Junior → SITierRate(INSTALL, FTTH, Aerial, null, Junior, TIME) → e.g. Multiplier = 0.8 → **payout = 100 × 0.8 = 80**.
- **Output:** Final payout = 80 (source: SITierRate).

If no SITierRate for Junior: return BaseRate 100 (Layer 1 only). If no BaseWorkRate: fallback to GponSiJobRate(Activation, FTTH, Aerial, Junior, TIME) and return that row’s PayoutAmount.

---

## 4. Migration Strategy

### 4.1 Phases (safe, staged)

| Phase | Description | Logic change |
|-------|-------------|--------------|
| **Phase 1** | Introduce new entities only (RateGroup, RateGroupOrderType, BaseWorkRate, SITier, SITierRate; ServiceInstaller.SITierId). No resolution change. | None |
| **Phase 2** | Populate RateGroup and RateGroupOrderType (e.g. one group per order type or INSTALL/SERVICE grouping). Seed SITier (Junior, Senior, Specialist). Backfill SITierId from SiLevel where possible. | None |
| **Phase 3** | Data migration: from GponSiJobRate derive BaseWorkRate (e.g. use Junior as base) and SITierRate (per level: multiplier or fixed). Scripts idempotent, run in staging first. | None |
| **Phase 4** | Dual calculation: in RateEngineService, when feature flag **UseLayeredPayoutResolution** is true, compute payout via new layers and **also** via legacy path; log both and optionally compare (e.g. to a comparison table or telemetry). Do not change returned value yet. | Additive logging only |
| **Phase 5** | Feature flag switch: when **UseLayeredPayoutResolution** is true, return the **new** payout (with legacy fallback when new returns null). When false, keep current behaviour. Validate payroll and P&L in staging. | Config-driven return value |
| **Phase 6** | Deprecate legacy: hide GponSiJobRate from new UI; mark read-only; eventually stop writing. **Do NOT drop tables** until long-term validation and business sign-off. | UI + policy only |

### 4.2 Legacy fallback

- Until Phase 5 is validated, legacy path must remain available. When new layers return null (e.g. no RateGroup for an OrderType), resolution **must** fall back to GponSiJobRate so payroll and P&L never see incorrect or zero payout due to missing mapping.

### 4.3 Data conversion (Phase 3) — high level

- **BaseWorkRate:** For each (OrderTypeId, OrderCategoryId, InstallationMethodId, PartnerGroupId) in GponSiJobRates, pick one reference tier (e.g. Junior); create RateGroup per order type (or by grouping); insert BaseWorkRate with RateGroupId, same Category/Method/PartnerGroup, OrderTypeId null (or set for subtype), BaseRate = that Junior row’s PayoutAmount.
- **SITierRate:** For each (OrderTypeId, OrderCategoryId, InstallationMethodId, SiLevel, PartnerGroupId) in GponSiJobRates, map OrderTypeId → RateGroupId, SiLevel → SITierId; insert SITierRate with PayoutAmount = row’s PayoutAmount (fixed) or compute Multiplier from Junior base. Prefer one row per (RateGroup, Category, Method, Tier, PartnerGroup) with Multiplier to minimise rows.

---

## 5. UI Recommendations

- **Rate Groups:** Settings page to list/create/edit Rate Groups and assign Order Types to a group (each Order Type at most one group).
- **Base Work Rates:** Settings page to manage BaseWorkRate by Rate Group, Partner Group, Order Category, Installation Method, optional Order Type (subtype override). Show “group default” vs “subtype override.”
- **SI Tiers:** Settings page to CRUD SITier (Junior, Senior, Specialist). Service Installer form: show/edit SITierId (and keep SiLevel in sync or deprecated).
- **SI Tier Rates:** Settings page to manage SITierRate: Rate Group, Category, Method, Tier, Partner Group; PayoutAmount (fixed) or Multiplier or Delta.
- **SI Overrides:** Keep existing Rate Engine Management → SI Custom Rates (GponSiCustomRate). No change to UX for Phase 1.
- **Rate Calculator:** Inputs: Order Type, Category, Install Method, Installer (optional), Partner. Outputs: Base rate (Layer 1), Tier adjustment (Layer 2), Override (Layer 3), **Final payout** and resolution steps (which layer was used).

---

## 6. Implementation Plan (stages in order)

| Stage | Deliverable | Layer |
|-------|-------------|--------|
| 1 | Create **RateGroup** entity, EF config, migration, API (list/create), minimal UI (list) | Domain, Infrastructure, Application, API, Frontend |
| 2 | Create **RateGroupOrderType** junction; API to assign order types to group; data seed or script for Phase 2 | Domain, Infrastructure, Application, API |
| 3 | Create **BaseWorkRate** entity, config, migration, API CRUD, UI (grid/form) | Domain, Infrastructure, Application, API, Frontend |
| 4 | Create **SITier** entity, config, migration; add **ServiceInstaller.SITierId**; API + UI; seed Junior/Senior/Specialist | Domain, Infrastructure, Application, API, Frontend |
| 5 | Create **SITierRate** entity, config, migration, API CRUD, UI | Domain, Infrastructure, Application, API, Frontend |
| 6 | **Extend RateEngineService:** add private methods ResolveBaseWorkRateAsync, ResolveSITierRateAsync; add feature flag **UseLayeredPayoutResolution**; when true, run new resolution after GponSiCustomRate, with legacy fallback when new returns null | Application only |
| 7 | **Dual resolution logging:** when flag true, also compute legacy payout; log (or store) both for comparison; no change to returned value | Application |
| 8 | **Switch return value:** when flag true, return new layered payout (with legacy fallback); run payroll and P&L tests; validate in staging | Application |
| 9 | **Rate Calculator UI:** show Base, Tier, Override, Final; resolution steps | Frontend |
| 10 | **Deprecation:** GponSiJobRate read-only in UI; hide from “primary” flow; do not drop | Frontend, docs |

---

## 7. Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Incorrect data migration (wrong BaseWorkRate or SITierRate) | Idempotent scripts; run in staging; dual calculation (Phase 4) and compare; validation report of orders where new ≠ legacy |
| Missing RateGroup mapping for an OrderType | Fallback to legacy GponSiJobRate; validation job to list OrderTypes with no RateGroup |
| Tier confusion (SiLevel vs SITierId) | Backfill SITierId from SiLevel; resolution uses SITierId when set, else SiLevel → SITier by Code; document clearly |
| Performance (extra lookups) | Indexes on BaseWorkRate (RateGroupId, PartnerGroupId, OrderCategoryId, InstallationMethodId), SITierRate (SITierId, RateGroupId); consider in-memory cache for reference data (RateGroup, SITier) |
| Legacy compatibility broken | No removal of GponSiJobRate or GponSiCustomRate; legacy path always available when new path returns null; feature flag to revert to legacy-only |
| Payroll/P&L regression | Phase 4 dual logging; Phase 5 switch only after comparison and sign-off; existing RateEngineServiceTests and Payroll tests must pass |

---

## 8. Minimum Safe First Implementation Slice

The smallest safe slice that adds structure **without changing any payout behaviour**:

1. **Domain:** Add **RateGroup** entity (Id, CompanyId, Code, Name, IsActive, DisplayOrder). Add **RateGroupOrderType** (RateGroupId, OrderTypeId) — no new table if using many-to-many with shadow FK.
2. **Infrastructure:** EF configuration and migration for RateGroup (and junction if separate table). **Do not** add BaseWorkRate, SITier, or SITierRate yet.
3. **Application:** RateGroupService (list/create/get) and DTOs; no change to IRateEngineService or RateEngineService.
4. **API:** GET/POST Rate Groups (company-scoped); no change to resolve endpoint.
5. **Frontend:** Simple Settings → Rate Groups page (list, create). No change to Rate Engine Management or calculator.

**Outcome:** RateGroup exists and can be populated; no resolution logic change; no risk to payroll or P&L.

---

## 9. What Must NOT Be Changed Yet

- **Do NOT** delete or drop GponSiJobRates or GponSiCustomRates.
- **Do NOT** change the signature or contract of IRateEngineService.ResolveGponRatesAsync or GetGponPayoutRateAsync (add overloads or optional parameters if needed for new path).
- **Do NOT** change PayrollService.CreatePayrollRunAsync’s use of ResolveGponRatesAsync or how it builds GponRateResolutionRequest (OrderTypeId, OrderCategoryId, InstallationMethodId, PartnerGroupId, ServiceInstallerId, SiLevel).
- **Do NOT** remove or repurpose SiRatePlan for base job payout; it remains KPI-only (OnTimeBonus, LatePenalty).
- **Do NOT** embed resolution logic in controllers; keep it in RateEngineService.
- **Do NOT** change Order entity (OrderTypeId, OrderCategoryId, InstallationMethodId) or Partner.GroupId usage.
- **Do NOT** change GponSiCustomRate table or resolution order (it must remain first in priority).
- **Do NOT** add new resolution behaviour to the **returned** payout until Phase 5 (feature flag) and validation are complete.

---

## 10. Affected Files (by layer)

### Domain

- **New:** `backend/src/CephasOps.Domain/Rates/Entities/RateGroup.cs`  
- **New:** `backend/src/CephasOps.Domain/Rates/Entities/RateGroupOrderType.cs` (or many-to-many in RateGroup)  
- **New:** `backend/src/CephasOps.Domain/Rates/Entities/BaseWorkRate.cs`  
- **New:** `backend/src/CephasOps.Domain/ServiceInstallers/Entities/SITier.cs`  
- **New:** `backend/src/CephasOps.Domain/Rates/Entities/SITierRate.cs`  
- **Modify (later):** `backend/src/CephasOps.Domain/ServiceInstallers/Entities/ServiceInstaller.cs` (add SITierId)  
- **Unchanged (do not modify in first slice):** GponSiJobRate.cs, GponSiCustomRate.cs

### Infrastructure

- **New:** `backend/src/CephasOps.Infrastructure/Persistence/Configurations/Rates/RateGroupConfiguration.cs`  
- **New:** Configurations for BaseWorkRate, SITier, SITierRate  
- **Modify:** `backend/src/CephasOps.Infrastructure/Persistence/ApplicationDbContext.cs` (DbSet for new entities)  
- **New:** EF migration(s) for new tables and ServiceInstaller.SITierId  
- **Unchanged:** GponSiJobRateConfiguration.cs, GponSiCustomRateConfiguration.cs

### Application

- **Modify (Phase 6+):** `backend/src/CephasOps.Application/Rates/Services/RateEngineService.cs` (add layered resolution, feature flag, legacy fallback)  
- **Modify (Phase 6+):** `backend/src/CephasOps.Application/Rates/Services/IRateEngineService.cs` (only if new method needed; prefer same interface)  
- **New:** Application services for RateGroup, BaseWorkRate, SITier, SITierRate (CRUD)  
- **New:** DTOs for new entities  
- **Unchanged:** PayrollService.cs (until Phase 5 switch; then still same call into IRateEngineService)

### API

- **New or extend:** Controller for Rate Groups (and later Base Work Rates, SI Tiers, SI Tier Rates)  
- **Unchanged:** `backend/src/CephasOps.Api/Controllers/RatesController.cs` resolve endpoint (same request/response); existing GponSiJobRate and GponSiCustomRate CRUD

### Frontend

- **New:** Settings pages for Rate Groups, Base Work Rates, SI Tiers, SI Tier Rates  
- **Modify (later):** Rate Engine Management / Rate Calculator to show layered result (Base, Tier, Override, Final)  
- **Unchanged (first slice):** Existing SI Custom Rates UI, existing resolve API call

### Payroll

- **Unchanged:** `backend/src/CephasOps.Application/Payroll/Services/PayrollService.cs` — continues to call IRateEngineService.ResolveGponRatesAsync with same request; no change until RateEngineService internally returns new layered result (Phase 5).

### Tests

- **Modify (Phase 6+):** `backend/tests/CephasOps.Application.Tests/Rates/RateEngineServiceTests.cs` — add tests for layered resolution and legacy fallback  
- **New:** Unit tests for new services (RateGroup, BaseWorkRate, SITier, SITierRate)  
- **Unchanged:** PayrollServiceOrderCategoryTests, existing RateEngineServiceTests (must keep passing)

---

*End of audit. No code implementation until architecture is approved.*

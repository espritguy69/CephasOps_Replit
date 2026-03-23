# Service Profile — Audit and Design

## 1. Executive summary

Service Profiles are a new **additive** pricing layer that groups Order Categories into service families (e.g. RESIDENTIAL_FIBER, BUSINESS_FIBER, MAINTENANCE). This reduces BaseWorkRate duplication when multiple categories share the same pricing model. **Phase 1 delivers only the foundation**: domain entities, EF config, CRUD services, API, and admin UI. **Engine integration is deferred** to a later phase with a clear resolution-order plan.

---

## 2. Current-state audit: OrderCategoryId usage

### 2.1 BaseWorkRate

| Location | Usage |
|----------|--------|
| **Domain** `BaseWorkRate.cs` | `OrderCategoryId` (nullable) — when null, acts as broad fallback across categories. |
| **EF** `BaseWorkRateConfiguration.cs` | FK to OrderCategory, index `(RateGroupId, OrderCategoryId, InstallationMethodId, OrderSubtypeId)`. |
| **RateEngineService** `ResolveBaseWorkRateFromDbAsync` | All fallback tiers filter by `OrderCategoryId`: (a) RG+Cat+InstM+Subtype, (b) RG+Cat+InstM, (c) RG+Cat+Subtype, (d) RG+Cat, (e) RG only (OrderCategoryId == null). |
| **BaseWorkRateService** | List filter by `OrderCategoryId`; create/update validate FK and duplicate key `(RateGroupId, OrderCategoryId, InstallationMethodId, OrderSubtypeId)`. |
| **DTOs** `BaseWorkRateDto.cs` | List/Create/Update/Get include `OrderCategoryId`, `ClearOrderCategoryId`, and display names. |

### 2.2 RateModifier

- **Not relevant.** RateModifier uses `ModifierType` (InstallationMethod, SITier, Partner) and `ModifierValueId` / `ModifierValueString`. No OrderCategoryId.

### 2.3 RateEngineService

| Method / flow | OrderCategoryId usage |
|---------------|------------------------|
| `ResolveGponRatesAsync` | Passes `request.OrderCategoryId` into revenue, custom rate, payout, and base-work-rate resolution. |
| `ResolveGponRevenueRateInternalAsync` | Filters GponPartnerJobRate by `OrderCategoryId`. |
| `ResolveGponCustomRateAsync` | Filters GponSiCustomRate by `OrderCategoryId`. |
| `ResolveGponPayoutRateInternalAsync` | Filters GponSiJobRate by `OrderCategoryId`. |
| `ResolveBaseWorkRateAsync` | Cache key and DB resolution use `orderCategoryId`. |
| `ResolveBaseWorkRateFromDbAsync` | All candidate filters use `OrderCategoryId` (exact or null). |

### 2.4 UI / Admin

| Location | Usage |
|----------|--------|
| **RateEngineManagementPage.tsx** | Filters and form state include `orderCategoryId`; order categories loaded for dropdowns. |
| **OrderCategoriesPage.tsx** | CRUD for Order Categories (FTTH, FTTO, etc.). |
| **GponBaseWorkRatesController** (Base Work Rate UI) | List/Get/Create/Update use OrderCategoryId; dropdowns resolve category names. |
| **Sidebar** | Links to Order Categories and Rate Engine under Settings → GPON. |

### 2.5 APIs

| API | OrderCategoryId |
|-----|-----------------|
| **RatesController** | GponPartnerJobRate, GponSiJobRate, GponSiCustomRate: query filters, DTOs, CSV export. |
| **GponBaseWorkRatesController** | List filter `orderCategoryId`; create/update DTOs. |
| **OrderCategoriesController** | Full CRUD for OrderCategory. |
| **GponRateGroupsController** | N/A. |

### 2.6 Tests

| Test file | OrderCategoryId usage |
|-----------|------------------------|
| **RateEngineServiceTests.cs** | Requests and GponSiJobRate/GponSiCustomRate creation use `OrderCategoryId`. |
| **RateEngineServicePhase3Tests.cs** | BaseWorkRate and resolution requests use `OrderCategoryId`. |
| **RateEngineServiceRateModifierTests.cs** | `GponRateResolutionRequest.OrderCategoryId` and GponSiJobRate/GponSiCustomRate. |
| **BaseWorkRateServiceTests.cs** | Filter and create DTOs. |
| **RateGroupServiceTests.cs** | ResolveGponRatesAsync with OrderCategoryId. |
| **OrderPricingContextResolverTests.cs** | OrderCategoryId on context. |
| **BillingServiceInvoiceLineTests.cs** | Order has OrderCategoryId. |
| **PayrollServiceOrderCategoryTests.cs** | Rejects orders without OrderCategoryId. |
| **OrderProfitAlertServiceTests.cs** | GponSiJobRate and orders with OrderCategoryId. |

---

## 3. Proposed ServiceProfile model

### 3.1 Entity: ServiceProfile

| Field | Type | Notes |
|-------|------|--------|
| Id | Guid | PK. |
| CompanyId | Guid? | Company scope (nullable for global). |
| Code | string | Unique per company (e.g. RESIDENTIAL_FIBER, BUSINESS_FIBER, MAINTENANCE). |
| Name | string | Display name. |
| Description | string? | Optional. |
| IsActive | bool | Default true. |
| DisplayOrder | int | Sort order in UI. |
| CreatedAt, UpdatedAt | DateTime | From CompanyScopedEntity. |
| IsDeleted, DeletedAt, DeletedByUserId, RowVersion | | From CompanyScopedEntity. |

### 3.2 Entity: OrderCategoryServiceProfile

| Field | Type | Notes |
|-------|------|--------|
| Id | Guid | PK. |
| CompanyId | Guid? | Company scope. |
| OrderCategoryId | Guid | FK to OrderCategory. |
| ServiceProfileId | Guid | FK to ServiceProfile. |
| CreatedAt, UpdatedAt | DateTime | |
| IsDeleted, etc. | | CompanyScopedEntity. |

**Uniqueness:** One active mapping per OrderCategory per company: `(CompanyId, OrderCategoryId)` unique where `IsDeleted = false`. An order category may belong to at most one service profile at a time.

### 3.3 How ServiceProfile participates in pricing (recommendation)

- **BaseWorkRate** will gain an optional **ServiceProfileId** in a future phase. When set, the row applies to any Order Category that maps to that profile.
- **Resolution order (future):**
  1. Custom SI rate (unchanged).
  2. BaseWorkRate: **exact OrderCategoryId** match (current behaviour).
  3. BaseWorkRate: **ServiceProfileId** match (resolve profile from OrderCategoryId via mapping; then match BWR by ServiceProfileId).
  4. BaseWorkRate: broader fallback (e.g. OrderCategoryId = null / ServiceProfileId = null).
  5. Legacy GponSiJobRate (unchanged).
  6. RateModifiers applied after base amount (unchanged).
- **This phase does not add ServiceProfileId to BaseWorkRate** and does not change resolution. It only adds the entities, mappings, and admin so that a later phase can safely introduce profile-based resolution with fallback.

---

## 4. Phase C — Safe engine integration plan (deferred)

Planned resolution order when engine integration is implemented:

1. GponSiCustomRate (per-SI override).
2. BaseWorkRate **exact OrderCategoryId** match (current).
3. BaseWorkRate **ServiceProfileId** match (new; resolve profile from request.OrderCategoryId; then match BWR by ServiceProfileId + same other dimensions).
4. BaseWorkRate broader fallback (OrderCategoryId null / ServiceProfileId null).
5. GponSiJobRate (legacy).
6. RateModifiers (InstallationMethod → SITier → Partner).

Risks if done in one pass: cache key changes, regression in payout for existing data. Recommendation: **implement foundation only in this phase**; add BaseWorkRate.ServiceProfileId and resolution steps in a follow-up with feature flag or config.

---

## 5. Phase C — Engine integration: DEFERRED

Engine integration was **not** implemented in this pass to avoid risk. Current payout behaviour is unchanged.

**Intended resolution order when implemented later:**

1. GponSiCustomRate (per-SI override)
2. BaseWorkRate **exact OrderCategoryId** match (current)
3. BaseWorkRate **ServiceProfileId** match (new: resolve profile from request.OrderCategoryId via mapping; match BWR by ServiceProfileId)
4. BaseWorkRate broader fallback (OrderCategoryId null / ServiceProfileId null)
5. GponSiJobRate (legacy)
6. RateModifiers (InstallationMethod → SITier → Partner)

**Next-phase steps:**

- Add optional `ServiceProfileId` to BaseWorkRate entity and EF config.
- In `ResolveBaseWorkRateFromDbAsync`, after failing to find an OrderCategoryId match, resolve ServiceProfileId from OrderCategoryServiceProfile for the request’s OrderCategoryId; then try matching BWR by ServiceProfileId with same RateGroup/InstallationMethod/Subtype logic.
- Cache key and invalidation must include profile resolution.
- Add tests for “exact category wins over profile” and “profile fallback when no category match”.
- Consider feature flag or company-level setting to enable profile-based resolution.

---

## 5.1 Service Profile Rate Resolution Rules

When the rate engine resolves payout (Base Work Rate path), the following rules apply so that ServiceProfileId is used consistently.

### Resolution order (implemented)

1. **GponSiCustomRate** – Per–service installer override (OrderTypeId, OrderCategoryId, InstallationMethodId, PartnerGroupId). No ServiceProfileId; legacy table uses OrderCategoryId only.
2. **BaseWorkRate exact OrderCategoryId** – Rows with `OrderCategoryId = request.OrderCategoryId` and `ServiceProfileId == null` (same RateGroup, InstallationMethodId, OrderSubtypeId). Takes precedence over profile.
3. **BaseWorkRate ServiceProfileId** – When no exact category row exists: resolve ServiceProfileId from **OrderCategoryServiceProfile** for the request’s OrderCategoryId (company-scoped). Then match BWR rows with `OrderCategoryId == null` and `ServiceProfileId == resolved profile` (same RateGroup, InstallationMethodId, OrderSubtypeId).
4. **BaseWorkRate broad fallback** – Rows with both `OrderCategoryId == null` and `ServiceProfileId == null` (rate group only).
5. **GponSiJobRate** – Legacy payout by OrderTypeId, OrderCategoryId, InstallationMethodId, SiLevel, PartnerGroupId. No ServiceProfileId.
6. **RateModifiers** – Applied after base amount (InstallationMethod → SITier → Partner).

### When ServiceProfileId is present, null, or not configured

- **Present:** Order category has a mapping in OrderCategoryServiceProfile → profile-based BWR is tried (step 3) when no exact category row (step 2) matches.
- **Null / not configured:** No mapping for that OrderCategoryId → step 3 is skipped; resolution goes from step 2 (exact category) to step 4 (broad) or step 5 (legacy).

### Cache key (Base Work Rate)

The in-memory cache key for Base Work Rate resolution includes **ServiceProfileId** so that different profile mappings do not share the same cache entry incorrectly:

`BWR:{CompanyId}:{RateGroupId}:{OrderCategoryId}:{ServiceProfileId}:{InstallationMethodId}:{OrderSubtypeId}:{Date}`

ServiceProfileId is resolved from OrderCategoryServiceProfile before building the key (or `"n"` when no mapping). This avoids collisions when the same OrderCategoryId later gets a different profile mapping or when two categories map to different profiles.

### GponSiJobRate and GponSiCustomRate

These tables do **not** have a ServiceProfileId column. Lookups use **OrderCategoryId** (and OrderTypeId, InstallationMethodId, SiLevel, PartnerGroupId as applicable). No query or cache changes are required for them.

### Payroll pipeline

Payroll uses `IRateEngineService.ResolveGponRatesAsync(GponRateResolutionRequest)` with `OrderCategoryId` from the order. Resolution (including Base Work Rate with exact category and ServiceProfile fallback) is unchanged; no extra parameters required.

---

## 6. Files added/changed (foundation)

**Domain:** `ServiceProfile.cs`, `OrderCategoryServiceProfile.cs`  
**Infrastructure:** `ServiceProfileConfiguration.cs`, `OrderCategoryServiceProfileConfiguration.cs`, `ApplicationDbContext.cs` (DbSets), `20260308133701_AddServiceProfiles.cs`  
**Application:** DTOs `ServiceProfileDto.cs`, `OrderCategoryServiceProfileDto.cs`; interfaces and services `IServiceProfileService`, `ServiceProfileService`, `IOrderCategoryServiceProfileService`, `OrderCategoryServiceProfileService`  
**API:** `ServiceProfilesController.cs`, `ServiceProfileMappingsController.cs`; `Program.cs` (service registration)  
**Frontend:** `api/serviceProfiles.ts`, `pages/settings/ServiceProfilesPage.tsx`, `pages/settings/ServiceProfileMappingsPage.tsx`; `App.tsx` (routes), `Sidebar.tsx` (links)  
**Tests:** `ServiceProfileServiceTests.cs`, `OrderCategoryServiceProfileServiceTests.cs`  
**Script:** `backend/scripts/apply-AddServiceProfiles.sql` (idempotent)

---

## Deliverable summary (requested format)

1. **Executive summary**  
   Service Profile is added as a first-class pricing concept. Order Categories can be grouped under a Service Profile (e.g. RESIDENTIAL_FIBER, BUSINESS_FIBER). Admins manage profiles and Order Category → Service Profile mappings in Settings → GPON. **Engine integration is deferred**; no change to payout resolution. System is ready for a later step where BaseWorkRate can target ServiceProfileId to reduce duplication.

2. **Current-state audit**  
   See §2. OrderCategoryId is used in BaseWorkRate (entity, resolution fallback tiers, cache key), RateEngineService (revenue, custom, payout, base-work-rate resolution), BaseWorkRateService (list/CRUD/validation), GponPartnerJobRate / GponSiJobRate / GponSiCustomRate (APIs and resolution). RateModifier does not use OrderCategoryId.

3. **Proposed ServiceProfile model**  
   See §3. Entities: ServiceProfile (Id, CompanyId, Code, Name, Description, IsActive, DisplayOrder, audit fields), OrderCategoryServiceProfile (Id, CompanyId, OrderCategoryId, ServiceProfileId, audit fields). Uniqueness: (CompanyId, Code) for profiles; (CompanyId, OrderCategoryId) for mappings.

4. **Exact files changed**  
   See §6 above.

5. **Migrations added**  
   `20260308133701_AddServiceProfiles.cs` (ServiceProfiles + OrderCategoryServiceProfiles tables and indexes). Idempotent script: `backend/scripts/apply-AddServiceProfiles.sql`.

6. **API endpoints added**  
   - `GET/POST /api/settings/gpon/service-profiles`, `GET/PUT/DELETE /api/settings/gpon/service-profiles/{id}`  
   - `GET/POST /api/settings/gpon/service-profile-mappings`, `GET/DELETE /api/settings/gpon/service-profile-mappings/{id}`  
   (List supports query filters: isActive, search for profiles; serviceProfileId, orderCategoryId for mappings.)

7. **Frontend screens added**  
   - **Settings → GPON → Service Profiles**: list, create, edit, delete profiles; short explanation of what Service Profiles are.  
   - **Settings → GPON → Service Profile Mappings**: list, add (Order Category + Service Profile), remove mapping; explanation of one-category-to-one-profile.

8. **Engine integration**  
   **Implemented.** BaseWorkRate.ServiceProfileId and resolution order (exact OrderCategory → ServiceProfile → broad fallback) are in place. See [SERVICE_PROFILE_ENGINE_INTEGRATION_STATUS.md](SERVICE_PROFILE_ENGINE_INTEGRATION_STATUS.md).

9. **If implemented, exact resolution order**  
   See §5: Custom SI → BaseWorkRate exact OrderCategory → BaseWorkRate ServiceProfile → broader BWR fallback → GponSiJobRate → modifiers.

10. **Risks / follow-up**  
    - **Risks:** None for current foundation; no engine or payout logic changed.  
    - **Follow-up:** (a) ~~Add optional ServiceProfileId to BaseWorkRate and resolution logic with fallback~~ **Done.** (b) ~~Cache key/expiry when profile-based resolution is added~~ **Done.** (c) ~~Tests for “exact category wins over profile” and “profile fallback”; (d) Optional: feature flag or company setting for profile-based resolution. Tests for exact category vs profile are in `RateEngineServiceServiceProfileResolutionTests.cs`.

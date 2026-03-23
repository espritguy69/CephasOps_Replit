# GPON Pricing Model Redesign — Layered Work-Based & SI Payout

**Date:** 2026-03-08  
**Status:** Design proposal (audit complete; no implementation yet)  
**Related:** [GPON_RATE_ENGINE_DIMENSIONS_AUDIT.md](GPON_RATE_ENGINE_DIMENSIONS_AUDIT.md), [payroll_rate_overview.md](business/payroll_rate_overview.md)

---

## 1. Executive Summary

The current GPON pricing model keys **SI payout** and **partner revenue** directly on **Order Type + Order Category + Installation Method + SI Level** (and optional Partner Group). This forces full rate tables per SI level (Junior/Senior) and makes it hard to:

- Share pricing across job families (e.g. Assurance and VAS) without duplicating rows.
- Support Activation without an Order Subtype while still allowing subtype overrides where needed.
- Add new SI tiers (e.g. Specialist) or per-installer overrides without cloning the entire grid.

This document proposes a **layered model**:

| Layer | Purpose | Example |
|-------|---------|--------|
| **1. Base Work Rate** | Work-based price by Rate Group, Order Category, Installation Method, optional Subtype | “FTTH Activation Prelaid” = 80 MYR |
| **2. SI Tier Rate** | Tier (Junior/Senior/Specialist) adjusts or overrides base | Senior = 1.1× base; Specialist = fixed 95 MYR |
| **3. SI Personal Override** | Per-installer exception | SI “Ali” gets 90 MYR for this combo |

**Resolution priority:** SI Override → SI Tier Rate → Base Work Rate.

The design is **additive and staged**: introduce Rate Group and Base Work Rate first, then SI Tier and overrides, with a clear migration path from the current `GponSiJobRate` / `GponSiCustomRate` structure.

---

## 2. Current-State Assessment

### 2.1 Rate and payout entities (backend)

| Entity | Purpose | Key dimensions | Used by |
|--------|---------|----------------|---------|
| **GponSiJobRate** | Default SI payout by job dimensions + level | OrderTypeId, OrderCategoryId, InstallationMethodId, **SiLevel** (string), PartnerGroupId | RateEngineService → Payroll (base payout) |
| **GponSiCustomRate** | Per-SI payout override | ServiceInstallerId, OrderTypeId, OrderCategoryId, InstallationMethodId, PartnerGroupId | RateEngineService (checked first) |
| **GponPartnerJobRate** | Partner revenue | PartnerGroupId, PartnerId?, OrderTypeId, OrderCategoryId, InstallationMethodId | RateEngineService (revenue) |
| **SiRatePlan** | KPI/bonus/penalty and legacy flat rates | DepartmentId?, ServiceInstallerId, InstallationMethodId?, Level, PrelaidRate, NonPrelaidRate, ActivationRate, … | Payroll (OnTimeBonus, LatePenalty only); **not** used for base job payout |

**Finding:** Base job payout is resolved only via **RateEngineService**: first `GponSiCustomRate`, then `GponSiJobRate` by `SiLevel`. `SiRatePlan` is used only for KPI adjustments (OnTimeBonus, LatePenalty), not for the base rate amount.

### 2.2 How installer payout is stored and resolved

- **Stored:**  
  - **GponSiJobRate:** one row per (OrderTypeId, OrderCategoryId, InstallationMethodId, SiLevel, PartnerGroupId?) with `PayoutAmount`.  
  - **GponSiCustomRate:** one row per (ServiceInstallerId, OrderTypeId, OrderCategoryId, InstallationMethodId, PartnerGroupId?) with `CustomPayoutAmount`.
- **SI level:** Comes from **ServiceInstaller.SiLevel** (enum `InstallerLevel`: Junior, Senior). Payroll passes `SiLevel.ToString()` into the rate engine. `GponSiJobRate.SiLevel` is a string (allows “Subcon” in data even though the enum does not).
- **Resolution (RateEngineService):**  
  1. If `ServiceInstallerId` provided → `ResolveGponCustomRateAsync` (exact match on SI + OrderType + OrderCategory + InstallationMethod + PartnerGroup).  
  2. Else if `SiLevel` provided → `ResolveGponPayoutRateInternalAsync` (match on OrderType + OrderCategory + InstallationMethod + SiLevel + PartnerGroup).  
  3. PartnerGroup: partner-specific row preferred, then default (PartnerGroupId null).

### 2.3 Order Type and Subtype

- **Order** has a single **OrderTypeId** (leaf: either a parent or a child “subtype”). There is no separate OrderSubtypeId.
- Rates are keyed by **OrderTypeId** only. So “Activation” without subtype is already supported when the order uses the parent Order Type; subtypes (e.g. MODIFICATION_INDOOR) are different OrderTypeIds and get their own rate rows.
- **Gap:** There is no concept of “base rate for Activation, override for MODIFICATION_INDOOR only”; every combination is a separate row. A **Rate Group** could group order types and carry one base rate with optional subtype overrides.

### 2.4 Pain points

1. **Duplication:** Junior and Senior each require a full set of GponSiJobRate rows (OrderType × OrderCategory × InstallationMethod × PartnerGroup). Adding a new tier multiplies rows again.
2. **No shared “work” price:** Assurance and VAS cannot share one base work rate; each OrderType has its own rows.
3. **Subtype handling:** Subtype-specific pricing is done only by adding more OrderType rows and more rate rows; no “base + subtype override” pattern.
4. **Individual overrides:** GponSiCustomRate already avoids cloning the full grid (one row per overridden combination per SI). The main issue is the flatness of the default grid (GponSiJobRate), not the override mechanism itself.

### 2.5 What already works

- **Per-SI overrides:** GponSiCustomRate is correct in intent and resolution order (checked first). The redesign keeps this as Layer 3.
- **Activation without subtype:** Supported today by using the parent OrderTypeId; no schema change required for “Activation works without subtype.”
- **Partner revenue:** GponPartnerJobRate is separate and can remain as-is; the redesign focuses on **payout** (SI) layers. Revenue can later be aligned to Rate Group if desired.

---

## 3. Target Entity / Model Design

### 3.1 Concepts

- **Rate Group**  
  Groups one or more Order Types for pricing (e.g. “Activation”, “Assurance & VAS”). Used to define **base work rates** once per group instead of per order type. Enables Activation (single type in group) and Assurance+VAS (multiple types sharing the same group rates).

- **Base Work Rate (Layer 1)**  
  Work-based price keyed by: **RateGroupId**, **OrderCategoryId**, **InstallationMethodId**, and optionally **OrderTypeId** (subtype override). No SI dimension. One row can cover all order types in the group (OrderTypeId null) or override for a specific subtype (OrderTypeId set).

- **SI Tier (e.g. Junior, Senior, Specialist)**  
  Stored on **ServiceInstaller** or a profile. Tier can define either a **multiplier** (e.g. 1.0, 1.1) or a **fixed override** per rate group (or per base rate key). Tier applies to “all SIs in this tier” without duplicating base rows.

- **SI Tier Rate (Layer 2)**  
  Optional tier-level override or adjustment: keyed by **RateGroupId** (or BaseWorkRate key), **SITierCode** (e.g. "Junior", "Senior", "Specialist"). Can store **PayoutAmount** (override) or **Multiplier** (e.g. 1.1). If present, replaces or adjusts base for that tier.

- **SI Personal Override (Layer 3)**  
  Same idea as current **GponSiCustomRate**: keyed by **ServiceInstallerId** + same work dimensions (e.g. RateGroupId + OrderCategoryId + InstallationMethodId, or OrderTypeId + OrderCategoryId + InstallationMethodId for backward compatibility). Highest priority.

### 3.2 Proposed entities (additive)

#### RateGroup (new)

| Column | Type | Description |
|--------|------|-------------|
| Id | Guid | PK |
| CompanyId | Guid | Tenant |
| Code | string | e.g. "ACTIVATION", "ASSURANCE_VAS" |
| Name | string | Display name |
| IsActive | bool | |
| DisplayOrder | int? | For UI |

**RateGroupOrderType (junction)**  
- RateGroupId, OrderTypeId.  
- An OrderType can belong to at most one Rate Group (for payout). Ensures “Activation” or “Assurance & VAS” map to one group.

#### BaseWorkRate (new) — Layer 1

| Column | Type | Description |
|--------|------|-------------|
| Id | Guid | PK |
| CompanyId | Guid | Tenant |
| RateGroupId | Guid | FK → RateGroup |
| OrderCategoryId | Guid | FK → OrderCategories |
| InstallationMethodId | Guid? | null = all methods |
| OrderTypeId | Guid? | null = group default; set = subtype override |
| PartnerGroupId | Guid? | null = default payout context |
| PayoutAmount | decimal | Base work rate (MYR) |
| Currency | string | e.g. MYR |
| ValidFrom | DateTime? | |
| ValidTo | DateTime? | |
| IsActive | bool | |

**Unique / index:** (RateGroupId, OrderCategoryId, InstallationMethodId, OrderTypeId, PartnerGroupId) + validity.

**Resolution:** For a given Order (OrderTypeId, OrderCategoryId, InstallationMethodId) and PartnerGroupId: resolve Order’s Rate Group → then find BaseWorkRate by (RateGroupId, OrderCategoryId, InstallationMethodId, OrderTypeId, PartnerGroupId); if not found, fall back to (RateGroupId, OrderCategoryId, InstallationMethodId, null, PartnerGroupId). So **Activation works without subtype** (OrderTypeId null row), and **subtype override** is a more specific row with OrderTypeId set.

#### SITier (new) — reference data

| Column | Type | Description |
|--------|------|-------------|
| Id | Guid | PK |
| CompanyId | Guid | Tenant |
| Code | string | e.g. "Junior", "Senior", "Specialist" |
| Name | string | Display name |
| IsActive | bool | |
| DisplayOrder | int? | |

ServiceInstaller gets **SITierId** (FK, nullable) or **SITierCode** (string). Prefer FK; code can be used for backward compatibility with existing SiLevel string.

#### SITierRate (new) — Layer 2

| Column | Type | Description |
|--------|------|-------------|
| Id | Guid | PK |
| CompanyId | Guid | Tenant |
| RateGroupId | Guid | FK → RateGroup (or key by BaseWorkRate if preferred) |
| OrderCategoryId | Guid | Same dimensions as base for clarity |
| InstallationMethodId | Guid? | |
| OrderTypeId | Guid? | Optional subtype scope |
| SITierId | Guid | FK → SITier |
| PartnerGroupId | Guid? | |
| PayoutAmount | decimal? | Fixed override for this tier (if set) |
| Multiplier | decimal? | e.g. 1.1 (if PayoutAmount not set, apply to base) |
| Currency | string | |
| ValidFrom | DateTime? | |
| ValidTo | DateTime? | |
| IsActive | bool | |

**Resolution:** For resolved base amount and SI’s tier: if SITierRate exists with PayoutAmount, use it; else if Multiplier set, use base × Multiplier; else use base. So **junior/senior (and specialist) differences** are supported without duplicating base rows.

#### SI Override Rate (Layer 3) — evolve GponSiCustomRate

Keep **GponSiCustomRate** but extend keying options:

- **Option A (minimal):** Keep (ServiceInstallerId, OrderTypeId, OrderCategoryId, InstallationMethodId, PartnerGroupId). Resolution stays as today; new layers only apply when no custom rate exists. No schema change.
- **Option B (align to groups):** Add optional **RateGroupId** and allow keying by (ServiceInstallerId, RateGroupId, OrderCategoryId, InstallationMethodId, OrderTypeId?, PartnerGroupId). Resolution: custom by (SI, RateGroup, Category, Method, Subtype, Partner) or (SI, OrderType, Category, Method, Partner). Prefer exact match, then fall back to group default.

Recommendation: **Option A** for Phase 1; Option B when Rate Group is the primary key in UI and resolution.

### 3.3 SI profile with tier

- **ServiceInstaller** already has **SiLevel** (enum: Junior, Senior).  
- Add **SITierId** (Guid?, FK to SITier) and/or keep **SiLevel** and map SiLevel → SITier in code (Junior → “Junior”, Senior → “Senior”).  
- New tiers (e.g. Specialist) use SITier table; existing data keeps SiLevel and a one-time migration sets SITierId from SiLevel.

### 3.4 Partner revenue (unchanged for now)

- **GponPartnerJobRate** remains keyed by PartnerGroupId, PartnerId?, OrderTypeId, OrderCategoryId, InstallationMethodId.  
- Later, revenue can optionally be keyed by RateGroupId + optional OrderTypeId for consistency; out of scope for this redesign.

---

## 4. Payout Resolution Logic

### 4.1 Resolution order (payout only)

1. **SI Personal Override (Layer 3)**  
   GponSiCustomRate: (ServiceInstallerId, OrderTypeId, OrderCategoryId, InstallationMethodId, PartnerGroupId). If found and valid → return CustomPayoutAmount, stop.

2. **Resolve Base Work Rate (Layer 1)**  
   - Resolve Rate Group from Order.OrderTypeId (via RateGroupOrderType).  
   - Query BaseWorkRate by (RateGroupId, OrderCategoryId, InstallationMethodId, OrderTypeId, PartnerGroupId); if missing, then (RateGroupId, OrderCategoryId, InstallationMethodId, null, PartnerGroupId).  
   - Base amount = PayoutAmount (or null if not found).

3. **SI Tier Rate (Layer 2)**  
   - Get SI’s tier (SITierId or SiLevel → SITier).  
   - If Base amount is null, skip tier; resolution fails or falls back to legacy (see migration).  
   - Look up SITierRate for (RateGroupId, OrderCategoryId, InstallationMethodId, OrderTypeId?, SITierId, PartnerGroupId).  
   - If row with PayoutAmount → return PayoutAmount.  
   - Else if row with Multiplier → return Base × Multiplier.  
   - Else → return Base.

4. **Legacy fallback (during migration)**  
   If no BaseWorkRate (and no SITierRate) for this context, fall back to current **GponSiJobRate** by (OrderTypeId, OrderCategoryId, InstallationMethodId, SiLevel, PartnerGroupId). This allows staged cutover.

### 4.2 Resolution priority (summary)

- **SI-specific override** (GponSiCustomRate)  
- **SI tier rate** (SITierRate: fixed amount or multiplier on base)  
- **Base work rate** (BaseWorkRate)  
- **Legacy** GponSiJobRate (until deprecated)

### 4.3 Activation without subtype

- Rate Group “ACTIVATION” contains one Order Type (Activation).  
- BaseWorkRate rows use **OrderTypeId = null** for “group default” so one row per (RateGroupId, OrderCategoryId, InstallationMethodId) is enough. Activation orders that use the parent OrderTypeId resolve via that default. No subtype required.

### 4.4 Assurance and VAS sharing pricing

- One Rate Group “ASSURANCE_VAS” with multiple Order Types (Assurance, VAS, etc.).  
- BaseWorkRate keyed by (RateGroupId, OrderCategoryId, InstallationMethodId, OrderTypeId=null, PartnerGroupId) so **one row per (Category, Method)** serves all order types in the group. Assurance and VAS share the same base; no duplicated rows. Subtype overrides (optional) add rows with OrderTypeId set.

### 4.5 Junior / Senior / Specialist

- SITier table: Junior, Senior, Specialist (and any future tier).  
- SITierRate: per (RateGroup, Category, Method, optional OrderType, SITier, PartnerGroup) either **PayoutAmount** (override) or **Multiplier**.  
- One base row + one tier row per tier gives different payout per level without duplicating the full grid.

### 4.6 Individual installer special rates

- **GponSiCustomRate** remains: one row per (SI, OrderType, OrderCategory, InstallationMethod, PartnerGroup) only where an exception is needed. No need to clone all base or tier rates.

---

## 5. Migration Strategy

### 5.1 Principles

- **Additive first:** New tables (RateGroup, BaseWorkRate, SITier, SITierRate); no drop of GponSiJobRate/GponSiCustomRate until full cutover.
- **Dual-read during transition:** Resolution tries new layers first; if no base/tier rate, falls back to GponSiJobRate (and GponSiCustomRate still first).
- **Data migration scripts:**  
  - Create default Rate Groups (e.g. one per distinct OrderTypeId in GponSiJobRate, or “ACTIVATION”, “MODIFICATION”, “ASSURANCE_VAS”).  
  - Create RateGroupOrderType from current Order Types.  
  - Backfill BaseWorkRate from GponSiJobRate: for each (OrderTypeId, OrderCategoryId, InstallationMethodId, PartnerGroupId), pick one reference level (e.g. Junior) and insert one BaseWorkRate with OrderTypeId=null per group (or one per OrderTypeId if you prefer subtype from day one).  
  - Create SITier (Junior, Senior, Subcon if used).  
  - Backfill SITierRate from existing GponSiJobRate rows (per level): either as fixed PayoutAmount or as multiplier derived from Junior base.
- **Feature flag or config:** “Use layered payout resolution” so rollout can be per company or global after validation.

### 5.2 Phased cutover

1. **Phase 1:** Add RateGroup, BaseWorkRate, SITier, SITierRate; add SITierId to ServiceInstaller (nullable). Resolution: Custom → Base → Tier (if base found); else Legacy GponSiJobRate. Migrate data; run in shadow or parallel and compare.  
2. **Phase 2:** Prefer Base + Tier everywhere; make legacy read-only or hide from UI.  
3. **Phase 3:** Deprecate GponSiJobRate (and optionally merge GponSiCustomRate into a single “SI Override” table keyed by group if desired).  
4. **SiRatePlan:** Leave as-is for KPI bonus/penalty; no change to payroll flow for OnTimeBonus/LatePenalty.

---

## 6. UI / Settings Recommendations

### 6.1 Rate Groups

- **Settings → Rate Groups** (or under “Rate Engine”): List/create/edit Rate Groups; assign Order Types to a group (one-to-one: each Order Type in at most one group).  
- Show which Order Types are in each group so “Assurance & VAS” is clear.

### 6.2 Base Work Rates

- **Settings → Base Work Rates** (or “Work rates” under Rate Engine): Grid or form keyed by Rate Group, Order Category, Installation Method, optional Subtype (Order Type), optional Partner Group.  
- One “default” row per (Group, Category, Method) with Subtype empty; optional rows with Subtype for overrides.  
- Activation: single default row per (Activation group, Category, Method); no subtype needed.

### 6.3 SI Tiers

- **Settings → SI Tiers:** CRUD for Junior, Senior, Specialist, etc.  
- **Service Installer** form: dropdown or display for SI Tier (and keep or hide SiLevel depending on migration).

### 6.4 SI Tier Rates

- **Settings → SI Tier Rates:** Keyed by Rate Group (and Category, Method, optional Subtype), SI Tier, Partner Group.  
- Fields: PayoutAmount (fixed override) or Multiplier.  
- UI can show “Base 80 MYR × 1.1 = 88 MYR” when Multiplier is used.

### 6.5 SI Overrides (existing)

- Keep **Settings → Rate Engine → Custom / SI Overrides** (current GponSiCustomRate UI).  
- Optionally allow selecting by Rate Group + dimensions instead of only Order Type, when Option B (group keying) is implemented.

### 6.6 Rate Engine Management page

- **Rate calculator:** Pass OrderTypeId, OrderCategoryId, InstallationMethodId, PartnerGroupId, ServiceInstallerId, SiLevel (or SITierId).  
- Show resolution steps: Override → Tier → Base → Legacy.  
- Tabs or sections: Partner Revenue (unchanged), Base Work Rates, SI Tier Rates, SI Overrides (existing custom rates).

---

## 7. Staged Implementation Plan

| Stage | Deliverable | Risk |
|-------|-------------|------|
| 1 | Design doc (this document) and stakeholder sign-off | Low |
| 2 | Add RateGroup + RateGroupOrderType; API and minimal UI (list/create); data migration: one group per order type or agreed grouping | Low |
| 3 | Add BaseWorkRate entity, config, API; resolution extended to read BaseWorkRate (with fallback to GponSiJobRate); migration script from GponSiJobRate (e.g. Junior as base) | Medium |
| 4 | Base Work Rate UI: grid/form by Rate Group, Category, Method, Subtype | Low |
| 5 | Add SITier + SITierRate; ServiceInstaller.SITierId; resolution Layer 2; migration from GponSiJobRate per level (multiplier or fixed) | Medium |
| 6 | SI Tier and SI Tier Rate UI; Rate Calculator shows layered steps | Low |
| 7 | Validation: payroll runs in parallel (old vs new resolution); reconcile and fix edge cases | Medium |
| 8 | Default to new resolution; make GponSiJobRate read-only in UI; deprecation notice | Low |
| 9 | (Optional) GponSiCustomRate keyed by RateGroupId; cleanup GponSiJobRate | Low |

---

## 8. Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Migration maps Order Types to groups incorrectly | Scripts idempotent; review mapping (Activation, Modification, Assurance, VAS) with business; dry-run in staging. |
| Base amount zero or null for some context | Resolution falls back to legacy GponSiJobRate during transition; validation job to list orders that would resolve to null base. |
| Tier multiplier vs fixed ambiguity | Business rule: if SITierRate.PayoutAmount set, use it; else use Multiplier; document in UI. |
| SiLevel vs SITierId dual source | Prefer SITierId; backfill from SiLevel; one-time sync and then use SITierId only. |
| Partner revenue out of sync | Leave GponPartnerJobRate as-is; later optional “revenue by rate group” phase. |
| Performance (extra lookups) | Index (RateGroupId, OrderCategoryId, InstallationMethodId, OrderTypeId, PartnerGroupId) on BaseWorkRate; same for SITierRate; cache reference data (Rate Groups, Tiers) where appropriate. |

---

## 9. Summary

- **Current:** SI payout is a flat grid (GponSiJobRate by OrderType, Category, Method, SiLevel) plus per-SI overrides (GponSiCustomRate). Payroll uses RateEngineService only for base amount; SiRatePlan only for KPI adjustments.  
- **Target:** Layered model — Base Work Rate (by Rate Group, Category, Method, optional Subtype) → SI Tier Rate (multiplier or fixed per tier) → SI Override (existing custom rate).  
- **Activation** works without subtype via group default (OrderTypeId null). **Assurance and VAS** share one Rate Group and one set of base rows. **Junior/Senior/Specialist** use tier rows instead of duplicating the grid. **Individual overrides** stay as today (one row per exception).  
- **Migration:** Additive tables and resolution with legacy fallback; phased cutover and validation; then deprecate GponSiJobRate.  
- **UI:** Rate Groups, Base Work Rates, SI Tiers, SI Tier Rates, plus existing SI Overrides and Rate Calculator with resolution steps.

No reckless rewrite: current paths are understood, and the redesign is additive and staged with clear rollback (keep using legacy resolution until new layers are validated).

# Versioned Pricing Architecture — Design & Rollout Plan

## Executive summary

This document proposes a **versioned pricing architecture** for the GPON pricing engine that makes the system harder to break and supports safe draft changes, controlled publishing, and historical payout auditability. The design is **additive**: existing resolution logic and persistence stay in place; a new **PricingRuleSet** concept and optional **PricingCalculationSnapshot** provide versioning and immutability without rewriting the current engine.

**Outcomes:**

- **Versioned rule sets:** Pricing entities can be grouped into named, versioned sets (Draft / Published / Archived) with effective dating.
- **Safe drafts:** Changes are made in draft rule sets; production resolution uses only published sets until a publish action.
- **Immutable audit trail:** Completed jobs can store a full calculation snapshot (base, modifiers, override, final payout, trace) tied to the rule set version used.
- **Stable payroll explanations:** Historical payouts remain explainable even after pricing is updated, by reading from the snapshot.

**Strict constraints honoured:** No rewrite of current pricing logic; additive implementation only; existing payout resolution remains the source of truth; legacy (GponSiJobRate) compatibility preserved.

---

## 1. Audit: current pricing persistence

### 1.1 Where rates and modifiers are stored

| Entity | Table | Key attributes | Effective dating | Company scope |
|--------|--------|----------------|------------------|---------------|
| **BaseWorkRate** | BaseWorkRates | RateGroupId, OrderCategoryId, ServiceProfileId, InstallationMethodId, OrderSubtypeId, Amount | EffectiveFrom, EffectiveTo | CompanyId (nullable) |
| **RateModifier** | RateModifiers | ModifierType, ModifierValueId/ModifierValueString, AdjustmentType, AdjustmentValue, Priority | **None** | CompanyId (nullable) |
| **ServiceProfile** | ServiceProfiles | Code, Name, IsActive | **None** (metadata) | CompanyId (nullable) |
| **OrderCategoryServiceProfile** | OrderCategoryServiceProfiles | OrderCategoryId, ServiceProfileId | **None** | CompanyId (nullable) |
| **OrderTypeSubtypeRateGroup** | OrderTypeSubtypeRateGroups | OrderTypeId, OrderSubtypeId, RateGroupId | **None** | CompanyId (nullable) |
| **RateGroup** | RateGroups | Name, Code, IsActive | **None** | CompanyId (nullable) |
| **GponSiCustomRate** | GponSiCustomRates | ServiceInstallerId, OrderTypeId, OrderCategoryId, InstallationMethodId, PartnerGroupId, CustomPayoutAmount | ValidFrom, ValidTo | CompanyId (nullable) |
| **GponSiJobRate** | GponSiJobRates | OrderTypeId, OrderCategoryId, InstallationMethodId, SiLevel, PartnerGroupId, PayoutAmount | ValidFrom, ValidTo | CompanyId (nullable) |

### 1.2 How resolution uses them

- **RateEngineService.ResolveGponRatesAsync** (and internal helpers):
  - **Custom:** `GponSiCustomRates` by SI, order type, category, installation method, partner group; filtered by CompanyId and `ValidFrom`/`ValidTo` vs reference date.
  - **Base path:** `OrderTypeSubtypeRateGroup` → RateGroupId; then `BaseWorkRates` by RateGroupId, order category (exact or via `OrderCategoryServiceProfile` → ServiceProfileId), installation method, subtype; filtered by CompanyId and `EffectiveFrom`/`EffectiveTo` vs reference date.
  - **Legacy fallback:** `GponSiJobRates` by order type, category, installation method, SiLevel, partner group; filtered by CompanyId and ValidFrom/ValidTo.
  - **Modifiers:** `RateModifiers` by CompanyId and IsActive (no date filter); applied in order InstallationMethod → SITier → Partner.

None of these entities currently reference a “rule set” or version; resolution is “live” from the tables.

### 1.3 How this would attach to a versioned rule set

- Introduce a **PricingRuleSet** entity (see below).
- Add an optional **PricingRuleSetId** (nullable FK) to each pricing entity that should be versioned. Rows with **null** PricingRuleSetId continue to mean “global / unversioned” and stay in play for backward compatibility.
- Resolution behaviour: **add** a rule-set filter only when the caller supplies a rule set (e.g. “use Published rule set for company X”). When no rule set is supplied, keep current behaviour (all rows with matching CompanyId/effective date, ignoring rule set). This preserves existing payroll and API behaviour until the new flow is adopted.

---

## 2. PricingRuleSet model

### 2.1 Recommended entity

```text
PricingRuleSet
--------------
Id                    Guid (PK)
CompanyId             Guid? (nullable for global; align with CompanyScopedEntity)
VersionNumber         int (e.g. 1, 2, 3; unique per company or per company+name)
Name                  string (e.g. "GPON Payout 2025-Q1")
Description           string? (optional)
Status                enum: Draft | Published | Archived
EffectiveFrom         DateTime? (when this set becomes effective; for Published sets)
EffectiveTo           DateTime? (when this set is superseded; null = current)
PublishedAt           DateTime? (set when Status → Published)
PublishedByUserId     Guid? (audit)
CreatedAt             DateTime
UpdatedAt             DateTime
CreatedByUserId       Guid?
```

- **Status:**  
  - **Draft:** Editable; not used for production resolution.  
  - **Published:** Used for production when EffectiveFrom ≤ reference date and (EffectiveTo is null or ≥ reference date). Immutable after publish (or only minor metadata edits, depending on policy).  
  - **Archived:** No longer used for resolution; kept for history.
- **EffectiveFrom / EffectiveTo:** Define the validity window of the rule set for resolution. Only one Published set per company (and optionally per “name” or “scope”) should cover a given date; publishing can set EffectiveFrom = publish time and set EffectiveTo on the previous published set.
- **VersionNumber:** Monotonically increasing per company (or per company + logical name) so that “latest published” and ordering are clear.

### 2.2 Optional: Rule set “scope” or “name”

- If multiple independent rule sets per company are needed (e.g. “GPON” vs “NWO”), add **Scope** or **RateContext** (e.g. `GPON_JOB`) so that resolution can ask for “published rule set for company X and scope GPON_JOB”.

---

## 3. Which pricing entities should belong to a PricingRuleSet

### 3.1 Recommendation

| Entity | Attach to rule set? | Notes |
|--------|---------------------|--------|
| **BaseWorkRate** | **Yes** | Core payout base; versioning gives “draft vs live” and effective dating at set level. |
| **RateModifier** | **Yes** | Modifiers are part of the same “pricing version”; draft and publish together with base. |
| **ServiceProfile** | **Optional (metadata)** | Profiles are mostly structure (category → family). Option A: leave unversioned. Option B: add PricingRuleSetId so draft can change category–profile mapping in a separate table (e.g. OrderCategoryServiceProfile) or profile list. Prefer **Option A** initially; add later if needed. |
| **OrderCategoryServiceProfile** | **Optional** | If ServiceProfile is unversioned, keep this unversioned. If profiles are versioned, add PricingRuleSetId here. |
| **OrderTypeSubtypeRateGroup** | **Yes** | Determines which rate group an order type uses; part of “this version’s” mapping. |
| **RateGroup** | **Optional** | Usually structural. Prefer **unversioned**; version the rows that reference it (BaseWorkRate, OrderTypeSubtypeRateGroup). If a draft needs a new rate group, create it with null rule set first, then attach BWR/OTSRG to draft set. |
| **GponSiCustomRate** | **Yes (recommended)** | SI overrides are pricing; include in rule set so “draft custom rates” don’t affect live until published. |
| **GponSiJobRate** | **Yes (legacy)** | Legacy fallback; versioning allows retiring or changing legacy rates in a controlled way. |

**Minimum for Phase 3:** BaseWorkRate, RateModifier, OrderTypeSubtypeRateGroup. Then add GponSiCustomRate and GponSiJobRate. ServiceProfile / OrderCategoryServiceProfile / RateGroup can stay unversioned unless product demands it.

---

## 4. Immutable calculation snapshot persistence

### 4.1 Purpose

- When a job/order is **completed** (or when payroll is run), persist a **one-way snapshot** of how the payout was calculated.
- This supports: historical payout auditability, stable payroll explanations, and “what did we use for this order?” even after pricing changes.

### 4.2 Recommended snapshot entity

```text
PricingCalculationSnapshot
--------------------------
Id                      Guid (PK)
CompanyId               Guid?
OrderId                 Guid (the job/order)
PricingRuleSetId        Guid? (which rule set was used; null = unversioned/live)
CalculatedAt            DateTime (UTC)

-- Matched rule identifiers (for audit)
PayoutPath              string? (CustomOverride | BaseWorkRate | Legacy)
BaseRateId              Guid? (BaseWorkRate.Id or GponSiJobRate.Id)
CustomRateId            Guid? (GponSiCustomRate.Id when path = CustomOverride)
RateGroupId             Guid?
ServiceProfileId        Guid? (if matched via profile)

-- Amounts (immutable)
BaseAmountBeforeModifiers  decimal?
FinalPayoutAmount          decimal
Currency                   string (e.g. MYR)

-- Modifiers applied (stored as JSON or separate table)
ModifierTraceJson       string? (JSON array of { ModifierType, Operation, Value, AmountBefore, AmountAfter })
-- Alternative: PricingCalculationSnapshotModifier (1:N) with columns ModifierType, Operation, Value, AmountBefore, AmountAfter, SortOrder

-- Override used (if any)
CustomOverrideAmount   decimal? (when path = CustomOverride, equals FinalPayoutAmount)

-- Full trace (optional but recommended)
ResolutionStepsJson    string? (JSON array of resolution step strings)
WarningsJson           string? (JSON array of warning strings)

-- Source (e.g. PayrollRunId) for correlation
SourcePayrollRunId     Guid? (if created during payroll run)
```

- **One snapshot per order** at the time of calculation (e.g. when payroll run creates JobEarningRecord). If the same order is recalculated (e.g. payroll rerun), either overwrite the snapshot for that order or create a new row and link to the payroll run; prefer **one current snapshot per order** plus optional history by PayrollRunId if needed.
- **PricingRuleSetId null:** When resolution ran without a rule set (current behaviour), store null; snapshot still captures all IDs and amounts for audit.

### 4.3 Relationship to JobEarningRecord

- **JobEarningRecord** today: BaseRate, FinalPay, RateSource, RateId. No modifier trace or rule set.
- **Option A (recommended):** Keep JobEarningRecord as-is. Add **PricingCalculationSnapshot** as a separate table; when creating a JobEarningRecord, also create a snapshot and optionally set `JobEarningRecord.SnapshotId` (FK) for direct link.
- **Option B:** Add a FK from JobEarningRecord to PricingCalculationSnapshot and store only minimal fields in JobEarningRecord (e.g. FinalPay, SnapshotId); detailed audit from snapshot.  
Recommend **Option A** plus an optional **SnapshotId** on JobEarningRecord so that “view payout breakdown for this job” can load the snapshot when present.

---

## 5. Rollout phases

### Phase 1: Rule set entity only

- Add **PricingRuleSet** table and entity; no FKs from existing pricing tables.
- Support CRUD and list in admin/API; **no change** to RateEngineService.
- Use for labelling and future attachment only.

**Deliverables:** Migration, entity, repository/service, minimal API (create/list/get).  
**Risk:** Low. No impact on resolution.

---

### Phase 2: Draft / Published model

- Implement **Status** workflow: Draft → Published → Archived.
- When **publishing:** set PublishedAt, PublishedByUserId; set EffectiveFrom (e.g. now or chosen date); optionally set EffectiveTo on the previous published set for that company/scope.
- Enforce **one current published set per company (and scope)** if applicable: only one row with Status = Published and EffectiveTo = null (or overlapping window).
- Resolution still **ignores** rule sets.

**Deliverables:** Status transitions, validation, effective-date handling, API.  
**Risk:** Low. Still no resolution change.

---

### Phase 3: Attach pricing records to rule set

- Add **PricingRuleSetId** (nullable) to: BaseWorkRate, RateModifier, OrderTypeSubtypeRateGroup; then GponSiCustomRate, GponSiJobRate if desired.
- **Migration:** set PricingRuleSetId = null for all existing rows (preserve current behaviour).
- **Resolution:** no change yet; all queries continue to ignore PricingRuleSetId.
- **UI/API:** when creating/editing rates or modifiers, allow optionally assigning a **Draft** rule set. Rows with a Draft set are still ignored by resolution until that set is published.

**Deliverables:** Migrations, entity changes, UI/API for “assign to draft rule set”.  
**Risk:** Low. Resolution unchanged; existing rows remain “global”.

---

### Phase 4: Publish flow and resolution by rule set

- **Publish flow:** On Publish, clone or “freeze” the draft (optional: copy rows into a snapshot table) or simply flip Status and set EffectiveFrom. Policy decision: either **immutable published rows** (no edits after publish) or **edits allowed but EffectiveFrom/To** define what resolution sees.
- **Resolution change (additive):**  
  - Add optional **PricingRuleSetId** (or “use published for company”) to **GponRateResolutionRequest**.  
  - When **not** provided: keep current behaviour (query all rows with matching CompanyId/date; PricingRuleSetId null or any).  
  - When provided: restrict queries to rows where PricingRuleSetId = that set (and Status = Published and reference date in [EffectiveFrom, EffectiveTo]).  
- **Payroll / order profitability:** continue to call resolution **without** rule set initially (live tables); once snapshot is in place (Phase 5), they can pass “published rule set for company” if desired.

**Deliverables:** Publish workflow, resolution filter by rule set when requested, API and feature flag or config.  
**Risk:** Medium. Careful testing required; legacy path (no rule set) must remain default and unchanged.

---

### Phase 5: Snapshot persistence

- Add **PricingCalculationSnapshot** table (and optional **PricingCalculationSnapshotModifier** if not using JSON).
- When **JobEarningRecord** is created (payroll run), after resolving payout: create snapshot with OrderId, CompanyId, PayoutPath, BaseRateId, CustomRateId, BaseAmountBeforeModifiers, FinalPayoutAmount, ModifierTraceJson, ResolutionStepsJson, WarningsJson, CalculatedAt, PricingRuleSetId (if resolution was run with a rule set), SourcePayrollRunId.
- Optionally set **JobEarningRecord.PricingSnapshotId** (FK) for quick lookup.
- **Installer Payout Breakdown** (and similar) can show from snapshot when available, else fall back to “re-resolve now” for historical orders.

**Deliverables:** Migration, snapshot entity, creation in payroll flow, optional link from JobEarningRecord.  
**Risk:** Low for existing behaviour; medium for payroll (ensure snapshot creation never fails payout write).

---

### Phase 6: UI support

- **Draft rule sets:** List/create/edit draft rule sets; assign rates/modifiers to a draft set.
- **Publish action:** Button to publish a draft (with effective-from date and validation).
- **Compare draft vs published:** Read-only comparison: show draft set vs current published set (e.g. side-by-side base rates, modifiers, or diff of key fields).
- **Historical calculation view:** On order/job detail or payout breakdown, when a snapshot exists: “Calculation at time of payroll” with base, modifiers, trace; otherwise “Current resolution” (re-run).

**Deliverables:** Screens and navigation as above.  
**Risk:** Low; UI only.

---

## 6. UI recommendations (summary)

| Feature | Recommendation |
|---------|----------------|
| **Draft rule sets** | List and CRUD; filter by Status; show VersionNumber, Name, EffectiveFrom/To, PublishedAt. |
| **Publish action** | Single action from draft; confirm effective-from date; validate “no overlapping published set”; show success and new published set. |
| **Compare draft vs published** | Separate view or panel: load draft set and current published set; compare BaseWorkRates (by key), RateModifiers, and optionally custom/legacy rates; highlight added/removed/changed. |
| **Historical calculation view** | In Installer Payout Breakdown (or order detail): if PricingCalculationSnapshot exists for order, show “As calculated on &lt;date&gt;” with full breakdown from snapshot; optional “Recalculate with current pricing” for comparison. |

---

## 7. Entity changes (summary)

| Entity | New/optional columns |
|--------|-----------------------|
| **PricingRuleSet** | New table (see §2). |
| **BaseWorkRate** | PricingRuleSetId (Guid?, FK, nullable). |
| **RateModifier** | PricingRuleSetId (Guid?, FK, nullable). |
| **OrderTypeSubtypeRateGroup** | PricingRuleSetId (Guid?, FK, nullable). |
| **GponSiCustomRate** | PricingRuleSetId (Guid?, FK, nullable). |
| **GponSiJobRate** | PricingRuleSetId (Guid?, FK, nullable). |
| **PricingCalculationSnapshot** | New table (see §4). |
| **JobEarningRecord** | PricingSnapshotId (Guid?, FK, optional). |

---

## 8. Migration impact

- **Phase 1:** One new table; no changes to existing tables.
- **Phase 3:** One migration adding nullable **PricingRuleSetId** to selected tables; default null; index (CompanyId, PricingRuleSetId) where useful for resolution.
- **Phase 5:** New snapshot table(s); optional column on JobEarningRecords. Backfill not required; snapshots apply going forward (optional backfill for recent payroll runs if needed).
- **Rollback:** Rule set and snapshot are additive; removing columns or tables can be done with a later migration if needed; resolution can stay on “no rule set” path.

---

## 9. Risks and mitigations

| Risk | Mitigation |
|------|------------|
| Resolution logic regressions | Keep “no rule set” path as default; add rule-set filter only when parameter present; comprehensive tests for both paths; feature flag for “use published set” in payroll. |
| Data inconsistency (draft vs published) | Validate on publish (e.g. at least one base rate; no duplicate keys); immutable published rows or strict change policy. |
| Snapshot storage growth | Snapshot per order per payroll run (or one per order); prune or archive old snapshots by policy; use JSON for trace to avoid huge modifier tables. |
| Legacy and unversioned rows | Rows with PricingRuleSetId = null remain in use when resolution is called without a rule set; document and test “mixed” behaviour if some entities versioned and others not. |
| Effective date overlaps | Validation on publish: only one published set per company/scope with overlapping EffectiveFrom/EffectiveTo; close previous set’s EffectiveTo when publishing new. |

---

## 10. Success criteria (mapped to design)

- **Versioned pricing:** PricingRuleSet with VersionNumber, Status, EffectiveFrom/To.  
- **Effective dating:** At rule-set level (EffectiveFrom/To) and retained on entities (BaseWorkRate, GponSiCustomRate, GponSiJobRate).  
- **Immutable payout audit trail:** PricingCalculationSnapshot stores full calculation and optional link from JobEarningRecord.  
- **Safer future pricing changes:** Draft rule sets and publish flow; resolution uses published set only when requested; existing behaviour unchanged when not using rule sets.

This gives CephasOps a clear, additive path to versioned pricing, effective dating, and an immutable payout audit trail without rewriting current pricing logic or breaking existing payout resolution.

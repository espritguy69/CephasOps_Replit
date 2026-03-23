# GPON Pricing Debugging Feature — Deliverable

## Executive summary

A **read-only debugging enhancement** was implemented for the Rate Designer. The existing `/rates/resolve` response was extended with optional debug fields (`PayoutPath`, `BaseAmountBeforeModifiers`, `Warnings`), and the Rate Designer UI now has a dedicated **Debug Trace** panel that shows request context, matched rule, payout path, base amount before modifiers, warnings (including “Used legacy fallback”), and the full resolution steps. **No payout calculation behaviour, rule precedence, or resolution logic was changed**; only additive, observational data was added for support and admins.

---

## 1. Current trace capability (audit)

### What `/rates/resolve` already returned (before this change)

- **resolutionSteps**: List of human-readable strings describing the resolution flow, e.g.:
  - “Starting revenue rate resolution for OrderType=…, OrderCategory=…”
  - “Revenue rate found: X MYR from GponPartnerJobRate (ID: …)” or “No revenue rate found”
  - “Starting payout rate resolution for SI=…, Level=…”
  - “Custom payout rate found: X MYR from GponSiCustomRate (ID: …)” when custom override wins
  - “No custom rate found for SI, checking default payout rates”
  - “Base work rate found: X MYR from BaseWorkRate (ID: …)” when base work rate wins
  - “Default payout rate found: X MYR from GponSiJobRate (ID: …)” when legacy wins
  - “SI Level not provided…” / “No default payout rate found…” when no payout
  - Modifier steps: “RateModifier (InstallationMethod): Add 15 → 115 MYR” etc., or “No rate modifiers matched.”
- **Payout/revenue**: `PayoutSource`, `PayoutRateId`, `RevenueSource`, `RevenueRateId`, `PayoutAmount`, `RevenueAmount`, `GrossMargin`, `MarginPercentage`, `Currency`, `ResolvedAt`, `Success`, `ErrorMessage`.

### What was missing for support-grade debugging

- **Explicit path label**: No single field stating “CustomOverride” vs “BaseWorkRate” vs “Legacy” (only implied by step text).
- **Base amount before modifiers**: Not exposed; only final payout was visible.
- **Structured warnings**: No “Used legacy fallback” or “No payout found” flags for the UI to highlight.
- **Request context echo**: Resolution request was not echoed in the response for traceability.
- **Cache hit/miss**: Base work rate resolution uses an in-memory cache; no step or flag indicated cache use (unchanged in this phase).
- **Exact category vs service profile vs broad**: Base work rate resolution uses that hierarchy internally but did not expose which tier matched (left as future enhancement to avoid touching `ResolveBaseWorkRateFromDbAsync`).

---

## 2. Debug / trace model (design)

Desired future trace capabilities (from your spec) and how they were addressed:

| Desired capability | Status in this phase |
|--------------------|----------------------|
| Evaluated rule list | Partially: `resolutionSteps` lists what was tried; no separate structured “evaluated rules” array. |
| Matched rule IDs | Already present: `PayoutRateId`, `RevenueRateId`. Now also surfaced in Debug Trace UI. |
| Skipped rule reasons | In step text (e.g. “No custom rate found…”, “No default payout rate…”). No separate “skipped reasons” array. |
| Exact category vs service profile vs broad vs legacy path | **Path**: Added `PayoutPath` = CustomOverride | BaseWorkRate | Legacy. Finer BaseWorkRate tier (exact vs profile vs broad) not exposed to avoid changing resolution method. |
| Modifier execution order | Already in `resolutionSteps` (InstallationMethod → SITier → Partner). Shown in Debug Trace. |
| Override precedence explanation | Step text + `PayoutPath = CustomOverride` when custom wins. |
| Cache hit/miss | Not added; would require touching cache layer. Documented as follow-up. |
| Company scoping visibility | Not added; would require passing through resolution. Documented as follow-up. |
| Effective date used | Reference date is request input; not echoed in result. Can be added later. |
| Partner / SI tier context used | Now echoed in Debug Trace via **request context** snapshot from the UI. |
| Warn when legacy fallback used | **Done**: `Warnings` list includes “Used legacy fallback (GponSiJobRate). Consider configuring Base Work Rate for this context.” |

---

## 3. Chosen implementation path

**Option B: Add a read-only trace DTO/endpoint enhancement to `/rates/resolve`.**

- No new endpoint; the existing `POST /rates/resolve` response was extended with optional debug fields.
- No change to resolution logic, precedence, or payout math; the engine only sets extra properties on the result after the fact.
- UI consumes these fields and shows a dedicated Debug Trace panel.

Options A (UI-only parsing of steps) and C (separate debug endpoint) were not chosen: A would not support structured warnings or path; C would duplicate logic or add maintenance cost.

---

## 4. Implementation summary

### Backend (additive only)

- **GponRateResolutionResult** (RateResolutionResult.cs):
  - `PayoutPath` (string?): `"CustomOverride"` | `"BaseWorkRate"` | `"Legacy"`.
  - `BaseAmountBeforeModifiers` (decimal?): Base payout before rate modifiers; null when path is CustomOverride or no payout.
  - `Warnings` (List<string>): Support messages, e.g. legacy fallback, no SI level, no payout found.
- **RateEngineService** (ResolveGponRatesAsync):
  - When custom rate wins: set `PayoutPath = "CustomOverride"`.
  - When base work rate wins: set `PayoutPath = "BaseWorkRate"`, `BaseAmountBeforeModifiers = baseWorkRate.Amount`.
  - When legacy wins: set `PayoutPath = "Legacy"`, `BaseAmountBeforeModifiers = payoutRate.PayoutAmount`, and add warning “Used legacy fallback (GponSiJobRate). Consider configuring Base Work Rate for this context.”
  - When no payout: add warnings for “SI Level not provided…” or “No payout rate found…”.

No changes to `ResolveBaseWorkRateFromDbAsync`, cache, or modifier application logic.

### Frontend

- **types/rates.ts**: Extended `GponRateResolutionResult` with `payoutPath?`, `baseAmountBeforeModifiers?`, `warnings?`.
- **components/rate-designer/DebugTracePanel.tsx**: New panel showing:
  - Request context (order type, category, installation method, SI level, partner group, service installer).
  - Matched rule (source + rate ID).
  - Payout path (with Legacy highlighted via warning-style badge).
  - Base amount before modifiers vs final payout when they differ.
  - Warnings list (e.g. legacy fallback).
  - Full resolution steps.
- **RateDesignerPage**: Stores `lastRequestContext` when running the calculator and passes it with `calcResult` to `DebugTracePanel`. Replaced the previous Resolution trace card with the new Debug Trace panel.

---

## 5. Files changed

| Area | File | Change |
|------|------|--------|
| Backend DTO | `backend/src/CephasOps.Application/Rates/DTOs/RateResolutionResult.cs` | Added `PayoutPath`, `BaseAmountBeforeModifiers`, `Warnings`. |
| Backend service | `backend/src/CephasOps.Application/Rates/Services/RateEngineService.cs` | Set the three debug fields and append to `Warnings` in existing branches; no logic or precedence change. |
| Frontend types | `frontend/src/types/rates.ts` | Added `payoutPath`, `baseAmountBeforeModifiers`, `warnings` to `GponRateResolutionResult`. |
| Frontend component | `frontend/src/components/rate-designer/DebugTracePanel.tsx` | New Debug Trace panel. |
| Frontend component | `frontend/src/components/rate-designer/index.ts` | Export `DebugTracePanel`. |
| Frontend page | `frontend/src/pages/settings/RateDesignerPage.tsx` | `lastRequestContext` state, pass to `DebugTracePanel`, use `DebugTracePanel` instead of `ResolutionTracePanel`. |

---

## 6. New debug fields

| Field | Type | Meaning |
|-------|------|--------|
| `PayoutPath` | string? | `"CustomOverride"` \| `"BaseWorkRate"` \| `"Legacy"`. |
| `BaseAmountBeforeModifiers` | decimal? | Payout base amount before InstallationMethod/SITier/Partner modifiers; null for CustomOverride or when no payout. |
| `Warnings` | List<string> | E.g. “Used legacy fallback…”, “SI Level not provided…”, “No payout rate found…”. |

---

## 7. Confirmation: pricing logic unchanged

- **Payout calculation**: Unchanged; same formulas and same use of custom rate, base work rate, legacy rate, and modifiers.
- **Rule precedence**: Unchanged; order remains 1) GponSiCustomRate, 2) BaseWorkRate (exact → profile → broad), 3) GponSiJobRate, then modifiers.
- **RateEngineService**: No changes to `ResolveBaseWorkRateFromDbAsync`, `ResolveBaseWorkRateAsync`, `ApplyRateModifiersAsync`, or any resolution logic; only assignment to the new result properties after existing logic.
- **Cache**: Unchanged; no cache hit/miss exposure and no cache behaviour change.
- **Schema**: No database or API route changes; only DTO properties added.

---

## 8. Follow-up todo items

- **Show evaluated rule list**: Optional structured list of “rules evaluated” (e.g. custom, base exact, base profile, base broad, legacy) with matched/skipped and reason. Would require minimal additions in the engine to record which branch was taken.
- **Show exact category vs service profile vs broad**: Have `ResolveBaseWorkRateFromDbAsync` return which tier matched (e.g. enum or string) and expose it on the result (e.g. `BaseWorkRateTier`) for the UI.
- **Show cache hit/miss**: If desired, add a step or flag when base work rate is served from cache vs database (read-only observation).
- **Company scoping visibility**: Echo `CompanyId` used in resolution (from request or current user) in the result or in a debug block.
- **Effective date used**: Echo `ReferenceDate` (or effective date) in the response for traceability.
- **Structured modifier steps**: Optional list of `{ Type, AdjustmentType, Value, AmountAfter }` in addition to the existing step strings.

These can be implemented in a later phase without changing payout behaviour.

---

## Success criteria

- Support/admin users can better see **why** a payout result happened (path, matched rule, base vs final amount, warnings, steps).  
- Pricing logic is **unchanged**.  
- Debugging is **additive and read-only**.

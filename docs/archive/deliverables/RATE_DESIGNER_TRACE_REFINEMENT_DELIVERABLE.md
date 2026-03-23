# Rate Designer Trace Refinement — Deliverable

## Executive summary

Trace output for the Rate Designer debug feature was refined so support and admins can see **why** a payout happened without reading backend code. The existing `GponRateResolutionResult` was extended with **structured debug fields** (ResolutionMatchLevel, ResolutionContext, MatchedRuleDetails, ModifierTrace), **resolution step wording** was clarified, and the **Debug Trace UI** was reorganized into labeled sections with resolution-level badges and a modifier execution table. **Payout calculations, rule precedence, and modifier application order were not changed**; only observability and presentation were improved.

---

## New trace fields added

| Field | Type | Description |
|-------|------|-------------|
| **ResolutionMatchLevel** | string? | `Custom` \| `ExactCategory` \| `ServiceProfile` \| `BroadRateGroup` \| `Legacy`. Indicates which tier matched for payout. |
| **ResolutionContext** | ResolutionContextDto? | Echo of request + effective date: CompanyId, EffectiveDateUsed, OrderTypeId, OrderSubtypeId, OrderCategoryId, InstallationMethodId, SiTier, PartnerGroupId. |
| **MatchedRuleDetails** | MatchedRuleDetailsDto? | IDs of matched rate(s): RateGroupId, BaseWorkRateId, LegacyRateId, CustomRateId, ServiceProfileId (only relevant ones set per path). |
| **ModifierTrace** | List&lt;ModifierTraceItemDto&gt; | Ordered list of modifier applications. Each item: ModifierType, Operation (Add/Multiply), Value, AmountBefore, AmountAfter. |

Existing fields **PayoutPath**, **BaseAmountBeforeModifiers**, **Warnings**, and **ResolutionSteps** are unchanged in meaning; step **wording** was improved (see below).

---

## Files changed

| Area | File | Change |
|------|------|--------|
| Backend DTOs | `backend/src/CephasOps.Application/Rates/DTOs/RateResolutionResult.cs` | Added ResolutionContextDto, MatchedRuleDetailsDto, ModifierTraceItemDto; added ResolutionMatchLevel, ResolutionContext, MatchedRuleDetails, ModifierTrace to GponRateResolutionResult. |
| Backend service | `backend/src/CephasOps.Application/Rates/Services/RateEngineService.cs` | Build ResolutionContext at start; set ResolutionMatchLevel and MatchedRuleDetails for Custom / BaseWorkRate (ExactCategory/ServiceProfile/BroadRateGroup) / Legacy; ApplyRateModifiersAsync now returns (AdjustedAmount, Steps, ModifierTrace) and populates ModifierTraceItemDto; improved ResolutionSteps wording; add "Final payout: X MYR" step. |
| Backend tests | `backend/tests/.../Rates/RateEngineServiceTests.cs`, `RateEngineServiceRateModifierTests.cs` | Updated assertions to match new step wording (Revenue matched, no matching partner rate, Custom override / Fallback Legacy, SI Level / legacy skipped, Installation Method / SI Tier modifier). |
| Frontend types | `frontend/src/types/rates.ts` | Added ResolutionContextDto, MatchedRuleDetailsDto, ModifierTraceItemDto; added resolutionMatchLevel, resolutionContext, matchedRuleDetails, modifierTrace to GponRateResolutionResult. |
| Frontend UI | `frontend/src/components/rate-designer/DebugTracePanel.tsx` | Reorganized into sections: Request context (API or UI), Matched rule (+ IDs), Resolution path (level badge + path summary), Modifier execution (table), Warnings, Final result, Resolution steps. Added resolution-level badges (Exact category, Service profile, Broad rate, Legacy fallback with warning color). |

---

## Example trace output

**Scenario: Base Work Rate (Exact Category) with two modifiers**

```json
{
  "success": true,
  "payoutAmount": 121,
  "payoutPath": "BaseWorkRate",
  "resolutionMatchLevel": "ExactCategory",
  "baseAmountBeforeModifiers": 100,
  "resolutionContext": {
    "companyId": "...",
    "effectiveDateUsed": "2026-03-08T12:00:00Z",
    "orderTypeId": "...",
    "orderSubtypeId": "...",
    "orderCategoryId": "...",
    "installationMethodId": "...",
    "siTier": "Senior",
    "partnerGroupId": "..."
  },
  "matchedRuleDetails": {
    "rateGroupId": "...",
    "baseWorkRateId": "...",
    "serviceProfileId": null
  },
  "modifierTrace": [
    { "modifierType": "InstallationMethod", "operation": "Add", "value": 10, "amountBefore": 100, "amountAfter": 110 },
    { "modifierType": "SITier", "operation": "Multiply", "value": 1.1, "amountBefore": 110, "amountAfter": 121 }
  ],
  "resolutionSteps": [
    "Revenue lookup: OrderType=..., OrderCategory=...",
    "Revenue: no matching partner rate found",
    "Payout resolution: SI=, Level=Senior",
    "Skipped Custom SI check (no installer selected)",
    "Matched Base Work Rate (ExactCategory): 100 MYR",
    "Applied Installation Method modifier: Add 10 → 110 MYR",
    "Applied SI Tier modifier: Multiply 1.1 → 121 MYR",
    "Final payout: 121 MYR"
  ]
}
```

**Scenario: Legacy fallback**

- `resolutionMatchLevel`: `"Legacy"`
- `payoutPath`: `"Legacy"`
- `matchedRuleDetails.legacyRateId`: set
- `warnings`: includes "Used legacy fallback (GponSiJobRate). Consider configuring Base Work Rate for this context."
- Steps include: "Fallback: matched Legacy SI rate (GponSiJobRate): 60 MYR"

---

## Resolution step wording improvements

| Previous (vague) | New (clear) |
|------------------|-------------|
| "Starting revenue rate resolution for..." | "Revenue lookup: OrderType=..., OrderCategory=..." |
| "Revenue rate found: X MYR from GponPartnerJobRate (ID: ...)" | "Revenue matched: X MYR (GponPartnerJobRate)" |
| "No revenue rate found in GponPartnerJobRates" | "Revenue: no matching partner rate found" |
| "Custom payout rate found: X MYR from GponSiCustomRate (ID: ...)" | "Checked Custom SI rate → matched" + "Custom override payout: X MYR (no modifiers applied)" + "Final payout: X MYR" |
| "No custom rate found for SI, checking default payout rates" | "Checked Custom SI rate → none found" |
| (no step when no SI) | "Skipped Custom SI check (no installer selected)" |
| "Base work rate found: X MYR from BaseWorkRate (ID: ...)" | "Matched Base Work Rate (ExactCategory|ServiceProfile|BroadRateGroup): X MYR" |
| "Default payout rate found: X MYR from GponSiJobRate (ID: ...)" | "Fallback: matched Legacy SI rate (GponSiJobRate): X MYR" |
| "RateModifier (InstallationMethod): Add 15 → 115 MYR" | "Applied Installation Method modifier: Add 15 → 115 MYR" |
| "No rate modifiers matched." | "No rate modifiers configured or matched." / "No rate modifiers matched for this context." |
| (no final step) | "Final payout: X MYR" |

---

## UI improvements

- **Request context**: Uses `result.resolutionContext` from API when present (includes effective date, company, order subtype); falls back to UI `requestContext` for installer ID.
- **Matched rule**: Shows payout source, rate ID badge, and when available a line of matched IDs (rate group, base work rate, service profile, legacy, custom).
- **Resolution path**: Single badge for **ResolutionMatchLevel** (Exact category / Service profile / Broad rate / **Legacy fallback** in warning color) plus short path summary, e.g. "BaseWorkRate (ExactCategory) → Modifiers" or "CustomOverride (no modifiers)". Base amount before modifiers vs final amount shown when they differ.
- **Modifier execution**: Table with columns Type, Operation, Value, Before → After for each `ModifierTrace` item.
- **Warnings**: Unchanged; still in an amber-highlighted block.
- **Final result**: Dedicated block showing "Payout: MYR X.XX".
- **Resolution steps**: Full step log kept at the bottom for copy/paste and audit.

---

## Confirmation: pricing logic unchanged

- **Payout calculation**: Unchanged. Same formulas for custom rate, base work rate, legacy rate, and modifier application (Add/Multiply).
- **Rule precedence**: Unchanged. Order remains 1) GponSiCustomRate, 2) BaseWorkRate (exact category → service profile → broad), 3) GponSiJobRate, then modifiers (InstallationMethod → SITier → Partner).
- **Modifier application order**: Unchanged. No changes to `ApplyOneModifier` or the order in which modifiers are applied; only the return value was extended with `ModifierTrace`.
- **ResolveBaseWorkRateFromDbAsync**: Unchanged. ResolutionMatchLevel for base work rate is derived from the returned entity (OrderCategoryId / ServiceProfileId / else broad) in the caller; no change to which rate is chosen.

---

## Success criteria

- Admins can see **why** a payout happened (path and match level).
- **Which rule matched** is clear (matched rule section + level badge).
- **What modifiers were applied** is visible in both the modifier table and the step log.
- **Why fallback occurred** is clear (Legacy badge + warning text).
- **Pricing logic remains unchanged** (calculations, precedence, modifier order unchanged).

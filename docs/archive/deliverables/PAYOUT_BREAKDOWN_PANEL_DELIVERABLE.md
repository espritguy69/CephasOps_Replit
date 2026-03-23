# Installer Payout Breakdown — Deliverable

## Executive summary

A **Payout Breakdown** panel was added to the Rate Designer so installers, finance, and support can see how a payout was calculated at a glance. The panel uses **existing debug data only** (baseAmountBeforeModifiers, modifierTrace, payoutAmount, resolutionMatchLevel, payoutPath). No backend or pricing logic was changed; this is a UI-only enhancement.

---

## Files created

| File | Purpose |
|------|---------|
| `frontend/src/components/rate-designer/PayoutBreakdownPanel.tsx` | New component that renders base rate, modifiers (with Add = green, Multiply = blue), and final payout in a Card. Handles Custom override (single line + final) and Base/Legacy + modifiers flows. |

---

## Files modified

| File | Change |
|------|--------|
| `frontend/src/components/rate-designer/index.ts` | Export `PayoutBreakdownPanel`. |
| `frontend/src/pages/settings/RateDesignerPage.tsx` | Import `PayoutBreakdownPanel` and render it between `RateCalculatorPanel` and `DebugTracePanel` in the right-hand column. |

---

## Data source

The panel reads only from the existing resolve result passed as `result: GponRateResolutionResult | null`:

- `result.baseAmountBeforeModifiers` — base amount before modifiers
- `result.modifierTrace` — list of `{ modifierType, operation, value, amountBefore, amountAfter }`
- `result.payoutAmount` — final payout
- `result.resolutionMatchLevel` — for base label (ExactCategory, ServiceProfile, BroadRateGroup, Legacy)
- `result.payoutPath` — CustomOverride vs Base/Legacy
- `result.currency` — e.g. MYR

No new API calls or backend changes.

---

## Example breakdown output

**Base Work Rate (Exact Category) + two modifiers**

```
Base Work Rate (Exact Category)      MYR 100.00

Installation Method modifier +MYR10.00    100.00 → 110.00
SI Tier modifier ×1.1                    110.00 → 121.00

--------------------------------------
Final payout                            MYR 121.00
```

**Custom override (no modifiers)**

```
Custom override                        MYR 80.00

--------------------------------------
Final payout                            MYR 80.00
```

**Legacy + no modifiers**

```
Legacy SI rate                         MYR 60.00

--------------------------------------
Final payout                            MYR 60.00
```

---

## UI design

- **Card** (shadcn): title “Payout breakdown”, subtitle describing that it shows how the payout was calculated.
- **Base rate**: Neutral text; label from resolution (Base Work Rate (Exact Category / Service Profile / Broad), Legacy SI rate, or Custom override).
- **Modifiers**: Each row shows modifier name and operation (e.g. “Installation Method modifier +MYR10.00” or “SI Tier modifier ×1.1”). **Add** modifiers use green (`text-emerald-600`), **Multiply** use blue (`text-blue-600`). Optional before → after amounts in muted text.
- **Final payout**: Bold, with a divider above.
- Empty state: “Run the calculator to see the breakdown.” when there is no result or no success.

---

## Integration

Flow on Rate Designer right column:

1. **Context** (left column)
2. **Calculate payout** — RateCalculatorPanel
3. **Breakdown** — PayoutBreakdownPanel (new)
4. **Debug trace** — DebugTracePanel

---

## Confirmation: no pricing logic changed

- **RateEngineService**: Not modified.
- **Payout calculations**: Unchanged; the panel only displays values already returned by `/rates/resolve`.
- **Backend**: No new endpoints or DTO changes; uses existing `GponRateResolutionResult` debug fields.

---

## Success criteria

- Installers/admins can see **why payout = RM121** (base + modifiers or custom).
- **Which modifiers applied** is clear (Installation Method, SI Tier, Partner with + or × and value).
- **What the base rate was** is shown with a clear label (Exact Category, Service Profile, Broad, Legacy, or Custom).
- All of this is visible without reading the resolution trace.

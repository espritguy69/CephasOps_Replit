# Rate Simulation Sandbox — Deliverable

## Executive summary

A **read-only simulation** feature was added to the Rate Designer so admins and support/finance can test hypothetical pricing (e.g. modifier +15 instead of +10, base 100→110) and compare **current payout** vs **simulated payout** and **difference**, without changing live pricing or saving any simulated values to production.

- **Implementation:** Frontend-only draft simulation. No new backend endpoints; no changes to `RateEngineService` or live payout logic.
- **Safety:** No production pricing data is altered. Simulated values exist only in component state and are cleared when the user runs a new calculation.

---

## Safest implementation path (chosen)

- **Option A (chosen):** Frontend-only draft layer  
  - Reuse existing resolve result (current payout, base amount, modifier trace).  
  - User enters draft overrides (draft base, per-modifier overrides, or a single draft payout for custom path).  
  - Simulated payout is computed in the browser using the same Add/Multiply order as the engine.  
  - No backend changes; no risk to live resolution or DB.

- **Option B (not implemented):** Dedicated read-only simulation endpoint  
  - Would allow simulating “what if service profile path was used” by re-running resolution with different inputs.  
  - Deferred to avoid touching `RateEngineService` and to keep the first release minimal.

- **Option C (not used):** Reusing resolve endpoint with “draft” query flag  
  - Would require backend support and careful isolation of draft vs live; skipped in favour of frontend-only.

---

## Files created

| File | Purpose |
|------|--------|
| `frontend/src/components/rate-designer/SimulationSandboxPanel.tsx` | Simulation UI: current payout, draft adjustments, simulated payout, difference, simulation trace. State: `draftBaseAmount`, `modifierOverrides`, `draftPayoutOverride`. Resets when `result` changes. |

---

## Files updated

| File | Change |
|------|--------|
| `frontend/src/components/rate-designer/index.ts` | Export `SimulationSandboxPanel`. |
| `frontend/src/pages/settings/RateDesignerPage.tsx` | Import and render `SimulationSandboxPanel` below `PayoutBreakdownPanel`, passing `result={calcResult}`. |

---

## Behaviour

1. **Current payout**  
   From existing resolve result (`result.payoutAmount`). Unchanged; no new API.

2. **Draft adjustments**  
   - **Custom path:** single “Draft payout” override.  
   - **Base/Legacy path:** “Draft base amount” + one input per modifier in `result.modifierTrace` (override that modifier’s value).  
   Placeholders show current/base/modifier values.

3. **Simulated payout**  
   Computed in the frontend: start from draft base (or `result.baseAmountBeforeModifiers`), then apply each modifier in order (Add: +value, Multiply: ×value), using overrides when provided.

4. **Difference**  
   `simulated − current`; styled (e.g. green/red) for quick reading.

5. **Simulation trace**  
   Lists which overrides were applied (e.g. “Base amount override applied”, “Installation Method modifier override applied”).

6. **Reset**  
   When `result` changes (e.g. after a new “Calculate”), draft overrides are cleared so the simulation is not applied to a stale breakdown.

---

## Confirmation: live pricing unchanged

- **RateEngineService:** Not modified.  
- **Resolve/pricing endpoints:** Not modified.  
- **Production tables:** No simulated values are written.  
- **Payout behaviour:** Unchanged; simulation is display-only and uses the same formula (base + Add/Multiply order) in the client for comparison only.

---

## Limitations (by design)

- **Service profile vs exact category:** The sandbox does **not** simulate “what if service profile fallback applied instead of exact category”; that would require re-running resolution on the backend (possible future enhancement).  
- **Scope (exact/shared/broad):** Not simulated in this release; could be added later via a dedicated read-only simulation API if needed.

---

## Success criteria (met)

- Admins can test pricing changes safely (draft overrides only).  
- No production pricing data is altered.  
- Current vs simulated payout and delta are clearly visible.  
- Support/finance can use the sandbox before approving rate changes.  
- Live pricing logic is unchanged.

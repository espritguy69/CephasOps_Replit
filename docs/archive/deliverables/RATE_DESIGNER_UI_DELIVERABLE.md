# Unified GPON Rate Designer UI — Deliverable

## Executive summary

A single admin-facing **Rate Designer** screen is available at **Settings → GPON → Rate Designer** (`/settings/gpon/rate-designer`). It gives a job-scenario view of pricing: admins pick a context (order type, subtype, category, installation method, SI tier, partner, installer override), see matching base work rates and installer overrides, run a payout calculation against the existing resolution API, and read a resolution trace. No pricing engine or resolution behaviour was changed; the page only composes existing APIs and UI.

---

## Files changed

| Area | File |
|------|------|
| **Types** | `frontend/src/types/rates.ts` — added `GponRateResolutionRequest`, `GponRateResolutionResult` (with `resolutionSteps`, `success`, `errorMessage`, etc.) |
| **API** | `frontend/src/api/rates.ts` — added `resolveGponRates()` calling POST `/rates/resolve` and returning full result with steps |
| **Components** | `frontend/src/components/rate-designer/types.ts` — `RateDesignerContext`, `emptyRateDesignerContext` |
| **Components** | `frontend/src/components/rate-designer/RateContextPanel.tsx` — context selection (order type, subtype, category, installation method, SI tier, partner, installer; derived rate group and service profile) |
| **Components** | `frontend/src/components/rate-designer/BaseRatePanel.tsx` — table of matching base work rates with Exact category / Shared profile / Broad badges; link to Rate Groups |
| **Components** | `frontend/src/components/rate-designer/ModifierPanel.tsx` — informational text (modifiers applied in engine; see trace) |
| **Components** | `frontend/src/components/rate-designer/OverridePanel.tsx` — table of matching SI custom rates; link to Rate Engine |
| **Components** | `frontend/src/components/rate-designer/RateCalculatorPanel.tsx` — Calculate button, result (revenue, payout, source, margin), payout source label (Custom SI / Base work rate / Legacy) |
| **Components** | `frontend/src/components/rate-designer/ResolutionTracePanel.tsx` — list of `resolutionSteps` from last calculation |
| **Components** | `frontend/src/components/rate-designer/index.ts` — exports |
| **Page** | `frontend/src/pages/settings/RateDesignerPage.tsx` — layout, data loading, context state, derive rate group/service profile from mappings |
| **Routing** | `frontend/src/App.tsx` — import `RateDesignerPage`, route `/settings/gpon/rate-designer` |
| **Sidebar** | `frontend/src/components/layout/Sidebar.tsx` — GPON section: "Rate Designer" with Palette icon |

---

## Components added

- **RateContextPanel** — Pricing context (order type, subtype, category, installation method, SI tier, partner group, service installer; derived rate group and service profile).
- **BaseRatePanel** — Base work rates for the selected rate group; badges: Exact category / Shared profile / Broad; link to manage base work rates.
- **ModifierPanel** — Informational only: modifiers applied in engine order; see Resolution trace.
- **OverridePanel** — Installer overrides (SI custom rates) matching context; link to Rate Engine.
- **RateCalculatorPanel** — Run payout (and revenue) calculation; show base amount, payout source, final payout, margin; label source (Custom SI override / Base work rate / Legacy SI rate).
- **ResolutionTracePanel** — Shows `resolutionSteps` from the last calculator result.

No separate toolbar component; the page uses the standard PageShell title and breadcrumbs.

---

## APIs consumed

- **Reference data:** `getOrderTypeParents`, `getOrderTypeSubtypes`, `getOrderCategories`, `getInstallationMethods`, `getPartnerGroups`, `getServiceInstallers`.
- **Mappings:** `getRateGroupMappings`, `getServiceProfileMappings` (to derive rate group from order type/subtype and service profile from category).
- **Rates:** `getBaseWorkRates` (filter: `rateGroupId`, `isActive`), `getGponSiCustomRates` (filter: `orderTypeId`, `orderCategoryId`, optional `serviceInstallerId`), `resolveGponRates` (POST `/rates/resolve` with full request, returns `GponRateResolutionResult` including `resolutionSteps`).

No new backend or read-only endpoints were added.

---

## New read-only endpoints

None. All data comes from existing endpoints.

---

## What is fully functional vs informational

- **Fully functional:** Context selection; derived rate group and service profile; list of base work rates for the rate group (with scope badges); list of matching installer overrides; payout calculator (calls existing resolve API); resolution trace (from API `resolutionSteps`); links to Rate Groups and Rate Engine for maintenance.
- **Informational only:** Modifier panel (no modifier list API; engine applies modifiers and reports steps in the trace). Which modifiers exist and their values are not shown in a table; only the trace after calculation shows which modifier steps ran.

---

## Data / trace gaps

- **Modifiers:** There is no API that returns the list of rate modifiers (e.g. by company/type). The resolution trace includes lines like `RateModifier (InstallationMethod): Add 15 → 115 MYR` when a modifier is applied, so the trace is the source of truth for “what applied” in a given run. A future enhancement could add a read-only GET for modifiers if needed.
- **Revenue source:** The result and trace include revenue resolution; the UI shows revenue amount and payout source. Revenue source is in the API result; the calculator panel could be extended to show it explicitly if desired.

---

## Manual verification summary

- **Page loads:** Navigate to Settings → GPON → Rate Designer; page loads with context panel, base rates, modifiers, overrides, calculator, and trace sections.
- **Context drives sections:** Selecting order type (and optionally subtype) derives rate group; selecting order category derives service profile; base work rates load for the derived rate group; installer overrides filter by order type, category, and optional installer.
- **No pricing behaviour change:** Only existing APIs are called; no backend logic or resolution order was modified.
- **Calculator:** With order type and category selected, “Calculate payout” calls `/rates/resolve`; result shows payout (and revenue when present); resolution trace shows steps (custom/base/legacy and modifier steps).
- **Existing pages:** Rate Groups, Rate Engine, Service Profiles, Service Profile Mappings and other GPON settings remain in the sidebar and work as before.
- **Routes / menu:** Route `/settings/gpon/rate-designer` and sidebar entry “Rate Designer” under GPON Master Data are present and correct.

---

## Success criteria

- CephasOps has a unified Rate Designer screen at `/settings/gpon/rate-designer`.
- Admins can understand pricing from one place (context → base rates → overrides → calculator → trace).
- Current engine behaviour is unchanged (no backend changes).
- Existing pricing pages are still available for advanced maintenance.
- The screen is ready for future debugging or modifier-list enhancements.

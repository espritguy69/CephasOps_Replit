# EF Baseline Cutover — Decision Matrix

**Purpose:** Decide when CephasOps should (1) continue the current governed model, (2) do a dedicated idempotent sync, (3) adopt InitialBaseline for new environments, or (4) do a full re-baseline of migration history.

**Use with:** `docs/operations/EF_INITIALBASELINE_STRATEGY.md`, `docs/operations/EF_REBASELINE_PLAN.md`, `docs/operations/EF_MIGRATION_GRAPH_INTEGRITY_DECISION.md`.

---

## 1. Options

| Option | Description | Outcome |
|--------|-------------|---------|
| **A. Continue current governed model** | Keep 142 migrations (95 discoverable, 47 script-only). All new migrations via create-migration.ps1 with Designer. Validator and classifier enforce counts and classification. | No change to chain or baseline. Default. |
| **B. Dedicated idempotent sync** | One-off migration or script that brings DB and/or snapshot in line with current model using IF NOT EXISTS / ADD COLUMN IF NOT EXISTS. Applied to existing DBs; history updated. | Reduces snapshot drift; does not introduce a second “baseline” track. |
| **C. InitialBaseline for new environments** | Create a single baseline migration + snapshot for **new** DBs only. Legacy chain (142) remains for existing DBs. Two-track strategy. | New envs get one script + one history row; existing envs unchanged. |
| **D. Full re-baseline of migration history** | Replace or archive the 142 migrations and start history from one “Initial” or “Baseline” migration for **all** environments. High risk; requires migration path for every existing DB. | Not recommended unless all DBs can be re-provisioned or have a documented one-way migration path. |

---

## 2. Signals (practical inputs)

| Signal | Low / OK | Medium | High / Critical |
|--------|----------|--------|------------------|
| **Number of migrations** | &lt; 150 total | 150–200 | &gt; 200 or growing fast |
| **Snapshot drift severity** | Snapshot close to model; small new diffs | New migrations often &gt; 200 lines | New migrations routinely &gt; 500 lines; PendingModelChanges noisy |
| **Number of script-only** | Stable 47; all documented | New script-only added occasionally | Many undocumented or count drift frequent |
| **Difficulty onboarding new envs** | Run idempotent script + history insert; acceptable | Slow or error-prone; many manual steps | Blocking or very high effort |
| **CI friction** | Validator/classifier pass; occasional warnings | Frequent count updates or classification fixes | CI or reviewer burnout |
| **Review complexity** | PR checklist manageable | Large migrations or ordering questions often | Every migration PR is a governance event |
| **Large migrations appearing** | Rare; &lt; 500 lines | Sometimes 300–500 lines | Often &gt; 500 lines; snapshot drift suspected |

---

## 3. Decision matrix

| If… | Then prefer… | Next step |
|-----|--------------|-----------|
| All signals Low/OK | **A. Continue governed model** | No change. Keep using create-migration.ps1, validator, classifier, runbook. |
| Snapshot drift Medium/High but new env onboarding OK | **B. Dedicated idempotent sync** | Design one-off idempotent sync (IF NOT EXISTS, etc.); apply to dev/staging; update snapshot or history as agreed. Do not add a second track. |
| New env onboarding High effort or Critical; snapshot drift any | **C. InitialBaseline for new envs** | Meet prerequisites in EF_INITIALBASELINE_STRATEGY.md; create baseline migration + snapshot; document two-track (legacy vs baseline). |
| All envs can be re-provisioned or have one-way migration path | **D. Full re-baseline** | Only with explicit decision and backup. See EF_REBASELINE_PLAN.md. Rare. |
| Multiple signals High (e.g. drift + onboarding + CI friction) | **C or B then C** | Reduce pain with (B) idempotent sync first if needed; then (C) InitialBaseline for new envs. |
| Unsure | **A** | Stay on current governed model until signals or team decision force a change. |

---

## 4. CephasOps current state (reference)

| Signal | Current state |
|--------|----------------|
| Total migrations | 142 (95 + 47) |
| Snapshot drift | Documented (snapshot behind model); PendingModelChanges suppressed |
| Script-only | 47; all in manifest and governed |
| Onboarding new envs | Idempotent script(s) + history; runbook and AGENTS.md describe |
| CI | Validator + classifier; migration-hygiene.yml |
| Large migrations | Some historical &gt; 500 lines; governance flags new large migrations |

**Current recommendation:** **A. Continue governed model.** Graph is healthy; no cutover required now. Use this matrix when signals change or when the team explicitly re-evaluates.

---

## 5. Re-evaluation triggers

- New migration routinely &gt; 500 lines.
- New environment onboarding becomes a bottleneck.
- Validator or classifier baseline is updated frequently due to new script-only migrations.
- Decision to support a new product or tenant type that needs greenfield DBs only.
- Team agrees to dedicate a sprint to “migration architecture” and is willing to adopt two-track.

Re-run the matrix when any of the above occurs; document the outcome in this file or in EF_MIGRATION_GRAPH_AND_BASELINE_FINAL_DECISION.md.

# Phase 11: CI/CD & Scheduled Drift Automation — Implementation Report

## Summary

Phase 11 wires existing CLI commands into automation only: nightly drift monitoring, weekly profile-pack regression gating, PR/main regression gate, markdown artifact upload, and safe pipeline behavior. No parser or business logic was modified.

---

## 1. Scripts added

| File | Purpose |
|------|---------|
| `backend/scripts/run-parser-governance.ps1` | PowerShell: requires `DefaultConnection` or `ConnectionStrings__DefaultConnection`; builds Release; runs drift-report (step 1) then replay-all-profile-packs (step 2); fails if step 2 exits 1; prints PASS/FAIL. |
| `backend/scripts/run-parser-governance.sh` | Bash: same behavior; sets `ConnectionStrings__DefaultConnection` from `DefaultConnection` if needed; never prints connection string. |

Both scripts build the backend, then run drift-report (markdown to `drift-weekly.md` or `$OutDir`/`$OUT_DIR`), then replay-all-profile-packs with `--ci-mode`. Exit 0 = PASS, 1 = FAIL.

---

## 2. Workflow file

| File | Purpose |
|------|---------|
| `.github/workflows/parser-governance.yml` | Three jobs: **nightly-drift** (cron 02:00 UTC), **weekly-profile-packs** (cron Sunday 03:00 UTC), **pr-profile-packs** (push to main, PRs, workflow_dispatch). |

- **Nightly:** drift-report only; upload artifact; `continue-on-error: true` on drift step so job does not fail on drift; artifact retained 30 days.
- **Weekly:** drift-report + replay-all-profile-packs; fail on regressions; upload drift report artifact; retain 30 days.
- **PR/Main:** replay-all-profile-packs only; fail on regressions.

All jobs require secret `DefaultConnection`; fail with a clear message if missing; never print the connection string.

---

## 3. CLI addition (automation only)

- **replay-all-profile-packs:** New command. Gets all enabled profiles; for each with a non-empty pack, runs pack replay (same logic as replay-profile-pack); aggregates regressions; exit 1 if any. Supports `--ci-mode` for concise one-line-per-profile summary.
- **replay-profile-pack:** Added `--ci-mode` to suppress verbose per-attachment lines and print only summary + PASS/NO-GO.

No parser or validation logic was changed.

---

## 4. How to configure DefaultConnection secret

1. GitHub repo → **Settings** → **Secrets and variables** → **Actions**.
2. **New repository secret**.
3. Name: `DefaultConnection`.
4. Value: full DB connection string (e.g. PostgreSQL).
5. Do not log or echo the value anywhere.

The workflow and scripts pass it as `ConnectionStrings__DefaultConnection` to the .NET app.

---

## 5. How to interpret CI failures

| Symptom | Cause | Action |
|--------|--------|--------|
| Job fails with "DefaultConnection secret is not configured" | Secret not set or not available. | Add `DefaultConnection` in repo Actions secrets. |
| Nightly job succeeds but no artifact | Drift report step failed (e.g. no DB). | Optional: ensure DB is available for scheduled runs; artifact upload uses `if-no-files-found: ignore`. |
| Weekly or PR job fails with "FAIL: Regressions detected" | One or more profile packs had parse regressions. | Fix or revert PROFILE_JSON/template changes; run `replay-profile-pack --profileId <id>` locally; restore profileVersion or pack. |
| "drift-report failed (startup): ..." | App startup failed (e.g. connection string or config). | Check secret value and DB connectivity. |

---

## 6. Artifact naming and retention

- Artifacts are named like `drift-report-<run_number>` (upload step); the file inside is `drift-report-<date>.md`.
- Retained **30 days**.
- No PII in artifacts (reports use only token/field names and counts).

---

## 7. Documentation

- **docs/operations/CI_GOVERNANCE.md:** Scripts, workflow summary, required secret, how to interpret failures, CLI reference.

---

## Acceptance criteria

- Nightly drift report runs and uploads markdown (when report step succeeds).
- Weekly profile-pack job fails CI on regressions.
- PR build fails if regressions exist (pr-profile-packs job).
- No PII in artifacts.
- Deterministic exit codes and output.
- No parser logic changes.

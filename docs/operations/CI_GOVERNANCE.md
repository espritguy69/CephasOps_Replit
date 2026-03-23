# Parser governance (CI/CD & scheduled drift automation)

Phase 11 wires the existing parser CLI commands into automation: nightly drift monitoring, weekly profile-pack regression gating, and PR/main regression gates. No parser or business logic is changed; automation only.

---

## Scripts

| Script | Purpose |
|--------|---------|
| `backend/scripts/run-parser-governance.ps1` | Cross-platform (Windows): drift report + replay-all-profile-packs; exit 0 = PASS, 1 = FAIL. |
| `backend/scripts/run-parser-governance.sh` | Cross-platform (Linux/macOS): same behavior. |

**Required environment:** `DefaultConnection` or `ConnectionStrings__DefaultConnection` must be set to the database connection string. The scripts and CI **must not** print or log this value.

**Behavior:**
1. **Step 1:** `drift-report --days 7 --format markdown --out drift-weekly.md` (informational; script continues if this fails).
2. **Step 2:** `replay-all-profile-packs --ci-mode` (regression gate; script fails if any profile pack reports regressions).
3. **Result:** Clear PASS/FAIL section; exit 0 on PASS, 1 on FAIL.

**Usage (local):**
```powershell
# PowerShell
$env:DefaultConnection = "Host=...;Database=cephasops;..."
.\backend\scripts\run-parser-governance.ps1 -OutDir . -DriftDays 7
```
```bash
# Bash
export DefaultConnection="Host=...;Database=cephasops;..."
./backend/scripts/run-parser-governance.sh
```

---

## GitHub Actions workflow

**File:** `.github/workflows/parser-governance.yml`

### Jobs

| Job | Trigger | Purpose |
|-----|---------|---------|
| **nightly-drift** | Cron `0 2 * * *` (daily 02:00 UTC) | Run drift report; upload markdown artifact; **do not** fail the workflow on drift or report errors. |
| **weekly-profile-packs** | Cron `0 3 * * 0` (Sunday 03:00 UTC) | Run drift report + replay-all-profile-packs; **fail** workflow if any regression; upload drift report artifact. |
| **pr-profile-packs** | Push to `main`, pull requests, `workflow_dispatch` | Run replay-all-profile-packs; **fail** workflow if any regression (PR/main gate). |

### Required secret

- **Name:** `DefaultConnection`
- **Value:** Full database connection string (e.g. PostgreSQL).
- **Where:** Repo **Settings → Secrets and variables → Actions → New repository secret**.

The workflow maps this into `ConnectionStrings__DefaultConnection` for the .NET app. If the secret is missing, the job fails with a clear message and **does not** print the connection string.

### Artifacts

- **drift-report-&lt;date&gt;.md** (or `drift-report-&lt;run_number&gt;` as artifact name): Markdown drift report; retained **30 days**.
- No PII in artifacts (reports contain only token/field names and counts).

### Configuring the secret

1. GitHub repo → **Settings** → **Secrets and variables** → **Actions**.
2. **New repository secret**.
3. Name: `DefaultConnection`.
4. Value: your connection string (e.g. `Host=...;Port=5432;Database=cephasops;Username=...;Password=...;SslMode=Disable`).
5. Do not use this secret in logs or in artifact content.

---

## How to interpret CI failures

| Failure | Meaning | Action |
|--------|--------|--------|
| **"DefaultConnection secret is not configured"** | The repo secret is missing or not available to the workflow. | Add `DefaultConnection` in repo Actions secrets. |
| **"drift-report failed (startup): ..."** | App could not start (e.g. config/DB). | Check connection string and DB availability; in nightly, the job may still succeed and simply not upload an artifact. |
| **"FAIL: Regressions detected"** (replay-all-profile-packs) | One or more profile packs had parse regressions (e.g. status got worse vs baseline). | Fix or revert PROFILE_JSON / template changes; run `replay-profile-pack --profileId <id>` locally to reproduce; restore previous profileVersion or pack. |
| **Build failed** | Compilation or tooling error. | Fix the codebase; not parser-governance specific. |

---

## CLI commands used (no changes to parser logic)

- **drift-report** — Phase 10; report only; deterministic exit 0/1.
- **replay-all-profile-packs** — Phase 11; runs pack replay for every enabled profile; exit 1 if any regression.
- **replay-profile-pack** — Single profile; supports `--ci-mode` for concise output.
- **replay-profiles** — Existing; replays by recent attachments per profile (different from pack-based).

---

## Deterministic behavior

- Exit codes: 0 = success, 1 = failure (config error or regression).
- Reports use invariant culture and deterministic ordering; no PII in output or artifacts.

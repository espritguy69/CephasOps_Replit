# Self-Hosted GitHub Runner Checklist — Phase 11 Parser Governance

Operational checklist for setting up a self-hosted runner for CephasOps parser governance (drift-report, replay-all-profile-packs). **No parser logic changes.** Execute in order; check off each item when done.

---

## 1. Machine Preparation

### Hardware requirements
- [ ] CPU: minimum 2 cores (4 recommended for concurrent jobs).
- [ ] RAM: minimum 4 GB (8 GB recommended).
- [ ] Disk: minimum 20 GB free for repo, .NET SDK, and workflow workspace.
- [ ] Network: stable connection; no outbound restrictions to `github.com` and `api.github.com`.

### OS requirements
- [ ] **Windows:** Windows Server 2019+ or Windows 10/11 (64-bit); supported for .NET and runner.
- [ ] **Linux:** Ubuntu 20.04+ LTS or equivalent (64-bit); supported for .NET and runner.
- [ ] OS is patched and supported (security updates enabled).

### Network requirements
- [ ] Runner can reach GitHub (HTTPS, port 443).
- [ ] Runner can reach Postgres **only** on a private network (e.g. VPN, private VPC, or same LAN). Confirm no path from public internet to Postgres.
- [ ] Outbound: `github.com`, `api.github.com`. Inbound: none required for GitHub (runner polls).

### Required software
- [ ] **Git** installed and on PATH (runner uses it for checkout).
- [ ] **.NET SDK** matching repo (e.g. 10.0.x or version in workflow); install from official Microsoft packages.
- [ ] **Postgres client** (optional but useful): `psql` or equivalent to test connectivity from runner host.

### Folder structure
- [ ] Decide runner root directory (e.g. `C:\actions-runner` or `/opt/actions-runner`).
- [ ] Ensure the directory is owned by the service account that will run the runner (no admin-only paths if possible).
- [ ] Ensure enough free space in that volume for workspace and build outputs.

---

## 2. GitHub Self-Hosted Runner Setup

### Repo settings path
- [ ] Open GitHub repo → **Settings** → **Actions** → **Runners**.
- [ ] Click **New self-hosted runner**; note the instructions and token (short-lived).

### Download and configure
- [ ] On the runner machine, download the runner package from the URL shown in GitHub (choose OS/architecture).
- [ ] **Windows:** Extract to the chosen folder; run `config.cmd` with the URL and token from GitHub.
- [ ] **Linux:** Extract (e.g. `tar -xzf`); run `./config.sh` with the URL and token from GitHub.
- [ ] When prompted for runner name, use a consistent name (e.g. `cephasops-ci-01`).
- [ ] When prompted for runner group, use **Default** unless you have a custom group.

### Labels to assign
- [ ] During config (or later in repo Settings → Runners), assign labels: **self-hosted**, **cephasops-ci** (or the exact label set your workflow will use in `runs-on`).
- [ ] Ensure the workflow’s `runs-on` value matches these labels (e.g. `runs-on: [self-hosted, cephasops-ci]`).

### Install as service
- [ ] **Windows:** Run the `run.cmd` once to confirm; then run the install command shown (e.g. `svc.sh install` equivalent for Windows, or use the provided install script) so the runner runs as a service and survives reboots.
- [ ] **Linux:** Run `./svc.sh install` (as the user that will run the runner); then `./svc.sh start`.
- [ ] Confirm the service is set to start automatically on boot.

### Verify online status
- [ ] In GitHub: **Settings** → **Actions** → **Runners**; runner appears with a green dot (Idle).
- [ ] Trigger a simple test workflow (or workflow_dispatch) and confirm the job is assigned to this runner.

---

## 3. Secure Database Setup

### Create CI database
- [ ] On the Postgres server (in the private network), create a **separate** database for CI, e.g. `cephasops_ci`. Do **not** use the production database name.
- [ ] Document the host, port, and database name used for CI (for the connection string).

### Create least-privilege user
- [ ] Create a dedicated user (e.g. `cephasops_ci_user`) with a strong password; store the password in a secrets manager or secure store, not in repo or docs.
- [ ] Grant only what is needed: connect to `cephasops_ci`, usage on schema(s) used by the app, and permissions to run migrations and read/write only the tables the parser governance needs (e.g. ParsedOrderDrafts, ParserReplayRuns, ParserTemplates, ParseSessions, EmailAttachment, etc., as per app’s EF model).
- [ ] Do **not** grant superuser, create database, or other admin roles to this user.

### Required permissions only
- [ ] Verify the CI user cannot access other databases or roles.
- [ ] Prefer read-only where possible for reporting; write only where the app needs it (e.g. replay runs, migrations if you run them in CI).

### Confirm connectivity from runner machine
- [ ] From the runner host, using the same network path the app will use, test connection to Postgres (e.g. `psql` or a small .NET test) with the CI user and `cephasops_ci` database.
- [ ] Confirm connection string format (Host, Port, Database, Username, Password, SslMode) and that no firewall or proxy blocks it.

---

## 4. Environment Variable Setup

### Set ConnectionStrings__DefaultConnection
- [ ] **Windows (system environment variable):**  
  - Open **System Properties** → **Advanced** → **Environment Variables**.  
  - Under **System variables** (or **User variables** for the service account), add a new variable:  
  - Name: `ConnectionStrings__DefaultConnection`  
  - Value: full connection string for `cephasops_ci` and `cephasops_ci_user`.  
  - Do **not** enclose in quotes unless required; ensure no trailing spaces.  
  - If the runner runs as a service, set the variable for the **account that runs the runner service** (e.g. System or a dedicated user); restart the runner service after changing env vars.
- [ ] **Linux (systemd service environment):**  
  - Edit the systemd service file for the runner (e.g. `/etc/systemd/system/actions.runner.*.service` or where it was installed).  
  - Add `Environment="ConnectionStrings__DefaultConnection=Host=...;Port=5432;Database=cephasops_ci;..."` (replace with real value; escape as needed).  
  - Alternatively, use an environment file with restricted permissions (e.g. `EnvironmentFile=/etc/cephasops-runner/env`) and ensure the file is not world-readable.  
  - Run `systemctl daemon-reload` and restart the runner service.

### Confirm availability to runner service
- [ ] Restart the runner service after setting the variable.
- [ ] Run a workflow job that echoes a non-secret env var (e.g. `RUNNER_NAME`) to confirm the job runs; do **not** echo or print `ConnectionStrings__DefaultConnection` or `DefaultConnection`.

### Confirm it is NOT echoed anywhere
- [ ] Search the workflow file and any scripts it runs for any step that could print env vars (e.g. `env`, `printenv`, or dumping secrets). Ensure no step prints connection strings or secrets.
- [ ] Confirm the app and scripts never log the connection string (they should not; this is a verification step).

---

## 5. Workflow Configuration

### Update runs-on
- [ ] In the workflow file, set each job’s `runs-on` to the self-hosted labels, e.g. `runs-on: [self-hosted, cephasops-ci]` (match the labels you assigned to the runner).
- [ ] Ensure no job in this workflow uses `runs-on: ubuntu-latest` (or remove/update those jobs so governance runs only on the self-hosted runner).

### Remove dependency on GitHub secret (if using host-level env)
- [ ] If the runner has `ConnectionStrings__DefaultConnection` set at host/service level, remove or leave unset the workflow step that injects the secret (e.g. `env: ConnectionStrings__DefaultConnection: ${{ secrets.DefaultConnection }}`) so the job uses the runner’s environment only.
- [ ] If you keep the secret as a fallback, ensure the runner env takes precedence or that only one source is used; avoid duplicate or conflicting values.
- [ ] Do **not** add the connection string to the repo or to workflow files in plain text.

### Confirm jobs
- [ ] **nightly-drift:** Runs on schedule; runs drift-report; uploads artifact; does not fail on drift.
- [ ] **weekly-profile-packs:** Runs on schedule; drift-report + replay-all-profile-packs; fails on regressions; uploads artifact.
- [ ] **pr-profile-packs:** Runs on push/PR/workflow_dispatch; replay-all-profile-packs; fails on regressions.
- [ ] All three jobs use the same `runs-on` so they run on the self-hosted runner.

---

## 6. First Validation Run

### Use workflow_dispatch
- [ ] In GitHub: **Actions** → select the Parser Governance workflow → **Run workflow** → **Run workflow**.
- [ ] Wait for the run to start and jobs to be scheduled.

### Confirm jobs run on self-hosted runner
- [ ] Open the run and each job; confirm the job is assigned to your self-hosted runner (runner name/labels visible in the job header or logs).
- [ ] Confirm no “waiting for runner” timeout; jobs start and complete.

### Drift markdown artifact uploads
- [ ] For a run that includes drift report (e.g. nightly or weekly), confirm the artifact (e.g. drift-report-*) is uploaded and downloadable.
- [ ] Open the artifact and confirm it is markdown and contains no PII (only token/field names and counts).

### No secret printed
- [ ] Open job logs and search for any substring of the connection string (host name, database name, password). Confirm none appear.
- [ ] Confirm no step prints `ConnectionStrings__DefaultConnection` or `DefaultConnection` values.

### PASS/FAIL behavior correct
- [ ] If no regressions: job completes with success and you see PASS or equivalent in logs.
- [ ] If you have a way to simulate a regression (e.g. temporary template change), confirm the job fails and logs show FAIL/regressions (then revert the test).

---

## 7. Security Hardening

### Postgres not publicly exposed
- [ ] Confirm Postgres listens only on private IP or localhost; not on 0.0.0.0 or a public IP.
- [ ] Confirm no port forwarding, NAT, or cloud security group allows 5432 from the public internet.
- [ ] Use VPN or private network path from runner to Postgres only.

### Firewall rules
- [ ] On the runner host: allow outbound to GitHub (443) and to Postgres (5432 or your port) on the private network; deny unnecessary inbound.
- [ ] On the Postgres server: allow 5432 only from the runner host (or runner subnet); deny all other sources.

### Least-privilege DB user
- [ ] CI user has no superuser, no create role, no create database; only connect and limited DDL/DML on the CI database as required by the app.
- [ ] Revoke any default public grants that are not needed.

### Restrict runner host access
- [ ] Limit who can log in to the runner machine (SSH or RDP); use strong auth and principle of least privilege.
- [ ] Do not run the runner as root/Administrator if avoidable; use a dedicated service account.
- [ ] Restrict physical and network access to the runner so only authorized personnel and GitHub can influence it.

### OS updates
- [ ] Enable automatic security updates (or a regular patch cycle) for the runner OS.
- [ ] Document and follow a monthly or quarterly review of pending updates.

### .NET SDK updates
- **CephasOps standard:** .NET 10.0.x SDK and net10.0 only; do not use lower versions.
- [ ] Subscribe to .NET release notes; plan updates when the repo’s target framework or workflow’s dotnet-version is updated.
- [ ] Test runner workflows after SDK updates in a non-production branch first.

---

## 8. Monitoring & Maintenance

### Runner online status
- [ ] Weekly: Check **Settings** → **Actions** → **Runners**; runner is Idle or Busy (green), not Offline.
- [ ] If offline, check service status, disk space, and network on the runner host; restart service if needed.

### Disk cleanup (artifacts folder)
- [ ] Configure or document a cleanup policy for the runner’s workspace (e.g. delete old `_work` contents or use runner’s built-in cleanup).
- [ ] Prevent disk fill from repeated workflow runs; monitor free space.

### Runner update procedure
- [ ] When GitHub advises a runner version update, follow the official docs: stop service, replace runner binary, start service (or use the script they provide).
- [ ] Test workflow_dispatch after updating the runner.

### Backup CI database (optional)
- [ ] If you need CI data retention, schedule periodic backups of `cephasops_ci` (e.g. pg_dump) to a secure location.
- [ ] Do not store backups in the repo or in public storage.

### Rotate CI DB password periodically
- [ ] Plan rotation (e.g. every 90 days); update the password in Postgres and in the runner’s environment (and in secrets manager if used); restart runner service; run a validation workflow.

---

## 9. What NOT To Do

- [ ] **Do not** use the production database for CI; use a dedicated `cephasops_ci` (or equivalent) database only.
- [ ] **Do not** commit connection strings or passwords in the repo, workflow files, or scripts.
- [ ] **Do not** print or log `ConnectionStrings__DefaultConnection`, `DefaultConnection`, or any secret in workflow steps or application logs.
- [ ] **Do not** expose Postgres port (5432) to the public internet; keep it on a private network only.
- [ ] **Do not** grant superuser or unnecessary privileges to the CI database user.
- [ ] **Do not** modify parser or business logic to “fix” CI; only operational and workflow/config changes.
- [ ] **Do not** disable or weaken firewall or access controls to make CI work; fix connectivity in a secure way instead.
- [ ] **Do not** run the runner as root/Administrator unless strictly required and documented.
- [ ] **Do not** store production credentials on the runner; use CI-only credentials.

---

## 10. Final Go-Live Checklist

- [ ] All steps in sections 1–8 are completed and checked off where applicable.
- [ ] At least one full test run (workflow_dispatch) succeeded: jobs ran on self-hosted runner, artifact uploaded, no secrets in logs, PASS/FAIL correct.
- [ ] Weekly job schedule is enabled and next run time is visible under **Actions**.
- [ ] PR gate is active: push/PR triggers the regression job and it runs on the self-hosted runner.
- [ ] Documentation (e.g. CI_GOVERNANCE.md, this checklist) has been reviewed and any org-specific details (hostnames, labels, user names) are updated.
- [ ] Team knows where to check runner status (GitHub Runners page) and how to interpret FAIL (regressions) vs other failures (config, connectivity).
- [ ] Escalation path for runner offline or CI failures is defined (e.g. who checks runner, who rotates DB password).

---

*End of checklist. No code, YAML, or scripts are included; this is operational execution guidance only.*

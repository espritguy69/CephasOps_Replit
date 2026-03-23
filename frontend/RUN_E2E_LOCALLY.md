# Running Playwright E2E Locally

Short guide to run smoke and auth tests on your machine. **No app code changes**—documentation only.

---

## 1. Services that must be running

| Service        | Required for | Notes |
|----------------|--------------|--------|
| **PostgreSQL** | All auth/smoke | Backend needs DB for login and data. |
| **Backend API**| All auth/smoke | Frontend proxies `/api` to it; setup and smoke call login. |
| **Frontend**   | All E2E       | Playwright can start it via `webServer` (default) or you run it yourself. |

Start **database** and **backend** before running Playwright. The config can start the frontend for you (`npm run dev`).

---

## 2. URLs and ports

| What        | Default URL              | Port |
|------------|---------------------------|------|
| Frontend   | `http://localhost:5173`  | 5173 |
| Backend API| `http://localhost:5000`   | 5000 |
| PostgreSQL | localhost                 | 5432 |

- Playwright `baseURL`: `PLAYWRIGHT_BASE_URL` or `http://localhost:5173`.
- Vite proxy: `/api` → `VITE_API_TARGET` or `http://localhost:5000`.

---

## 3. Env vars for E2E login

Set in `frontend/.env` or the shell. Without these, **auth setup skips** and smoke (which depends on it) will not run authenticated tests.

| Variable | Purpose | Alternative |
|----------|---------|-------------|
| `TEST_EMAIL` | E2E login email | `E2E_TEST_USER_EMAIL` |
| `TEST_PASSWORD` | E2E login password | `E2E_TEST_USER_PASSWORD` |

Optional: `PLAYWRIGHT_BASE_URL`, `PLAYWRIGHT_API_BASE_URL` (defaults above). For API-only health check, backend uses the same user for token.

---

## 4. Startup commands

**Terminal 1 – PostgreSQL**  
Ensure the server is running (e.g. start the `postgres` service or use your usual command).

**Terminal 2 – Backend**
```bash
cd backend/src/CephasOps.Api
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://0.0.0.0:5000
```
- Swagger: `http://localhost:5000/swagger`

**Terminal 3 – Frontend (optional)**  
If you want to reuse an existing dev server instead of Playwright starting it:
```bash
cd frontend
npm run dev
```
- App: `http://localhost:5173`

If you don’t start the frontend, Playwright will run `npm run dev` for you (see `playwright.config.ts` `webServer`). Backend and DB must still be running.

---

## 5. How to run smoke

From **frontend** directory:

| Scope | Command |
|-------|---------|
| **Core smoke** (minimal: Boot & Health + P1 modules) | `npx playwright test --project=smoke --grep "Core smoke"` |
| **Extended smoke** (Core + P2 modules + UI routes + basic flows) | `npx playwright test --project=smoke --grep "Extended smoke"` |
| **Full smoke** (all smoke specs, including P3) | `npx playwright test --project=smoke` |

- Smoke project **depends on setup**: setup runs login and saves `.auth/user.json`; smoke uses that storage state.
- To exclude P3 (admin/department) in CI:  
  `npx playwright test --project=smoke --grep-invert "Future smoke"`

---

## 6. Troubleshooting

### Backend not running

- **Symptom:** `[vite] http proxy error: /api/auth/login` and `ECONNREFUSED`.
- **Cause:** Nothing is listening on the API port (default 5000).
- **Fix:** Start the backend (see §4). Confirm in another terminal: `curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/api/admin/health` (or open Swagger).

### Login failing

- **Symptom:** Auth setup times out waiting for `app-shell-main`; you stay on the login page.
- **Causes:** (1) Backend not running or not reachable (proxy ECONNREFUSED). (2) Wrong or missing `TEST_EMAIL` / `TEST_PASSWORD`. (3) User doesn’t exist or DB not migrated.
- **Fix:** Ensure backend is up, DB is migrated, and env has valid credentials (e.g. the seeded admin from AGENTS.md). Check backend logs for 401/5xx on `/api/auth/login`.

### Proxy ECONNREFUSED

- **Symptom:** Browser or tests hit the frontend; requests to `/api/*` fail with connection refused.
- **Cause:** Vite proxies `/api` to `VITE_API_TARGET` (default `http://localhost:5000`). That host:port is not accepting connections.
- **Fix:** Start the backend on that port. If the API runs elsewhere, set `VITE_API_TARGET` (and optionally `PLAYWRIGHT_API_BASE_URL` for API-level tests) to that URL.

### Database unavailable

- **Symptom:** Backend fails on startup or returns 500 on login/health; logs show connection errors.
- **Cause:** PostgreSQL not running or wrong connection string (see `.cursor/rules/postgress.mdc` or `appsettings.Development.json`).
- **Fix:** Start PostgreSQL. Verify connection string (host, port 5432, database `cephasops`, user/password). Run migrations if needed (see AGENTS.md).

---

For more (env vars, structure, CI): `frontend/e2e/README.md`. For smoke tiers and design: `frontend/e2e/SMOKE_BLUEPRINT.md`.

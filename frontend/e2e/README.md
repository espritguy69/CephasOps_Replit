# E2E Testing (Playwright)

Playwright end-to-end tests for the CephasOps admin portal. Backend must be running for full smoke and auth flows.

## Setup

1. **Install dependencies** (from repo root or `frontend/`):
   ```bash
   cd frontend && npm ci
   ```

2. **Install Playwright browsers** (Chromium is used in CI):
   ```bash
   npx playwright install chromium
   ```
   For all browsers: `npx playwright install`

3. **Environment variables** (optional; create `frontend/.env` or export):
   - `PLAYWRIGHT_BASE_URL` – App URL (default: `http://localhost:5173`)
   - `PLAYWRIGHT_API_BASE_URL` – API URL (default: `http://localhost:5000`)
   - `TEST_EMAIL` / `TEST_PASSWORD` – E2E login (or `E2E_TEST_USER_EMAIL` / `E2E_TEST_USER_PASSWORD`)
   - Optional: `ADMIN_TEST_EMAIL` / `ADMIN_TEST_PASSWORD`, `TEST_TENANT_SLUG` for future role/tenant tests

   Without credentials, auth-dependent tests are skipped and the auth setup writes empty storage state.

## Running tests

- **All E2E tests** (guest → setup → smoke + auth):
  ```bash
  cd frontend && npx playwright test
  ```

- **Headed (see browser)**:
  ```bash
  npx playwright test --headed
  ```

- **Single spec**:
  ```bash
  npx playwright test e2e/specs/smoke.spec.ts
  npx playwright test e2e/specs/auth.spec.ts
  ```

- **Single project**:
  ```bash
  npx playwright test --project=guest
  npx playwright test --project=smoke
  ```

- **UI mode** (interactive):
  ```bash
  npm run test:e2e:ui
  ```

## Open report

After a run:
```bash
npx playwright show-report
```

## Structure

- `e2e/constants.ts` – Centralized `ROUTES`, `TEST_IDS`, and `SELECTORS` for specs and helpers
- `e2e/SMOKE_BLUEPRINT.md` – Smoke suite design (tiers: Core / Extended / Future, anchors, P1/P2/P3). Run minimal set: `--grep "Core smoke"`
- `e2e/specs/` – Test specs: `smoke.spec.ts`, `smoke-modules.spec.ts`, `auth.spec.ts`, `auth.guest.spec.ts`, `health.spec.ts`
- `e2e/helpers/` – Env, auth, navigation, shared expectations
- `e2e/pages/` – Lightweight page objects (Login, AppShell)
- `e2e/fixtures/` – Custom fixtures (optional)
- `e2e/test-data/` – Placeholder for static test data; credentials from env only
- `e2e/auth.setup.ts` – Generates `.auth/user.json` for authenticated projects (guest has no storageState)

## CI

GitHub Actions runs the E2E workflow on push/PR to `main` and `development`. It:

- Starts PostgreSQL and the backend, then runs the frontend dev server
- Runs Playwright with Chromium
- Uploads the HTML report and (on failure) test-results as artifacts

Secrets `E2E_TEST_USER_EMAIL` and `E2E_TEST_USER_PASSWORD` enable the backend health check and full auth flow; without them, those tests are skipped.

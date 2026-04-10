# CephasOps E2E Testing Runbook

## Environment Support Matrix

| Environment | Guest/Health Tests | Route Smoke Tests | Authenticated Tests |
|-------------|-------------------|-------------------|---------------------|
| **Replit** | YES | YES | NO — .NET 10 backend cannot run here |
| **Dev/Staging VPS** | YES | YES | YES — requires backend + DB + credentials |
| **CI Pipeline** | YES | YES | YES — if backend service is available |

## Replit: Frontend-Only E2E

These tests validate the frontend boots, routes resolve, and unauthenticated flows work.
No backend is required.

### Prerequisites

1. Frontend dev server running on port 5000 (the default workflow)
2. Playwright Chromium installed: `npx playwright install chromium`
3. System libraries installed via Nix (glib, nss, gtk3, mesa, etc. — already configured)

### Commands

**Guest tests** (login page, auth redirects, health checks):
```bash
cd frontend && bash scripts/run-e2e.sh guest
```

**Health tests only** (app boot, HTTP 200):
```bash
cd frontend && bash scripts/run-e2e.sh guest "Health"
```

**Route smoke tests** (all 30+ routes render without 404):
```bash
cd frontend && bash scripts/run-e2e.sh smoke "Extended smoke"
```

**All frontend-safe tests at once:**
```bash
cd frontend && bash scripts/run-e2e.sh guest && bash scripts/run-e2e.sh smoke "Extended smoke"
```

### What These Tests Verify

- `health.spec.ts` — Frontend returns 200 with text/html; login page loads
- `auth.guest.spec.ts` — Login form renders; protected routes redirect to /login; /orders shows login
- `smoke.spec.ts` (Extended) — 30+ routes (/orders, /scheduler, /inventory/*, /reports/*, /billing/*, /payroll/*, /pnl/*, /accounting, etc.) load without 404

### What These Tests Cannot Verify

- Backend API health (requires .NET 10)
- Authenticated dashboard, sidebar, user menu
- Module-specific data rendering (requires DB)
- Login/logout flows with real credentials

## Full-Stack E2E (Dev/Staging VPS)

These tests require the .NET 10 backend running with PostgreSQL and valid test credentials.

### Prerequisites

1. Backend running on port 5001 (or configured port)
2. Frontend running on port 5000
3. PostgreSQL database accessible
4. Test user credentials set in environment

### Environment Variables

```bash
export PLAYWRIGHT_BASE_URL=http://localhost:5000
export PLAYWRIGHT_API_BASE_URL=http://localhost:5001
export TEST_EMAIL=your-test-user@example.com
export TEST_PASSWORD=your-test-password
```

### Commands

**Full suite (all projects):**
```bash
cd frontend && npx playwright test --reporter=list
```

**Core smoke only (fastest authenticated check):**
```bash
cd frontend && npx playwright test --project=smoke --grep "Core smoke" --reporter=list
```

**Auth flows (login, logout, session):**
```bash
cd frontend && npx playwright test --project=auth --reporter=list
```

**Authenticated module visibility:**
```bash
cd frontend && npx playwright test --project=smoke --grep "P1\|P2" --reporter=list
```

**Everything except nightly/P3:**
```bash
cd frontend && npx playwright test --grep-invert "Future smoke" --reporter=list
```

### Expected Results (Full Stack)

| Project | Tests | Expected |
|---------|-------|----------|
| guest | 5 | All pass |
| health | 2 | All pass |
| setup | 1 | Pass (performs real login, saves session) |
| smoke (Core) | 5 | All pass (backend health + P1 modules) |
| smoke (Extended) | 50+ | All pass (route coverage) |
| smoke-modules (P2) | 6 | All pass (inventory, reports, settings, payroll, P&L, accounting) |
| smoke-modules (P3) | 2 | All pass (admin, department selector) |
| auth | 3 | All pass (dashboard load, nav visible, logout) |

## Test Architecture

```
frontend/e2e/
├── auth.setup.ts              # Login once, save session to .auth/user.json
├── constants.ts               # Shared routes, test IDs, selectors
├── E2E_RUNBOOK.md             # This file
├── SMOKE_BLUEPRINT.md         # Test tier design rationale
├── specs/
│   ├── health.spec.ts         # [guest] Boot + login page
│   ├── auth.guest.spec.ts     # [guest] Unauthenticated flows
│   ├── auth.spec.ts           # [auth] Authenticated flows (needs backend)
│   ├── smoke.spec.ts          # [smoke] API health + 30+ route checks
│   └── smoke-modules.spec.ts  # [smoke] Module visibility via data-testid
├── helpers/
│   ├── auth.ts                # Login helpers, credential access
│   ├── env.ts                 # Environment variable access
│   └── expectations.ts        # Shared assertion helpers
└── pages/
    ├── AppShell.ts            # App shell page object
    └── LoginPage.ts           # Login page object
```

## Troubleshooting

### Chromium fails to launch with "cannot open shared object file"
The Nix environment is missing system libraries. The required packages are already
configured in `.replit` (glib, nss, gtk3, mesa, etc.). If you see `libgbm.so.1` errors,
the `run-e2e.sh` script handles this automatically by adding the mesa-libgbm path to
LD_LIBRARY_PATH.

### Tests timeout waiting for app-shell-main
This means the test expects an authenticated session but no backend is available.
In Replit, only run `guest` project and `Extended smoke` grep — skip Core smoke
and auth projects.

### Setup project creates empty auth state
This is expected behavior when TEST_EMAIL/TEST_PASSWORD are not set. The setup
writes an empty `{cookies: [], origins: []}` file so dependent projects can still
run (they'll see the login page instead of authenticated content).

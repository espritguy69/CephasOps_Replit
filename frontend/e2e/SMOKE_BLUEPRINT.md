# CephasOps E2E Smoke Suite Blueprint

Smoke tests answer: **Is the core product healthy after a change?** They are fast, stable, and read-only where possible.

---

## Test tiers: Core / Extended / Future

| Tier | Purpose | How to run |
|------|---------|------------|
| **Core smoke** | Must-run minimal set. Boot, health, and critical authenticated modules. | `npx playwright test --project=smoke --grep "Core smoke"` |
| **Extended smoke** | Full route and module coverage. UI Routes (all), Basic flows, P2 modules. | Run full smoke project (no grep), or `--grep "Extended smoke"` |
| **Future / nightly** | Optional or run in nightly. P3 admin, department selector. | `--grep "Future smoke"` or exclude with `--grep-invert "Future smoke"` |

**Describe names used for filtering:**

- **smoke.spec.ts:** `Core smoke – Boot & Health` | `Extended smoke – UI Routes (all)` | `Extended smoke – Basic flows`
- **smoke-modules.spec.ts:** `Core smoke – P1 Authenticated modules` | `Extended smoke – P2 Authenticated modules` | `Future smoke – P3 Admin & department`

Keep new tests in the appropriate tier so core stays small and fast.

---

## 1. Platform boot / access

| Module/Route | Why it matters | Smoke goal | Anchor | Guest/Auth | Read/Mutate | Priority |
|--------------|----------------|------------|--------|------------|-------------|----------|
| Frontend root | App serves | 200 + text/html | Response status, Content-Type | guest | read | P1 |
| Login page | Auth entry | Form visible | `expectLoginPageVisible` (email + sign in button) | guest | read | P1 |
| Protected route redirect | Guests cannot access app | Redirect to /login | URL + no app-shell-main | guest | read | P1 |
| Authenticated shell | Post-login shell renders | Main + user menu visible | app-shell-main, user-menu-trigger | auth | read | P1 |

---

## 2. Core navigation

| Module/Route | Why it matters | Smoke goal | Anchor | Guest/Auth | Read/Mutate | Priority |
|--------------|----------------|------------|--------|------------|-------------|----------|
| Sidebar | Primary nav | Visible when authenticated | sidebar (testid) | auth | read | P1 |
| Top nav / user menu | User identity, logout | User menu trigger visible | user-menu-trigger | auth | read | P1 |
| Major modules reachable | No crash on navigation | Shell + module heading or root | app-shell-main + heading/testid | auth | read | P1 |
| Logout | Session end | Redirect to login, form visible | expectLoginPageVisible | auth | read | P1 |

---

## 3. High-value module smokes

| Module/Route | Why it matters | Smoke goal | Anchor | Guest/Auth | Read/Mutate | Priority |
|--------------|----------------|------------|--------|------------|-------------|----------|
| Dashboard | Home after login | Shell + dashboard content | app-shell-main, heading "Operations Dashboard" | auth | read | P1 |
| Orders | Core workflow | List or empty state loads | heading "Orders" or main | auth | read | P1 |
| Scheduler timeline | Resource scheduling | Timeline root visible | scheduler-timeline-root (testid) | auth | read | P1 |
| Inventory stock summary | Materials visibility | Page loads | heading "Stock Summary" or main | auth | read | P2 |
| Reports hub | Reporting entry | Page loads | heading or search placeholder | auth | read | P2 |
| Settings (company) | Settings access | Company profile or settings shell | heading "Company Profile" / "Settings" | auth | read | P2 |
| Admin (background-jobs) | Admin-only access | Page or access denied | heading "Background Jobs" or "Access Denied" | auth | read | P3 |
| Department selector | Department-scoped views | Selector visible, can open | department-selector-trigger (desktop) | auth | read | P3 |

**Intentionally not covered as smoke (or deferred):**

- **Customers/Accounts**: No dedicated customers module in routes (CompaniesPage removed). Skip.
- **Deep settings sub-routes**: One settings entry (e.g. /settings/company) is enough for smoke.
- **RMA, Operations, Billing, Payroll, P&L, KPI, Accounting, Assets, Workflow, Tasks, Buildings**: P2/P3 expansion; current blueprint focuses on Dashboard, Orders, Scheduler, Inventory, Reports, Settings, Admin.

---

## 4. Auth / access control

| Goal | How | Anchor | Priority |
|------|-----|--------|----------|
| Guest cannot access protected route | Visit /dashboard, expect /login | URL, no app-shell-main | P1 |
| Logged-in user can access normal page | Visit /orders, expect shell + content | expectAuthenticatedShell, heading | P1 |
| Non-admin blocked from admin page | Visit /admin/background-jobs as normal user; expect Access Denied or redirect | heading "Access Denied" or login | P3 (only if seeded non-admin exists) |

---

## 5. Tenant / department awareness

| Goal | How | Anchor | Priority |
|------|-----|--------|----------|
| Department selector visible (desktop) | On dashboard or orders, selector in top nav | department-selector-trigger | P3 |
| Selector can open | Click trigger, dropdown or list visible | Same + menu/list | P3 |
| No shell break on context change | Optional: change department, assert shell still visible | app-shell-main | P3 (light) |

Tenant/company switcher: reserved testid only; no dedicated UI in current app. Skip until feature exists.

---

## Selector strategy

- **Prefer:** TEST_IDS (app-shell-main, user-menu-trigger, sidebar, scheduler-timeline-root, department-selector-trigger), getByRole('heading', { name: /.../ }), existing helpers.
- **Add testids only when:** High-value module has no stable heading/role; add one root testid (e.g. orders-page-root) and centralize in `e2e/constants.ts`.
- **Avoid:** Brittle text, deep CRUD, long workflows, drag/drop, race-prone waits.

---

## Test design rules

- Navigate → assert no crash → assert guest or auth state → assert one stable anchor.
- Optional: one lightweight interaction (e.g. open department dropdown) if stable.
- No long workflows, no mutation-heavy flows, no dependence on specific seed data beyond “has access”.

---

## File layout

- **health.spec.ts** (guest): Root response, login page form.
- **auth.guest.spec.ts** (guest): Login form, protected redirect, guest isolation.
- **auth.spec.ts** (auth): Dashboard load, sidebar, logout.
- **smoke.spec.ts** (auth): Boot/health API, UI routes (not 404), basic flows.
- **smoke-modules.spec.ts** (auth): P1/P2/P3 authenticated module smokes (dashboard, orders, scheduler timeline, inventory, reports, settings, admin, department selector).

Smoke project `testMatch`: `smoke\.spec\.ts` and `smoke-modules\.spec\.ts`.

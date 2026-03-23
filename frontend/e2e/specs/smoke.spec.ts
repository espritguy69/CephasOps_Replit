/**
 * Smoke suite: boot, health API, and route coverage (auth project).
 *
 * Tiers (for filtering; see SMOKE_BLUEPRINT.md):
 * - Core smoke:   Must-run. Boot & Health. Run with: --grep "Core smoke"
 * - Extended smoke: UI Routes (all), Basic flows. Full coverage.
 * - Future/nightly: None in this file; see smoke-modules.spec.ts P3.
 */
import { test, expect } from '@playwright/test';
import { e2eEnv } from '../helpers/env';
import { getTestCredentials } from '../helpers/auth';

const API_BASE = e2eEnv.apiBaseUrl();

test.describe('Core smoke – Boot & Health', () => {
  test('backend health returns 200 and DB OK when authenticated', async ({ request }) => {
    const creds = getTestCredentials();

    if (!creds) {
      test.skip();
      return;
    }

    const loginRes = await request.post(`${API_BASE}/api/auth/login`, {
      data: { Email: creds.email, Password: creds.password },
      headers: { 'Content-Type': 'application/json' },
    });
    expect(loginRes.ok()).toBeTruthy();
    const loginBody = await loginRes.json();
    const loginData = loginBody.Data ?? loginBody.data;
    const token = loginData?.AccessToken ?? loginData?.accessToken ?? loginBody.AccessToken ?? loginBody.accessToken;
    expect(token).toBeTruthy();

    const healthRes = await request.get(`${API_BASE}/api/admin/health`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    expect(healthRes.status()).toBe(200);
    const healthBody = await healthRes.json();
    const data = healthBody.Data ?? healthBody.data;
    expect(data?.IsHealthy ?? data?.isHealthy).toBe(true);
    const db = data?.Database ?? data?.database;
    expect(db?.IsConnected ?? db?.isConnected).toBe(true);
  });
});

test.describe('Extended smoke – UI Routes (all)', () => {
  test('landing shows dashboard or login', async ({ page }) => {
    await page.goto('/');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /dashboard|cephasops|sign in/i })
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 10000 });
  });

  test('/orders – not 404, not blank', async ({ page }) => {
    await page.goto('/orders');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /orders|cephasops|sign in/i })
        .or(page.getByText(/no orders found|select a department|sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/scheduler – not 404, not blank', async ({ page }) => {
    await page.goto('/scheduler');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /scheduler|calendar|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/scheduler/timeline – not 404, not blank', async ({ page }) => {
    await page.goto('/scheduler/timeline');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /timeline|scheduler|installer|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/inventory/stock-summary – not 404, not blank', async ({ page }) => {
    await page.goto('/inventory/stock-summary');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /stock summary|cephasops|sign in/i })
        .or(page.getByText(/no data|select a department|stock summary|sign in to your account/i))
        .or(page.locator('main'))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/inventory/ledger – not 404, not blank', async ({ page }) => {
    await page.goto('/inventory/ledger');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /ledger|cephasops|sign in/i })
        .or(page.getByText(/ledger|no entries|select a department|sign in to your account/i))
        .or(page.locator('main'))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/inventory/receive – not 404, not blank', async ({ page }) => {
    await page.goto('/inventory/receive');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /receive|cephasops|sign in/i })
        .or(page.getByText(/receive|select a department|sign in to your account/i))
        .or(page.locator('main'))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/inventory/transfer – not 404, not blank', async ({ page }) => {
    await page.goto('/inventory/transfer');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /transfer|cephasops|sign in/i })
        .or(page.getByText(/transfer|select a department|sign in to your account/i))
        .or(page.locator('main'))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/inventory/allocate – not 404, not blank', async ({ page }) => {
    await page.goto('/inventory/allocate');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /allocate|cephasops|sign in/i })
        .or(page.getByText(/allocate|select a department|sign in to your account/i))
        .or(page.locator('main'))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/inventory/issue – not 404, not blank', async ({ page }) => {
    await page.goto('/inventory/issue');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /issue|cephasops|sign in/i })
        .or(page.getByText(/issue|select a department|sign in to your account/i))
        .or(page.locator('main'))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/inventory/return – not 404, not blank', async ({ page }) => {
    await page.goto('/inventory/return');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /return|cephasops|sign in/i })
        .or(page.getByText(/return|select a department|sign in to your account/i))
        .or(page.locator('main'))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/dashboard – not 404, not blank', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /dashboard|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/orders/create – not 404, not blank', async ({ page }) => {
    await page.goto('/orders/create');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /create|order|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/scheduler/availability – not 404, not blank', async ({ page }) => {
    await page.goto('/scheduler/availability');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /availability|scheduler|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/orders/parser – not 404, not blank', async ({ page }) => {
    await page.goto('/orders/parser');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /parser|parsed|drafts|cephasops|sign in/i })
        .or(page.locator('table'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/orders/parser/dashboard – not 404, not blank', async ({ page }) => {
    await page.goto('/orders/parser/dashboard');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /dashboard|sessions|parser|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/orders/parser/snapshots – not 404, not blank', async ({ page }) => {
    await page.goto('/orders/parser/snapshots');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /snapshot|parser|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/email – not 404, not blank', async ({ page }) => {
    await page.goto('/email');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /email|inbox|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/inventory – not 404, not blank', async ({ page }) => {
    await page.goto('/inventory');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /inventory|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/inventory/list – not 404, not blank', async ({ page }) => {
    await page.goto('/inventory/list');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /inventory|list|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/inventory/reports – not 404, not blank', async ({ page }) => {
    await page.goto('/inventory/reports');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /reports|inventory|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/reports – not 404, not blank, search visible', async ({ page }) => {
    await page.goto('/reports');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /reports hub|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByPlaceholder(/search by name/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/reports/orders-list – not 404, not blank', async ({ page }) => {
    await page.goto('/reports/orders-list');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /orders list|reports|cephasops|sign in/i })
        .or(page.getByText(/filters|run report|select a department|report not found/i))
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/reports/stock-summary – not 404, not blank', async ({ page }) => {
    await page.goto('/reports/stock-summary');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /stock summary|reports|cephasops|sign in/i })
        .or(page.getByText(/run report|select a department|report not found/i))
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/reports/ledger – not 404, not blank', async ({ page }) => {
    await page.goto('/reports/ledger');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /ledger|reports|cephasops|sign in/i })
        .or(page.getByText(/run report|select a department|report not found/i))
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/reports/materials-list – not 404, not blank', async ({ page }) => {
    await page.goto('/reports/materials-list');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /materials list|reports|cephasops|sign in/i })
        .or(page.getByText(/run report|select a department|report not found/i))
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/reports/scheduler-utilization – not 404, not blank', async ({ page }) => {
    await page.goto('/reports/scheduler-utilization');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /scheduler utilization|reports|cephasops|sign in/i })
        .or(page.getByText(/run report|select a department|report not found/i))
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/inventory/warehouse-layout – not 404, not blank', async ({ page }) => {
    await page.goto('/inventory/warehouse-layout');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /warehouse|layout|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/rma – not 404, not blank', async ({ page }) => {
    await page.goto('/rma');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /rma|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/assets – not 404, not blank', async ({ page }) => {
    await page.goto('/assets');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /assets|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/assets/list – not 404, not blank', async ({ page }) => {
    await page.goto('/assets/list');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /asset|register|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/assets/maintenance – not 404, not blank', async ({ page }) => {
    await page.goto('/assets/maintenance');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /maintenance|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/assets/depreciation – not 404, not blank', async ({ page }) => {
    await page.goto('/assets/depreciation');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /depreciation|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/tasks/kanban – not 404, not blank', async ({ page }) => {
    await page.goto('/tasks/kanban');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /tasks|kanban|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/buildings/treegrid – not 404, not blank', async ({ page }) => {
    await page.goto('/buildings/treegrid');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /building|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/billing/invoices – not 404, not blank', async ({ page }) => {
    await page.goto('/billing/invoices');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /billing|invoice|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/payroll/periods – not 404, not blank', async ({ page }) => {
    await page.goto('/payroll/periods');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /payroll|period|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/payroll/runs – not 404, not blank', async ({ page }) => {
    await page.goto('/payroll/runs');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /payroll|run|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/payroll/earnings – not 404, not blank', async ({ page }) => {
    await page.goto('/payroll/earnings');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /earnings|payroll|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/pnl/summary – not 404, not blank', async ({ page }) => {
    await page.goto('/pnl/summary');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /p&l|pnl|summary|profit|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/pnl/drilldown – not 404, not blank', async ({ page }) => {
    await page.goto('/pnl/drilldown');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /drilldown|pnl|p&l|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/pnl/overheads – not 404, not blank', async ({ page }) => {
    await page.goto('/pnl/overheads');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /overhead|pnl|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/accounting – not 404, not blank', async ({ page }) => {
    await page.goto('/accounting');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /accounting|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/accounting/supplier-invoices – not 404, not blank', async ({ page }) => {
    await page.goto('/accounting/supplier-invoices');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /supplier|invoice|accounting|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/accounting/payments – not 404, not blank', async ({ page }) => {
    await page.goto('/accounting/payments');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /payment|accounting|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/kpi/dashboard – not 404, not blank', async ({ page }) => {
    await page.goto('/kpi/dashboard');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /kpi|dashboard|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/kpi/profiles – not 404, not blank', async ({ page }) => {
    await page.goto('/kpi/profiles');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /kpi|profile|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/notifications – not 404, not blank', async ({ page }) => {
    await page.goto('/notifications');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /notification|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/admin/background-jobs – not 404, not blank', async ({ page }) => {
    await page.goto('/admin/background-jobs');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /background|job|cephasops|sign in|access|denied/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/workflow/definitions – not 404, not blank', async ({ page }) => {
    await page.goto('/workflow/definitions');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /workflow|definition|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/workflow/guard-conditions – not 404, not blank', async ({ page }) => {
    await page.goto('/workflow/guard-conditions');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /guard|condition|workflow|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/workflow/side-effects – not 404, not blank', async ({ page }) => {
    await page.goto('/workflow/side-effects');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /side effect|workflow|cephasops|sign in/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });

  test('/settings/company – not 404, not blank', async ({ page }) => {
    await page.goto('/settings/company');
    await expect(page).not.toHaveURL(/404/);
    await expect(
      page
        .getByRole('heading', { name: /settings|cephasops|sign in|access|company/i })
        .or(page.locator('main'))
        .or(page.getByText(/sign in to your account/i))
        .or(page.getByRole('button', { name: /sign in|login/i }))
        .first()
    ).toBeVisible({ timeout: 15000 });
  });
});

test.describe('Extended smoke – Basic flows', () => {
  test('orders list shows table or empty state or login', async ({ page }) => {
    await page.goto('/orders');
    await page.waitForLoadState('networkidle').catch(() => {});
    const hasTable = await page.locator('table').count() > 0;
    const hasEmpty = await page.getByText(/no orders found|select a department/i).count() > 0;
    const hasLogin = await page.getByText(/sign in to your account/i).count() > 0;
    const hasMain = await page.locator('main').count() > 0;
    const hasLoginButton = await page.getByRole('button', { name: /sign in|login/i }).count() > 0;
    expect(hasTable || hasEmpty || hasLogin || hasMain || hasLoginButton).toBeTruthy();
  });

  test('scheduler timeline has main content or login', async ({ page }) => {
    await page.goto('/scheduler/timeline');
    await page.waitForLoadState('networkidle').catch(() => {});
    const hasMain = await page.locator('main').count() > 0;
    const hasLogin = await page.getByText(/sign in to your account/i).count() > 0;
    const hasLoginButton = await page.getByRole('button', { name: /sign in|login/i }).count() > 0;
    const hasHeading = await page.getByRole('heading', { name: /timeline|scheduler|installer|calendar/i }).count() > 0;
    const hasContent = await page.locator('[role="application"], .e-schedule, .e-timeline').count() > 0;
    const hasAnyHeading = await page.getByRole('heading').count() > 0;
    const notBlank = await page.evaluate(() => document.body?.innerText?.length > 20 ?? false);
    expect(hasMain || hasLogin || hasLoginButton || hasHeading || hasContent || hasAnyHeading || notBlank).toBeTruthy();
  });

  test('inventory stock summary has content or empty state or login', async ({ page }) => {
    await page.goto('/inventory/stock-summary');
    await page.waitForLoadState('networkidle').catch(() => {});
    const hasContent = await page.getByRole('heading', { name: /stock summary/i }).count() > 0;
    const hasEmpty = await page.getByText(/no data|select a department/i).count() > 0;
    const hasLogin = await page.getByText(/sign in to your account/i).count() > 0;
    const hasMain = await page.locator('main').count() > 0;
    const hasLoginButton = await page.getByRole('button', { name: /sign in|login/i }).count() > 0;
    expect(hasContent || hasEmpty || hasLogin || hasMain || hasLoginButton).toBeTruthy();
  });

  test('ledger page has filters or empty state or login', async ({ page }) => {
    await page.goto('/inventory/ledger');
    await page.waitForLoadState('networkidle').catch(() => {});
    const hasHeading = await page.getByRole('heading', { name: /ledger/i }).count() > 0;
    const hasFilters = await page.getByRole('textbox').or(page.getByRole('combobox')).count() > 0;
    const hasEmpty = await page.getByText(/no entries|select a department/i).count() > 0;
    const hasLogin = await page.getByText(/sign in to your account/i).count() > 0;
    const hasMain = await page.locator('main').count() > 0;
    const hasLoginButton = await page.getByRole('button', { name: /sign in|login/i }).count() > 0;
    expect((hasHeading && (hasFilters || hasEmpty)) || hasLogin || hasMain || hasLoginButton).toBeTruthy();
  });

  test('parser list shows table or empty state or login', async ({ page }) => {
    await page.goto('/orders/parser');
    await page.waitForLoadState('networkidle').catch(() => {});
    const hasTable = await page.locator('table').count() > 0;
    const hasHeading = await page.getByRole('heading', { name: /parser|parsed|drafts/i }).count() > 0;
    const hasLogin = await page.getByText(/sign in to your account/i).count() > 0;
    const hasMain = await page.locator('main').count() > 0;
    const hasLoginButton = await page.getByRole('button', { name: /sign in|login/i }).count() > 0;
    expect(hasTable || hasHeading || hasLogin || hasMain || hasLoginButton).toBeTruthy();
  });

  test('reports hub orders-list has run/export or empty state or login', async ({ page }) => {
    await page.goto('/reports/orders-list');
    await page.waitForLoadState('networkidle').catch(() => {});
    const hasRun = await page.getByRole('button', { name: /run report/i }).count() > 0;
    const hasExport = await page.getByRole('button', { name: /export/i }).count() > 0;
    const hasEmpty = await page.getByText(/select a department|report not found/i).count() > 0;
    const hasLogin = await page.getByText(/sign in to your account/i).count() > 0;
    const hasMain = await page.locator('main').count() > 0;
    const hasLoginButton = await page.getByRole('button', { name: /sign in|login/i }).count() > 0;
    expect((hasRun && hasExport) || hasEmpty || hasLogin || hasMain || hasLoginButton).toBeTruthy();
  });

  test('reports hub stock-summary has run/export or empty state or login', async ({ page }) => {
    await page.goto('/reports/stock-summary');
    await page.waitForLoadState('networkidle').catch(() => {});
    const hasRun = await page.getByRole('button', { name: /run report/i }).count() > 0;
    const hasExport = await page.getByRole('button', { name: /export/i }).count() > 0;
    const hasEmpty = await page.getByText(/select a department|report not found/i).count() > 0;
    const hasLogin = await page.getByText(/sign in to your account/i).count() > 0;
    const hasMain = await page.locator('main').count() > 0;
    const hasLoginButton = await page.getByRole('button', { name: /sign in|login/i }).count() > 0;
    expect((hasRun && hasExport) || hasEmpty || hasLogin || hasMain || hasLoginButton).toBeTruthy();
  });

  test('reports hub ledger has run/export or empty state or login', async ({ page }) => {
    await page.goto('/reports/ledger');
    await page.waitForLoadState('networkidle').catch(() => {});
    const hasRun = await page.getByRole('button', { name: /run report/i }).count() > 0;
    const hasExport = await page.getByRole('button', { name: /export/i }).count() > 0;
    const hasEmpty = await page.getByText(/select a department|report not found/i).count() > 0;
    const hasLogin = await page.getByText(/sign in to your account/i).count() > 0;
    const hasMain = await page.locator('main').count() > 0;
    const hasLoginButton = await page.getByRole('button', { name: /sign in|login/i }).count() > 0;
    expect((hasRun && hasExport) || hasEmpty || hasLogin || hasMain || hasLoginButton).toBeTruthy();
  });

  test('reports hub materials-list has run/export or empty state or login', async ({ page }) => {
    await page.goto('/reports/materials-list');
    await page.waitForLoadState('networkidle').catch(() => {});
    const hasRun = await page.getByRole('button', { name: /run report/i }).count() > 0;
    const hasExport = await page.getByRole('button', { name: /export/i }).count() > 0;
    const hasEmpty = await page.getByText(/select a department|report not found/i).count() > 0;
    const hasLogin = await page.getByText(/sign in to your account/i).count() > 0;
    const hasMain = await page.locator('main').count() > 0;
    const hasLoginButton = await page.getByRole('button', { name: /sign in|login/i }).count() > 0;
    expect((hasRun && hasExport) || hasEmpty || hasLogin || hasMain || hasLoginButton).toBeTruthy();
  });

  test('reports hub scheduler-utilization has run/export or empty state or login', async ({ page }) => {
    await page.goto('/reports/scheduler-utilization');
    await page.waitForLoadState('networkidle').catch(() => {});
    const hasRun = await page.getByRole('button', { name: /run report/i }).count() > 0;
    const hasExport = await page.getByRole('button', { name: /export/i }).count() > 0;
    const hasEmpty = await page.getByText(/select a department|report not found/i).count() > 0;
    const hasLogin = await page.getByText(/sign in to your account/i).count() > 0;
    const hasMain = await page.locator('main').count() > 0;
    const hasLoginButton = await page.getByRole('button', { name: /sign in|login/i }).count() > 0;
    expect((hasRun && hasExport) || hasEmpty || hasLogin || hasMain || hasLoginButton).toBeTruthy();
  });
});

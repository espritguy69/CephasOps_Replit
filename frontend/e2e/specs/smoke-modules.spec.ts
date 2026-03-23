/**
 * Authenticated key-module smoke tests. Run with smoke project (storageState).
 *
 * Tiers (for filtering; see SMOKE_BLUEPRINT.md):
 * - Core smoke:   P1 – dashboard, orders, scheduler timeline, core nav. Run with: --grep "Core smoke"
 * - Extended smoke: P2 – inventory, reports, settings.
 * - Future/nightly: P3 – admin, department selector. Optional or run in nightly.
 */
import { test, expect } from '@playwright/test';
import { expectAuthenticatedShell, expectAuthenticatedModuleVisible } from '../helpers/expectations';
import { ROUTES, TEST_IDS } from '../constants';

const MODULE_TIMEOUT = 15_000;

test.describe('Core smoke – P1 Authenticated modules', () => {
  test('dashboard: shell and operations dashboard visible', async ({ page }) => {
    await page.goto(ROUTES.DASHBOARD);
    await expectAuthenticatedShell(page, { timeout: MODULE_TIMEOUT });
    await expect(page.getByRole('heading', { name: /operations dashboard/i }).first()).toBeVisible({ timeout: MODULE_TIMEOUT });
  });

  test('orders: shell and orders module visible', async ({ page }) => {
    await expectAuthenticatedModuleVisible(page, {
      route: ROUTES.ORDERS,
      moduleTestId: TEST_IDS.ORDERS_PAGE_ROOT,
      timeout: MODULE_TIMEOUT,
    });
  });

  test('scheduler timeline: shell and timeline root visible', async ({ page }) => {
    await expectAuthenticatedModuleVisible(page, {
      route: ROUTES.SCHEDULER_TIMELINE,
      moduleTestId: TEST_IDS.SCHEDULER_TIMELINE_ROOT,
      timeout: MODULE_TIMEOUT,
    });
  });

  test('core nav: sidebar and user menu visible', async ({ page }) => {
    await page.goto(ROUTES.DASHBOARD);
    await expectAuthenticatedShell(page, { includeSidebar: true, timeout: MODULE_TIMEOUT });
  });
});

test.describe('Extended smoke – P2 Authenticated modules', () => {
  test('inventory stock summary: shell and module visible', async ({ page }) => {
    await expectAuthenticatedModuleVisible(page, {
      route: ROUTES.INVENTORY_STOCK_SUMMARY,
      moduleTestId: TEST_IDS.INVENTORY_STOCK_SUMMARY_ROOT,
      timeout: MODULE_TIMEOUT,
    });
  });

  test('reports hub: shell and module visible', async ({ page }) => {
    await expectAuthenticatedModuleVisible(page, {
      route: ROUTES.REPORTS,
      moduleTestId: TEST_IDS.REPORTS_HUB_ROOT,
      timeout: MODULE_TIMEOUT,
    });
  });

  test('settings company: shell and module visible', async ({ page }) => {
    await expectAuthenticatedModuleVisible(page, {
      route: ROUTES.SETTINGS_COMPANY,
      moduleTestId: TEST_IDS.SETTINGS_COMPANY_ROOT,
      timeout: MODULE_TIMEOUT,
    });
  });

  test('payroll periods: shell and module visible', async ({ page }) => {
    await expectAuthenticatedModuleVisible(page, {
      route: ROUTES.PAYROLL_PERIODS,
      moduleTestId: TEST_IDS.PAYROLL_PERIODS_ROOT,
      timeout: MODULE_TIMEOUT,
    });
  });

  test('P&L summary: shell and module visible', async ({ page }) => {
    await expectAuthenticatedModuleVisible(page, {
      route: ROUTES.PNL_SUMMARY,
      moduleTestId: TEST_IDS.PNL_SUMMARY_ROOT,
      timeout: MODULE_TIMEOUT,
    });
  });

  test('accounting dashboard: shell and module visible', async ({ page }) => {
    await expectAuthenticatedModuleVisible(page, {
      route: ROUTES.ACCOUNTING,
      moduleTestId: TEST_IDS.ACCOUNTING_DASHBOARD_ROOT,
      timeout: MODULE_TIMEOUT,
    });
  });
});

/** P3: optional / nightly; run with --grep "Future smoke" or exclude with --grep-invert "Future smoke" in CI. */
test.describe('Future smoke – P3 Admin & department', () => {
  test('admin background-jobs: page or access denied visible', async ({ page }) => {
    await page.goto(ROUTES.ADMIN_BACKGROUND_JOBS);
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: MODULE_TIMEOUT });
    await expect(
      page
        .getByRole('heading', { name: /background jobs/i })
        .or(page.getByRole('heading', { name: /access denied/i }))
        .first()
    ).toBeVisible({ timeout: MODULE_TIMEOUT });
  });

  test('department selector visible on desktop (dashboard)', async ({ page }) => {
    await page.goto(ROUTES.DASHBOARD);
    await expectAuthenticatedShell(page, { timeout: MODULE_TIMEOUT });
    await expect(page.getByTestId(TEST_IDS.DEPARTMENT_SELECTOR_TRIGGER)).toBeVisible({ timeout: 10_000 });
  });
});

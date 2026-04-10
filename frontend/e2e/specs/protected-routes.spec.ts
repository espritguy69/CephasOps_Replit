import { test, expect } from '@playwright/test';
import { hasAuthCredentials, loginViaUi } from '../helpers/auth';
import { expectAuthenticatedShell } from '../helpers/expectations';
import { TEST_IDS } from '../constants';

const TIMEOUT = 15_000;

const PROTECTED_ROUTES = [
  { path: '/orders', name: 'Orders' },
  { path: '/inventory', name: 'Inventory' },
  { path: '/inventory/stock-summary', name: 'Inventory Stock Summary' },
  { path: '/billing/invoices', name: 'Billing Invoices' },
  { path: '/reports', name: 'Reports Hub' },
  { path: '/scheduler/timeline', name: 'Scheduler Timeline' },
  { path: '/dashboard', name: 'Dashboard' },
  { path: '/payroll/periods', name: 'Payroll Periods' },
  { path: '/pnl/summary', name: 'P&L Summary' },
  { path: '/accounting', name: 'Accounting' },
  { path: '/settings/company', name: 'Settings Company' },
];

test.describe('Launch readiness – Protected routes (authenticated)', () => {
  test.beforeEach(async () => {
    if (!hasAuthCredentials()) test.skip();
  });

  for (const route of PROTECTED_ROUTES) {
    test(`${route.name} (${route.path}) loads with app shell, no forbidden`, async ({ page }) => {
      await loginViaUi(page);
      await expectAuthenticatedShell(page, { timeout: TIMEOUT });

      await page.goto(route.path);

      await expect(page).not.toHaveURL(/404/);

      await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });

      const forbidden = page.getByText(/403|forbidden|access denied|unauthorized|not authorized/i);
      const hasForbidden = await forbidden.isVisible({ timeout: 2000 }).catch(() => false);
      expect(hasForbidden).toBeFalsy();

      await expect(
        page.getByRole('heading').first()
          .or(page.locator('main'))
          .or(page.locator('table'))
          .or(page.locator('.e-grid'))
      ).toBeVisible({ timeout: TIMEOUT });
    });
  }
});

test.describe('Launch readiness – Protected routes (unauthenticated redirect)', () => {
  const CRITICAL_ROUTES = ['/orders', '/inventory', '/billing/invoices', '/reports', '/dashboard'];

  for (const path of CRITICAL_ROUTES) {
    test(`${path} redirects unauthenticated user to login`, async ({ browser }) => {
      const context = await browser.newContext({ storageState: { cookies: [], origins: [] } });
      const page = await context.newPage();

      await page.goto(path);
      await expect(page).toHaveURL(/\/login/, { timeout: TIMEOUT });

      await context.close();
    });
  }
});

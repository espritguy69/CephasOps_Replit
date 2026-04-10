import { test, expect } from '@playwright/test';
import { hasAuthCredentials, loginViaUi } from '../helpers/auth';
import { expectAuthenticatedShell } from '../helpers/expectations';
import { createAuthHeadersFromPage } from '../helpers/auth-api';
import { apiGet } from '../helpers/api';
import { TEST_IDS } from '../constants';

const TIMEOUT = 15_000;

const PROTECTED_ROUTES = [
  { path: '/orders', name: 'Orders', apiPath: '/orders' },
  { path: '/inventory', name: 'Inventory', apiPath: '/inventory/materials' },
  { path: '/inventory/stock-summary', name: 'Inventory Stock Summary', apiPath: '/inventory/stock' },
  { path: '/billing/invoices', name: 'Billing Invoices', apiPath: '/billing/invoices' },
  { path: '/reports', name: 'Reports Hub', apiPath: null },
  { path: '/scheduler/timeline', name: 'Scheduler Timeline', apiPath: null },
  { path: '/dashboard', name: 'Dashboard', apiPath: '/auth/me' },
  { path: '/payroll/periods', name: 'Payroll Periods', apiPath: null },
  { path: '/pnl/summary', name: 'P&L Summary', apiPath: null },
  { path: '/accounting', name: 'Accounting', apiPath: '/billing/payments' },
  { path: '/settings/company', name: 'Settings Company', apiPath: null },
];

test.describe('Launch readiness – Protected routes (authenticated)', () => {
  test.beforeEach(async () => {
    if (!hasAuthCredentials()) test.skip();
  });

  for (const route of PROTECTED_ROUTES) {
    test(`${route.name} (${route.path}) — UI loads, no forbidden, API no 500`, async ({ page, request }) => {
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

      if (route.apiPath) {
        const headers = await createAuthHeadersFromPage(page);
        const res = await apiGet(request, route.apiPath, headers);
        expect(res.status(), `API ${route.apiPath} returned 500`).not.toBe(500);
      }
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

test.describe('Launch readiness – Protected API endpoints (unauthenticated)', () => {
  const API_ENDPOINTS = ['/orders', '/billing/invoices', '/billing/payments', '/inventory/stock', '/auth/me'];

  for (const endpoint of API_ENDPOINTS) {
    test(`API ${endpoint} rejects unauthenticated request`, async ({ request }) => {
      const res = await apiGet(request, endpoint);
      expect([401, 403]).toContain(res.status());
    });
  }
});

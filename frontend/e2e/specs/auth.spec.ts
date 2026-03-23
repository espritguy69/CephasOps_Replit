import { test, expect } from '@playwright/test';
import { AppShell } from '../pages/AppShell';
import { hasAuthCredentials } from '../helpers/auth';
import { expectAuthenticatedShell, expectLoginPageVisible } from '../helpers/expectations';
import { ROUTES, SELECTORS } from '../constants';

/**
 * Auth flows that require authenticated storage state (run after setup).
 */
test.describe('E2E Auth – Authenticated', () => {
  test('dashboard loads after login', async ({ page }) => {
    await page.goto(ROUTES.DASHBOARD);
    await expectAuthenticatedShell(page);
  });

  test('main navigation is visible when authenticated', async ({ page }) => {
    await page.goto(ROUTES.DASHBOARD);
    await expectAuthenticatedShell(page, { includeSidebar: true });
  });

  test('logout returns to login page', async ({ page }) => {
    if (!hasAuthCredentials()) {
      test.skip();
      return;
    }
    await page.goto(ROUTES.DASHBOARD);
    await expectAuthenticatedShell(page);

    const appShell = new AppShell(page);
    await appShell.logout();

    await expect(page).toHaveURL(/\/login/, { timeout: 10_000 });
    await expectLoginPageVisible(page);
  });
});

import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/LoginPage';
import { AppShell } from '../pages/AppShell';
import { hasAuthCredentials, loginViaUi } from '../helpers/auth';
import { expectAuthenticatedShell, expectLoginPageVisible } from '../helpers/expectations';
import { ROUTES, TEST_IDS } from '../constants';

test.describe('Launch readiness – Auth login flow', () => {
  test.beforeEach(async () => {
    if (!hasAuthCredentials()) test.skip();
  });

  test('login with valid credentials shows authenticated shell', async ({ page }) => {
    await loginViaUi(page);

    await expect(page).not.toHaveURL(/\/login/, { timeout: 15_000 });

    await expectAuthenticatedShell(page, { timeout: 15_000 });
  });

  test('login form rejects invalid credentials', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.expectVisible();

    await loginPage.fillAndSubmit('invalid@nonexistent.test', 'WrongPassword999!');

    await expect(
      loginPage.errorAlert
        .or(page.getByText(/invalid|incorrect|failed|unauthorized|wrong/i))
        .first()
    ).toBeVisible({ timeout: 10_000 });

    await expect(page).toHaveURL(/\/login/);
  });

  test('authenticated user can access dashboard', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: 15_000 });

    await page.goto(ROUTES.DASHBOARD);
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: 15_000 });
    await expect(
      page.getByRole('heading', { name: /dashboard|operations/i }).first()
    ).toBeVisible({ timeout: 10_000 });
  });

  test('unauthenticated access to protected route redirects to login', async ({ browser }) => {
    const context = await browser.newContext({ storageState: { cookies: [], origins: [] } });
    const page = await context.newPage();

    await page.goto(ROUTES.DASHBOARD);
    await expect(page).toHaveURL(/\/login/, { timeout: 15_000 });
    await expectLoginPageVisible(page);

    await context.close();
  });

  test('logout returns to login and clears session', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: 15_000 });

    const appShell = new AppShell(page);
    await appShell.logout();

    await expect(page).toHaveURL(/\/login/, { timeout: 10_000 });
    await expectLoginPageVisible(page);

    await page.goto(ROUTES.ORDERS);
    await expect(page).toHaveURL(/\/login/, { timeout: 15_000 });
  });
});

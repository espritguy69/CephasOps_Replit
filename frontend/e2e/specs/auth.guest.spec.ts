import { test, expect } from '@playwright/test';
import { expectLoginPageVisible, expectAppShellOrLogin } from '../helpers/expectations';
import { LoginPage } from '../pages/LoginPage';
import { ROUTES, TEST_IDS, SELECTORS } from '../constants';

/**
 * Auth flows that run without authenticated storage (guest/unauthenticated).
 * This project must never use storageState or depend on setup.
 */
test.describe('E2E Auth – Guest (unauthenticated)', () => {
  test('login page renders and shows form', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.expectVisible();
    await expectLoginPageVisible(page);
  });

  test('homepage shows app shell or login', async ({ page }) => {
    await page.goto('/');
    await expectAppShellOrLogin(page);
  });

  test('visiting protected route without auth redirects to login', async ({ page }) => {
    await page.goto(ROUTES.PROTECTED_EXAMPLE);
    await expect(page).toHaveURL(/\/login/, { timeout: 15_000 });
  });

  test('visiting /orders without auth shows login or redirects', async ({ page }) => {
    await page.goto(ROUTES.ORDERS);
    const onLogin = page.url().includes('/login');
    const hasLoginForm = await page.getByRole('button', { name: SELECTORS.SIGN_IN_BUTTON }).isVisible().catch(() => false);
    expect(onLogin || hasLoginForm).toBeTruthy();
  });

  test('guest context has no auth: protected URL redirects to login', async ({ page }) => {
    await page.goto(ROUTES.DASHBOARD);
    await expect(page).toHaveURL(/\/login/, { timeout: 10_000 });
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).not.toBeVisible();
  });
});

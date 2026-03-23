import type { Page } from '@playwright/test';
import { expect } from '@playwright/test';
import { TEST_IDS, SELECTORS } from '../constants';

/**
 * Shared E2E expectations. Prefer getByRole and getByLabel; use data-testid from constants when needed.
 */

/** Visible: app shell (dashboard/heading) or login (sign in / button) */
export async function expectAppShellOrLogin(page: Page, options?: { timeout?: number }): Promise<void> {
  const timeout = options?.timeout ?? 15_000;
  await expect(
    page
      .getByRole('heading', { name: SELECTORS.CEPHASOPS_HEADING })
      .or(page.getByText(SELECTORS.SIGN_IN_TO_ACCOUNT_TEXT))
      .or(page.getByRole('button', { name: SELECTORS.SIGN_IN_BUTTON }))
      .first()
  ).toBeVisible({ timeout });
}

/** Login page shows form (email field or sign in button) */
export async function expectLoginPageVisible(page: Page, options?: { timeout?: number }): Promise<void> {
  const timeout = options?.timeout ?? 10_000;
  await expect(
    page.getByLabel(SELECTORS.LOGIN_EMAIL_LABEL).or(page.getByRole('button', { name: SELECTORS.SIGN_IN_BUTTON })).first()
  ).toBeVisible({ timeout });
}

/** Dashboard or main app content is visible (app-shell-main or heading) */
export async function expectDashboardOrAppVisible(page: Page, options?: { timeout?: number }): Promise<void> {
  const timeout = options?.timeout ?? 15_000;
  await expect(
    page
      .getByTestId(TEST_IDS.APP_SHELL_MAIN)
      .or(page.getByRole('heading', { name: SELECTORS.DASHBOARD_HEADING }))
      .first()
  ).toBeVisible({ timeout });
}

/**
 * Authenticated app shell is visible: main content + user menu (and optionally sidebar).
 * Use after login or on protected routes instead of relying only on URL.
 * Waits for all required elements in parallel to avoid stacking timeouts in CI.
 */
export async function expectAuthenticatedShell(
  page: Page,
  options?: { timeout?: number; includeSidebar?: boolean }
): Promise<void> {
  const timeout = options?.timeout ?? 15_000;
  const assertions: Promise<void>[] = [
    expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout }),
    expect(page.getByTestId(TEST_IDS.USER_MENU_TRIGGER)).toBeVisible({ timeout }),
  ];
  if (options?.includeSidebar) {
    assertions.push(expect(page.getByTestId(TEST_IDS.SIDEBAR)).toBeVisible({ timeout }));
  }
  await Promise.all(assertions);
}

/**
 * Navigate to an authenticated module route and assert shell + module root visible.
 * Use for stable, read-only module smoke tests. Ensures consistent goto → shell → module order.
 */
export async function expectAuthenticatedModuleVisible(
  page: Page,
  options: { route: string; moduleTestId: string; timeout?: number }
): Promise<void> {
  const timeout = options.timeout ?? 15_000;
  await page.goto(options.route);
  await expectAuthenticatedShell(page, { timeout });
  await expect(page.getByTestId(options.moduleTestId)).toBeVisible({ timeout });
}

/** Page is not 404 and at least one of the given or default selectors is visible */
export async function expectNot404OrBlank(
  page: Page,
  selectors?: { heading?: RegExp; text?: RegExp },
  options?: { timeout?: number }
): Promise<void> {
  const timeout = options?.timeout ?? 15_000;
  expect(page.url()).not.toMatch(/404/);
  const heading = selectors?.heading
    ? page.getByRole('heading', { name: selectors.heading })
    : page.getByRole('heading');
  await expect(
    heading
      .or(selectors?.text ? page.getByText(selectors.text) : page.getByTestId(TEST_IDS.APP_SHELL_MAIN))
      .or(page.getByText(SELECTORS.SIGN_IN_TO_ACCOUNT_TEXT))
      .or(page.getByRole('button', { name: SELECTORS.SIGN_IN_BUTTON }))
      .first()
  ).toBeVisible({ timeout });
}

/** At least one of heading, main (app-shell-main), or login is visible */
export async function expectPageContentVisible(page: Page, options?: { timeout?: number }): Promise<void> {
  const timeout = options?.timeout ?? 15_000;
  await expect(
    page.getByRole('heading').or(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).or(page.getByText(SELECTORS.SIGN_IN_TO_ACCOUNT_TEXT)).first()
  ).toBeVisible({ timeout });
}

/**
 * Visit a protected route as guest; expect redirect to login and no authenticated shell.
 */
export async function expectProtectedRouteRedirect(page: Page, path: string, options?: { timeout?: number }): Promise<void> {
  const timeout = options?.timeout ?? 15_000;
  await page.goto(path);
  await expect(page).toHaveURL(/\/login/, { timeout });
  await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).not.toBeVisible();
}

/**
 * Authenticated module page: shell visible and at least one of heading or testid visible.
 */
export async function expectModuleVisible(
  page: Page,
  options: { heading?: RegExp; testId?: string; timeout?: number }
): Promise<void> {
  const timeout = options.timeout ?? 15_000;
  await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout });
  if (options.heading) {
    await expect(page.getByRole('heading', { name: options.heading }).first()).toBeVisible({ timeout });
  }
  if (options.testId) {
    await expect(page.getByTestId(options.testId)).toBeVisible({ timeout });
  }
}

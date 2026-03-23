import { expect } from '@playwright/test';
import type { Page } from '@playwright/test';

/**
 * Navigate to app root. Use baseURL from config when calling page.goto('/').
 */
export async function gotoApp(page: Page, path = '/'): Promise<void> {
  await page.goto(path);
}

/**
 * Navigate to login page.
 */
export async function gotoLogin(page: Page): Promise<void> {
  await page.goto('/login');
}

/**
 * Assert current URL is login (or contains /login).
 */
export function expectLoginUrl(page: Page): void {
  expect(page.url()).toMatch(/\/login/);
}

/**
 * Assert current URL is inside app (dashboard or other app route, not login).
 */
export function expectAppUrl(page: Page): void {
  expect(page.url()).not.toMatch(/\/login$/);
  expect(page.url()).not.toMatch(/404/);
}

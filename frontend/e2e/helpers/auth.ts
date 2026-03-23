import type { Page } from '@playwright/test';
import { e2eEnv } from './env';

/**
 * Credentials for E2E login. From env only; never hardcoded.
 */
export function getTestCredentials(): { email: string; password: string } | null {
  const email = e2eEnv.email();
  const password = e2eEnv.password();
  if (!email || !password) return null;
  return { email, password };
}

/**
 * Perform UI login: go to /login, fill email/password, submit.
 * Expects login form with labels "Email" and "Password" and button "Sign in".
 */
export async function loginViaUi(
  page: Page,
  options?: { email?: string; password?: string }
): Promise<void> {
  const creds = options?.email && options?.password
    ? { email: options.email, password: options.password }
    : getTestCredentials();
  if (!creds) {
    throw new Error('E2E credentials not set. Set TEST_EMAIL and TEST_PASSWORD (or E2E_TEST_USER_EMAIL and E2E_TEST_USER_PASSWORD).');
  }
  await page.goto('/login');
  await page.getByLabel(/email/i).waitFor({ state: 'visible', timeout: 15_000 });
  await page.getByLabel(/email/i).fill(creds.email);
  await page.getByLabel(/password/i).fill(creds.password);
  await page.getByRole('button', { name: /sign in|login/i }).click();
}

/**
 * Whether authenticated E2E tests can run (credentials available).
 */
export function hasAuthCredentials(): boolean {
  return getTestCredentials() !== null;
}

import { test as base } from '@playwright/test';
import { LoginPage } from '../pages/LoginPage';
import { AppShell } from '../pages/AppShell';
import { getTestCredentials, hasAuthCredentials } from '../helpers/auth';
import { e2eEnv } from '../helpers/env';

type E2EFixtures = {
  loginPage: LoginPage;
  appShell: AppShell;
  /** Whether env has E2E credentials (no secrets exposed) */
  hasAuth: boolean;
  /** Base URL for the app */
  baseUrl: string;
};

/**
 * Extended fixtures for CephasOps E2E. Use when you need page objects or env in tests.
 */
export const test = base.extend<E2EFixtures>({
  baseUrl: async ({}, use) => {
    await use(e2eEnv.baseUrl());
  },
  hasAuth: async ({}, use) => {
    await use(hasAuthCredentials());
  },
  loginPage: async ({ page }, use) => {
    await use(new LoginPage(page));
  },
  appShell: async ({ page }, use) => {
    await use(new AppShell(page));
  },
});

export { expect } from '@playwright/test';
export { getTestCredentials, hasAuthCredentials } from '../helpers/auth';
export { e2eEnv } from '../helpers/env';

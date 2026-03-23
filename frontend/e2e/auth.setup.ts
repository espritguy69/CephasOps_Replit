import { test as setup } from '@playwright/test';
import * as fs from 'node:fs';
import * as path from 'node:path';
import { fileURLToPath } from 'node:url';
import { getTestCredentials } from './helpers/auth';
import { expectAuthenticatedShell } from './helpers/expectations';
import { ROUTES, SELECTORS } from './constants';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
/** Must match storageState path in playwright.config.ts (relative to frontend/) */
const AUTH_STORAGE_PATH = path.join(__dirname, '..', '.auth', 'user.json');

setup('authenticate for e2e', async ({ page }) => {
  const creds = getTestCredentials();

  if (!creds) {
    const dir = path.dirname(AUTH_STORAGE_PATH);
    if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
    fs.writeFileSync(AUTH_STORAGE_PATH, JSON.stringify({ cookies: [], origins: [] }));
    return;
  }

  await page.goto(ROUTES.LOGIN);
  await page.getByLabel(SELECTORS.LOGIN_EMAIL_LABEL).waitFor({ state: 'visible', timeout: 60_000 });
  await page.getByLabel(SELECTORS.LOGIN_EMAIL_LABEL).fill(creds.email);
  await page.getByLabel(SELECTORS.LOGIN_PASSWORD_LABEL).fill(creds.password);
  await page.getByRole('button', { name: SELECTORS.SIGN_IN_BUTTON }).click();

  await expectAuthenticatedShell(page, { timeout: 15_000 });
  await page.context().storageState({ path: AUTH_STORAGE_PATH });
});

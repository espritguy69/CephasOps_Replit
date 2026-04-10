import { defineConfig, devices } from '@playwright/test';
import path from 'node:path';
import fs from 'node:fs';
import { fileURLToPath } from 'node:url';
import { execSync } from 'node:child_process';
import dotenv from 'dotenv';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
dotenv.config({ path: path.join(__dirname, '.env') });

if (process.platform === 'linux' && !process.env.CI) {
  const cacheFile = path.join(__dirname, '.playwright-libgbm-path');
  let gbmDir = '';
  if (fs.existsSync(cacheFile)) {
    gbmDir = fs.readFileSync(cacheFile, 'utf8').trim();
    if (!fs.existsSync(path.join(gbmDir, 'libgbm.so.1'))) gbmDir = '';
  }
  if (!gbmDir) {
    try {
      const result = execSync(
        'ls /nix/store/*mesa*gbm*/lib/libgbm.so.1 2>/dev/null | head -1',
        { encoding: 'utf8', timeout: 10000 }
      ).trim();
      if (result) {
        gbmDir = path.dirname(result);
        fs.writeFileSync(cacheFile, gbmDir);
      }
    } catch { /* CI or non-Nix: Chromium deps installed via apt */ }
  }
  if (gbmDir) {
    process.env.LD_LIBRARY_PATH = `${gbmDir}:${process.env.LD_LIBRARY_PATH ?? ''}`;
  }
}

const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost:5000';
const AUTH_STORAGE_PATH = '.auth/user.json';

export default defineConfig({
  testDir: './e2e',
  outputDir: 'test-results',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [['html', { outputFolder: 'playwright-report', open: 'never' }], ['list']],
  use: {
    baseURL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    // Guest: explicitly unauthenticated (empty storage state).
    {
      name: 'guest',
      testMatch: /auth\.guest\.spec\.ts|health\.spec\.ts/,
      use: { ...devices['Desktop Chrome'], storageState: { cookies: [], origins: [] } },
    },
    // Setup: generates .auth/user.json for authenticated projects.
    { name: 'setup', testMatch: /auth\.setup\.ts/, timeout: 90_000, use: { ...devices['Desktop Chrome'] } },
    // Authenticated: depend on setup and use saved storage state.
    {
      name: 'smoke',
      use: { ...devices['Desktop Chrome'], storageState: AUTH_STORAGE_PATH },
      dependencies: ['setup'],
      // Explicit list: run both smoke spec files. Use --grep "Core smoke" for minimal must-run set.
      testMatch: [/smoke\.spec\.ts/, /smoke-modules\.spec\.ts/],
    },
    {
      name: 'auth',
      use: { ...devices['Desktop Chrome'], storageState: AUTH_STORAGE_PATH },
      dependencies: ['setup'],
      testMatch: /auth\.spec\.ts/,
    },
    {
      name: 'auth-flow',
      use: { ...devices['Desktop Chrome'] },
      testMatch: /auth\.login\.spec\.ts/,
    },
    {
      name: 'tenant',
      use: { ...devices['Desktop Chrome'] },
      testMatch: /tenant-isolation\.spec\.ts/,
    },
    {
      name: 'orders',
      use: { ...devices['Desktop Chrome'] },
      testMatch: /order-lifecycle\.spec\.ts|order-inventory\.spec\.ts/,
    },
    {
      name: 'billing',
      use: { ...devices['Desktop Chrome'] },
      testMatch: /billing-flow\.spec\.ts/,
    },
    {
      name: 'protected',
      use: { ...devices['Desktop Chrome'] },
      testMatch: /protected-routes\.spec\.ts/,
    },
    {
      name: 'launch-readiness',
      use: { ...devices['Desktop Chrome'] },
      testMatch: /auth\.login\.spec\.ts|tenant-isolation\.spec\.ts|order-lifecycle\.spec\.ts|order-inventory\.spec\.ts|billing-flow\.spec\.ts|protected-routes\.spec\.ts/,
    },
    {
      name: 'edge-cases',
      use: { ...devices['Desktop Chrome'] },
      testMatch: /edge-cases\.spec\.ts/,
    },
    {
      name: 'idempotency',
      use: { ...devices['Desktop Chrome'] },
      testMatch: /idempotency\.spec\.ts/,
    },
    {
      name: 'financial',
      use: { ...devices['Desktop Chrome'] },
      testMatch: /financial-reconciliation\.spec\.ts/,
    },
    {
      name: 'load',
      use: { ...devices['Desktop Chrome'] },
      testMatch: /load\.spec\.ts/,
    },
    {
      name: 'business-day',
      use: { ...devices['Desktop Chrome'] },
      testMatch: /business-day\.spec\.ts/,
    },
    {
      name: 'api-contracts',
      use: { ...devices['Desktop Chrome'] },
      testMatch: /api-contract\.spec\.ts/,
    },
    {
      name: 'security',
      use: { ...devices['Desktop Chrome'] },
      testMatch: /security\.spec\.ts/,
    },
    {
      name: 'scheduler',
      use: { ...devices['Desktop Chrome'] },
      testMatch: /scheduler\.spec\.ts/,
    },
    {
      name: 'system-validation',
      use: { ...devices['Desktop Chrome'] },
      testMatch: /auth\.login\.spec\.ts|tenant-isolation\.spec\.ts|order-lifecycle\.spec\.ts|order-inventory\.spec\.ts|billing-flow\.spec\.ts|protected-routes\.spec\.ts|edge-cases\.spec\.ts|idempotency\.spec\.ts|financial-reconciliation\.spec\.ts|load\.spec\.ts|business-day\.spec\.ts|api-contract\.spec\.ts|security\.spec\.ts|scheduler\.spec\.ts/,
    },
  ],
  webServer: {
    command: 'npm run dev',
    url: baseURL,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
});

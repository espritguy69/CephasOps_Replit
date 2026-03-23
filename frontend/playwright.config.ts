import { defineConfig, devices } from '@playwright/test';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import dotenv from 'dotenv';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
dotenv.config({ path: path.join(__dirname, '.env') });

const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost:5173';
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
  ],
  webServer: {
    command: 'npm run dev',
    url: baseURL,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
});

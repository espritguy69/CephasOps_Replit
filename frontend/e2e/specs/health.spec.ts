import { test, expect } from '@playwright/test';
import { ROUTES } from '../constants';
import { expectLoginPageVisible } from '../helpers/expectations';

/**
 * Minimal health-check: app boot and landing respond. Runs in guest context (no auth).
 */
test.describe('E2E Health – App boot', () => {
  test('frontend root returns 200 with text/html', async ({ request }) => {
    const res = await request.get('/');
    expect(res.status()).toBe(200);
    const contentType = res.headers()['content-type'] ?? '';
    expect(contentType.toLowerCase()).toMatch(/text\/html/);
    const text = await res.text();
    expect(text.length).toBeGreaterThan(100);
  });

  test('login page loads and form is visible', async ({ page }) => {
    await page.goto(ROUTES.LOGIN);
    await expect(page).not.toHaveURL(/404/);
    await expectLoginPageVisible(page, { timeout: 15_000 });
  });
});

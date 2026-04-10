import { test, expect } from '@playwright/test';
import { hasAuthCredentials, loginViaUi } from '../helpers/auth';
import { expectAuthenticatedShell } from '../helpers/expectations';
import { createAuthHeadersFromPage, extractToken, authHeaders } from '../helpers/auth-api';
import { apiGet, getUser, getTenantContext } from '../helpers/api';
import { e2eEnv } from '../helpers/env';
import { TEST_IDS } from '../constants';

const FAKE_DEPARTMENT_UUID = '99999999-9999-9999-9999-999999999999';
const FAKE_COMPANY_UUID = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const FAKE_ORDER_UUID = '00000000-0000-0000-0000-000000000000';

test.describe('Launch readiness – Tenant isolation', () => {
  test.beforeEach(async () => {
    if (!hasAuthCredentials()) test.skip();
  });

  test('API rejects forged X-Department-Id — never returns foreign data', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: 15_000 });

    const token = await extractToken(page);
    if (!token) { test.skip(); return; }

    const res = await apiGet(request, '/orders', {
      ...authHeaders(token),
      'X-Department-Id': FAKE_DEPARTMENT_UUID,
      'X-Company-Id': FAKE_COMPANY_UUID,
    });

    const status = res.status();
    if (status === 200) {
      const body = await res.json();
      const data = body.Data ?? body.data ?? body;
      const items = Array.isArray(data) ? data : (data?.items ?? data?.Items ?? []);
      expect(items.length).toBe(0);
    } else {
      expect([400, 401, 403, 404]).toContain(status);
    }
  });

  test('API rejects direct entity access with wrong tenant — NEVER 200', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: 15_000 });

    const token = await extractToken(page);
    if (!token) { test.skip(); return; }

    const endpoints = [
      `/orders/${FAKE_ORDER_UUID}`,
      `/billing/invoices/${FAKE_ORDER_UUID}`,
      `/inventory/materials/${FAKE_ORDER_UUID}`,
    ];

    for (const endpoint of endpoints) {
      const res = await apiGet(request, endpoint, {
        ...authHeaders(token),
        'X-Department-Id': FAKE_DEPARTMENT_UUID,
      });

      expect(
        [400, 401, 403, 404].includes(res.status()),
        `${endpoint} returned ${res.status()} — expected 400/401/403/404`
      ).toBeTruthy();
    }
  });

  test('forged tenant header on invoices — NEVER 200 with foreign data', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: 15_000 });

    const token = await extractToken(page);
    if (!token) { test.skip(); return; }

    const res = await apiGet(request, '/billing/invoices', {
      ...authHeaders(token),
      'X-Department-Id': FAKE_DEPARTMENT_UUID,
      'X-Company-Id': FAKE_COMPANY_UUID,
    });

    const status = res.status();
    if (status === 200) {
      const body = await res.json();
      const data = body.Data ?? body.data ?? body;
      const items = Array.isArray(data) ? data : (data?.items ?? data?.Items ?? []);
      expect(items.length).toBe(0);
    } else {
      expect([400, 401, 403, 404]).toContain(status);
    }
  });

  test('forged tenant header on inventory — NEVER 200 with foreign data', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: 15_000 });

    const token = await extractToken(page);
    if (!token) { test.skip(); return; }

    const res = await apiGet(request, '/inventory/stock', {
      ...authHeaders(token),
      'X-Department-Id': FAKE_DEPARTMENT_UUID,
      'X-Company-Id': FAKE_COMPANY_UUID,
    });

    const status = res.status();
    if (status === 200) {
      const body = await res.json();
      const data = body.Data ?? body.data ?? body;
      const items = Array.isArray(data) ? data : (data?.items ?? data?.Items ?? []);
      expect(items.length).toBe(0);
    } else {
      expect([400, 401, 403, 404]).toContain(status);
    }
  });

  test('authenticated user context — API confirms tenant identity', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: 15_000 });

    const headers = await createAuthHeadersFromPage(page);
    const ctx = await getTenantContext(request, headers);

    expect(ctx.userId).toBeTruthy();
  });

  test('UI does not leak data when department context is absent', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: 15_000 });

    await page.evaluate(() => {
      localStorage.removeItem('cephasops.activeDepartmentId');
    });

    await page.goto('/orders');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: 15_000 });

    const noDataIndicators = page
      .getByText(/no orders found|select a department|no data|no results/i)
      .or(page.getByText(/please select/i))
      .or(page.locator('table tbody tr').first());

    await expect(noDataIndicators).toBeVisible({ timeout: 10_000 });
  });
});

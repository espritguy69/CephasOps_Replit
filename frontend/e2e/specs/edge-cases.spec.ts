import { test, expect } from '@playwright/test';
import { hasAuthCredentials, loginViaUi } from '../helpers/auth';
import { expectAuthenticatedShell } from '../helpers/expectations';
import { createAuthHeadersFromPage, extractToken, authHeaders } from '../helpers/auth-api';
import { apiPost, apiGet, apiPut } from '../helpers/api';
import { TEST_IDS } from '../constants';

const TIMEOUT = 15_000;

async function waitForNetworkIdle(page: import('@playwright/test').Page): Promise<void> {
  await page.waitForLoadState('networkidle', { timeout: TIMEOUT }).catch(() => {});
}

test.describe('System validation – Edge cases & negative tests', () => {
  test.beforeEach(async () => {
    if (!hasAuthCredentials()) test.skip();
  });

  test('create order without required fields — API rejects with validation error', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);

    const res = await apiPost(request, '/orders', {}, headers);
    expect([400, 422]).toContain(res.status());
  });

  test('create order with empty body — API rejects, not 500', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);

    const res = await apiPost(request, '/orders', null, headers);
    expect(res.status()).not.toBe(500);
    expect([400, 415, 422]).toContain(res.status());
  });

  test('create invoice with negative amount — API rejects', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);

    const res = await apiPost(request, '/billing/invoices', {
      partnerId: '00000000-0000-0000-0000-000000000000',
      invoiceDate: new Date().toISOString(),
      lineItems: [
        { description: 'E2E Negative Test', quantity: 1, unitPrice: -100 },
      ],
    }, headers);

    expect(res.status()).not.toBe(201);
    expect(res.status()).not.toBe(500);
  });

  test('create invoice with zero amount — API handles gracefully', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);

    const res = await apiPost(request, '/billing/invoices', {
      partnerId: '00000000-0000-0000-0000-000000000000',
      invoiceDate: new Date().toISOString(),
      lineItems: [
        { description: 'E2E Zero Amount', quantity: 0, unitPrice: 0 },
      ],
    }, headers);

    expect(res.status()).not.toBe(500);
  });

  test('unauthorized API access — returns 401/403, never 200', async ({ request }) => {
    const endpoints = [
      '/orders',
      '/billing/invoices',
      '/billing/payments',
      '/inventory/stock',
      '/inventory/materials',
      '/auth/me',
    ];

    for (const endpoint of endpoints) {
      const res = await apiGet(request, endpoint);
      expect(
        [401, 403].includes(res.status()),
        `${endpoint} returned ${res.status()} without auth — expected 401/403`
      ).toBeTruthy();
    }
  });

  test('invalid status transition — API rejects', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);

    const res = await apiPost(request, '/orders/00000000-0000-0000-0000-000000000000/status', {
      status: 'INVALID_STATUS_THAT_DOES_NOT_EXIST',
    }, headers);

    expect([400, 404, 422]).toContain(res.status());
  });

  test('access non-existent order — API returns 404, not 500', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);

    const res = await apiGet(request, '/orders/00000000-0000-0000-0000-000000000000', headers);
    expect([400, 404]).toContain(res.status());
  });

  test('access non-existent invoice — API returns 404, not 500', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);

    const res = await apiGet(request, '/billing/invoices/00000000-0000-0000-0000-000000000000', headers);
    expect([400, 404]).toContain(res.status());
  });

  test('use material beyond available stock — API rejects with error', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);

    const res = await apiPost(request, '/inventory/stock/movements', {
      materialId: '00000000-0000-0000-0000-000000000000',
      locationId: '00000000-0000-0000-0000-000000000000',
      movementType: 'Issue',
      quantity: 999999,
      remarks: 'E2E: attempt to issue beyond stock',
    }, headers);

    expect(res.status()).not.toBe(201);
    expect(res.status()).not.toBe(200);
  });

  test('stock movement with negative quantity — API rejects', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);

    const res = await apiPost(request, '/inventory/stock/movements', {
      materialId: '00000000-0000-0000-0000-000000000000',
      locationId: '00000000-0000-0000-0000-000000000000',
      movementType: 'Issue',
      quantity: -10,
      remarks: 'E2E: negative quantity test',
    }, headers);

    expect(res.status()).not.toBe(200);
    expect(res.status()).not.toBe(201);
  });

  test('UI — submit order form without required fields shows validation error', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/orders');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });
    await waitForNetworkIdle(page);

    const createBtn = page
      .getByRole('button', { name: /new order|create order|add order/i })
      .or(page.getByTestId('create-order-button'))
      .first();

    if (!(await createBtn.isVisible({ timeout: 5000 }).catch(() => false))) {
      test.skip();
      return;
    }

    await createBtn.click();
    await expect(
      page.getByRole('heading', { name: /create order/i })
        .or(page.getByRole('dialog'))
        .first()
    ).toBeVisible({ timeout: TIMEOUT });

    const submitBtn = page
      .getByRole('button', { name: /create order|save|submit/i })
      .last();

    if (await submitBtn.isVisible({ timeout: 3000 }).catch(() => false)) {
      await submitBtn.click();

      await expect(
        page.getByText(/required|cannot be empty|please fill|validation|error|failed/i)
          .or(page.getByRole('alert'))
          .or(page.locator('[aria-invalid="true"]'))
          .first()
      ).toBeVisible({ timeout: TIMEOUT });
    }
  });
});

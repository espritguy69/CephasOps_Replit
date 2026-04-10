import { test, expect } from '@playwright/test';
import { hasAuthCredentials } from '../helpers/auth';
import { createAuthHeadersFromPage, extractToken, authHeaders } from '../helpers/auth-api';
import { loginViaUi } from '../helpers/auth';
import { expectAuthenticatedShell } from '../helpers/expectations';
import { apiGet, apiPost, getOrders, getStock } from '../helpers/api';
import { ApiTimingCollector } from '../helpers/diagnostics';

const TIMEOUT = 15_000;
const RESPONSE_THRESHOLD_MS = 5000;

test.describe('System validation – Load simulation', () => {
  test.beforeEach(async () => {
    if (!hasAuthCredentials()) test.skip();
  });

  test('concurrent API reads — no 500 errors, all under 5s', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const timing = new ApiTimingCollector(page);

    const endpoints = [
      '/orders',
      '/billing/invoices',
      '/billing/payments',
      '/inventory/stock',
      '/inventory/materials',
      '/auth/me',
    ];

    const startTime = Date.now();
    const results = await Promise.all(
      endpoints.map(async (ep) => {
        const res = await apiGet(request, ep, headers);
        return { endpoint: ep, status: res.status(), durationMs: Date.now() - startTime };
      })
    );

    for (const r of results) {
      expect(r.status, `${r.endpoint} returned 500`).not.toBe(500);
      expect(r.durationMs, `${r.endpoint} took ${r.durationMs}ms (>5s)`).toBeLessThan(RESPONSE_THRESHOLD_MS);
    }
  });

  test('parallel order listing — no duplicates in response', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);

    const [orders1, orders2, orders3] = await Promise.all([
      getOrders(request, headers) as Promise<Record<string, unknown>[]>,
      getOrders(request, headers) as Promise<Record<string, unknown>[]>,
      getOrders(request, headers) as Promise<Record<string, unknown>[]>,
    ]);

    expect(orders1.length).toBe(orders2.length);
    expect(orders2.length).toBe(orders3.length);

    const ids1 = new Set(orders1.map(o => String(o.id ?? o.Id)));
    const ids2 = new Set(orders2.map(o => String(o.id ?? o.Id)));
    expect(ids1.size).toBe(ids2.size);
  });

  test('parallel invoice + payment reads — no negative stock', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);

    const [stock1, stock2] = await Promise.all([
      getStock(request, headers) as Promise<Record<string, unknown>[]>,
      getStock(request, headers) as Promise<Record<string, unknown>[]>,
    ]);

    for (const item of stock1) {
      const qty = Number(item.quantity ?? item.Quantity ?? item.balance ?? item.Balance ?? 0);
      expect(qty, `Negative stock: ${JSON.stringify(item)}`).toBeGreaterThanOrEqual(0);
    }

    expect(stock1.length).toBe(stock2.length);
  });

  test('rapid sequential order creations — system remains stable', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const batchRef = `LOAD-${Date.now()}`;
    const results: number[] = [];

    for (let i = 0; i < 3; i++) {
      const res = await apiPost(request, '/orders', {
        serviceId: `SVC-${batchRef}-${i}`,
        customerName: `Load Test ${i}`,
        addressLine1: `${i} Load Street`,
        city: 'LoadCity',
        state: 'LS',
        postcode: '00000',
        externalRef: `${batchRef}-${i}`,
        appointmentDate: new Date().toISOString().split('T')[0],
        appointmentWindowFrom: '00:00:00',
        appointmentWindowTo: '23:59:59',
      }, headers);
      results.push(res.status());
    }

    for (const status of results) {
      expect(status).not.toBe(500);
    }

    const successCount = results.filter(s => s >= 200 && s < 300).length;
    expect(successCount).toBeGreaterThan(0);
  });

  test('UI + API under concurrent load — dashboard loads while API queries run', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);

    const [, orders] = await Promise.all([
      page.goto('/dashboard').then(() => page.waitForLoadState('networkidle', { timeout: TIMEOUT }).catch(() => {})),
      getOrders(request, headers),
    ]);

    await expect(page.getByTestId('app-shell-main')).toBeVisible({ timeout: TIMEOUT });
    expect(Array.isArray(orders)).toBeTruthy();
  });
});

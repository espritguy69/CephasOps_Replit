import { test, expect } from '../helpers/fixtures';
import { hasAuthCredentials, loginViaUi } from '../helpers/auth';
import { createAuthHeadersFromPage } from '../helpers/auth-api';
import { expectAuthenticatedShell } from '../helpers/expectations';
import { apiGet, apiPost, getOrders, getStock } from '../helpers/api';

const TIMEOUT = 15_000;
const RESPONSE_THRESHOLD_MS = 5000;

test.describe('System validation – Load simulation', () => {
  test.beforeEach(async () => {
    if (!hasAuthCredentials()) test.skip();
  });

  test('concurrent API reads — no 500 errors, all under 5s', async ({ page, request, diagnostics, apiTiming }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);

    const endpoints = [
      '/orders',
      '/billing/invoices',
      '/billing/payments',
      '/inventory/stock',
      '/inventory/materials',
      '/auth/me',
    ];

    const results = await Promise.all(
      endpoints.map(async (ep) => {
        const start = Date.now();
        const res = await apiGet(request, ep, headers);
        return { endpoint: ep, status: res.status(), durationMs: Date.now() - start };
      })
    );

    for (const r of results) {
      expect(r.status, `${r.endpoint} returned 500`).not.toBe(500);
      expect(r.durationMs, `${r.endpoint} took ${r.durationMs}ms (>5s)`).toBeLessThan(RESPONSE_THRESHOLD_MS);
    }
  });

  test('parallel order listing — no duplicates within response', async ({ page, request, diagnostics }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);

    const [orders1, orders2, orders3] = await Promise.all([
      getOrders(request, headers) as Promise<Record<string, unknown>[]>,
      getOrders(request, headers) as Promise<Record<string, unknown>[]>,
      getOrders(request, headers) as Promise<Record<string, unknown>[]>,
    ]);

    for (const orders of [orders1, orders2, orders3]) {
      const ids = orders.map(o => String(o.id ?? o.Id));
      const uniqueIds = new Set(ids);
      expect(uniqueIds.size, `Duplicate IDs detected within single response: expected ${ids.length} unique but got ${uniqueIds.size}`).toBe(ids.length);
    }

    expect(orders1.length).toBe(orders2.length);
    expect(orders2.length).toBe(orders3.length);
  });

  test('parallel stock reads — no negative stock balances', async ({ page, request, diagnostics }) => {
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

  test('rapid sequential order creations — system remains stable', async ({ page, request, diagnostics }) => {
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

    expect(diagnostics.hasErrors(), `System errors during load: ${diagnostics.getSummary()}`).toBeFalsy();
  });

  test('UI + API under concurrent load — dashboard loads while API queries run', async ({ page, request, diagnostics }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);

    const [, orders] = await Promise.all([
      page.goto('/dashboard').then(() => page.waitForLoadState('networkidle', { timeout: TIMEOUT }).catch(() => {})),
      getOrders(request, headers),
    ]);

    await expect(page.getByTestId('app-shell-main')).toBeVisible({ timeout: TIMEOUT });
    expect(Array.isArray(orders)).toBeTruthy();
    expect(diagnostics.hasErrors(), `Errors during concurrent load: ${diagnostics.getSummary()}`).toBeFalsy();
  });
});

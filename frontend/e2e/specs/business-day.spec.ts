import { test, expect } from '../helpers/fixtures';
import { hasAuthCredentials, loginViaUi } from '../helpers/auth';
import { expectAuthenticatedShell } from '../helpers/expectations';
import { createAuthHeadersFromPage } from '../helpers/auth-api';
import {
  apiGet, apiPost, getOrders, getInvoices, getPayments, getStock,
} from '../helpers/api';
import { captureSnapshot, diffSnapshots, type DataSnapshot } from '../helpers/snapshot';

const TIMEOUT = 15_000;

test.describe('System validation – Business day simulation', () => {
  test.describe.configure({ mode: 'serial' });

  test.beforeEach(async () => {
    if (!hasAuthCredentials()) test.skip();
  });

  const batchRef = `BIZDAY-${Date.now()}`;
  let createdOrderIds: string[] = [];
  let baselineSnapshot: DataSnapshot | null = null;

  test('capture baseline snapshot', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    baselineSnapshot = await captureSnapshot(request, headers);

    expect(baselineSnapshot.capturedAt).toBeGreaterThan(0);
    expect(baselineSnapshot.orderCount).toBeGreaterThanOrEqual(0);
  });

  test('create batch of orders — simulating morning intake', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);

    for (let i = 0; i < 3; i++) {
      const res = await apiPost(request, '/orders', {
        serviceId: `SVC-${batchRef}-${i}`,
        customerName: `BizDay Customer ${i}`,
        addressLine1: `${100 + i} Business Avenue`,
        city: 'BusinessCity',
        state: 'BC',
        postcode: '55555',
        externalRef: `${batchRef}-${i}`,
        appointmentDate: new Date().toISOString().split('T')[0],
        appointmentWindowFrom: '08:00:00',
        appointmentWindowTo: '17:00:00',
      }, headers);

      if (res.ok()) {
        const body = await res.json();
        const data = body.Data ?? body.data ?? body;
        const id = String((data as Record<string, unknown>).id ?? (data as Record<string, unknown>).Id ?? '');
        if (id) createdOrderIds.push(id);
      }
    }

    expect(createdOrderIds.length).toBeGreaterThan(0);
  });

  test('verify all orders exist via API', async ({ page, request }) => {
    if (createdOrderIds.length === 0) test.skip();

    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const allOrders = await getOrders(request, headers) as Record<string, unknown>[];

    for (const orderId of createdOrderIds) {
      const found = allOrders.find(o => String(o.id ?? o.Id) === orderId);
      expect(found, `Order ${orderId} not found in API response`).toBeTruthy();
    }
  });

  test('financial state — payments match invoices, no negative values', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const invoices = await getInvoices(request, headers) as Record<string, unknown>[];
    const payments = await getPayments(request, headers) as Record<string, unknown>[];

    for (const inv of invoices) {
      const total = Number(inv.totalAmount ?? inv.TotalAmount ?? inv.total ?? inv.Total ?? 0);
      expect(total, `Invoice ${inv.id ?? inv.Id} has negative total`).toBeGreaterThanOrEqual(0);
    }

    for (const pmt of payments) {
      const amount = Number(pmt.amount ?? pmt.Amount ?? 0);
      expect(amount, `Payment ${pmt.id ?? pmt.Id} has negative amount`).toBeGreaterThanOrEqual(0);
    }

    const paidInvoiceIds = new Set(
      payments
        .map(p => String((p as Record<string, unknown>).invoiceId ?? (p as Record<string, unknown>).InvoiceId ?? ''))
        .filter(id => id && id !== 'undefined')
    );

    for (const invId of paidInvoiceIds) {
      const invoiceExists = invoices.find(i => String(i.id ?? i.Id) === invId);
      expect(invoiceExists, `Payment references invoice ${invId} that does not exist — orphan record`).toBeTruthy();
    }
  });

  test('inventory integrity — no negative stock balances', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const stock = await getStock(request, headers) as Record<string, unknown>[];

    for (const item of stock) {
      const qty = Number(item.quantity ?? item.Quantity ?? item.balance ?? item.Balance ?? 0);
      expect(qty, `Negative stock for item ${item.id ?? item.Id}`).toBeGreaterThanOrEqual(0);
    }
  });

  test('end-of-day snapshot diff — only expected changes, no unexpected deletions', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const endSnapshot = await captureSnapshot(request, headers);

    if (baselineSnapshot) {
      const diff = diffSnapshots(baselineSnapshot, endSnapshot);

      expect(diff.unexpectedDeletions.length, `Unexpected deletions detected: ${diff.unexpectedDeletions.join(', ')}`).toBe(0);

      expect(diff.ordersAdded).toBeGreaterThanOrEqual(createdOrderIds.length);

      expect(diff.ordersRemoved, 'Orders were unexpectedly removed during business day').toBe(0);
      expect(diff.invoicesRemoved, 'Invoices were unexpectedly removed during business day').toBe(0);
      expect(diff.paymentsRemoved, 'Payments were unexpectedly removed during business day').toBe(0);
    } else {
      expect(endSnapshot.orderCount).toBeGreaterThanOrEqual(createdOrderIds.length);
    }
  });

  test('system stability — all API endpoints respond after business day', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);

    const endpoints = ['/orders', '/billing/invoices', '/billing/payments', '/inventory/stock', '/auth/me'];
    for (const ep of endpoints) {
      const res = await apiGet(request, ep, headers);
      expect(res.status(), `${ep} returned ${res.status()} — system unstable`).not.toBe(500);
    }
  });
});

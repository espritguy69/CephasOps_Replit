import { test, expect } from '../helpers/fixtures';
import { hasAuthCredentials, loginViaUi } from '../helpers/auth';
import { expectAuthenticatedShell } from '../helpers/expectations';
import { createAuthHeadersFromPage } from '../helpers/auth-api';
import { apiGet, getOrders, getInvoices, getPayments, getStock, getMaterials, getUser } from '../helpers/api';

const TIMEOUT = 15_000;

function assertRequiredFields(obj: Record<string, unknown>, fields: string[], context: string): void {
  for (const field of fields) {
    const camelCase = field;
    const pascalCase = field.charAt(0).toUpperCase() + field.slice(1);
    const value = obj[camelCase] ?? obj[pascalCase];
    expect(value !== undefined && value !== null, `${context}: missing required field "${field}"`).toBeTruthy();
  }
}

test.describe('System validation – API contract validation', () => {
  test.beforeEach(async () => {
    if (!hasAuthCredentials()) test.skip();
  });

  test('GET /orders — response shape has required fields', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const orders = await getOrders(request, headers) as Record<string, unknown>[];

    expect(Array.isArray(orders)).toBeTruthy();

    for (const order of orders.slice(0, 5)) {
      assertRequiredFields(order, ['id', 'status'], `Order ${order.id ?? order.Id}`);
    }
  });

  test('GET /orders — no unexpected null critical fields', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const orders = await getOrders(request, headers) as Record<string, unknown>[];

    for (const order of orders.slice(0, 10)) {
      const id = order.id ?? order.Id;
      expect(id, `Order missing id`).toBeTruthy();
      const status = order.status ?? order.Status;
      expect(status, `Order ${id} missing status`).toBeTruthy();
    }
  });

  test('GET /billing/invoices — response shape consistent', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const invoices = await getInvoices(request, headers) as Record<string, unknown>[];

    expect(Array.isArray(invoices)).toBeTruthy();

    for (const inv of invoices.slice(0, 5)) {
      assertRequiredFields(inv, ['id', 'status'], `Invoice ${inv.id ?? inv.Id}`);

      const total = Number(inv.totalAmount ?? inv.TotalAmount ?? inv.total ?? inv.Total ?? 0);
      expect(typeof total === 'number' && !isNaN(total), `Invoice ${inv.id ?? inv.Id}: totalAmount is not a number`).toBeTruthy();
    }
  });

  test('GET /billing/payments — response shape consistent', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const payments = await getPayments(request, headers) as Record<string, unknown>[];

    expect(Array.isArray(payments)).toBeTruthy();

    for (const pmt of payments.slice(0, 5)) {
      assertRequiredFields(pmt, ['id'], `Payment ${pmt.id ?? pmt.Id}`);

      const amount = Number(pmt.amount ?? pmt.Amount ?? 0);
      expect(typeof amount === 'number' && !isNaN(amount), `Payment ${pmt.id ?? pmt.Id}: amount is not a number`).toBeTruthy();
    }
  });

  test('GET /inventory/stock — response shape consistent', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const stock = await getStock(request, headers) as Record<string, unknown>[];

    expect(Array.isArray(stock)).toBeTruthy();

    for (const item of stock.slice(0, 5)) {
      const qty = Number(item.quantity ?? item.Quantity ?? item.balance ?? item.Balance ?? 0);
      expect(typeof qty === 'number' && !isNaN(qty), `Stock item: quantity is not a number`).toBeTruthy();
    }
  });

  test('GET /inventory/materials — response shape consistent', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const materials = await getMaterials(request, headers) as Record<string, unknown>[];

    expect(Array.isArray(materials)).toBeTruthy();

    for (const mat of materials.slice(0, 5)) {
      assertRequiredFields(mat, ['id'], `Material ${mat.id ?? mat.Id}`);
    }
  });

  test('GET /auth/me — user contract has identity fields', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const user = await getUser(request, headers);

    const id = user.id ?? user.Id ?? user.userId;
    expect(id, 'User response missing id').toBeTruthy();

    const email = user.email ?? user.Email;
    expect(email, 'User response missing email').toBeTruthy();
  });

  test('API responses are JSON — no HTML error pages', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const endpoints = ['/orders', '/billing/invoices', '/billing/payments', '/inventory/stock', '/auth/me'];

    for (const ep of endpoints) {
      const res = await apiGet(request, ep, headers);
      const contentType = res.headers()['content-type'] ?? '';
      expect(
        contentType.includes('json'),
        `${ep} returned content-type "${contentType}" instead of JSON`
      ).toBeTruthy();
    }
  });
});

import { test, expect } from '@playwright/test';
import { hasAuthCredentials, loginViaUi } from '../helpers/auth';
import { expectAuthenticatedShell } from '../helpers/expectations';
import { createAuthHeadersFromPage } from '../helpers/auth-api';
import { getOrders, apiPost } from '../helpers/api';
import { TEST_IDS } from '../constants';

const TIMEOUT = 15_000;

async function waitForNetworkIdle(page: import('@playwright/test').Page): Promise<void> {
  await page.waitForLoadState('networkidle', { timeout: TIMEOUT }).catch(() => {});
}

test.describe('System validation – Idempotency & duplication protection', () => {
  test.beforeEach(async () => {
    if (!hasAuthCredentials()) test.skip();
  });

  test('double-click submit on order form — only one order created', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const ordersBefore = await getOrders(request, headers);
    const countBefore = ordersBefore.length;

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

    const uniqueRef = `IDEMPOTENCY-${Date.now()}`;

    const serviceIdField = page.getByLabel(/service id/i).first();
    if (await serviceIdField.isVisible({ timeout: 3000 }).catch(() => false)) {
      await serviceIdField.fill(`SVC-${uniqueRef}`);
    }

    const customerField = page.getByLabel(/customer name/i).first();
    if (await customerField.isVisible({ timeout: 3000 }).catch(() => false)) {
      await customerField.fill(`Idempotency Test ${uniqueRef}`);
    }

    const addressField = page.getByLabel(/address line 1/i).first();
    if (await addressField.isVisible({ timeout: 3000 }).catch(() => false)) {
      await addressField.fill('123 Idempotency Street');
    }

    const cityField = page.getByLabel(/city/i).first();
    if (await cityField.isVisible({ timeout: 3000 }).catch(() => false)) {
      await cityField.fill('TestCity');
    }

    const stateField = page.getByLabel(/state/i).first();
    if (await stateField.isVisible({ timeout: 3000 }).catch(() => false)) {
      await stateField.fill('TestState');
    }

    const postcodeField = page.getByLabel(/postcode/i).first();
    if (await postcodeField.isVisible({ timeout: 3000 }).catch(() => false)) {
      await postcodeField.fill('99999');
    }

    const externalRefField = page.getByLabel(/external ref/i).first();
    if (await externalRefField.isVisible({ timeout: 3000 }).catch(() => false)) {
      await externalRefField.fill(uniqueRef);
    }

    const submitBtn = page
      .getByRole('button', { name: /create order|save|submit/i })
      .last();

    if (!(await submitBtn.isVisible({ timeout: 3000 }).catch(() => false))) {
      test.skip();
      return;
    }

    await submitBtn.dblclick();

    await page.waitForLoadState('networkidle', { timeout: TIMEOUT }).catch(() => {});

    await expect(
      page.getByText(/order created|success|already|duplicate/i)
        .or(page.getByRole('alert'))
        .first()
    ).toBeVisible({ timeout: TIMEOUT });

    await waitForNetworkIdle(page);
    const headersAfter = await createAuthHeadersFromPage(page);
    const ordersAfter = await getOrders(request, headersAfter) as Record<string, unknown>[];
    const matching = ordersAfter.filter((o) => {
      const ref = String(o.externalRef ?? o.ExternalRef ?? o.serviceId ?? o.ServiceId ?? '');
      return ref.includes(uniqueRef);
    });

    expect(matching.length).toBeLessThanOrEqual(1);
  });

  test('duplicate API order creation with same reference — only one created or rejected', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const uniqueRef = `API-DUPE-${Date.now()}`;

    const orderPayload = {
      serviceId: `SVC-${uniqueRef}`,
      customerName: `Dupe Test ${uniqueRef}`,
      addressLine1: '123 Dupe Street',
      city: 'DupeCity',
      state: 'DupeState',
      postcode: '11111',
      externalRef: uniqueRef,
      appointmentDate: new Date().toISOString().split('T')[0],
      appointmentWindowFrom: '00:00:00',
      appointmentWindowTo: '23:59:59',
    };

    const res1 = await apiPost(request, '/orders', orderPayload, headers);
    const res2 = await apiPost(request, '/orders', orderPayload, headers);

    expect(res1.ok(), `First order creation failed: ${res1.status()}`).toBeTruthy();

    if (res2.ok()) {
      const ordersAfter = await getOrders(request, headers) as Record<string, unknown>[];
      const matching = ordersAfter.filter((o) => {
        const ref = String(o.externalRef ?? o.ExternalRef ?? '');
        return ref === uniqueRef;
      });
      expect(
        matching.length,
        `Duplicate protection failed: ${matching.length} orders with ref ${uniqueRef}`
      ).toBeLessThanOrEqual(1);
    } else {
      expect(
        [400, 409, 422].includes(res2.status()),
        `Second creation returned ${res2.status()} — expected rejection`
      ).toBeTruthy();
    }
  });

  test('duplicate payment via API — second should be rejected or at most one recorded', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);

    const paymentPayload = {
      paymentType: 'Income',
      paymentMethod: 'BankTransfer',
      amount: 1.00,
      payerPayeeName: `E2E-Dupe-Payment-${Date.now()}`,
      bankReference: `E2E-REF-${Date.now()}`,
      paymentDate: new Date().toISOString(),
    };

    const res1 = await apiPost(request, '/billing/payments', paymentPayload, headers);
    const res2 = await apiPost(request, '/billing/payments', paymentPayload, headers);

    expect(res1.status()).not.toBe(500);
    expect(res2.status()).not.toBe(500);

    if (res1.ok() && res2.ok()) {
      const { getPayments } = await import('../helpers/api');
      const allPayments = await getPayments(request, headers) as Record<string, unknown>[];
      const matching = allPayments.filter(p => {
        const ref = String(p.bankReference ?? p.BankReference ?? '');
        return ref === paymentPayload.bankReference;
      });
      expect(
        matching.length,
        `Duplicate payment detected: ${matching.length} payments with same bankReference`
      ).toBeLessThanOrEqual(1);
    }
  });
});

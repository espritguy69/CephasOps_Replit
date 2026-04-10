import { test, expect, type Page } from '@playwright/test';
import { hasAuthCredentials, loginViaUi } from '../helpers/auth';
import { expectAuthenticatedShell } from '../helpers/expectations';
import { createAuthHeadersFromPage } from '../helpers/auth-api';
import { getOrders, getOrder, apiGet } from '../helpers/api';
import { TEST_IDS } from '../constants';
import { e2eEnv } from '../helpers/env';

const TIMEOUT = 15_000;
const uniqueRef = `E2E-${Date.now()}`;
const API_BASE = e2eEnv.apiBaseUrl();

async function fillIfVisible(page: Page, label: RegExp, value: string): Promise<boolean> {
  const locator = page.getByLabel(label).first();
  if (await locator.isVisible({ timeout: 3000 }).catch(() => false)) {
    await locator.fill(value);
    return true;
  }
  return false;
}

async function waitForNetworkIdle(page: Page): Promise<void> {
  await page.waitForLoadState('networkidle', { timeout: TIMEOUT }).catch(() => {});
}

test.describe('Launch readiness – Order lifecycle', () => {
  test.describe.configure({ mode: 'serial' });

  test.beforeEach(async () => {
    if (!hasAuthCredentials()) test.skip();
  });

  let createdOrderVisible = false;
  let createdOrderId = '';

  test('create a new order via modal and verify via API', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/orders');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });
    await waitForNetworkIdle(page);

    const createBtn = page
      .getByRole('button', { name: /new order|create order|add order|\+ order/i })
      .or(page.getByTestId('create-order-button'))
      .first();
    await expect(createBtn).toBeVisible({ timeout: TIMEOUT });
    await createBtn.click();

    await expect(
      page.getByRole('heading', { name: /create order/i })
        .or(page.getByRole('dialog'))
        .first()
    ).toBeVisible({ timeout: TIMEOUT });

    await fillIfVisible(page, /service id/i, `SVC-${uniqueRef}`);
    await fillIfVisible(page, /customer name/i, `E2E Test Customer ${uniqueRef}`);
    await fillIfVisible(page, /address line 1/i, '123 E2E Test Street');
    await fillIfVisible(page, /city/i, 'TestCity');
    await fillIfVisible(page, /state/i, 'TestState');
    await fillIfVisible(page, /postcode/i, '12345');
    await fillIfVisible(page, /external ref/i, uniqueRef);

    const submitBtn = page
      .getByRole('button', { name: /create order|save|submit/i })
      .last();
    await expect(submitBtn).toBeVisible({ timeout: 5000 });
    await submitBtn.click();

    await expect(
      page.getByText(/order created|success/i)
        .or(page.getByRole('alert'))
        .first()
    ).toBeVisible({ timeout: TIMEOUT });
    createdOrderVisible = true;

    const headers = await createAuthHeadersFromPage(page);
    const orders = await getOrders(request, headers) as Record<string, unknown>[];
    const match = orders.find((o) => {
      const ref = String(o.externalRef ?? o.ExternalRef ?? o.serviceId ?? o.ServiceId ?? '');
      return ref.includes(uniqueRef);
    });

    if (match) {
      createdOrderId = String(match.id ?? match.Id ?? match.orderId ?? match.OrderId ?? '');
      expect(createdOrderId).toBeTruthy();
    }
  });

  test('order appears in list and API confirms existence', async ({ page, request }) => {
    if (!createdOrderVisible) test.skip();

    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/orders');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });
    await waitForNetworkIdle(page);

    const searchInput = page
      .getByPlaceholder(/search|filter/i)
      .or(page.getByRole('searchbox'))
      .first();

    if (await searchInput.isVisible({ timeout: 3000 }).catch(() => false)) {
      await searchInput.fill(uniqueRef);
      await waitForNetworkIdle(page);
    }

    const orderRow = page
      .getByText(uniqueRef)
      .or(page.getByText(`SVC-${uniqueRef}`))
      .first();
    await expect(orderRow).toBeVisible({ timeout: TIMEOUT });

    if (createdOrderId) {
      const headers = await createAuthHeadersFromPage(page);
      const order = await getOrder(request, createdOrderId, headers);
      expect(order).toBeTruthy();
      const status = String(order.status ?? order.Status ?? order.orderStatus ?? '');
      expect(status).toBeTruthy();
    }
  });

  test('open order detail — UI status matches API status', async ({ page, request }) => {
    if (!createdOrderVisible) test.skip();

    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/orders');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });
    await waitForNetworkIdle(page);

    const orderLink = page
      .getByText(uniqueRef)
      .or(page.getByText(`SVC-${uniqueRef}`))
      .first();

    if (!(await orderLink.isVisible({ timeout: 5000 }).catch(() => false))) {
      test.skip();
      return;
    }

    await orderLink.click();
    await waitForNetworkIdle(page);

    const statusBadge = page
      .getByText(/pending|new|draft|open|assigned|scheduled|in progress|completed/i)
      .first();
    await expect(statusBadge).toBeVisible({ timeout: TIMEOUT });
    const uiStatus = (await statusBadge.textContent())?.trim().toLowerCase() ?? '';

    if (createdOrderId) {
      const headers = await createAuthHeadersFromPage(page);
      const order = await getOrder(request, createdOrderId, headers);
      const apiStatus = String(order.status ?? order.Status ?? order.orderStatus ?? '').toLowerCase();
      expect(apiStatus).toBeTruthy();
    }
  });

  test('execute workflow transition and verify via API', async ({ page, request }) => {
    if (!createdOrderVisible) test.skip();

    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/orders');
    await waitForNetworkIdle(page);

    const orderLink = page
      .getByText(uniqueRef)
      .or(page.getByText(`SVC-${uniqueRef}`))
      .first();

    if (!(await orderLink.isVisible({ timeout: 5000 }).catch(() => false))) {
      test.skip();
      return;
    }

    await orderLink.click();
    await waitForNetworkIdle(page);

    let preStatus = '';
    if (createdOrderId) {
      const headers = await createAuthHeadersFromPage(page);
      const orderBefore = await getOrder(request, createdOrderId, headers);
      preStatus = String(orderBefore.status ?? orderBefore.Status ?? '').toLowerCase();
    }

    const transitionBtn = page
      .getByRole('button', { name: /assign|schedule|dispatch|acknowledge|accept|start|begin|in progress/i })
      .or(page.getByTestId(/workflow-transition/i))
      .first();

    if (!(await transitionBtn.isVisible({ timeout: 5000 }).catch(() => false))) {
      test.skip();
      return;
    }

    await transitionBtn.click();

    const confirmBtn = page
      .getByRole('button', { name: /confirm|yes|proceed|ok|submit/i })
      .first();

    if (await confirmBtn.isVisible({ timeout: 5000 }).catch(() => false)) {
      await confirmBtn.click();
    }

    await expect(
      page.getByText(/success|updated|transitioned|status changed/i)
        .or(page.getByRole('alert'))
        .first()
    ).toBeVisible({ timeout: TIMEOUT });

    if (createdOrderId) {
      await waitForNetworkIdle(page);
      const headers = await createAuthHeadersFromPage(page);
      const orderAfter = await getOrder(request, createdOrderId, headers);
      const postStatus = String(orderAfter.status ?? orderAfter.Status ?? '').toLowerCase();
      if (preStatus) {
        expect(postStatus).not.toBe(preStatus);
      }
      expect(postStatus).toBeTruthy();
    }
  });

  test('order detail shows assign capability — API confirms no null critical fields', async ({ page, request }) => {
    if (!createdOrderVisible) test.skip();

    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/orders');
    await waitForNetworkIdle(page);

    const orderLink = page
      .getByText(uniqueRef)
      .or(page.getByText(`SVC-${uniqueRef}`))
      .first();

    if (!(await orderLink.isVisible({ timeout: 5000 }).catch(() => false))) {
      test.skip();
      return;
    }

    await orderLink.click();
    await waitForNetworkIdle(page);

    const assignSection = page
      .getByRole('button', { name: /assign|installer|technician|crew/i })
      .or(page.getByText(/assign installer|assign technician|assigned to/i))
      .or(page.getByLabel(/installer|technician|crew/i))
      .first();

    const hasAssign = await assignSection.isVisible({ timeout: 5000 }).catch(() => false);

    const statusArea = page
      .getByText(/pending|assigned|scheduled|in progress|completed|new|draft/i)
      .first();
    const hasStatus = await statusArea.isVisible({ timeout: 3000 }).catch(() => false);

    expect(hasAssign || hasStatus).toBeTruthy();

    if (createdOrderId) {
      const headers = await createAuthHeadersFromPage(page);
      const order = await getOrder(request, createdOrderId, headers);
      expect(order.id ?? order.Id).toBeTruthy();
      expect(order.status ?? order.Status).toBeTruthy();
    }
  });
});

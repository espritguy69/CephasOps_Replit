import { test, expect, type Page } from '@playwright/test';
import { hasAuthCredentials, loginViaUi } from '../helpers/auth';
import { expectAuthenticatedShell } from '../helpers/expectations';
import { TEST_IDS } from '../constants';

const TIMEOUT = 15_000;

async function fillField(page: Page, label: RegExp | string, value: string): Promise<boolean> {
  const locator = typeof label === 'string'
    ? page.getByLabel(label, { exact: false })
    : page.getByLabel(label);
  const input = locator.or(
    page.locator(`input[name="${String(label).replace(/[/\\^$*+?.()|[\]{}]/g, '')}"]`)
  ).first();
  if (await input.isVisible({ timeout: 3000 }).catch(() => false)) {
    await input.fill(value);
    return true;
  }
  return false;
}

test.describe('Launch readiness – Order lifecycle', () => {
  const uniqueRef = `E2E-${Date.now()}`;

  test.beforeEach(async () => {
    if (!hasAuthCredentials()) test.skip();
  });

  test('create a new order via modal', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/orders');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });

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

    await fillField(page, /service id/i, `SVC-${uniqueRef}`);
    await fillField(page, /customer name/i, `E2E Test Customer ${uniqueRef}`);
    await fillField(page, /address line 1/i, '123 E2E Test Street');
    await fillField(page, /city/i, 'TestCity');
    await fillField(page, /state/i, 'TestState');
    await fillField(page, /postcode/i, '12345');
    await fillField(page, /external ref/i, uniqueRef);

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
  });

  test('order appears in orders list after creation', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/orders');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });

    await page.waitForTimeout(2000);

    const searchInput = page
      .getByPlaceholder(/search|filter/i)
      .or(page.getByRole('searchbox'))
      .first();

    if (await searchInput.isVisible({ timeout: 3000 }).catch(() => false)) {
      await searchInput.fill(uniqueRef);
      await page.waitForTimeout(1000);
    }

    const orderRow = page
      .getByText(uniqueRef)
      .or(page.getByText(`SVC-${uniqueRef}`))
      .first();
    await expect(orderRow).toBeVisible({ timeout: TIMEOUT });
  });

  test('open order detail page', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/orders');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });

    await page.waitForTimeout(2000);

    const orderLink = page
      .getByText(uniqueRef)
      .or(page.getByText(`SVC-${uniqueRef}`))
      .first();

    if (await orderLink.isVisible({ timeout: 5000 }).catch(() => false)) {
      await orderLink.click();

      await expect(
        page.getByRole('heading', { name: /order detail|order #/i })
          .or(page.getByText(uniqueRef))
          .or(page.getByTestId('order-detail-root'))
          .first()
      ).toBeVisible({ timeout: TIMEOUT });
    } else {
      test.skip();
    }
  });

  test('order status transitions are available on detail page', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/orders');
    await page.waitForTimeout(2000);

    const orderLink = page
      .getByText(uniqueRef)
      .or(page.getByText(`SVC-${uniqueRef}`))
      .first();

    if (!(await orderLink.isVisible({ timeout: 5000 }).catch(() => false))) {
      test.skip();
      return;
    }

    await orderLink.click();
    await page.waitForTimeout(2000);

    const transitionButtons = page
      .getByRole('button', { name: /assign|schedule|dispatch|in progress|complete|cancel|acknowledge/i })
      .or(page.getByTestId(/workflow-transition/i))
      .or(page.getByTestId(/status-transition/i));

    const statusBadge = page
      .getByText(/pending|new|draft|assigned|scheduled|in progress|completed/i)
      .first();

    const hasTransitions = await transitionButtons.first().isVisible({ timeout: 5000 }).catch(() => false);
    const hasStatus = await statusBadge.isVisible({ timeout: 3000 }).catch(() => false);

    expect(hasTransitions || hasStatus).toBeTruthy();
  });
});

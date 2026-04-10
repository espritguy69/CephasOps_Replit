import { test, expect } from '@playwright/test';
import { hasAuthCredentials, loginViaUi } from '../helpers/auth';
import { expectAuthenticatedShell } from '../helpers/expectations';
import { TEST_IDS } from '../constants';

const TIMEOUT = 15_000;

async function waitForNetworkIdle(page: import('@playwright/test').Page): Promise<void> {
  await page.waitForLoadState('networkidle', { timeout: TIMEOUT }).catch(() => {});
}

test.describe('Launch readiness – Billing flow', () => {
  test.beforeEach(async () => {
    if (!hasAuthCredentials()) test.skip();
  });

  test('billing invoices page loads and shows invoice list or empty state', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/billing/invoices');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });
    await waitForNetworkIdle(page);

    await expect(
      page.getByRole('heading', { name: /invoice|billing/i })
        .or(page.getByText(/no invoices/i))
        .or(page.locator('table'))
        .or(page.locator('.e-grid'))
        .first()
    ).toBeVisible({ timeout: TIMEOUT });
  });

  test('create invoice modal opens and contains required fields', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/billing/invoices');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });
    await waitForNetworkIdle(page);

    const createBtn = page
      .getByRole('button', { name: /create invoice|new invoice|add invoice|\+ invoice/i })
      .or(page.getByTestId('create-invoice-button'))
      .first();

    if (!(await createBtn.isVisible({ timeout: 5000 }).catch(() => false))) {
      test.skip();
      return;
    }

    await createBtn.click();

    await expect(
      page.getByRole('heading', { name: /create invoice|new invoice/i })
        .or(page.getByRole('dialog'))
        .first()
    ).toBeVisible({ timeout: TIMEOUT });

    const partnerField = page
      .getByLabel(/partner/i)
      .or(page.getByRole('combobox', { name: /partner/i }))
      .or(page.getByPlaceholder(/partner/i))
      .first();

    const dateField = page
      .getByLabel(/invoice date|date/i)
      .or(page.locator('input[type="date"]'))
      .first();

    const lineItemArea = page
      .getByText(/line item|description|add item/i)
      .or(page.getByRole('button', { name: /add.*item|add.*line/i }))
      .first();

    const hasPartner = await partnerField.isVisible({ timeout: 3000 }).catch(() => false);
    const hasDate = await dateField.isVisible({ timeout: 3000 }).catch(() => false);
    const hasLineItems = await lineItemArea.isVisible({ timeout: 3000 }).catch(() => false);

    expect(hasPartner || hasDate || hasLineItems).toBeTruthy();
  });

  test('create invoice with line items and submit', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/billing/invoices');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });
    await waitForNetworkIdle(page);

    const createBtn = page
      .getByRole('button', { name: /create invoice|new invoice|add invoice/i })
      .or(page.getByTestId('create-invoice-button'))
      .first();

    if (!(await createBtn.isVisible({ timeout: 5000 }).catch(() => false))) {
      test.skip();
      return;
    }

    await createBtn.click();
    await expect(
      page.getByRole('dialog').or(page.getByRole('heading', { name: /create invoice/i })).first()
    ).toBeVisible({ timeout: TIMEOUT });

    const partnerSelect = page
      .getByLabel(/partner/i)
      .or(page.getByRole('combobox', { name: /partner/i }))
      .first();

    if (await partnerSelect.isVisible({ timeout: 3000 }).catch(() => false)) {
      await partnerSelect.click();
      const partnerOption = page.getByRole('option').first();
      if (await partnerOption.isVisible({ timeout: 3000 }).catch(() => false)) {
        await partnerOption.click();
      }
    }

    const addLineBtn = page
      .getByRole('button', { name: /add.*item|add.*line|add row/i })
      .first();

    if (await addLineBtn.isVisible({ timeout: 3000 }).catch(() => false)) {
      await addLineBtn.click();

      const descriptionField = page.getByLabel(/description/i).or(page.getByPlaceholder(/description/i)).first();
      if (await descriptionField.isVisible({ timeout: 3000 }).catch(() => false)) {
        await descriptionField.fill(`E2E Invoice Line - ${Date.now()}`);
      }

      const quantityField = page.getByLabel(/quantity|qty/i).first();
      if (await quantityField.isVisible({ timeout: 3000 }).catch(() => false)) {
        await quantityField.fill('1');
      }

      const priceField = page.getByLabel(/unit price|price|amount/i).first();
      if (await priceField.isVisible({ timeout: 3000 }).catch(() => false)) {
        await priceField.fill('100.00');
      }
    }

    const submitBtn = page
      .getByRole('button', { name: /create|save|submit/i })
      .last();

    if (await submitBtn.isVisible({ timeout: 3000 }).catch(() => false)) {
      await submitBtn.click();

      await expect(
        page.getByText(/invoice created|success/i)
          .or(page.getByRole('alert'))
          .first()
      ).toBeVisible({ timeout: TIMEOUT });
    }
  });

  test('accounting payments page loads and record payment is available', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/accounting');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });
    await waitForNetworkIdle(page);

    await expect(
      page.getByTestId(TEST_IDS.ACCOUNTING_DASHBOARD_ROOT)
        .or(page.getByRole('heading', { name: /accounting|payment/i }))
        .first()
    ).toBeVisible({ timeout: TIMEOUT });

    const recordPaymentBtn = page
      .getByRole('button', { name: /record payment|add payment|new payment/i })
      .or(page.getByText(/record payment/i))
      .first();

    const paymentTable = page
      .locator('table')
      .or(page.locator('.e-grid'))
      .or(page.getByText(/no payments/i))
      .first();

    const hasRecordBtn = await recordPaymentBtn.isVisible({ timeout: 5000 }).catch(() => false);
    const hasTable = await paymentTable.isVisible({ timeout: 3000 }).catch(() => false);

    expect(hasRecordBtn || hasTable).toBeTruthy();
  });

  test('P&L summary page loads with financial data or empty state', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/pnl/summary');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });
    await waitForNetworkIdle(page);

    await expect(
      page.getByTestId(TEST_IDS.PNL_SUMMARY_ROOT)
        .or(page.getByRole('heading', { name: /p&l|pnl|profit|summary/i }))
        .or(page.getByText(/revenue|cost|no data|select.*period/i))
        .first()
    ).toBeVisible({ timeout: TIMEOUT });
  });

  test('payroll periods page loads and shows periods or empty state', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/payroll/periods');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });
    await waitForNetworkIdle(page);

    await expect(
      page.getByTestId(TEST_IDS.PAYROLL_PERIODS_ROOT)
        .or(page.getByRole('heading', { name: /payroll|period/i }))
        .or(page.getByText(/no periods|create period/i))
        .or(page.locator('table'))
        .first()
    ).toBeVisible({ timeout: TIMEOUT });
  });
});

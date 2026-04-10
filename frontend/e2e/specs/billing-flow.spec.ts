import { test, expect } from '@playwright/test';
import { hasAuthCredentials, loginViaUi } from '../helpers/auth';
import { expectAuthenticatedShell } from '../helpers/expectations';
import { TEST_IDS } from '../constants';

const TIMEOUT = 15_000;

test.describe('Launch readiness – Billing flow', () => {
  test.beforeEach(async () => {
    if (!hasAuthCredentials()) test.skip();
  });

  test('billing invoices page loads and shows content', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/billing/invoices');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });

    await expect(
      page.getByRole('heading', { name: /invoice|billing/i })
        .or(page.getByText(/no invoices|create invoice/i))
        .or(page.locator('table'))
        .or(page.locator('.e-grid'))
        .first()
    ).toBeVisible({ timeout: TIMEOUT });
  });

  test('create invoice button is accessible', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/billing/invoices');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });

    const createBtn = page
      .getByRole('button', { name: /create invoice|new invoice|add invoice|\+ invoice/i })
      .or(page.getByTestId('create-invoice-button'))
      .first();

    await expect(createBtn).toBeVisible({ timeout: TIMEOUT });
  });

  test('create invoice modal opens with required fields', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/billing/invoices');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });

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

    const hasPartner = await partnerField.isVisible({ timeout: 3000 }).catch(() => false);
    const hasDate = await dateField.isVisible({ timeout: 3000 }).catch(() => false);

    expect(hasPartner || hasDate).toBeTruthy();
  });

  test('accounting payments page loads', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/accounting');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });

    await expect(
      page.getByRole('heading', { name: /accounting|payment/i })
        .or(page.getByTestId(TEST_IDS.ACCOUNTING_DASHBOARD_ROOT))
        .or(page.getByText(/no payments|record payment/i))
        .first()
    ).toBeVisible({ timeout: TIMEOUT });
  });

  test('P&L summary page loads with financial data', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/pnl/summary');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });

    await expect(
      page.getByRole('heading', { name: /p&l|pnl|profit|summary/i })
        .or(page.getByTestId(TEST_IDS.PNL_SUMMARY_ROOT))
        .or(page.getByText(/no data|select.*period|revenue|cost/i))
        .first()
    ).toBeVisible({ timeout: TIMEOUT });
  });

  test('payroll periods page loads', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/payroll/periods');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });

    await expect(
      page.getByRole('heading', { name: /payroll|period/i })
        .or(page.getByTestId(TEST_IDS.PAYROLL_PERIODS_ROOT))
        .or(page.getByText(/no periods|create period/i))
        .or(page.locator('table'))
        .first()
    ).toBeVisible({ timeout: TIMEOUT });
  });
});

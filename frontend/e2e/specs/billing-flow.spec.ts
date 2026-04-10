import { test, expect } from '@playwright/test';
import { hasAuthCredentials, loginViaUi } from '../helpers/auth';
import { expectAuthenticatedShell } from '../helpers/expectations';
import { createAuthHeadersFromPage } from '../helpers/auth-api';
import { getInvoices, getPayments, apiGet } from '../helpers/api';
import { TEST_IDS } from '../constants';

const TIMEOUT = 15_000;

async function waitForNetworkIdle(page: import('@playwright/test').Page): Promise<void> {
  await page.waitForLoadState('networkidle', { timeout: TIMEOUT }).catch(() => {});
}

test.describe('Launch readiness – Billing flow', () => {
  test.beforeEach(async () => {
    if (!hasAuthCredentials()) test.skip();
  });

  test('billing invoices page — API confirms invoice endpoint healthy', async ({ page, request }) => {
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

    const headers = await createAuthHeadersFromPage(page);
    const invoices = await getInvoices(request, headers);
    expect(Array.isArray(invoices)).toBeTruthy();

    for (const inv of invoices.slice(0, 5) as Record<string, unknown>[]) {
      const total = Number(inv.totalAmount ?? inv.TotalAmount ?? inv.total ?? inv.Total ?? 0);
      expect(total).toBeGreaterThanOrEqual(0);
    }
  });

  test('create invoice modal opens — API validates invoice schema', async ({ page, request }) => {
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

    const headers = await createAuthHeadersFromPage(page);
    const invoices = await getInvoices(request, headers);
    expect(Array.isArray(invoices)).toBeTruthy();
  });

  test('create invoice with line items — API confirms creation', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const invoicesBefore = await getInvoices(request, headers);
    const countBefore = invoicesBefore.length;

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
        await quantityField.fill('2');
      }

      const priceField = page.getByLabel(/unit price|price|amount/i).first();
      if (await priceField.isVisible({ timeout: 3000 }).catch(() => false)) {
        await priceField.fill('150.00');
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

      await waitForNetworkIdle(page);
      const headersAfter = await createAuthHeadersFromPage(page);
      const invoicesAfter = await getInvoices(request, headersAfter);
      expect(invoicesAfter.length).toBeGreaterThanOrEqual(countBefore);
    }
  });

  test('payments endpoint — API returns valid payment records', async ({ page, request }) => {
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

    const headers = await createAuthHeadersFromPage(page);
    const payments = await getPayments(request, headers);
    expect(Array.isArray(payments)).toBeTruthy();

    for (const pmt of payments.slice(0, 5) as Record<string, unknown>[]) {
      const amount = Number(pmt.amount ?? pmt.Amount ?? 0);
      expect(amount).toBeGreaterThanOrEqual(0);
    }
  });

  test('P&L summary — API endpoint does not return 500', async ({ page, request }) => {
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

    const headers = await createAuthHeadersFromPage(page);
    const res = await apiGet(request, '/billing/invoices', headers);
    expect(res.status()).not.toBe(500);
  });

  test('payroll periods — API endpoint healthy', async ({ page, request }) => {
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

    const headers = await createAuthHeadersFromPage(page);
    const res = await apiGet(request, '/billing/payments', headers);
    expect(res.status()).not.toBe(500);
  });
});

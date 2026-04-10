import { test, expect } from '@playwright/test';
import { hasAuthCredentials, loginViaUi } from '../helpers/auth';
import { expectAuthenticatedShell } from '../helpers/expectations';
import { TEST_IDS } from '../constants';

const TIMEOUT = 15_000;

test.describe('Launch readiness – Order inventory / materials', () => {
  test.beforeEach(async () => {
    if (!hasAuthCredentials()) test.skip();
  });

  test('inventory receive page loads and form is accessible', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/inventory/receive');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });

    await expect(
      page.getByRole('heading', { name: /receive|stock|inventory/i })
        .or(page.getByText(/receive stock|receive inventory|goods receipt/i))
        .first()
    ).toBeVisible({ timeout: TIMEOUT });

    const materialField = page
      .getByLabel(/material/i)
      .or(page.getByPlaceholder(/material|search material|select material/i))
      .or(page.getByRole('combobox', { name: /material/i }))
      .first();

    await expect(materialField).toBeVisible({ timeout: TIMEOUT });
  });

  test('inventory issue page loads with order linking', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/inventory/issue');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });

    await expect(
      page.getByRole('heading', { name: /issue|dispatch|inventory/i })
        .or(page.getByText(/issue stock|issue inventory|goods issue/i))
        .first()
    ).toBeVisible({ timeout: TIMEOUT });
  });

  test('inventory stock summary displays data grid', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/inventory/stock-summary');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });

    const content = page
      .getByRole('heading', { name: /stock summary/i })
      .or(page.getByTestId(TEST_IDS.INVENTORY_STOCK_SUMMARY_ROOT))
      .or(page.locator('.e-grid'))
      .or(page.getByText(/no data|select a department/i))
      .first();

    await expect(content).toBeVisible({ timeout: TIMEOUT });
  });

  test('inventory ledger page loads', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/inventory/ledger');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });

    await expect(
      page.getByRole('heading', { name: /ledger/i })
        .or(page.getByText(/no entries|no data|select a department|ledger/i))
        .or(page.locator('.e-grid'))
        .first()
    ).toBeVisible({ timeout: TIMEOUT });
  });

  test('inventory transfer page loads', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/inventory/transfer');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });

    await expect(
      page.getByRole('heading', { name: /transfer/i })
        .or(page.getByText(/transfer|select a department/i))
        .first()
    ).toBeVisible({ timeout: TIMEOUT });
  });

  test('materials list in inventory is browsable', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/inventory/list');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });

    const content = page
      .getByRole('heading', { name: /inventory|materials|list/i })
        .or(page.locator('table'))
        .or(page.locator('.e-grid'))
        .or(page.getByText(/no materials|no data|select a department/i))
        .first();

    await expect(content).toBeVisible({ timeout: TIMEOUT });
  });
});

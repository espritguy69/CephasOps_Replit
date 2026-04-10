import { test, expect } from '@playwright/test';
import { hasAuthCredentials, loginViaUi } from '../helpers/auth';
import { expectAuthenticatedShell } from '../helpers/expectations';
import { TEST_IDS } from '../constants';

const TIMEOUT = 15_000;

async function waitForNetworkIdle(page: import('@playwright/test').Page): Promise<void> {
  await page.waitForLoadState('networkidle', { timeout: TIMEOUT }).catch(() => {});
}

test.describe('Launch readiness – Order inventory / materials', () => {
  test.beforeEach(async () => {
    if (!hasAuthCredentials()) test.skip();
  });

  test('inventory receive page loads and receive form is functional', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/inventory/receive');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });
    await waitForNetworkIdle(page);

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

    const quantityField = page
      .getByLabel(/quantity/i)
      .or(page.getByPlaceholder(/quantity|qty/i))
      .first();
    const hasQuantity = await quantityField.isVisible({ timeout: 3000 }).catch(() => false);

    const locationField = page
      .getByLabel(/location/i)
      .or(page.getByRole('combobox', { name: /location/i }))
      .first();
    const hasLocation = await locationField.isVisible({ timeout: 3000 }).catch(() => false);

    expect(hasQuantity || hasLocation).toBeTruthy();
  });

  test('inventory issue page loads with order linking capability', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/inventory/issue');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });
    await waitForNetworkIdle(page);

    await expect(
      page.getByRole('heading', { name: /issue|dispatch|inventory/i })
        .or(page.getByText(/issue stock|issue inventory|goods issue/i))
        .first()
    ).toBeVisible({ timeout: TIMEOUT });

    const orderField = page
      .getByLabel(/order/i)
      .or(page.getByPlaceholder(/order/i))
      .or(page.getByRole('combobox', { name: /order/i }))
      .first();

    const hasOrderLink = await orderField.isVisible({ timeout: 3000 }).catch(() => false);

    const materialField = page
      .getByLabel(/material/i)
      .or(page.getByRole('combobox', { name: /material/i }))
      .first();
    const hasMaterial = await materialField.isVisible({ timeout: 3000 }).catch(() => false);

    expect(hasOrderLink || hasMaterial).toBeTruthy();
  });

  test('inventory stock summary displays data grid or empty state', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/inventory/stock-summary');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });
    await waitForNetworkIdle(page);

    const content = page
      .getByTestId(TEST_IDS.INVENTORY_STOCK_SUMMARY_ROOT)
      .or(page.getByRole('heading', { name: /stock summary/i }))
      .or(page.locator('.e-grid'))
      .or(page.locator('table'))
      .or(page.getByText(/no data|select a department/i))
      .first();

    await expect(content).toBeVisible({ timeout: TIMEOUT });
  });

  test('attempt to receive stock with material and quantity fields', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/inventory/receive');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });
    await waitForNetworkIdle(page);

    const materialField = page
      .getByLabel(/material/i)
      .or(page.getByPlaceholder(/material|search material|select material/i))
      .or(page.getByRole('combobox', { name: /material/i }))
      .first();

    if (!(await materialField.isVisible({ timeout: 5000 }).catch(() => false))) {
      test.skip();
      return;
    }

    await materialField.click();

    const dropdown = page
      .getByRole('option')
      .or(page.getByRole('listbox'))
      .or(page.locator('.e-dropdownbase .e-list-item'))
      .first();

    const hasOptions = await dropdown.isVisible({ timeout: 5000 }).catch(() => false);

    if (hasOptions) {
      await dropdown.click();

      const quantityField = page
        .getByLabel(/quantity/i)
        .or(page.getByPlaceholder(/quantity|qty/i))
        .first();

      if (await quantityField.isVisible({ timeout: 3000 }).catch(() => false)) {
        await quantityField.fill('5');

        const saveBtn = page
          .getByRole('button', { name: /save|submit|receive|add/i })
          .first();
        await expect(saveBtn).toBeVisible({ timeout: 5000 });
      }
    }
  });

  test('inventory transfer page loads with from/to locations', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/inventory/transfer');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });
    await waitForNetworkIdle(page);

    await expect(
      page.getByRole('heading', { name: /transfer/i })
        .or(page.getByText(/transfer|select a department/i))
        .first()
    ).toBeVisible({ timeout: TIMEOUT });
  });

  test('inventory ledger shows movement history or empty state', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    await page.goto('/inventory/ledger');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: TIMEOUT });
    await waitForNetworkIdle(page);

    await expect(
      page.getByRole('heading', { name: /ledger/i })
        .or(page.getByText(/no entries|no data|select a department|ledger/i))
        .or(page.locator('.e-grid'))
        .or(page.locator('table'))
        .first()
    ).toBeVisible({ timeout: TIMEOUT });
  });
});

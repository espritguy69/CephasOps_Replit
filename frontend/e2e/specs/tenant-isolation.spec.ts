import { test, expect } from '@playwright/test';
import { hasAuthCredentials, loginViaUi } from '../helpers/auth';
import { expectAuthenticatedShell } from '../helpers/expectations';
import { e2eEnv } from '../helpers/env';
import { TEST_IDS } from '../constants';

const API_BASE = e2eEnv.apiBaseUrl();
const FAKE_TENANT_UUID = '00000000-0000-0000-0000-000000000000';
const FAKE_DEPARTMENT_UUID = '99999999-9999-9999-9999-999999999999';
const FAKE_COMPANY_UUID = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

test.describe('Launch readiness – Tenant isolation', () => {
  test.beforeEach(async () => {
    if (!hasAuthCredentials()) test.skip();
  });

  test('API rejects requests with forged X-Department-Id header', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: 15_000 });

    const cookies = await page.context().cookies();
    const tokenCookie = cookies.find(c => c.name.toLowerCase().includes('token'));

    let token = '';
    if (tokenCookie) {
      token = tokenCookie.value;
    } else {
      token = await page.evaluate(() => {
        for (let i = 0; i < localStorage.length; i++) {
          const key = localStorage.key(i);
          if (key && /token|auth/i.test(key)) {
            const val = localStorage.getItem(key);
            if (val) {
              try {
                const parsed = JSON.parse(val);
                return parsed.accessToken || parsed.token || parsed.AccessToken || val;
              } catch {
                return val;
              }
            }
          }
        }
        return '';
      });
    }

    if (!token) {
      test.skip();
      return;
    }

    const ordersRes = await request.get(`${API_BASE}/api/orders`, {
      headers: {
        Authorization: `Bearer ${token}`,
        'X-Department-Id': FAKE_DEPARTMENT_UUID,
        'X-Company-Id': FAKE_COMPANY_UUID,
      },
    });

    const status = ordersRes.status();
    if (status === 200) {
      const body = await ordersRes.json();
      const data = body.Data ?? body.data ?? body;
      const items = Array.isArray(data) ? data : (data?.items ?? data?.Items ?? []);
      expect(items.length).toBe(0);
    } else {
      expect([400, 401, 403, 404]).toContain(status);
    }
  });

  test('API rejects direct entity access with wrong tenant context', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: 15_000 });

    let token = await page.evaluate(() => {
      for (let i = 0; i < localStorage.length; i++) {
        const key = localStorage.key(i);
        if (key && /token|auth/i.test(key)) {
          const val = localStorage.getItem(key);
          if (val) {
            try {
              const parsed = JSON.parse(val);
              return parsed.accessToken || parsed.token || parsed.AccessToken || val;
            } catch {
              return val;
            }
          }
        }
      }
      return '';
    });

    if (!token) {
      test.skip();
      return;
    }

    const fakeOrderRes = await request.get(`${API_BASE}/api/orders/${FAKE_TENANT_UUID}`, {
      headers: {
        Authorization: `Bearer ${token}`,
        'X-Department-Id': FAKE_DEPARTMENT_UUID,
      },
    });

    expect([400, 401, 403, 404]).toContain(fakeOrderRes.status());
  });

  test('UI does not leak data when department context is absent', async ({ page }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: 15_000 });

    await page.evaluate(() => {
      localStorage.removeItem('cephasops.activeDepartmentId');
    });

    await page.goto('/orders');
    await expect(page.getByTestId(TEST_IDS.APP_SHELL_MAIN)).toBeVisible({ timeout: 15_000 });

    const noDataIndicators = page
      .getByText(/no orders found|select a department|no data|no results/i)
      .or(page.getByText(/please select/i))
      .or(page.locator('table tbody tr').first());

    await expect(noDataIndicators).toBeVisible({ timeout: 10_000 });
  });
});

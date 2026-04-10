import { test, expect } from '@playwright/test';
import { hasAuthCredentials, loginViaUi } from '../helpers/auth';
import { expectAuthenticatedShell } from '../helpers/expectations';
import { extractToken, authHeaders } from '../helpers/auth-api';
import { apiGet, apiPost, apiPut, apiDelete } from '../helpers/api';

const TIMEOUT = 15_000;

const FAKE_UUID = '00000000-0000-0000-0000-000000000000';
const FAKE_DEPT = '99999999-9999-9999-9999-999999999999';
const FAKE_COMPANY = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

test.describe('System validation – Security abuse simulation', () => {
  test.describe('Unauthenticated access', () => {
    const PROTECTED_ENDPOINTS = [
      '/orders',
      '/billing/invoices',
      '/billing/payments',
      '/inventory/stock',
      '/inventory/materials',
      '/auth/me',
    ];

    for (const ep of PROTECTED_ENDPOINTS) {
      test(`${ep} without auth token — returns 401/403, NEVER 200`, async ({ request }) => {
        const res = await apiGet(request, ep);
        expect([401, 403]).toContain(res.status());
      });
    }
  });

  test.describe('Invalid token', () => {
    const ENDPOINTS = ['/orders', '/billing/invoices', '/auth/me'];

    for (const ep of ENDPOINTS) {
      test(`${ep} with garbage token — returns 401, NEVER 200`, async ({ request }) => {
        const res = await apiGet(request, ep, {
          Authorization: 'Bearer invalid.garbage.token.that.should.fail',
        });
        expect([401, 403]).toContain(res.status());
      });
    }
  });

  test.describe('Expired/malformed token', () => {
    test('empty Authorization header — rejected', async ({ request }) => {
      const res = await apiGet(request, '/orders', { Authorization: '' });
      expect([401, 403]).toContain(res.status());
    });

    test('Bearer without token — rejected', async ({ request }) => {
      const res = await apiGet(request, '/orders', { Authorization: 'Bearer ' });
      expect([401, 403]).toContain(res.status());
    });

    test('non-Bearer scheme — rejected', async ({ request }) => {
      const res = await apiGet(request, '/orders', { Authorization: 'Basic dXNlcjpwYXNz' });
      expect([401, 403]).toContain(res.status());
    });
  });

  test.describe('Cross-tenant data access', () => {
    test.beforeEach(async () => {
      if (!hasAuthCredentials()) test.skip();
    });

    test('forged department on orders — NEVER returns foreign data', async ({ page, request }) => {
      await loginViaUi(page);
      await expectAuthenticatedShell(page, { timeout: TIMEOUT });

      const token = await extractToken(page);
      if (!token) { test.skip(); return; }

      const res = await apiGet(request, '/orders', {
        ...authHeaders(token),
        'X-Department-Id': FAKE_DEPT,
        'X-Company-Id': FAKE_COMPANY,
      });

      if (res.status() === 200) {
        const body = await res.json();
        const data = body.Data ?? body.data ?? body;
        const items = Array.isArray(data) ? data : (data?.items ?? data?.Items ?? []);
        expect(items.length, 'Tenant breach: foreign dept returned data').toBe(0);
      } else {
        expect([400, 401, 403, 404]).toContain(res.status());
      }
    });

    test('forged department on invoices — NEVER returns foreign data', async ({ page, request }) => {
      await loginViaUi(page);
      await expectAuthenticatedShell(page, { timeout: TIMEOUT });

      const token = await extractToken(page);
      if (!token) { test.skip(); return; }

      const res = await apiGet(request, '/billing/invoices', {
        ...authHeaders(token),
        'X-Department-Id': FAKE_DEPT,
        'X-Company-Id': FAKE_COMPANY,
      });

      if (res.status() === 200) {
        const body = await res.json();
        const data = body.Data ?? body.data ?? body;
        const items = Array.isArray(data) ? data : (data?.items ?? data?.Items ?? []);
        expect(items.length, 'Tenant breach: foreign dept returned invoices').toBe(0);
      } else {
        expect([400, 401, 403, 404]).toContain(res.status());
      }
    });

    test('forged department on inventory — NEVER returns foreign data', async ({ page, request }) => {
      await loginViaUi(page);
      await expectAuthenticatedShell(page, { timeout: TIMEOUT });

      const token = await extractToken(page);
      if (!token) { test.skip(); return; }

      const res = await apiGet(request, '/inventory/stock', {
        ...authHeaders(token),
        'X-Department-Id': FAKE_DEPT,
        'X-Company-Id': FAKE_COMPANY,
      });

      if (res.status() === 200) {
        const body = await res.json();
        const data = body.Data ?? body.data ?? body;
        const items = Array.isArray(data) ? data : (data?.items ?? data?.Items ?? []);
        expect(items.length, 'Tenant breach: foreign dept returned stock').toBe(0);
      } else {
        expect([400, 401, 403, 404]).toContain(res.status());
      }
    });

    test('direct entity access with foreign ID — NEVER 200', async ({ page, request }) => {
      await loginViaUi(page);
      await expectAuthenticatedShell(page, { timeout: TIMEOUT });

      const token = await extractToken(page);
      if (!token) { test.skip(); return; }

      const endpoints = [
        `/orders/${FAKE_UUID}`,
        `/billing/invoices/${FAKE_UUID}`,
        `/inventory/materials/${FAKE_UUID}`,
      ];

      for (const ep of endpoints) {
        const res = await apiGet(request, ep, {
          ...authHeaders(token),
          'X-Department-Id': FAKE_DEPT,
        });
        expect(
          [400, 401, 403, 404].includes(res.status()),
          `${ep} returned ${res.status()} with forged context — NEVER 200`
        ).toBeTruthy();
      }
    });
  });

  test.describe('Tampered request payloads', () => {
    test.beforeEach(async () => {
      if (!hasAuthCredentials()) test.skip();
    });

    test('order creation with injected HTML/script — sanitized or rejected', async ({ page, request }) => {
      await loginViaUi(page);
      await expectAuthenticatedShell(page, { timeout: TIMEOUT });

      const token = await extractToken(page);
      if (!token) { test.skip(); return; }

      const res = await apiPost(request, '/orders', {
        serviceId: '<script>alert("xss")</script>',
        customerName: '"><img src=x onerror=alert(1)>',
        addressLine1: "'; DROP TABLE orders; --",
        city: 'TestCity',
        state: 'TS',
        postcode: '00000',
        appointmentDate: new Date().toISOString().split('T')[0],
        appointmentWindowFrom: '00:00:00',
        appointmentWindowTo: '23:59:59',
      }, authHeaders(token));

      expect(res.status()).not.toBe(500);

      if (res.ok()) {
        const body = await res.json();
        const data = (body.Data ?? body.data ?? body) as Record<string, unknown>;
        const name = String(data.customerName ?? data.CustomerName ?? '');
        expect(name).not.toContain('<script>');
        expect(name).not.toContain('onerror=');
      }
    });

    test('invoice creation with SQL injection payload — rejected or sanitized', async ({ page, request }) => {
      await loginViaUi(page);
      await expectAuthenticatedShell(page, { timeout: TIMEOUT });

      const token = await extractToken(page);
      if (!token) { test.skip(); return; }

      const res = await apiPost(request, '/billing/invoices', {
        partnerId: "'; DROP TABLE invoices; --",
        invoiceDate: 'not-a-date',
        lineItems: [{ description: '<script>alert(1)</script>', quantity: -1, unitPrice: -100 }],
      }, authHeaders(token));

      expect(res.status()).not.toBe(500);
    });

    test('oversized request payload — does not crash server', async ({ page, request }) => {
      await loginViaUi(page);
      await expectAuthenticatedShell(page, { timeout: TIMEOUT });

      const token = await extractToken(page);
      if (!token) { test.skip(); return; }

      const largePayload = {
        serviceId: 'A'.repeat(10000),
        customerName: 'B'.repeat(10000),
        addressLine1: 'C'.repeat(10000),
      };

      const res = await apiPost(request, '/orders', largePayload, authHeaders(token));
      expect(res.status()).not.toBe(500);
    });
  });

  test.describe('Write operation protection', () => {
    test('DELETE on orders without auth — rejected', async ({ request }) => {
      const res = await apiDelete(request, `/orders/${FAKE_UUID}`);
      expect([401, 403, 404, 405]).toContain(res.status());
    });

    test('PUT on orders without auth — rejected', async ({ request }) => {
      const res = await apiPut(request, `/orders/${FAKE_UUID}`, { status: 'Completed' });
      expect([401, 403, 404, 405]).toContain(res.status());
    });

    test('POST payment without auth — rejected', async ({ request }) => {
      const res = await apiPost(request, '/billing/payments', {
        amount: 99999,
        paymentType: 'Income',
      });
      expect([401, 403]).toContain(res.status());
    });
  });
});

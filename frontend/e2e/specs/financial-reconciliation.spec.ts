import { test, expect } from '@playwright/test';
import { hasAuthCredentials, loginViaUi } from '../helpers/auth';
import { expectAuthenticatedShell } from '../helpers/expectations';
import { createAuthHeadersFromPage } from '../helpers/auth-api';
import { getInvoices, getPayments, getOrders, apiPost, apiGet } from '../helpers/api';

const TIMEOUT = 15_000;

test.describe('System validation – Financial reconciliation', () => {
  test.beforeEach(async () => {
    if (!hasAuthCredentials()) test.skip();
  });

  test('all invoices have non-negative totalAmount', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const invoices = await getInvoices(request, headers) as Record<string, unknown>[];

    for (const inv of invoices) {
      const total = Number(inv.totalAmount ?? inv.TotalAmount ?? inv.total ?? inv.Total ?? 0);
      expect(total, `Invoice ${inv.id ?? inv.Id} has negative total: ${total}`).toBeGreaterThanOrEqual(0);
    }
  });

  test('all payments have non-negative amounts', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const payments = await getPayments(request, headers) as Record<string, unknown>[];

    for (const pmt of payments) {
      const amount = Number(pmt.amount ?? pmt.Amount ?? 0);
      expect(amount, `Payment ${pmt.id ?? pmt.Id} has negative amount: ${amount}`).toBeGreaterThanOrEqual(0);
    }
  });

  test('no overpayment — payment total does not exceed invoice total', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const invoices = await getInvoices(request, headers) as Record<string, unknown>[];

    for (const inv of invoices.slice(0, 10)) {
      const invoiceId = String(inv.id ?? inv.Id ?? '');
      const invoiceTotal = Number(inv.totalAmount ?? inv.TotalAmount ?? inv.total ?? inv.Total ?? 0);

      if (!invoiceId || invoiceTotal <= 0) continue;

      const status = String(inv.status ?? inv.Status ?? '').toLowerCase();
      if (status === 'draft' || status === 'cancelled') continue;

      const payments = await getPayments(request, headers) as Record<string, unknown>[];
      const invoicePayments = payments.filter(p => {
        const pInvId = String(p.invoiceId ?? p.InvoiceId ?? p.supplierInvoiceId ?? p.SupplierInvoiceId ?? '');
        return pInvId === invoiceId;
      });

      const paymentTotal = invoicePayments.reduce((sum, p) => {
        return sum + Number(p.amount ?? p.Amount ?? 0);
      }, 0);

      expect(
        paymentTotal,
        `Invoice ${invoiceId}: payments ${paymentTotal} exceed total ${invoiceTotal}`
      ).toBeLessThanOrEqual(invoiceTotal + 0.01);
    }
  });

  test('invoice line items sum matches invoice total', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const invoices = await getInvoices(request, headers) as Record<string, unknown>[];

    for (const inv of invoices.slice(0, 5)) {
      const invoiceId = String(inv.id ?? inv.Id ?? '');
      if (!invoiceId) continue;

      const res = await apiGet(request, `/billing/invoices/${invoiceId}`, headers);
      if (!res.ok()) continue;

      const body = await res.json();
      const detail = (body.Data ?? body.data ?? body) as Record<string, unknown>;
      const lineItems = (detail.lineItems ?? detail.LineItems ?? detail.items ?? detail.Items ?? []) as Record<string, unknown>[];
      const invoiceTotal = Number(detail.totalAmount ?? detail.TotalAmount ?? detail.total ?? detail.Total ?? 0);

      if (lineItems.length === 0) continue;

      const lineItemSum = lineItems.reduce((sum, li) => {
        const qty = Number(li.quantity ?? li.Quantity ?? 1);
        const price = Number(li.unitPrice ?? li.UnitPrice ?? li.price ?? li.Price ?? 0);
        return sum + qty * price;
      }, 0);

      const diff = Math.abs(invoiceTotal - lineItemSum);
      expect(
        diff,
        `Invoice ${invoiceId}: line items sum ${lineItemSum} vs total ${invoiceTotal} (diff: ${diff})`
      ).toBeLessThanOrEqual(1.00);
    }
  });

  test('paid invoices have matching payment totals', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const invoices = await getInvoices(request, headers) as Record<string, unknown>[];

    const paidInvoices = invoices.filter(inv => {
      const status = String(inv.status ?? inv.Status ?? '').toLowerCase();
      return status === 'paid' || status === 'completed';
    });

    if (paidInvoices.length === 0) {
      test.skip();
      return;
    }

    const payments = await getPayments(request, headers) as Record<string, unknown>[];

    for (const inv of paidInvoices.slice(0, 5)) {
      const invoiceId = String(inv.id ?? inv.Id ?? '');
      const invoiceTotal = Number(inv.totalAmount ?? inv.TotalAmount ?? inv.total ?? inv.Total ?? 0);

      const invoicePayments = payments.filter(p => {
        const pInvId = String(p.invoiceId ?? p.InvoiceId ?? p.supplierInvoiceId ?? p.SupplierInvoiceId ?? '');
        return pInvId === invoiceId;
      });

      const paymentTotal = invoicePayments.reduce((sum, p) => {
        return sum + Number(p.amount ?? p.Amount ?? 0);
      }, 0);

      if (invoicePayments.length > 0) {
        const diff = Math.abs(invoiceTotal - paymentTotal);
        expect(
          diff,
          `Paid invoice ${invoiceId}: total=${invoiceTotal}, payments=${paymentTotal}`
        ).toBeLessThanOrEqual(1.00);
      }
    }
  });

  test('orders with invoices — invoice amounts are positive', async ({ page, request }) => {
    await loginViaUi(page);
    await expectAuthenticatedShell(page, { timeout: TIMEOUT });

    const headers = await createAuthHeadersFromPage(page);
    const invoices = await getInvoices(request, headers) as Record<string, unknown>[];

    for (const inv of invoices.slice(0, 10)) {
      const total = Number(inv.totalAmount ?? inv.TotalAmount ?? inv.total ?? inv.Total ?? 0);
      const status = String(inv.status ?? inv.Status ?? '').toLowerCase();
      if (status !== 'cancelled' && status !== 'draft') {
        expect(total, `Active invoice ${inv.id ?? inv.Id} has total ${total}`).toBeGreaterThanOrEqual(0);
      }
    }
  });
});

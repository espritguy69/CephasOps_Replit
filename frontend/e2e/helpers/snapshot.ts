import type { APIRequestContext } from '@playwright/test';
import { getOrders, getInvoices, getPayments, getStock } from './api';

export interface DataSnapshot {
  orderCount: number;
  invoiceCount: number;
  paymentCount: number;
  stockItemCount: number;
  orderIds: Set<string>;
  invoiceIds: Set<string>;
  paymentIds: Set<string>;
  capturedAt: number;
}

function extractIds(items: unknown[]): Set<string> {
  const ids = new Set<string>();
  for (const item of items) {
    if (item && typeof item === 'object') {
      const obj = item as Record<string, unknown>;
      const id = String(obj.id ?? obj.Id ?? obj.orderId ?? obj.OrderId ?? '');
      if (id) ids.add(id);
    }
  }
  return ids;
}

export async function captureSnapshot(
  request: APIRequestContext,
  headers: Record<string, string>
): Promise<DataSnapshot> {
  const [orders, invoices, payments, stock] = await Promise.all([
    getOrders(request, headers),
    getInvoices(request, headers),
    getPayments(request, headers),
    getStock(request, headers),
  ]);

  return {
    orderCount: orders.length,
    invoiceCount: invoices.length,
    paymentCount: payments.length,
    stockItemCount: stock.length,
    orderIds: extractIds(orders),
    invoiceIds: extractIds(invoices),
    paymentIds: extractIds(payments),
    capturedAt: Date.now(),
  };
}

export interface SnapshotDiff {
  ordersAdded: number;
  ordersRemoved: number;
  invoicesAdded: number;
  invoicesRemoved: number;
  paymentsAdded: number;
  paymentsRemoved: number;
  stockItemDelta: number;
  unexpectedDeletions: string[];
  durationMs: number;
}

export function diffSnapshots(before: DataSnapshot, after: DataSnapshot): SnapshotDiff {
  const removedOrders: string[] = [];
  for (const id of before.orderIds) {
    if (!after.orderIds.has(id)) removedOrders.push(`order:${id}`);
  }
  const removedInvoices: string[] = [];
  for (const id of before.invoiceIds) {
    if (!after.invoiceIds.has(id)) removedInvoices.push(`invoice:${id}`);
  }
  const removedPayments: string[] = [];
  for (const id of before.paymentIds) {
    if (!after.paymentIds.has(id)) removedPayments.push(`payment:${id}`);
  }

  let addedOrders = 0;
  for (const id of after.orderIds) {
    if (!before.orderIds.has(id)) addedOrders++;
  }
  let addedInvoices = 0;
  for (const id of after.invoiceIds) {
    if (!before.invoiceIds.has(id)) addedInvoices++;
  }
  let addedPayments = 0;
  for (const id of after.paymentIds) {
    if (!before.paymentIds.has(id)) addedPayments++;
  }

  return {
    ordersAdded: addedOrders,
    ordersRemoved: removedOrders.length,
    invoicesAdded: addedInvoices,
    invoicesRemoved: removedInvoices.length,
    paymentsAdded: addedPayments,
    paymentsRemoved: removedPayments.length,
    stockItemDelta: after.stockItemCount - before.stockItemCount,
    unexpectedDeletions: [...removedOrders, ...removedInvoices, ...removedPayments],
    durationMs: after.capturedAt - before.capturedAt,
  };
}

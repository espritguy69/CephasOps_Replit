import type { APIRequestContext, APIResponse } from '@playwright/test';
import { expect } from '@playwright/test';
import { e2eEnv } from './env';

function getApiBase(): string {
  const raw = e2eEnv.apiBaseUrl();
  return raw.replace(/\/+$/, '');
}

function fullUrl(path: string): string {
  const base = getApiBase();
  const clean = path.startsWith('/') ? path : `/${path}`;
  const prefix = base.endsWith('/api') ? '' : '/api';
  return `${base}${prefix}${clean}`;
}

export async function apiGet(
  request: APIRequestContext,
  path: string,
  headers?: Record<string, string>
): Promise<APIResponse> {
  const res = await request.get(fullUrl(path), { headers });
  return res;
}

export async function apiPost(
  request: APIRequestContext,
  path: string,
  body?: unknown,
  headers?: Record<string, string>
): Promise<APIResponse> {
  const res = await request.post(fullUrl(path), {
    data: body,
    headers: { 'Content-Type': 'application/json', ...headers },
  });
  return res;
}

export async function apiPut(
  request: APIRequestContext,
  path: string,
  body?: unknown,
  headers?: Record<string, string>
): Promise<APIResponse> {
  const res = await request.put(fullUrl(path), {
    data: body,
    headers: { 'Content-Type': 'application/json', ...headers },
  });
  return res;
}

export async function apiDelete(
  request: APIRequestContext,
  path: string,
  headers?: Record<string, string>
): Promise<APIResponse> {
  const res = await request.delete(fullUrl(path), { headers });
  return res;
}

function unwrapBody(body: unknown): unknown {
  if (body && typeof body === 'object') {
    const obj = body as Record<string, unknown>;
    if ('Data' in obj) return obj.Data;
    if ('data' in obj) return obj.data;
  }
  return body;
}

function unwrapList(body: unknown): unknown[] {
  const data = unwrapBody(body);
  if (Array.isArray(data)) return data;
  if (data && typeof data === 'object') {
    const obj = data as Record<string, unknown>;
    const inner = obj.items ?? obj.Items ?? obj.results ?? obj.Results ?? [];
    return Array.isArray(inner) ? inner : [];
  }
  return [];
}

export async function getOrders(request: APIRequestContext, headers?: Record<string, string>) {
  const res = await apiGet(request, '/orders', headers);
  expect(res.ok(), `GET /orders failed: ${res.status()}`).toBeTruthy();
  return unwrapList(await res.json());
}

export async function getOrder(request: APIRequestContext, orderId: string, headers?: Record<string, string>) {
  const res = await apiGet(request, `/orders/${orderId}`, headers);
  expect(res.ok(), `GET /orders/${orderId} failed: ${res.status()}`).toBeTruthy();
  return unwrapBody(await res.json()) as Record<string, unknown>;
}

export async function getInvoices(request: APIRequestContext, headers?: Record<string, string>) {
  const res = await apiGet(request, '/billing/invoices', headers);
  expect(res.ok(), `GET /billing/invoices failed: ${res.status()}`).toBeTruthy();
  return unwrapList(await res.json());
}

export async function getInvoice(request: APIRequestContext, invoiceId: string, headers?: Record<string, string>) {
  const res = await apiGet(request, `/billing/invoices/${invoiceId}`, headers);
  expect(res.ok(), `GET /billing/invoices/${invoiceId} failed: ${res.status()}`).toBeTruthy();
  return unwrapBody(await res.json()) as Record<string, unknown>;
}

export async function getPayments(request: APIRequestContext, headers?: Record<string, string>) {
  const res = await apiGet(request, '/billing/payments', headers);
  expect(res.ok(), `GET /billing/payments failed: ${res.status()}`).toBeTruthy();
  return unwrapList(await res.json());
}

export async function getStock(request: APIRequestContext, headers?: Record<string, string>) {
  const res = await apiGet(request, '/inventory/stock', headers);
  expect(res.ok(), `GET /inventory/stock failed: ${res.status()}`).toBeTruthy();
  return unwrapList(await res.json());
}

export async function getStockMovements(request: APIRequestContext, headers?: Record<string, string>) {
  const res = await apiGet(request, '/inventory/stock/movements', headers);
  expect(res.ok(), `GET /inventory/stock/movements failed: ${res.status()}`).toBeTruthy();
  return unwrapList(await res.json());
}

export async function getMaterials(request: APIRequestContext, headers?: Record<string, string>) {
  const res = await apiGet(request, '/inventory/materials', headers);
  expect(res.ok(), `GET /inventory/materials failed: ${res.status()}`).toBeTruthy();
  return unwrapList(await res.json());
}

export async function getUser(request: APIRequestContext, headers?: Record<string, string>) {
  const res = await apiGet(request, '/auth/me', headers);
  expect(res.ok(), `GET /auth/me failed: ${res.status()}`).toBeTruthy();
  return unwrapBody(await res.json()) as Record<string, unknown>;
}

export async function getTenantContext(request: APIRequestContext, headers?: Record<string, string>) {
  const res = await apiGet(request, '/auth/me', headers);
  expect(res.ok(), `GET /auth/me (tenant context) failed: ${res.status()}`).toBeTruthy();
  const user = unwrapBody(await res.json()) as Record<string, unknown>;
  return {
    userId: user.id ?? user.Id ?? user.userId,
    companyId: user.companyId ?? user.CompanyId,
    departmentId: user.departmentId ?? user.DepartmentId,
    role: user.role ?? user.Role ?? user.roles,
  };
}

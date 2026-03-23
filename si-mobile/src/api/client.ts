/**
 * API client for SI Mobile.
 * Reuses same base URL and envelope handling as frontend-si.
 */
import Constants from 'expo-constants';

const API_BASE_URL =
  (Constants.expoConfig?.extra as { apiBaseUrl?: string } | undefined)?.apiBaseUrl ??
  'http://localhost:5000/api';

export type AuthTokenGetter = () => Promise<string | null>;
let getAuthTokenFn: AuthTokenGetter | null = null;

export function setAuthTokenGetter(fn: AuthTokenGetter): void {
  getAuthTokenFn = fn;
}

async function getAuthToken(): Promise<string | null> {
  if (getAuthTokenFn) {
    try {
      return await getAuthTokenFn();
    } catch {
      return null;
    }
  }
  return null;
}

export interface ApiClientConfig {
  params?: Record<string, string | number | undefined | null> | Record<string, unknown>;
  headers?: Record<string, string>;
  skipAuth?: boolean;
}

export interface ApiError extends Error {
  status?: number;
  data?: unknown;
}

function buildUrl(url: string, params?: ApiClientConfig['params']): string {
  if (!params || Object.keys(params).length === 0) return url;
  const search = new URLSearchParams();
  Object.entries(params).forEach(([k, v]) => {
    if (v !== undefined && v !== null && v !== '') search.append(k, String(v));
  });
  const q = search.toString();
  return q ? `${url}?${q}` : url;
}

function unwrap<T>(raw: unknown): T {
  if (raw && typeof raw === 'object' && 'success' in raw) {
    const r = raw as { success?: boolean; data?: T; message?: string; errors?: string[] };
    if (!r.success) {
      const err: ApiError = new Error(r.message || r.errors?.join(', ') || 'API request failed');
      err.data = raw;
      throw err;
    }
    return r.data as T;
  }
  return raw as T;
}

async function handleError(res: Response): Promise<void> {
  if (!res.ok) {
    let message = `API Error: ${res.status} ${res.statusText}`;
    let data: unknown = null;
    const contentType = res.headers.get('content-type');
    if (contentType?.includes('application/json')) {
      try {
        data = await res.json();
        const d = data as { message?: string; errors?: string[] };
        message = d.message || d.errors?.join(', ') || message;
      } catch {
        // ignore
      }
    }
    const err: ApiError = new Error(message);
    err.status = res.status;
    err.data = data;
    throw err;
  }
}

async function headers(skipAuth = false): Promise<Record<string, string>> {
  const h: Record<string, string> = { 'Content-Type': 'application/json' };
  if (!skipAuth) {
    const token = await getAuthToken();
    if (token) h.Authorization = `Bearer ${token}`;
  }
  return h;
}

export const apiClient = {
  async get<T>(path: string, config: ApiClientConfig = {}): Promise<T> {
    const url = buildUrl(`${API_BASE_URL}${path}`, config.params);
    const res = await fetch(url, { method: 'GET', headers: await headers(config.skipAuth) });
    await handleError(res);
    const json = await res.json();
    return unwrap<T>(json);
  },
  async post<T>(path: string, data?: unknown, config: ApiClientConfig = {}): Promise<T> {
    const res = await fetch(`${API_BASE_URL}${path}`, {
      method: 'POST',
      headers: await headers(config.skipAuth),
      body: data ? JSON.stringify(data) : undefined,
    });
    await handleError(res);
    const json = await res.json();
    return unwrap<T>(json);
  },
  async patch<T>(path: string, data?: unknown, config: ApiClientConfig = {}): Promise<T> {
    const res = await fetch(`${API_BASE_URL}${path}`, {
      method: 'PATCH',
      headers: await headers(config.skipAuth),
      body: data ? JSON.stringify(data) : undefined,
    });
    await handleError(res);
    const json = await res.json();
    return unwrap<T>(json);
  },
};

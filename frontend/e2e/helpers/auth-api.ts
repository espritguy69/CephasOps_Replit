import type { Page, APIRequestContext, BrowserContext } from '@playwright/test';
import { request as playwrightRequest } from '@playwright/test';
import { e2eEnv } from './env';

export async function extractToken(page: Page): Promise<string> {
  const cookies = await page.context().cookies();
  const tokenCookie = cookies.find(c => c.name.toLowerCase().includes('token'));
  if (tokenCookie) return tokenCookie.value;

  const token = await page.evaluate(() => {
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

  return token || '';
}

export async function createAuthRequest(page: Page): Promise<APIRequestContext> {
  const token = await extractToken(page);
  if (!token) {
    throw new Error('No auth token found in browser context. Ensure loginViaUi was called first.');
  }

  return playwrightRequest.newContext({
    baseURL: e2eEnv.apiBaseUrl(),
    extraHTTPHeaders: {
      Authorization: `Bearer ${token}`,
    },
  });
}

export function authHeaders(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}` };
}

export async function createAuthHeadersFromPage(page: Page): Promise<Record<string, string>> {
  const token = await extractToken(page);
  if (!token) {
    throw new Error('No auth token found in browser context.');
  }
  return authHeaders(token);
}

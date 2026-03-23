/**
 * E2E environment variables. Prefer TEST_* for new code; E2E_TEST_USER_* kept for compatibility.
 */
function getEnv(key: string): string | undefined {
  return process.env[key];
}

export const e2eEnv = {
  baseUrl: () => getEnv('PLAYWRIGHT_BASE_URL') ?? 'http://localhost:5173',
  apiBaseUrl: () => getEnv('PLAYWRIGHT_API_BASE_URL') ?? 'http://localhost:5000',
  /** Email for E2E login (TEST_EMAIL or E2E_TEST_USER_EMAIL) */
  email: () => getEnv('TEST_EMAIL') ?? getEnv('E2E_TEST_USER_EMAIL'),
  /** Password for E2E login (TEST_PASSWORD or E2E_TEST_USER_PASSWORD) */
  password: () => getEnv('TEST_PASSWORD') ?? getEnv('E2E_TEST_USER_PASSWORD'),
  /** Optional admin user for role-aware tests */
  adminEmail: () => getEnv('ADMIN_TEST_EMAIL') ?? getEnv('E2E_TEST_USER_EMAIL'),
  adminPassword: () => getEnv('ADMIN_TEST_PASSWORD') ?? getEnv('E2E_TEST_USER_PASSWORD'),
  /** Optional tenant/company identifier if app uses it in URLs */
  tenantSlug: () => getEnv('TEST_TENANT_SLUG') ?? getEnv('TEST_COMPANY_CODE'),
};

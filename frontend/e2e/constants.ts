/**
 * Centralized E2E route and selector constants. Use these in specs and helpers
 * to keep assumptions in one place.
 */

/** Key app routes (for navigation and redirect assertions) */
export const ROUTES = {
  LOGIN: '/login',
  DASHBOARD: '/dashboard',
  ORDERS: '/orders',
  SCHEDULER_TIMELINE: '/scheduler/timeline',
  INVENTORY_STOCK_SUMMARY: '/inventory/stock-summary',
  REPORTS: '/reports',
  SETTINGS_COMPANY: '/settings/company',
  PAYROLL_PERIODS: '/payroll/periods',
  PNL_SUMMARY: '/pnl/summary',
  ACCOUNTING: '/accounting',
  ADMIN_BACKGROUND_JOBS: '/admin/background-jobs',
  /** Protected route used to assert guest redirect */
  PROTECTED_EXAMPLE: '/dashboard',
} as const;

/** data-testid values added for stable E2E selectors. Do not change without updating tests. */
export const TEST_IDS = {
  APP_SHELL: 'app-shell',
  APP_SHELL_MAIN: 'app-shell-main',
  SIDEBAR: 'sidebar',
  USER_MENU_TRIGGER: 'user-menu-trigger',
  LOGOUT_ACTION: 'logout-action',
  /** Desktop only (lg breakpoint): TopNav hides this on small viewports. E2E uses Desktop Chrome. */
  DEPARTMENT_SELECTOR_TRIGGER: 'department-selector-trigger',
  SCHEDULER_TIMELINE_ROOT: 'scheduler-timeline-root',
  ORDERS_PAGE_ROOT: 'orders-page-root',
  INVENTORY_STOCK_SUMMARY_ROOT: 'inventory-stock-summary-root',
  REPORTS_HUB_ROOT: 'reports-hub-root',
  SETTINGS_COMPANY_ROOT: 'settings-company-root',
  PAYROLL_PERIODS_ROOT: 'payroll-periods-root',
  PNL_SUMMARY_ROOT: 'pnl-summary-root',
  ACCOUNTING_DASHBOARD_ROOT: 'accounting-dashboard-root',
  /** Reserved for future tenant/company switcher */
  TENANT_SWITCHER: 'tenant-switcher',
} as const;

/** Selector assumptions (role/label) when testid not used */
export const SELECTORS = {
  SIGN_IN_BUTTON: /sign in|login/i,
  LOGIN_EMAIL_LABEL: /email/i,
  LOGIN_PASSWORD_LABEL: /password/i,
  SIGN_IN_TO_ACCOUNT_TEXT: /sign in to your account/i,
  DASHBOARD_HEADING: /dashboard|operations/i,
  CEPHASOPS_HEADING: /cephasops|sign in/i,
} as const;

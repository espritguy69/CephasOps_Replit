import type { Page } from '@playwright/test';
import { TEST_IDS } from '../constants';

/**
 * Shared app shell (sidebar/top nav) after login. Use for navigation and logout.
 */
export class AppShell {
  constructor(private readonly page: Page) {}

  /** Main content area (after login) */
  get mainContent() {
    return this.page.getByTestId(TEST_IDS.APP_SHELL_MAIN);
  }

  /** User menu trigger – stable testid */
  get userMenuTrigger() {
    return this.page.getByTestId(TEST_IDS.USER_MENU_TRIGGER);
  }

  /** Logout action in dropdown – stable testid */
  get logoutButton() {
    return this.page.getByTestId(TEST_IDS.LOGOUT_ACTION);
  }

  /** Sidebar – stable testid */
  get sidebar() {
    return this.page.getByTestId(TEST_IDS.SIDEBAR);
  }

  /** Department selector trigger – stable testid. Desktop only (lg); not visible on small viewports. */
  get departmentSelectorTrigger() {
    return this.page.getByTestId(TEST_IDS.DEPARTMENT_SELECTOR_TRIGGER);
  }

  async openUserMenu(): Promise<void> {
    await this.userMenuTrigger.click();
  }

  async logout(): Promise<void> {
    await this.openUserMenu();
    await this.logoutButton.click();
  }

  /** Navigate to a path (uses baseURL) */
  async goto(path: string): Promise<void> {
    await this.page.goto(path);
  }
}

import type { Page } from '@playwright/test';
import { ROUTES, SELECTORS } from '../constants';

/**
 * Lightweight page object for the login screen. Use getByRole/getByLabel for stability.
 */
export class LoginPage {
  constructor(private readonly page: Page) {}

  async goto(): Promise<void> {
    await this.page.goto(ROUTES.LOGIN);
  }

  get emailInput() {
    return this.page.getByLabel(SELECTORS.LOGIN_EMAIL_LABEL);
  }

  get passwordInput() {
    return this.page.getByLabel(SELECTORS.LOGIN_PASSWORD_LABEL);
  }

  get signInButton() {
    return this.page.getByRole('button', { name: SELECTORS.SIGN_IN_BUTTON });
  }

  get forgotPasswordLink() {
    return this.page.getByRole('button', { name: /forgot your password/i }).or(
      this.page.getByText(/forgot your password/i)
    );
  }

  get errorAlert() {
    return this.page.getByRole('alert');
  }

  async fillAndSubmit(email: string, password: string): Promise<void> {
    await this.emailInput.waitFor({ state: 'visible', timeout: 15_000 });
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.signInButton.click();
  }

  async expectVisible(options?: { timeout?: number }): Promise<void> {
    const timeout = options?.timeout ?? 10_000;
    await this.signInButton.waitFor({ state: 'visible', timeout });
  }
}

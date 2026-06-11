import type { Page } from '@playwright/test';
import { TestData } from '../test-data';

export class LoginPage {
  constructor(private readonly page: Page) {}

  async goto(tenantName?: string): Promise<void> {
    const loginUrl = tenantName
      ? `/Account/Login?__tenant=${encodeURIComponent(tenantName)}`
      : '/Account/Login';
    await this.page.goto(loginUrl);
    await this.page.waitForLoadState('domcontentloaded');
  }

  async login(username: string, password = TestData.password): Promise<void> {
    const loginInput = this.page
      .locator(
        'input[name="LoginInput.UserNameOrEmailAddress"], #LoginInput_UserNameOrEmailAddress, input[name="LoginInput"], #LoginInput',
      )
      .first();
    await loginInput.waitFor({ state: 'visible' });
    await loginInput.fill(username);

    const passwordInput = this.page.locator('input[type="password"], input[name="Password"]').first();
    await passwordInput.fill(password);

    await this.page.getByRole('button', { name: /log\s*in|sign\s*in/i }).click();
    await this.page.waitForURL((url) => !url.pathname.includes('/Account/Login'), { timeout: 90_000 });
  }
}

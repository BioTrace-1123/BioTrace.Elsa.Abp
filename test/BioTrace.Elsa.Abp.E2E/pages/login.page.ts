import type { Page } from '@playwright/test';
import { E2E_TIMEOUTS } from '../timeouts';
import { TestData } from '../test-data';

export class LoginPage {
  constructor(private readonly page: Page) {}

  async goto(tenantName?: string): Promise<void> {
    if (tenantName) {
      // Preserve OIDC ReturnUrl from the Studio redirect; bare ?__tenant= loses it and login lands on /.
      const loginUrl = new URL(this.page.url());
      loginUrl.searchParams.set('__tenant', tenantName);
      await this.page.goto(`${loginUrl.pathname}${loginUrl.search}`);
    } else {
      await this.page.goto('/Account/Login');
    }

    await this.page.waitForLoadState('domcontentloaded');
  }

  async login(username: string, password = TestData.password): Promise<void> {
    const loginInput = this.page
      .getByRole('textbox', { name: /username|email/i })
      .or(
        this.page.locator(
          'input[name="LoginInput.UserNameOrEmailAddress"], #LoginInput_UserNameOrEmailAddress, input[name="LoginInput"], #LoginInput',
        ),
      )
      .first();
    await loginInput.waitFor({ state: 'visible' });
    await loginInput.fill(username);

    const passwordInput = this.page
      .getByRole('textbox', { name: /password/i })
      .or(this.page.locator('input[type="password"], input[name="Password"]'))
      .first();
    await passwordInput.fill(password);

    await this.page.getByRole('button', { name: /log\s*in|sign\s*in/i }).click();
    await this.page.waitForURL(
      /\/studio\/authentication\/login-callback|\/studio\/workflows|\/studio$/,
      { timeout: E2E_TIMEOUTS.loginFlow },
    );
    await this.page
      .getByRole('link', { name: /workflows/i })
      .or(this.page.getByRole('button', { name: /workflows/i }))
      .first()
      .waitFor({ state: 'visible', timeout: E2E_TIMEOUTS.loginFlow });
  }
}

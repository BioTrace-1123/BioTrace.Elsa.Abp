import type { Page } from '@playwright/test';
import { waitForDefinitionsReady, waitForInstancesReady } from '../helpers/studio-ready';
import { TestData } from '../test-data';
import { LoginPage } from './login.page';

export class StudioShellPage {
  constructor(private readonly page: Page) {}

  async loginViaStudio(username: string, tenantName?: string): Promise<void> {
    const loginPage = new LoginPage(this.page);
    const loginInput = this.page
      .locator(
        'input[name="LoginInput.UserNameOrEmailAddress"], #LoginInput_UserNameOrEmailAddress, input[name="LoginInput"], #LoginInput',
      )
      .first();
    const workflowsLink = this.page.getByRole('link', { name: /workflows/i }).first();

    await this.page.goto(TestData.studioPath);
    await this.correctAuthenticationLoginPath();

    const needsLogin = await Promise.race([
      loginInput.waitFor({ state: 'visible', timeout: 90_000 }).then(() => true),
      workflowsLink.waitFor({ state: 'visible', timeout: 90_000 }).then(() => false),
    ]);

    if (needsLogin) {
      if (tenantName) {
        const loginUrl = new URL(this.page.url(), TestData.baseUrl);
        loginUrl.searchParams.set('__tenant', tenantName);
        await this.page.goto(`${loginUrl.pathname}?${loginUrl.searchParams.toString()}`);
        await loginInput.waitFor({ state: 'visible', timeout: 30_000 });
      }
      await loginPage.login(username);
    }

    await this.waitForStudioReady();
  }

  private async correctAuthenticationLoginPath(): Promise<void> {
    await this.page.waitForURL(/\/studio|\/authentication\/login|\/Account\/Login/, { timeout: 90_000 });

    // Studio SPA may client-navigate from /studio to the wrong /authentication/login after first paint.
    if (new URL(this.page.url()).pathname === '/studio') {
      await this.page
        .waitForURL(/\/authentication\/login|\/Account\/Login|\/studio\//, { timeout: 60_000 })
        .catch(() => undefined);
    }

    if (new URL(this.page.url()).pathname === '/authentication/login') {
      await this.page.goto(`${TestData.studioPath}/authentication/login`);
      await this.page.waitForURL(/\/Account\/Login|\/studio/, { timeout: 60_000 });
    }
  }

  async waitForStudioReady(): Promise<void> {
    await this.page.waitForURL(/\/studio/, { timeout: 90_000 });
    await this.page.locator('#app').waitFor({ state: 'attached', timeout: 90_000 });
    await this.page
      .getByRole('link', { name: /workflows/i })
      .or(this.page.getByRole('button', { name: /workflows/i }))
      .first()
      .waitFor({ state: 'visible', timeout: 90_000 });
  }

  async selectTenant(displayName: string): Promise<void> {
    // MudBlazor MudSelect does not expose a native <label>; open via the select control in the app bar.
    const tenantSelect = this.page.locator('.mud-select').first();
    await tenantSelect.waitFor({ state: 'visible', timeout: 30_000 });
    await tenantSelect.click();

    const definitionsResponse = this.page.waitForResponse(
      (response) => response.url().includes('/elsa/api/workflow-definitions') && response.ok(),
      { timeout: 60_000 },
    );
    // MudSelect renders items as paragraphs inside a popover, not native <option> elements.
    const tenantOption = this.page.locator('.mud-popover').getByText(displayName, { exact: true });
    await tenantOption.waitFor({ state: 'visible', timeout: 30_000 });
    await tenantOption.click();
    await definitionsResponse;
  }

  async gotoDefinitions(): Promise<void> {
    await this.page.goto(`${TestData.studioPath}/workflows/definitions`);

    const definitionsReady = await this.page
      .getByRole('button', { name: /create|new workflow/i })
      .or(this.page.getByRole('table'))
      .first()
      .isVisible()
      .catch(() => false);

    if (!definitionsReady) {
      await this.page.goto(TestData.studioPath);
      await this.waitForStudioReady();
      await this.page.goto(`${TestData.studioPath}/workflows/definitions`);
    }

    await this.waitForDefinitionsPage();
  }

  async gotoInstances(): Promise<void> {
    await this.page.goto(`${TestData.studioPath}/workflows/instances`);
    await waitForInstancesReady(this.page);
  }

  async waitForDefinitionsPage(): Promise<void> {
    await waitForDefinitionsReady(this.page);
  }
}

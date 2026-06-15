import type { Page } from '@playwright/test';
import { waitForDefinitionsReady, waitForInstancesReady } from '../helpers/studio-ready';
import { E2E_TIMEOUTS } from '../timeouts';
import { TestData } from '../test-data';
import { LoginPage } from './login.page';

export class StudioShellPage {
  constructor(private readonly page: Page) {}

  async loginViaStudio(username: string, tenantName?: string): Promise<void> {
    const loginPage = new LoginPage(this.page);

    await this.page.goto(`${TestData.studioPath}/authentication/login`);
    await this.page.locator('#app').waitFor({ state: 'attached', timeout: E2E_TIMEOUTS.wasmBoot });
    await this.page.waitForURL(/\/Account\/Login|\/studio\//, { timeout: E2E_TIMEOUTS.loginFlow });

    if (this.page.url().includes('/authentication/login')) {
      await this.page
        .waitForURL(/\/Account\/Login|\/studio\/authentication\/login-callback|\/studio\/workflows/, {
          timeout: E2E_TIMEOUTS.studioPage,
        })
        .catch(() => undefined);
    }

    if (this.page.url().includes('/Account/Login')) {
      if (tenantName && !this.page.url().includes('__tenant')) {
        await loginPage.goto(tenantName);
      }
      await loginPage.login(username);
    }

    await this.waitForStudioReady();
  }

  async waitForStudioReady(): Promise<void> {
    await this.page.waitForURL(/\/studio/, { timeout: E2E_TIMEOUTS.loginFlow });
    await this.page.locator('#app').waitFor({ state: 'attached', timeout: E2E_TIMEOUTS.wasmBoot });
    await this.page
      .getByRole('link', { name: /workflows/i })
      .or(this.page.getByRole('button', { name: /workflows/i }))
      .first()
      .waitFor({ state: 'visible', timeout: E2E_TIMEOUTS.loginFlow });
  }

  async selectTenant(displayName: string): Promise<void> {
    // Scope to the app-bar user menu (not the table rows-per-page MudSelect).
    const tenantSelect = this.page.locator('.d-flex.align-center.gap-2 .mud-select').first();
    await tenantSelect.waitFor({ state: 'visible', timeout: E2E_TIMEOUTS.ui });
    await tenantSelect.click();

    const definitionsResponse = this.page.waitForResponse(
      (response) => response.url().includes('/elsa/api/workflow-definitions') && response.ok(),
      { timeout: E2E_TIMEOUTS.studioPage },
    );
    // MudSelect renders items as paragraphs inside a popover, not native <option> elements.
    const tenantOption = this.page.locator('.mud-popover').getByText(displayName, { exact: true });
    await tenantOption.waitFor({ state: 'visible', timeout: E2E_TIMEOUTS.ui });
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

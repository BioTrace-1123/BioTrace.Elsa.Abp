import type { Page } from '@playwright/test';
import { TestData } from '../test-data';
import { LoginPage } from './login.page';

export class StudioShellPage {
  constructor(private readonly page: Page) {}

  async loginViaStudio(username: string, tenantName?: string): Promise<void> {
    await this.page.goto(TestData.studioPath);
    await this.page.waitForURL(/\/Account\/Login|\/studio/, { timeout: 60_000 });

    if (this.page.url().includes('/Account/Login')) {
      const loginPage = new LoginPage(this.page);
      if (tenantName && !this.page.url().includes('__tenant')) {
        await loginPage.goto(tenantName);
      }
      await loginPage.login(username);
    }

    await this.waitForStudioReady();
  }

  async waitForStudioReady(): Promise<void> {
    await this.page.waitForURL(/\/studio/, { timeout: 90_000 });
    await this.page.locator('#app').waitFor({ state: 'attached', timeout: 90_000 });
    await this.page.waitForLoadState('networkidle', { timeout: 90_000 }).catch(() => undefined);
    await this.page.getByRole('link', { name: /workflows/i }).first().waitFor({ state: 'visible', timeout: 90_000 });
  }

  async selectTenant(displayName: string): Promise<void> {
    const tenantSelect = this.page.getByLabel('Tenant');
    await tenantSelect.waitFor({ state: 'visible', timeout: 30_000 });
    await tenantSelect.click();
    await this.page.getByRole('option', { name: displayName }).click();
    await this.page.waitForLoadState('networkidle', { timeout: 60_000 }).catch(() => undefined);
  }

  async gotoDefinitions(): Promise<void> {
    await this.page.goto(`${TestData.studioPath}/workflows/definitions`);
    await this.waitForDefinitionsPage();
  }

  async gotoInstances(): Promise<void> {
    await this.page.goto(`${TestData.studioPath}/workflows/instances`);
    await this.page.waitForLoadState('networkidle', { timeout: 60_000 }).catch(() => undefined);
  }

  async waitForDefinitionsPage(): Promise<void> {
    await this.page.waitForURL(/\/studio\/workflows\/definitions/, { timeout: 60_000 });
    await this.page.waitForLoadState('networkidle', { timeout: 60_000 }).catch(() => undefined);
  }
}

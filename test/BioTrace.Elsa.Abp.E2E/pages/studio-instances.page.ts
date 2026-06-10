import type { Page } from '@playwright/test';

export class StudioInstancesPage {
  constructor(private readonly page: Page) {}

  async waitForLoaded(): Promise<void> {
    await this.page.waitForURL(/\/studio\/workflows\/instances/, { timeout: 60_000 });
    await this.page.waitForLoadState('networkidle', { timeout: 60_000 }).catch(() => undefined);
  }

  async expectInstanceWithStatus(definitionId: string, statusPattern: RegExp): Promise<void> {
    const row = this.page.getByRole('row').filter({ hasText: definitionId });
    await row.first().waitFor({ state: 'visible', timeout: 60_000 });
    await this.page.getByText(statusPattern).first().waitFor({ state: 'visible', timeout: 60_000 });
  }

  async waitForInstanceStatus(definitionId: string, statusPattern: RegExp, timeoutMs = 90_000): Promise<void> {
    const deadline = Date.now() + timeoutMs;
    while (Date.now() < deadline) {
      const row = this.page.getByRole('row').filter({ hasText: definitionId });
      if ((await row.count()) > 0) {
        const text = await row.first().innerText();
        if (statusPattern.test(text)) {
          return;
        }
      }

      await this.page.reload();
      await this.waitForLoaded();
      await this.page.waitForTimeout(2_000);
    }

    throw new Error(`Timed out waiting for instance of '${definitionId}' with status matching ${statusPattern}.`);
  }

  async cancelFirstRunningInstance(definitionId: string): Promise<void> {
    const row = this.page.getByRole('row').filter({ hasText: definitionId }).first();
    await row.waitFor({ state: 'visible', timeout: 60_000 });

    const cancelButton = row.getByRole('button', { name: /cancel/i }).or(this.page.getByRole('button', { name: /cancel/i }).first());
    await cancelButton.click();

    const confirmButton = this.page.getByRole('button', { name: /cancel|confirm|yes/i }).last();
    if (await confirmButton.isVisible().catch(() => false)) {
      await confirmButton.click();
    }
  }
}

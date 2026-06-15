import { expect, type Page } from '@playwright/test';
import { waitForInstancesReady } from '../helpers/studio-ready';
import { E2E_TIMEOUTS } from '../timeouts';

export class StudioInstancesPage {
  constructor(private readonly page: Page) {}

  async waitForLoaded(): Promise<void> {
    await waitForInstancesReady(this.page);
  }

  async expectInstanceWithStatus(definitionId: string, statusPattern: RegExp): Promise<void> {
    await this.expectInstanceVisible(definitionId, statusPattern);
  }

  async expectInstanceVisible(instanceId: string, statusPattern: RegExp): Promise<void> {
    const searchBox = this.page.getByRole('textbox', { name: /search on id/i });
    await searchBox.fill(instanceId);

    const row = this.page.getByRole('row').filter({ hasText: instanceId });
    await row.first().waitFor({ state: 'visible', timeout: E2E_TIMEOUTS.studioPage });
    await expect(row.first()).toContainText(statusPattern);
  }

  async waitForInstanceStatus(definitionId: string, statusPattern: RegExp, timeoutMs = E2E_TIMEOUTS.workflowPoll): Promise<void> {
    await expect
      .poll(
        async () => {
          const row = this.page.getByRole('row').filter({ hasText: definitionId });
          if ((await row.count()) > 0) {
            const text = await row.first().innerText();
            if (statusPattern.test(text)) {
              return true;
            }
          }

          await this.page.reload();
          await waitForInstancesReady(this.page);
          return false;
        },
        { timeout: timeoutMs, intervals: [2_000] },
      )
      .toBe(true);
  }

  async cancelFirstRunningInstance(instanceId: string): Promise<void> {
    const searchBox = this.page.getByRole('textbox', { name: /search on id/i });
    await searchBox.fill(instanceId);

    const row = this.page.getByRole('row').filter({ hasText: instanceId }).first();
    await row.waitFor({ state: 'visible', timeout: E2E_TIMEOUTS.studioPage });

    await row.locator('button').last().click();
    const cancelAction = this.page
      .getByRole('menuitem', { name: /cancel/i })
      .or(this.page.getByRole('listitem', { name: /cancel/i }))
      .or(this.page.getByText(/^cancel workflow$/i))
      .or(this.page.getByText(/^cancel$/i));
    await cancelAction.first().click();

    const confirmButton = this.page
      .getByRole('button', { name: /^(yes|confirm|cancel)$/i })
      .or(this.page.getByRole('menuitem', { name: /cancel/i }));
    if (await confirmButton.first().isVisible().catch(() => false)) {
      await confirmButton.first().click();
    }
  }
}

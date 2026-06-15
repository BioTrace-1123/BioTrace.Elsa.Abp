import { expect, type Page } from '@playwright/test';
import { waitForDefinitionsReady } from '../helpers/studio-ready';
import { E2E_TIMEOUTS } from '../timeouts';

export class StudioDefinitionsPage {
  constructor(private readonly page: Page) {}

  async waitForLoaded(): Promise<void> {
    await waitForDefinitionsReady(this.page);
  }

  async hasDefinition(definitionId: string): Promise<boolean> {
    const row = this.page.getByRole('row').filter({ hasText: definitionId });
    try {
      await row.first().waitFor({ state: 'visible', timeout: E2E_TIMEOUTS.uiShort });
      return true;
    } catch {
      return false;
    }
  }

  async expectDefinitionVisible(definitionId: string): Promise<void> {
    await this.page.getByText(definitionId, { exact: false }).first().waitFor({ state: 'visible', timeout: E2E_TIMEOUTS.ui });
  }

  async expectDefinitionHidden(definitionId: string): Promise<void> {
    await this.waitForLoaded();
    await expect
      .poll(async () => this.page.getByText(definitionId, { exact: false }).count(), { timeout: E2E_TIMEOUTS.uiShort })
      .toBe(0);
  }

  async clickCreateWorkflow(): Promise<void> {
    const createButton = this.page
      .getByRole('button', { name: /create|new workflow/i })
      .or(this.page.getByRole('link', { name: /create|new workflow/i }))
      .first();
    await createButton.waitFor({ state: 'visible', timeout: E2E_TIMEOUTS.ui });
    await createButton.click();
  }

  async fillCreateWorkflowDialog(name: string, description?: string): Promise<void> {
    const dialog = this.page.getByRole('dialog');
    await dialog.waitFor({ state: 'visible', timeout: E2E_TIMEOUTS.ui });

    const nameField = dialog.getByLabel(/name/i);
    await nameField.fill(name);

    if (description) {
      const descriptionField = dialog.getByLabel(/description/i);
      await descriptionField.fill(description);
    }

    const okButton = dialog.getByRole('button', { name: /^ok$/i });
    await okButton.click();
    await dialog.waitFor({ state: 'hidden', timeout: E2E_TIMEOUTS.dialogClose }).catch(() => undefined);
  }

  async isCreateWorkflowEnabled(): Promise<boolean> {
    const createButton = this.page
      .getByRole('button', { name: /create|new workflow/i })
      .or(this.page.getByRole('link', { name: /create|new workflow/i }))
      .first();

    try {
      await createButton.waitFor({ state: 'visible', timeout: E2E_TIMEOUTS.uiShort });
      return await createButton.isEnabled();
    } catch {
      return false;
    }
  }

  async openDefinition(definitionId: string): Promise<void> {
    const row = this.page.getByRole('row').filter({ hasText: definitionId }).first();
    await row.click();
    await this.page.waitForURL(/\/studio\/workflows\/definitions\//, { timeout: E2E_TIMEOUTS.studioPage });
  }

  async publishCurrentWorkflow(): Promise<void> {
    await this.page.locator('[role="progressbar"]').waitFor({ state: 'hidden', timeout: E2E_TIMEOUTS.workflowPoll }).catch(() => undefined);

    const publishButton = this.page
      .getByRole('button', { name: /publish/i })
      .or(this.page.locator('button[title*="Publish" i], button[aria-label*="Publish" i]'))
      .first();
    await publishButton.waitFor({ state: 'visible', timeout: E2E_TIMEOUTS.studioPage });
    await publishButton.click();

    const confirmButton = this.page.getByRole('button', { name: /publish|confirm|ok/i }).last();
    if (await confirmButton.isVisible().catch(() => false)) {
      await confirmButton.click();
    }
  }
}

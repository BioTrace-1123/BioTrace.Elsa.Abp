import type { Page } from '@playwright/test';

export class StudioDefinitionsPage {
  constructor(private readonly page: Page) {}

  async waitForLoaded(): Promise<void> {
    await this.page.waitForURL(/\/studio\/workflows\/definitions/, { timeout: 60_000 });
    await this.page.waitForLoadState('networkidle', { timeout: 60_000 }).catch(() => undefined);
  }

  async hasDefinition(definitionId: string): Promise<boolean> {
    const row = this.page.getByRole('row').filter({ hasText: definitionId });
    try {
      await row.first().waitFor({ state: 'visible', timeout: 15_000 });
      return true;
    } catch {
      return false;
    }
  }

  async expectDefinitionVisible(definitionId: string): Promise<void> {
    await this.page.getByText(definitionId, { exact: false }).first().waitFor({ state: 'visible', timeout: 30_000 });
  }

  async expectDefinitionHidden(definitionId: string): Promise<void> {
    await this.page.waitForTimeout(1_000);
    const matches = this.page.getByText(definitionId, { exact: false });
    const count = await matches.count();
    if (count > 0) {
      throw new Error(`Expected workflow definition '${definitionId}' to be hidden, but found ${count} match(es).`);
    }
  }

  async clickCreateWorkflow(): Promise<void> {
    const createButton = this.page
      .getByRole('button', { name: /create|new workflow/i })
      .or(this.page.getByRole('link', { name: /create|new workflow/i }))
      .first();
    await createButton.waitFor({ state: 'visible', timeout: 30_000 });
    await createButton.click();
  }

  async fillCreateWorkflowDialog(name: string, description?: string): Promise<void> {
    const dialog = this.page.getByRole('dialog');
    await dialog.waitFor({ state: 'visible', timeout: 30_000 });

    const nameField = dialog.getByLabel(/name/i);
    await nameField.fill(name);

    if (description) {
      const descriptionField = dialog.getByLabel(/description/i);
      await descriptionField.fill(description);
    }

    const okButton = dialog.getByRole('button', { name: /^ok$/i });
    await okButton.click();
    await dialog.waitFor({ state: 'hidden', timeout: 60_000 }).catch(() => undefined);
  }

  async isCreateWorkflowEnabled(): Promise<boolean> {
    const createButton = this.page
      .getByRole('button', { name: /create|new workflow/i })
      .or(this.page.getByRole('link', { name: /create|new workflow/i }))
      .first();

    try {
      await createButton.waitFor({ state: 'visible', timeout: 10_000 });
      return await createButton.isEnabled();
    } catch {
      return false;
    }
  }

  async openDefinition(definitionId: string): Promise<void> {
    const row = this.page.getByRole('row').filter({ hasText: definitionId }).first();
    await row.click();
    await this.page.waitForURL(/\/studio\/workflows\/definitions\//, { timeout: 60_000 });
  }

  async publishCurrentWorkflow(): Promise<void> {
    const publishButton = this.page.getByRole('button', { name: /publish/i }).first();
    await publishButton.waitFor({ state: 'visible', timeout: 30_000 });
    await publishButton.click();

    const confirmButton = this.page.getByRole('button', { name: /publish|confirm|ok/i }).last();
    if (await confirmButton.isVisible().catch(() => false)) {
      await confirmButton.click();
    }
  }
}

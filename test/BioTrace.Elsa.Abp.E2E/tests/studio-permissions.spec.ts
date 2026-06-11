import { test, expect, projectIs } from '../fixtures/auth.fixture';
import { TestData } from '../test-data';

test.describe('Studio permissions (designer)', () => {
  test.skip(() => !projectIs('chromium-tenant-a-designer'), 'Runs on tenant-a-designer project only.');

  test('tenant-a-designer can read definitions but cannot create workflows', async ({
    page,
    elsaApi,
    studioShell,
    studioDefinitions,
  }) => {
    await studioShell.gotoDefinitions();
    await studioDefinitions.waitForLoaded();
    await studioDefinitions.expectDefinitionVisible(TestData.workflows.demoTenantA);

    const currentUser = await elsaApi.getCurrentUser();
    expect(currentUser.permissions).toContain('read:workflow-definitions');
    expect(currentUser.permissions).not.toContain('*');
    expect(currentUser.permissions).not.toContain('write:workflow-definitions');

    await studioDefinitions.clickCreateWorkflow();
    const dialog = page.getByRole('dialog');
    await dialog.waitFor({ state: 'visible', timeout: 30_000 });
    await expect(dialog.getByText(/do not have permission to create workflow definitions/i)).toBeVisible();
    await expect(dialog.getByRole('button', { name: /^ok$/i })).toBeDisabled();
  });
});

test.describe('Studio permissions (admin)', () => {
  test.skip(() => !projectIs('chromium-tenant-a-admin'), 'Runs on tenant-a-admin project only.');

  test('tenant-a-admin can create workflow definitions', async ({ elsaApi, studioShell, studioDefinitions }) => {
    await studioShell.gotoDefinitions();
    await studioDefinitions.waitForLoaded();

    const currentUser = await elsaApi.getCurrentUser();
    expect(currentUser.permissions).toContain('write:workflow-definitions');

    const createEnabled = await studioDefinitions.isCreateWorkflowEnabled();
    expect(createEnabled).toBe(true);
  });
});

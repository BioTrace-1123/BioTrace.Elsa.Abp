import { test, expect, projectIs } from '../fixtures/auth.fixture';
import { createElsaApi } from '../helpers/elsa-api';
import { TestData } from '../test-data';

test.describe('Studio permissions', () => {
  test('tenant-a-designer can read definitions but cannot create workflows', async ({
    page,
    studioShell,
    studioDefinitions,
  }) => {
    test.skip(!projectIs('chromium-tenant-a-designer'), 'Runs on tenant-a-designer project only.');

    await studioShell.gotoDefinitions();
    await studioDefinitions.waitForLoaded();
    await studioDefinitions.expectDefinitionVisible(TestData.workflows.demoTenantA);

    const elsaApi = createElsaApi(page, TestData.tenants.tenantA);
    const currentUser = await elsaApi.getCurrentUser();
    expect(currentUser.permissions).toContain('read:workflow-definitions');
    expect(currentUser.permissions).not.toContain('*');
    expect(currentUser.permissions).not.toContain('write:workflow-definitions');

    const createEnabled = await studioDefinitions.isCreateWorkflowEnabled();
    expect(createEnabled).toBe(false);
  });

  test('tenant-a-admin can create workflow definitions', async ({ page, studioShell, studioDefinitions }) => {
    test.skip(!projectIs('chromium-tenant-a-admin'), 'Runs on tenant-a-admin project only.');

    await studioShell.gotoDefinitions();
    await studioDefinitions.waitForLoaded();

    const elsaApi = createElsaApi(page, TestData.tenants.tenantA);
    const currentUser = await elsaApi.getCurrentUser();
    expect(currentUser.permissions).toContain('write:workflow-definitions');

    const createEnabled = await studioDefinitions.isCreateWorkflowEnabled();
    expect(createEnabled).toBe(true);
  });
});

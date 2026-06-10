import { test, expect, projectIs } from '../fixtures/auth.fixture';
import { TestData } from '../test-data';

test.describe('Studio multi-tenancy', () => {
  test('tenant-a-admin sees DemoTenantAWorkflow', async ({ studioShell, studioDefinitions }) => {
    test.skip(!projectIs('chromium-tenant-a-admin'), 'Runs on tenant-a-admin project only.');

    await studioShell.gotoDefinitions();
    await studioDefinitions.expectDefinitionVisible(TestData.workflows.demoTenantA);
  });

  test('tenant-b-admin does not see DemoTenantAWorkflow', async ({ studioShell, studioDefinitions }) => {
    test.skip(!projectIs('chromium-tenant-b-admin'), 'Runs on tenant-b-admin project only.');

    await studioShell.gotoDefinitions();
    await studioDefinitions.expectDefinitionHidden(TestData.workflows.demoTenantA);
  });

  test('host admin can switch to Tenant A and see DemoTenantAWorkflow', async ({
    page,
    studioShell,
    studioDefinitions,
  }) => {
    test.skip(!projectIs('chromium-admin'), 'Runs on host admin project only.');

    await studioShell.gotoDefinitions();
    await studioShell.selectTenant(TestData.tenants.tenantADisplayName);
    await studioDefinitions.waitForLoaded();
    await studioDefinitions.expectDefinitionVisible(TestData.workflows.demoTenantA);

    await page.reload();
    await studioDefinitions.waitForLoaded();
    await studioDefinitions.expectDefinitionVisible(TestData.workflows.demoTenantA);
  });
});

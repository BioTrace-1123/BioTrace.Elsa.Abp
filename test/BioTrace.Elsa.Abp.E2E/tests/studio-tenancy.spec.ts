import { test, expect, projectIs } from '../fixtures/auth.fixture';
import { TestData } from '../test-data';

test.describe('Studio multi-tenancy (tenant-a-admin)', () => {
  test.skip(() => !projectIs('chromium-tenant-a-admin'), 'Runs on tenant-a-admin project only.');

  test('tenant-a-admin sees DemoTenantAWorkflow', async ({ studioShell, studioDefinitions }) => {
    await studioShell.gotoDefinitions();
    await studioDefinitions.expectDefinitionVisible(TestData.workflows.demoTenantA);
  });
});

test.describe('Studio multi-tenancy (tenant-b-admin)', () => {
  test.skip(() => !projectIs('chromium-tenant-b-admin'), 'Runs on tenant-b-admin project only.');

  test('tenant-b-admin does not see DemoTenantAWorkflow', async ({ studioShell, studioDefinitions }) => {
    await studioShell.gotoDefinitions();
    await studioDefinitions.expectDefinitionHidden(TestData.workflows.demoTenantA);
  });
});

test.describe('Studio multi-tenancy (host admin)', () => {
  test.skip(() => !projectIs('chromium-admin'), 'Runs on host admin project only.');

  test('host admin can switch to Tenant A and see DemoTenantAWorkflow', async ({
    page,
    studioShell,
    studioDefinitions,
  }) => {
    await studioShell.gotoDefinitions();
    await studioShell.selectTenant(TestData.tenants.tenantADisplayName);
    await studioDefinitions.waitForLoaded();
    await studioDefinitions.expectDefinitionVisible(TestData.workflows.demoTenantA);

    await page.reload();
    await studioDefinitions.waitForLoaded();
    await studioDefinitions.expectDefinitionVisible(TestData.workflows.demoTenantA);
  });
});

import { test, expect, projectIs } from '../fixtures/auth.fixture';
import { createElsaApi } from '../helpers/elsa-api';
import { TestData } from '../test-data';

test.describe('Workflow lifecycle', () => {
  test('tenant-a-admin can create, publish, execute and see finished instance', async ({
    page,
    studioShell,
    studioDefinitions,
    studioInstances,
  }) => {
    test.skip(!projectIs('chromium-tenant-a-admin'), 'Runs on tenant-a-admin project only.');

    const workflowName = `E2E Lifecycle ${Date.now()}`;
    const elsaApi = createElsaApi(page, TestData.tenants.tenantA);

    await studioShell.gotoDefinitions();
    await studioDefinitions.waitForLoaded();

    await studioDefinitions.clickCreateWorkflow();
    await studioDefinitions.fillCreateWorkflowDialog(workflowName, 'E2E lifecycle workflow');
    await page.waitForURL(/\/studio\/workflows\/definitions\/[^/]+/, { timeout: 60_000 });

    const definitionId = extractDefinitionId(page.url());
    await studioDefinitions.publishCurrentWorkflow();
    await studioShell.gotoDefinitions();
    await studioDefinitions.expectDefinitionVisible(workflowName);

    const executeResult = await elsaApi.executeWorkflowDefinition(definitionId);
    expect(executeResult.workflowInstanceId ?? executeResult.status).toBeTruthy();

    await studioShell.gotoInstances();
    await studioInstances.waitForLoaded();
    await studioInstances.waitForInstanceStatus(workflowName, /finished|completed/i, 90_000);
  });
});

function extractDefinitionId(url: string): string {
  const match = url.match(/\/studio\/workflows\/definitions\/([^/?#]+)/i);
  if (!match?.[1]) {
    throw new Error(`Could not extract workflow definition id from URL: ${url}`);
  }

  return decodeURIComponent(match[1]);
}

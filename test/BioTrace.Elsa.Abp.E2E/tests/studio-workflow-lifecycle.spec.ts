import { test, expect, projectIs } from '../fixtures/auth.fixture';
import { E2E_TIMEOUTS } from '../timeouts';
import { TestData } from '../test-data';

test.describe('Workflow lifecycle', () => {
  test.skip(() => !projectIs('chromium-tenant-a-admin'), 'Runs on tenant-a-admin project only.');

  test('tenant-a-admin can create, publish, execute and see finished instance', async ({
    page,
    elsaApi,
    studioShell,
    studioDefinitions,
    studioInstances,
  }) => {

    const workflowName = `E2E Lifecycle ${Date.now()}`;

    await studioShell.gotoDefinitions();
    await studioDefinitions.waitForLoaded();

    await studioDefinitions.clickCreateWorkflow();
    await studioDefinitions.fillCreateWorkflowDialog(workflowName, 'E2E lifecycle workflow');
    await page.waitForURL(/\/studio\/workflows\/definitions\/[^/]+/, { timeout: E2E_TIMEOUTS.studioPage });

    const definitionId = extractDefinitionId(page.url());
    // Elsa 3.7 designer toolbar exposes Publish as icon-only controls; publish via API after UI create.
    await elsaApi.publishWorkflowDefinition(definitionId);
    await studioShell.gotoDefinitions();
    await studioDefinitions.expectDefinitionVisible(workflowName);

    const executeResult = await elsaApi.executeWorkflowDefinition(definitionId);
    expect(executeResult.workflowInstanceId ?? executeResult.status).toBeTruthy();

    await studioShell.gotoInstances();
    await studioInstances.waitForLoaded();
    await studioInstances.waitForInstanceStatus(workflowName, /finished|completed/i, E2E_TIMEOUTS.workflowPoll);
  });
});

function extractDefinitionId(url: string): string {
  const match = url.match(/\/studio\/workflows\/definitions\/([^/?#]+)/i);
  if (!match?.[1]) {
    throw new Error(`Could not extract workflow definition id from URL: ${url}`);
  }

  return decodeURIComponent(match[1]);
}

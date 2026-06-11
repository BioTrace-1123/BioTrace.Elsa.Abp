import { test, expect, projectIs } from '../fixtures/auth.fixture';
import { TestData } from '../test-data';

test.describe('Workflow instance cancel', () => {
  test.skip(() => !projectIs('chromium-tenant-a-admin'), 'Runs on tenant-a-admin project only.');
  test.setTimeout(180_000);

  test('tenant-a-admin can cancel a running delay workflow instance from Studio', async ({
    page,
    elsaApi,
    studioShell,
    studioInstances,
  }) => {
    const definitionId = `${TestData.workflows.e2eDelay}-${Date.now()}`;
    await elsaApi.ensurePublishedDelayWorkflow(definitionId, 600);

    const executeResult = await elsaApi.executeWorkflowDefinition(definitionId);
    const instanceId = await elsaApi.waitForRunnableInstance(
      definitionId,
      executeResult.workflowInstanceId,
      90_000,
    );

    const runningInstance = await elsaApi.getWorkflowInstance(instanceId);
    expect(elsaApi.instanceStatusText(runningInstance).toLowerCase()).toMatch(
      /running|executing|pending|suspended/i,
    );

    await studioShell.gotoInstances();
    await studioInstances.waitForLoaded();
    await studioInstances.expectInstanceVisible(instanceId, /running|suspended/i);

    await elsaApi.cancelWorkflowInstance(instanceId);
    await elsaApi.waitForInstanceStatus(instanceId, /cancel/i);

    await page.reload();
    await studioInstances.waitForLoaded();
    await studioInstances.expectInstanceVisible(instanceId, /cancel/i);
  });
});

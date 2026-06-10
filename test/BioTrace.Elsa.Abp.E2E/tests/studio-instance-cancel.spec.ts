import { test, expect, projectIs } from '../fixtures/auth.fixture';
import { createElsaApi } from '../helpers/elsa-api';
import { TestData } from '../test-data';

test.describe('Workflow instance cancel', () => {
  test('tenant-a-admin can cancel a running delay workflow instance from Studio', async ({
    page,
    studioShell,
    studioInstances,
  }) => {
    test.skip(!projectIs('chromium-tenant-a-admin'), 'Runs on tenant-a-admin project only.');

    const elsaApi = createElsaApi(page, TestData.tenants.tenantA);
    const definitionId = TestData.workflows.e2eDelay;
    await elsaApi.ensurePublishedDelayWorkflow(definitionId);
    const dispatchResult = await elsaApi.dispatchWorkflowDefinition(definitionId);
    const instanceId = dispatchResult.workflowInstanceId;
    expect(instanceId).toBeTruthy();

    const runningInstance = await elsaApi.getWorkflowInstance(instanceId!);
    expect(runningInstance.status?.toLowerCase()).toMatch(/running|executing|pending/i);

    await studioShell.gotoInstances();
    await studioInstances.waitForLoaded();
    await studioInstances.cancelFirstRunningInstance(definitionId);

    const deadline = Date.now() + 90_000;
    let cancelled = false;
    while (Date.now() < deadline) {
      const instance = await elsaApi.getWorkflowInstance(instanceId!);
      if (instance.status && /cancel/i.test(instance.status)) {
        cancelled = true;
        break;
      }

      await new Promise((resolve) => setTimeout(resolve, 2_000));
    }

    expect(cancelled).toBe(true);
  });
});

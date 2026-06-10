import { test as base } from '@playwright/test';
import { StudioShellPage } from '../pages/studio-shell.page';
import { StudioDefinitionsPage } from '../pages/studio-definitions.page';
import { StudioInstancesPage } from '../pages/studio-instances.page';
import { createElsaApi, ElsaApi } from '../helpers/elsa-api';

type StudioFixtures = {
  studioShell: StudioShellPage;
  studioDefinitions: StudioDefinitionsPage;
  studioInstances: StudioInstancesPage;
  elsaApi: ElsaApi;
  tenantName: string | null;
};

export const test = base.extend<StudioFixtures>({
  tenantName: null,

  studioShell: async ({ page }, use) => {
    await use(new StudioShellPage(page));
  },

  studioDefinitions: async ({ page }, use) => {
    await use(new StudioDefinitionsPage(page));
  },

  studioInstances: async ({ page }, use) => {
    await use(new StudioInstancesPage(page));
  },

  elsaApi: async ({ page, tenantName }, use) => {
    await use(createElsaApi(page, tenantName));
  },
});

export { expect } from '@playwright/test';

export function projectIs(name: string): boolean {
  return test.info().project.name === name;
}

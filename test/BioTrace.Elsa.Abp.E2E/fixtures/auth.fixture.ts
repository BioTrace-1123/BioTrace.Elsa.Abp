import { test as base, type BrowserContext } from '@playwright/test';
import fs from 'fs';
import path from 'path';
import { createElsaApi, ElsaApi } from '../helpers/elsa-api';
import { StudioDefinitionsPage } from '../pages/studio-definitions.page';
import { StudioInstancesPage } from '../pages/studio-instances.page';
import { StudioShellPage } from '../pages/studio-shell.page';
import { getAuthForProject, localStoragePath, sessionStoragePath, type AuthRole } from '../test-data';

type StudioFixtures = {
  studioShell: StudioShellPage;
  studioDefinitions: StudioDefinitionsPage;
  studioInstances: StudioInstancesPage;
  elsaApi: ElsaApi;
};

async function restoreWasmAuthStorage(context: BrowserContext, role: AuthRole): Promise<void> {
  const e2eRoot = path.resolve(__dirname, '..');
  const sessionFile = path.join(e2eRoot, sessionStoragePath(role));
  const localFile = path.join(e2eRoot, localStoragePath(role));

  if (fs.existsSync(sessionFile)) {
    const storage = JSON.parse(fs.readFileSync(sessionFile, 'utf-8')) as Record<string, string>;
    await context.addInitScript((data) => {
      for (const [key, value] of Object.entries(data)) {
        sessionStorage.setItem(key, value);
      }
    }, storage);
  }

  if (fs.existsSync(localFile)) {
    const storage = JSON.parse(fs.readFileSync(localFile, 'utf-8')) as Record<string, string>;
    await context.addInitScript((data) => {
      for (const [key, value] of Object.entries(data)) {
        localStorage.setItem(key, value);
      }
    }, storage);
  }
}

export const test = base.extend<StudioFixtures>({
  context: async ({ browser, storageState }, use, testInfo) => {
    const context = await browser.newContext({
      storageState,
      ignoreHTTPSErrors: true,
    });

    const { role } = getAuthForProject(testInfo.project.name);
    await restoreWasmAuthStorage(context, role);

    await use(context);
    await context.close();
  },

  studioShell: async ({ page }, use) => {
    await use(new StudioShellPage(page));
  },

  studioDefinitions: async ({ page }, use) => {
    await use(new StudioDefinitionsPage(page));
  },

  studioInstances: async ({ page }, use) => {
    await use(new StudioInstancesPage(page));
  },

  elsaApi: async ({ request }, use, testInfo) => {
    const { username, tenantName } = getAuthForProject(testInfo.project.name);
    await use(createElsaApi(request, username, tenantName));
  },
});

export { expect } from '@playwright/test';

export function projectIs(name: string): boolean {
  return test.info().project.name === name;
}

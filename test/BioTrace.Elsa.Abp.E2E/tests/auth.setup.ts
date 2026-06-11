import { test as setup } from '@playwright/test';
import fs from 'fs';
import path from 'path';
import { authStoragePath, localStoragePath, sessionStoragePath, TestData, type AuthRole } from '../test-data';
import { StudioShellPage } from '../pages/studio-shell.page';

const e2eRoot = path.resolve(__dirname, '..');

const roles: Array<{ role: AuthRole; username: string; tenantName?: string }> = [
  { role: 'admin', username: TestData.users.hostAdmin },
  { role: 'tenant-a-admin', username: TestData.users.tenantAAdmin, tenantName: TestData.tenants.tenantA },
  { role: 'tenant-a-designer', username: TestData.users.tenantADesigner, tenantName: TestData.tenants.tenantA },
  { role: 'tenant-b-admin', username: TestData.users.tenantBAdmin, tenantName: TestData.tenants.tenantB },
];

for (const { role, username, tenantName } of roles) {
  setup(`authenticate as ${role}`, async ({ page }) => {
    fs.mkdirSync(path.join(e2eRoot, '.auth'), { recursive: true });

    const studio = new StudioShellPage(page);
    await studio.loginViaStudio(username, tenantName);

    const browserStorage = await page.evaluate(() => {
      const readStore = (store: Storage): Record<string, string> => {
        const data: Record<string, string> = {};
        for (let index = 0; index < store.length; index++) {
          const key = store.key(index);
          if (key) {
            data[key] = store.getItem(key) ?? '';
          }
        }

        return data;
      };

      return {
        session: readStore(sessionStorage),
        local: readStore(localStorage),
      };
    });

    fs.writeFileSync(path.join(e2eRoot, sessionStoragePath(role)), JSON.stringify(browserStorage.session), 'utf-8');
    fs.writeFileSync(path.join(e2eRoot, localStoragePath(role)), JSON.stringify(browserStorage.local), 'utf-8');
    await page.context().storageState({ path: path.join(e2eRoot, authStoragePath(role)) });
  });
}

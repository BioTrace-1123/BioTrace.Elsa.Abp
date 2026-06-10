import { test as setup } from '@playwright/test';
import fs from 'fs';
import path from 'path';
import { authStoragePath, TestData, type AuthRole } from '../test-data';
import { StudioShellPage } from '../pages/studio-shell.page';

const authDir = path.resolve(__dirname, '../.auth');

const roles: Array<{ role: AuthRole; username: string; tenantName?: string }> = [
  { role: 'admin', username: TestData.users.hostAdmin },
  { role: 'tenant-a-admin', username: TestData.users.tenantAAdmin, tenantName: TestData.tenants.tenantA },
  { role: 'tenant-a-designer', username: TestData.users.tenantADesigner, tenantName: TestData.tenants.tenantA },
  { role: 'tenant-b-admin', username: TestData.users.tenantBAdmin, tenantName: TestData.tenants.tenantB },
];

for (const { role, username, tenantName } of roles) {
  setup(`authenticate as ${role}`, async ({ page }) => {
    fs.mkdirSync(authDir, { recursive: true });

    const studio = new StudioShellPage(page);
    await studio.loginViaStudio(username, tenantName);

    await page.context().storageState({ path: path.resolve(__dirname, '..', authStoragePath(role)) });
  });
}

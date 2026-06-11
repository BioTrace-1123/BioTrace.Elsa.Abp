import { test, expect, projectIs } from '../fixtures/auth.fixture';
import { TestData } from '../test-data';

test.describe('Studio smoke', () => {
  test.skip(() => !projectIs('chromium-admin'), 'Host admin smoke tests run on chromium-admin project only.');

  test('admin can load Studio and open workflow definitions', async ({ page, studioShell, studioDefinitions }) => {
    await studioShell.gotoDefinitions();
    await studioDefinitions.waitForLoaded();
    await expect(page).toHaveURL(/\/studio\/workflows\/definitions/);
  });

  test('admin current-user returns wildcard permissions', async ({ elsaApi }) => {
    const currentUser = await elsaApi.getCurrentUser();
    expect(currentUser.permissions).toContain('*');
  });
});

import { defineConfig, devices } from '@playwright/test';
import path from 'path';
import { authStoragePath } from './test-data';

// Use full Chromium instead of headless shell so Dev Container install-deps libraries apply.
process.env.PLAYWRIGHT_CHROMIUM_USE_HEADLESS_NEW ??= '0';

const baseURL = process.env.E2E_BASE_URL ?? 'https://localhost:44388';
const startHostScript = path.join(__dirname, 'scripts/start-e2e-host.sh');

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  timeout: 120_000,
  expect: { timeout: 30_000 },
  reporter: process.env.CI ? [['github'], ['html', { open: 'never' }]] : [['list'], ['html', { open: 'never' }]],
  globalSetup: require.resolve('./global-setup'),
  use: {
    baseURL,
    ignoreHTTPSErrors: true,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 20_000,
    navigationTimeout: 60_000,
  },
  webServer: {
    command: `"${startHostScript}"`,
    url: `${baseURL}/swagger/v1/swagger.json`,
    // start-e2e-host.sh recreates E2E databases immediately before Host starts.
    reuseExistingServer: false,
    timeout: 180_000,
    ignoreHTTPSErrors: true,
    env: {
      ...process.env,
      E2E_BASE_URL: baseURL,
      E2E_POSTGRES_HOST: process.env.E2E_POSTGRES_HOST ?? process.env.INTEGRATION_TEST_POSTGRES_HOST ?? 'localhost',
    },
  },
  projects: [
    {
      name: 'setup',
      testMatch: /auth\.setup\.ts/,
    },
    {
      name: 'chromium-admin',
      use: {
        ...devices['Desktop Chrome'],
        storageState: authStoragePath('admin'),
      },
      dependencies: ['setup'],
      testIgnore: /auth\.setup\.ts/,
      testMatch: /studio-smoke\.spec\.ts|studio-tenancy\.spec\.ts/,
    },
    {
      name: 'chromium-tenant-a-admin',
      use: {
        ...devices['Desktop Chrome'],
        storageState: authStoragePath('tenant-a-admin'),
      },
      dependencies: ['setup'],
      testMatch: /studio-tenancy\.spec\.ts|studio-permissions\.spec\.ts|studio-workflow-lifecycle\.spec\.ts|studio-instance-cancel\.spec\.ts/,
    },
    {
      name: 'chromium-tenant-a-designer',
      use: {
        ...devices['Desktop Chrome'],
        storageState: authStoragePath('tenant-a-designer'),
      },
      dependencies: ['setup'],
      testMatch: /studio-permissions\.spec\.ts/,
    },
    {
      name: 'chromium-tenant-b-admin',
      use: {
        ...devices['Desktop Chrome'],
        storageState: authStoragePath('tenant-b-admin'),
      },
      dependencies: ['setup'],
      testMatch: /studio-tenancy\.spec\.ts/,
    },
  ],
});

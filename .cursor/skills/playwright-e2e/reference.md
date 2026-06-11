# Playwright E2E Reference

## Minimal playwright.config.ts

```typescript
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: process.env.CI ? 2 : undefined,
  timeout: 60_000,
  expect: { timeout: 10_000 },
  reporter: process.env.CI
    ? [['github'], ['html', { open: 'never' }]]
    : [['list'], ['html']],
  use: {
    baseURL: process.env.BASE_URL ?? 'http://localhost:3000',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
  },
  webServer: {
    command: 'npm run start:test',
    url: 'http://localhost:3000/health',
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
  projects: [
    { name: 'setup', testMatch: /auth\.setup\.ts/ },
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'], storageState: '.auth/user.json' },
      dependencies: ['setup'],
      testIgnore: /auth\.setup\.ts/,
    },
  ],
});
```

## Auth setup pattern

```typescript
// tests/auth.setup.ts
import { test as setup } from '@playwright/test';

setup('authenticate', async ({ page }) => {
  await page.goto('/login');
  await page.getByLabel('Email').fill(process.env.E2E_USER!);
  await page.getByLabel('Password').fill(process.env.E2E_PASSWORD!);
  await page.getByRole('button', { name: 'Sign in' }).click();
  await page.waitForURL('/dashboard');
  await page.context().storageState({ path: '.auth/user.json' });
});
```

## API helper via request fixture

```typescript
import { APIRequestContext } from '@playwright/test';

export async function getAuthToken(request: APIRequestContext): Promise<string> {
  const response = await request.post('/oauth/token', {
    form: { grant_type: 'password', username: 'user', password: 'pass' },
  });
  if (!response.ok()) {
    throw new Error(`Token request failed: ${response.status()}`);
  }
  const body = await response.json();
  return body.access_token;
}
```

## Network mock (third-party)

```typescript
await page.route('**/api.stripe.com/**', (route) =>
  route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ id: 'pi_test_123', status: 'succeeded' }),
  }),
);
```

## GitHub Actions CI job

```yaml
e2e-tests:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-node@v4
      with:
        node-version: 22
        cache: npm
        cache-dependency-path: e2e/package-lock.json
    - run: npm ci
      working-directory: e2e
    - run: npx playwright install --with-deps chromium
      working-directory: e2e
    - run: npm run test:e2e
      working-directory: e2e
      env:
        CI: true
    - uses: actions/upload-artifact@v4
      if: failure()
      with:
        name: playwright-report
        path: e2e/playwright-report/
        retention-days: 7
```

## Parallel sharding

```bash
npx playwright test --shard=1/4
npx playwright test --shard=2/4
# Run all shards in CI matrix for ~4x speedup
```

---
name: playwright-e2e
description: >-
  Guides Playwright end-to-end test design, implementation, debugging, and CI
  integration following official and community best practices. Use when writing
  or fixing E2E tests, Playwright specs, browser automation, test flakiness,
  storageState auth, Page Object Model, or webServer setup.
---

# Playwright E2E Best Practices

## Scope (Testing Pyramid)

- E2E covers **5–10%** of tests: critical user journeys only (login, checkout, core CRUD).
- Push edge cases to unit/integration tests; do not duplicate API contract validation in browser tests.
- One E2E test = one user-facing outcome; avoid multi-scenario mega-tests.

## Locator Strategy

Priority order (most resilient first):

1. `getByRole('button', { name: 'Sign in' })`
2. `getByLabel('Email')` / `getByPlaceholder`
3. `getByTestId('checkout-submit')` — explicit test contract
4. CSS class / XPath — last resort only

Rules:

- Scope locators: `page.getByRole('dialog').getByRole('button', { name: 'OK' })`
- Never use `nth-child` or generated class names
- Prefer `filter({ hasText })` over brittle full-text XPath

## Test Isolation

Each test must pass **alone**, in any order, in parallel:

- Unique data per run: UUID or timestamp suffix (`user-${Date.now()}@test.local`)
- Seed via API/fixture in `beforeEach`, not shared DB rows
- No dependency on prior test state
- Teardown or use disposable sandbox accounts

## Authentication

Avoid UI login in every test:

```typescript
// playwright.config.ts
projects: [
  { name: 'setup', testMatch: /auth\.setup\.ts/ },
  {
    name: 'chromium',
    use: { storageState: '.auth/user.json' },
    dependencies: ['setup'],
  },
],
```

- `auth.setup.ts`: perform real login once, save `storageState`
- Business specs: start authenticated via `storageState`
- API assertions: use `request` fixture with token grant, not browser cookies

## Project Structure

```
e2e/
├── playwright.config.ts
├── fixtures/          # extended test + shared setup
├── helpers/           # API clients, wait utilities
├── pages/             # Page Object Model
├── tests/             # *.spec.ts
└── global-setup.ts    # env checks (DB, ports)
```

### Page Object Model

```typescript
export class LoginPage {
  constructor(private readonly page: Page) {}
  async login(email: string, password: string) {
    await this.page.getByLabel('Email').fill(email);
    await this.page.getByLabel('Password').fill(password);
    await this.page.getByRole('button', { name: 'Sign in' }).click();
  }
}
```

### Fixtures

```typescript
export const test = base.extend<{ loginPage: LoginPage }>({
  loginPage: async ({ page }, use) => use(new LoginPage(page)),
});
```

## Waiting and Assertions

- Rely on Playwright **auto-wait**; locators retry until timeout
- Use `await expect(locator).toBeVisible()` — not manual polling loops
- **Never** use `page.waitForTimeout()` except as last-resort debug
- For async state: `await expect.poll(async () => ...).toBe(expected)`

### API readiness before UI

```typescript
const response = await page.waitForResponse(
  (r) => r.url().includes('/api/items') && r.ok(),
  { timeout: 30_000 },
);
if (!response.ok()) {
  throw new Error(`API failed: ${response.status()} ${response.url()}`);
}
```

Do not `.catch(() => undefined)` on readiness checks — fail fast with context.

## Network Strategy

| Boundary | Approach |
|----------|----------|
| Third-party (Stripe, email, analytics) | Mock via `page.route()` |
| Own backend — revenue-critical paths | Real API + seeded DB |
| Own backend — empty/error states | Mock selectively |
| Auth | `storageState` or API token, not UI login per test |

## webServer (Self-Hosted App)

```typescript
webServer: {
  command: 'npm run start:test',
  url: 'http://localhost:3000/health',
  reuseExistingServer: !process.env.CI,
  timeout: 120_000,
},
```

- Health-check URL must return 200 before tests start
- Use dedicated test DB; recreate or migrate per run
- `reuseExistingServer: false` in CI for determinism

## CI Configuration

```typescript
export default defineConfig({
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: process.env.CI ? 2 : undefined,
  use: {
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  reporter: process.env.CI
    ? [['github'], ['html', { open: 'never' }]]
    : [['list'], ['html']],
});
```

- Upload `playwright-report/` and `test-results/` as CI artifacts on failure
- Shard large suites: `npx playwright test --shard=1/4`
- Target flake rate **< 2%**; quarantine chronically flaky tests

## Debugging Workflow

1. `npx playwright show-report` — HTML report
2. Open trace: `npx playwright show-trace test-results/.../trace.zip`
3. Re-run single test: `npx playwright test path/to.spec.ts -g "test name" --debug`
4. Headed mode: `npx playwright test --headed`

## Anti-Patterns

| Avoid | Use instead |
|-------|-------------|
| `waitForTimeout(5000)` | `expect(locator).toBeVisible()` |
| Shared hardcoded user/email | Unique per-run identifiers |
| UI login in every test | `storageState` + setup project |
| Swallowing API wait errors | Throw with status + URL |
| CSS `.btn-primary-v3` selectors | `getByRole` / `getByTestId` |
| Over-mocking own backend on critical paths | Real API for contract validation |
| `test.only` committed to main | `forbidOnly` in CI |

## Additional Resources

- Config templates and CI YAML snippets: [reference.md](reference.md)
- Official docs: https://playwright.dev/docs/best-practices

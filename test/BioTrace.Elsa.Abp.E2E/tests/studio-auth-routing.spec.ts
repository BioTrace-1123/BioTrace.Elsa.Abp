import { test, expect } from '@playwright/test';
import { StudioShellPage } from '../pages/studio-shell.page';
import { E2E_TIMEOUTS } from '../timeouts';
import { studioOidcRedirectUri, TestData } from '../test-data';

test.describe('Studio auth routing (guest)', () => {
  test('legacy /authentication/login redirects to PathBase-scoped Studio route', async ({ request }) => {
    const response = await request.get(TestData.legacyAuthenticationLoginPath, {
      maxRedirects: 0,
      failOnStatusCode: false,
    });

    expect(response.status()).toBe(302);
    expect(response.headers().location).toBe(`${TestData.studioPath}/authentication/login`);
  });

  test('legacy /authentication/login loads Studio WASM instead of site-root 404', async ({ page }) => {
    await page.goto(TestData.legacyAuthenticationLoginPath);

    await expect(page).toHaveURL(/\/studio\/authentication\/login/);
    await expect(page.locator('#app')).toBeAttached({ timeout: E2E_TIMEOUTS.wasmBoot });
    await expect(page.getByText(/not found|404/i)).toHaveCount(0);
  });

  test('Studio WASM uses OpenIddict-registered ElsaStudio redirect_uri', async ({ page }) => {
    const expectedRedirectUri = studioOidcRedirectUri();
    const authorizeResponsePromise = page.waitForResponse(
      (response) => response.url().includes('/connect/authorize'),
      { timeout: E2E_TIMEOUTS.loginFlow },
    );

    await page.goto(`${TestData.studioPath}/authentication/login`);
    await page.locator('#app').waitFor({ state: 'attached', timeout: E2E_TIMEOUTS.wasmBoot });

    const authorizeResponse = await authorizeResponsePromise;
    const authorizeUrl = new URL(authorizeResponse.url());

    expect(authorizeUrl.searchParams.get('client_id')).toBe(TestData.oidcClientId);
    expect(authorizeUrl.searchParams.get('redirect_uri')).toBe(expectedRedirectUri);
    expect(authorizeUrl.searchParams.get('error')).not.toBe('invalid_request');
    expect(authorizeResponse.status()).not.toBe(400);

    const location = authorizeResponse.headers()['location'] ?? '';
    expect(location).not.toContain('error=invalid_request');
    expect(location).not.toContain('invalid_request');
  });

  test('guest can sign in after legacy /authentication/login redirect', async ({ page }) => {
    const studio = new StudioShellPage(page);

    await page.goto(TestData.legacyAuthenticationLoginPath);
    await expect(page).toHaveURL(/\/studio\/authentication\/login/);
    await studio.completeLoginIfNeeded(TestData.users.hostAdmin);

    await expect(page).toHaveURL(/\/studio/);
    await expect(
      page
        .getByRole('link', { name: /workflows/i })
        .or(page.getByRole('button', { name: /workflows/i }))
        .first(),
    ).toBeVisible();
  });
});

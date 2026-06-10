import type { Page } from '@playwright/test';
import { TestData } from '../test-data';

const OIDC_STORAGE_PREFIX = 'oidc.user:';

export async function getAccessToken(page: Page): Promise<string> {
  const authority = TestData.oidcAuthority.replace(/\/$/, '');
  const clientId = TestData.oidcClientId;
  const expectedKey = `${OIDC_STORAGE_PREFIX}${authority}:${clientId}`;

  const token = await page.evaluate((key) => {
    const raw = localStorage.getItem(key);
    if (!raw) {
      const fallbackKey = Object.keys(localStorage).find((k) => k.startsWith('oidc.user:'));
      if (!fallbackKey) {
        return null;
      }

      const fallbackRaw = localStorage.getItem(fallbackKey);
      if (!fallbackRaw) {
        return null;
      }

      try {
        const parsed = JSON.parse(fallbackRaw) as { access_token?: string };
        return parsed.access_token ?? null;
      } catch {
        return null;
      }
    }

    try {
      const parsed = JSON.parse(raw) as { access_token?: string };
      return parsed.access_token ?? null;
    } catch {
      return null;
    }
  }, expectedKey);

  if (!token) {
    throw new Error('OIDC access_token was not found in browser localStorage.');
  }

  return token;
}

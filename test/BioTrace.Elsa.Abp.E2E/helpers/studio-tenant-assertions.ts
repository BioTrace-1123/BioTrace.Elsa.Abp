import { expect, type Page } from '@playwright/test';

const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

const fetchHeaderErrorPatterns = [
  /non ISO-8859-1/i,
  /Failed to execute 'append' on 'Headers'/i,
];

export const studioTenantLocalStorageIdKey = 'biotrace.elsa.studio.tenantId';
export const studioTenantLocalStorageLegacyKey = 'biotrace.elsa.studio.tenant';

export interface StudioTenantDiagnostics {
  assertNoFetchHeaderErrors: () => void;
  assertGuidTenantHeaders: () => void;
}

export function attachStudioTenantDiagnostics(page: Page): StudioTenantDiagnostics {
  const consoleErrors: string[] = [];
  const tenantHeaders: string[] = [];

  page.on('console', (message) => {
    if (message.type() === 'error') {
      consoleErrors.push(message.text());
    }
  });

  page.on('request', (request) => {
    const tenantHeader = request.headers()['__tenant'];
    if (tenantHeader) {
      tenantHeaders.push(tenantHeader);
    }
  });

  return {
    assertNoFetchHeaderErrors() {
      const fetchHeaderErrors = consoleErrors.filter((error) =>
        fetchHeaderErrorPatterns.some((pattern) => pattern.test(error)),
      );
      expect(fetchHeaderErrors, `Unexpected Fetch header errors: ${fetchHeaderErrors.join('; ')}`).toHaveLength(0);
    },

    assertGuidTenantHeaders() {
      expect(tenantHeaders.length, 'Expected at least one outbound request with __tenant header').toBeGreaterThan(0);
      for (const tenantHeader of tenantHeaders) {
        expect(tenantHeader, `__tenant header must be a Guid, got '${tenantHeader}'`).toMatch(guidPattern);
      }
    },
  };
}

export async function assertTenantIdLocalStorage(page: Page): Promise<void> {
  const storage = await page.evaluate(
    ([idKey, legacyKey]) => ({
      tenantId: localStorage.getItem(idKey),
      legacyTenantName: localStorage.getItem(legacyKey),
    }),
    [studioTenantLocalStorageIdKey, studioTenantLocalStorageLegacyKey],
  );

  expect(storage.tenantId, 'biotrace.elsa.studio.tenantId should be set').toMatch(guidPattern);
  expect(storage.legacyTenantName, 'legacy tenant name key should not be used').toBeNull();
}

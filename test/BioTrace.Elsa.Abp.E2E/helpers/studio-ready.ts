import type { Page, Response } from '@playwright/test';
import { E2E_TIMEOUTS } from '../timeouts';

async function waitForSuccessfulApiResponse(
  page: Page,
  urlFragment: string,
  timeout: number,
): Promise<Response> {
  const response = await page.waitForResponse(
    (candidate) => candidate.url().includes(urlFragment) && candidate.ok(),
    { timeout },
  );

  if (!response.ok()) {
    throw new Error(
      `Expected successful response for ${urlFragment}, got ${response.status()} ${response.url()}`,
    );
  }

  return response;
}

export async function waitForDefinitionsReady(page: Page): Promise<void> {
  await page.waitForURL(/\/studio\/workflows\/definitions/, { timeout: E2E_TIMEOUTS.studioPage });

  const definitionsUi = page
    .getByRole('button', { name: /create|new workflow/i })
    .or(page.getByRole('table'))
    .or(page.getByRole('grid'))
    .or(page.getByRole('link', { name: /definitions/i }))
    .first();

  if (!(await definitionsUi.isVisible().catch(() => false))) {
    const responsePromise = page.waitForResponse(
      (candidate) => candidate.url().includes('/elsa/api/workflow-definitions') && candidate.ok(),
      { timeout: E2E_TIMEOUTS.studioPage },
    );
    await page.reload();
    const response = await responsePromise;
    if (!response.ok()) {
      throw new Error(
        `Expected successful workflow-definitions response, got ${response.status()} ${response.url()}`,
      );
    }
  }

  await definitionsUi.waitFor({ state: 'visible', timeout: E2E_TIMEOUTS.studioPage });
}

export async function waitForInstancesReady(page: Page): Promise<void> {
  await page.waitForURL(/\/studio\/workflows\/instances/, { timeout: E2E_TIMEOUTS.studioPage });

  const instancesUi = page
    .getByRole('table')
    .or(page.getByRole('grid'))
    .or(page.getByRole('heading', { name: /instances/i }))
    .first();

  if (!(await instancesUi.isVisible().catch(() => false))) {
    const responsePromise = page.waitForResponse(
      (candidate) => candidate.url().includes('/elsa/api/workflow-instances') && candidate.ok(),
      { timeout: E2E_TIMEOUTS.studioPage },
    );
    await page.reload();
    const response = await responsePromise;
    if (!response.ok()) {
      throw new Error(
        `Expected successful workflow-instances response, got ${response.status()} ${response.url()}`,
      );
    }
  }

  await instancesUi.waitFor({ state: 'visible', timeout: E2E_TIMEOUTS.studioPage });
}

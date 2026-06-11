import type { APIRequestContext } from '@playwright/test';
import { TestData } from '../test-data';

/** Mirrors test/BioTrace.Elsa.Abp.HttpApi.Host.Tests/Helpers/OpenIddictTokenClient.cs */
export const IntegrationTestClient = {
  clientId: 'BioTrace_Elsa_Abp_IntegrationTests',
  clientSecret: 'integration-test-secret',
  scope: 'BioTrace_Elsa_Abp openid profile roles',
} as const;

export async function requestPasswordToken(
  request: APIRequestContext,
  username: string,
  tenantName?: string | null,
  password = TestData.password,
): Promise<string> {
  const headers: Record<string, string> = {};
  if (tenantName) {
    headers['__tenant'] = tenantName;
  }

  const response = await request.post('/connect/token', {
    headers,
    form: {
      grant_type: 'password',
      client_id: IntegrationTestClient.clientId,
      client_secret: IntegrationTestClient.clientSecret,
      username,
      password,
      scope: IntegrationTestClient.scope,
    },
  });

  if (!response.ok()) {
    throw new Error(`Token request failed (${response.status()}): ${await response.text()}`);
  }

  const body = (await response.json()) as { access_token?: string };
  if (!body.access_token) {
    throw new Error('Token response did not contain access_token.');
  }

  return body.access_token;
}

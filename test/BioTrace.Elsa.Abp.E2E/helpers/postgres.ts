import { Client } from 'pg';

export const E2E_POSTGRES_HOST = process.env.E2E_POSTGRES_HOST ?? process.env.INTEGRATION_TEST_POSTGRES_HOST ?? 'localhost';

export const E2E_ABP_DATABASE = 'BioTrace_Abp_E2E';
export const E2E_ELSA_DATABASE = 'BioTrace_Elsa_E2E';

export function getPostgresConnectionBase(): string {
  return `postgresql://postgres:postgres@${E2E_POSTGRES_HOST}:5432`;
}

export async function ensureE2eDatabasesExist(): Promise<void> {
  const client = new Client({
    connectionString: `${getPostgresConnectionBase()}/postgres`,
  });

  await client.connect();

  try {
    for (const database of [E2E_ABP_DATABASE, E2E_ELSA_DATABASE]) {
      const exists = await client.query('SELECT 1 FROM pg_database WHERE datname = $1', [database]);
      if (exists.rowCount === 0) {
        await client.query(`CREATE DATABASE "${database}"`);
      }
    }
  } finally {
    await client.end();
  }
}

export function getE2eHostEnvironment(): NodeJS.ProcessEnv {
  const base = `Host=${E2E_POSTGRES_HOST};Port=5432;Username=postgres;Password=postgres`;
  return {
    ...process.env,
    ASPNETCORE_ENVIRONMENT: 'Development',
    ConnectionStrings__Default: `${base};Database=${E2E_ABP_DATABASE}`,
    ConnectionStrings__Elsa: `${base};Database=${E2E_ELSA_DATABASE}`,
    E2E_BASE_URL: process.env.E2E_BASE_URL ?? 'https://localhost:44388',
  };
}

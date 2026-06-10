import { ensureE2eDatabasesExist, E2E_POSTGRES_HOST } from './helpers/postgres';

export default async function globalSetup(): Promise<void> {
  try {
    await ensureE2eDatabasesExist();
    console.log(`E2E databases ready (PostgreSQL host: ${E2E_POSTGRES_HOST}).`);
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    throw new Error(
      `PostgreSQL is not available for E2E tests (${E2E_POSTGRES_HOST}). ` +
        'Run `docker compose up -d` or set E2E_POSTGRES_HOST=postgres in Dev Container. ' +
        `Details: ${message}`,
    );
  }
}

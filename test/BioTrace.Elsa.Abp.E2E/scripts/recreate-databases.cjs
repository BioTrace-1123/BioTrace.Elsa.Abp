const { Client } = require('pg');

const host = process.env.E2E_POSTGRES_HOST ?? process.env.INTEGRATION_TEST_POSTGRES_HOST ?? 'localhost';
const databases = ['BioTrace_Abp_E2E', 'BioTrace_Elsa_E2E'];

async function recreateDatabase(client, databaseName) {
  await client.query(
    `SELECT pg_terminate_backend(pid)
     FROM pg_stat_activity
     WHERE datname = $1 AND pid <> pg_backend_pid()`,
    [databaseName],
  );
  await client.query(`DROP DATABASE IF EXISTS "${databaseName}"`);
  await client.query(`CREATE DATABASE "${databaseName}"`);
}

async function main() {
  const client = new Client({
    connectionString: `postgresql://postgres:postgres@${host}:5432/postgres`,
  });

  await client.connect();

  try {
    for (const database of databases) {
      await recreateDatabase(client, database);
    }
    console.log(`E2E databases recreated (PostgreSQL host: ${host}).`);
  } finally {
    await client.end();
  }
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});

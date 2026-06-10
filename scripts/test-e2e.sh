#!/usr/bin/env bash
set -euo pipefail

export E2E_POSTGRES_HOST="${E2E_POSTGRES_HOST:-${INTEGRATION_TEST_POSTGRES_HOST:-localhost}}"
export E2E_BASE_URL="${E2E_BASE_URL:-https://localhost:44388}"

if [[ "$E2E_POSTGRES_HOST" == "localhost" ]]; then
  echo "Hint: on the host machine run 'docker compose up -d' before E2E tests."
else
  echo "Using PostgreSQL host: $E2E_POSTGRES_HOST"
fi

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

npm ci --prefix "$repo_root/host/BioTrace.Elsa.Abp.HttpApi.Host"
npm ci --prefix "$repo_root/test/BioTrace.Elsa.Abp.E2E"
npx --prefix "$repo_root/test/BioTrace.Elsa.Abp.E2E" playwright install --with-deps chromium

npm run test:e2e --prefix "$repo_root/test/BioTrace.Elsa.Abp.E2E" "$@"

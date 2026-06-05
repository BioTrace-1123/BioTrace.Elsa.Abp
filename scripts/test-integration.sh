#!/usr/bin/env bash
set -euo pipefail

export INTEGRATION_TEST_POSTGRES_HOST="${INTEGRATION_TEST_POSTGRES_HOST:-localhost}"

if [[ "$INTEGRATION_TEST_POSTGRES_HOST" == "localhost" ]]; then
  echo "Hint: on the host machine run 'docker compose up -d' before integration tests."
else
  echo "Using PostgreSQL host: $INTEGRATION_TEST_POSTGRES_HOST"
fi

dotnet test BioTrace.Elsa.Abp.slnx --filter "Category=Integration" "$@"

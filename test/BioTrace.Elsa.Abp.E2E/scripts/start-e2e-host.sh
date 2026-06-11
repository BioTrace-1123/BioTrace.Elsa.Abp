#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
e2e_dir="$(dirname "$script_dir")"
repo_root="$(cd "$e2e_dir/../.." && pwd)"

host_project="${repo_root}/host/BioTrace.Elsa.Abp.HttpApi.Host/BioTrace.Elsa.Abp.HttpApi.Host.csproj"
base_url="${E2E_BASE_URL:-https://localhost:44388}"

export E2E_POSTGRES_HOST="${E2E_POSTGRES_HOST:-${INTEGRATION_TEST_POSTGRES_HOST:-localhost}}"

node "$e2e_dir/scripts/recreate-databases.cjs"

postgres_base="Host=${E2E_POSTGRES_HOST};Port=5432;Username=postgres;Password=postgres"
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__Default="${postgres_base};Database=BioTrace_Abp_E2E"
export ConnectionStrings__Elsa="${postgres_base};Database=BioTrace_Elsa_E2E"
export E2E_BASE_URL="$base_url"
export AuthServer__AllowPasswordGrantForIntegrationTests=true
export OpenIddict__Applications__IntegrationTests__ClientId=BioTrace_Elsa_Abp_IntegrationTests
export OpenIddict__Applications__IntegrationTests__ClientSecret=integration-test-secret

exec dotnet run --project "$host_project" --no-launch-profile --urls "$base_url"

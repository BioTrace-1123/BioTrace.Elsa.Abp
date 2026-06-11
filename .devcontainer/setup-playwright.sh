#!/usr/bin/env bash
# Install Playwright Chromium for browser E2E tests.
set -euo pipefail

if [[ -f test/BioTrace.Elsa.Abp.E2E/package.json ]]; then
  echo "Installing E2E Playwright browsers..."
  npm ci --prefix test/BioTrace.Elsa.Abp.E2E
  npm exec --prefix test/BioTrace.Elsa.Abp.E2E -- playwright install chromium
fi

echo "Playwright Chromium ready."

#!/usr/bin/env bash
# Install Chromium browser binary for Playwright MCP (@playwright/mcp).
# System libraries are installed in the devcontainer Dockerfile via playwright install-deps.
set -euo pipefail

echo "Installing Playwright Chromium for Agent MCP verification..."
npx -y playwright@latest install chromium

if [[ -f test/BioTrace.Elsa.Abp.E2E/package.json ]]; then
  echo "Installing E2E Playwright browsers..."
  npm ci --prefix test/BioTrace.Elsa.Abp.E2E
  npm exec --prefix test/BioTrace.Elsa.Abp.E2E -- playwright install chromium
fi

echo "Playwright Chromium ready."

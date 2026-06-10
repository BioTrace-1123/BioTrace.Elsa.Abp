#!/usr/bin/env bash
# Install Chromium browser binary for Playwright MCP (@playwright/mcp).
# System libraries are installed in the devcontainer Dockerfile via playwright install-deps.
set -euo pipefail

echo "Installing Playwright Chromium for Agent MCP verification..."
npx -y playwright@latest install chromium
echo "Playwright Chromium ready."

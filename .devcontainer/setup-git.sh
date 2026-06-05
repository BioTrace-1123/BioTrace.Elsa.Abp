#!/usr/bin/env bash
# Fix credential helper after Dev Container copies host .gitconfig with host-only paths.
set -euo pipefail

export PATH="/home/vscode/.dotnet/tools:${PATH}"

git config --global --unset-all credential.helper 2>/dev/null || true
git config --global credential.helper manager

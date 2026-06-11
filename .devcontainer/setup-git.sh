#!/usr/bin/env bash
# Configure Git Credential Manager inside the Dev Container.
set -euo pipefail

export PATH="/home/vscode/.dotnet/tools:${PATH}"

git config --global --unset-all credential.helper 2>/dev/null || true
git config --global credential.helper manager

if [ -n "${SSH_AUTH_SOCK:-}" ] && [ -S "${SSH_AUTH_SOCK}" ]; then
  mkdir -p "${HOME}/.ssh"
  chmod 700 "${HOME}/.ssh"
  if [ ! -f "${HOME}/.ssh/config" ]; then
    cat > "${HOME}/.ssh/config" <<'EOF'
Host github.com
  HostName github.com
  User git
  IdentitiesOnly yes
EOF
    chmod 600 "${HOME}/.ssh/config"
  fi
fi

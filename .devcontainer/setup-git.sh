#!/usr/bin/env bash
# Fix credential helper after Dev Container copies host .gitconfig with host-only paths.
set -euo pipefail

export PATH="/home/vscode/.dotnet/tools:${PATH}"

git config --global --unset-all credential.helper 2>/dev/null || true
git config --global credential.helper manager

# SSH：凭据走宿主机 ssh-agent 转发（禁止挂载 ~/.ssh）
mkdir -p "${HOME}/.ssh"
chmod 700 "${HOME}/.ssh"
cat > "${HOME}/.ssh/config" <<'EOF'
Host github.com
  HostName github.com
  User git
  IdentitiesOnly yes

Host codeup.aliyun.com
  HostName bi0trace-cn-beijing.devops.aliyuncs.com
  User bi0trace
  IdentitiesOnly yes

Host gitea-ssh
  HostName gitea.a1mu.top
  Port 2222
  User git
  IdentitiesOnly yes
EOF
chmod 600 "${HOME}/.ssh/config"

if [ -z "${SSH_AUTH_SOCK:-}" ] || [ ! -S "${SSH_AUTH_SOCK}" ]; then
  echo "[setup-git] 警告: SSH_AUTH_SOCK 未挂载，容器内 git/ssh 可能无法认证" >&2
elif ! ssh-add -l >/dev/null 2>&1; then
  echo "[setup-git] 警告: ssh-agent 中无密钥，请在宿主机执行 ssh-add ~/.ssh/id_ed25519" >&2
fi

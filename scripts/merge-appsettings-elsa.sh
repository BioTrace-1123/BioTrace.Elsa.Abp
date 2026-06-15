#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
用法:
  ./scripts/merge-appsettings-elsa.sh \
    --target src/MyApp.HttpApi.Host/appsettings.json \
    [--source docs/appsettings.elsa.json]

将 Elsa / ElsaStudio / OpenIddict 相关配置合并到调用方 appsettings.json（保留已有键，仅补充缺失项）。

依赖: jq
EOF
}

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
SOURCE="$REPO_ROOT/docs/appsettings.elsa.json"
TARGET=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --target) TARGET="$2"; shift 2 ;;
    --source) SOURCE="$2"; shift 2 ;;
    -h|--help) usage; exit 0 ;;
    *) echo "未知参数: $1" >&2; usage; exit 1 ;;
  esac
done

if [[ -z "$TARGET" ]]; then
  echo "错误: --target 为必填项。" >&2
  usage
  exit 1
fi

if ! command -v jq >/dev/null 2>&1; then
  echo "错误: 需要 jq。请安装后重试。" >&2
  exit 1
fi

if [[ ! -f "$SOURCE" ]]; then
  echo "错误: 源文件不存在: $SOURCE" >&2
  exit 1
fi

mkdir -p "$(dirname "$TARGET")"
if [[ ! -f "$TARGET" ]]; then
  echo "{}" > "$TARGET"
fi

TMP="$(mktemp)"
jq -s 'reduce .[] as $item ({}; . * $item)' "$TARGET" "$SOURCE" > "$TMP"
mv "$TMP" "$TARGET"

echo "已合并 Elsa 配置到: $TARGET"

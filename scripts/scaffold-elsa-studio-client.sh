#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
用法:
  ./scripts/scaffold-elsa-studio-client.sh \
    --name MyCompany.MyApp.Studio.Client \
    --output src/MyCompany.MyApp.Studio.Client \
    --host-project src/MyCompany.MyApp.HttpApi.Host/MyCompany.MyApp.HttpApi.Host.csproj \
    --solution MyCompany.MyApp.sln

选项:
  --name            WASM Client 项目名（必填）
  --output          输出目录（必填）
  --host-project    Host .csproj 路径（必填）
  --solution        解决方案 .sln/.slnx 路径（可选）
  --authority       OIDC Authority，默认从 Host launchSettings 推断或 https://localhost:44388
  --backend-url     Elsa API 地址，默认 {authority}/elsa/api
  --api-scope       OIDC API Scope，默认从 Host 项目名推断
  --package-version BioTrace.Elsa.Abp.Studio.BlazorWasm 版本，默认读取仓库 common.props
  --template-dir    模板目录，默认仓库 Installer/Templates/Studio.Client
  --dry-run         仅打印将执行的操作
  -h, --help        显示帮助
EOF
}

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DEFAULT_TEMPLATE_DIR="$REPO_ROOT/src/BioTrace.Elsa.Abp.Installer/Templates/Studio.Client"

NAME=""
OUTPUT=""
HOST_PROJECT=""
SOLUTION=""
AUTHORITY=""
BACKEND_URL=""
API_SCOPE=""
PACKAGE_VERSION=""
TEMPLATE_DIR="$DEFAULT_TEMPLATE_DIR"
DRY_RUN=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --name) NAME="$2"; shift 2 ;;
    --output) OUTPUT="$2"; shift 2 ;;
    --host-project) HOST_PROJECT="$2"; shift 2 ;;
    --solution) SOLUTION="$2"; shift 2 ;;
    --authority) AUTHORITY="$2"; shift 2 ;;
    --backend-url) BACKEND_URL="$2"; shift 2 ;;
    --api-scope) API_SCOPE="$2"; shift 2 ;;
    --package-version) PACKAGE_VERSION="$2"; shift 2 ;;
    --template-dir) TEMPLATE_DIR="$2"; shift 2 ;;
    --dry-run) DRY_RUN=true; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "未知参数: $1" >&2; usage; exit 1 ;;
  esac
done

if [[ -z "$NAME" || -z "$OUTPUT" || -z "$HOST_PROJECT" ]]; then
  echo "错误: --name、--output、--host-project 为必填项。" >&2
  usage
  exit 1
fi

if [[ ! -d "$TEMPLATE_DIR" ]]; then
  echo "错误: 模板目录不存在: $TEMPLATE_DIR" >&2
  exit 1
fi

if [[ ! -f "$HOST_PROJECT" ]]; then
  echo "错误: Host 项目不存在: $HOST_PROJECT" >&2
  exit 1
fi

if [[ -z "$PACKAGE_VERSION" ]]; then
  PACKAGE_VERSION="$(grep -oP '(?<=<Version>)[^<]+' "$REPO_ROOT/common.props" | head -1)"
fi

HOST_DIR="$(cd "$(dirname "$HOST_PROJECT")" && pwd)"
LAUNCH_SETTINGS="$HOST_DIR/Properties/launchSettings.json"
if [[ -z "$AUTHORITY" && -f "$LAUNCH_SETTINGS" ]]; then
  AUTHORITY="$(python3 - <<'PY' "$LAUNCH_SETTINGS"
import json, sys
from pathlib import Path
data = json.loads(Path(sys.argv[1]).read_text())
profiles = data.get("profiles", {})
for profile in profiles.values():
    url = profile.get("applicationUrl", "")
    for part in url.replace(";", " ").split():
        if part.startswith("https://"):
            print(part.rstrip("/"))
            raise SystemExit
PY
)"
fi
AUTHORITY="${AUTHORITY:-https://localhost:44388}"
BACKEND_URL="${BACKEND_URL:-${AUTHORITY}/elsa/api}"

if [[ -z "$API_SCOPE" ]]; then
  HOST_BASENAME="$(basename "$HOST_PROJECT" .csproj)"
  API_SCOPE="${HOST_BASENAME//./_}"
fi

ROOT_NAMESPACE="$NAME"
PROJECT_FILE_NAME="$(basename "$NAME").csproj"
OUTPUT_ABS="$(mkdir -p "$OUTPUT" && cd "$OUTPUT" && pwd)"

run() {
  if $DRY_RUN; then
    echo "[dry-run] $*"
  else
    "$@"
  fi
}

echo "==> 生成 Elsa Studio WASM Client: $NAME"
echo "    输出目录: $OUTPUT_ABS"
echo "    Authority: $AUTHORITY"
echo "    Backend:   $BACKEND_URL"

if [[ -e "$OUTPUT_ABS/$PROJECT_FILE_NAME" ]]; then
  echo "错误: 项目已存在: $OUTPUT_ABS/$PROJECT_FILE_NAME" >&2
  exit 1
fi

if $DRY_RUN; then
  echo "[dry-run] 复制 Program.cs、appsettings.json、$PROJECT_FILE_NAME"
else
  cp "$TEMPLATE_DIR/Program.cs" "$OUTPUT_ABS/Program.cs"
  mkdir -p "$OUTPUT_ABS/wwwroot"
  sed -e "s|{{Authority}}|$AUTHORITY|g" \
      -e "s|{{BackendUrl}}|$BACKEND_URL|g" \
      -e "s|{{ApiScope}}|$API_SCOPE|g" \
      "$TEMPLATE_DIR/appsettings.json" > "$OUTPUT_ABS/wwwroot/appsettings.json"
  sed -e "s|{{RootNamespace}}|$ROOT_NAMESPACE|g" \
      -e "s|{{PackageVersion}}|$PACKAGE_VERSION|g" \
      "$TEMPLATE_DIR/Project.csproj.tpl" > "$OUTPUT_ABS/$PROJECT_FILE_NAME"
fi

if [[ -n "$SOLUTION" ]]; then
  if [[ ! -f "$SOLUTION" ]]; then
    echo "错误: 解决方案不存在: $SOLUTION" >&2
    exit 1
  fi
  run dotnet sln "$SOLUTION" add "$OUTPUT_ABS/$PROJECT_FILE_NAME"
fi

HOST_PROJECT_ABS="$(cd "$(dirname "$HOST_PROJECT")" && pwd)/$(basename "$HOST_PROJECT")"
RELATIVE_CLIENT_REF="$(python3 - <<PY
import os
host = os.path.dirname("$HOST_PROJECT_ABS")
client = "$OUTPUT_ABS/$PROJECT_FILE_NAME"
print(os.path.relpath(client, host).replace(os.sep, "/"))
PY
)"

if $DRY_RUN; then
  echo "[dry-run] 向 Host 添加 ProjectReference: $RELATIVE_CLIENT_REF"
else
  python3 - <<PY
import pathlib
import xml.etree.ElementTree as ET

host_path = pathlib.Path("$HOST_PROJECT_ABS")
client_ref = "$RELATIVE_CLIENT_REF"
tree = ET.parse(host_path)
root = tree.getroot()
ns = {"ms": "http://schemas.microsoft.com/developer/msbuild/2003"}

def find_item_groups(element):
    groups = []
    for child in element:
        tag = child.tag.split("}")[-1] if "}" in child.tag else child.tag
        if tag == "ItemGroup":
            groups.append(child)
    return groups

item_groups = find_item_groups(root)
project_ref_tag = None
for group in item_groups:
    for child in group:
        tag = child.tag.split("}")[-1] if "}" in child.tag else child.tag
        if tag == "ProjectReference":
            include = child.attrib.get("Include", "")
            if include.replace("\\\\", "/") == client_ref:
                print(f"Host 已包含 ProjectReference: {client_ref}")
                raise SystemExit(0)
            project_ref_tag = child.tag

if project_ref_tag is None:
    project_ref_tag = "ProjectReference"

target_group = None
for group in item_groups:
    for child in group:
        tag = child.tag.split("}")[-1] if "}" in child.tag else child.tag
        if tag == "ProjectReference":
            target_group = group
            break
    if target_group is not None:
        break

if target_group is None:
    target_group = ET.SubElement(root, "ItemGroup")

ref = ET.SubElement(target_group, project_ref_tag if "}" not in project_ref_tag else project_ref_tag.split("}")[1])
ref.set("Include", client_ref)

ET.indent(tree, space="  ")
tree.write(host_path, encoding="utf-8", xml_declaration=False)
print(f"已向 Host 添加 ProjectReference: {client_ref}")
PY
fi

cat <<'EOF'

==> 请在 Host 模块中注册 Elsa Studio 托管扩展（托管中间件由 Studio.AspNetCore 提供，勿在 Client 中实现）:

  // ConfigureServices:
  context.Services.AddBioTraceElsaAbpStudioHost(configuration);

  // OnApplicationInitialization（UseConfiguredEndpoints 之前）:
  app.UseBioTraceElsaAbpStudioHost();
  app.UseStaticFiles();
  // ... 其他中间件 ...
  app.UseBioTraceElsaAbpStudioFallback();

另请配置 Host appsettings 中 ElsaStudio:Enabled、ElsaStudio:PathBase，以及 OpenIddict 客户端 ElsaStudio 的 RedirectUris。

EOF

echo "完成。"

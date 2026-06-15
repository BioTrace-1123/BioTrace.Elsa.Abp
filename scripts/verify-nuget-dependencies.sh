#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
OUTPUT_DIR="${1:-$REPO_ROOT/artifacts/nuget-verify}"
CONFIGURATION="${CONFIGURATION:-Release}"
VERIFY_ONLY="${VERIFY_ONLY:-false}"

if [[ "${1:-}" == "--verify-only" ]]; then
  VERIFY_ONLY=true
  OUTPUT_DIR="${2:-$REPO_ROOT/artifacts/nuget}"
fi

PROJECTS=(
  "src/BioTrace.Elsa.Abp.Domain.Shared/BioTrace.Elsa.Abp.Domain.Shared.csproj"
  "src/BioTrace.Elsa.Abp.Domain/BioTrace.Elsa.Abp.Domain.csproj"
  "src/BioTrace.Elsa.Abp.Application.Contracts/BioTrace.Elsa.Abp.Application.Contracts.csproj"
  "src/BioTrace.Elsa.Abp.Application/BioTrace.Elsa.Abp.Application.csproj"
  "src/BioTrace.Elsa.Abp.HttpApi/BioTrace.Elsa.Abp.HttpApi.csproj"
  "src/BioTrace.Elsa.Abp.HttpApi.Client/BioTrace.Elsa.Abp.HttpApi.Client.csproj"
  "src/BioTrace.Elsa.Abp.AspNetCore/BioTrace.Elsa.Abp.AspNetCore.csproj"
  "src/BioTrace.Elsa.Abp.Studio.BlazorWasm/BioTrace.Elsa.Abp.Studio.BlazorWasm.csproj"
  "src/BioTrace.Elsa.Abp.Studio.AspNetCore/BioTrace.Elsa.Abp.Studio.AspNetCore.csproj"
  "src/BioTrace.Elsa.Abp.Installer/BioTrace.Elsa.Abp.Installer.csproj"
  "test/BioTrace.Elsa.Abp.IntegrationTesting/BioTrace.Elsa.Abp.IntegrationTesting.csproj"
)

if [[ "$VERIFY_ONLY" != "true" ]]; then
  echo "==> Restore & build"
  dotnet restore "$REPO_ROOT/BioTrace.Elsa.Abp.slnx"
  dotnet build "$REPO_ROOT/BioTrace.Elsa.Abp.slnx" -c "$CONFIGURATION" --no-restore

  rm -rf "$OUTPUT_DIR"
  mkdir -p "$OUTPUT_DIR"

  echo "==> Pack published projects to $OUTPUT_DIR"
  for project in "${PROJECTS[@]}"; do
    dotnet pack "$REPO_ROOT/$project" -c "$CONFIGURATION" --no-build -o "$OUTPUT_DIR"
  done
else
  echo "==> Verify existing packages in $OUTPUT_DIR"
  if ! compgen -G "$OUTPUT_DIR/*.nupkg" > /dev/null; then
    echo "错误: 未找到 nupkg: $OUTPUT_DIR" >&2
    exit 1
  fi
fi

echo "==> Verify BioTrace.Elsa.Abp.* transitive dependencies"
python3 - <<'PY' "$OUTPUT_DIR"
import sys
import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path

output_dir = Path(sys.argv[1])
ns = {"n": "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"}

def read_package_id(nupkg_path: Path) -> str:
    with zipfile.ZipFile(nupkg_path) as zf:
        nuspec_name = next(n for n in zf.namelist() if n.endswith(".nuspec"))
        root = ET.fromstring(zf.read(nuspec_name))
    return root.findtext("n:metadata/n:id", default="", namespaces=ns)

nupkgs = {read_package_id(p) for p in output_dir.glob("*.nupkg")}

errors = []
for nupkg_path in sorted(output_dir.glob("*.nupkg")):
    package_id = read_package_id(nupkg_path)
    with zipfile.ZipFile(nupkg_path) as zf:
        nuspec_name = next(n for n in zf.namelist() if n.endswith(".nuspec"))
        root = ET.fromstring(zf.read(nuspec_name))

    for dep in root.findall(".//n:dependency", ns):
        dep_id = dep.attrib.get("id", "")
        if not dep_id.startswith("BioTrace.Elsa.Abp."):
            continue
        if dep_id not in nupkgs:
            errors.append(f"{package_id} -> missing internal dependency {dep_id}")

if errors:
    print("NuGet 内部传递依赖校验失败:", file=sys.stderr)
    for err in errors:
        print(f"  - {err}", file=sys.stderr)
    raise SystemExit(1)

print(f"OK: verified {len(nupkgs)} packages in {output_dir}")
PY

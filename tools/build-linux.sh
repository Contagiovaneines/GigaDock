#!/usr/bin/env bash
set -euo pipefail
workspace=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
runtime=${GIGADOCK_RUNTIME:-linux-x64}
if [[ "$runtime" != linux-x64 ]]; then printf 'Arquitetura ainda não homologada: %s\n' "$runtime" >&2; exit 1; fi
dotnet_tool=${DOTNET:-dotnet}
publish="$workspace/src/GigaDock.App.Linux/bin/package-$runtime"
extra=()
if [[ -n "${GIGADOCK_ARTIFACTS:-}" ]]; then extra+=(--artifacts-path "$GIGADOCK_ARTIFACTS"); fi
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
"$dotnet_tool" publish "$workspace/src/GigaDock.App.Linux/GigaDock.App.Linux.csproj" -c Release -r "$runtime" --self-contained true -p:PublishTrimmed=false -o "$publish" "${extra[@]}"
python3 "$workspace/tools/linux-package.py" build --workspace "$workspace" --publish "$publish" --output "$workspace/release/linux"

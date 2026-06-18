#!/usr/bin/env bash
# Publishes self-contained, single-file Unbound installers for every target platform.
# Output: ./artifacts/<rid>/Unbound
#
# Usage:  build/publish.sh                 # all platforms
#         build/publish.sh linux-x64       # one platform
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
proj="$here/src/Unbound.App/Unbound.App.csproj"

if [ "$#" -gt 0 ]; then rids=("$@"); else rids=(win-x64 osx-x64 osx-arm64 linux-x64); fi

for rid in "${rids[@]}"; do
  echo "==> Publishing $rid"
  dotnet publish "$proj" -c Release -r "$rid" -o "artifacts/$rid"
  # Debug symbols (incl. native Skia/HarfBuzz .pdb) don't belong in a release artifact.
  rm -f "artifacts/$rid"/*.pdb
done

echo "Done. Single-file builds are in ./artifacts/<rid>/"

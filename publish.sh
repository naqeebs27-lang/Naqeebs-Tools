#!/usr/bin/env bash
# Builds a single-file, self-contained Linux x64 executable (no .NET install needed on the target PC).
# Usage: ./publish.sh            -> publish/SmartToolsSuite
set -euo pipefail
cd "$(dirname "$0")"
dotnet publish SmartTools/SmartTools.csproj -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
echo
echo "Done. Your program is in the 'publish' folder: SmartToolsSuite"

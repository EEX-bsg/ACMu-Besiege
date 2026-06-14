#!/usr/bin/env bash
set -euo pipefail

dotnet msbuild src/acmu/acmu.csproj /p:Configuration=Release

if [ -n "${BESIEGE_DIR:-}" ]; then
    DEST="$BESIEGE_DIR/Besiege_Data/Mods/ACMu"
    mkdir -p "$DEST"
    cp ACMu/acmu.dll "$DEST/"
    if [ -f ACMu/Mod.xml ]; then
        cp ACMu/Mod.xml "$DEST/"
    fi
    echo "Deployed to $DEST"
fi

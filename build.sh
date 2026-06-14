#!/usr/bin/env bash
set -euo pipefail

# vswhere で VS MSBuild を探す (dotnet msbuild は .NET 3.5 Unity プロファイル非対応)
VSWHERE="/c/Program Files (x86)/Microsoft Visual Studio/Installer/vswhere.exe"
MSBUILD=""
if [ -f "$VSWHERE" ]; then
    MSBUILD=$("$VSWHERE" -latest -requires Microsoft.Component.MSBuild \
        -find 'MSBuild\**\Bin\MSBuild.exe' 2>/dev/null | head -1 | tr '\\' '/')
fi
if [ -z "$MSBUILD" ]; then
    MSBUILD="/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe"
fi

"$MSBUILD" src/acmu/acmu.csproj /p:Configuration=Release /nologo /v:minimal

if [ -n "${BESIEGE_DIR:-}" ]; then
    DEST="$BESIEGE_DIR/Besiege_Data/Mods/ACMu"
    mkdir -p "$DEST"
    cp ACMu/acmu.dll "$DEST/"
    if [ -f ACMu/Mod.xml ]; then
        cp ACMu/Mod.xml "$DEST/"
    fi
    echo "Deployed to $DEST"
fi

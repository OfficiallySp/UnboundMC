#!/usr/bin/env pwsh
# Publishes self-contained, single-file Unbound installers for every target platform.
# Output: ./artifacts/<rid>/Unbound(.exe)
#
# Usage:  pwsh build/publish.ps1                 # all platforms
#         pwsh build/publish.ps1 win-x64         # one platform

$ErrorActionPreference = 'Stop'
$proj = Join-Path $PSScriptRoot '..' 'src/Unbound.App/Unbound.App.csproj'

$rids = if ($args.Count -gt 0) { $args } else { @('win-x64', 'osx-x64', 'osx-arm64', 'linux-x64') }

foreach ($rid in $rids) {
    Write-Host "==> Publishing $rid" -ForegroundColor Cyan
    dotnet publish $proj -c Release -r $rid -o "artifacts/$rid"
    if ($LASTEXITCODE -ne 0) { throw "publish failed for $rid" }
    # Debug symbols (incl. native Skia/HarfBuzz .pdb) don't belong in a release artifact.
    Get-ChildItem "artifacts/$rid" -Filter *.pdb -ErrorAction SilentlyContinue | Remove-Item -Force
}

Write-Host "Done. Single-file builds are in ./artifacts/<rid>/" -ForegroundColor Green

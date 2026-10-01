$ErrorActionPreference = 'Stop'
$repoPath = Split-Path $PSScriptRoot -Parent
Push-Location (Join-Path $repoPath 'frontend')
try {
    if (!(Test-Path -LiteralPath 'node_modules')) { & (Join-Path $PSScriptRoot 'Npm.ps1') ci }
    & (Join-Path $PSScriptRoot 'Npm.ps1') run dev
} finally { Pop-Location }

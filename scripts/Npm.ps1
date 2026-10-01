param([Parameter(ValueFromRemainingArguments = $true)][string[]]$NpmArguments)
$ErrorActionPreference = 'Stop'
$npmPath = Get-Command npm.cmd -ErrorAction SilentlyContinue
if ($npmPath) {
    & $npmPath.Source @NpmArguments
} else {
    # Use the desktop's bundled runtime when a system Node/npm installation is absent.
    $runtimePath = Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies'
    $nodeDirectory = Join-Path $runtimePath 'node/bin'
    $pnpmPath = Join-Path $runtimePath 'bin/fallback/pnpm.cmd'
    if (!(Test-Path -LiteralPath $pnpmPath) -or !(Test-Path -LiteralPath (Join-Path $nodeDirectory 'node.exe'))) {
        throw 'Chưa tìm thấy npm. Cài Node LTS kèm npm và mở lại terminal.'
    }
    $previousPath = $env:PATH
    try {
        $env:PATH = "$nodeDirectory;$previousPath"
        & $pnpmPath dlx npm @NpmArguments
    } finally { $env:PATH = $previousPath }
}
if ($LASTEXITCODE -ne 0) { throw "npm thất bại (exit $LASTEXITCODE)." }

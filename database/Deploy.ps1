param([string]$Server = '.\SQLEXPRESS', [switch]$Verify)
$ErrorActionPreference = 'Stop'
$scripts = @('001_schema.sql', '002_operations.sql', '004_auth_sessions.sql')
if ($Verify) { $scripts += '003_verify.sql' }
foreach ($script in $scripts) {
    & sqlcmd -S $Server -E -C -I -b -f 65001 -i (Join-Path $PSScriptRoot $script)
    if ($LASTEXITCODE -ne 0) { throw "SQL failed: $script (exit $LASTEXITCODE)" }
}

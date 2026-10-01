param([string]$Server = '.\SQLEXPRESS')
$ErrorActionPreference = 'Stop'

$testId = [guid]::NewGuid().ToString('N')
$databaseName = "SalesForecastTest_$testId"
$temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$scriptDirectory = Join-Path $temporaryRoot "SalesForecastDisposable_$testId"
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$oldConnection = $env:TEST_SQL_CONNECTION

try {
    New-Item -ItemType Directory -Path $scriptDirectory | Out-Null
    $scripts = @('001_schema.sql', '004_auth_sessions.sql', '005_english_schema.sql',
        '006_english_operations.sql', '007_schema_cleanup.sql', '008_english_defaults.sql',
        '009_product_enhancements.sql', '010_customers.sql', '011_order_workflow.sql',
        '012_hierarchical_categories.sql', '013_category_codes.sql')
    foreach ($script in $scripts) {
        $source = Join-Path $projectRoot "database/$script"
        $destination = Join-Path $scriptDirectory $script
        (Get-Content -LiteralPath $source -Raw).Replace('SalesForecastingDB', $databaseName) |
            Set-Content -LiteralPath $destination -Encoding utf8
        & sqlcmd -S $Server -E -C -I -b -f 65001 -i $destination
        if ($LASTEXITCODE -ne 0) { throw "Database setup failed: $script" }
    }
    $env:TEST_SQL_CONNECTION = "Server=$Server;Database=$databaseName;Trusted_Connection=True;TrustServerCertificate=True"
    & dotnet run --project (Join-Path $projectRoot 'tests/SalesForecastSystem.IntegrationChecks') -- --full-flow
    if ($LASTEXITCODE -ne 0) { throw "Disposable integration checks failed (exit $LASTEXITCODE)." }
}
finally {
    $env:TEST_SQL_CONNECTION = $oldConnection
    if ($databaseName -match '^SalesForecastTest_[0-9a-f]{32}$') {
        & sqlcmd -S $Server -E -C -I -b -Q "USE master; IF DB_ID(N'$databaseName') IS NOT NULL BEGIN ALTER DATABASE [$databaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$databaseName]; END"
        if ($LASTEXITCODE -ne 0) { Write-Warning "Could not drop disposable database $databaseName." }
    }
    $resolvedDirectory = [IO.Path]::GetFullPath($scriptDirectory)
    if ($resolvedDirectory.StartsWith($temporaryRoot, [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Path]::GetFileName($resolvedDirectory) -match '^SalesForecastDisposable_[0-9a-f]{32}$' -and
        (Test-Path -LiteralPath $resolvedDirectory)) {
        Remove-Item -LiteralPath $resolvedDirectory -Recurse -Force
    }
}

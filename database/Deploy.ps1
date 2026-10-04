param([string]$Server = '.\SQLEXPRESS', [switch]$Verify)
$ErrorActionPreference = 'Stop'
$scripts = @('001_schema.sql', '004_auth_sessions.sql', '005_english_schema.sql', '006_english_operations.sql', '007_schema_cleanup.sql', '008_english_defaults.sql', '009_product_enhancements.sql', '010_customers.sql', '011_order_workflow.sql', '012_hierarchical_categories.sql', '013_category_codes.sql', '014_user_codes.sql')
if ($Verify) { $scripts += '003_verify.sql' }
foreach ($script in $scripts) {
    & sqlcmd -S $Server -E -C -I -b -f 65001 -i (Join-Path $PSScriptRoot $script)
    if ($LASTEXITCODE -ne 0) { throw "SQL failed: $script (exit $LASTEXITCODE)" }
}

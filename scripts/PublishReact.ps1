param(
    [Parameter(Mandatory = $true)][string]$Destination,
    [string]$NpmCommand = (Join-Path $PSScriptRoot 'Npm.ps1')
)
$ErrorActionPreference = 'Stop'
$repoPath = Split-Path $PSScriptRoot -Parent
$publishPath = [IO.Path]::GetFullPath($Destination)
$repoFullPath = [IO.Path]::GetFullPath($repoPath).TrimEnd('\')
if ($publishPath.TrimEnd('\') -eq [IO.Path]::GetPathRoot($publishPath).TrimEnd('\') -or $publishPath -eq $repoFullPath -or $repoFullPath.StartsWith($publishPath.TrimEnd('\') + '\') -or $publishPath.StartsWith($repoFullPath + '\')) {
    throw 'Destination phải là thư mục publish riêng ngoài repository, không được dùng gốc ổ đĩa hoặc thư mục cha repository.'
}
Push-Location (Join-Path $repoPath 'frontend')
try {
    & $NpmCommand ci
    if ($LASTEXITCODE -ne 0) { throw 'npm ci thất bại.' }
    & $NpmCommand run build
    if ($LASTEXITCODE -ne 0) { throw 'Build frontend thất bại.' }
} finally { Pop-Location }
dotnet publish (Join-Path $repoPath 'SalesForecastSystem.API/SalesForecastSystem.API.csproj') -c Release -o $publishPath
if ($LASTEXITCODE -ne 0) { throw 'Publish API thất bại.' }
$webrootPath = Join-Path $publishPath 'wwwroot'
New-Item -ItemType Directory -Path $webrootPath -Force | Out-Null
# Copy build artifacts only. Never delete the webroot or runtime uploads.
Copy-Item -Path (Join-Path $repoPath 'frontend/dist/*') -Destination $webrootPath -Recurse -Force
Write-Host "Đã tạo bản publish React tại $publishPath. Chưa triển khai lên server."

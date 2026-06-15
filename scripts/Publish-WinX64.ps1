param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $projectRoot "GtavOfflineModLauncher.csproj"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$outputRoot = Join-Path $projectRoot (Join-Path "release\win-x64" $timestamp)
$publishDir = Join-Path $outputRoot "publish"
$zipPath = Join-Path $outputRoot "GTAVOfflineModLauncher-win-x64.zip"

New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

Write-Host "Publishing self-contained win-x64 build..." -ForegroundColor Cyan
dotnet publish $projectPath `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publishDir

Write-Host "Creating zip package..." -ForegroundColor Cyan
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force

Write-Host "Publish complete:" -ForegroundColor Green
Write-Host "  Folder: $publishDir"
Write-Host "  Zip:    $zipPath"

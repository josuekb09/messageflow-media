#Requires -Version 5.1
<#
.SYNOPSIS
  Publishes MessageFlow Media and compiles MessageFlowMediaSetup.exe with Inno Setup.

.NOTES
  Copyright (c) 2026 MessageFlow Media project author.
  Distributed free of charge for church use. Not for sale.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$env:TEMP = "D:\Temp"
$env:TMP = "D:\Temp"
$env:NUGET_PACKAGES = "D:\Temp\nuget"
$env:NUGET_HTTP_CACHE_PATH = "D:\Temp\nuget-http"
$env:DOTNET_CLI_HOME = "D:\Temp\dotnet-cli"
New-Item -ItemType Directory -Force -Path "D:\Temp\nuget", "D:\Temp\nuget-http", "D:\Temp\dotnet-cli" | Out-Null

$scriptDir = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDir)) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
}
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $scriptDir "..\..")).Path
}

$publishDir = Join-Path $RepoRoot "dist\publish"
$outputDir = Join-Path $RepoRoot "dist"
$databaseFile = Join-Path $RepoRoot "database\messageflow.db"
$issFile = Join-Path $RepoRoot "messageflow.iss"
$appProject = Join-Path $RepoRoot "src\MessageFlow.App\MessageFlow.App.csproj"

if (-not (Test-Path -LiteralPath $databaseFile)) {
    throw "The production database was not found: $databaseFile"
}

New-Item -ItemType Directory -Force -Path $publishDir, $outputDir, "D:\Temp" | Out-Null

$snapshotDb = "D:\Temp\bundled-messageflow.db"
$snapshotScript = Join-Path $scriptDir "snapshot-sqlite.py"
$verifyScript = Join-Path $scriptDir "verify-library.py"
if (Test-Path -LiteralPath $snapshotDb) {
    Remove-Item -LiteralPath $snapshotDb -Force
}

Write-Host "Checkpointing SQLite WAL and creating a consistent snapshot (VACUUM INTO)..."
python $snapshotScript $databaseFile $snapshotDb
if ($LASTEXITCODE -ne 0) {
    throw "SQLite snapshot failed."
}

Write-Host "Verifying bundled library counts..."
python $verifyScript $snapshotDb
if ($LASTEXITCODE -ne 0) {
    throw "Bundled SQLite snapshot failed library verification."
}

Write-Host "Publishing self-contained win-x64 Release..."
$env:TEMP = "D:\Temp"
$env:TMP = "D:\Temp"
$env:NUGET_PACKAGES = "D:\Temp\nuget"
$env:NUGET_HTTP_CACHE_PATH = "D:\Temp\nuget-http"
$env:DOTNET_CLI_HOME = "D:\Temp\dotnet-cli"
dotnet publish $appProject `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publishDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed."
}

$publishDatabaseDir = Join-Path $publishDir "database"
New-Item -ItemType Directory -Force -Path $publishDatabaseDir | Out-Null
Copy-Item -LiteralPath $snapshotDb -Destination (Join-Path $publishDatabaseDir "messageflow.db") -Force

$isccCandidates = @(
    (Join-Path $RepoRoot "Inno Setup 6\ISCC.exe"),
    (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
    (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
)
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    throw "Inno Setup Compiler (ISCC.exe) was not found. Install Inno Setup 6, then run: ISCC.exe `"$issFile`""
}

Write-Host "Compiling installer with $iscc..."
& $iscc "/DDatabaseFile=$snapshotDb" $issFile
if ($LASTEXITCODE -ne 0) {
    throw "ISCC failed."
}

$setup = Join-Path $outputDir "MessageFlowMediaSetup.exe"
if (-not (Test-Path -LiteralPath $setup)) {
    throw "Installer was not created: $setup"
}

Get-Item -LiteralPath $setup | Format-List FullName, Length, LastWriteTime
Write-Host "Installer ready: $setup"

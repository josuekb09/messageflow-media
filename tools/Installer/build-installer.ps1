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

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $scriptDir = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptDir)) {
        $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    }
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

Write-Host "Checkpointing SQLite WAL..."
python -c @"
import sqlite3, sys
path = sys.argv[1]
con = sqlite3.connect(path)
con.execute('PRAGMA wal_checkpoint(TRUNCATE)')
con.close()
print('checkpoint ok')
"@ $databaseFile

Write-Host "Publishing self-contained win-x64 Release..."
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
Copy-Item -LiteralPath $databaseFile -Destination (Join-Path $publishDatabaseDir "messageflow.db") -Force

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
& $iscc $issFile
if ($LASTEXITCODE -ne 0) {
    throw "ISCC failed."
}

$setup = Join-Path $outputDir "MessageFlowMediaSetup.exe"
if (-not (Test-Path -LiteralPath $setup)) {
    throw "Installer was not created: $setup"
}

Get-Item -LiteralPath $setup | Format-List FullName, Length, LastWriteTime
Write-Host "Installer ready: $setup"

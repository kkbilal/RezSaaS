param(
    [string] $OutputDirectory = "artifacts/backups"
)

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$resolvedOutputDirectory = Join-Path $repositoryRoot $OutputDirectory
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupPath = Join-Path $resolvedOutputDirectory "rezsaas-$timestamp.sql"

. (Join-Path $PSScriptRoot "Import-LocalEnvironment.ps1")

New-Item -ItemType Directory -Force -Path $resolvedOutputDirectory | Out-Null

$docker = Get-Command docker -ErrorAction Stop
$dumpArguments = @(
    "compose",
    "exec",
    "-T",
    "postgres",
    "pg_dump",
    "--username",
    $env:REZSAAS_POSTGRES_USER,
    "--dbname",
    $env:REZSAAS_POSTGRES_DB,
    "--clean",
    "--if-exists",
    "--no-owner",
    "--no-privileges"
)

& $docker.Source @dumpArguments | Set-Content -LiteralPath $backupPath -Encoding UTF8

if ($LASTEXITCODE -ne 0) {
    throw "PostgreSQL backup failed with exit code $LASTEXITCODE."
}

Write-Host "Created PostgreSQL backup at '$backupPath'."
Write-Output $backupPath

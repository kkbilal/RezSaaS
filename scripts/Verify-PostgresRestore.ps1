param(
    [string] $BackupPath = ""
)

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot

. (Join-Path $PSScriptRoot "Import-LocalEnvironment.ps1")

if ([string]::IsNullOrWhiteSpace($BackupPath)) {
    $createdBackupPath = & (Join-Path $PSScriptRoot "Backup-Postgres.ps1")
    $BackupPath = @($createdBackupPath)[-1]
}

$resolvedBackupPath = if ([System.IO.Path]::IsPathRooted($BackupPath)) {
    $BackupPath
}
else {
    Join-Path $repositoryRoot $BackupPath
}

if (-not (Test-Path -LiteralPath $resolvedBackupPath)) {
    throw "Backup '$resolvedBackupPath' was not found."
}

$verifyDatabaseName = "rezsaas_restore_verify_$((Get-Date).ToString("yyyyMMddHHmmss"))"
$docker = Get-Command docker -ErrorAction Stop

try {
    & $docker.Source compose exec -T postgres createdb `
        --username $env:REZSAAS_POSTGRES_USER `
        $verifyDatabaseName

    if ($LASTEXITCODE -ne 0) {
        throw "Could not create restore verification database '$verifyDatabaseName'."
    }

    Get-Content -LiteralPath $resolvedBackupPath -Raw |
        & $docker.Source compose exec -T postgres psql `
            --username $env:REZSAAS_POSTGRES_USER `
            --dbname $verifyDatabaseName `
            --set ON_ERROR_STOP=1

    if ($LASTEXITCODE -ne 0) {
        throw "Restore verification failed for '$resolvedBackupPath'."
    }

    $tableCount = & $docker.Source compose exec -T postgres psql `
        --username $env:REZSAAS_POSTGRES_USER `
        --dbname $verifyDatabaseName `
        --tuples-only `
        --no-align `
        --command "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema NOT IN ('pg_catalog', 'information_schema');"

    if ($LASTEXITCODE -ne 0) {
        throw "Restore verification metadata query failed."
    }

    Write-Host "Restore verification succeeded for '$resolvedBackupPath' into '$verifyDatabaseName'."
    Write-Host "Restored user table count: $($tableCount.Trim())."
}
finally {
    & $docker.Source compose exec -T postgres dropdb `
        --username $env:REZSAAS_POSTGRES_USER `
        --if-exists `
        --force `
        $verifyDatabaseName | Out-Null
}

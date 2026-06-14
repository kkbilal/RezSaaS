$repositoryRoot = Split-Path -Parent $PSScriptRoot
$environmentPath = Join-Path $repositoryRoot ".env"

if (-not (Test-Path -LiteralPath $environmentPath)) {
    throw "Local environment file '$environmentPath' is missing. Copy '.env.example' to '.env' and replace the password."
}

Get-Content -LiteralPath $environmentPath | ForEach-Object {
    $line = $_.Trim()

    if ($line.Length -eq 0 -or $line.StartsWith("#")) {
        return
    }

    $parts = $line.Split("=", 2)

    if ($parts.Length -ne 2 -or [string]::IsNullOrWhiteSpace($parts[0])) {
        throw "Invalid .env entry: '$line'"
    }

    Set-Item -Path "Env:$($parts[0].Trim())" -Value $parts[1].Trim()
}

$requiredVariables = @(
    "REZSAAS_POSTGRES_HOST",
    "REZSAAS_POSTGRES_PORT",
    "REZSAAS_POSTGRES_DB",
    "REZSAAS_POSTGRES_USER",
    "REZSAAS_POSTGRES_PASSWORD"
)

foreach ($variableName in $requiredVariables) {
    $value = [Environment]::GetEnvironmentVariable($variableName)

    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Environment variable '$variableName' is required."
    }
}

function ConvertTo-ConnectionStringValue {
    param([string] $Value)

    return '"' + $Value.Replace('"', '""') + '"'
}

$hostValue = ConvertTo-ConnectionStringValue $env:REZSAAS_POSTGRES_HOST
$portValue = ConvertTo-ConnectionStringValue $env:REZSAAS_POSTGRES_PORT
$databaseValue = ConvertTo-ConnectionStringValue $env:REZSAAS_POSTGRES_DB
$userValue = ConvertTo-ConnectionStringValue $env:REZSAAS_POSTGRES_USER
$passwordValue = ConvertTo-ConnectionStringValue $env:REZSAAS_POSTGRES_PASSWORD

$env:ConnectionStrings__IdentityDatabase =
    "Host=$hostValue;Port=$portValue;Database=$databaseValue;Username=$userValue;Password=$passwordValue"
$env:ConnectionStrings__IntegrationsDatabase =
    "Host=$hostValue;Port=$portValue;Database=$databaseValue;Username=$userValue;Password=$passwordValue"
$env:ConnectionStrings__AdminDatabase =
    "Host=$hostValue;Port=$portValue;Database=$databaseValue;Username=$userValue;Password=$passwordValue"
$env:ConnectionStrings__TenantManagementDatabase =
    "Host=$hostValue;Port=$portValue;Database=$databaseValue;Username=$userValue;Password=$passwordValue"
$env:ConnectionStrings__MessagingDatabase =
    "Host=$hostValue;Port=$portValue;Database=$databaseValue;Username=$userValue;Password=$passwordValue"
$env:ConnectionStrings__OrganizationDatabase =
    "Host=$hostValue;Port=$portValue;Database=$databaseValue;Username=$userValue;Password=$passwordValue"
$env:ConnectionStrings__PaymentsDatabase =
    "Host=$hostValue;Port=$portValue;Database=$databaseValue;Username=$userValue;Password=$passwordValue"
$env:ConnectionStrings__CatalogDatabase =
    "Host=$hostValue;Port=$portValue;Database=$databaseValue;Username=$userValue;Password=$passwordValue"
$env:ConnectionStrings__ResourcesDatabase =
    "Host=$hostValue;Port=$portValue;Database=$databaseValue;Username=$userValue;Password=$passwordValue"
$env:ConnectionStrings__AvailabilityDatabase =
    "Host=$hostValue;Port=$portValue;Database=$databaseValue;Username=$userValue;Password=$passwordValue"
$env:ConnectionStrings__BookingDatabase =
    "Host=$hostValue;Port=$portValue;Database=$databaseValue;Username=$userValue;Password=$passwordValue"
$env:REZSAAS_TEST_POSTGRES_CONNECTION_STRING =
    "Host=$hostValue;Port=$portValue;Database=""postgres"";Username=$userValue;Password=$passwordValue"

Write-Host "Loaded local RezSaaS environment from '$environmentPath'."

param(
    [string] $OpenApiPath = "artifacts/openapi/rezsaas-api-v1.json",

    [string] $WebAppPath = "src/Apps/RezSaaS.Web",

    [string] $OutputPath = "src/shared/api/rezsaas-api.generated.ts"
)

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$resolvedOpenApiPath = Join-Path $repositoryRoot $OpenApiPath
$resolvedWebAppPath = Join-Path $repositoryRoot $WebAppPath
$packageJsonPath = Join-Path $resolvedWebAppPath "package.json"
$resolvedOutputPath = Join-Path $resolvedWebAppPath $OutputPath
$outputDirectory = Split-Path -Parent $resolvedOutputPath

if (-not (Test-Path -LiteralPath $resolvedOpenApiPath)) {
    throw "OpenAPI artifact '$resolvedOpenApiPath' was not found. Run scripts/Export-OpenApi.ps1 first."
}

if (-not (Test-Path -LiteralPath $packageJsonPath)) {
    throw "Frontend app '$resolvedWebAppPath' is not initialized yet. Create RezSaaS.Web before generating TypeScript API types."
}

New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

Push-Location $resolvedWebAppPath
try {
    npx --yes openapi-typescript $resolvedOpenApiPath --output $resolvedOutputPath
}
finally {
    Pop-Location
}

Write-Host "Generated TypeScript OpenAPI types at '$resolvedOutputPath'."

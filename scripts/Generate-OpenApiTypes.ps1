param(
    [string] $OpenApiPath = "artifacts/openapi/rezsaas-api-v1.json",

    [string] $WebAppPath = "src/Apps/RezSaaS.Web",

    [string] $OutputPath = "src/shared/api/rezsaas-api.generated.ts",

    [string] $NodePath = ""
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
    $pnpm = Get-Command pnpm -ErrorAction SilentlyContinue
    $npx = Get-Command npx -ErrorAction SilentlyContinue
    $localOpenApiTypescript = Join-Path $resolvedWebAppPath "node_modules/openapi-typescript/bin/cli.js"

    if ($pnpm) {
        pnpm exec openapi-typescript $resolvedOpenApiPath --output $resolvedOutputPath
    }
    elseif ($npx) {
        npx --yes openapi-typescript@7.13.0 $resolvedOpenApiPath --output $resolvedOutputPath
    }
    elseif ((Test-Path -LiteralPath $localOpenApiTypescript) -and ($NodePath -or (Get-Command node -ErrorAction SilentlyContinue))) {
        $nodeExecutable = if ($NodePath) { $NodePath } else { (Get-Command node -ErrorAction Stop).Source }

        if (-not (Test-Path -LiteralPath $nodeExecutable)) {
            throw "Node executable '$nodeExecutable' was not found."
        }

        & $nodeExecutable $localOpenApiTypescript $resolvedOpenApiPath --output $resolvedOutputPath
    }
    else {
        throw "Neither pnpm nor npx is available, and local openapi-typescript cannot be run. Install Node.js with pnpm 11, run pnpm install, or pass -NodePath to this script."
    }

    if ($LASTEXITCODE -ne 0) {
        throw "OpenAPI TypeScript generation failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

Write-Host "Generated TypeScript OpenAPI types at '$resolvedOutputPath'."

param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Debug",

    [string] $Framework = "net10.0",

    [string] $DocumentName = "v1",

    [string] $OutputPath = "artifacts/openapi/rezsaas-api-v1.json",

    [switch] $SkipBuild
)

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot "src/Apps/RezSaaS.Api/RezSaaS.Api.csproj"
$projectDirectory = Split-Path -Parent $projectPath
$assemblyPath = Join-Path $repositoryRoot "src/Apps/RezSaaS.Api/bin/$Configuration/$Framework/RezSaaS.Api.dll"
$resolvedOutputPath = Join-Path $repositoryRoot $OutputPath
$outputDirectory = Split-Path -Parent $resolvedOutputPath

. (Join-Path $PSScriptRoot "Import-LocalEnvironment.ps1")

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:Messaging__PlatformNotificationWorker__Enabled = "false"
$env:Operations__Reconciliation__Enabled = "false"

if (-not $SkipBuild) {
    dotnet build $projectPath --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "API build failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path -LiteralPath $assemblyPath)) {
    throw "API assembly '$assemblyPath' was not found. Build the API before exporting OpenAPI."
}

New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

dotnet tool restore
if ($LASTEXITCODE -ne 0) {
    throw "dotnet tool restore failed with exit code $LASTEXITCODE."
}

Push-Location $projectDirectory
try {
    dotnet swagger tofile --output $resolvedOutputPath $assemblyPath $DocumentName
    if ($LASTEXITCODE -ne 0) {
        throw "OpenAPI export failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

Write-Host "Exported RezSaaS OpenAPI '$DocumentName' artifact to '$resolvedOutputPath'."

param(
    [string] $Email = "admin.local@rezsaas.test",
    [int] $RecoveryCodeCount = 10,
    [string] $OutputPath = "artifacts/local/platform-admin-mfa-recovery-codes.txt"
)

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot "Import-LocalEnvironment.ps1")

if ($RecoveryCodeCount -lt 1 -or $RecoveryCodeCount -gt 20) {
    throw "RecoveryCodeCount must be between 1 and 20."
}

$toolDirectory = Join-Path $repositoryRoot "artifacts/local/tools/PlatformAdminMfaSetup"
$projectPath = Join-Path $toolDirectory "PlatformAdminMfaSetup.csproj"
$programPath = Join-Path $toolDirectory "Program.cs"
$absoluteOutputPath = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
} else {
    Join-Path $repositoryRoot $OutputPath
}

New-Item -ItemType Directory -Force -Path $toolDirectory | Out-Null
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $absoluteOutputPath) | Out-Null

$identityProjectPath = Join-Path $repositoryRoot "src/Modules/RezSaaS.Modules.Identity/RezSaaS.Modules.Identity.csproj"
$escapedIdentityProjectPath = [System.Security.SecurityElement]::Escape($identityProjectPath)

@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="$escapedIdentityProjectPath" />
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $projectPath -Encoding UTF8

@'
using System.Text;
using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RezSaaS.Modules.Identity.Domain;
using RezSaaS.Modules.Identity.Infrastructure.Persistence;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: PlatformAdminMfaSetup <email> <recoveryCodeCount> <outputPath>");
    return 64;
}

string email = args[0];
int recoveryCodeCount = int.Parse(args[1], CultureInfo.InvariantCulture);
string outputPath = args[2];
string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__IdentityDatabase")
    ?? throw new InvalidOperationException("ConnectionStrings__IdentityDatabase is required.");

ServiceCollection services = new();

services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(connectionString));
services
    .AddIdentityCore<UserAccount>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<IdentityDbContext>();

await using ServiceProvider provider = services.BuildServiceProvider();
UserManager<UserAccount> userManager = provider.GetRequiredService<UserManager<UserAccount>>();
IdentityDbContext dbContext = provider.GetRequiredService<IdentityDbContext>();

UserAccount? user = await userManager.FindByEmailAsync(email);

if (user is null)
{
    Console.Error.WriteLine($"User '{email}' was not found.");
    return 2;
}

IList<string> roles = await userManager.GetRolesAsync(user);

if (!roles.Contains("PlatformAdmin", StringComparer.Ordinal))
{
    Console.Error.WriteLine($"User '{email}' does not have the PlatformAdmin role.");
    return 3;
}

IdentityResult resetKeyResult = await userManager.ResetAuthenticatorKeyAsync(user);

if (!resetKeyResult.Succeeded)
{
    WriteErrors("ResetAuthenticatorKey", resetKeyResult);
    return 4;
}

IdentityResult enableMfaResult = await userManager.SetTwoFactorEnabledAsync(user, true);

if (!enableMfaResult.Succeeded)
{
    WriteErrors("SetTwoFactorEnabled", enableMfaResult);
    return 5;
}

IEnumerable<string>? recoveryCodes =
    await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, recoveryCodeCount);
string? authenticatorKey = await userManager.GetAuthenticatorKeyAsync(user);

if (string.IsNullOrWhiteSpace(authenticatorKey) || recoveryCodes is null)
{
    Console.Error.WriteLine("MFA setup did not return an authenticator key or recovery codes.");
    return 6;
}

DateTimeOffset now = DateTimeOffset.UtcNow;
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

StringBuilder builder = new();
builder.AppendLine("# RezSaaS local PlatformAdmin MFA recovery codes");
builder.AppendLine("# This file is local-only and ignored by git. Do not commit or share it.");
builder.Append("Email: ");
builder.AppendLine(email);
builder.Append("GeneratedAtUtc: ");
builder.AppendLine(now.ToString("O", CultureInfo.InvariantCulture));
builder.AppendLine();
builder.AppendLine("AuthenticatorKey:");
builder.AppendLine(authenticatorKey);
builder.AppendLine();
builder.AppendLine("RecoveryCodes:");

foreach (string code in recoveryCodes)
{
    builder.AppendLine(code);
}

await File.WriteAllTextAsync(outputPath, builder.ToString());

dbContext.IdentityAuditLogEntries.Add(
    IdentityAuditLogEntry.Create(
        actorUserAccountId: user.Id,
        subjectUserAccountId: user.Id,
        action: "LocalDevelopmentPlatformAdminMfaEnabled",
        detailsJson: $$"""{"recoveryCodeCount":{{recoveryCodeCount}},"generatedAtUtc":"{{now:O}}"}""",
        occurredAtUtc: now));
await dbContext.SaveChangesAsync();

Console.WriteLine($"MFA enabled for '{email}'.");
Console.WriteLine($"Recovery codes were written to '{outputPath}'.");
return 0;

static void WriteErrors(string operation, IdentityResult result)
{
    Console.Error.WriteLine($"{operation} failed:");

    foreach (IdentityError error in result.Errors)
    {
        Console.Error.WriteLine($"- {error.Code}: {error.Description}");
    }
}
'@ | Set-Content -LiteralPath $programPath -Encoding UTF8

$dotnetArguments = @(
    "run",
    "--project",
    $projectPath,
    "--",
    $Email,
    $RecoveryCodeCount.ToString([Globalization.CultureInfo]::InvariantCulture),
    $absoluteOutputPath
)

& dotnet @dotnetArguments

if ($LASTEXITCODE -ne 0) {
    throw "Local PlatformAdmin MFA setup failed with exit code $LASTEXITCODE."
}

Write-Host "Local PlatformAdmin MFA setup completed."
Write-Host "Recovery codes are stored at: $absoluteOutputPath"

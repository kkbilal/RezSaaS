param(
    [string] $Email = "",
    [string] $Password = "",
    [switch] $ResetAll
)

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot "Import-LocalEnvironment.ps1")

# Default test password: length 16, upper/lower/digit/symbol -> satisfies Identity policy
# (min 12, unique 4, requires digit, requires non-alphanumeric in API tests).
$defaultPassword = "RezSaaS!Local2026"

$accounts = @(
    @{ Email = "admin.local@rezsaas.test";          Password = $defaultPassword; Role = "PlatformAdmin (global)" },
    @{ Email = "owner.local@rezsaas.test";           Password = $defaultPassword; Role = "BusinessOwner (tenant: RezSaaS Demo Salon)" },
    @{ Email = "branch.manager.local@rezsaas.test";  Password = $defaultPassword; Role = "BranchManager (tenant: RezSaaS Demo Salon)" },
    @{ Email = "staff.local@rezsaas.test";           Password = $defaultPassword; Role = "Staff (tenant: RezSaaS Demo Salon)" },
    @{ Email = "customer.local@rezsaas.test";        Password = $defaultPassword; Role = "Customer" },
    @{ Email = "customer2.local@rezsaas.test";       Password = $defaultPassword; Role = "Customer" }
)

if ($ResetAll) {
    $targets = $accounts
} elseif ($Email -ne "") {
    $pwd = if ($Password -ne "") { $Password } else { $defaultPassword }
    $targets = @( @{ Email = $Email; Password = $pwd; Role = "(custom)" } )
} else {
    Write-Host "Local development account reference (no changes applied)."
    Write-Host ""
    Write-Host "Default password (use -ResetAll to apply): $defaultPassword"
    Write-Host ""
    Write-Host "Accounts:"
    $accounts | ForEach-Object {
        Write-Host ("  - {0,-35} {1}" -f $_.Email, $_.Role)
    }
    Write-Host ""
    Write-Host "Usage:"
    Write-Host "  .\Reset-LocalPasswords.ps1                  # show reference only"
    Write-Host "  .\Reset-LocalPasswords.ps1 -ResetAll        # reset all local accounts to default password"
    Write-Host "  .\Reset-LocalPasswords.ps1 -Email x@y -Password z"
    return
}

$toolDirectory = Join-Path $repositoryRoot "artifacts/local/tools/LocalPasswordReset"
$projectPath = Join-Path $toolDirectory "LocalPasswordReset.csproj"
$programPath = Join-Path $toolDirectory "Program.cs"

New-Item -ItemType Directory -Force -Path $toolDirectory | Out-Null

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

# Build the JSON payload that the inner Program.cs reads from argv[1].
$payload = $targets | ForEach-Object {
    [pscustomobject]@{ email = $_.Email; password = $_.Password }
} | ConvertTo-Json -Compress

$payloadPath = Join-Path $toolDirectory "reset-payload.json"
$payload | Set-Content -LiteralPath $payloadPath -Encoding UTF8

@'
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RezSaaS.Modules.Identity.Domain;
using RezSaaS.Modules.Identity.Infrastructure.Persistence;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: LocalPasswordReset <payloadPath>");
    return 64;
}

string payloadPath = args[0];
string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__IdentityDatabase")
    ?? throw new InvalidOperationException("ConnectionStrings__IdentityDatabase is required.");

if (!File.Exists(payloadPath))
{
    Console.Error.WriteLine($"Payload file not found: {payloadPath}");
    return 65;
}

string payloadJson = await File.ReadAllTextAsync(payloadPath);

JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };
List<ResetEntry>? entries =
    JsonSerializer.Deserialize<List<ResetEntry>>(payloadJson, jsonOptions)
    ?? throw new InvalidOperationException("Payload could not be deserialized.");

ServiceCollection services = new();
services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(connectionString));

// Register the default token providers so ResetPasswordAsync works standalone.
services.AddIdentityCore<UserAccount>(options =>
    {
        options.Tokens.ProviderMap["Default"] = new TokenProviderDescriptor(
            typeof(DataProtectorTokenProvider<UserAccount>));
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<IdentityDbContext>()
    .AddDefaultTokenProviders()
    .AddTokenProvider<DataProtectorTokenProvider<UserAccount>>("Default");

// DataProtection is required by DataProtectorTokenProvider.
services.AddDataProtection();

await using ServiceProvider provider = services.BuildServiceProvider();
UserManager<UserAccount> userManager = provider.GetRequiredService<UserManager<UserAccount>>();
IdentityDbContext dbContext = provider.GetRequiredService<IdentityDbContext>();

DateTimeOffset now = DateTimeOffset.UtcNow;
int resetCount = 0;

foreach (ResetEntry entry in entries)
{
    UserAccount? user = await userManager.FindByEmailAsync(entry.Email);

    if (user is null)
    {
        Console.Error.WriteLine($"[skip] User '{entry.Email}' not found.");
        continue;
    }

    if (user.Status is not AccountStatus.Active)
    {
        Console.Error.WriteLine($"[skip] User '{entry.Email}' is not Active (Status={user.Status}).");
        continue;
    }

    // Directly hash and set the password. This avoids relying on token providers
    // and mirrors what UserManager.ResetPasswordAsync does internally.
    user.PasswordHash = userManager.PasswordHasher.HashPassword(user, entry.Password);
    IdentityResult updateResult = await userManager.UpdateAsync(user);

    if (!updateResult.Succeeded)
    {
        Console.Error.WriteLine($"[fail] Reset failed for '{entry.Email}':");
        foreach (IdentityError error in updateResult.Errors)
        {
            Console.Error.WriteLine($"    - {error.Code}: {error.Description}");
        }
        continue;
    }

    // Clear any stale lockout so the local account is immediately usable.
    await userManager.ResetAccessFailedCountAsync(user);
    await userManager.SetLockoutEndDateAsync(user, null);

    dbContext.IdentityAuditLogEntries.Add(
        IdentityAuditLogEntry.Create(
            actorUserAccountId: user.Id,
            subjectUserAccountId: user.Id,
            action: "LocalDevelopmentPasswordReset",
            detailsJson: $$"""{"email":"{{entry.Email}}","resetAtUtc":"{{now:O}}"}""",
            occurredAtUtc: now));

    Console.WriteLine($"[ok]   Password reset for '{entry.Email}'.");
    resetCount++;
}

if (resetCount > 0)
{
    await dbContext.SaveChangesAsync();
}

Console.WriteLine($"Done. {resetCount}/{entries.Count} password(s) reset.");
return 0;

internal sealed class ResetEntry
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
'@ | Set-Content -LiteralPath $programPath -Encoding UTF8

$dotnetArguments = @(
    "run",
    "--project",
    $projectPath,
    "--",
    $payloadPath
)

& dotnet @dotnetArguments

if ($LASTEXITCODE -ne 0) {
    throw "Local password reset failed with exit code $LASTEXITCODE."
}

Write-Host ""
Write-Host "Local password reset completed."
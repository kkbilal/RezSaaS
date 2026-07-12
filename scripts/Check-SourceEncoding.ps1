<#
.SYNOPSIS
  UTF-8 / mojibake protection gate (AGENTS.md section 9.1 reference script).

.DESCRIPTION
  Scans all source files (.cs, .ts, .tsx, .css, .md, .mjs, .cjs, .json) in the
  repo as raw bytes and applies two checks:

    1. UTF-8 strict decode: invalid byte sequence fails.
       Equivalent of TextDecoder('utf-8', { fatal: true }) — UTF8Encoding(false, $true).
    2. Common mojibake patterns and U+FFFD (REPLACEMENT CHARACTER).

  Turkish UTF-8 bytes mis-decoded as Windows-1252/1254 yield these patterns:
    0xC4 0xB1 -> "A-tilde + 1-superscript"   (correct: i-dotless, U+0131)
    0xC5 0x9F -> "A-ring + Y-diaeresis"      (correct: s-cedilla, U+015F)
    ... etc.

  Patterns are defined via Unicode code points to be encoding-agnostic in the
  script source itself (PowerShell 5.1 reads this file without BOM as Windows-1252,
  so literal multi-byte mojibake glyphs in the script body would corrupt parsing).

  Run before: dotnet build, pnpm typecheck, pnpm test, CI lint, pre-commit.

.NOTES
  Windows + PowerShell trap (AGENTS.md section 9.1): PowerShell 5.1 cannot combine
  Get-Content -Raw with -Encoding UTF8, and Set-Content defaults to Windows-1252.
  This script uses [System.IO.File]::ReadAllBytes + [System.Text.UTF8Encoding]
  to stay safe.

.EXAMPLE
  powershell -File scripts/Check-SourceEncoding.ps1
  powershell -File scripts/Check-SourceEncoding.ps1 -Path src,tests
#>

[CmdletBinding()]
param(
    [string[]] $Path = @("src", "tests", "scripts", "docs"),
    [string[]] $Include = @("*.cs", "*.ts", "*.tsx", "*.css", "*.md", "*.mjs", "*.cjs", "*.json"),
    [switch] $Detailed
)

$ErrorActionPreference = "Stop"

# UTF-8 strict decoder: emitBOM=false, throwOnInvalidBytes=true
$utf8Strict = New-Object System.Text.UTF8Encoding($false, $true)

# Mojibake patterns as Unicode code points (encoding-agnostic in script source).
# These appear when Turkish UTF-8 bytes are decoded as Windows-1252.
# Format: array of (code-point array) -> string. Built at runtime so the script
# body stays pure ASCII for PowerShell 5.1 without-BOM compatibility.
function Build-MojibakePatterns {
    $patterns = @()
    # Common Turkish mojibake: each pair is the Windows-1252 rendering of UTF-8 bytes
    $codePointPairs = @(
        @(0x00C3, 0x00A7),  # c-cedilla
        @(0x00C4, 0x00B1),  # i-dotless
        @(0x00C5, 0x009F),  # s-cedilla
        @(0x00C4, 0x009F),  # g-breve
        @(0x00C3, 0x00BC),  # u-diaeresis
        @(0x00C3, 0x00B6),  # o-diaeresis
        @(0x00C4, 0x00B0),  # I-dot
        @(0x00C5, 0x009E),  # S-cedilla
        @(0x00C4, 0x009E),  # G-breve
        @(0x00C3, 0x0087),  # C-cedilla
        @(0x00C3, 0x009C),  # U-diaeresis
        @(0x00C3, 0x0096),  # O-diaeresis
        @(0x00C3, 0x00A4),  # a-diaeresis
        @(0x00C3, 0x00A9),  # e-acute
        @(0x00C3, 0x00A1)   # a-acute
    )
    foreach ($pair in $codePointPairs) {
        $str = ""
        foreach ($cp in $pair) {
            $str += [char]$cp
        }
        $patterns += $str
    }
    return $patterns
}

$mojibakePatterns = Build-MojibakePatterns

# U+FFFD REPLACEMENT CHARACTER
$replacementChar = [char]0xFFFD

function Test-FileEncoding {
    param([string]$FilePath)

    $bytes = $null
    try {
        $bytes = [System.IO.File]::ReadAllBytes($FilePath)
    }
    catch {
        return @{ Status = "read-error"; Detail = $_.Exception.Message }
    }

    if ($bytes.Length -eq 0) {
        return @{ Status = "ok"; Detail = "" }
    }

    # BOM check: UTF-8 BOM (EF BB BF) triggers a warning — policy is UTF-8 without BOM
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
        return @{ Status = "bom"; Detail = "UTF-8 BOM present; policy is UTF-8 without BOM" }
    }

    # Strict UTF-8 decode — invalid byte sequence throws
    $decoded = $null
    try {
        $decoded = $utf8Strict.GetString($bytes)
    }
    catch {
        return @{ Status = "invalid-utf8"; Detail = "Invalid UTF-8 byte sequence: $($_.Exception.Message)" }
    }

    # U+FFFD check
    if ($decoded.IndexOf($replacementChar) -ge 0) {
        $count = 0
        foreach ($ch in $decoded.ToCharArray()) {
            if ($ch -eq $replacementChar) { $count++ }
        }
        return @{
            Status = "replacement-char"
            Detail = "$count U+FFFD (REPLACEMENT CHARACTER) found - signals data loss during decode"
        }
    }

    # Mojibake pattern check
    foreach ($pattern in $mojibakePatterns) {
        if ($decoded.IndexOf($pattern) -ge 0) {
            return @{
                Status = "mojibake-pattern"
                Detail = "Mojibake pattern detected (UTF-8 bytes decoded as Windows-1252)"
            }
        }
    }

    return @{ Status = "ok"; Detail = "" }
}

$root = (Get-Location).Path
$filesToCheck = @()

foreach ($p in $Path) {
    $fullPath = Join-Path $root $p
    if (-not (Test-Path -LiteralPath $fullPath)) {
        if ($Detailed) { Write-Host "[skip] Path missing: $p" }
        continue
    }
    foreach ($inc in $Include) {
        $filesToCheck += @(Get-ChildItem -Path $fullPath -Filter $inc -Recurse -File -ErrorAction SilentlyContinue)
    }
}

$excludePatterns = @(
    "[\\/]node_modules[\\/]",
    "[\\/]\.next[\\/]",
    "[\\/]bin[\\/]",
    "[\\/]obj[\\/]",
    "[\\/]artifacts[\\/]",
    "[\\/]\.git[\\/]"
)

$filteredFiles = $filesToCheck | Where-Object {
    $f = $_.FullName
    $excluded = $false
    foreach ($pat in $excludePatterns) {
        if ($f -match $pat) { $excluded = $true; break }
    }
    -not $excluded
}

if ($filteredFiles.Count -eq 0) {
    Write-Host "[ok] No source files matched the path/include filters"
    exit 0
}

if ($Detailed) {
    Write-Host "[info] Scanning $($filteredFiles.Count) files..."
}

$failures = @()
$okCount = 0
$bomCount = 0

foreach ($file in $filteredFiles) {
    $result = Test-FileEncoding -FilePath $file.FullName

    switch ($result.Status) {
        "ok" {
            $okCount++
        }
        "bom" {
            # BOM is a policy preference (UTF-8 without BOM), NOT a mojibake failure.
            # Counted separately as a warning; does not affect exit code.
            $bomCount++
        }
        default {
            $failures += [PSCustomObject]@{
                File = $file.FullName.Substring($root.Length).TrimStart('\', '/')
                Status = $result.Status
                Detail = $result.Detail
            }
        }
    }
}

Write-Host ""
Write-Host "=== Mojibake / Encoding Check Summary ==="
Write-Host "  Files scanned:            $($filteredFiles.Count)"
Write-Host "  Clean (UTF-8 no BOM):     $okCount"
Write-Host "  BOM warnings:             $bomCount"
Write-Host "  Failures:                 $($failures.Count)"
Write-Host ""

if ($failures.Count -eq 0) {
    if ($bomCount -gt 0) {
        Write-Host "[warn] $bomCount file(s) have UTF-8 BOM (policy prefers without BOM, but BOM is not a failure)." -ForegroundColor Yellow
    }
    Write-Host "[ok] All source files are valid UTF-8 and mojibake-free."
    exit 0
}

Write-Host "[FAIL] Encoding issues detected in $($failures.Count) file(s):" -ForegroundColor Red
Write-Host ""
$failures | Format-Table -AutoSize -Wrap | Out-String | Write-Host

Write-Host ""
Write-Host "Recovery (AGENTS.md section 9.1):" -ForegroundColor Yellow
Write-Host "  1. Restore original:                git checkout -- <file>"
Write-Host "  2. Re-apply changes encoding-safe:"
Write-Host "     - Read:    [System.IO.File]::ReadAllBytes"
Write-Host "     - Decode:  [System.Text.Encoding]::UTF8.GetString"
Write-Host "     - Edit, then:"
Write-Host "     - Write:   [System.IO.File]::WriteAllBytes (UTF-8, no BOM)"
Write-Host "  3. NEVER commit a mojibake-corrupted file."
Write-Host ""
exit 1

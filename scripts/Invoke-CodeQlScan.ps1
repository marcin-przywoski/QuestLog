[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [switch]$ReuseDatabase,

    [switch]$SkipCommunityPack
)

$ErrorActionPreference = "Stop"

function Invoke-CodeQlCommand {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    & gh codeql @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "CodeQL command failed with exit code $LASTEXITCODE."
    }
}

function Get-SarifResultCount {
    param(
        [Parameter(Mandatory)]
        [string]$ReportPath
    )

    $sarif = Get-Content -Raw -LiteralPath $ReportPath | ConvertFrom-Json
    return @($sarif.runs | ForEach-Object { $_.results }).Count
}

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI is required. Install it from https://cli.github.com/."
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET SDK is required to build the solution. Install it from https://dotnet.microsoft.com/download."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $repoRoot "QuestLog.slnx"
$databasePath = Join-Path $repoRoot ".codeql-db"
$reportDirectory = Join-Path $repoRoot "artifacts\codeql"
$officialReportPath = Join-Path $reportDirectory "official.sarif"
$communityReportPath = Join-Path $reportDirectory "community.sarif"

if (-not (Test-Path -LiteralPath $solutionPath)) {
    throw "Expected solution file was not found: $solutionPath"
}

Push-Location $repoRoot

try {
    & gh codeql version *> $null

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Installing the GitHub CodeQL CLI extension..."
        & gh extension install github/gh-codeql

        if ($LASTEXITCODE -ne 0) {
            throw "Unable to install the GitHub CodeQL CLI extension."
        }
    }

    New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null

    if ((Test-Path -LiteralPath $databasePath) -and -not $ReuseDatabase) {
        Write-Host "Removing the existing CodeQL database..."
        Remove-Item -LiteralPath $databasePath -Recurse -Force
    }

    if (-not (Test-Path -LiteralPath $databasePath)) {
        $buildCommand = "dotnet build `"$solutionPath`" --configuration $Configuration"

        Write-Host "Creating a C# CodeQL database..."
        Invoke-CodeQlCommand -Arguments @(
            "database", "create", $databasePath,
            "--language=csharp",
            "--source-root=$repoRoot",
            "--command=$buildCommand"
        )
    }

    Write-Host "Downloading the official C# query pack..."
    Invoke-CodeQlCommand -Arguments @("pack", "download", "codeql/csharp-queries")

    Write-Host "Running the official C# security suites..."
    Invoke-CodeQlCommand -Arguments @(
        "database", "analyze", $databasePath,
        "codeql/csharp-queries:codeql-suites/csharp-security-extended.qls",
        "codeql/csharp-queries:codeql-suites/csharp-security-and-quality.qls",
        "--format=sarif-latest",
        "--sarif-category=official-csharp",
        "--output=$officialReportPath"
    )

    Write-Host "Official findings: $(Get-SarifResultCount -ReportPath $officialReportPath)"
    Write-Host "Official SARIF: $officialReportPath"

    if (-not $SkipCommunityPack) {
        Write-Host "Downloading the C# community query pack..."
        Invoke-CodeQlCommand -Arguments @("pack", "download", "githubsecuritylab/codeql-csharp-queries")

        Write-Host "Running the C# community query pack..."
        Invoke-CodeQlCommand -Arguments @(
            "database", "analyze", $databasePath,
            "githubsecuritylab/codeql-csharp-queries",
            "--format=sarif-latest",
            "--sarif-category=community-csharp",
            "--output=$communityReportPath"
        )

        Write-Host "Community findings: $(Get-SarifResultCount -ReportPath $communityReportPath)"
        Write-Host "Community SARIF: $communityReportPath"
    }
}
finally {
    Pop-Location
}
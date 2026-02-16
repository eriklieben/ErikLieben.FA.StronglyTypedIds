<#
.SYNOPSIS
    Configures GitHub repository settings, environments, and secrets for CI/CD workflows.

.DESCRIPTION
    Sets up:
    - 'elevation-approved' environment with required reviewers (for fork PR security scans)
    - Repository secrets (from file, copied from a source repo, or prompted interactively)
    - NuGet Trusted Publishing guidance

    The secrets file is a simple JSON object:
    {
      "SONAR_TOKEN": "your-token-here",
      "SNYK_TOKEN": "your-token-here",
      "REPORTGENERATOR_LICENSE": "your-license-key",
      "NUGET_USER": "your-nuget-username"
    }

.PARAMETER Owner
    GitHub repository owner. Defaults to 'eriklieben'.

.PARAMETER Repo
    GitHub repository name. Defaults to 'ErikLieben.FA.StronglyTypedIds'.

.PARAMETER SecretsFile
    Path to a JSON file containing secret values. Searched in order:
    1. Explicit -SecretsFile path
    2. .secrets.json in repo root
    3. .secrets.json in parent folder (gitsetup workspace)
    4. ~/.config/fa-github-secrets.json (shared across repos)
    If not found, offers to copy from a source repo or prompts interactively.

.PARAMETER CopySecretsFrom
    Copy secrets from another repo (e.g. 'eriklieben/ErikLieben.FA.ES').
    Uses gh CLI to read secret names from the source repo and prompts to re-enter values.
    Note: GitHub does not allow reading secret values, only names.

.PARAMETER ReviewerUsername
    GitHub username to add as required reviewer for the elevation-approved environment.
    Defaults to the currently authenticated gh user.

.EXAMPLE
    ./scripts/configure-github-repo.ps1
    ./scripts/configure-github-repo.ps1 -SecretsFile ~/.config/fa-github-secrets.json
    ./scripts/configure-github-repo.ps1 -CopySecretsFrom eriklieben/ErikLieben.FA.ES
#>

param(
    [string]$Owner = "eriklieben",
    [string]$Repo = "ErikLieben.FA.StronglyTypedIds",
    [string]$SecretsFile,
    [string]$CopySecretsFrom,
    [string]$ReviewerUsername
)

$ErrorActionPreference = "Stop"
$repoSlug = "$Owner/$Repo"
$repoRoot = Split-Path -Parent $PSScriptRoot

Write-Host "`n=== GitHub Repository Configuration ===" -ForegroundColor Cyan
Write-Host "Repository: $repoSlug`n"

# Verify gh CLI is available and authenticated
try {
    $authStatus = gh auth status 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "gh CLI is not authenticated. Run 'gh auth login' first."
        exit 1
    }
}
catch {
    Write-Error "gh CLI not found. Install it from https://cli.github.com/"
    exit 1
}

# ------------------------------------------------------------------
# Resolve secrets file
# ------------------------------------------------------------------
$secretValues = @{}
$searchPaths = @(
    $SecretsFile,
    (Join-Path $repoRoot ".." ".secrets.json"),       # parent folder (e.g. gitsetup workspace)
    (Join-Path $HOME ".config" "fa-github-secrets.json")
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

$resolvedSecretsFile = $null
foreach ($path in $searchPaths) {
    if (Test-Path $path) {
        $resolvedSecretsFile = $path
        break
    }
}

if ($resolvedSecretsFile) {
    Write-Host "Reading secrets from: $resolvedSecretsFile" -ForegroundColor Gray
    $secretValues = Get-Content $resolvedSecretsFile -Raw | ConvertFrom-Json -AsHashtable
}
elseif ($CopySecretsFrom) {
    Write-Host "Will prompt for secret values (copying structure from $CopySecretsFrom)" -ForegroundColor Gray
}
else {
    Write-Host "No secrets file found. Will prompt interactively." -ForegroundColor Gray
    Write-Host "  Searched:" -ForegroundColor DarkGray
    foreach ($path in $searchPaths) {
        Write-Host "    - $path" -ForegroundColor DarkGray
    }
    Write-Host ""
    Write-Host "  Tip: create a .secrets.json file to avoid prompts (see script help)" -ForegroundColor DarkGray
}

# Get current authenticated user if reviewer not specified
if (-not $ReviewerUsername) {
    $ReviewerUsername = (gh api user --jq '.login' 2>$null)
    if (-not $ReviewerUsername) {
        Write-Error "Could not determine authenticated user. Specify -ReviewerUsername explicitly."
        exit 1
    }
    Write-Host "Using authenticated user as reviewer: $ReviewerUsername" -ForegroundColor Gray
}

# ------------------------------------------------------------------
# 1. Create 'elevation-approved' environment with required reviewers
# ------------------------------------------------------------------
Write-Host "`n--- Creating 'elevation-approved' environment ---" -ForegroundColor Yellow

# Get reviewer's user ID
$reviewerId = gh api "users/$ReviewerUsername" --jq '.id' 2>$null
if (-not $reviewerId) {
    Write-Error "Could not find GitHub user '$ReviewerUsername'."
    exit 1
}

# Create/update the environment with a required reviewer protection rule
$envPayload = @{
    deployment_branch_policy = $null
    reviewers = @(
        @{
            type = "User"
            id   = [int]$reviewerId
        }
    )
} | ConvertTo-Json -Depth 5

$envPayload | gh api "repos/$repoSlug/environments/elevation-approved" -X PUT --input - 2>$null | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] Environment 'elevation-approved' created with reviewer: $ReviewerUsername" -ForegroundColor Green
}
else {
    Write-Warning "  Failed to create environment. You may need admin permissions."
}

# ------------------------------------------------------------------
# 2. Set repository secrets
# ------------------------------------------------------------------
Write-Host "`n--- Configuring repository secrets ---" -ForegroundColor Yellow

$secrets = @(
    @{
        Name        = "SONAR_TOKEN"
        Description = "SonarCloud token (https://sonarcloud.io/account/security)"
    },
    @{
        Name        = "SNYK_TOKEN"
        Description = "Snyk API token (https://app.snyk.io/account)"
    },
    @{
        Name        = "REPORTGENERATOR_LICENSE"
        Description = "ReportGenerator Pro license key (empty = community edition)"
    },
    @{
        Name        = "NUGET_USER"
        Description = "NuGet.org username for Trusted Publishing OIDC"
    }
)

foreach ($secret in $secrets) {
    $value = $null

    # Try to read from secrets file first
    if ($secretValues.ContainsKey($secret.Name) -and
        -not [string]::IsNullOrWhiteSpace($secretValues[$secret.Name])) {
        $value = $secretValues[$secret.Name]
        Write-Host "  $($secret.Name)" -ForegroundColor White -NoNewline
        Write-Host " (from file)" -ForegroundColor Gray
    }

    # Fall back to interactive prompt
    if ([string]::IsNullOrWhiteSpace($value)) {
        Write-Host "  $($secret.Name)" -ForegroundColor White -NoNewline
        Write-Host " - $($secret.Description)" -ForegroundColor Gray
        $value = Read-Host "    Value (Enter to skip)"
    }

    if ([string]::IsNullOrWhiteSpace($value)) {
        Write-Host "    [SKIPPED]" -ForegroundColor DarkGray
        continue
    }

    $value | gh secret set $secret.Name --repo $repoSlug 2>$null

    if ($LASTEXITCODE -eq 0) {
        Write-Host "    [OK] $($secret.Name) set" -ForegroundColor Green
    }
    else {
        Write-Warning "    Failed to set $($secret.Name)"
    }
}

# ------------------------------------------------------------------
# 3. Configure NuGet Trusted Publishing
# ------------------------------------------------------------------
Write-Host "`n--- NuGet Trusted Publishing ---" -ForegroundColor Yellow

# Determine the packable project names by looking for IsPackable=true in csproj files
$packableProjects = @()
$csprojFiles = Get-ChildItem -Path $repoRoot -Filter "*.csproj" -Recurse -ErrorAction SilentlyContinue
foreach ($csproj in $csprojFiles) {
    $content = Get-Content $csproj.FullName -Raw -ErrorAction SilentlyContinue
    if ($content -match '<IsPackable>true</IsPackable>') {
        # Extract PackageId or fall back to project name
        if ($content -match '<PackageId>([^<]+)</PackageId>') {
            $packableProjects += $Matches[1]
        }
        else {
            $packableProjects += $csproj.BaseName
        }
    }
}

if ($packableProjects.Count -eq 0) {
    Write-Host "  No packable projects found (no <IsPackable>true</IsPackable>)" -ForegroundColor DarkGray
}
else {
    Write-Host "  Packable NuGet packages found:" -ForegroundColor White
    foreach ($pkg in $packableProjects) {
        Write-Host "    - $pkg" -ForegroundColor Gray
    }

    Write-Host ""
    Write-Host "  Trusted Publishing requires manual setup on nuget.org for each package." -ForegroundColor White
    Write-Host "  For each package above, add a Trusted Publisher with:" -ForegroundColor Gray
    Write-Host "    Repository owner:  $Owner" -ForegroundColor Gray
    Write-Host "    Repository name:   $Repo" -ForegroundColor Gray
    Write-Host "    Workflow filename: release.yml" -ForegroundColor Gray
    Write-Host "    Environment:       (leave empty)" -ForegroundColor Gray
    Write-Host ""

    $openNuGet = Read-Host "  Open nuget.org package management page? (y/N)"
    if ($openNuGet -eq 'y') {
        foreach ($pkg in $packableProjects) {
            $url = "https://www.nuget.org/packages/$pkg/manage/trusted-publishers"
            Write-Host "    Opening: $url" -ForegroundColor Gray
            Start-Process $url
        }
    }
}

# ------------------------------------------------------------------
# 4. Verify configuration
# ------------------------------------------------------------------
Write-Host "`n--- Verifying configuration ---" -ForegroundColor Yellow

# Check environment exists
$envCheck = gh api "repos/$repoSlug/environments/elevation-approved" --jq '.name' 2>$null
if ($envCheck -eq "elevation-approved") {
    Write-Host "  [OK] Environment 'elevation-approved' exists" -ForegroundColor Green
}
else {
    Write-Host "  [MISSING] Environment 'elevation-approved'" -ForegroundColor Red
}

# List configured secrets
Write-Host "`n  Configured secrets:" -ForegroundColor White
gh secret list --repo $repoSlug 2>$null | ForEach-Object {
    Write-Host "    $_" -ForegroundColor Gray
}

# Check if any secrets are missing
$existingSecrets = gh secret list --repo $repoSlug --json name --jq '.[].name' 2>$null
$missingSecrets = $secrets | Where-Object { $_.Name -notin ($existingSecrets -split "`n") }
if ($missingSecrets.Count -gt 0) {
    Write-Host "`n  Missing secrets:" -ForegroundColor Red
    foreach ($s in $missingSecrets) {
        Write-Host "    - $($s.Name)" -ForegroundColor Red
    }
}

Write-Host "`n=== Configuration complete ===" -ForegroundColor Cyan
Write-Host "Next steps:" -ForegroundColor White
Write-Host "  1. Create SonarCloud project: https://sonarcloud.io/projects/create" -ForegroundColor Gray
Write-Host "  2. Import repo in Snyk: https://app.snyk.io" -ForegroundColor Gray
if ($packableProjects.Count -gt 0) {
    Write-Host "  3. Set up NuGet Trusted Publishing (see above)" -ForegroundColor Gray
}
Write-Host ""

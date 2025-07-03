# Semantic Version Release Helper
# Usage: .\release.ps1 <version>
# Example: .\release.ps1 v1.2.3

param(
    [Parameter(Mandatory=$true)]
    [string]$Version
)

# Validate semantic version format
if ($Version -notmatch '^v\d+\.\d+\.\d+$') {
    Write-Error "Version must be in format v<major>.<minor>.<patch> (e.g., v1.2.3)"
    exit 1
}

Write-Host "Creating release $Version..." -ForegroundColor Green

# Check if we're on main branch
$currentBranch = git branch --show-current
if ($currentBranch -ne "main") {
    Write-Warning "You're not on the main branch. Current branch: $currentBranch"
    $response = Read-Host "Continue anyway? (y/N)"
    if ($response -ne "y" -and $response -ne "Y") {
        exit 1
    }
}

# Check for uncommitted changes
$status = git status --porcelain
if ($status) {
    Write-Error "You have uncommitted changes. Please commit them first."
    git status --short
    exit 1
}

# Create and push the tag
Write-Host "Creating tag $Version..." -ForegroundColor Yellow
git tag -a $Version -m "Release $Version"

Write-Host "Pushing tag to origin..." -ForegroundColor Yellow
git push origin $Version

Write-Host "✅ Release $Version created successfully!" -ForegroundColor Green
Write-Host "🚀 GitHub Actions will now build and push the Docker image with tags:" -ForegroundColor Cyan

$majorMinor = ($Version -split '\.')[0..1] -join '.'
$major = ($Version -split '\.')[0]

Write-Host "   - $Version" -ForegroundColor White
Write-Host "   - $majorMinor" -ForegroundColor White  
Write-Host "   - $major" -ForegroundColor White
Write-Host ""
Write-Host "Check the GitHub Actions workflow: https://github.com/clrslate/clrswarm/actions" -ForegroundColor Blue

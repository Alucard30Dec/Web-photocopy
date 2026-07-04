# verify_views.ps1
# E2E verification script for WebPhotocopyHub.Web.Customer views

$ErrorActionPreference = "Stop"

# 1. Run dotnet build and check exit code
Write-Host "Running dotnet build on WebPhotocopyHub.Web.Customer..." -ForegroundColor Cyan
$buildProcess = Start-Process dotnet -ArgumentList "build WebPhotocopyHub.Web.Customer/WebPhotocopyHub.Web.Customer.csproj" -NoNewWindow -PassThru -Wait
if ($buildProcess.ExitCode -ne 0) {
    Write-Host "Build failed with exit code $($buildProcess.ExitCode)." -ForegroundColor Red
    exit $buildProcess.ExitCode
}
Write-Host "Build succeeded." -ForegroundColor Green

# 2. Define Bootstrap patterns to scan
$bootstrapPatterns = @(
    '^btn(-[a-z0-9-]+)?$',
    '^row$',
    '^col(-[a-z0-9-]+)?$',
    '^form-(control|label|select|check|group|row|text|floating|range)(-[a-z0-9-]+)?$',
    '^alert(-[a-z0-9-]+)?$',
    '^badge(-[a-z0-9-]+)?$',
    '^card(-[a-z0-9-]+)?$',
    '^input-group(-[a-z0-9-]+)?$',
    '^d-(sm|md|lg|xl|xxl)?-?(flex|none|block|inline|grid|inline-block|inline-flex|table)$',
    '^justify-content-([a-z]+)$',
    '^align-items-([a-z]+)$',
    '^align-self-([a-z]+)$',
    '^text-(danger|muted|success|warning|info)$',
    '^bg-(danger|warning|success|info|light|dark)$',
    '^(w|h)-(100|75|50|25)$',
    '^pagination$',
    '^page-item$',
    '^page-link$',
    '^fw-(semibold|bold|normal|light|bolder|lighter)$',
    '^fs-(1|2|3|4|5|6)$'
)

function Is-Bootstrap-Class ($className) {
    # Exclude Tailwind grid column classes (col-span-*, col-start-*, col-end-*)
    if ($className -match '^col-(span-|start-|end-)') {
        return $false
    }
    
    foreach ($pattern in $bootstrapPatterns) {
        if ($className -match $pattern) {
            return $true
        }
    }
    return $false
}

Write-Host "Scanning views under WebPhotocopyHub.Web.Customer/Views for Bootstrap classes..." -ForegroundColor Cyan

# Use absolute or relative pathing based on script location
$viewsPath = Join-Path $PSScriptRoot "WebPhotocopyHub.Web.Customer/Views"
$views = Get-ChildItem -Path $viewsPath -Filter *.cshtml -Recurse

$violationsCount = 0
$violationDetails = @()

foreach ($view in $views) {
    # Ignore the old layout file: Views/Shared/_BranchCustomerLayout.cshtml during scans
    if ($view.FullName -like "*_BranchCustomerLayout.cshtml") {
        continue
    }

    $lines = Get-Content $view.FullName
    for ($lineNumber = 1; $lineNumber -le $lines.Length; $lineNumber++) {
        $line = $lines[$lineNumber - 1]
        
        # Check if the line contains a class attribute declaration
        if ($line -match 'class\s*=') {
            $tokens = [regex]::Matches($line, '[a-zA-Z0-9_-]+')
            $violatingClassesOnLine = @()
            
            foreach ($t in $tokens) {
                $val = $t.Value
                if ($val -and (Is-Bootstrap-Class $val)) {
                    if ($violatingClassesOnLine -notcontains $val) {
                        $violatingClassesOnLine += $val
                    }
                }
            }
            
            if ($violatingClassesOnLine.Count -gt 0) {
                $violationsCount++
                $relPath = Resolve-Path -Path $view.FullName -Relative
                $details = [PSCustomObject]@{
                    FilePath = $relPath
                    LineNumber = $lineNumber
                    LineContent = $line.Trim()
                    Violations = $violatingClassesOnLine -join ", "
                }
                $violationDetails += $details
                
                Write-Host "Violation found in $relPath at line ${lineNumber}:" -ForegroundColor Yellow
                Write-Host "  Line Content: $($line.Trim())" -ForegroundColor Gray
                Write-Host "  Bootstrap Classes: $($violatingClassesOnLine -join ', ')" -ForegroundColor Red
                Write-Host ""
            }
        }
    }
}

Write-Host "Scan completed." -ForegroundColor Cyan
$uniqueFiles = $violationDetails | Select-Object -Property FilePath -Unique
$uniqueFilesCount = if ($uniqueFiles) { @($uniqueFiles).Count } else { 0 }
Write-Host "Total files with violations: $uniqueFilesCount" -ForegroundColor Yellow
Write-Host "Total line violations: $violationsCount" -ForegroundColor Yellow

if ($violationsCount -gt 0) {
    Write-Host "E2E Check FAILED: Bootstrap classes found in customer views." -ForegroundColor Red
    exit 1
} else {
    Write-Host "E2E Check PASSED: No Bootstrap classes found in customer views." -ForegroundColor Green
    exit 0
}

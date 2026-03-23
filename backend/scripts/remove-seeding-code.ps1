# ============================================
# Remove C# Seeding Code
# ============================================
# Removes DatabaseSeeder, DocumentPlaceholderSeeder, and Program.cs seeding
# ============================================

param(
    [switch]$DryRun = $false,
    [switch]$Backup = $true
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Remove C# Seeding Code" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Files to delete
$filesToDelete = @(
    "$projectRoot\backend\src\CephasOps.Infrastructure\Persistence\DatabaseSeeder.cs",
    "$projectRoot\backend\src\CephasOps.Infrastructure\Persistence\Seeders\DocumentPlaceholderSeeder.cs"
)

# File to modify
$programCsPath = "$projectRoot\backend\src\CephasOps.Api\Program.cs"

# Lines to remove from Program.cs (638-666)
$startLine = 638
$endLine = 666

Write-Host "Files to delete:" -ForegroundColor Yellow
foreach ($file in $filesToDelete) {
    if (Test-Path $file) {
        Write-Host "  ✓ $file" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $file (NOT FOUND)" -ForegroundColor Red
    }
}
Write-Host ""

Write-Host "File to modify:" -ForegroundColor Yellow
if (Test-Path $programCsPath) {
    Write-Host "  ✓ $programCsPath" -ForegroundColor Green
    Write-Host "    Remove lines $startLine-$endLine (seeding block)" -ForegroundColor Gray
} else {
    Write-Host "  ✗ $programCsPath (NOT FOUND)" -ForegroundColor Red
}
Write-Host ""

if ($DryRun) {
    Write-Host "DRY RUN MODE - No changes will be made" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To execute removal, run without -DryRun flag" -ForegroundColor Yellow
    return
}

# Backup Program.cs if requested
if ($Backup -and (Test-Path $programCsPath)) {
    $backupPath = "$programCsPath.backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    Copy-Item $programCsPath $backupPath
    Write-Host "Backed up Program.cs to: $backupPath" -ForegroundColor Green
    Write-Host ""
}

# Delete files
Write-Host "Deleting seed class files..." -ForegroundColor Cyan
foreach ($file in $filesToDelete) {
    if (Test-Path $file) {
        try {
            Remove-Item $file -Force
            Write-Host "  ✓ Deleted: $file" -ForegroundColor Green
        } catch {
            Write-Host "  ✗ Error deleting $file : $_" -ForegroundColor Red
        }
    } else {
        Write-Host "  ⊘ Skipped (not found): $file" -ForegroundColor Gray
    }
}
Write-Host ""

# Modify Program.cs
if (Test-Path $programCsPath) {
    Write-Host "Modifying Program.cs..." -ForegroundColor Cyan
    
    $content = Get-Content $programCsPath -Raw
    $lines = Get-Content $programCsPath
    
    # Find the seeding block
    $seedingBlockStart = $null
    $seedingBlockEnd = $null
    
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $lineNum = $i + 1
        if ($lines[$i] -match "// Seed database with default data" -and $seedingBlockStart -eq $null) {
            $seedingBlockStart = $i
        }
        if ($seedingBlockStart -ne $null -and $lines[$i] -match "^\s*\}\s*$" -and $i -gt $seedingBlockStart + 5) {
            # Check if this is the closing brace of the using block
            $nextLine = if ($i + 1 -lt $lines.Count) { $lines[$i + 1] } else { "" }
            if ($nextLine -match "^\s*app\.Run\(\)" -or $nextLine -match "^\s*$") {
                $seedingBlockEnd = $i
                break
            }
        }
    }
    
    if ($seedingBlockStart -ne $null -and $seedingBlockEnd -ne $null) {
        # Remove the seeding block
        $newLines = @()
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($i -lt $seedingBlockStart -or $i -gt $seedingBlockEnd) {
                $newLines += $lines[$i]
            }
        }
        
        # Remove unused using statement if present
        $finalLines = @()
        foreach ($line in $newLines) {
            if ($line -notmatch "using CephasOps\.Infrastructure\.Persistence\.Seeders;") {
                $finalLines += $line
            }
        }
        
        $finalLines | Set-Content $programCsPath
        Write-Host "  ✓ Removed seeding block from Program.cs" -ForegroundColor Green
        Write-Host "  ✓ Removed unused using statements" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ Could not find seeding block in Program.cs" -ForegroundColor Yellow
        Write-Host "    Please manually remove lines $startLine-$endLine" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ✗ Program.cs not found" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Removal Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Review Program.cs to ensure seeding block is removed" -ForegroundColor Gray
Write-Host "  2. Build and test the application" -ForegroundColor Gray
Write-Host "  3. Run PostgreSQL seed scripts" -ForegroundColor Gray


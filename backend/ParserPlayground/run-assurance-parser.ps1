# Script to run the Assurance File Parser
# This temporarily backs up Program.cs and uses ParseAssuranceProgram.cs as the entry point

$originalProgram = "Program.cs"
$backupProgram = "Program.cs.backup"
$assuranceProgram = "ParseAssuranceProgram.cs"

Write-Host "🔧 Setting up Assurance Parser..." -ForegroundColor Cyan

# Backup original Program.cs if it exists and backup doesn't exist
if ((Test-Path $originalProgram) -and (-Not (Test-Path $backupProgram))) {
    Copy-Item $originalProgram $backupProgram
    Write-Host "✅ Backed up original Program.cs" -ForegroundColor Green
}

# Copy ParseAssuranceProgram.cs to Program.cs
if (Test-Path $assuranceProgram) {
    Copy-Item $assuranceProgram $originalProgram -Force
    Write-Host "✅ Using ParseAssuranceProgram.cs as entry point" -ForegroundColor Green
} else {
    Write-Host "❌ ParseAssuranceProgram.cs not found!" -ForegroundColor Red
    exit 1
}

Write-Host "`n🚀 Building and running Assurance Parser...`n" -ForegroundColor Cyan

try {
    # Build and run
    dotnet run
    
    Write-Host "`n✅ Parser execution complete!" -ForegroundColor Green
} catch {
    Write-Host "`n❌ Error running parser: $_" -ForegroundColor Red
} finally {
    # Restore original Program.cs if backup exists
    if (Test-Path $backupProgram) {
        Copy-Item $backupProgram $originalProgram -Force
        Remove-Item $backupProgram
        Write-Host "`n✅ Restored original Program.cs" -ForegroundColor Green
    }
}


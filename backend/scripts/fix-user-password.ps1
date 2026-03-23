# Script to fix admin user password in database
# This script updates the admin user's password hash to the correct value
# Do not commit connection strings. Pass -ConnectionString or set $env:DefaultConnection.

param(
    [string]$ConnectionString = $env:DefaultConnection
)
if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    Write-Host "ERROR: Connection string required. Set env DefaultConnection or pass -ConnectionString." -ForegroundColor Red
    exit 1
}
Write-Host "Fixing admin user password hash..." -ForegroundColor Cyan

# Calculate the correct password hash
# Using the same logic as DatabaseSeeder.HashPassword
Add-Type -TypeDefinition @"
using System;
using System.Security.Cryptography;
using System.Text;

public class PasswordHasher {
    public static string HashPassword(string password) {
        using (var sha256 = SHA256.Create()) {
            var salt = "CephasOps_Salt_2024";
            var saltedPassword = password + salt;
            var bytes = Encoding.UTF8.GetBytes(saltedPassword);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
"@

$password = "J@saw007"
$hash = [PasswordHasher]::HashPassword($password)

Write-Host "Calculated password hash: $hash" -ForegroundColor Gray

# Extract connection details
$host = if ($ConnectionString -match "Host=([^;]+)") { $matches[1] } else { "localhost" }
$port = if ($ConnectionString -match "Port=([^;]+)") { $matches[1] } else { "5432" }
$database = if ($ConnectionString -match "Database=([^;]+)") { $matches[1] } else { "cephasops" }
$username = if ($ConnectionString -match "Username=([^;]+)") { $matches[1] } else { "postgres" }
$password = if ($ConnectionString -match "Password=([^;]+)") { $matches[1] } else { "postgres" }

$env:PGPASSWORD = $password

$sql = @"
-- Update user password hash and ensure user is active
UPDATE "Users"
SET 
    "PasswordHash" = '$hash',
    "IsActive" = true,
    "UpdatedAt" = CURRENT_TIMESTAMP
WHERE "Email" = 'simon@cephas.com.my';

-- Verify the update
SELECT 
    "Id",
    "Email",
    "Name",
    "IsActive",
    CASE 
        WHEN "PasswordHash" = '$hash' THEN 'CORRECT'
        ELSE 'INCORRECT'
    END as "PasswordStatus"
FROM "Users"
WHERE "Email" = 'simon@cephas.com.my';
"@

Write-Host "`nUpdating user password hash..." -ForegroundColor Cyan
$result = psql -h $host -p $port -U $username -d $database -c $sql 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host $result
    Write-Host "`nPassword hash updated successfully!" -ForegroundColor Green
    Write-Host "You can now login with:" -ForegroundColor Green
    Write-Host "  Email: simon@cephas.com.my" -ForegroundColor White
    Write-Host "  Password: J@saw007" -ForegroundColor White
} else {
    Write-Host "Error updating password: $result" -ForegroundColor Red
    Write-Host "`nTrying alternative method..." -ForegroundColor Yellow
    
    # Try with connection string format
    $result2 = psql "$ConnectionString" -c $sql 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host $result2
        Write-Host "`nPassword hash updated successfully!" -ForegroundColor Green
    } else {
        Write-Host "Failed to update password hash." -ForegroundColor Red
        Write-Host "Please check your connection string and database access." -ForegroundColor Yellow
    }
}


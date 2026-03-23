# Script to verify the test Service Installer account exists and check details

param(
    [string]$ConnectionString = "Host=db.jgahsbfoydwdgipcjvxe.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=J@saw007;SslMode=Require;Trust Server Certificate=true"
)

Write-Host "Verifying test Service Installer account..." -ForegroundColor Cyan

$testSiEmail = "si.test@cephas.com.my"
$testSiPassword = "TestSI123!"

# Calculate password hash
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

$expectedHash = [PasswordHasher]::HashPassword($testSiPassword)
Write-Host "Expected password hash: $expectedHash" -ForegroundColor Gray

# Extract connection details
$dbHost = if ($ConnectionString -match "Host=([^;]+)") { $matches[1] } else { "localhost" }
$dbPort = if ($ConnectionString -match "Port=([^;]+)") { $matches[1] } else { "5432" }
$dbName = if ($ConnectionString -match "Database=([^;]+)") { $matches[1] } else { "postgres" }
$dbUser = if ($ConnectionString -match "Username=([^;]+)") { $matches[1] } else { "postgres" }
$dbPassword = if ($ConnectionString -match "Password=([^;]+)") { $matches[1] } else { "postgres" }

$env:PGPASSWORD = $dbPassword

$sql = @"
SELECT 
    u."Id" as "UserId",
    u."Email",
    u."Name",
    u."Phone",
    u."IsActive" as "UserActive",
    u."PasswordHash",
    CASE 
        WHEN u."PasswordHash" = '$expectedHash' THEN 'CORRECT'
        ELSE 'INCORRECT'
    END as "PasswordStatus",
    si."Id" as "ServiceInstallerId",
    si."Name" as "SIName",
    si."EmployeeId",
    si."IsActive" as "SIActive",
    si."SiLevel",
    si."IsSubcontractor",
    r."Name" as "RoleName"
FROM "Users" u
LEFT JOIN "ServiceInstallers" si ON si."UserId" = u."Id"
LEFT JOIN "UserRoles" ur ON ur."UserId" = u."Id"
LEFT JOIN "Roles" r ON r."Id" = ur."RoleId"
WHERE u."Email" = '$testSiEmail';
"@

Write-Host "`nChecking account details..." -ForegroundColor Yellow

try {
    $result = & psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -c $sql 2>&1
    
    Write-Host $result
    
    if ($result -match "0 rows") {
        Write-Host "`n❌ Account not found!" -ForegroundColor Red
        Write-Host "The account may not have been created. Let's create it now..." -ForegroundColor Yellow
        Write-Host "`nRunning account creation script..." -ForegroundColor Cyan
        & "$PSScriptRoot\create-test-si-account.ps1" -ConnectionString $ConnectionString
    } else {
        Write-Host "`n✅ Account found in database" -ForegroundColor Green
    }
} catch {
    Write-Host "`n❌ Error: $_" -ForegroundColor Red
}


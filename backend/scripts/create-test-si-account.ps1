# Script to create a test Service Installer account for testing the SI App UI
# This script creates a user account and links it to a service installer profile

param(
    [string]$ConnectionString = "Host=db.jgahsbfoydwdgipcjvxe.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=J@saw007;SslMode=Require;Trust Server Certificate=true"
)

Write-Host "Creating test Service Installer account..." -ForegroundColor Cyan

# Test SI Account Details
$testSiEmail = "si.test@cephas.com.my"
$testSiPassword = "TestSI123!"
$testSiName = "Test Service Installer"
$testSiPhone = "+60123456789"
$testSiEmployeeId = "SI-TEST-001"
$testSiLevel = "Senior"

# Calculate password hash (same as DatabaseSeeder.HashPassword)
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

$passwordHash = [PasswordHasher]::HashPassword($testSiPassword)
Write-Host "Password hash calculated: $passwordHash" -ForegroundColor Gray

# Extract connection details
$dbHost = if ($ConnectionString -match "Host=([^;]+)") { $matches[1] } else { "localhost" }
$dbPort = if ($ConnectionString -match "Port=([^;]+)") { $matches[1] } else { "5432" }
$dbName = if ($ConnectionString -match "Database=([^;]+)") { $matches[1] } else { "postgres" }
$dbUser = if ($ConnectionString -match "Username=([^;]+)") { $matches[1] } else { "postgres" }
$dbPassword = if ($ConnectionString -match "Password=([^;]+)") { $matches[1] } else { "postgres" }

$env:PGPASSWORD = $dbPassword

# Generate GUIDs
$userId = [guid]::NewGuid().ToString()
$siId = [guid]::NewGuid().ToString()
$installerRoleId = "00000000-0000-0000-0000-000000000000" # Will be set from database

Write-Host "User ID: $userId" -ForegroundColor Gray
Write-Host "Service Installer ID: $siId" -ForegroundColor Gray

$sql = @"
-- Check if user already exists
DO \$\$
DECLARE
    v_user_id UUID;
    v_si_id UUID;
    v_installer_role_id UUID;
    v_department_id UUID;
BEGIN
    -- Get or create Installer role
    SELECT "Id" INTO v_installer_role_id
    FROM "Roles"
    WHERE "Name" = 'Installer' AND "Scope" = 'Global'
    LIMIT 1;
    
    IF v_installer_role_id IS NULL THEN
        v_installer_role_id := gen_random_uuid();
        INSERT INTO "Roles" ("Id", "Name", "Scope", "CreatedAt")
        VALUES (v_installer_role_id, 'Installer', 'Global', CURRENT_TIMESTAMP);
        RAISE NOTICE 'Created Installer role: %', v_installer_role_id;
    END IF;
    
    -- Get first active department (or create a default one)
    SELECT "Id" INTO v_department_id
    FROM "Departments"
    WHERE "IsActive" = true
    LIMIT 1;
    
    IF v_department_id IS NULL THEN
        v_department_id := gen_random_uuid();
        INSERT INTO "Departments" ("Id", "CompanyId", "Name", "Code", "IsActive", "CreatedAt")
        VALUES (v_department_id, NULL, 'GPON', 'GPON', true, CURRENT_TIMESTAMP);
        RAISE NOTICE 'Created default department: %', v_department_id;
    END IF;
    
    -- Check if user exists
    SELECT "Id" INTO v_user_id
    FROM "Users"
    WHERE "Email" = '$testSiEmail';
    
    IF v_user_id IS NULL THEN
        -- Create new user
        v_user_id := gen_random_uuid();
        INSERT INTO "Users" ("Id", "Name", "Email", "Phone", "PasswordHash", "IsActive", "CreatedAt")
        VALUES (
            v_user_id,
            '$testSiName',
            '$testSiEmail',
            '$testSiPhone',
            '$passwordHash',
            true,
            CURRENT_TIMESTAMP
        );
        RAISE NOTICE 'Created user: %', v_user_id;
        
        -- Assign Installer role
        INSERT INTO "UserRoles" ("UserId", "CompanyId", "RoleId", "CreatedAt")
        VALUES (v_user_id, NULL, v_installer_role_id, CURRENT_TIMESTAMP)
        ON CONFLICT DO NOTHING;
        RAISE NOTICE 'Assigned Installer role to user';
    ELSE
        -- Update existing user
        UPDATE "Users"
        SET 
            "Name" = '$testSiName',
            "Phone" = '$testSiPhone',
            "PasswordHash" = '$passwordHash',
            "IsActive" = true,
            "UpdatedAt" = CURRENT_TIMESTAMP
        WHERE "Id" = v_user_id;
        RAISE NOTICE 'Updated existing user: %', v_user_id;
        
        -- Ensure Installer role is assigned
        INSERT INTO "UserRoles" ("UserId", "CompanyId", "RoleId", "CreatedAt")
        VALUES (v_user_id, NULL, v_installer_role_id, CURRENT_TIMESTAMP)
        ON CONFLICT DO NOTHING;
    END IF;
    
    -- Check if service installer profile exists
    SELECT "Id" INTO v_si_id
    FROM "ServiceInstallers"
    WHERE "UserId" = v_user_id;
    
    IF v_si_id IS NULL THEN
        -- Create service installer profile
        v_si_id := gen_random_uuid();
        INSERT INTO "ServiceInstallers" (
            "Id", "CompanyId", "DepartmentId", "UserId", "Name", 
            "EmployeeId", "Phone", "Email", "SiLevel", 
            "IsSubcontractor", "IsActive", "CreatedAt"
        )
        VALUES (
            v_si_id,
            NULL,
            v_department_id,
            v_user_id,
            '$testSiName',
            '$testSiEmployeeId',
            '$testSiPhone',
            '$testSiEmail',
            '$testSiLevel',
            false,
            true,
            CURRENT_TIMESTAMP
        );
        RAISE NOTICE 'Created service installer profile: %', v_si_id;
    ELSE
        -- Update existing service installer profile
        UPDATE "ServiceInstallers"
        SET 
            "Name" = '$testSiName',
            "EmployeeId" = '$testSiEmployeeId',
            "Phone" = '$testSiPhone',
            "Email" = '$testSiEmail',
            "SiLevel" = '$testSiLevel',
            "IsActive" = true,
            "UpdatedAt" = CURRENT_TIMESTAMP
        WHERE "Id" = v_si_id;
        RAISE NOTICE 'Updated existing service installer profile: %', v_si_id;
    END IF;
    
    RAISE NOTICE 'Test SI account created/updated successfully!';
    RAISE NOTICE 'Email: $testSiEmail';
    RAISE NOTICE 'Password: $testSiPassword';
    RAISE NOTICE 'User ID: %', v_user_id;
    RAISE NOTICE 'Service Installer ID: %', v_si_id;
END \$\$;
"@

Write-Host "Executing SQL script..." -ForegroundColor Yellow

# Write SQL to temporary file to avoid escaping issues
$tempSqlFile = [System.IO.Path]::GetTempFileName()
$sql | Out-File -FilePath $tempSqlFile -Encoding UTF8

try {
    $result = & psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -f $tempSqlFile 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✅ Test Service Installer account created successfully!" -ForegroundColor Green
        Write-Host "`n📋 Account Details:" -ForegroundColor Cyan
        Write-Host "   Email: $testSiEmail" -ForegroundColor White
        Write-Host "   Password: $testSiPassword" -ForegroundColor White
        Write-Host "   Name: $testSiName" -ForegroundColor White
        Write-Host "   Phone: $testSiPhone" -ForegroundColor White
        Write-Host "   Employee ID: $testSiEmployeeId" -ForegroundColor White
        Write-Host "   SI Level: $testSiLevel" -ForegroundColor White
        Write-Host "`n🔐 You can now login to the SI App with these credentials" -ForegroundColor Yellow
    } else {
        Write-Host "`n❌ Error executing SQL script:" -ForegroundColor Red
        Write-Host $result -ForegroundColor Red
        Write-Host "`n💡 Tip: You can also run the SQL file directly:" -ForegroundColor Yellow
        Write-Host "   psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -f backend/scripts/create-test-si-account.sql" -ForegroundColor Gray
    }
} catch {
    Write-Host "`n❌ Error: $_" -ForegroundColor Red
    Write-Host "`nMake sure PostgreSQL client (psql) is installed and in PATH" -ForegroundColor Yellow
    Write-Host "Alternatively, you can run the SQL script directly:" -ForegroundColor Yellow
    Write-Host "   psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -f backend/scripts/create-test-si-account.sql" -ForegroundColor Gray
} finally {
    # Clean up temp file
    if (Test-Path $tempSqlFile) {
        Remove-Item $tempSqlFile -Force
    }
}

Write-Host "`nSQL Script also available at: backend/scripts/create-test-si-account.sql" -ForegroundColor Gray


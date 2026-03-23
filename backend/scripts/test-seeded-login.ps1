# Test Seeded Login
# This script tests the login endpoint with the seeded admin user

param(
    [string]$ApiBaseUrl = "http://localhost:5000"
)

Write-Host "=== Testing Seeded Login ===" -ForegroundColor Cyan
Write-Host ""

$loginEndpoint = "$ApiBaseUrl/api/auth/login"
$email = "simon@cephas.com.my"
$password = "J@saw007"

Write-Host "Testing login with:" -ForegroundColor Yellow
Write-Host "  Email: $email" -ForegroundColor White
Write-Host "  Endpoint: $loginEndpoint" -ForegroundColor White
Write-Host ""

try {
    $body = @{
        Email = $email
        Password = $password
    } | ConvertTo-Json

    $headers = @{
        "Content-Type" = "application/json"
    }

    Write-Host "Sending login request..." -ForegroundColor Yellow
    $response = Invoke-RestMethod -Uri $loginEndpoint -Method Post -Body $body -Headers $headers -ErrorAction Stop

    if ($response.success -eq $true) {
        Write-Host "✅ Login successful!" -ForegroundColor Green
        Write-Host ""
        Write-Host "User Information:" -ForegroundColor Cyan
        if ($response.data.user) {
            Write-Host "  Name: $($response.data.user.name)" -ForegroundColor White
            Write-Host "  Email: $($response.data.user.email)" -ForegroundColor White
            if ($response.data.user.roles) {
                Write-Host "  Roles: $($response.data.user.roles -join ', ')" -ForegroundColor White
            }
        }
        Write-Host ""
        Write-Host "Token Information:" -ForegroundColor Cyan
        Write-Host "  Access Token: $($response.data.accessToken.Substring(0, [Math]::Min(50, $response.data.accessToken.Length)))..." -ForegroundColor White
        if ($response.data.refreshToken) {
            Write-Host "  Refresh Token: Present" -ForegroundColor White
        }
        if ($response.data.expiresAt) {
            Write-Host "  Expires At: $($response.data.expiresAt)" -ForegroundColor White
        }
    } else {
        Write-Host "❌ Login failed: $($response.message)" -ForegroundColor Red
    }
} catch {
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "❌ Login failed with status code: $statusCode" -ForegroundColor Red
        
        try {
            $errorStream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($errorStream)
            $errorBody = $reader.ReadToEnd()
            $errorJson = $errorBody | ConvertFrom-Json
            Write-Host "Error message: $($errorJson.message)" -ForegroundColor Red
        } catch {
            Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        }
    } else {
        Write-Host "❌ Connection error: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        Write-Host "Make sure the backend API is running:" -ForegroundColor Yellow
        Write-Host "  cd backend/src/CephasOps.Api" -ForegroundColor White
        Write-Host "  dotnet run" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Cyan


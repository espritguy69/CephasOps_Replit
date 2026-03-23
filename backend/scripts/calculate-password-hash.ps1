# Calculate password hash using the same logic as DatabaseSeeder
$password = "J@saw007"
$salt = "CephasOps_Salt_2024"
$saltedPassword = $password + $salt
$bytes = [System.Text.Encoding]::UTF8.GetBytes($saltedPassword)
$sha256 = [System.Security.Cryptography.SHA256]::Create()
$hash = $sha256.ComputeHash($bytes)
$base64 = [Convert]::ToBase64String($hash)
Write-Host "Admin password hash: $base64"

$password2 = "E5pr!tg@L"
$saltedPassword2 = $password2 + $salt
$bytes2 = [System.Text.Encoding]::UTF8.GetBytes($saltedPassword2)
$hash2 = $sha256.ComputeHash($bytes2)
$base642 = [Convert]::ToBase64String($hash2)
Write-Host "Finance password hash: $base642"

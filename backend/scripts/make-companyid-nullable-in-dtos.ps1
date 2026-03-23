# Script to make CompanyId nullable in all DTOs
$dtoFiles = Get-ChildItem -Path "src/CephasOps.Application" -Recurse -Filter "*Dto.cs" | Where-Object {
    (Get-Content $_.FullName -Raw) -match "public Guid CompanyId"
}

foreach ($file in $dtoFiles) {
    Write-Host "Processing: $($file.Name)" -ForegroundColor Cyan
    $content = Get-Content $file.FullName -Raw
    
    # Replace non-nullable Guid CompanyId with nullable Guid? CompanyId
    $content = $content -replace "public Guid CompanyId \{ get; set; \}", "public Guid? CompanyId { get; set; } // Company feature removed - now nullable"
    
    Set-Content -Path $file.FullName -Value $content -NoNewline
    Write-Host "  -> Updated" -ForegroundColor Green
}

Write-Host "`nDone! Processed $($dtoFiles.Count) DTO files." -ForegroundColor Green


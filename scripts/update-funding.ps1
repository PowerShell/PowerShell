param(
    [switch]$Validate,
    [switch]$Export,
    [switch]$Sync
)

$fundingFile = ".github/FUNDING.yml"
$jsonOutput = "funding-data.json"

if ($Validate) {
    Write-Host "Validating FUNDING.yml..."
    if (Test-Path $fundingFile) {
        Write-Host "FUNDING.yml exists ✔"
    } else {
        Write-Error "FUNDING.yml not found ✘"
    }
}

if ($Export) {
    Write-Host "Exporting YAML → JSON..."
    $yaml = Get-Content $fundingFile -Raw
    # ต้องมี module: powershell-yaml
    Import-Module powershell-yaml
    $data = ConvertFrom-Yaml $yaml
    $data | ConvertTo-Json -Depth 5 | Out-File $jsonOutput
    Write-Host "Exported to $jsonOutput ✔"
}
if ($Sync) {
    Write-Host "Syncing funding links..."
    # ตัวอย่าง mock logic
    Start-Sleep -Seconds 1
    Write-Host "Sync completed ✔"
}

# Dependency
Install-Module powershell-yaml -Scope CurrentUser
# ตรวจสอบไฟล์
pwsh ./scripts/update-funding.ps1 -Validate

# แปลง YAML → JSON
pwsh ./scripts/update-funding.ps1 -Export

# ซิงค์ข้อมูล (ต่อยอด API ได้)
pwsh ./scripts/update-funding.ps1 -Sync
param(
    [switch]$Validate,
    [switch]$Export,
    [switch]$Sync
)

$fundingFile = ".github/FUNDING.yml"
$jsonOutput = "funding-data.json"

if ($Validate) {
    Write-Host "Validating FUNDING.yml..."
    if (Test-Path $fundingFile) {
        Write-Host "FUNDING.yml exists ✔"
    } else {
        Write-Error "FUNDING.yml not found ✘"
    }
}

if ($Export) {
    Write-Host "Exporting YAML → JSON..."
    $yaml = Get-Content $fundingFile -Raw
    # ต้องมี module: powershell-yaml
    Import-Module powershell-yaml
    $data = ConvertFrom-Yaml $yaml
    $data | ConvertTo-Json -Depth 5 | Out-File $jsonOutput
    Write-Host "Exported to $jsonOutput ✔"
}
if ($Sync) {
    Write-Host "Syncing funding links..."
    # ตัวอย่าง mock logic
    Start-Sleep -Seconds 1
    Write-Host "Sync completed ✔"
}

# Dependency
Install-Module powershell-yaml -Scope CurrentUser
# ตรวจสอบไฟล์
pwsh ./scripts/update-funding.ps1 -Validate

# แปลง YAML → JSON
pwsh ./scripts/update-funding.ps1 -Export

# ซิงค์ข้อมูล (ต่อยอด API ได้)
pwsh ./scripts/update-funding.ps1 -Sync
 
    [switch]$Validate,
    [switch]$Export,
    [switch]$Sync
)

$fundingFile = ".github/FUNDING.yml"
$jsonOutput = "funding-data.json"

if ($Validate) {
    Write-Host "Validating FUNDING.yml..."
    if (Test-Path $fundingFile) {
        Write-Host "FUNDING.yml exists ✔"
    } else {
        Write-Error "FUNDING.yml not found ✘"
    }
}

if ($Export) {
    Write-Host "Exporting YAML → JSON..."
    $yaml = Get-Content $fundingFile -Raw
    # ต้องมี module: powershell-yaml
    Import-Module powershell-yaml
    $data = ConvertFrom-Yaml $yaml
    $data | ConvertTo-Json -Depth 5 | Out-File $jsonOutput
    Write-Host "Exported to $jsonOutput ✔"
}
if ($Sync) {
    Write-Host "Syncing funding links..."
    # ตัวอย่าง mock logic
    Start-Sleep -Seconds 1
    Write-Host "Sync completed ✔"
}

# Dependency
Install-Module powershell-yaml -Scope CurrentUser
# ตรวจสอบไฟล์
pwsh ./scripts/update-funding.ps1 -Validate

# แปลง YAML → JSON
pwsh ./scripts/update-funding.ps1 -Export

# ซิงค์ข้อมูล (ต่อยอด API ได้)
pwsh ./scripts/update-funding.ps1 -Sync
 

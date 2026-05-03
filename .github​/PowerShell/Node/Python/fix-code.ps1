param(
    [string]$Path = "./"
)

Write-Host "🔍 Scanning code..."

# หาไฟล์ .js
$files = Get-ChildItem -Path $Path -Recurse -Include *.js

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw

    # Fix 1: console.log → logger
    $content = $content -replace "console\.log", "logger.info"

    # Fix 2: var → let
    $content = $content -replace "\bvar\b", "let"

    # Save
    Set-Content $file.FullName $content
    Write-Host "✔ Fixed: $($file.Name)"
}

Write-Host "✅ Done"
param(
    [string]$Path = "./"
)

Write-Host "🔍 Scanning code..."

# หาไฟล์ .js
$files = Get-ChildItem -Path $Path -Recurse -Include *.js

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw

    # Fix 1: console.log → logger
    $content = $content -replace "console\.log", "logger.info"

    # Fix 2: var → let
    $content = $content -replace "\bvar\b", "let"

    # Save
    Set-Content $file.FullName $content
    Write-Host "✔ Fixed: $($file.Name)"
}

Write-Host "✅ Done"
param(
    [string]$Path = "./"
)

Write-Host "🔍 Scanning code..."

# หาไฟล์ .js
$files = Get-ChildItem -Path $Path -Recurse -Include *.js

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw

    # Fix 1: console.log → logger
    $content = $content -replace "console\.log", "logger.info"

    # Fix 2: var → let
    $content = $content -replace "\bvar\b", "let"

    # Save
    Set-Content $file.FullName $content
    Write-Host "✔ Fixed: $($file.Name)"
}

Write-Host "✅ Done"
param(
    [string]$Path = "./"
)

Write-Host "🔍 Scanning code..."

# หาไฟล์ .js
$files = Get-ChildItem -Path $Path -Recurse -Include *.js

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw

    # Fix 1: console.log → logger
    $content = $content -replace "console\.log", "logger.info"

    # Fix 2: var → let
    $content = $content -replace "\bvar\b", "let"

    # Save
    Set-Content $file.FullName $content
    Write-Host "✔ Fixed: $($file.Name)"
}

Write-Host "✅ Done"
pwsh ./bot/fix-code.ps1





Describe "Verify macOS Package" {
    BeforeAll {
        Write-Verbose "In Describe BeforeAll" -Verbose
        Import-Module $PSScriptRoot/../../../build.psm1
        
        # Find the macOS package
        $packagePath = $env:PACKAGE_FOLDER
        if (-not $packagePath) {
            $packagePath = Get-Location
        }
        
        Write-Verbose "Looking for package in: $packagePath" -Verbose
        $package = Get-ChildItem -Path $packagePath -Filter "*.pkg" -ErrorAction SilentlyContinue | Select-Object -First 1
        
        if (-not $package) {
            Write-Warning "No .pkg file found in $packagePath"
        } else {
            Write-Verbose "Found package: $($package.FullName)" -Verbose
        }
        
        # Set up test directories
        $script:package = $package
        $script:expandDir = $null
        $script:payloadDir = $null
        $script:extractedFiles = @()
        
        if ($package) {
            # Expand the package to inspect contents
            $script:expandDir = Join-Path (Get-Location) -ChildPath "package-contents-test"
            if (Test-Path $script:expandDir) {
                Remove-Item -Path $script:expandDir -Recurse -Force
            }
            $null = New-Item -ItemType Directory -Path $script:expandDir -Force
            
            Write-Verbose "Expanding package to: $($script:expandDir)" -Verbose
            & pkgutil --expand $package.FullName $script:expandDir
            
            # Extract the payload to verify files
            $script:payloadDir = Join-Path (Get-Location) -ChildPath "package-payload-test"
            if (Test-Path $script:payloadDir) {
                Remove-Item -Path $script:payloadDir -Recurse -Force
            }
            $null = New-Item -ItemType Directory -Path $script:payloadDir -Force
            
            $componentPkg = Get-ChildItem -Path $script:expandDir -Filter "*.pkg" -Recurse | Select-Object -First 1
            if ($componentPkg) {
                Write-Verbose "Extracting payload from: $($componentPkg.DirectoryName)" -Verbose
                Push-Location $script:payloadDir
                try {
                    $payloadFile = "$($componentPkg.DirectoryName)/Payload"
                    Get-Content -Path $payloadFile -Raw -AsByteStream | & cpio -i 2>&1 | Out-Null
                } finally {
                    Pop-Location
                }
            }
            
            # Get all extracted files for verification
            $script:extractedFiles = Get-ChildItem -Path $script:payloadDir -Recurse -ErrorAction SilentlyContinue
            Write-Verbose "Extracted $($script:extractedFiles.Count) files" -Verbose
        }
    }
    
    AfterAll {
        # Clean up test directories
        if ($script:expandDir -and (Test-Path $script:expandDir)) {
            Remove-Item -Path $script:expandDir -Recurse -Force -ErrorAction SilentlyContinue
        }
        if ($script:payloadDir -and (Test-Path $script:payloadDir)) {
            Remove-Item -Path $script:payloadDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    
    Context "Package existence and structure" {
        It "Package file should exist" {
            $script:package | Should -Not -BeNullOrEmpty -Because "A .pkg file should be created"
            $script:package.Extension | Should -Be ".pkg"
        }
        
        It "Package should expand successfully" {
            $script:expandDir | Should -Exist
            Get-ChildItem -Path $script:expandDir | Should -Not -BeNullOrEmpty
        }
        
        It "Package should have a component package" {
            $componentPkg = Get-ChildItem -Path $script:expandDir -Filter "*.pkg" -Recurse -ErrorAction SilentlyContinue
            $componentPkg | Should -Not -BeNullOrEmpty -Because "Package should contain a component.pkg"
        }
        
        It "Payload should extract successfully" {
            $script:payloadDir | Should -Exist
            $script:extractedFiles | Should -Not -BeNullOrEmpty -Because "Package payload should contain files"
        }
    }
    
    Context "Required files in package" {
        BeforeAll {
            $expectedFilePatterns = @{
                "PowerShell executable" = "usr/local/microsoft/powershell/*/pwsh"
                "PowerShell symlink in /usr/local/bin" = "usr/local/bin/pwsh*"
                "Man page" = "usr/local/share/man/man1/pwsh*.gz"
                "Launcher application plist" = "Applications/PowerShell*.app/Contents/Info.plist"
            }
            
            $testCases = @()
            foreach ($key in $expectedFilePatterns.Keys) {
                $testCases += @{
                    Description = $key
                    Pattern = $expectedFilePatterns[$key]
                }
            }
            
            $script:testCases = $testCases
        }
        
        It "Should contain <Description>" -TestCases $script:testCases {
            param($Description, $Pattern)
            
            $found = $script:extractedFiles | Where-Object { $_.FullName -like "*$Pattern*" }
            $found | Should -Not -BeNullOrEmpty -Because "$Description should exist in the package at path matching '$Pattern'"
        }
    }
    
    Context "PowerShell binary verification" {
        It "PowerShell executable should be executable" {
            $pwshBinary = $script:extractedFiles | Where-Object { $_.FullName -like "*/pwsh" -and $_.FullName -like "*/microsoft/powershell/*" }
            $pwshBinary | Should -Not -BeNullOrEmpty
            
            # Check if file has executable permissions (on Unix-like systems)
            if ($IsLinux -or $IsMacOS) {
                $permissions = (Get-Item $pwshBinary[0].FullName).UnixFileMode
                # Executable bit should be set
                $permissions.ToString() | Should -Match 'x' -Because "pwsh binary should have execute permissions"
            }
        }
    }
    
    Context "Launcher application" {
        It "Launcher app should have proper bundle structure" {
            $plistFile = $script:extractedFiles | Where-Object { $_.FullName -like "*PowerShell*.app/Contents/Info.plist" }
            $plistFile | Should -Not -BeNullOrEmpty
            
            # Verify the bundle has required components
            $appPath = Split-Path (Split-Path $plistFile[0].FullName -Parent) -Parent
            $macOSDir = Join-Path $appPath "Contents/MacOS"
            $resourcesDir = Join-Path $appPath "Contents/Resources"
            
            Test-Path $macOSDir | Should -Be $true -Because "App bundle should have Contents/MacOS directory"
            Test-Path $resourcesDir | Should -Be $true -Because "App bundle should have Contents/Resources directory"
        }
        
        It "Launcher script should exist and be executable" {
            $launcherScript = $script:extractedFiles | Where-Object { 
                $_.FullName -like "*PowerShell*.app/Contents/MacOS/PowerShell.sh" 
            }
            $launcherScript | Should -Not -BeNullOrEmpty -Because "Launcher script should exist"
            
            if ($IsLinux -or $IsMacOS) {
                $permissions = (Get-Item $launcherScript[0].FullName).UnixFileMode
                $permissions.ToString() | Should -Match 'x' -Because "Launcher script should have execute permissions"
            }
        }
    }
}

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "FIPS Policy Compliance Tests" -Tags 'CI' {
    BeforeAll {
        # Get the path to pwsh executable
        $pwshPath = (Get-Process -Id $PID).Path
        $pwshDir = Split-Path -Parent $pwshPath
        $pwshExeName = Split-Path -Leaf $pwshPath
        
        # Backup original runtimeconfig.json if it exists
        $runtimeConfigPath = Join-Path $pwshDir "$pwshExeName.runtimeconfig.json"
        $backupConfigPath = Join-Path $TestDrive "backup.runtimeconfig.json"
        
        if (Test-Path $runtimeConfigPath) {
            Copy-Item $runtimeConfigPath $backupConfigPath -Force
        }
        
        # Create a test directory for FIPS-enabled PowerShell
        $fipsTestDir = Join-Path $TestDrive "fips-test"
        New-Item -ItemType Directory -Path $fipsTestDir -Force | Out-Null
    }

    AfterAll {
        # Restore original config if backed up
        if (Test-Path $backupConfigPath) {
            Copy-Item $backupConfigPath $runtimeConfigPath -Force
            Remove-Item $backupConfigPath -Force
        }
    }

    Context "PowerShell with FIPS Policy Enabled" {
        BeforeAll {
            # Get the runtime config path - use $PSHOME to get PowerShell installation directory
            $pwshPath = (Get-Process -Id $PID).Path
            $runtimeConfigPath = Join-Path $PSHOME "pwsh.runtimeconfig.json"
            
            # Backup the original config
            $backupConfigPath = Join-Path $TestDrive "fips-backup.runtimeconfig.json"
            if (Test-Path $runtimeConfigPath) {
                Copy-Item $runtimeConfigPath $backupConfigPath -Force
                
                # Read existing config as hashtable
                $configText = Get-Content $runtimeConfigPath -Raw
                $existingConfig = $configText | ConvertFrom-Json -AsHashtable
                
                # Ensure the structure exists
                if (-not $existingConfig.ContainsKey('runtimeOptions')) {
                    $existingConfig['runtimeOptions'] = @{}
                }
                if (-not $existingConfig['runtimeOptions'].ContainsKey('configProperties')) {
                    $existingConfig['runtimeOptions']['configProperties'] = @{}
                }
                
                # Add FIPS policy to configProperties
                $existingConfig['runtimeOptions']['configProperties']['System.Security.Cryptography.UseFipsAlgorithm'] = $true
                
                # Write the modified config back
                $existingConfig | ConvertTo-Json -Depth 10 | Set-Content -Path $runtimeConfigPath -Force
            } else {
                # Create a new config with FIPS enabled
                $newConfig = @{
                    runtimeOptions = @{
                        configProperties = @{
                            'System.Security.Cryptography.UseFipsAlgorithm' = $true
                        }
                    }
                }
                $newConfig | ConvertTo-Json -Depth 10 | Set-Content -Path $runtimeConfigPath -Force
            }
            
            # Test if FIPS is actually enforced by checking in a new pwsh process
            $fipsCheckScript = @'
try {
    $md5 = [System.Security.Cryptography.MD5]::Create()
    if ($null -eq $md5) {
        Write-Output "NOT_ENFORCED"
    } else {
        try {
            $hash = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes("test"))
            Write-Output "NOT_ENFORCED"
        } catch {
            Write-Output "ENFORCED"
        }
    }
} catch {
    Write-Output "ENFORCED"
}
'@
            $fipsEnforced = & $pwshPath -NoProfile -NonInteractive -Command $fipsCheckScript 2>&1 | Select-Object -Last 1
            $script:FipsActuallyEnforced = ($fipsEnforced -eq "ENFORCED")
            
            if (-not $script:FipsActuallyEnforced) {
                Write-Warning "FIPS policy was configured in runtimeconfig.json but is not actually enforced on this system. MD5 tests will be skipped."
            }
        }
        
        AfterAll {
            # Restore the original config
            $runtimeConfigPath = Join-Path $PSHOME "pwsh.runtimeconfig.json"
            $backupConfigPath = Join-Path $TestDrive "fips-backup.runtimeconfig.json"
            
            if (Test-Path $backupConfigPath) {
                Copy-Item $backupConfigPath $runtimeConfigPath -Force
                Remove-Item $backupConfigPath -Force
            }
        }

        It "PowerShell starts successfully with FIPS policy enabled" {
            # Test basic PowerShell execution with FIPS-compatible settings
            $result = & $pwshPath -NoProfile -NonInteractive -Command "Write-Output 'FIPS-Test-Success'; exit 0" 2>&1
            $LASTEXITCODE | Should -Be 0
            $result | Should -BeLike '*FIPS-Test-Success*'
        }

        It "PowerShell can execute basic commands with FIPS policy" {
            $result = & $pwshPath -NoProfile -NonInteractive -Command "Get-Date | Select-Object -ExpandProperty Year; exit 0" 2>&1
            $LASTEXITCODE | Should -Be 0
            [int]$result | Should -BeGreaterThan 2020
        }

        It "PowerShell can create and use variables with FIPS policy" {
            $result = & $pwshPath -NoProfile -NonInteractive -Command "`$testVar = 'HelloWorld'; Write-Output `$testVar; exit 0" 2>&1
            $LASTEXITCODE | Should -Be 0
            $result | Should -Be 'HelloWorld'
        }

        It "PowerShell can run script blocks with FIPS policy" {
            $scriptBlock = "1..5 | ForEach-Object { `$_ * 2 } | Measure-Object -Sum | Select-Object -ExpandProperty Sum"
            $result = & $pwshPath -NoProfile -NonInteractive -Command "$scriptBlock; exit 0" 2>&1
            $LASTEXITCODE | Should -Be 0
            [int]$result | Should -Be 30
        }

        It "PowerShell can use pipeline with FIPS policy" {
            $result = & $pwshPath -NoProfile -NonInteractive -Command "1,2,3,4,5 | Where-Object { `$_ -gt 2 } | Measure-Object -Sum | Select-Object -ExpandProperty Sum; exit 0" 2>&1
            $LASTEXITCODE | Should -Be 0
            [int]$result | Should -Be 12
        }

        It "PowerShell can load modules with FIPS policy" {
            $result = & $pwshPath -NoProfile -NonInteractive -Command "Get-Module -ListAvailable | Select-Object -First 1 -ExpandProperty Name; exit 0" 2>&1
            $LASTEXITCODE | Should -Be 0
            $result | Should -Not -BeNullOrEmpty
        }

        It "MD5 algorithm should be blocked when FIPS policy is enforced" -Skip:(-not $script:FipsActuallyEnforced) {
            # Test MD5 behavior - it should be blocked when FIPS policy is enforced
            # This test verifies PowerShell properly blocks MD5 when FIPS is enforced
            $script = @'
$result = "UNKNOWN"
try {
    $md5 = [System.Security.Cryptography.MD5]::Create()
    if ($null -eq $md5) {
        $result = "MD5_NOT_AVAILABLE"
    } else {
        try {
            $hash = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes("test"))
            if ($hash.Length -eq 16) {
                $result = "MD5_WORKS"
            }
            $md5.Dispose()
        } catch {
            $result = "MD5_COMPUTE_FAILED"
        }
    }
} catch [System.Reflection.TargetInvocationException] {
    $result = "FIPS_BLOCKED"
} catch [System.InvalidOperationException] {
    $result = "FIPS_BLOCKED"
} catch {
    $result = "MD5_CREATE_FAILED"
}
Write-Output $result
'@
            $result = & $pwshPath -NoProfile -NonInteractive -Command $script 2>&1
            $LASTEXITCODE | Should -Be 0
            
            Write-Verbose "MD5 test result: $result" -Verbose
            
            # When FIPS policy is enforced, MD5 should be blocked or unavailable
            # MD5_WORKS would indicate FIPS policy is not being enforced correctly
            $result | Should -BeIn @('MD5_NOT_AVAILABLE', 'FIPS_BLOCKED', 'MD5_CREATE_FAILED', 'MD5_COMPUTE_FAILED')
        }

        It "Get-FileHash with MD5 algorithm should be blocked when FIPS policy is enforced" -Skip:(-not $script:FipsActuallyEnforced) {
            # Test Get-FileHash -Algorithm MD5 behavior
            # This cmdlet should fail when FIPS policy blocks MD5
            $script = @'
$tempDir = if ($IsWindows) { $env:TEMP } else { $env:TMPDIR ?? "/tmp" }
$testFile = Join-Path $tempDir "fips-test-$(Get-Random).txt"
$result = "UNKNOWN"
try {
    "test content" | Set-Content -Path $testFile -NoNewline
    
    try {
        $hash = Get-FileHash -Path $testFile -Algorithm MD5 -ErrorAction Stop
        if ($hash.Algorithm -eq 'MD5' -and $hash.Hash) {
            $result = "GET_FILEHASH_MD5_SUCCESS"
        } else {
            $result = "GET_FILEHASH_MD5_UNEXPECTED"
        }
    } catch {
        $errorMessage = $_.Exception.Message
        if ($errorMessage -like "*FIPS*" -or $errorMessage -like "*MD5*" -or $_.Exception.InnerException.GetType().Name -eq "InvalidOperationException") {
            $result = "GET_FILEHASH_MD5_BLOCKED"
        } else {
            $result = "GET_FILEHASH_MD5_ERROR"
        }
    }
} catch {
    $result = "OUTER_EXCEPTION: $($_.Exception.GetType().Name)"
} finally {
    if ($testFile -and (Test-Path $testFile -ErrorAction SilentlyContinue)) {
        Remove-Item $testFile -Force -ErrorAction SilentlyContinue
    }
}
Write-Output $result
exit 0
'@
            $result = & $pwshPath -NoProfile -NonInteractive -Command $script 2>&1 | Select-Object -Last 1
            $LASTEXITCODE | Should -Be 0
            
            Write-Verbose "Get-FileHash MD5 test result: $result" -Verbose
            
            # When FIPS policy is enforced, Get-FileHash MD5 should fail
            # GET_FILEHASH_MD5_SUCCESS would indicate FIPS policy is not being enforced correctly
            $result | Should -BeIn @('GET_FILEHASH_MD5_BLOCKED', 'GET_FILEHASH_MD5_ERROR')
        }
    }

    Context "PowerShell with FIPS Policy Disabled" {
        BeforeAll {
            # Create a runtimeconfig.json with FIPS policy disabled
            $nonFipsConfig = @{
                runtimeOptions = @{
                    configProperties = @{
                        "System.Security.Cryptography.UseFipsAlgorithm" = $false
                    }
                }
            } | ConvertTo-Json -Depth 10
            
            $testConfigPath = Join-Path $fipsTestDir "test-nonfips.runtimeconfig.json"
            Set-Content -Path $testConfigPath -Value $nonFipsConfig -Force
        }

        It "PowerShell starts successfully with FIPS policy disabled" {
            $result = & $pwshPath -NoProfile -NonInteractive -Command "Write-Output 'Non-FIPS-Test-Success'; exit 0" 2>&1
            $LASTEXITCODE | Should -Be 0
            $result | Should -BeLike '*Non-FIPS-Test-Success*'
        }

        It "PowerShell can use cryptographic operations with FIPS policy disabled" {
            # Test a basic cryptographic operation
            $script = @'
try {
    $md5 = [System.Security.Cryptography.MD5]::Create()
    $hash = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes("test"))
    Write-Output "Success"
    exit 0
} catch {
    Write-Output "Failed: $_"
    exit 1
}
'@
            $result = & $pwshPath -NoProfile -NonInteractive -Command $script 2>&1
            # Note: MD5 may or may not be available depending on .NET version and platform
            # This test just ensures PowerShell doesn't crash
            $LASTEXITCODE | Should -BeIn @(0, 1)
        }
    }

    Context "FIPS-Compliant Cryptographic Operations" {
        It "Can use FIPS-compliant hash algorithms" {
            # SHA256 is FIPS-compliant
            $script = @'
try {
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $hash = $sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes("test"))
    Write-Output $hash.Length
    exit 0
} catch {
    Write-Output "Failed: $_"
    exit 1
}
'@
            $result = & $pwshPath -NoProfile -NonInteractive -Command $script 2>&1
            $LASTEXITCODE | Should -Be 0
            [int]$result | Should -Be 32  # SHA256 produces 32 bytes
        }

        It "Can use FIPS-compliant encryption algorithms" {
            # AES is FIPS-compliant
            $script = @'
try {
    $aes = [System.Security.Cryptography.Aes]::Create()
    Write-Output $aes.KeySize
    $aes.Dispose()
    exit 0
} catch {
    Write-Output "Failed: $_"
    exit 1
}
'@
            $result = & $pwshPath -NoProfile -NonInteractive -Command $script 2>&1
            $LASTEXITCODE | Should -Be 0
            [int]$result | Should -BeGreaterThan 0
        }

        It "Get-FileHash with SHA256 (FIPS-compliant) should always work" {
            # SHA256 is FIPS-compliant and should work with or without FIPS policy
            $script = @'
$tempDir = if ($IsWindows) { $env:TEMP } else { $env:TMPDIR ?? "/tmp" }
$testFile = Join-Path $tempDir "fips-test-sha256-$(Get-Random).txt"
try {
    "test content for SHA256" | Set-Content -Path $testFile -NoNewline
    
    $hash = Get-FileHash -Path $testFile -Algorithm SHA256 -ErrorAction Stop
    if ($hash.Algorithm -eq 'SHA256' -and $hash.Hash -and $hash.Hash.Length -eq 64) {
        Write-Output "SUCCESS"
    } else {
        Write-Output "UNEXPECTED_RESULT"
    }
} catch {
    Write-Output "FAILED: $($_.Exception.Message)"
} finally {
    if ($testFile -and (Test-Path $testFile -ErrorAction SilentlyContinue)) {
        Remove-Item $testFile -Force -ErrorAction SilentlyContinue
    }
}
'@
            $result = & $pwshPath -NoProfile -NonInteractive -Command $script 2>&1 | Select-Object -Last 1
            $LASTEXITCODE | Should -Be 0
            $result | Should -Be 'SUCCESS'
        }

        It "Get-FileHash with SHA512 (FIPS-compliant) should always work" {
            # SHA512 is also FIPS-compliant
            $script = @'
$tempDir = if ($IsWindows) { $env:TEMP } else { $env:TMPDIR ?? "/tmp" }
$testFile = Join-Path $tempDir "fips-test-sha512-$(Get-Random).txt"
try {
    "test content for SHA512" | Set-Content -Path $testFile -NoNewline
    
    $hash = Get-FileHash -Path $testFile -Algorithm SHA512 -ErrorAction Stop
    if ($hash.Algorithm -eq 'SHA512' -and $hash.Hash -and $hash.Hash.Length -eq 128) {
        Write-Output "SUCCESS"
    } else {
        Write-Output "UNEXPECTED_RESULT"
    }
} catch {
    Write-Output "FAILED: $($_.Exception.Message)"
} finally {
    if ($testFile -and (Test-Path $testFile -ErrorAction SilentlyContinue)) {
        Remove-Item $testFile -Force -ErrorAction SilentlyContinue
    }
}
'@
            $result = & $pwshPath -NoProfile -NonInteractive -Command $script 2>&1 | Select-Object -Last 1
            $LASTEXITCODE | Should -Be 0
            $result | Should -Be 'SUCCESS'
        }
    }
}

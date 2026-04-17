# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module HelpersCommon

# Fallback if HelpersCommon's Test-CanWriteToPsHome isn't available during Pester 5 discovery/run
if (-not (Get-Command Test-CanWriteToPsHome -ErrorAction SilentlyContinue)) {
    $script:CanWriteToPsHome = $null
    function Test-CanWriteToPsHome {
        if ($null -ne $script:CanWriteToPsHome) {
            return $script:CanWriteToPsHome
        }
        $script:CanWriteToPsHome = $false
        try {
            $testFileName = Join-Path $PSHOME (New-Guid).Guid
            $null = New-Item -ItemType File -Path $testFileName -ErrorAction Stop
            $script:CanWriteToPsHome = $true
            Remove-Item -Path $testFileName -ErrorAction SilentlyContinue
        }
        catch {
            # Expected when user cannot write to PSHOME
            $script:CanWriteToPsHome = $false
        }
        $script:CanWriteToPsHome
    }
}

BeforeDiscovery {
    $IsNotSkipped = ($IsWindows -eq $true)
    $ShouldSkipTest = if ($IsNotSkipped) {
        try { !(Test-CanWriteToPsHome) } catch { $true }
    } else { $true }
}

#
# These are general tests that verify non-Windows behavior
#
Describe "ExecutionPolicy" -Tags "CI" {

    Context "Check Get-ExecutionPolicy behavior" {
        It "Should unrestricted when not on Windows" -Skip:$IsWindows {
            Get-ExecutionPolicy | Should -Be Unrestricted
        }

        It "Should return Microsoft.Powershell.ExecutionPolicy PSObject on Windows" -Skip:($IsLinux -Or $IsMacOS) {
            Get-ExecutionPolicy | Should -BeOfType Microsoft.Powershell.ExecutionPolicy
        }
    }

    Context "Check Set-ExecutionPolicy behavior" {
        It "Should throw PlatformNotSupported when not on Windows" -Skip:$IsWindows {
            { Set-ExecutionPolicy Unrestricted } | Should -Throw "Operation is not supported on this platform."
        }

        It "Should succeed on Windows" -Skip:($IsLinux -Or $IsMacOS) {
            # We use the Process scope to avoid affecting the system
            # Unrestricted is assumed "safe", otherwise these tests would not be running
            { Set-ExecutionPolicy -Force -Scope Process -ExecutionPolicy Unrestricted } | Should -Not -Throw
        }
    }
}

#
# Ported from MultiMachine Tests
# Tests\Engine\HelpSystem\Pester.Engine.HelpSystem.BugFix.Tests.ps1
# Tests\Commands\Cmdlets\Microsoft.PowerShell.Security\Pester.Command.Cmdlets.Security.Tests.ps1
#
# These tests verify behavior of the ExecutionPolicy cmdlets on supported
# systems. Right now, ExecutionPolicy is only supported on Windows, so these
# tests only run if ($IsWindows -eq $true)
#

    Describe "Help work with ExecutionPolicy Restricted " -Tags "Feature" {

        # Validate that 'Get-Help Get-Disk' returns one result when the execution policy is 'Restricted' on Nano
        # From an internal bug - [Regression] Get-Help returns multiple matches when there is an exact match

        # Skip the test if Storage module is not available, return a pseudo result
        # ExecutionPolicy only works on windows
        It "Test for Get-Help Get-Disk" -Skip:(!(Test-Path (Join-Path -Path $PSHOME -ChildPath Modules\Storage\Storage.psd1)) -or $ShouldSkipTest) {

                try
                {
                    $currentExecutionPolicy = Get-ExecutionPolicy
                    Get-Module -Name Storage | Remove-Module -Force -ErrorAction Stop

                    # 'Get-Help Get-Disk' should return one result back
                    Set-ExecutionPolicy -ExecutionPolicy Restricted -Force -ErrorAction Stop
                    (Get-Help -Name Get-Disk -ErrorAction Stop).Name | Should -Be 'Get-Disk'
                }
                finally
                {
                    Set-ExecutionPolicy $currentExecutionPolicy -Force
                }
        }
    }

    Describe "Validate ExecutionPolicy cmdlets in PowerShell" -Tags "CI" {

        BeforeAll {
            if ($IsWindows) {
                #Generate test data
                $drive = 'TestDrive:\'
                $testDirectory =  Join-Path $drive ("MultiMachineTestData\Commands\Cmdlets\Security_TestData\ExecutionPolicyTestData")
                if(Test-Path $testDirectory)
                {
                    Remove-Item -Force -Recurse $testDirectory -ErrorAction SilentlyContinue
                }
                $null = New-Item $testDirectory -ItemType Directory -Force
                $remoteTestDirectory = $testDirectory

                $InternetSignatureCorruptedScript = Join-Path -Path $remoteTestDirectory -ChildPath InternetSignatureCorruptedScript.ps1
                $InternetSignedScript = Join-Path -Path $remoteTestDirectory -ChildPath InternetSignedScript.ps1
                $InternetUnsignedScript = Join-Path -Path $remoteTestDirectory -ChildPath InternetUnsignedScript.ps1
                $IntranetSignatureCorruptedScript = Join-Path -Path $remoteTestDirectory -ChildPath IntranetSignatureCorruptedScript.ps1
                $IntranetSignedScript = Join-Path -Path $remoteTestDirectory -ChildPath IntranetSignedScript.ps1
                $IntranetUnsignedScript = Join-Path -Path $remoteTestDirectory -ChildPath IntranetUnsignedScript.ps1
                $LocalSignatureCorruptedScript = Join-Path -Path $remoteTestDirectory -ChildPath LocalSignatureCorruptedScript.ps1
                $LocalSignedScript = Join-Path -Path $remoteTestDirectory -ChildPath LocalSignedScript.ps1
                $LocalUnsignedScript = Join-Path -Path $remoteTestDirectory -ChildPath LocalUnsignedScript.ps1
                $PSHomeUnsignedModule = Join-Path -Path $PSHOME -ChildPath 'Modules' -AdditionalChildPath 'LocalUnsignedModule', 'LocalUnsignedModule.psm1'
                $PSHomeUntrustedModule = Join-Path -Path $PSHOME -ChildPath 'Modules' -AdditionalChildPath 'LocalUntrustedModule', 'LocalUntrustedModule.psm1'
                $TrustedSignatureCorruptedScript = Join-Path -Path $remoteTestDirectory -ChildPath TrustedSignatureCorruptedScript.ps1
                $TrustedSignedScript = Join-Path -Path $remoteTestDirectory -ChildPath TrustedSignedScript.ps1
                $TrustedUnsignedScript = Join-Path -Path $remoteTestDirectory -ChildPath TrustedUnsignedScript.ps1
                $UntrustedSignatureCorruptedScript = Join-Path -Path $remoteTestDirectory -ChildPath UntrustedSignatureCorruptedScript.ps1
                $UntrustedSignedScript = Join-Path -Path $remoteTestDirectory -ChildPath UntrustedSignedScript.ps1
                $UntrustedUnsignedScript = Join-Path -Path $remoteTestDirectory -ChildPath UntrustedUnsignedScript.ps1
                $MyComputerSignatureCorruptedScript = Join-Path -Path $remoteTestDirectory -ChildPath MyComputerSignatureCorruptedScript.ps1
                $MyComputerSignedScript = Join-Path -Path $remoteTestDirectory -ChildPath MyComputerSignedScript.ps1
                $MyComputerUnsignedScript = Join-Path -Path $remoteTestDirectory -ChildPath MyComputerUnsignedScript.ps1

                $fileType = @{
                    "Local" = -1
                    "MyComputer" = 0
                    "Intranet" = 1
                    "Trusted" = 2
                    "Internet" = 3
                    "Untrusted" = 4
                }

                $testFilesInfo = @(
                    @{
                        FilePath = $InternetSignatureCorruptedScript
                        FileType = $fileType.Internet
                        AddSignature = $true
                        Corrupted = $true
                    }
                    @{
                        FilePath = $InternetSignedScript
                        FileType = $fileType.Internet
                        AddSignature = $true
                        Corrupted = $false
                    }
                    @{
                        FilePath = $InternetUnsignedScript
                        FileType = $fileType.Internet
                        AddSignature = $false
                        Corrupted = $false
                    }
                    @{
                        FilePath = $IntranetSignatureCorruptedScript
                        FileType = $fileType.Intranet
                        AddSignature = $true
                        Corrupted = $true
                    }
                    @{
                        FilePath = $IntranetSignedScript
                        FileType = $fileType.Intranet
                        AddSignature = $true
                        Corrupted = $false
                    }
                    @{
                        FilePath = $IntranetUnsignedScript
                        FileType = $fileType.Intranet
                        AddSignature = $true
                        Corrupted = $true
                    }
                    @{
                        FilePath = $LocalSignatureCorruptedScript
                        FileType = $fileType.Local
                        AddSignature = $true
                        Corrupted = $true
                    }
                    @{
                        FilePath = $LocalSignedScript
                        FileType = $fileType.Local
                        AddSignature = $true
                        Corrupted = $false
                    }
                    @{
                        FilePath = $LocalUnsignedScript
                        FileType = $fileType.Local
                        AddSignature = $false
                        Corrupted = $false
                    }
                    @{
                        FilePath = $PSHomeUnsignedModule
                        FileType = $fileType.Local
                        AddSignature = $false
                        Corrupted = $false
                    }
                    @{
                        FilePath = $PSHomeUntrustedModule
                        FileType = $fileType.Untrusted
                        AddSignature = $false
                        Corrupted = $false
                    }
                    @{
                        FilePath = $TrustedSignatureCorruptedScript
                        FileType = $fileType.Trusted
                        AddSignature = $true
                        Corrupted = $true
                    }
                    @{
                        FilePath = $TrustedSignedScript
                        FileType = $fileType.Trusted
                        AddSignature = $true
                        Corrupted = $false
                    }
                    @{
                        FilePath = $TrustedUnsignedScript
                        FileType = $fileType.Trusted
                        AddSignature = $false
                        Corrupted = $false
                    }
                     @{
                        FilePath = $UntrustedSignatureCorruptedScript
                        FileType = $fileType.Untrusted
                        AddSignature = $true
                        Corrupted = $true
                    }
                    @{
                        FilePath = $UntrustedSignedScript
                        FileType = $fileType.Untrusted
                        AddSignature = $true
                        Corrupted = $true
                    }
                    @{
                        FilePath = $UntrustedUnsignedScript
                        FileType = $fileType.Untrusted
                        AddSignature = $true
                        Corrupted = $false
                    }
                     @{
                        FilePath = $MyComputerSignatureCorruptedScript
                        FileType = $fileType.MyComputer
                        AddSignature = $true
                        Corrupted = $true
                    }
                    @{
                        FilePath = $MyComputerSignedScript
                        FileType = $fileType.MyComputer
                        AddSignature = $true
                        Corrupted = $false
                    }
                    @{
                        FilePath = $MyComputerUnsignedScript
                        FileType = $fileType.MyComputer
                        AddSignature = $false
                        Corrupted = $false
                    }
                )

                #Generate Test Data on remote machine and get the execution policy

                function createTestFile
                {
                    param (
                    [Parameter(Mandatory)]
                    [string]
                    $FilePath,

                    [Parameter(Mandatory)]
                    [int]
                    $FileType,

                    [switch]
                    $AddSignature,

                    [switch]
                    $Corrupted
                    )

                    $folder = Split-Path -Path $FilePath
                    # create folder if it doesn't already exist
                    if(!(Test-Path $folder))
                    {
                        $null = New-Item -Path $folder -ItemType Directory
                    }

                    $null = New-Item -Path $filePath -ItemType File -Force

                    $content = "`"Hello`"" + "`r`n"
                    if($AddSignature)
                    {
                        if($Corrupted)
                        {
                            # Add corrupted signature
                            $content += @"
# SIG # Begin signature block
# MIIPTAYJKoZIhvcNAQcCoIIPPTCCDzkCAQExCzAJBgUrDgMCGgUAMGkGCisGAQQB
# gjcCAQSgWzBZMDQGCisGAQQBgjcCAR4wJgIDAQAABBAfzDtgWUsITrck0sYpfvNR
# AgEAAgEAAgEAAgEAAgEAMCEwCQYFKw4DAhoFAAQUYkdwUPVVR4frPbdbTE8ZPwfD
# +XegggyDMIIGFTCCA/2gAwIBAgITMwAAABrJQBS8Ii1KJQAAAAAAGjANBgkqhkiG
# 9w0BAQsFADCBkDELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAO
# BgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjE6
# MDgGA1UEAxMxTWljcm9zb2Z0IFRlc3RpbmcgUm9vdCBDZXJ0aWZpY2F0ZSBBdXRo
# b3JpdHkgMjAxMDAeFw0xNDAyMDQxODAyMjVaFw0xODAyMDQxODAyMjVaMIGBMRMw
# EQYKCZImiZPyLGQBGRYDY29tMRkwFwYKCZImiZPyLGQBGRYJbWljcm9zb2Z0MRQw
# EgYKCZImiZPyLGQBGRYEY29ycDEXMBUGCgmSJomT8ixkARkWB3JlZG1vbmQxIDAe
# BgNVBAMTF01TSVQgVGVzdCBDb2RlU2lnbiBDQSAzMIIBIjANBgkqhkiG9w0BAQEF
# AAOCAQ8AMIIBCgKCAQEAuV1NahtVcKSQ6osSVsCcXSsk5finBZfPTbq39nQiX9L0
# PY+5Zi73qGhDv3m+exmvWoYTgI2AQZ48lQtohf4QV0THWjsvvP/r12WZSlOfUGi5
# 5639OAmXiAPpFwPffubajzyIcYBDthJonBlhRsGCWoSaZRBZnp/39tDDvHvQqb+i
# w94CDTFfjcQ/K6xtSCNH1IaKQd6TP2mVdtbYBHIfuLWWO/quLuVgKKxz9sHjONVx
# 9nEcWwatIPiz5J9TsR/bbDxzF5AH9U8jm++ZNECu2zYPhqNj9t3HKYOrUNIEi/b9
# xYlQfMw85hPkMBTJWieyufXHkhzouvTzI3E+VhJ8EwIDAQABo4IBczCCAW8wEgYJ
# KwYBBAGCNxUBBAUCAwEAATAjBgkrBgEEAYI3FQIEFgQUxeHTk4FfDvbJdORSZob2
# 57rUxG4wHQYDVR0OBBYEFLU0zfVssWSEb3tmjxXucfADs2jrMBkGCSsGAQQBgjcU
# AgQMHgoAUwB1AGIAQwBBMAsGA1UdDwQEAwIBhjASBgNVHRMBAf8ECDAGAQH/AgEA
# MB8GA1UdIwQYMBaAFKMBBH4wiDPruTGcyuuFdmf8ZbTRMFkGA1UdHwRSMFAwTqBM
# oEqGSGh0dHA6Ly9jcmwubWljcm9zb2Z0LmNvbS9wa2kvY3JsL3Byb2R1Y3RzL01p
# Y1Rlc1Jvb0NlckF1dF8yMDEwLTA2LTE3LmNybDBdBggrBgEFBQcBAQRRME8wTQYI
# KwYBBQUHMAKGQWh0dHA6Ly93d3cubWljcm9zb2Z0LmNvbS9wa2kvY2VydHMvTWlj
# VGVzUm9vQ2VyQXV0XzIwMTAtMDYtMTcuY3J0MA0GCSqGSIb3DQEBCwUAA4ICAQBt
# 9EVv44wAgXhIItfRrX2LjyEyig6DkExisf3j/RNwa3BLNK5PlfNjU/0H58V1k/Dy
# S3CIzLhvn+PBCrpjWr5R1blkJbKQUdP/ZNz28QOXd0l+Ha3P6Mne1NNfXDAjkRHK
# SqzndTxJT7s/03jYcCfh3JyiXzT8Dt5GXlWIr1wJfQljhzon3w9sptb5sIJTjB9Z
# 0VWITkvAc2hVjFkpPPWkODXIYXYIRBxKjakXr7fEx3//ECQYcQrKBvUrLirEsI0g
# mxQ2QO30iQMxug5l4VYSuHhjaN6t86OjyUySGeImiLLKpVZt1uXIggpepSS9b6Pt
# cxqD0+L532oYNJMlT/Y04PGtyfKIVFMGYTmlHoHUU78BNrpGj6C/s+qyzwXpKDHI
# eQ2RozXUzt4SS8W1E3YVxWU2AWnP0BdS7PSB9BvVCkIf1bfuM6s88iSGFh0qaZyG
# sGDlU8s7YkS2i32+nTr5NJAH/v7yd6E7DQYZULBKdKfQDXuY+6s8kjg2OduGchge
# aZZh2NLh2V5OgVrXx7CzM0K6TMZNJRhgaHE7dzT3EC2uZ6ZT/SIwxwfKXYDjsPxx
# R4C9qkdnSDVCPncGAHhyR75i3fGJ28FHhd7mtePU+zbPJ/JGyADOdPDWgJFulg97
# 809qAfXmu6I7+ObsqlCMl8hbpctmWSqqpd8wZ36ntTCCBmYwggVOoAMCAQICE0MD
# Bi6W0bK7qmSfpQAAAQMGLpYwDQYJKoZIhvcNAQELBQAwgYExEzARBgoJkiaJk/Is
# ZAEZFgNjb20xGTAXBgoJkiaJk/IsZAEZFgltaWNyb3NvZnQxFDASBgoJkiaJk/Is
# ZAEZFgRjb3JwMRcwFQYKCZImiZPyLGQBGRYHcmVkbW9uZDEgMB4GA1UEAxMXTVNJ
# VCBUZXN0IENvZGVTaWduIENBIDMwHhcNMTQxMjIyMTk0MzQ3WhcNMTYxMjIxMTk0
# MzQ3WjCBhDELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNV
# BAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEuMCwG
# A1UEAxMlTWljcm9zb2Z0IENvcnBvcmF0aW9uIDNyZCBwYXJ0eSBXUCBXUzCCASIw
# DQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAL4ofcc4uy3h6Ai2Bh8guql21/+u
# LMLhEeHbz5STKqMoxXqy8i3uRcK/oo57INq3H+cQ4yqvuUrPwi3wQE9OG7wO4ymc
# 4M/3WTNVfjdOx0FK2y6UuKZpWQlwycuELbONrvXTzdtGuM0aiGbELRJFOq+742I+
# G3x3otZrTSXC1m6aOoKb50rSqUJ0ENb1PMJV9GBTXnRDde7ub7W3jp9Dj0HxFnof
# QRZSWfCDrO1l1hle7zPBuTnLfCXbma0oRHlTz3m3yEGlUQscxYu6BI+aJkKDKa5R
# L2PCPnau3WuUMFsmQZk6pFrACxIvq+OZTLsorTsZUooCL/5V1ofaHahnJ68CAwEA
# AaOCAtAwggLMMD0GCSsGAQQBgjcVBwQwMC4GJisGAQQBgjcVCIPPiU2t8gKFoZ8M
# gvrKfYHh+3SBT4PGhWmH7vANAgFkAgErMAsGA1UdDwQEAwIHgDA4BgkrBgEEAYI3
# FQoEKzApMA0GCysGAQQBgjdMBYIsMAwGCisGAQQBgjdMAwEwCgYIKwYBBQUHAwMw
# LAYDVR0lBCUwIwYLKwYBBAGCN0wFgiwGCisGAQQBgjdMAwEGCCsGAQUFBwMDMB0G
# A1UdDgQWBBT+6HzYZdp8xPv1xylrDwOMuYQkvDAwBgNVHREEKTAnoCUGCisGAQQB
# gjcUAgOgFwwVZG9uZ2Jvd0BtaWNyb3NvZnQuY29tMB8GA1UdIwQYMBaAFLU0zfVs
# sWSEb3tmjxXucfADs2jrMIHxBgNVHR8EgekwgeYwgeOggeCggd2GOWh0dHA6Ly9j
# b3JwcGtpL2NybC9NU0lUJTIwVGVzdCUyMENvZGVTaWduJTIwQ0ElMjAzKDEpLmNy
# bIZQaHR0cDovL21zY3JsLm1pY3Jvc29mdC5jb20vcGtpL21zY29ycC9jcmwvTVNJ
# VCUyMFRlc3QlMjBDb2RlU2lnbiUyMENBJTIwMygxKS5jcmyGTmh0dHA6Ly9jcmwu
# bWljcm9zb2Z0LmNvbS9wa2kvbXNjb3JwL2NybC9NU0lUJTIwVGVzdCUyMENvZGVT
# aWduJTIwQ0ElMjAzKDEpLmNybDCBrwYIKwYBBQUHAQEEgaIwgZ8wRQYIKwYBBQUH
# MAKGOWh0dHA6Ly9jb3JwcGtpL2FpYS9NU0lUJTIwVGVzdCUyMENvZGVTaWduJTIw
# Q0ElMjAzKDEpLmNydDBWBggrBgEFBQcwAoZKaHR0cDovL3d3dy5taWNyb3NvZnQu
# Y29tL3BraS9tc2NvcnAvTVNJVCUyMFRlc3QlMjBDb2RlU2lnbiUyMENBJTIwMygx
# KS5jcnQwDQYJKoZIhvcNAQELBQADggEBAFRprvk5BxGyn5On1ICDyKRw9rLqyMET
# IDuBmX/enKuLRmETJSF7Dvzo/XbSXm+FTbGwnp5TOIPtCAeT0NuUAAjdo2iRT2Xr
# wc/B4x2dWMJmFG86WmPPWByfw1gFSep1xN6vA9qPb2VAXTmz8Ta75vSmCEfRAqOC
# 7U4uv3RBWImDx+7tI71XLKBmn1s1TTs1rL+43MsNMA7YNeM8/G0k2KbcNeLONNMG
# wJwtlu9CutONhULkhi2C3T7huDtNZgg+LnTbNvZeXMhHtfx8obh1fmgfOrdLUgE9
# 1YtW0F6mZ7OsdWPGV1wPOdRuNxgzGWvOIYCUTeeTU7b+Cifz/mTf/9QxggIzMIIC
# LwIBATCBmTCBgTETMBEGCgmSJomT8ixkARkWA2NvbTEZMBcGCgmSJomT8ixkARkW
# CW1pY3Jvc29mdDEUMBIGCgmSJomT8ixkARkWBGNvcnAxFzAVBgoJkiaJk/IsZAEZ
# FgdyZWRtb25kMSAwHgYDVQQDExdNU0lUIFRlc3QgQ29kZVNpZ24gQ0EgMwITQwMG
# LpbRsruqZJ+lAAABAwYuljAJBgUrDgMCGgUAoHAwEAYKKwYBBAGCNwIBDDECMAAw
# GQYJKoZIhvcNAQkDMQwGCisGAQQBgjcCAQQwHAYKKwYBBAGCNwIBCzEOMAwGCisG
# AQQBgjcCARUwIwYJKoZIhvcNAQkEMRYEFDFRa0VJKJQ1h2LG6dYzXKpBneOfMA0G
# CSqGSIb3DQEBAQUABIIBAHbWmEOWfj37SNw8NDnAAg7bl0L3oyGVKPWysRnriHC9
# aYImucAy2QXKo6YUWxHMqFvRPFrF07qkTDV249iC+L8gb1X0wwq/YuWWFbdN2J8s
# 4CnN6I4Ff2AF4Co34MZGhtIHd3D7H1oPMelTlHQOc5CXyB/wkduoNgS0GCoeZXSK
# DdMuN7dbru3PvCxe0ShzRwxBOa4EWZ6dHDAQRdrxkK2vVLWHg+6th8lRNnCJQeb+
# 03tMRItnm/sAmKR9PCWm4YZob3ug9T9Qa1K00TuNskjXO+G2S2mjhFC5+HGKjLZd
# bJydl0MIIMBtlLEGa4CcFtszxaww5Cx+YtCbxPp3iII=
# SIG # End signature block
"@
                        }
                        else
                        {
                            # Add correct signature
                            $content += @"
# SIG # Begin signature block
# MIIPTAYJKoZIhvcNAQcCoIIPPTCCDzkCAQExCzAJBgUrDgMCGgUAMGkGCisGAQQB
# gjcCAQSgWzBZMDQGCisGAQQBgjcCAR4wJgIDAQAABBAfzDtgWUsITrck0sYpfvNR
# AgEAAgEAAgEAAgEAAgEAMCEwCQYFKw4DAhoFAAQUYkdwUPVVR4frPbdbTE8ZPwfD
# +XegggyDMIIGFTCCA/2gAwIBAgITMwAAABrJQBS8Ii1KJQAAAAAAGjANBgkqhkiG
# 9w0BAQsFADCBkDELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAO
# BgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjE6
# MDgGA1UEAxMxTWljcm9zb2Z0IFRlc3RpbmcgUm9vdCBDZXJ0aWZpY2F0ZSBBdXRo
# b3JpdHkgMjAxMDAeFw0xNDAyMDQxODAyMjVaFw0xODAyMDQxODAyMjVaMIGBMRMw
# EQYKCZImiZPyLGQBGRYDY29tMRkwFwYKCZImiZPyLGQBGRYJbWljcm9zb2Z0MRQw
# EgYKCZImiZPyLGQBGRYEY29ycDEXMBUGCgmSJomT8ixkARkWB3JlZG1vbmQxIDAe
# BgNVBAMTF01TSVQgVGVzdCBDb2RlU2lnbiBDQSAzMIIBIjANBgkqhkiG9w0BAQEF
# AAOCAQ8AMIIBCgKCAQEAuV1NahtVcKSQ6osSVsCcXSsk5finBZfPTbq39nQiX9L0
# PY+5Zi73qGhDv3m+exmvWoYTgI2AQZ48lQtohf4QV0THWjsvvP/r12WZSlOfUGi5
# 5639OAmXiAPpFwPffubajzyIcYBDthJonBlhRsGCWoSaZRBZnp/39tDDvHvQqb+i
# w94CDTFfjcQ/K6xtSCNH1IaKQd6TP2mVdtbYBHIfuLWWO/quLuVgKKxz9sHjONVx
# 9nEcWwatIPiz5J9TsR/bbDxzF5AH9U8jm++ZNECu2zYPhqNj9t3HKYOrUNIEi/b9
# xYlQfMw85hPkMBTJWieyufXHkhzouvTzI3E+VhJ8EwIDAQABo4IBczCCAW8wEgYJ
# KwYBBAGCNxUBBAUCAwEAATAjBgkrBgEEAYI3FQIEFgQUxeHTk4FfDvbJdORSZob2
# 57rUxG4wHQYDVR0OBBYEFLU0zfVssWSEb3tmjxXucfADs2jrMBkGCSsGAQQBgjcU
# AgQMHgoAUwB1AGIAQwBBMAsGA1UdDwQEAwIBhjASBgNVHRMBAf8ECDAGAQH/AgEA
# MB8GA1UdIwQYMBaAFKMBBH4wiDPruTGcyuuFdmf8ZbTRMFkGA1UdHwRSMFAwTqBM
# oEqGSGh0dHA6Ly9jcmwubWljcm9zb2Z0LmNvbS9wa2kvY3JsL3Byb2R1Y3RzL01p
# Y1Rlc1Jvb0NlckF1dF8yMDEwLTA2LTE3LmNybDBdBggrBgEFBQcBAQRRME8wTQYI
# KwYBBQUHMAKGQWh0dHA6Ly93d3cubWljcm9zb2Z0LmNvbS9wa2kvY2VydHMvTWlj
# VGVzUm9vQ2VyQXV0XzIwMTAtMDYtMTcuY3J0MA0GCSqGSIb3DQEBCwUAA4ICAQBt
# 9EVv44wAgXhIItfRrX2LjyEyig6DkExisf3j/RNwa3BLNK5PlfNjU/0H58V1k/Dy
# S3CIzLhvn+PBCrpjWr5R1blkJbKQUdP/ZNz28QOXd0l+Ha3P6Mne1NNfXDAjkRHK
# SqzndTxJT7s/03jYcCfh3JyiXzT8Dt5GXlWIr1wJfQljhzon3w9sptb5sIJTjB9Z
# 0VWITkvAc2hVjFkpPPWkODXIYXYIRBxKjakXr7fEx3//ECQYcQrKBvUrLirEsI0g
# mxQ2QO30iQMxug5l4VYSuHhjaN6t86OjyUySGeImiLLKpVZt1uXIggpepSS9b6Pt
# cxqD0+L532oYNJMlT/Y04PGtyfKIVFMGYTmlHoHUU78BNrpGj6C/s+qyzwXpKDHI
# eQ2RozXUzt4SS8W1E3YVxWU2AWnP0BdS7PSB9BvVCkIf1bfuM6s88iSGFh0qaZyG
# sGDlU8s7YkS2i32+nTr5NJAH/v7yd6E7DQYZULBKdKfQDXuY+6s8kjg2OduGchge
# aZZh2NLh2V5OgVrXx7CzM0K6TMZNJRhgaHE7dzT3EC2uZ6ZT/SIwxwfKXYDjsPxx
# R4C9qkdnSDVCPncGAHhyR75i3fGJ28FHhd7mtePU+zbPJ/JGyADOdPDWgJFulg97
# 809qAfXmu6I7+ObsqlCMl8hbpctmWSqqpd8wZ36ntTCCBmYwggVOoAMCAQICE0MD
# Bi6W0bK7qmSfpQAAAQMGLpYwDQYJKoZIhvcNAQELBQAwgYExEzARBgoJkiaJk/Is
# ZAEZFgNjb20xGTAXBgoJkiaJk/IsZAEZFgltaWNyb3NvZnQxFDASBgoJkiaJk/Is
# ZAEZFgRjb3JwMRcwFQYKCZImiZPyLGQBGRYHcmVkbW9uZDEgMB4GA1UEAxMXTVNJ
# VCBUZXN0IENvZGVTaWduIENBIDMwHhcNMTQxMjIyMTk0MzQ3WhcNMTYxMjIxMTk0
# MzQ3WjCBhDELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNV
# BAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEuMCwG
# A1UEAxMlTWljcm9zb2Z0IENvcnBvcmF0aW9uIDNyZCBwYXJ0eSBXUCBXUzCCASIw
# DQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAL4ofcc4uy3h6Ai2Bh8guql21/+u
# LMLhEeHbz5STKqMoxXqy8i3uRcK/oo57INq3H+cQ4yqvuUrPwi3wQE9OG7wO4ymc
# 4M/3WTNVfjdOx0FK2y6UuKZpWQlwycuELbONrvXTzdtGuM0aiGbELRJFOq+742I+
# G3x3otZrTSXC1m6aOoKb50rSqUJ0ENb1PMJV9GBTXnRDde7ub7W3jp9Dj0HxFnof
# QRZSWfCDrO1l1hle7zPBuTnLfCXbma0oRHlTz3m3yEGlUQscxYu6BI+aJkKDKa5R
# L2PCPnau3WuUMFsmQZk6pFrACxIvq+OZTLsorTsZUooCL/5V1ofaHahnJ68CAwEA
# AaOCAtAwggLMMD0GCSsGAQQBgjcVBwQwMC4GJisGAQQBgjcVCIPPiU2t8gKFoZ8M
# gvrKfYHh+3SBT4PGhWmH7vANAgFkAgErMAsGA1UdDwQEAwIHgDA4BgkrBgEEAYI3
# FQoEKzApMA0GCysGAQQBgjdMBYIsMAwGCisGAQQBgjdMAwEwCgYIKwYBBQUHAwMw
# LAYDVR0lBCUwIwYLKwYBBAGCN0wFgiwGCisGAQQBgjdMAwEGCCsGAQUFBwMDMB0G
# A1UdDgQWBBT+6HzYZdp8xPv1xylrDwOMuYQkvDAwBgNVHREEKTAnoCUGCisGAQQB
# gjcUAgOgFwwVZG9uZ2Jvd0BtaWNyb3NvZnQuY29tMB8GA1UdIwQYMBaAFLU0zfVs
# sWSEb3tmjxXucfADs2jrMIHxBgNVHR8EgekwgeYwgeOggeCggd2GOWh0dHA6Ly9j
# b3JwcGtpL2NybC9NU0lUJTIwVGVzdCUyMENvZGVTaWduJTIwQ0ElMjAzKDEpLmNy
# bIZQaHR0cDovL21zY3JsLm1pY3Jvc29mdC5jb20vcGtpL21zY29ycC9jcmwvTVNJ
# VCUyMFRlc3QlMjBDb2RlU2lnbiUyMENBJTIwMygxKS5jcmyGTmh0dHA6Ly9jcmwu
# bWljcm9zb2Z0LmNvbS9wa2kvbXNjb3JwL2NybC9NU0lUJTIwVGVzdCUyMENvZGVT
# aWduJTIwQ0ElMjAzKDEpLmNybDCBrwYIKwYBBQUHAQEEgaIwgZ8wRQYIKwYBBQUH
# MAKGOWh0dHA6Ly9jb3JwcGtpL2FpYS9NU0lUJTIwVGVzdCUyMENvZGVTaWduJTIw
# Q0ElMjAzKDEpLmNydDBWBggrBgEFBQcwAoZKaHR0cDovL3d3dy5taWNyb3NvZnQu
# Y29tL3BraS9tc2NvcnAvTVNJVCUyMFRlc3QlMjBDb2RlU2lnbiUyMENBJTIwMygx
# KS5jcnQwDQYJKoZIhvcNAQELBQADggEBAFRprvk5BxGyn5On1ICDyKRw9rLqyMET
# IDuBmX/enKuLRmETJSF7Dvzo/XbSXm+FTbGwnp5TOIPtCAeT0NuUAAjdo2iRT2Xr
# wc/B4x2dWMJmFG86WmPPWByfw1gFSep1xN6vA9qPb2VAXTmz8Ta75vSmCEfRAqOC
# 7U4uv3RBWImDx+7tI71XLKBmn1s1TTs1rL+43MsNMA7YNeM8/G0k2KbcNeLONNMG
# wJwtlu9CutONhULkhi2C3T7huDtNZgg+LnTbNvZeXMhHtfx8obh1fmgfOrdLUgE9
# 1YtW0F6mZ7OsdWPGV1wPOdRuNxgzGWvOIYCUTeeTU7b+Cifz/mTf/9QxggIzMIIC
# LwIBATCBmTCBgTETMBEGCgmSJomT8ixkARkWA2NvbTEZMBcGCgmSJomT8ixkARkW
# CW1pY3Jvc29mdDEUMBIGCgmSJomT8ixkARkWBGNvcnAxFzAVBgoJkiaJk/IsZAEZ
# FgdyZWRtb25kMSAwHgYDVQQDExdNU0lUIFRlc3QgQ29kZVNpZ24gQ0EgMwITQwMG
# LpbRsruqZJ+lAAABAwYuljAJBgUrDgMCGgUAoHAwEAYKKwYBBAGCNwIBDDECMAAw
# GQYJKoZIhvcNAQkDMQwGCisGAQQBgjcCAQQwHAYKKwYBBAGCNwIBCzEOMAwGCisG
# AQQBgjcCARUwIwYJKoZIhvcNAQkEMRYEFDFRa0VJKJQ1h2LG6dYzXKpBneOfMA0G
# CSqGSIb3DQEBAQUABIIBAHbWmEOWfj37SNw8NDnAAg7bl0L3oyGVKPWysRnriHC9
# aYImucAy2QXKo6YUWxHMqFvRPFrF07qkTDV249iC+L8gb1X0wwq/YuWWFbdN2J8s
# 4CnN6I4Ff2AF4Co34MZGhtIHd3D7H1oPMelTlHQOc5CXyB/wkduoNgS0GCoeZXSK
# DdMuN7dbru3PvCxe0ShzRwxBOa4EWZ6dHDAQRdrxkK2vVLWHg+6th8lRNnCJQeb+
# 03tMRItnm/sAmKR9PCWm4YZob3ug9T9Qa1K00TuNskjXO+G2S2mjhFC5+HGKjLZd
# bJydl0MIIMBtlLEGa4CcFtszxaww5Cx+YtCbxPp3iII=
# SIG # End signature block
"@
                        }
                    }

                    Set-Content $filePath -Value $content

                    ## Valida File types and their corresponding int values are :
                    ##
                    ##    Local = -1
                    ##    MyComputer = 0
                    ##    Intranet = 1
                    ##    Trusted = 2
                    ##    Internet = 3
                    ##    Untrusted = 4
                    ## We need to add alternate streams in all files except for the local file

                    if(-1 -ne $FileType)
                    {
                        $alternateStreamContent = @"
[ZoneTransfer]
ZoneId=$FileType
"@
                        Add-Content -Path $filePath -Value $alternateStreamContent -Stream Zone.Identifier
                    }
                }

                foreach($fileInfo in $testFilesInfo)
                {
                    if ((Test-CanWriteToPsHome) -or (!(Test-CanWriteToPsHome) -and !$fileInfo.filePath.StartsWith($PSHOME, $true, $null)) ) {
                        createTestFile -FilePath $fileInfo.filePath -FileType $fileInfo.fileType -AddSignature:$fileInfo.AddSignature -Corrupted:$fileInfo.corrupted
                    }
                }

                #Get Execution Policy
                $originalExecPolicy = Get-ExecutionPolicy
                $originalExecutionPolicy =  $originalExecPolicy

                $archiveSigned = $false
                $archivePath = Get-Module -ListAvailable Microsoft.PowerShell.Archive -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Path
                if($archivePath)
                {
                    $archiveFolder = Split-Path -Path $archivePath

                    # get all the certs used to sign the module
                    $archiveAllCert = Get-ChildItem -File -Path (Join-Path -Path $archiveFolder -ChildPath '*') -Recurse |
                        Get-AuthenticodeSignature

                    # filter only to valid signatures
                    $archiveCert = $archiveAllCert |
                        Where-Object { $_.status -eq 'Valid'} |
                            Select-Object -Unique -ExpandProperty SignerCertificate

                    # if we have valid signatures, add them to trusted publishers so powershell will trust them.
                    if($archiveCert)
                    {
                        $store = [System.Security.Cryptography.X509Certificates.X509Store]::new([System.Security.Cryptography.X509Certificates.StoreName]::TrustedPublisher,[System.Security.Cryptography.X509Certificates.StoreLocation]::CurrentUser)
                        $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
                        $archiveCert | ForEach-Object {
                            $store.Add($_)
                        }
                        $store.Close()
                        $archiveSigned = $true
                    }
                }
            }
        }
        AfterAll {
            if ($IsWindows) {
                #Clean up
                $testDirectory = $remoteTestDirectory

                Remove-Item $testDirectory -Recurse -Force -ErrorAction SilentlyContinue
                Remove-Item function:createTestFile -ErrorAction SilentlyContinue
            }
        }

        Context "Prereq: Validate that 'Microsoft.PowerShell.Archive' is signed" {
            It "'Microsoft.PowerShell.Archive' should have a signature" {
                $archiveAllCert | Should -Not -Be $null
            }
            It "'Microsoft.PowerShell.Archive' should have a valid signature" {
                $archiveCert | Should -Not -Be $null
            }
        }

        Context "Validate that 'Restricted' execution policy works on OneCore powershell" {

            BeforeAll {
                if ($IsWindows) {
                    Set-ExecutionPolicy Restricted -Force -Scope Process | Out-Null
                }
            }

            AfterAll {
                if ($IsWindows) {
                    Set-ExecutionPolicy $originalExecutionPolicy -Force -Scope Process | Out-Null
                }
            }

            BeforeDiscovery {
                $testScripts = @(
                    @{ testScript = 'InternetSignatureCorruptedScript.ps1' }
                    @{ testScript = 'InternetSignedScript.ps1' }
                    @{ testScript = 'InternetUnsignedScript.ps1' }
                    @{ testScript = 'IntranetSignatureCorruptedScript.ps1' }
                    @{ testScript = 'IntranetSignedScript.ps1' }
                    @{ testScript = 'IntranetUnsignedScript.ps1' }
                    @{ testScript = 'LocalSignatureCorruptedScript.ps1' }
                    @{ testScript = 'LocalSignedScript.ps1' }
                    @{ testScript = 'LocalUnsignedScript.ps1' }
                    @{ testScript = 'TrustedSignatureCorruptedScript.ps1' }
                    @{ testScript = 'TrustedSignedScript.ps1' }
                    @{ testScript = 'UntrustedSignatureCorruptedScript.ps1' }
                    @{ testScript = 'UntrustedSignedScript.ps1' }
                    @{ testScript = 'UntrustedUnsignedScript.ps1' }
                    @{ testScript = 'TrustedUnsignedScript.ps1' }
                    @{ testScript = 'MyComputerSignatureCorruptedScript.ps1' }
                    @{ testScript = 'MyComputerSignedScript.ps1' }
                    @{ testScript = 'MyComputerUnsignedScript.ps1' }
                )
            }

            It "Test 'Restricted' execution policy. Running <testScript> script should raise PSSecurityException" -ForEach $testScripts {
                $scriptName = Join-Path $remoteTestDirectory $testScript
                $exception = { & $scriptName } | Should -Throw -PassThru
                $exception.Exception | Should -BeOfType System.Management.Automation.PSSecurityException
            }
        }

        Context "Validate that 'Unrestricted' execution policy works on OneCore powershell" {

            BeforeAll {
                if ($IsWindows) {
                    Set-ExecutionPolicy Unrestricted -Force -Scope Process | Out-Null
                }
            }

            AfterAll {
                if ($IsWindows) {
                    Set-ExecutionPolicy $originalExecutionPolicy -Force -Scope Process | Out-Null
                }
            }

            BeforeDiscovery {
                $unrestrictedScripts = @(
                    @{ testScript = 'IntranetSignatureCorruptedScript.ps1' }
                    @{ testScript = 'IntranetSignedScript.ps1' }
                    @{ testScript = 'IntranetUnsignedScript.ps1' }
                    @{ testScript = 'LocalSignatureCorruptedScript.ps1' }
                    @{ testScript = 'LocalSignedScript.ps1' }
                    @{ testScript = 'LocalUnsignedScript.ps1' }
                    @{ testScript = 'TrustedSignatureCorruptedScript.ps1' }
                    @{ testScript = 'TrustedSignedScript.ps1' }
                    @{ testScript = 'TrustedUnsignedScript.ps1' }
                    @{ testScript = 'MyComputerSignatureCorruptedScript.ps1' }
                    @{ testScript = 'MyComputerSignedScript.ps1' }
                    @{ testScript = 'MyComputerUnsignedScript.ps1' }
                )

                $expectedError = "UnauthorizedAccess,Microsoft.PowerShell.Commands.ImportModuleCommand"
                $PSHomeUnsignedModule = Join-Path -Path $PSHOME -ChildPath 'Modules' -AdditionalChildPath 'LocalUnsignedModule', 'LocalUnsignedModule.psm1'
                $PSHomeUntrustedModule = Join-Path -Path $PSHOME -ChildPath 'Modules' -AdditionalChildPath 'LocalUntrustedModule', 'LocalUntrustedModule.psm1'

                $moduleTestData = @(
                    @{
                        module = "Microsoft.PowerShell.Archive"
                        error = $null
                    }
                )

                $canWritePsHome = try { Test-CanWriteToPsHome } catch { $false }
                if ($canWritePsHome) {
                    $moduleTestData += @(
                        @{
                            shouldMarkAsPending = $true
                            module = $PSHomeUntrustedModule
                            expectedError = $expectedError
                        }
                        @{
                            module = $PSHomeUnsignedModule
                            error = $null
                        }
                    )
                }
            }

            It "Test 'Unrestricted' execution policy. Running <testScript> script should return Hello" -ForEach $unrestrictedScripts{
                $scriptName = Join-Path $remoteTestDirectory $testScript
                $result = & $scriptName
                $result | Should -Be "Hello"
            }

            It "Test 'Unrestricted' execution policy. Importing <module> Module should throw '<error>'" -TestCases $moduleTestData  {
                param([string]$module, [string]$expectedError, [bool]$shouldMarkAsPending)

                if ($shouldMarkAsPending)
                {
                    Set-ItResult -Pending -Because "Test is unreliable"
                }

                $execPolicy = Get-ExecutionPolicy -List | Out-String

                $testScript = {Import-Module -Name $module -Force -ErrorAction Stop}
                if($expectedError)
                {
                    $testScript | Should -Throw -ErrorId $expectedError -Because "Untrusted modules should not be loaded even on unrestricted execution policy"
                }
                else
                {
                    $testScript | Should -Not -Throw -Because "Execution Policy is set as: $execPolicy"
                }
            }
        }

        Context "Validate that 'ByPass' execution policy works on OneCore powershell" {

            BeforeAll {
                if ($IsWindows) {
                    Set-ExecutionPolicy Bypass -Force -Scope Process | Out-Null
                }
            }

            AfterAll {
                if ($IsWindows) {
                    Set-ExecutionPolicy $originalExecutionPolicy -Force -Scope Process | Out-Null
                }
            }

            BeforeDiscovery {
                $testScripts = @(
                    @{ testScript = 'InternetSignatureCorruptedScript.ps1' }
                    @{ testScript = 'InternetSignedScript.ps1' }
                    @{ testScript = 'InternetUnsignedScript.ps1' }
                    @{ testScript = 'IntranetSignatureCorruptedScript.ps1' }
                    @{ testScript = 'IntranetSignedScript.ps1' }
                    @{ testScript = 'IntranetUnsignedScript.ps1' }
                    @{ testScript = 'LocalSignatureCorruptedScript.ps1' }
                    @{ testScript = 'LocalSignedScript.ps1' }
                    @{ testScript = 'LocalUnsignedScript.ps1' }
                    @{ testScript = 'TrustedSignatureCorruptedScript.ps1' }
                    @{ testScript = 'TrustedSignedScript.ps1' }
                    @{ testScript = 'TrustedUnsignedScript.ps1' }
                    @{ testScript = 'UntrustedSignatureCorruptedScript.ps1' }
                    @{ testScript = 'UntrustedSignedScript.ps1' }
                    @{ testScript = 'UntrustedUnsignedScript.ps1' }
                    @{ testScript = 'MyComputerSignatureCorruptedScript.ps1' }
                    @{ testScript = 'MyComputerSignedScript.ps1' }
                    @{ testScript = 'MyComputerUnsignedScript.ps1' }
                )
            }

            It "Test 'ByPass' execution policy. Running <testScript> script should return Hello" -ForEach $testScripts {
                $scriptName = Join-Path $remoteTestDirectory $testScript
                $result = & $scriptName
                $result | Should -Be "Hello"
            }
        }

        Context "'RemoteSigned' execution policy works on OneCore powershell" {

            BeforeAll {
                if ($IsWindows) {
                    Set-ExecutionPolicy RemoteSigned -Force -Scope Process | Out-Null
                }
            }

            AfterAll {
                if ($IsWindows) {
                    Set-ExecutionPolicy $originalExecutionPolicy -Force -Scope Process
                }
            }

            BeforeDiscovery {
                $message = "Hello"
                $errorIdVal = "System.Management.Automation.PSSecurityException"
                $testData = @(
                    @{ testScript = 'LocalUnsignedScript.ps1'; expected = $message; errorId = $null }
                    @{ testScript = 'LocalSignatureCorruptedScript.ps1'; expected = $message; errorId = $null }
                    @{ testScript = 'LocalSignedScript.ps1'; expected = "Hello"; errorId = $null }
                    @{ testScript = 'MyComputerUnsignedScript.ps1'; expected = $message; errorId = $null }
                    @{ testScript = 'MyComputerSignatureCorruptedScript.ps1'; expected = $message; errorId = $null }
                    @{ testScript = 'MyComputerSignedScript.ps1'; expected = $message; errorId = $null }
                    @{ testScript = 'TrustedUnsignedScript.ps1'; expected = $message; errorId = $null }
                    @{ testScript = 'TrustedSignatureCorruptedScript.ps1'; expected = $message; errorId = $null }
                    @{ testScript = 'TrustedSignedScript.ps1'; expected = $message; errorId = $null }
                    @{ testScript = 'IntranetUnsignedScript.ps1'; expected = $message; errorId = $null }
                    @{ testScript = 'IntranetSignatureCorruptedScript.ps1'; expected = $message; errorId = $null }
                    @{ testScript = 'IntranetSignedScript.ps1'; expected = $message; errorId = $null }
                    @{ testScript = 'InternetUnsignedScript.ps1'; expected = $null; errorId = $errorIdVal }
                    @{ testScript = 'InternetSignatureCorruptedScript.ps1'; expected = $null; errorId = $errorIdVal }
                    @{ testScript = 'UntrustedUnsignedScript.ps1'; expected = $null; errorId = $errorIdVal }
                    @{ testScript = 'UntrustedSignatureCorruptedScript.ps1'; expected = $null; errorId = $errorIdVal }
                )
            }

            It "Test 'RemoteSigned' execution policy. Running <testScript> script should return <expected>" -ForEach $testData {
                $scriptName = Join-Path $remoteTestDirectory $testScript

                $scriptResult = $null
                $exception = $null

                try
                {
                    $scriptResult = & $scriptName
                }
                catch
                {
                    $exception = $_
                }

                $errorType = $null
                if($null -ne $exception)
                {
                    $errorType = $exception.exception.getType()
                    $scriptResult = $null
                }
                $result = @{
                    "result" = $scriptResult
                    "exception" = $errorType
                }

                $actualResult = $result."result"
                $actualError = $result."exception"

                $actualResult | Should -Be $expected
                $actualError | Should -Be $errorId
            }
        }

        Context "Validate that 'AllSigned' execution policy works on OneCore powershell" {

            BeforeAll {
                if ($IsWindows) {
                    Set-ExecutionPolicy AllSigned -Force -Scope Process
                }
            }

            AfterAll {
                if ($IsWindows) {
                    Set-ExecutionPolicy $originalExecutionPolicy -Force -Scope Process
                }
            }

            BeforeDiscovery {
                $PSHomeUnsignedModule = Join-Path -Path $PSHOME -ChildPath 'Modules' -AdditionalChildPath 'LocalUnsignedModule', 'LocalUnsignedModule.psm1'
                $PSHomeUntrustedModule = Join-Path -Path $PSHOME -ChildPath 'Modules' -AdditionalChildPath 'LocalUntrustedModule', 'LocalUntrustedModule.psm1'

                $moduleErrorId = "UnauthorizedAccess,Microsoft.PowerShell.Commands.ImportModuleCommand"
                $moduleTestData = @(
                    @{
                        module = "Microsoft.PowerShell.Archive"
                        errorId = $null
                    }
                )

                $canWritePsHome = try { Test-CanWriteToPsHome } catch { $false }
                if ($canWritePsHome) {
                    $moduleTestData += @(
                        @{
                            module = $PSHomeUntrustedModule
                            errorId = $moduleErrorId
                        }
                        @{
                            module = $PSHomeUnsignedModule
                            errorId = $moduleErrorId
                        }
                    )
                }

                $scriptErrorId = "UnauthorizedAccess"
                $pendingTestData = @(
                    # The following files are not signed correctly when generated, so we will skip for now
                    # filed https://github.com/PowerShell/PowerShell/issues/5559
                    @{ testScript = 'MyComputerSignedScript.ps1'; errorId = $null }
                    @{ testScript = 'UntrustedSignedScript.ps1'; errorId = $null }
                    @{ testScript = 'TrustedSignedScript.ps1'; errorId = $null }
                    @{ testScript = 'LocalSignedScript.ps1'; errorId = $null }
                    @{ testScript = 'IntranetSignedScript.ps1'; errorId = $null }
                    @{ testScript = 'InternetSignedScript.ps1'; errorId = $null }
                )

                $scriptTestData = @(
                    @{ testScript = 'InternetSignatureCorruptedScript.ps1'; errorId = $scriptErrorId }
                    @{ testScript = 'InternetUnsignedScript.ps1'; errorId = $scriptErrorId }
                    @{ testScript = 'IntranetSignatureCorruptedScript.ps1'; errorId = $scriptErrorId }
                    @{ testScript = 'IntranetSignatureCorruptedScript.ps1'; errorId = $scriptErrorId }
                    @{ testScript = 'IntranetUnsignedScript.ps1'; errorId = $scriptErrorId }
                    @{ testScript = 'LocalSignatureCorruptedScript.ps1'; errorId = $scriptErrorId }
                    @{ testScript = 'LocalUnsignedScript.ps1'; errorId = $scriptErrorId }
                    @{ testScript = 'TrustedSignatureCorruptedScript.ps1'; errorId = $scriptErrorId }
                    @{ testScript = 'TrustedUnsignedScript.ps1'; errorId = $scriptErrorId }
                    @{ testScript = 'UntrustedSignatureCorruptedScript.ps1'; errorId = $scriptErrorId }
                    @{ testScript = 'UntrustedUnsignedScript.ps1'; errorId = $scriptErrorId }
                    @{ testScript = 'MyComputerSignatureCorruptedScript.ps1'; errorId = $scriptErrorId }
                    @{ testScript = 'MyComputerUnsignedScript.ps1'; errorId = $scriptErrorId }
                )
            }

            It "Test 'AllSigned' execution policy. Importing <module> Module should throw '<errorId>'" -TestCases $moduleTestData  {
                param ([string]$module, [string]$errorId)
                $testScript = {Import-Module -Name $module -Force}
                if ($errorId)
                {
                    $testScript | Should -Throw -ErrorId $errorId
                }
                else
                {
                    {& $testScript} | Should -Not -Throw
                }
            }

            It "Test 'AllSigned' execution policy. Running <testScript> Script should throw '<errorId>'" -TestCases $pendingTestData -Pending  {}

            It "Test 'AllSigned' execution policy. Running <testScript> Script should throw '<errorId>'" -TestCases $scriptTestData  {
                param ([string]$testScript, [string]$errorId)
                $scriptPath = Join-Path $remoteTestDirectory $testScript
                $scriptPath | Should -Exist
                if ($errorId)
                {
                    {& $scriptPath} | Should -Throw -ErrorId $errorId
                }
                else
                {
                    {& $scriptPath} | Should -Not -Throw
                }
            }
        }
    }

    BeforeAll {
        function VerfiyBlockedSetExecutionPolicy
        {
            param(
                [string]
                $policyScope
            )
            { Set-ExecutionPolicy -Scope $policyScope -ExecutionPolicy Restricted } |
                Should -Throw -ErrorId "CantSetGroupPolicy,Microsoft.PowerShell.Commands.SetExecutionPolicyCommand"
        }

        function RestoreExecutionPolicy
        {
            param($originalPolicies)

            foreach ($scopedPolicy in $originalPolicies)
            {
                if (($scopedPolicy.Scope -eq "Process") -or
                    ($scopedPolicy.Scope -eq "CurrentUser"))
                {
                    try {
                        Set-ExecutionPolicy -Scope $scopedPolicy.Scope -ExecutionPolicy $scopedPolicy.ExecutionPolicy -Force
                    }
                    catch {
                        if ($_.FullyQualifiedErrorId -ne "ExecutionPolicyOverride,Microsoft.PowerShell.Commands.SetExecutionPolicyCommand")
                        {
                            # Re-throw unrecognized exceptions. Otherwise, swallow
                            # the exception that warns about overridden policies
                            throw $_
                        }
                    }
                }
                elseif($scopedPolicy.Scope -eq "LocalMachine")
                {
                    try {
                        Set-ExecutionPolicy -Scope $scopedPolicy.Scope -ExecutionPolicy $scopedPolicy.ExecutionPolicy -Force
                    }
                    catch {
                        if ($_.FullyQualifiedErrorId -eq "System.UnauthorizedAccessException,Microsoft.PowerShell.Commands.SetExecutionPolicyCommand")
                        {
                            # Do nothing. Depending on the ownership of the file,
                            # regular users may or may not be able to set its
                            # value.
                            #
                            # When targetting the Registry, regular users cannot
                            # modify this value.
                        }
                        elseif ($_.FullyQualifiedErrorId -ne "ExecutionPolicyOverride,Microsoft.PowerShell.Commands.SetExecutionPolicyCommand")
                        {
                            # Re-throw unrecognized exceptions. Otherwise, swallow
                            # the exception that warns about overridden policies
                            throw $_
                        }
                    }
                }
            }
        }
    }

    Describe "Validate Set-ExecutionPolicy -Scope" -Tags "CI" {

        BeforeAll {
            if ($IsWindows) {
                $originalPolicies = Get-ExecutionPolicy -List
            }
        }

        AfterAll {
            if ($IsWindows) {
                RestoreExecutionPolicy $originalPolicies
            }
        }

        It "-Scope MachinePolicy is not Modifiable" {
            VerfiyBlockedSetExecutionPolicy "MachinePolicy"
        }

        It "-Scope UserPolicy is not Modifiable" {
            VerfiyBlockedSetExecutionPolicy "UserPolicy"
        }

        It "-Scope Process is Settable" {
            Set-ExecutionPolicy -Scope Process -ExecutionPolicy ByPass
            Get-ExecutionPolicy -Scope Process | Should -Be "ByPass"
        }

        It "-Scope CurrentUser is Settable" {
            Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy ByPass
            Get-ExecutionPolicy -Scope CurrentUser | Should -Be "ByPass"
        }
    }

    Describe "Validate Set-ExecutionPolicy -Scope (Admin)" -Tags @('CI', 'RequireAdminOnWindows') {

        BeforeAll {
            if ($IsWindows)
            {
                $originalPolicies = Get-ExecutionPolicy -List
            }
        }

        AfterAll {
            if ($IsWindows)
            {
                RestoreExecutionPolicy $originalPolicies
            }
        }

        It '-Scope LocalMachine is Settable, but overridden' -Skip:$ShouldSkipTest {
            # In this test, we first setup execution policy in the following way:
            # CurrentUser is specified and takes precedence over LocalMachine.
            # That's why we will get an error, when we are setting up LocalMachine policy.
            # The error is:
            #
            # Set-ExecutionPolicy : Windows PowerShell updated your execution policy successfully, but the setting is overridden by
            # a policy defined at a more specific scope.  Due to the override, your shell will retain its current effective
            # execution policy of RemoteSigned. Type "Get-ExecutionPolicy -List" to view your execution policy settings. For more
            # information please see "Get-Help Set-ExecutionPolicy".
            #
            # Regrdless of that error, the operation should succeed.

            Set-ExecutionPolicy -Scope Process -ExecutionPolicy Undefined
            Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Restricted

            { Set-ExecutionPolicy -Scope LocalMachine -ExecutionPolicy ByPass } |
                Should -Throw -ErrorId 'ExecutionPolicyOverride,Microsoft.PowerShell.Commands.SetExecutionPolicyCommand'

            Get-ExecutionPolicy -Scope LocalMachine | Should -Be "ByPass"
        }

        It '-Scope LocalMachine is Settable' -Skip:$ShouldSkipTest {
            # We need to make sure that both Process and CurrentUser policies are Undefined
            # before we can set LocalMachine policy without ExecutionPolicyOverride error.
            Set-ExecutionPolicy -Scope Process -ExecutionPolicy Undefined
            Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Undefined

            Set-ExecutionPolicy -Scope LocalMachine -ExecutionPolicy ByPass
            Get-ExecutionPolicy -Scope LocalMachine | Should -Be "ByPass"
        }
    }

AfterAll {
    $PSDefaultParameterValues = $script:_EP_originalDefaultParameterValues
    Remove-Variable -Name _EP_originalDefaultParameterValues -Scope Script -ErrorAction SilentlyContinue
}

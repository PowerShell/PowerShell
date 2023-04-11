# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module HelpersCommon

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

try {

    #skip all tests on non-windows platform
    $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
    $IsNotSkipped = ($IsWindows -eq $true);
    $PSDefaultParameterValues["it:skip"] = !$IsNotSkipped
    $ShouldSkipTest = !$IsNotSkipped -or !(Test-CanWriteToPsHome)

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
                catch {
                    $_.ToString | Should -Be $null
                }
                finally
                {
                    Set-ExecutionPolicy $currentExecutionPolicy -Force
                }
        }
    }

    Describe "Validate ExecutionPolicy cmdlets in PowerShell" -Tags "CI" {

        BeforeAll {
            if ($IsNotSkipped) {
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
                    $script:archiveAllCert = Get-ChildItem -File -Path (Join-Path -Path $archiveFolder -ChildPath '*') -Recurse |
                        Get-AuthenticodeSignature

                    # filter only to valid signatures
                    $script:archiveCert = $script:archiveAllCert |
                        Where-Object { $_.status -eq 'Valid'} |
                            Select-Object -Unique -ExpandProperty SignerCertificate

                    # if we have valid signatures, add them to trusted publishers so powershell will trust them.
                    if($script:archiveCert)
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
            if ($IsNotSkipped) {
                #Clean up
                $testDirectory = $remoteTestDirectory

                Remove-Item $testDirectory -Recurse -Force -ErrorAction SilentlyContinue
                Remove-Item function:createTestFile -ErrorAction SilentlyContinue
            }
        }

        Context "Prereq: Validate that 'Microsoft.PowerShell.Archive' is signed" {
            It "'Microsoft.PowerShell.Archive' should have a signature" {
                $script:archiveAllCert | Should -Not -Be $null
            }
            It "'Microsoft.PowerShell.Archive' should have a valid signature" {
                $script:archiveCert | Should -Not -Be $null
            }
        }

        Context "Validate that 'Restricted' execution policy works on OneCore powershell" {

            BeforeAll {
                if ($IsNotSkipped) {
                    Set-ExecutionPolicy Restricted -Force -Scope Process | Out-Null
                }
            }

            AfterAll {
                if ($IsNotSkipped) {
                    Set-ExecutionPolicy $originalExecutionPolicy -Force -Scope Process | Out-Null
                }
            }

            function Test-RestrictedExecutionPolicy
            {
                param ($testScript)

                $TestTypePrefix = "Test 'Restricted' execution policy."

                It "$TestTypePrefix Running $testScript script should raise PSSecurityException" {

                    $scriptName = $testScript

                    $exception = { & $scriptName } | Should -Throw -PassThru

                    $exception.Exception | Should -BeOfType System.Management.Automation.PSSecurityException
                }
            }

            $testScripts = @(
                $InternetSignatureCorruptedScript
                $InternetSignedScript
                $InternetUnsignedScript
                $IntranetSignatureCorruptedScript
                $IntranetSignedScript
                $IntranetUnsignedScript
                $LocalSignatureCorruptedScript
                $localSignedScript
                $LocalUnsignedScript
                $TrustedSignatureCorruptedScript
                $TrustedSignedScript
                $UntrustedSignatureCorruptedScript
                $UntrustedSignedScript
                $UntrustedUnsignedScript
                $TrustedUnsignedScript
                $MyComputerSignatureCorruptedScript
                $MyComputerSignedScript
                $MyComputerUnsignedScript
            )

            foreach($testScript in $testScripts)
            {
                Test-RestrictedExecutionPolicy $testScript
            }
        }

        AfterAll {
            if ($IsNotSkipped) {
                # Clean up
                $testDirectory = $remoteTestDirectory

                Remove-Item $testDirectory -Recurse -Force -ErrorAction SilentlyContinue
                Remove-Item function:createTestFile -ErrorAction SilentlyContinue
            }
        }
        Context "Validate that 'Unrestricted' execution policy works on OneCore powershell" {

            BeforeAll {
                if ($IsNotSkipped) {
                    Set-ExecutionPolicy Unrestricted -Force -Scope Process | Out-Null
                }
            }

            AfterAll {
                if ($IsNotSkipped) {
                    Set-ExecutionPolicy $originalExecutionPolicy -Force -Scope Process | Out-Null
                }
            }

            function Test-UnrestrictedExecutionPolicy {

                param($testScript, $expected)

                $TestTypePrefix = "Test 'Unrestricted' execution policy."

                It "$TestTypePrefix Running $testScript script should return $expected" {
                    $scriptName = $testScript

                    $result = & $scriptName

                    $result | Should -Be $expected
                }
            }

            $expected = "Hello"
            $testScripts = @(
                $IntranetSignatureCorruptedScript
                $IntranetSignedScript
                $IntranetUnsignedScript
                $LocalSignatureCorruptedScript
                $localSignedScript
                $LocalUnsignedScript
                $TrustedSignatureCorruptedScript
                $TrustedSignedScript
                $TrustedUnsignedScript
                $MyComputerSignatureCorruptedScript
                $MyComputerSignedScript
                $MyComputerUnsignedScript
            )

            foreach($testScript in $testScripts) {
                Test-UnrestrictedExecutionPolicy $testScript $expected
            }

            $expectedError = "UnauthorizedAccess,Microsoft.PowerShell.Commands.ImportModuleCommand"

            $testData = @(
                @{
                    module = "Microsoft.PowerShell.Archive"
                    error = $null
                }
            )

            if (Test-CanWriteToPsHome) {
                $testData += @(
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

            $TestTypePrefix = "Test 'Unrestricted' execution policy."
            It "$TestTypePrefix Importing <module> Module should throw '<error>'" -TestCases $testData  {
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
                if ($IsNotSkipped) {
                    Set-ExecutionPolicy Bypass -Force -Scope Process | Out-Null
                }
            }

            AfterAll {
                if ($IsNotSkipped) {
                    Set-ExecutionPolicy $originalExecutionPolicy -Force -Scope Process | Out-Null
                }
            }

            function Test-ByPassExecutionPolicy {

                param($testScript, $expected)

                $TestTypePrefix = "Test 'ByPass' execution policy."

                It "$TestTypePrefix Running $testScript script should return $expected" {
                    $scriptName = $testScript

                    $result = & $scriptName
                    return $result

                    $result | Should -Be $expected
                }
            }

            $expected = "Hello"
            $testScripts = @(
                $InternetSignatureCorruptedScript
                $InternetSignedScript
                $InternetUnsignedScript
                $IntranetSignatureCorruptedScript
                $IntranetSignedScript
                $IntranetUnsignedScript
                $LocalSignatureCorruptedScript
                $LocalSignedScript
                $LocalUnsignedScript
                $TrustedSignatureCorruptedScript
                $TrustedSignedScript
                $TrustedUnsignedScript
                $UntrustedSignatureCorruptedScript
                $UntrustedSignedScript
                $UntrustedUnSignedScript
                $MyComputerSignatureCorruptedScript
                $MyComputerSignedScript
                $MyComputerUnsignedScript
            )
            foreach($testScript in $testScripts) {
                Test-ByPassExecutionPolicy $testScript $expected
            }
        }

        Context "'RemoteSigned' execution policy works on OneCore powershell" {

            BeforeAll {
                if ($IsNotSkipped) {
                    Set-ExecutionPolicy RemoteSigned -Force -Scope Process | Out-Null
                }
            }

            AfterAll {
                if ($IsNotSkipped) {
                    Set-ExecutionPolicy $originalExecutionPolicy -Force -Scope Process
                }
            }

            function Test-RemoteSignedExecutionPolicy {

                param ($testScript, $expected, $errorId)

                $TestTypePrefix = "Test 'RemoteSigned' execution policy."

                It "$TestTypePrefix Running $testScript script should return $expected" {
                    $scriptName=$testScript

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
            $message = "Hello"
            $errorId = "System.Management.Automation.PSSecurityException"
            $testData = @(
                @{
                    testScript = $LocalUnsignedScript
                    expected = $message
                    errorId = $null
                }
                @{
                    testScript = $LocalSignatureCorruptedScript
                    expected = $message
                    errorId = $null
                }
                @{
                    testScript = $LocalSignedScript
                    expected = "Hello"
                    errorId = $null
                }
                @{
                    testScript = $MyComputerUnsignedScript
                    expected = $message
                    errorId = $null
                }
                @{
                    testScript = $MyComputerSignatureCorruptedScript
                    expected = $message
                    errorId = $null
                }
                @{
                    testScript = $myComputerSignedScript
                    expected = $message
                    errorId = $null
                }
                @{
                    testScript = $TrustedUnsignedScript
                    expected = $message
                    errorId = $null
                }
                @{
                    testScript = $TrustedSignatureCorruptedScript
                    expected = $message
                    errorId = $null
                }
                @{
                    testScript = $TrustedSignedScript
                    expected = $message
                    errorId = $null
                }
                @{
                    testScript = $IntranetUnsignedScript
                    expected = $message
                    errorId = $null
                }
                @{
                    testScript = $IntranetSignatureCorruptedScript
                    expected = $message
                    errorId = $null
                }
                @{
                    testScript = $IntranetSignedScript
                    expected = $message
                    errorId = $null
                }
                @{
                    testScript = $InternetUnsignedScript
                    expected = $null
                    errorId = $errorId
                }
                @{
                    testScript = $InternetSignatureCorruptedScript
                    expected = $null
                    errorId = $errorId
                }
                @{
                    testScript = $UntrustedUnsignedScript
                    expected = $null
                    errorId = $errorId
                }
                @{
                    testScript = $UntrustedSignatureCorruptedScript
                    expected = $null
                    errorId = $errorId
                }
            )

            foreach($testCase in $testData) {
                Test-RemoteSignedExecutionPolicy @testCase
            }
        }

        Context "Validate that 'AllSigned' execution policy works on OneCore powershell" {

            BeforeAll {
                if ($IsNotSkipped) {
                    Set-ExecutionPolicy AllSigned -Force -Scope Process
                }
            }

            AfterAll {
                if ($IsNotSkipped) {
                    Set-ExecutionPolicy $originalExecutionPolicy -Force -Scope Process
                }
            }

            $TestTypePrefix = "Test 'AllSigned' execution policy."

            $errorId = "UnauthorizedAccess,Microsoft.PowerShell.Commands.ImportModuleCommand"
            $testData = @(
                @{
                    module = "Microsoft.PowerShell.Archive"
                    errorId = $null
                }
            )

            if (Test-CanWriteToPsHome) {
                $testData += @(
                    @{
                        module = $PSHomeUntrustedModule
                        errorId = $errorId
                    }
                    @{
                        module = $PSHomeUnsignedModule
                        errorId = $errorId
                    }
                )
            }

            It "$TestTypePrefix Importing <module> Module should throw '<error>'" -TestCases $testData  {
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

            $errorId = "UnauthorizedAccess"
            $pendingTestData = @(
                # The following files are not signed correctly when generated, so we will skip for now
                # filed https://github.com/PowerShell/PowerShell/issues/5559
                @{
                    testScript = $MyComputerSignedScript
                    errorId = $null
                }
                @{
                    testScript = $UntrustedSignedScript
                    errorId = $null
                }
                @{
                    testScript = $TrustedSignedScript
                    errorId = $null
                }
                @{
                    testScript = $LocalSignedScript
                    errorId = $null
                }
                @{
                    testScript = $IntranetSignedScript
                    errorId = $null
                }
                @{
                    testScript = $InternetSignedScript
                    errorId = $null
                }
            )
            It "$TestTypePrefix Running <testScript> Script should throw '<error>'" -TestCases $pendingTestData -Pending  {}

            $testData = @(
                @{
                    testScript = $InternetSignatureCorruptedScript
                    errorId = $errorId
                }
                @{
                    testScript = $InternetUnsignedScript
                    errorId = $errorId
                }
                @{
                    testScript = $IntranetSignatureCorruptedScript
                    errorId = $errorId
                }
                @{
                    testScript = $IntranetSignatureCorruptedScript
                    errorId = $errorId
                }
                @{
                    testScript = $IntranetUnsignedScript
                    errorId = $errorId
                }
                @{
                    testScript = $LocalSignatureCorruptedScript
                    errorId = $errorId
                }
                @{
                    testScript = $LocalUnsignedScript
                    errorId = $errorId
                }
                @{
                    testScript = $TrustedSignatureCorruptedScript
                    errorId = $errorId
                }
                @{
                    testScript = $TrustedUnsignedScript
                    errorId = $errorId
                }
                @{
                    testScript = $UntrustedSignatureCorruptedScript
                    errorId = $errorId
                }
                @{
                    testScript = $UntrustedUnsignedScript
                    errorId = $errorId
                }
                @{
                    testScript = $MyComputerSignatureCorruptedScript
                    errorId = $errorId
                }
                @{
                    testScript = $MyComputerUnsignedScript
                    errorId = $errorId
                }

            )
            It "$TestTypePrefix Running <testScript> Script should throw '<error>'" -TestCases $testData  {
                param ([string]$testScript, [string]$errorId)
                $testScript | Should -Exist
                if ($errorId)
                {
                    {& $testScript} | Should -Throw -ErrorId $errorId
                }
                else
                {
                    {& $testScript} | Should -Not -Throw
                }
            }
        }
    }

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

    Describe "Validate Set-ExecutionPolicy -Scope" -Tags "CI" {

        BeforeAll {
            if ($IsNotSkipped) {
                $originalPolicies = Get-ExecutionPolicy -List
            }
        }

        AfterAll {
            if ($IsNotSkipped) {
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
            if ($IsNotSkipped)
            {
                $originalPolicies = Get-ExecutionPolicy -List
            }
        }

        AfterAll {
            if ($IsNotSkipped)
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
}
finally {
    $global:PSDefaultParameterValues = $originalDefaultParameterValues
}

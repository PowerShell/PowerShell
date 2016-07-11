# This is a Pester test suite to validate the Security cmdlets in PowerShell.
#
# Copyright (c) Microsoft Corporation, 2015
#
if ( ! $IsWindows ) {
    Describe "ExecutionPolicy cmdlet valiation" {
        It -skip "ExecutionPolicy cmdlets work" {
            $true | should be $true
        }
    }
    return
}

$currentDirectory = Split-Path $MyInvocation.MyCommand.Path
Describe "Validate ExecutionPolicy cmdlets in PowerShell" -Tags "Innerloop", "BVT" {

    BeforeAll {

        function createTestFile
        {
            param (
            [Parameter (Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [string]
            $filePath,

            [Parameter( Mandatory= $true)]
            [ValidateSet(-1,0,1,2,3,4)]
            [int]
            $FileType,

            [Parameter()]
            [switch]
            $AddSignature,

            [Parameter()]
            [switch]
            $Corrupted
            )
        
            if (Test-Path $filePath)
            {
                Remove-Item $filePath -Force
            }
            
            $null = New-Item -Path $filePath -ItemType File

            $content = "`"Hello`"" + "`n `n" 
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
        
            set-content $filePath -Value $content

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
                Add-Content -Path $filePath -Value $alternateStreamContent -stream Zone.Identifier
            }
        }

        $testDirectory = Join-Path $currentDirectory "Security_TestData"
        if(Test-Path $testDirectory)
        {
            Remove-Item -Path $testDirectory -Recurse -Force -ErrorAction Stop
        }
        $null = New-Item -Path $testDirectory -ItemType Directory -ErrorAction Stop
        #Generate test data

        $fileType = @{
            "Local" = -1
            "MyComputer" = 0
            "Intranet" = 1
            "Trusted" = 2
            "Internet" = 3
            "Untrusted" = 4
        }

        $InternetSignatureCorruptedScript = Join-Path -Path $testDirectory -ChildPath InternetSignatureCorruptedScript.ps1
        createTestFile -filePath $internetSignatureCorruptedScript -FileType $fileType.Internet -AddSignature -Corrupted
        
        $InternetSignedScript = Join-Path -Path $testDirectory -ChildPath InternetSignedScript.ps1
        createTestFile -filePath $internetSignedScript -FileType $fileType.Internet -AddSignature
        
        $InternetUnsignedScript = Join-Path -Path $testDirectory -ChildPath InternetUnsignedScript.ps1
        createTestFile -filePath $internetUnsignedScript -FileType $fileType.Internet

        $IntranetSignatureCorruptedScript = Join-Path -Path $testDirectory -ChildPath IntranetSignatureCorruptedScript.ps1
        createTestFile -filePath $intranetSignatureCorruptedScript -FileType $fileType.Intranet -AddSignature -Corrupted
        
        $IntranetSignedScript = Join-Path -Path $testDirectory -ChildPath IntranetSignedScript.ps1
        createTestFile -filePath $intranetSignedScript -FileType $fileType.Intranet -AddSignature
        
        $IntranetUnsignedScript = Join-Path -Path $testDirectory -ChildPath IntranetUnsignedScript.ps1
        createTestFile -filePath $intranetUnsignedScript -FileType $fileType.Intranet

        $LocalSignatureCorruptedScript = Join-Path -Path $testDirectory -ChildPath LocalSignatureCorruptedScript.ps1
        createTestFile -filePath $localSignatureCorruptedScript -FileType $fileType.Local -AddSignature -Corrupted
        
        $LocalSignedScript = Join-Path -Path $testDirectory -ChildPath LocalSignedScript.ps1
        createTestFile -filePath $localSignedScript -FileType $fileType.Local -AddSignature
        
        $LocalUnsignedScript = Join-Path -Path $testDirectory -ChildPath LocalUnsignedScript.ps1
        createTestFile -filePath $localUnsignedScript -FileType $fileType.Local

        $TrustedSignatureCorruptedScript = Join-Path -Path $testDirectory -ChildPath TrustedSignatureCorruptedScript.ps1
        createTestFile -filePath $trustedSignatureCorruptedScript -FileType $fileType.Trusted -AddSignature -Corrupted
        
        $TrustedSignedScript = Join-Path -Path $testDirectory -ChildPath trustedSignedScript.ps1
        createTestFile -filePath $trustedSignedScript -FileType $fileType.Trusted -AddSignature
        
        $TrustedUnsignedScript = Join-Path -Path $testDirectory -ChildPath TrustedUnsignedScript.ps1
        createTestFile -filePath $trustedUnsignedScript -FileType $fileType.Trusted

        $UntrustedSignatureCorruptedScript = Join-Path -Path $testDirectory -ChildPath UntrustedSignatureCorruptedScript.ps1
        createTestFile -filePath $untrustedSignatureCorruptedScript -FileType $fileType.Untrusted -AddSignature -Corrupted
        
        $UntrustedSignedScript = Join-Path -Path $testDirectory -ChildPath UntrustedSignedScript.ps1
        createTestFile -filePath $UntrustedSignedScript -FileType $fileType.Untrusted -AddSignature
        
        $UntrustedUnsignedScript = Join-Path -Path $testDirectory -ChildPath UntrustedUnsignedScript.ps1
        createTestFile -filePath $untrustedUnsignedScript -FileType $fileType.Untrusted

        $MyComputerSignatureCorruptedScript = Join-Path -Path $testDirectory -ChildPath MyComputerSignatureCorruptedScript.ps1
        createTestFile -filePath $myComputerSignatureCorruptedScript -FileType $fileType.MyComputer -AddSignature -Corrupted
        
        $MyComputerSignedScript = Join-Path -Path $testDirectory -ChildPath MyComputerSignedScript.ps1
        createTestFile -filePath $myComputerSignedScript -FileType $fileType.MyComputer -AddSignature
        
        $MyComputerUnsignedScript = Join-Path -Path $testDirectory -ChildPath MyComputerUnsignedScript.ps1
        createTestFile -filePath $myComputerUnsignedScript -FileType $fileType.MyComputer

        $originalExecutionPolicy = Get-ExecutionPolicy
    }

    AfterAll {
    
        #Remove Test data
        Remove-Item -Path $testDirectory -Recurse -Force -ErrorAction SilentlyContinue
    }

    Context "Validate that 'Restricted' execution policy works as expected" {

        BeforeAll {
            Set-ExecutionPolicy Restricted -Force
        }

        AfterAll {
            Set-ExecutionPolicy $originalExecutionPolicy -Force
        }

        function Test-RestrictedExecutionPolicy
        {
            param ($testScript)

            $TestTypePrefix = "Test 'Restricted' execution policy."

            It "$TestTypePrefix Running $testScript script should raise PSSecurityException" {

                $exception = $null
                try {
                    & $testScript
                }
                catch
                {
                    $exception = $_
                }
                $exception.Exception.getType() |  Should be "System.Management.Automation.PSSecurityException"
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

    Context "Validate that 'Unrestricted' execution policy works as expected" {

        BeforeAll {
            Set-ExecutionPolicy Unrestricted -Force
        }

        AfterAll {
            Set-ExecutionPolicy $originalExecutionPolicy -Force
        }

        function Test-UnrestrictedExecutionPolicy {

            param($testScript, $expected)

            $TestTypePrefix = "Test 'Unrestricted' execution policy."

            It "$TestTypePrefix Running $testScript script should return $expected" {
                $result = & $testScript
                $result |  Should be $expected
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
    }

    Context "Validate that 'ByPass' execution policy works as expected" {

        BeforeAll {
            Set-ExecutionPolicy Bypass -Force
        }

        AfterAll {
            Set-ExecutionPolicy $originalExecutionPolicy -Force
        }

        function Test-ByPassExecutionPolicy {

            param($testScript, $expected)

            $TestTypePrefix = "Test 'ByPass' execution policy."

            It "$TestTypePrefix Running $testScript script should return $expected" {
                $result = & $testScript
                $result |  Should be $expected
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

    Context "'RemoteSigned' execution policy works as expected" {

        BeforeAll {
            Set-ExecutionPolicy RemoteSigned -Force
        }

        AfterAll {
            Set-ExecutionPolicy $originalExecutionPolicy -Force
        }

        function Test-RemoteSignedExecutionPolicy {

            param($testScript, $expected, $error)

            $TestTypePrefix = "Test 'RemoteSigned' execution policy."

            It "$TestTypePrefix Running $testScript script should return $expected" {

                $a = Get-ExecutionPolicy

                $scriptResult = $null
                $exception = $null

                try
                {
                    $scriptResult = & $testScript
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
                    
                $scriptResult |  Should be $expected
                $errorType | Should be $error
            }
        }

        $testData = @(
        @{
            testScript = $LocalUnsignedScript
            expected = "Hello"
            error = $null
        }
        @{
            testScript = $LocalSignatureCorruptedScript
            expected = "Hello"
            error = $null
        }
        @{
            testScript = $LocalSignedScript
            expected = "Hello"
            error = $null
        }
        @{
            testScript = $MyComputerUnsignedScript
            expected = "Hello"
            error = $null
        }
        @{
            testScript = $MyComputerSignatureCorruptedScript
            expected = "Hello"
            error = $null
        }
        @{
            testScript = $myComputerSignedScript
            expected = "Hello"
            error = $null
        }
        @{
            testScript = $TrustedUnsignedScript
            expected = "Hello"
            error = $null
        }
        @{
            testScript = $TrustedSignatureCorruptedScript
            expected = "Hello"
            error = $null
        }
        @{
            testScript = $TrustedSignedScript
            expected = "Hello"
            error = $null
        }
        @{
            testScript = $IntranetUnsignedScript
            expected = "Hello"
            error = $null
        }
        @{
            testScript = $IntranetSignatureCorruptedScript
            expected = "Hello"
            error = $null
        }
        @{
            testScript = $IntranetSignedScript
            expected = "Hello"
            error = $null
        }
        @{
            testScript = $InternetUnsignedScript
            expected = $null
            error = "System.Management.Automation.PSSecurityException"
        }
        @{
            testScript = $InternetSignatureCorruptedScript
            expected = $null
            error = "System.Management.Automation.PSSecurityException"
        }
        @{
            testScript = $UntrustedUnsignedScript
            expected = $null
            error = "System.Management.Automation.PSSecurityException"
        }
        @{
            testScript = $UntrustedSignatureCorruptedScript
            expected = $null
            error = "System.Management.Automation.PSSecurityException"
        }
        )

        foreach($testCase in $testData) {
            Test-RemoteSignedExecutionPolicy @testCase
        }
    }

    Context "Validate that 'AllSigned' execution policy works as expected" {

        BeforeAll {
            Set-ExecutionPolicy AllSigned -Force
        }

        AfterAll {
            Set-ExecutionPolicy $originalExecutionPolicy -Force
        }

        function Test-AllSignedExecutionPolicy {

            param($testScript, $error)

            $TestTypePrefix = "Test 'AllSigned' execution policy."

            It "$TestTypePrefix Running $testScript script should return $error" {
                $exception = $null
                try
                {
                    & $testScript
                }
                catch
                {
                    $exception = $_
                }
                $errorType = $null

                if($null -ne $exception)
                {
                    $errorType = $exception.exception.getType()
                }
                $errorType | Should be $error
            }
        }

        $testData = @(
        @{
            testScript = $LocalUnsignedScript
            expected = $null
            error = "System.Management.Automation.PSSecurityException"
        }
        @{
            testScript = $LocalSignatureCorruptedScript
            expected = $null
            error = "System.Management.Automation.PSSecurityException"
        }
        @{
            testScript = $MyComputerUnsignedScript
            expected = $null
            error = "System.Management.Automation.PSSecurityException"
        }
        @{
            testScript = $MyComputerSignatureCorruptedScript
            expected = $null
            error = "System.Management.Automation.PSSecurityException"
        }
        @{
            testScript = $TrustedUnsignedScript
            expected = $null
            error = "System.Management.Automation.PSSecurityException"
        }
        @{
            testScript = $TrustedSignatureCorruptedScript
            expected = $null
            error = "System.Management.Automation.PSSecurityException"
        }
        @{
            testScript = $IntranetUnsignedScript
            expected = $null
            error = "System.Management.Automation.PSSecurityException"
        }
        @{
            testScript = $IntranetSignatureCorruptedScript
            expected = $null
            error = "System.Management.Automation.PSSecurityException"
        }
        @{
            testScript = $InternetUnsignedScript
            expected = $null
            error = "System.Management.Automation.PSSecurityException"
        }
        @{
            testScript = $InternetSignatureCorruptedScript
            expected = $null
            error = "System.Management.Automation.PSSecurityException"
        }
        @{
            testScript = $UntrustedUnsignedScript
            expected = $null
            error = "System.Management.Automation.PSSecurityException"
        }
        @{
            testScript = $UntrustedSignatureCorruptedScript
            expected = $null
            error = "System.Management.Automation.PSSecurityException"
        }
        )
        foreach($testScript in $testScripts) {
            Test-AllSignedExecutionPolicy $testScript $error
        }
    }
}

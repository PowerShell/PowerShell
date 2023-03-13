# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Import-Module (Join-Path -Path $PSScriptRoot 'certificateCommon.psm1') -Force

Describe "CmsMessage cmdlets and Get-PfxCertificate basic tests" -Tags "CI" {

    BeforeAll {
        $certLocation = New-GoodCertificate
        $certLocation | Should -Not -BeNullOrEmpty | Out-Null

        $protectedCertLocation = New-ProtectedCertificate
        $protectedCertLocation | Should -Not -BeNullOrEmpty | Out-Null
    }

    It "Verify Get-PfxCertificate -FilePath" {
        $cert = Get-PfxCertificate -FilePath $certLocation
        $cert.Subject | Should -Be "CN=MyDataEnciphermentCert"
    }

    It "Verify Get-PfxCertificate -LiteralPath" {
        $cert = Get-PfxCertificate -LiteralPath $certLocation
        $cert.Subject | Should -Be "CN=MyDataEnciphermentCert"
    }

    It "Verify Get-PfxCertificate positional argument" {
        $cert = Get-PfxCertificate $certLocation
        $cert.Subject | Should -Be "CN=MyDataEnciphermentCert"
    }

    It "Verify Get-PfxCertificate right password" {
        $password = Get-CertificatePassword
        $cert = Get-PfxCertificate $protectedCertLocation -Password $password
        $cert.Subject | Should -Be "CN=localhost"
    }

    It "Verify Get-PfxCertificate wrong password" {
        #[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Test secret.")]
        $pass = ConvertTo-SecureString "wrongpass" -AsPlainText -Force
        { Get-PfxCertificate $protectedCertLocation -Password $pass -ErrorAction Stop } |
            Should -Throw -ErrorId "GetPfxCertificateUnknownCryptoError,Microsoft.PowerShell.Commands.GetPfxCertificateCommand"
    }

    It "Verify CMS message recipient resolution by path" -Skip:(!$IsWindows) {
        $errors = $null
        $recipient = [System.Management.Automation.CmsMessageRecipient] $certLocation
        $recipient.Resolve($ExecutionContext.SessionState, "Encryption", [ref] $errors)

        $recipient.Certificates.Count | Should -Be 1
        $recipient.Certificates[0].Subject | Should -Match 'CN=MyDataEnciphermentCert'
    }

    It "Verify CMS message recipient resolution by cert" -Skip:(!$IsWindows) {
        $errors = $null
        $cert = Get-PfxCertificate $certLocation
        $recipient = [System.Management.Automation.CmsMessageRecipient] $cert
        $recipient.Resolve($ExecutionContext.SessionState, "Encryption", [ref] $errors)

        $recipient.Certificates.Count | Should -Be 1
        $recipient.Certificates[0].Subject | Should -Match 'CN=MyDataEnciphermentCert'
    }

    It "Verify a CMS message can be protected / unprotected" -Skip:(!$IsWindows) {
        $protected = "Hello World","How are you?" | Protect-CmsMessage -To $certLocation
        $protected.IndexOf("-----BEGIN CMS-----") | Should -Be 0

        $message = $protected | Get-CmsMessage
        $message.Recipients.Count | Should -Be 1
        $message.Recipients[0].IssuerName | Should -Be "CN=MyDataEnciphermentCert"

        $expected = "Hello World" + [System.Environment]::NewLine + "How are you?"
        $decrypted = $message | Unprotect-CmsMessage -To $certLocation
        $decrypted | Should -Be $expected

        $decrypted = $protected | Unprotect-CmsMessage -To $certLocation
        $decrypted | Should -Be $expected
    }
}

Describe "CmsMessage cmdlets thorough tests" -Tags "Feature" {

    BeforeAll{
        if($IsWindows)
        {
            if (-not (Install-TestCertificates) ) {
                $SetupFailure = $true
            } else {
                Push-Location Cert:\
                $SetupFailure = $false
            }
        }
        else
        {
            # Skip for non-Windows platforms
            $defaultParamValues = $PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues = @{ "it:skip" = $true }
        }
    }

    AfterAll {
        if($IsWindows -and -not $SetupFailure)
        {
            Remove-TestCertificates
        }
        else
        {
            if ($defaultParamValues -ne $null) {
                $global:PSDefaultParameterValues = $defaultParamValues
            }

        }
    }

    It "Verify message recipient resolution by Base64Cert" {
        $certContent = "
            -----BEGIN CERTIFICATE-----
            MIIDXTCCAkWgAwIBAgIQRTsRwsx0LZBHrx9z5Dag2zANBgkqhkiG9w0BAQUFADAh
            MR8wHQYDVQQDDBZNeURhdGFFbmNpcGhlcm1lbnRDZXJ0MCAXDTE0MDcyNTIyMjkz
            OVoYDzMwMTQwNzI1MjIzOTM5WjAhMR8wHQYDVQQDDBZNeURhdGFFbmNpcGhlcm1l
            bnRDZXJ0MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAx3SuShUvnRqn
            tYOIouJdP3wPZ5rtDi2KYPurpngGNZjM0EGDTrnhmEAI8DL4Kp6n/zz1mYVoX73+
            6uCpZX/13VDXg1neebJ261XpBX6FzxtclIQr8ywdUtrEgCnUAhgqgvO1Wwm4ogNR
            tWGCGkmlnqyaoV1j/V4KSn4WvKqSUIOZm0umGCTtNAJ6VtdpYO+uxxnRAapPUCY+
            qQ7DFzTUECIo1lMlBcuMiXj6NSFr4/D7ltkZ27jCdsZmzI7ZvRnDlfSYTPQnAO/E
            0uYn9uyKY/xfngWkUX/pe+j+10Lm1ypbASrj2Ezgf0KeZRXBwqKUOLhKheEmBJ18
            rLV27qwHeQIDAQABo4GOMIGLMA4GA1UdDwEB/wQEAwIEMDAUBgNVHSUEDTALBgkr
            BgEEAYI3UAEwRAYJKoZIhvcNAQkPBDcwNTAOBggqhkiG9w0DAgICAIAwDgYIKoZI
            hvcNAwQCAgCAMAcGBSsOAwIHMAoGCCqGSIb3DQMHMB0GA1UdDgQWBBRIyIzwInLJ
            3B+FajVUFMACf1hrxjANBgkqhkiG9w0BAQUFAAOCAQEAfFt4rmUmWfCbbwi2mCrZ
            Osq0lfVNUiZ+iLlEKga4VAI3sJZRtErnVM70eXUt7XpRaOdIfxjuXFpsgc37KyLi
            ByCORLuRC0itZVs3aba48opfMDXivxBy0ngqCPPLQsyaN9K7WnpvYV1QxiudYwwU
            8U5rFmzlwNLvc3XiyoGWaVZluk2DIJawQ5QYAU9/NMBBCbPHjTG7k0l4cpcEC+Ex
            od3RlO6/MOYuK2WB4VTxKsV80EdA3ljlu7Td8P4movnrbB4rG4wpCpk05eREkg/5
            Y54Ilo9m5OSAWtdx4yfS779eebLgUs3P+dk6EKwovXMokVveZA8cenIp3QkqSpeT
            cQ==
            -----END CERTIFICATE-----
            "

            $errors = $null
            $recipient = [System.Management.Automation.CmsMessageRecipient] $certContent
            $recipient.Resolve($ExecutionContext.SessionState, "Encryption", [ref] $errors)

            $recipient.Certificates.Count | Should -Be 1
            $recipient.Certificates[0].Subject | Should -Match 'CN=MyDataEnciphermentCert'
    }

    It "Verify wildcarded recipient resolution by path [Decryption]" {
        if ($SetupFailure) { Set-ItResult -Inconclusive -Because "Test certificates are not installed"}
        $errors = $null
        $recipient = [System.Management.Automation.CmsMessageRecipient] ((Get-GoodCertificateLocation) + "*")
        $recipient.Resolve($ExecutionContext.SessionState, "Decryption", [ref] $errors)

        # Should have resolved single cert
        $recipient.Certificates.Count | Should -Be 1
    }

    It "Verify wildcarded recipient resolution by path [Encryption]" {
        if ($SetupFailure) { Set-ItResult -Inconclusive -Because "Test certificates are not installed"}
        $errors = $null
        $recipient = [System.Management.Automation.CmsMessageRecipient] ((Get-GoodCertificateLocation) + "*")
        $recipient.Resolve($ExecutionContext.SessionState, "Encryption", [ref] $errors)

        $recipient.Certificates.Count | Should -Be 1
    }

    It "Verify resolution by directory" {
        if ($SetupFailure) { Set-ItResult -Inconclusive -Because "Test certificates are not installed"}
        $protectedEventLoggingCertPath = Join-Path $TestDrive ProtectedEventLoggingDir
        $null = New-Item -ItemType Directory $protectedEventLoggingCertPath -Force
        Copy-Item (Get-GoodCertificateLocation) $protectedEventLoggingCertPath
        Copy-Item (Get-GoodCertificateLocation) (Join-Path $protectedEventLoggingCertPath "SecondCert.pfx")
        Copy-Item (Get-GoodCertificateLocation) (Join-Path $protectedEventLoggingCertPath "ThirdCert.pfx")

        $errors = $null
        $recipient = [System.Management.Automation.CmsMessageRecipient] $protectedEventLoggingCertPath
        $recipient.Resolve($ExecutionContext.SessionState, "Decryption", [ref] $errors)

        $recipient.Certificates.Count | Should -Be 1
    }

    It "Verify resolution by thumbprint" {
        if ($SetupFailure) { Set-ItResult -Inconclusive -Because "Test certificates are not installed"}
        $errors = $null
        $recipient = [System.Management.Automation.CmsMessageRecipient] (Get-GoodCertificateObject).Thumbprint
        $recipient.Resolve($ExecutionContext.SessionState, "Decryption", [ref] $errors)

        # "Should have certs from thumbprint in 'My' store"
        $recipient.Certificates.Count | Should -Be 1
        $recipient.Certificates[0].Thumbprint | Should -Be (Get-GoodCertificateObject).Thumbprint
    }

    It "Verify resolution by subject name" {
        if ($SetupFailure) { Set-ItResult -Inconclusive -Because "Test certificates are not installed"}
        $errors = $null
        $recipient = [System.Management.Automation.CmsMessageRecipient] (Get-GoodCertificateObject).Subject
        $recipient.Resolve($ExecutionContext.SessionState, "Decryption", [ref] $errors)

        $recipient.Certificates.Count | Should -Be 1
        $recipient.Certificates[0].Thumbprint | Should -Be (Get-GoodCertificateObject).Thumbprint
    }

    It "Verify error when no cert found in encryption for encryption" {
        if ($SetupFailure) { Set-ItResult -Inconclusive -Because "Test certificates are not installed"}
        $errors = $null
        $recipient = [System.Management.Automation.CmsMessageRecipient] "SomeCertificateThatDoesNotExist*"
        $recipient.Resolve($ExecutionContext.SessionState, "Encryption", [ref] $errors)

        $errors.Count | Should -Be 1
        $errors[0].FullyQualifiedErrorId | Should -Be "NoCertificateFound"
    }

    It "Verify error when encrypting to non-wildcarded identifier for decryption" {
        if ($SetupFailure) { Set-ItResult -Inconclusive -Because "Test certificates are not installed"}
        $errors = $null
        $recipient = [System.Management.Automation.CmsMessageRecipient] "SomeCertificateThatDoesNotExist"
        $recipient.Resolve($ExecutionContext.SessionState, "Decryption", [ref] $errors)

        $errors.Count | Should -Be 1
        $errors[0].FullyQualifiedErrorId | Should -Be "NoCertificateFound"
    }

    It "Verify error when encrypting to wrong cert" {
        if ($SetupFailure) { Set-ItResult -Inconclusive -Because "Test certificates are not installed"}
        $errors = $null
        $recipient = [System.Management.Automation.CmsMessageRecipient] (Get-BadCertificateObject).Thumbprint
        $recipient.Resolve($ExecutionContext.SessionState, "Encryption", [ref] $errors)

        $errors.Count | Should -Be 1
        $errors[0].FullyQualifiedErrorId | Should -Be "CertificateCannotBeUsedForEncryption"
    }

    It "Verify no error when encrypting to wildcarded identifier for decryption" {
        if ($SetupFailure) { Set-ItResult -Inconclusive -Because "Test certificates are not installed"}
        $errors = $null
        $recipient = [System.Management.Automation.CmsMessageRecipient] "SomeCertificateThatDoesNotExist*"
        $recipient.Resolve($ExecutionContext.SessionState, "Decryption", [ref] $errors)

        $errors | Should -Be $null
        $recipient.Certificates.Count | Should -Be 0
    }

    It "Verify Protect-CmsMessage emits recipient errors" {
        if ($SetupFailure) { Set-ItResult -Inconclusive -Because "Test certificates are not installed"}
        { "Hello World" | Protect-CmsMessage -To "SomeThumbprintThatDoesNotExist" -ErrorAction Stop } |
            Should -Throw -ErrorId "NoCertificateFound,Microsoft.PowerShell.Commands.ProtectCmsMessageCommand"
    }

    It "Verify CmsMessage cmdlets works with paths" {
        if ($SetupFailure) { Set-ItResult -Inconclusive -Because "Test certificates are not installed"}

        try {
            $randomNum = Get-Random -Minimum 1000 -Maximum 9999
            $tempPath = Join-Path $TestDrive "$randomNum-Path-Test-File"
            $encryptedPath = $tempPath + ".encrypted.txt"
            "Hello World","How are you?" | Set-Content $tempPath

            Protect-CmsMessage -Path $tempPath -To (Get-GoodCertificateLocation) -OutFile $encryptedPath

            $message = Get-CmsMessage -LiteralPath $encryptedPath
            $message.Recipients.Count | Should -Be 1
            $message.Recipients[0].IssuerName | Should -Be "CN=MyDataEnciphermentCert"

            $expected = "Hello World" + [System.Environment]::NewLine + "How are you?" + [System.Environment]::NewLine
            $decrypted = $message | Unprotect-CmsMessage -To (Get-GoodCertificateLocation)
            $decrypted | Should -Be $expected

            $decrypted = Unprotect-CmsMessage -Path $encryptedPath -To (Get-GoodCertificateLocation)
            $decrypted | Should -Be $expected
        } finally {
            Remove-Item $tempPath, $encryptedPath -Force -ErrorAction SilentlyContinue
        }
    }

    It "Verify Unprotect-CmsMessage works with local store" {
        if ($SetupFailure) { Set-ItResult -Inconclusive -Because "Test certificates are not installed"}

        try {
            $randomNum = Get-Random -Minimum 1000 -Maximum 9999
            $tempPath = Join-Path $TestDrive "$randomNum-Path-Test-File"
            "Hello World" | Protect-CmsMessage -To (Get-GoodCertificateLocation) -OutFile $tempPath

            # Decrypt using $importedCert in the Cert store
            $decrypted = Unprotect-CmsMessage -Path $tempPath
            $decrypted | Should -Be "Hello World"
        } finally {
            Remove-Item $tempPath -Force -ErrorAction SilentlyContinue
        }
    }

    It "Verify Unprotect-CmsMessage emits recipient errors" {
        if ($SetupFailure) { Set-ItResult -Inconclusive -Because "Test certificates are not installed"}

        { "" | Unprotect-CmsMessage -To "SomeThumbprintThatDoesNotExist" -IncludeContext -ErrorAction Stop } |
            Should -Throw -ErrorId "NoCertificateFound,Microsoft.PowerShell.Commands.UnprotectCmsMessageCommand"
    }

    It "Verify failure to extract Ascii armor generates an error [Unprotect-CmsMessage]" {
        if ($SetupFailure) { Set-ItResult -Inconclusive -Because "Test certificates are not installed"}

        { "Hello World" | Unprotect-CmsMessage -ErrorAction Stop } |
            Should -Throw -ErrorId "InputContainedNoEncryptedContentIncludeContext,Microsoft.PowerShell.Commands.UnprotectCmsMessageCommand"
    }

    It "Verify failure to extract Ascii armor generates an error [Get-CmsMessage]" {
        if ($SetupFailure) { Set-ItResult -Inconclusive -Because "Test certificates are not installed"}

        { "Hello World" | Get-CmsMessage -ErrorAction Stop } |
            Should -Throw -ErrorId "InputContainedNoEncryptedContent,Microsoft.PowerShell.Commands.GetCmsMessageCommand"
    }

    It "Verify 'Unprotect-CmsMessage -IncludeContext' with no encrypted input" {
        if ($SetupFailure) { Set-ItResult -Inconclusive -Because "Test certificates are not installed"}

        # Should have round-tripped content
        $result = "Hello World" | Unprotect-CmsMessage -IncludeContext
        $result | Should -Be "Hello World"
    }

    It "Verify Unprotect-CmsMessage lets you include context" {
        if ($SetupFailure) { Set-ItResult -Inconclusive -Because "Test certificates are not installed"}

        $protected = "Hello World" | Protect-CmsMessage -To (Get-GoodCertificateLocation)
        $adjustedProtected = "Pre content" + [System.Environment]::NewLine + $protected + [System.Environment]::NewLine + "Post content"

        $decryptedNoContext = $adjustedProtected | Unprotect-CmsMessage -To (Get-GoodCertificateLocation)
        $decryptedWithContext = $adjustedProtected | Unprotect-CmsMessage -To (Get-GoodCertificateLocation) -IncludeContext

        $decryptedNoContext | Should -Be "Hello World"

        $expected = "Pre content" + [System.Environment]::NewLine + "Hello World" + [System.Environment]::NewLine + "Post content"
        $decryptedWithContext | Should -Be $expected
    }

    It "Verify Unprotect-CmsMessage treats event logs as a first class citizen" {
        if ($SetupFailure) { Set-ItResult -Inconclusive -Because "Test certificates are not installed"}

        $protected = "Encrypted Message1","Encrypted Message2" | Protect-CmsMessage -To (Get-GoodCertificateLocation)
        $virtualEventLog = Get-WinEvent Microsoft-Windows-PowerShell/Operational -MaxEvents 1
        $savedId = $virtualEventLog.Id
        $virtualEventLog.Message = $protected

        $expected = "Encrypted Message1" + [System.Environment]::NewLine + "Encrypted Message2"
        $decrypted = $virtualEventLog | Unprotect-CmsMessage -To (Get-GoodCertificateLocation)
        $decrypted | Should -Be $expected

        $processed = $virtualEventLog | Unprotect-CmsMessage -To (Get-GoodCertificateLocation) -IncludeContext
        $processed.Id | Should -Be $savedId
        $processed.Message | Should -Be $expected
    }

    # Pending due to #3847
    It "Verify -DocumentEncryptionCert parameter works" -Pending {
        $foundCerts = Get-ChildItem Cert:\CurrentUser -Recurse -DocumentEncryptionCert

        # Validate they all match the EKU
        $correctMatching = $foundCerts | Where-Object {
            ($_.EnhancedKeyUsageList.Count -gt 0) -and
            ($_.EnhancedKeyUsageList[0].ObjectId -eq '1.3.6.1.4.1.311.80.1')
        }
        # "All Document Encryption Cert should have had correct EKU"
        @($foundCerts).Count | Should -Be @($correctMatching).Count
    }

    It "Verify protect message using OutString" {
        if ($SetupFailure) { Set-ItResult -Inconclusive -Because "Test certificates are not installed"}

        $protected = Get-Process -Id $PID | Protect-CmsMessage -To (Get-GoodCertificateLocation)
        $decrypted = $protected | Unprotect-CmsMessage -To (Get-GoodCertificateLocation)
        # Should have had PID in output
        $decrypted | Should -Match $PID
    }
}

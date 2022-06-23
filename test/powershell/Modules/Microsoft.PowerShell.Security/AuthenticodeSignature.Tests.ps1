# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Get-AuthenticodeSignature detail tests" -Tags "Feature" {
    BeforeAll {
        $testDataPath = "$PSScriptRoot\TestData\SignedTestData"
    }

    Context "PowerShell Scripts - .ps1" {
        It "Gets signature details without timestamp" {
            # Set-AuthenticodeSignature -FilePath foo.ps1 -Certificate $cert -HashAlgorithm SHA256

            $filePath = Join-Path $testDataPath "signed.ps1"
            $actual = Get-AuthenticodeSignature -FilePath $filePath
            $actual.Signatures.Count | Should -Be 1

            $sig = $actual.Signatures[0]
            $sig.Certificate.Subject | Should -Be 'CN=PwshTestCode-SelfSigned'
            $sig.Certificate.Thumbprint | Should -Be 'BF8734E0142FEF69D742E5F46E487EE9B671B530'
            $sig.DigestAlgorithm | Should -Be 'sha256'
            $sig.SignatureAlgorithm | Should -Be 'RSA'
            $sig.SigningTime | Should -BeNullOrEmpty
            $sig.Timestamp | Should -BeNullOrEmpty
        }

        It "Gets signature details with timestamp" {
            # Set-AuthenticodeSignature -FilePath foo.ps1 -Certificate $cert -HashAlgorithm SHA256 -TimestampServer http://timestamp.digicert.com

            $filePath = Join-Path $testDataPath "signed-timestamp.ps1"
            $actual = Get-AuthenticodeSignature -FilePath $filePath
            $actual.Signatures.Count | Should -Be 1

            $sig = $actual.Signatures[0]
            $sig.Certificate.Subject | Should -Be 'CN=PwshTestCode-SelfSigned'
            $sig.Certificate.Thumbprint | Should -Be 'BF8734E0142FEF69D742E5F46E487EE9B671B530'
            $sig.DigestAlgorithm | Should -Be 'sha256'
            $sig.SignatureAlgorithm | Should -Be 'RSA'
            $sig.SigningTime | Should -BeNullOrEmpty

            $ts = $sig.Timestamp
            $ts | Should -Not -BeNullOrEmpty
            $ts.Certificate.Subject | Should -Be 'CN=DigiCert Timestamp 2022 - 2, O="DigiCert, Inc.", C=US'
            $ts.Certificate.Thumbprint | Should -Be '8508F386515CB3D3077DB6B4B7C07F1B4A5E41DE'
            $ts.DigestAlgorithm | Should -Be 'sha256'
            $ts.SignatureAlgorithm | Should -Be 'RSA'
            $ts.SigningTime | Should -Not -BeNullOrEmpty
            $ts.SigningTime.Kind | Should -Be ([System.DateTimeKind]::Utc)
            $ts.SigningTime.ToString('o') | Should -Be '2022-06-23T05:13:36.0000000Z'
            $ts.Timestamp | Should -BeNullOrEmpty
        }
    }

    Context "Executables - .exe" {
        # The exe used here is based on the example in http://www.phreedom.org/research/tinype/
        # It removes the C runtime library to slim it down but other tricks
        # will break the authenticode structures making it unsignable.
        It "Gets signature details without timestamp" {
            # signtool.exe sign /n "PwshTestCode-SelfSigned" /fd SHA256 tiny.exe

            $filePath = Join-Path $testDataPath "tiny-single.exe"
            $actual = Get-AuthenticodeSignature -FilePath $filePath
            $actual.Signatures.Count | Should -Be 1

            $sig = $actual.Signatures[0]
            $sig.Certificate.Subject | Should -Be 'CN=PwshTestCode-SelfSigned'
            $sig.Certificate.Thumbprint | Should -Be 'BF8734E0142FEF69D742E5F46E487EE9B671B530'
            $sig.DigestAlgorithm | Should -Be 'sha256'
            $sig.SignatureAlgorithm | Should -Be 'RSA'
            $sig.SigningTime | Should -BeNullOrEmpty
            $sig.Timestamp | Should -BeNullOrEmpty
        }

        It "Gets signature details with timestamp" {
            # signtool.exe sign /n "PwshTestCode-SelfSigned" /fd SHA256 /t http://timestamp.digicert.com tiny.exe

            $filePath = Join-Path $testDataPath "tiny-single-timestamp.exe"
            $actual = Get-AuthenticodeSignature -FilePath $filePath
            $actual.Signatures.Count | Should -Be 1

            $sig = $actual.Signatures[0]
            $sig.Certificate.Subject | Should -Be 'CN=PwshTestCode-SelfSigned'
            $sig.Certificate.Thumbprint | Should -Be 'BF8734E0142FEF69D742E5F46E487EE9B671B530'
            $sig.DigestAlgorithm | Should -Be 'sha256'
            $sig.SignatureAlgorithm | Should -Be 'RSA'
            $sig.SigningTime | Should -BeNullOrEmpty

            $ts = $sig.Timestamp
            $ts | Should -Not -BeNullOrEmpty
            $ts.Certificate.Subject | Should -Be 'CN=DigiCert Timestamp 2022 - 2, O="DigiCert, Inc.", C=US'
            $ts.Certificate.Thumbprint | Should -Be '8508F386515CB3D3077DB6B4B7C07F1B4A5E41DE'
            $ts.DigestAlgorithm | Should -Be 'sha256'
            $ts.SignatureAlgorithm | Should -Be 'RSA'
            $ts.SigningTime | Should -Not -BeNullOrEmpty
            $ts.SigningTime.Kind | Should -Be ([System.DateTimeKind]::Utc)
            $ts.SigningTime.ToString('o') | Should -Be '2022-06-23T05:49:24.0000000Z'
            $ts.Timestamp | Should -BeNullOrEmpty
        }

        It "Gets signature details with multiple signatures" {
            # signtool.exe sign /n "PwshTestCode-SelfSigned" /fd SHA256 /t http://timestamp.digicert.com tiny.exe
            # signtool.exe sign /n "PwshTestCode-SelfSigned" /as /fd SHA1 tiny.exe
            # signtool.exe sign /n "PwshTestCode-SelfSigned" /as /fd SHA384 /tr http://timestamp.digicert.com /td SHA384 tiny.exe

            $filePath = Join-Path $testDataPath "tiny-multiple.exe"
            $actual = Get-AuthenticodeSignature -FilePath $filePath
            $actual.Signatures.Count | Should -Be 3

            # SHA256 entry
            $sig = $actual.Signatures[0]
            $sig.Certificate.Subject | Should -Be 'CN=PwshTestCode-SelfSigned'
            $sig.Certificate.Thumbprint | Should -Be 'BF8734E0142FEF69D742E5F46E487EE9B671B530'
            $sig.DigestAlgorithm | Should -Be 'sha256'
            $sig.SignatureAlgorithm | Should -Be 'RSA'
            $sig.SigningTime | Should -BeNullOrEmpty

            $ts = $sig.Timestamp
            $ts | Should -Not -BeNullOrEmpty
            $ts.Certificate.Subject | Should -Be 'CN=DigiCert Timestamp 2022 - 2, O="DigiCert, Inc.", C=US'
            $ts.Certificate.Thumbprint | Should -Be '8508F386515CB3D3077DB6B4B7C07F1B4A5E41DE'
            $ts.DigestAlgorithm | Should -Be 'sha256'
            $ts.SignatureAlgorithm | Should -Be 'RSA'
            $ts.SigningTime | Should -Not -BeNullOrEmpty
            $ts.SigningTime.Kind | Should -Be ([System.DateTimeKind]::Utc)
            $ts.SigningTime.ToString('o') | Should -Be '2022-06-23T05:50:43.0000000Z'
            $ts.Timestamp | Should -BeNullOrEmpty

            # SHA1 entry
            $sig = $actual.Signatures[0]
            $sig.Certificate.Subject | Should -Be 'CN=PwshTestCode-SelfSigned'
            $sig.Certificate.Thumbprint | Should -Be 'BF8734E0142FEF69D742E5F46E487EE9B671B530'
            $sig.DigestAlgorithm | Should -Be 'sha1'
            $sig.SignatureAlgorithm | Should -Be 'RSA'
            $sig.SigningTime | Should -BeNullOrEmpty
            $sig.Timestamp | Should -BeNullOrEmpty

            # SHA384 entry
            $sig = $actual.Signatures[0]
            $sig.Certificate.Subject | Should -Be 'CN=PwshTestCode-SelfSigned'
            $sig.Certificate.Thumbprint | Should -Be 'BF8734E0142FEF69D742E5F46E487EE9B671B530'
            $sig.DigestAlgorithm | Should -Be 'sha384'
            $sig.SignatureAlgorithm | Should -Be 'RSA'
            $sig.SigningTime | Should -BeNullOrEmpty

            $ts = $sig.Timestamp
            $ts | Should -Not -BeNullOrEmpty
            $ts.Certificate.Subject | Should -Be 'CN=DigiCert Timestamp 2022 - 2, O="DigiCert, Inc.", C=US'
            $ts.Certificate.Thumbprint | Should -Be '8508F386515CB3D3077DB6B4B7C07F1B4A5E41DE'
            $ts.DigestAlgorithm | Should -Be 'sha384'
            $ts.SignatureAlgorithm | Should -Be 'RSA'
            $ts.SigningTime | Should -Not -BeNullOrEmpty
            $ts.SigningTime.Kind | Should -Be ([System.DateTimeKind]::Utc)
            $ts.SigningTime.ToString('o') | Should -Be '2022-06-23T05:50:52.0000000Z'
            $ts.Timestamp | Should -BeNullOrEmpty
        }
    }
}

Describe "AuthenticodeSignature cmdlets tests" -Tags "Feature" {

    BeforeAll {
        if (-not $IsWindows) {
            # Skip for non-Windows platforms
            $defaultParamValues = $PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues = @{ "it:skip" = $true }
            return
        }

        $certParams = @{
            Subject           = "CN=PwshTestCode-SelfSigned"
            CertStoreLocation = 'Cert:\CurrentUser\My'
            Type              = 'CodeSigning'
            HashAlgorithm     = "SHA256"
        }
        $codeCertificate = New-SelfSignedCertificate @certParams
        $rootStore = Get-Item -LiteralPath Cert:\LocalMachine\Root
        $rootStore.Open("ReadWrite")
        $rootStore.Add($codeCertificate)
        $rootStore.Dispose()
    }

    AfterAll {
        if (-not $IsWindows) {
            $global:PSdefaultParameterValues = $defaultParamValues
            return
        }
        Remove-Item -LiteralPath "Cert:\CurrentUser\My\$($codeCertificate.Thumbprint)" -Force
        Remove-Item -LiteralPath "Cert:\LocalMachine\Root\$($codeCertificate.Thumbprint)" -Force
    }

    It "Signs with default hash algorithm of SHA256" {
        $ps1File = Join-Path $TestDrive AuthenticodeTest.ps1
        Set-Content -LiteralPath $ps1File -Value '"foo"'

        $actual = Set-AuthenticodeSignature -FilePath $ps1File -Certificate $codeCertificate
        $actual.Status | Should -Be "Valid"
        $actual.Signatures.Count | Should -Be 1
        $actual.Signatures[0].DigestAlgorithm | Should -Be 'sha256'
        $actual.Signatures[0].SigningAlgorithm | Should -Be 'rsa'

        $actual = Get-AuthenticodeSignature -FilePath $ps1File
        $actual.Status | Should -Be "Valid"
        $actual.Signatures.Count | Should -Be 1
        $actual.Signatures[0].DigestAlgorithm | Should -Be 'sha256'
        $actual.Signatures[0].SigningAlgorithm | Should -Be 'rsa'
    }

    It "Signs with explicit hash algorithm" {
        $ps1File = Join-Path $TestDrive AuthenticodeTest.ps1
        Set-Content -LiteralPath $ps1File -Value '"foo"'

        $actual = Set-AuthenticodeSignature -FilePath $ps1File -Certificate $codeCertificate -HashAlgorithm SHA384
        $actual.Status | Should -Be "Valid"
        $actual.Signatures.Count | Should -Be 1
        $actual.Signatures[0].DigestAlgorithm | Should -Be 'sha256'
        $actual.Signatures[0].SigningAlgorithm | Should -Be 'rsa'

        $actual = Get-AuthenticodeSignature -FilePath $ps1File
        $actual.Status | Should -Be "Valid"
        $actual.Signatures.Count | Should -Be 1
        $actual.Signatures[0].DigestAlgorithm | Should -Be 'sha256'
        $actual.Signatures[0].SigningAlgorithm | Should -Be 'rsa'
    }
}

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Windows platform file signatures" -Tags 'Feature' {

    It "Verifies Get-AuthenticodeSignature returns correct signature for catalog signed file" -Skip:(!$IsWindows) {

        if ($null -eq $env:windir) {
            throw "Expected Windows platform environment path variable '%windir%' not available."
        }

        $filePath = Join-Path -Path $env:windir -ChildPath 'System32\ntdll.dll'
        if (! (Test-Path -Path $filePath)) {
            throw "Expected Windows PowerShell platform module path '$filePath' not found."
        }

        $signature = Get-AuthenticodeSignature -FilePath $filePath
        $signature | Should -Not -BeNullOrEmpty
        $signature.Status | Should -BeExactly 'Valid'
        $signature.SignatureType | Should -BeExactly 'Catalog'
        
        # Verify that SubjectAlternativeName property exists
        $signature.PSObject.Properties.Name | Should -Contain 'SubjectAlternativeName'
    }
}

Describe "Windows file content signatures" -Tags @('Feature', 'RequireAdminOnWindows') {
    BeforeAll {
        $shouldSkip = (-not $IsWindows) -or (Test-IsWinServer2012R2)

        if ($shouldSkip) {
            Push-DefaultParameterValueStack @{ "it:skip" = $shouldSkip }
            return
        }

        $session = New-PSSession -UseWindowsPowerShell
        try {
            # New-SelfSignedCertificate runs in implicit remoting so do all the
            # setup work over there
            $caRootThumbprint, $signingThumbprint = Invoke-Command -Session $session -ScriptBlock {
                $testPrefix = 'SelfSignedTest'

                $enhancedKeyUsage = [Security.Cryptography.OidCollection]::new()
                $null = $enhancedKeyUsage.Add('1.3.6.1.5.5.7.3.3')  # Code Signing

                $caParams = @{
                    Extension         = @(
                        [Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]::new($true, $false, 0, $true),
                        [Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new('KeyCertSign', $false),
                        [Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension ]::new($enhancedKeyUsage, $false)
                    )
                    CertStoreLocation = 'Cert:\CurrentUser\My'
                    NotAfter          = (Get-Date).AddDays(1)
                    Type              = 'Custom'
                }
                $caRoot = PKI\New-SelfSignedCertificate @caParams -Subject "CN=$testPrefix-CA"

                $rootStore = Get-Item -Path Cert:\LocalMachine\Root
                $rootStore.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
                try {
                    $rootStore.Add([System.Security.Cryptography.X509Certificates.X509Certificate2]::new($caRoot.RawData))
                } finally {
                    $rootStore.Close()
                }

                $certParams = @{
                    CertStoreLocation = 'Cert:\CurrentUser\My'
                    KeyUsage          = 'DigitalSignature'
                    TextExtension     = @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
                    Type              = 'Custom'
                }
                $certificate = PKI\New-SelfSignedCertificate @certParams -Subject "CN=$testPrefix-Signed" -Signer $caRoot

                $publisherStore = Get-Item -Path Cert:\LocalMachine\TrustedPublisher
                $publisherStore.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
                try {
                    $publisherStore.Add([System.Security.Cryptography.X509Certificates.X509Certificate2]::new($certificate.RawData))
                } finally {
                    $publisherStore.Close()
                }

                $caRoot | Remove-Item

                $caRoot.Thumbprint, $certificate.Thumbprint
            }
        } finally {
            $session | Remove-PSSession
        }

        $certificate = Get-Item -Path Cert:\CurrentUser\My\$signingThumbprint
    }

    AfterAll {
        if ($shouldSkip) {
            return
        }

        $paths = @(
            "Cert:\LocalMachine\Root\$caRootThumbprint"
            "Cert:\LocalMachine\TrustedPublisher\$signingThumbprint"
            "Cert:\CurrentUser\My\$signingThumbprint"
        )

        foreach($path in $paths) {
            if (Test-Path $path -PathType Leaf) {
                # failing to remove is not fatal
                Remove-Item -Force -Path $path -ErrorAction Ignore
            }
        }
    }

    It "Validates signature using path on even char count with Encoding <Encoding>" -TestCases @(
        @{ Encoding = 'ASCII' }
        @{ Encoding = 'Unicode' }
        @{ Encoding = 'UTF8BOM' }
        @{ Encoding = 'UTF8NoBOM' }
    ) {
        param ($Encoding)

        Set-Content -Path testdrive:\test.ps1 -Value 'Write-Output "Hello World"' -Encoding $Encoding

        $scriptPath = Join-Path $TestDrive test.ps1
        $status = Set-AuthenticodeSignature -FilePath $scriptPath -Certificate $certificate
        $status.Status | Should -Be 'Valid'

        $actual = Get-AuthenticodeSignature -FilePath $scriptPath
        $actual.SignerCertificate.Thumbprint | Should -Be $certificate.Thumbprint
        $actual.Status | Should -Be 'Valid'
    }

    It "Validates signature using path on odd char count with Encoding <Encoding>" -TestCases @(
        @{ Encoding = 'ASCII' }
        @{ Encoding = 'Unicode' }
        @{ Encoding = 'UTF8BOM' }
        @{ Encoding = 'UTF8NoBOM' }
    ) {
        param ($Encoding)

        Set-Content -Path testdrive:\test.ps1 -Value 'Write-Output "Hello World!"' -Encoding $Encoding

        $scriptPath = Join-Path $TestDrive test.ps1
        $status = Set-AuthenticodeSignature -FilePath $scriptPath -Certificate $certificate
        $status.Status | Should -Be 'Valid'

        $actual = Get-AuthenticodeSignature -FilePath $scriptPath
        $actual.SignerCertificate.Thumbprint | Should -Be $certificate.Thumbprint
        $actual.Status | Should -Be 'Valid'
    }

    It "Validates signature using content on even char count with Encoding <Encoding>" -TestCases @(
        @{ Encoding = 'ASCII' }
        @{ Encoding = 'Unicode' }
        @{ Encoding = 'UTF8BOM' }
        @{ Encoding = 'UTF8NoBOM' }
    ) {
        param ($Encoding)

        Set-Content -Path testdrive:\test.ps1 -Value 'Write-Output "Hello World"' -Encoding $Encoding

        $scriptPath = Join-Path $TestDrive test.ps1
        $status = Set-AuthenticodeSignature -FilePath $scriptPath -Certificate $certificate
        $status.Status | Should -Be 'Valid'

        $fileBytes = Get-Content -Path testdrive:\test.ps1 -AsByteStream

        $actual = Get-AuthenticodeSignature -Content $fileBytes -SourcePathOrExtension .ps1
        $actual.SignerCertificate.Thumbprint | Should -Be $certificate.Thumbprint
        $actual.Status | Should -Be 'Valid'
    }

    It "Validates signature using content on odd char count with Encoding <Encoding>" -TestCases @(
        @{ Encoding = 'ASCII' }
        @{ Encoding = 'Unicode' }
        @{ Encoding = 'UTF8BOM' }
        @{ Encoding = 'UTF8NoBOM' }
    ) {
        param ($Encoding)

        Set-Content -Path testdrive:\test.ps1 -Value 'Write-Output "Hello World!"' -Encoding $Encoding

        $scriptPath = Join-Path $TestDrive test.ps1
        $status = Set-AuthenticodeSignature -FilePath $scriptPath -Certificate $certificate
        $status.Status | Should -Be 'Valid'

        $fileBytes = Get-Content -Path testdrive:\test.ps1 -AsByteStream

        $actual = Get-AuthenticodeSignature -Content $fileBytes -SourcePathOrExtension .ps1
        $actual.SignerCertificate.Thumbprint | Should -Be $certificate.Thumbprint
        $actual.Status | Should -Be 'Valid'
    }

    It "Verifies SubjectAlternativeName is populated for certificate with SAN" {
        $session = New-PSSession -UseWindowsPowerShell
        try {
            $sanThumbprint = Invoke-Command -Session $session -ScriptBlock {
                $testPrefix = 'SelfSignedTestSAN'

                $enhancedKeyUsage = [Security.Cryptography.OidCollection]::new()
                $null = $enhancedKeyUsage.Add('1.3.6.1.5.5.7.3.3')

                $caParams = @{
                    Extension         = @(
                        [Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]::new($true, $false, 0, $true),
                        [Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new('KeyCertSign', $false),
                        [Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]::new($enhancedKeyUsage, $false)
                    )
                    CertStoreLocation = 'Cert:\CurrentUser\My'
                    NotAfter          = (Get-Date).AddDays(1)
                    Type              = 'Custom'
                }
                $sanCA = PKI\New-SelfSignedCertificate @caParams -Subject "CN=$testPrefix-CA"

                $rootStore = Get-Item -Path Cert:\LocalMachine\Root
                $rootStore.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
                try {
                    $rootStore.Add([System.Security.Cryptography.X509Certificates.X509Certificate2]::new($sanCA.RawData))
                } finally {
                    $rootStore.Close()
                }

                $certParams = @{
                    CertStoreLocation = 'Cert:\CurrentUser\My'
                    KeyUsage          = 'DigitalSignature'
                    TextExtension     = @(
                        "2.5.29.37={text}1.3.6.1.5.5.7.3.3",
                        "2.5.29.19={text}",
                        "2.5.29.17={text}DNS=test.example.com&DNS=*.example.com"
                    )
                    Type              = 'Custom'
                }
                $sanCert = PKI\New-SelfSignedCertificate @certParams -Subject "CN=$testPrefix-Signed" -Signer $sanCA

                $publisherStore = Get-Item -Path Cert:\LocalMachine\TrustedPublisher
                $publisherStore.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
                try {
                    $publisherStore.Add([System.Security.Cryptography.X509Certificates.X509Certificate2]::new($sanCert.RawData))
                } finally {
                    $publisherStore.Close()
                }

                $sanCA | Remove-Item
                $sanCA.Thumbprint, $sanCert.Thumbprint
            }
        } finally {
            $session | Remove-PSSession
        }

        $sanCARootThumbprint = $sanThumbprint[0]
        $sanCertThumbprint = $sanThumbprint[1]
        $sanCertificate = Get-Item -Path Cert:\CurrentUser\My\$sanCertThumbprint

        try {
            Set-Content -Path testdrive:\test.ps1 -Value 'Write-Output "Test SAN"' -Encoding UTF8NoBOM

            $scriptPath = Join-Path $TestDrive test.ps1
            $status = Set-AuthenticodeSignature -FilePath $scriptPath -Certificate $sanCertificate
            $status.Status | Should -Be 'Valid'

            $actual = Get-AuthenticodeSignature -FilePath $scriptPath
            $actual.SubjectAlternativeName | Should -Not -BeNullOrEmpty
            $actual.SubjectAlternativeName | Should -BeOfType [string]
            $actual.SubjectAlternativeName.Count | Should -Be 2
            $actual.SubjectAlternativeName | Should -Contain 'DNS Name=test.example.com'
            $actual.SubjectAlternativeName | Should -Contain 'DNS Name=*.example.com'
        } finally {
            Remove-Item -Path "Cert:\LocalMachine\Root\$sanCARootThumbprint" -Force -ErrorAction Ignore
            Remove-Item -Path "Cert:\LocalMachine\TrustedPublisher\$sanCertThumbprint" -Force -ErrorAction Ignore
            Remove-Item -Path "Cert:\CurrentUser\My\$sanCertThumbprint" -Force -ErrorAction Ignore
        }
    }

    It "Verifies SubjectAlternativeName is null when certificate has no SAN" {
        Set-Content -Path testdrive:\test.ps1 -Value 'Write-Output "Test No SAN"' -Encoding UTF8NoBOM

        $scriptPath = Join-Path $TestDrive test.ps1
        $status = Set-AuthenticodeSignature -FilePath $scriptPath -Certificate $certificate
        $status.Status | Should -Be 'Valid'

        $actual = Get-AuthenticodeSignature -FilePath $scriptPath
        $actual.SignerCertificate.Thumbprint | Should -Be $certificate.Thumbprint
        $actual.Status | Should -Be 'Valid'
        
        # Verify that SubjectAlternativeName property exists
        $actual.PSObject.Properties.Name | Should -Contain 'SubjectAlternativeName'
        
        # Verify the content is null when certificate has no SAN extension
        $actual.SubjectAlternativeName | Should -BeNullOrEmpty
    }
}

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'CertificateProvider.PSDnsNameListCertificateParser' -Tags "CI" {
    BeforeAll {
        $keyFilePath = Join-Path -Path $TestDrive -ChildPath 'privateKey.key'
        $certFilePath = Join-Path -Path $TestDrive -ChildPath 'certificate.crt'
        $pfxFilePath = Join-Path -Path $TestDrive -ChildPath 'certificate.pfx'
        $password = New-CertificatePassword | ConvertFrom-SecureString -AsPlainText

        $originalDefaultParams = $PSDefaultParameterValues.Clone()
        $PSDefaultParameterValues['It:Skip'] = -not [ExperimentalFeature]::IsEnabled('PSDnsNameListCertificateParser')

        Import-Module (Join-Path -Path $PSScriptRoot 'certificateCommon.psm1') -Force
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It "<Title>" -TestCases @(
        @{
            Title              = 'Should set DNSNameList from Subject Distinguished name'
            Commands           = @(
                "openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout $keyFilePath -out $certFilePath -subj '/CN=yourdomain.com' -addext 'subjectAltName=DNS:www.yourdomain.com'",
                "openssl pkcs12 -export -out $pfxFilePath -inkey $keyFilePath -in $certFilePath -passout pass:$password"
            )
            ExpectedDnsNameList = @(
                [PSCustomObject]@{ Punycode = "yourdomain.com"; Unicode = "yourdomain.com" }
                [PSCustomObject]@{ Punycode = "www.yourdomain.com"; Unicode = "www.yourdomain.com" }
            )
        }
        @{
            Title              = 'Should set DNSNameList for multi-value RDN'
            Commands           = @(
                "openssl req -x509 -nodes -keyout $keyFilePath -subj '/C=DK/O=Ingen organisatorisk tilknytning/CN=yourdomain.com+serialNumber=XYZ:1111-2222-3-444444444444' -out $certFilePath",
                "openssl pkcs12 -export -out $pfxFilePath -inkey $keyFilePath -in $certFilePath -passout pass:$password"
            )
            ExpectedDnsNameList = @(
                !$IsWindows ? [PSCustomObject]@{ Punycode = "yourdomain.com"; Unicode = "yourdomain.com" } : [PSCustomObject]@{ Punycode = "yourdomain.com+serialNumber=XYZ:1111-2222-3-444444444444"; Unicode = "yourdomain.com+serialNumber=XYZ:1111-2222-3-444444444444" }
            )
        }
    ) {
        param($Commands, $ExpectedDnsNameList)

        # Execute the OpenSSL commands in silent mode
        Invoke-Expression -Command "& { $($Commands -join ';') } 2>&1 1>`$null"

        # Create the certificate object
        $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($pfxFilePath, $password)

        # Validate DnsNameList is correct
        $cert | Should -Not -BeNullOrEmpty
        $cert.DnsNameList | Should -HaveCount $ExpectedDnsNameList.Count
        ($cert.DnsNameList | ConvertTo-Json -Compress) | Should -BeExactly ($ExpectedDnsNameList | ConvertTo-Json -Compress)
    }

    AfterEach {
        Remove-Item -Path $keyFilePath -Force -ErrorAction SilentlyContinue
        Remove-Item -Path $certFilePath -Force -ErrorAction SilentlyContinue
        Remove-Item -Path $pfxFilePath -Force -ErrorAction SilentlyContinue
    }
}

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'CertificateProvider.PSDnsNameSubjectNameCertificateParser' -Tags "CI" {
    BeforeAll {
        Import-Module (Join-Path -Path $PSScriptRoot 'certificateCommon.psm1') -Force

        $keyFilePath = Join-Path -Path $TestDrive -ChildPath 'privateKey.key'
        $certFilePath = Join-Path -Path $TestDrive -ChildPath 'certificate.crt'
        $pfxFilePath = Join-Path -Path $TestDrive -ChildPath 'certificate.pfx'
        $password = New-CertificatePassword | ConvertFrom-SecureString -AsPlainText

        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        $PSDefaultParameterValues['It:Skip'] = -not [ExperimentalFeature]::IsEnabled('PSDnsNameSubjectNameCertificateParser')
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It "<Title>" -TestCases @(
        @{
            Title              = 'Should set DNSNameList from multi-value RDN with special characters'
            Commands           = @(
                "openssl req -x509 -nodes -keyout $keyFilePath -subj '/CN=exämple.com+SN=SpecialSerial123' -out $certFilePath",
                "openssl pkcs12 -export -out $pfxFilePath -inkey $keyFilePath -in $certFilePath -passout pass:$password"
            )
            ExpectedDnsNameList = @(
                [PSCustomObject]@{ Punycode = "exämple.com"; Unicode = "exämple.com" }
            )
        }
        @{
            Title              = 'Should handle RDN with subject alternative names'
            Commands           = @(
                "openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout $keyFilePath -out $certFilePath -subj '/CN=example.com' -addext 'subjectAltName=DNS:alt.example.com,DNS:another.example.com'",
                "openssl pkcs12 -export -out $pfxFilePath -inkey $keyFilePath -in $certFilePath -passout pass:$password"
            )
            ExpectedDnsNameList = @(
                [PSCustomObject]@{ Punycode = "example.com"; Unicode = "example.com" }
                [PSCustomObject]@{ Punycode = "alt.example.com"; Unicode = "alt.example.com" }
                [PSCustomObject]@{ Punycode = "another.example.com"; Unicode = "another.example.com" }
            )
        }
        @{
            Title              = 'Should handle long and complex RDN'
            Commands           = @(
                "openssl req -x509 -nodes -keyout $keyFilePath -subj '/C=AU/ST=NSW/L=Sydney/O=ExampleOrg/CN=longexample.com+OU=Engineering' -out $certFilePath",
                "openssl pkcs12 -export -out $pfxFilePath -inkey $keyFilePath -in $certFilePath -passout pass:$password"
            )
            ExpectedDnsNameList = @(
                [PSCustomObject]@{ Punycode = "longexample.com"; Unicode = "longexample.com" }
            )
        }
        @{
            Title              = 'Should handle empty RDN attributes gracefully'
            Commands           = @(
                "openssl req -x509 -nodes -keyout $keyFilePath -subj '/CN=/OU=' -out $certFilePath",
                "openssl pkcs12 -export -out $pfxFilePath -inkey $keyFilePath -in $certFilePath -passout pass:$password"
            )
            ExpectedDnsNameList = @(
                # Expect no entries due to empty attributes
            )
        }
        @{
            Title              = 'Should validate DNSNameList with mixed case RDN'
            Commands           = @(
                "openssl req -x509 -nodes -keyout $keyFilePath -subj '/CN=MiXeDCasE.com+OU=TestCase' -out $certFilePath",
                "openssl pkcs12 -export -out $pfxFilePath -inkey $keyFilePath -in $certFilePath -passout pass:$password"
            )
            ExpectedDnsNameList = @(
                [PSCustomObject]@{ Punycode = "MiXeDCasE.com"; Unicode = "MiXeDCasE.com" }
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

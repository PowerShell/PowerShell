# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'CertificateProvider.PSDnsNameSubjectNameCertificateParser' -Tags "CI" {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        $PSDefaultParameterValues['It:Skip'] = -not [ExperimentalFeature]::IsEnabled('PSDnsNameSubjectNameCertificateParser')

        $testDataCertificatesPath = Join-Path -Path $PSScriptRoot -ChildPath 'TestData' -AdditionalChildPath 'Certificates'
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It "<Title>" -TestCases @(
        @{
            Title              = 'Should set DNSNameList from multi-value RDN with special characters'
            ExpectedDnsNameList = @(
                [PSCustomObject]@{ Punycode = "exämple.com"; Unicode = "exämple.com" }
            )

            # openssl req -x509 -nodes -keyout $keyFilePath -subj '/CN=exämple.com+SN=SpecialSerial123' -out $certFilePath
            # openssl pkcs12 -export -out $pfxFilePath -inkey $keyFilePath -in $certFilePath -passout pass:
            CertificatePath = Join-Path -Path $testDataCertificatesPath -ChildPath 'certificate3.pfx'
        }
        @{
            Title              = 'Should handle RDN with subject alternative names'
            ExpectedDnsNameList = @(
                [PSCustomObject]@{ Punycode = "example.com"; Unicode = "example.com" }
                [PSCustomObject]@{ Punycode = "alt.example.com"; Unicode = "alt.example.com" }
                [PSCustomObject]@{ Punycode = "another.example.com"; Unicode = "another.example.com" }
            )

            # openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout $keyFilePath -out $certFilePath -subj '/CN=example.com' -addext 'subjectAltName=DNS:alt.example.com,DNS:another.example.com'
            # openssl pkcs12 -export -out $pfxFilePath -inkey $keyFilePath -in $certFilePath -passout pass:
            CertificatePath = Join-Path -Path $testDataCertificatesPath -ChildPath 'certificate4.pfx'
        }
        @{
            Title              = 'Should handle long and complex RDN'
            ExpectedDnsNameList = @(
                [PSCustomObject]@{ Punycode = "longexample.com"; Unicode = "longexample.com" }
            )

            # openssl req -x509 -nodes -keyout $keyFilePath -subj '/C=AU/ST=NSW/L=Sydney/O=ExampleOrg/CN=longexample.com+OU=Engineering' -out $certFilePath
            # openssl pkcs12 -export -out $pfxFilePath -inkey $keyFilePath -in $certFilePath -passout pass:
            CertificatePath = Join-Path -Path $testDataCertificatesPath -ChildPath 'certificate5.pfx'
        }
        @{
            Title              = 'Should handle empty RDN attributes gracefully'
            ExpectedDnsNameList = @(
                # Expect no entries due to empty attributes
            )

            # openssl req -x509 -nodes -keyout $keyFilePath -subj '/CN=/OU=' -out $certFilePath
            # openssl pkcs12 -export -out $pfxFilePath -inkey $keyFilePath -in $certFilePath -passout pass:
            CertificatePath = Join-Path -Path $testDataCertificatesPath -ChildPath 'certificate6.pfx'
        }
        @{
            Title              = 'Should validate DNSNameList with mixed case RDN'
            ExpectedDnsNameList = @(
                [PSCustomObject]@{ Punycode = "MiXeDCasE.com"; Unicode = "MiXeDCasE.com" }
            )

            # openssl req -x509 -nodes -keyout $keyFilePath -subj '/CN=MiXeDCasE.com+OU=TestCase' -out $certFilePath
            # openssl pkcs12 -export -out $pfxFilePath -inkey $keyFilePath -in $certFilePath -passout pass:
            CertificatePath = Join-Path -Path $testDataCertificatesPath -ChildPath 'certificate7.pfx'
        }
    ) {
        param($ExpectedDnsNameList, $CertificatePath)

        # Create the certificate object
        $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($CertificatePath)

        # Validate DnsNameList is correct
        $cert | Should -Not -BeNullOrEmpty
        $cert.DnsNameList | Should -HaveCount $ExpectedDnsNameList.Count
        ($cert.DnsNameList | ConvertTo-Json -Compress) | Should -BeExactly ($ExpectedDnsNameList | ConvertTo-Json -Compress)
    }
}

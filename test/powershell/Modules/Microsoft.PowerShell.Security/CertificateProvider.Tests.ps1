# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# The import and table creation work on non-windows, but are currently not needed
if($IsWindows)
{
    Import-Module (Join-Path -Path $PSScriptRoot 'certificateCommon.psm1') -Force
}

    $currentUserMyLocations = @(
        @{path = 'Cert:\CurrentUser\my'}
        @{path = 'cert:\currentuser\my'}
        @{path = 'Microsoft.PowerShell.Security\Certificate::CurrentUser\My'}
        @{path = 'Microsoft.PowerShell.Security\certificate::currentuser\my'}
    )

    $testLocations = @(
        @{path = 'cert:\'}
        @{path = 'CERT:\'}
        @{path = 'Microsoft.PowerShell.Security\Certificate::'}
    )

# Add CurrentUserMyLocations to TestLocations
foreach($location in $currentUserMyLocations)
{
    $testLocations += $location
}

Describe "Certificate Provider tests" -Tags "CI" {
    BeforeAll{
        if(!$IsWindows)
        {
            # Skip for non-Windows platforms
            $defaultParamValues = $global:PSDefaultParameterValues.Clone()
            $global:PSDefaultParameterValues = @{ "it:skip" = $true }
        }
    }

    AfterAll {
        if(!$IsWindows)
        {
            $global:PSDefaultParameterValues = $defaultParamValues
        }
    }

    Context "Get-Item tests" {
        It "Should be able to get a certificate store, path: <path>" -TestCases $testLocations {
            param([string] $path)
            $expectedResolvedPath = Resolve-Path -LiteralPath $path
            $result = Get-Item -LiteralPath $path
            $result | Should -Not -Be null
            $result | ForEach-Object {
                $resolvedPath = Resolve-Path $_.PSPath
                $resolvedPath.Provider | Should -Be $expectedResolvedPath.Provider
                $resolvedPath.ProviderPath.TrimStart('\') | Should -Be $expectedResolvedPath.ProviderPath.TrimStart('\')
            }
        }
        It "Should return two items at the root of the provider" {
            (Get-Item -Path cert:\*).Count | Should -Be 2
        }
        It "Should be able to get multiple items explictly" {
            (Get-Item cert:\LocalMachine , cert:\CurrentUser).Count | Should -Be 2
        }
        It "Should return PathNotFound when getting a non-existant certificate store" {
            {Get-Item cert:\IDONTEXIST -ErrorAction Stop} | Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.GetItemCommand"
        }
        It "Should return PathNotFound when getting a non-existant certificate" {
            {Get-Item cert:\currentuser\my\IDONTEXIST -ErrorAction Stop} | Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.GetItemCommand"
        }
    }
    Context "Get-ChildItem tests"{
        It "should be able to get a container using a wildcard" {
            (Get-ChildItem Cert:\CurrentUser\M?).PSPath | Should -Be 'Microsoft.PowerShell.Security\Certificate::CurrentUser\My'
        }
        It "Should return two items at the root of the provider" {
            (Get-ChildItem -Path cert:\).Count | Should -Be 2
        }
    }
}

Describe "Certificate Provider tests" -Tags "Feature" {
    BeforeAll{
        if($IsWindows)
        {
            if (-not (Install-TestCertificates) ) {
                $SetupFailure = $true
            }
            else {
                Push-Location Cert:\
                $SetupFailure = $false
            }
        }
        else
        {
            # Skip for non-Windows platforms
            $defaultParamValues = $global:PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues = @{ "it:skip" = $true }
        }
    }

    AfterAll {
        if($IsWindows -and -not $SetupFailure)
        {
            Remove-TestCertificates
            Pop-Location
        }
        else
        {
            if ($defaultParamValues -ne $null) {
                $global:PSDefaultParameterValues = $defaultParamValues
            }
        }
    }

    Context "Get-Item tests" {
        It "Should be able to get certifate by path: <path>" -TestCases $currentUserMyLocations {
            param([string] $path)
            if ($SetupFailure) {
                Set-ItResult -Inconclusive -Because "Test certificates are not installed"
            }

            if ($SetupFailure) {
                Set-ItResult -Inconclusive -Because "Test certificates are not installed"
            }

            $expectedThumbprint = (Get-GoodCertificateObject).Thumbprint
            $leafPath = Join-Path -Path $path -ChildPath $expectedThumbprint
            $cert = (Get-Item -LiteralPath $leafPath)
            if ($SetupFailure) {
                Set-ItResult -Inconclusive -Because "Test certificates are not installed"
            }
            $cert.Thumbprint | Should -Be $expectedThumbprint
        }
        It "Should be able to get DnsNameList of certifate by path: <path>" -TestCases $currentUserMyLocations {
            param([string] $path)

            if ($SetupFailure) {
                Set-ItResult -Inconclusive -Because "Test certificates are not installed"
            }

            $expectedThumbprint = (Get-GoodCertificateObject).Thumbprint
            $expectedName = (Get-GoodCertificateObject).DnsNameList
            $expectedEncodedName = (Get-GoodCertificateObject).DnsNameList
            $leafPath = Join-Path -Path $path -ChildPath $expectedThumbprint
            $cert = (Get-item -LiteralPath $leafPath)
            $cert | Should -Not -Be $null
            $cert.DnsNameList | Should -Not -Be $null
            $cert.DnsNameList.Count | Should -Be 3
            $cert.DnsNameList[0].Unicode | Should -Be $expectedName[0].Unicode
            $cert.DnsNameList[0].Punycode | Should -Be $expectedEncodedName[0].Punycode
            $cert.DnsNameList[1].Unicode | Should -Be $expectedName[1].Unicode
            $cert.DnsNameList[1].Punycode | Should -Be $expectedEncodedName[1].Punycode
            $cert.DnsNameList[2].Unicode | Should -Be $expectedName[2].Unicode
            $cert.DnsNameList[2].Punycode | Should -Be $expectedEncodedName[2].Punycode
        }
        it "Should be able to get EnhancedKeyUsageList of certifate by path: <path>" -TestCases $currentUserMyLocations {
            param([string] $path)

            if ($SetupFailure) {
                Set-ItResult -Inconclusive -Because "Test certificates are not installed"
            }

            $expectedThumbprint = (Get-GoodCertificateObject).Thumbprint
            $expectedOid = (Get-GoodCertificateObject).EnhancedKeyUsageList[0].ObjectId
            $leafPath = Join-Path -Path $path -ChildPath $expectedThumbprint
            $cert = (Get-item -LiteralPath $leafPath)
            $cert | Should -Not -Be $null
            $cert.EnhancedKeyUsageList | Should -Not -Be null
            $cert.EnhancedKeyUsageList.Count | Should -Be 3
            $cert.EnhancedKeyUsageList[0].ObjectId.Length | Should -Not -Be 0
            $cert.EnhancedKeyUsageList[0].ObjectId | Should -Be $expectedOid
        }
        It "Should filter to codesign certificates" {

            if ($SetupFailure) {
                Set-ItResult -Inconclusive -Because "Test certificates are not installed"
            }

            $allCerts = Get-Item cert:\CurrentUser\My\*
            $codeSignCerts = Get-Item cert:\CurrentUser\My\* -CodeSigningCert
            $codeSignCerts | Should -Not -Be null
            $allCerts | Should -Not -Be null
            $nonCodeSignCertCount = $allCerts.Count - $codeSignCerts.Count
            $nonCodeSignCertCount | Should -Not -Be 0
        }
        It "Should be able to exclude by thumbprint" {

            if ($SetupFailure) {
                Set-ItResult -Inconclusive -Because "Test certificates are not installed"
            }

            $allCerts = Get-Item cert:\CurrentUser\My\*
            $testThumbprint = (Get-GoodCertificateObject).Thumbprint
            $allCertsExceptOne = (Get-Item "cert:\currentuser\my\*" -Exclude $testThumbprint)
            $allCerts | Should -Not -Be null
            $allCertsExceptOne | Should -Not -Be null
            $countDifference = $allCerts.Count - $allCertsExceptOne.Count
            $countDifference | Should -Be 1
        }
    }
    Context "Get-ChildItem tests"{
        BeforeAll {
            $cert = Get-GoodServerCertificateObject
        }
        it "Should filter to codesign certificates" {

            if ($SetupFailure) {
                Set-ItResult -Inconclusive -Because "Test certificates are not installed"
            }

            $allCerts = get-ChildItem cert:\CurrentUser\My
            $codeSignCerts = get-ChildItem cert:\CurrentUser\My -CodeSigningCert
            $codeSignCerts | Should -Not -Be null
            $allCerts | Should -Not -Be null
            $nonCodeSignCertCount = $allCerts.Count - $codeSignCerts.Count
            $nonCodeSignCertCount | Should -Not -Be 0
        }
        it "Should filter to ExpiringInDays certificates" {

            if ($SetupFailure) {
                Set-ItResult -Inconclusive -Because "Test certificates are not installed"
            }

            $thumbprint = $cert.Thumbprint
            $NotAfter = $cert.NotAfter
            $before = ($NotAfter.AddDays(-1) - (Get-Date)).Days
            $after = ($NotAfter.AddDays(+1) - (Get-Date)).Days
            $beforeCerts = Get-ChildItem cert:\CurrentUser\My\$thumbprint -ExpiringInDays $before
            $afterCerts = Get-ChildItem cert:\CurrentUser\My\$thumbprint -ExpiringInDays $after

            $beforeCerts.Count | Should -Be 0
            $afterCerts.Count | Should -Be 1
            $afterCerts.Thumbprint | Should -BeExactly $thumbprint
        }
        it "Should filter to DocumentEncryptionCert certificates" {

            if ($SetupFailure) {
                Set-ItResult -Inconclusive -Because "Test certificates are not installed"
            }

            $thumbprint = $cert.Thumbprint
            $certs = Get-ChildItem cert:\CurrentUser\My\$thumbprint -DocumentEncryptionCert

            $certs.Count | Should -Be 1
            $certs.Thumbprint | Should -BeExactly $thumbprint
        }
        it "Should filter to DNSName certificates: <name>" -TestCases @(
            @{ Name = "in Subject";                  SearchName = '*ncipher*'; Count = 1; Thumbprint = $cert.Thumbprint }
            @{ Name = "in Subject Alternative Name"; SearchName = '*conto*';   Count = 1; Thumbprint = $cert.Thumbprint }
            @{ Name = "not existing name";           SearchName = '*QWERTY*';  Count = 0; Thumbprint = $null }
        ) {
            param($name, $searchName, $count, $thumbprint)

            if ($SetupFailure) {
                Set-ItResult -Inconclusive -Because "Test certificates are not installed"
            }

            $certs = Get-ChildItem cert:\CurrentUser\My\$thumbprint -DNSName $searchName

            $certs.Count | Should -Be $count
            $certs.Thumbprint | Should -BeExactly $thumbprint

        }
        it "Should filter to SSLServerAuthentication certificates" {

            if ($SetupFailure) {
                Set-ItResult -Inconclusive -Because "Test certificates are not installed"
            }

            $thumbprint = $cert.Thumbprint

            $certs = Get-ChildItem cert:\CurrentUser\My\$thumbprint -SSLServerAuthentication

            $certs.Count | Should -Be 1
            $certs.Thumbprint | Should -BeExactly $thumbprint
        }
        it "Should filter to EKU certificates: <name>" -TestCases @(
            @{ Name = "can filter by name";                            EKU = '*encryp*';                             Count = 1; Thumbprint = $cert.Thumbprint }
            @{ Name = "can filter by OID";                             EKU = '*1.4.1.311.80.1*';                     Count = 1; Thumbprint = $cert.Thumbprint }
            @{ Name = "all patterns should be passed - positive test"; EKU = "1.3.6.1.5.5.7.3.2","*1.4.1.311.80.1*"; Count = 1; Thumbprint = $cert.Thumbprint }
            @{ Name = "all patterns should be passed - negative test"; EKU = "*QWERTY*","*encryp*";                  Count = 0; Thumbprint = $null }
        ) {
            param($name, $ekuSearch, $count, $thumbprint)

            if ($SetupFailure) {
                Set-ItResult -Inconclusive -Because "Test certificates are not installed"
            }

            $certs = Get-ChildItem cert:\CurrentUser\My\$thumbprint -EKU $ekuSearch

            $certs.Count | Should -Be $count
            $certs.Thumbprint | Should -BeExactly $thumbprint
        }
    }

    Context "SAN DNS Name Tests" {
        BeforeAll {
            $configFilePath = Join-Path -Path $TestDrive -ChildPath 'openssl.cnf'
            $keyFilePath = Join-Path -Path $TestDrive -ChildPath 'privateKey.key'
            $certFilePath = Join-Path -Path $TestDrive -ChildPath 'certificate.crt'
            $pfxFilePath = Join-Path -Path $TestDrive -ChildPath 'certificate.pfx'
            $password = New-CertificatePassword | ConvertFrom-SecureString -AsPlainText

            $config = @"
            [ req ]
            default_bits       = 2048
            distinguished_name = req_distinguished_name
            req_extensions     = v3_req
            prompt             = no

            [ req_distinguished_name ]
            CN                 = yourdomain.com

            [ v3_req ]
            subjectAltName     = @alt_names

            [ alt_names ]
            DNS.1              = yourdomain.com
            DNS.2              = www.yourdomain.com
            DNS.3              = api.yourdomain.com
            DNS.4              = xn--mnchen-3ya.com
            DNS.5              = xn--80aaxitdbjr.com
            DNS.6              = xn--caf-dma.com
"@

            # Write the configuration to the specified path
            Set-Content -Path $configFilePath -Value $config

            # Generate the self-signed certificate with SANs
            openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout $keyFilePath -out $certFilePath -config $configFilePath -extensions v3_req

            # Create the PFX file
            openssl pkcs12 -export -out $pfxFilePath -inkey $keyFilePath -in $certFilePath -passout pass:$password
        }

        It "Should set DNSNameList from SAN extensions" {
            $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($pfxFilePath, $password)

            $expectedDnsNameList = @(
                [PSCustomObject]@{
                    Punycode = "yourdomain.com"
                    Unicode  = "yourdomain.com"
                }
                [PSCustomObject]@{
                    Punycode = "www.yourdomain.com"
                    Unicode  = "www.yourdomain.com"
                }
                [PSCustomObject]@{
                    Punycode = "api.yourdomain.com"
                    Unicode  = "api.yourdomain.com"
                }
                [PSCustomObject]@{
                    Punycode = "xn--mnchen-3ya.com"
                    Unicode  = "münchen.com"
                }
                [PSCustomObject]@{
                    Punycode = "xn--80aaxitdbjr.com"
                    Unicode  = "папитрока.com"
                }
                [PSCustomObject]@{
                    Punycode = "xn--caf-dma.com"
                    Unicode  = "café.com"
                }
            )

            $cert | Should -Not -BeNullOrEmpty
            $cert.DnsNameList | Should -HaveCount 6
            ($cert.DnsNameList | ConvertTo-Json -Compress)  | Should -BeExactly ($expectedDnsNameList | ConvertTo-Json -Compress)
        }
    }
}

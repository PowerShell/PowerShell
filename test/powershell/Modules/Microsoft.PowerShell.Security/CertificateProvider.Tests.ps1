# Copyright (c) Microsoft Corporation. All rights reserved.
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
        it "Should be able to get a certificate store, path: <path>" -TestCases $testLocations {
            param([string] $path)
            $expectedResolvedPath = Resolve-Path -LiteralPath $path
            $result = Get-Item -LiteralPath $path
            $result | should not be null
            $result | ForEach-Object {
                $resolvedPath = Resolve-Path $_.PSPath
                $resolvedPath.Provider | Should -Be $expectedResolvedPath.Provider
                $resolvedPath.ProviderPath.TrimStart('\') | Should -Be $expectedResolvedPath.ProviderPath.TrimStart('\')
            }
        }
        it "Should return two items at the root of the provider" {
            (Get-Item -Path cert:\*).Count | Should -Be 2
        }
        it "Should be able to get multiple items explictly" {
            (get-item cert:\LocalMachine , cert:\CurrentUser).Count | Should -Be 2
        }
        it "Should return PathNotFound when getting a non-existant certificate store" {
            {Get-Item cert:\IDONTEXIST -ErrorAction Stop} | Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.GetItemCommand"
        }
        it "Should return PathNotFound when getting a non-existant certificate" {
            {Get-Item cert:\currentuser\my\IDONTEXIST -ErrorAction Stop} | Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.GetItemCommand"
        }
    }
    Context "Get-ChildItem tests"{
        it "should be able to get a container using a wildcard" {
            (Get-ChildItem Cert:\CurrentUser\M?).PSPath | Should -Be 'Microsoft.PowerShell.Security\Certificate::CurrentUser\My'
        }
        it "Should return two items at the root of the provider" {
            (Get-ChildItem -Path cert:\).Count | Should -Be 2
        }
    }
}

Describe "Certificate Provider tests" -Tags "Feature" {
    BeforeAll{
        if($IsWindows)
        {
            Install-TestCertificates
            Push-Location Cert:\
        }
        else
        {
            # Skip for non-Windows platforms
            $defaultParamValues = $global:PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues = @{ "it:skip" = $true }
        }
    }

    AfterAll {
        if($IsWindows)
        {
            Remove-TestCertificates
            Pop-Location
        }
        else
        {
            $global:PSDefaultParameterValues = $defaultParamValues
        }
    }

    Context "Get-Item tests" {
        it "Should be able to get certifate by path: <path>" -TestCases $currentUserMyLocations {
            param([string] $path)
            $expectedThumbprint = (Get-GoodCertificateObject).Thumbprint
            $leafPath = Join-Path -Path $path -ChildPath $expectedThumbprint
            $cert = (Get-item -LiteralPath $leafPath)
            $cert | should not be null
            $cert.Thumbprint | Should -Be $expectedThumbprint
        }
        it "Should be able to get DnsNameList of certifate by path: <path>" -TestCases $currentUserMyLocations {
            param([string] $path)
            $expectedThumbprint = (Get-GoodCertificateObject).Thumbprint
            $expectedName = (Get-GoodCertificateObject).DnsNameList[0].Unicode
            $expectedEncodedName = (Get-GoodCertificateObject).DnsNameList[0].Punycode
            $leafPath = Join-Path -Path $path -ChildPath $expectedThumbprint
            $cert = (Get-item -LiteralPath $leafPath)
            $cert | should not be null
            $cert.DnsNameList | should not be null
            $cert.DnsNameList.Count | Should -Be 1
            $cert.DnsNameList[0].Unicode | Should -Be $expectedName
            $cert.DnsNameList[0].Punycode | Should -Be $expectedEncodedName
        }
        it "Should be able to get DNSNameList of certifate by path: <path>" -TestCases $currentUserMyLocations {
            param([string] $path)
            $expectedThumbprint = (Get-GoodCertificateObject).Thumbprint
            $expectedOid = (Get-GoodCertificateObject).EnhancedKeyUsageList[0].ObjectId
            $leafPath = Join-Path -Path $path -ChildPath $expectedThumbprint
            $cert = (Get-item -LiteralPath $leafPath)
            $cert | should not be null
            $cert.EnhancedKeyUsageList | should not be null
            $cert.EnhancedKeyUsageList.Count | Should -Be 1
            $cert.EnhancedKeyUsageList[0].ObjectId.Length | Should -Not -Be 0
            $cert.EnhancedKeyUsageList[0].ObjectId | Should -Be $expectedOid
        }
        it "Should filter to codesign certificates" {
            $allCerts = get-item cert:\CurrentUser\My\*
            $codeSignCerts = get-item cert:\CurrentUser\My\* -CodeSigningCert
            $codeSignCerts | should not be null
            $allCerts | should not be null
            $nonCodeSignCertCount = $allCerts.Count - $codeSignCerts.Count
            $nonCodeSignCertCount | Should -Not -Be 0
        }
        it "Should be able to exclude by thumbprint" {
            $allCerts = get-item cert:\CurrentUser\My\*
            $testThumbprint = (Get-GoodCertificateObject).Thumbprint
            $allCertsExceptOne = (Get-Item "cert:\currentuser\my\*" -Exclude $testThumbprint)
            $allCerts | should not be null
            $allCertsExceptOne | should not be null
            $countDifference = $allCerts.Count - $allCertsExceptOne.Count
            $countDifference | Should -Be 1
        }
    }
    Context "Get-ChildItem tests"{
        it "Should filter to codesign certificates" {
            $allCerts = get-ChildItem cert:\CurrentUser\My
            $codeSignCerts = get-ChildItem cert:\CurrentUser\My -CodeSigningCert
            $codeSignCerts | should not be null
            $allCerts | should not be null
            $nonCodeSignCertCount = $allCerts.Count - $codeSignCerts.Count
            $nonCodeSignCertCount | Should -Not -Be 0
        }
    }
}

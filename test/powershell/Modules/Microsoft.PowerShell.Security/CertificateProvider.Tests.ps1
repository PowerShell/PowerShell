# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Certificate Provider tests" -Tags "CI" {
    BeforeAll{
        if(!$IsWindows)
        {
            # Skip for non-Windows platforms
            $defaultParamValues = $global:PSDefaultParameterValues.Clone()
            $global:PSDefaultParameterValues = @{ "it:skip" = $true }
        }
        else
        {
            $testLocations = @(
                @{path = 'cert:\'}
                @{path = 'CERT:\'}
                @{path = 'Microsoft.PowerShell.Security\Certificate::'}
            )
            $currentUserMyLocations = @(
                @{path = 'Cert:\CurrentUser\my'}
                @{path = 'cert:\currentuser\my'}
                @{path = 'Microsoft.PowerShell.Security\Certificate::CurrentUser\My'}
                @{path = 'Microsoft.PowerShell.Security\certificate::currentuser\my'}
            )
        }
    }

    AfterAll {
        if(!$IsWindows)
        {
            $global:PSDefaultParameterValues = $defaultParamValues
        }
    }

    Context "Get-Item tests" {
        BeforeAll {
            function GetItemTestHelper {
                param([string] $path)
                $expectedResolvedPath = Resolve-Path -LiteralPath $path
                $result = Get-Item -LiteralPath $path
                $result | Should -Not -Be $null
                $result | ForEach-Object {
                    $resolvedPath = Resolve-Path $_.PSPath
                    $resolvedPath.Provider | Should -Be $expectedResolvedPath.Provider
                    $resolvedPath.ProviderPath.TrimStart('\') | Should -Be $expectedResolvedPath.ProviderPath.TrimStart('\')
                }
            }
        }
        It "Should be able to get a certificate store, path: <path>" -TestCases $currentUserMyLocations {
            param([string] $path)
            GetItemTestHelper $path
        }
        It "Should be able to get a certificate store, path: <path>" -TestCases $testLocations -Pending:$true {
            param([string] $path)
            GetItemTestHelper $path
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
            # The import and table creation work on non-windows, but are currently not needed
            Import-Module (Join-Path -Path $PSScriptRoot 'certificateCommon.psm1') -Force
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
        It "Should be able to get certifate by path: <path>" -TestCases $currentUserMyLocations {
            param([string] $path)
            $expectedThumbprint = (Get-GoodCertificateObject).Thumbprint
            $leafPath = Join-Path -Path $path -ChildPath $expectedThumbprint
            $cert = (Get-Item -LiteralPath $leafPath)
            $cert | Should -Not -Be $null
            $cert.Thumbprint | Should -Be $expectedThumbprint
        }
        It "Should be able to get DnsNameList of certifate by path: <path>" -TestCases $currentUserMyLocations {
            param([string] $path)
            $expectedThumbprint = (Get-GoodCertificateObject).Thumbprint
            $expectedName = (Get-GoodCertificateObject).DnsNameList[0].Unicode
            $expectedEncodedName = (Get-GoodCertificateObject).DnsNameList[0].Punycode
            $leafPath = Join-Path -Path $path -ChildPath $expectedThumbprint
            $cert = (Get-Item -LiteralPath $leafPath)
            $cert | Should -Not -Be $null
            $cert.DnsNameList | Should -Not -Be $null
            $cert.DnsNameList.Count | Should -Be 1
            $cert.DnsNameList[0].Unicode | Should -Be $expectedName
            $cert.DnsNameList[0].Punycode | Should -Be $expectedEncodedName
        }
        It "Should be able to get DNSNameList of certifate by path: <path>" -TestCases $currentUserMyLocations {
            param([string] $path)
            $expectedThumbprint = (Get-GoodCertificateObject).Thumbprint
            $expectedOid = (Get-GoodCertificateObject).EnhancedKeyUsageList[0].ObjectId
            $leafPath = Join-Path -Path $path -ChildPath $expectedThumbprint
            $cert = (Get-Item -LiteralPath $leafPath)
            $cert | Should -Not -Be $null
            $cert.EnhancedKeyUsageList | Should -Not -Be $null
            $cert.EnhancedKeyUsageList.Count | Should -Be 1
            $cert.EnhancedKeyUsageList[0].ObjectId.Length | Should -Not -Be 0
            $cert.EnhancedKeyUsageList[0].ObjectId | Should -Be $expectedOid
        }
        It "Should filter to codesign certificates" -Pending:$true {
            $allCerts = Get-Item cert:\CurrentUser\My\*
            $codeSignCerts = Get-Item cert:\CurrentUser\My\* -CodeSigningCert
            $codeSignCerts | Should -Not -Be $null
            $allCerts | Should -Not -Be $null
            $nonCodeSignCertCount = $allCerts.Count - $codeSignCerts.Count
            $nonCodeSignCertCount | Should -Not -Be 0
        }
        It "Should be able to exclude by thumbprint" {
            $allCerts = Get-Item cert:\CurrentUser\My\*
            $testThumbprint = (Get-GoodCertificateObject).Thumbprint
            $allCertsExceptOne = (Get-Item "cert:\currentuser\my\*" -Exclude $testThumbprint)
            $allCerts | Should -Not -Be $null
            $allCertsExceptOne | Should -Not -Be $null
            $countDifference = $allCerts.Count - $allCertsExceptOne.Count
            $countDifference | Should -Be 1
        }
    }
    Context "Get-ChildItem tests"{
        It "Should filter to codesign certificates" -Pending:$true {
            $allCerts = Get-ChildItem cert:\CurrentUser\My
            $codeSignCerts = Get-ChildItem cert:\CurrentUser\My -CodeSigningCert
            $codeSignCerts | Should -Not -Be $null
            $allCerts | Should -Not -Be $null
            $nonCodeSignCertCount = $allCerts.Count - $codeSignCerts.Count
            $nonCodeSignCertCount | Should -Not -Be 0
        }
    }
}

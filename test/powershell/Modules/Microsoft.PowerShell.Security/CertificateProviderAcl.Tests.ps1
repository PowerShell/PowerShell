# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

using namespace System.Security.AccessControl
using namespace System.Security.Cryptography
using namespace System.Security.Cryptography.X509Certificates
using namespace System.Security.Principal

$IsAdmin = $false
if ($IsWindows) {
    $IsAdmin = ([WindowsPrincipal][WindowsIdentity]::GetCurrent()).IsInRole([WindowsBuiltInRole]::Administrator)
}

$CertCases = @(
    @{ Case = 'RSA-CryptoAPI' }
    @{ Case = 'RSA-CNG' }
    @{ Case = 'ECDSA-CNG' }
)

Function New-RSACryptoAPICertificate {
    [OutputType([X509Certificate2])]
    [CmdletBinding()]
    param()

    $rsaParams = @{
        KeyAlgorithm = 'RSA'
        KeyExportPolicy = 'Exportable'
        KeyLength = 2048
        KeySpec = 'Signature'
        CertStoreLocation = 'Cert:\CurrentUser\My'
        Provider = 'Microsoft Base Cryptographic Provider v1.0'
        Subject = 'ACL Test RSA - CryptoAPI'

    }
    PKI\New-SelfSignedCertificate @rsaParams
}

Function New-RSACNGertificate {
    [OutputType([X509Certificate2])]
    [CmdletBinding()]
    param()

    $rsaParams = @{
        KeyAlgorithm = 'RSA'
        KeyExportPolicy = 'Exportable'
        KeyLength = 2048
        CertStoreLocation = 'Cert:\CurrentUser\My'
        Provider = 'Microsoft Software Key Storage Provider'
        Subject = 'ACL Test RSA - CNG'

    }
    PKI\New-SelfSignedCertificate @rsaParams
}

Function New-ECDSACNGCertificate {
    [OutputType([X509Certificate2])]
    [CmdletBinding()]
    param()

    $ecdsaParams = @{
        CertStoreLocation = 'Cert:\CurrentUser\My'
        KeyUsage = 'DigitalSignature'
        KeyAlgorithm = 'ECDSA_nistP256'
        CurveExport = 'CurveName'
        Subject = 'ACL Test ECDSA - CNG'
        Type = 'Custom'

    }
    PKI\New-SelfSignedCertificate @ecdsaParams
}

Function New-CertificateWithoutKey {
    [OutputType([X509Certificate2])]
    [CmdletBinding()]
    param()

    $request = [CertificateRequest]::new(
        "CN=ACL Test - NoKey",
        [System.Security.Cryptography.RSA]::Create(2048),
        "SHA256",
        [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)
    $notBefore = [DateTimeOffset]::UtcNow.AddDays(-1)
    $notAfter = $notBefore.AddDays(30)
    $testCert = $request.CreateSelfSigned($notBefore, $notAfter)
    $userStore = Get-Item Cert:\CurrentUser\My
    $userStore.Open('ReadWrite')
    $userStore.Add([X509Certificate2]::new($testCert.Export('Cert')))
    $userStore.Dispose()
    $certWithoutKey = Get-Item "Cert:\CurrentUser\My\$($testCert.Thumbprint)"
    $testCert.Dispose()

    $certWithoutKey
}

Describe "Certificate Provider tests" -Tags CI {
    BeforeAll {
        $certs = @{}

        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if (-not $IsWindows) {
            $PSDefaultParameterValues["It:Skip"] = $true
            return
        }

        $currentSid = [WindowsIdentity]::GetCurrent().User
        $currentAccount = $currentSid.Translate([NTAccount])

        $everyoneSid = [SecurityIdentifier]::new('S-1-1-0')
        $everyoneAccount = $everyoneSid.Translate([NTAccount])

        $certs['RSA-CryptoAPI'] = New-RSACryptoAPICertificate
        $certs['RSA-CNG'] = New-RSACNGertificate
        $certs['ECDSA-CNG'] = New-ECDSACNGCertificate
        $certs['RSA-NoKey'] = New-CertificateWithoutKey
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
        foreach ($cert in $certs.Values) {
            # We cannot pipe as -DeleteKey won't work.
            Remove-Item -LiteralPath $cert.PSPath -DeleteKey
        }
    }

    Context "*-Acl cmdlets" {
        It "Fails to get ACL when getting location" {
            $expected = "You cannot get an ACL for the certificate provider path 'CurrentUser', only certificate items are supported."
            { Get-Acl -Path Cert:\CurrentUser } | Should -Throw $expected
        }

        It "Fails to get ACL when getting store" {
            $expected = "You cannot get an ACL for the certificate provider path 'CurrentUser\My', only certificate items are supported."
            { Get-Acl -Path Cert:\CurrentUser\My } | Should -Throw $expected
        }

        It "Fails to get ACL on cert without a key" {
            $expected = "Failed to retrieve certificate key handle as certificate has no associated key: *"
            { Get-Acl -Path $certs['RSA-NoKey'].PSPath } | Should -Throw
            [string]$Error[0] | Should -BeLike $expected
        }

        It "Gets ACL for <Case> key" -TestCases $CertCases {
            param ($Case)
            $cert = $certs[$Case]

            $actual = $cert.PSPath | Get-Acl

            $actual | Should -BeOfType ([Microsoft.PowerShell.Commands.CertificateKeySecurity])
            $actual.Owner | Should -Not -BeNullOrEmpty  # Changes based on elevation
            $actual.Group | Should -BeOfType ([string])
            $actual.Access | Should -BeOfType ([AccessRule[Microsoft.PowerShell.Commands.CertificateKeyRights]])

            $userAccess = $actual.Access | Where-Object IdentityReference -EQ $currentAccount
            $userAccess | Should -Not -BeNullOrEmpty
            $userAccess.Rights | Should -BeOfType ([Microsoft.PowerShell.Commands.CertificateKeyRights])
            $userAccess.AccessControlType | Should -Be Allow
            $userAccess.InheritanceFlags | Should -Be None
            $userAccess.PropagationFlags | Should -Be None
        }

        It "Adds and Removes DACL for <Case> key" -TestCases $CertCases {
            param ($Case)
            $cert = $certs[$Case]

            $sd = $cert.PSPath | Get-Acl

            $newRule = $sd.AccessRuleType::new(
                $everyoneSid,
                'Read',
                'Allow')
            $sd.AddAccessRule($newRule)

            $sd | Set-Acl -LiteralPath $cert.PSPath
            $addActual = $cert.PSPath | Get-Acl
            $everyoneRule = $addActual.Access | Where-Object IdentityReference -EQ $everyoneAccount
            $everyoneRule | Should -Not -BeNullOrEmpty

            $sd.RemoveAccessRuleSpecific($newRule)
            $sd | Set-Acl -LiteralPath $cert.PSPath
            $removeActual = $cert.PSPath | Get-Acl
            $everyoneRule = $removeActual.Access | Where-Object IdentityReference -EQ $everyoneAccount
            $everyoneRule | Should -BeNullOrEmpty
        }

        It "Failed to get SACL for <Case> key - Non-Admin" -Skip:(-not $IsWindows -or $IsAdmin) -TestCases $CertCases {
            param ($Case)
            $cert = $certs[$Case]

            { $cert.PSPath | Get-Acl -Audit } | Should -Throw
            [string]$Error[0] | Should -BeLike 'Failed to retrieve certificate key security descriptor: *'
        }

        It "Failed to add SACL for <Case> key - Non-Admin" -Skip:(-not $IsWindows -or $IsAdmin) -TestCases $CertCases {
            param ($Case)
            $cert = $certs[$Case]

            $sd = $cert.PSPath | Get-Acl

            $newRule = $sd.AuditRuleType::new(
                $everyoneSid,
                'Read',
                'Success')
            $sd.AddAuditRule($newRule)

            { $sd | Set-Acl -LiteralPath $cert.PSPath } | Should -Throw
            [string]$Error[0] | Should -Be 'Failed to set certificate key security descriptor: A required privilege is not held by the client.'
        }
    }

    Context "Provider API" {
        It "Gets empty SD from path" {
            $actual = $ExecutionContext.InvokeProvider.SecurityDescriptor.NewFromPath($certs['RSA-CryptoAPI'].PSPath, "All")
            $actual | Should -BeOfType ([Microsoft.PowerShell.Commands.CertificateKeySecurity])
        }

        It "Fails to get SD from path - null path" {
            $expected = "*Cannot process argument because the value of argument ""path"" is null*"
            {
                $ExecutionContext.InvokeProvider.SecurityDescriptor.NewFromPath([NullString]::Value, "All")
            } | Should -Throw
            [string]$Error[0] | Should -BeLike $expected
        }

        It "Fails to get SD from path - invalid sections" {
            $sections = [Enum]::ToObject([System.Security.AccessControl.AccessControlSections], 20)
            $expected = "*Cannot process argument because the value of argument ""includeSections"" is not valid*"
            {
                $ExecutionContext.InvokeProvider.SecurityDescriptor.NewFromPath($certs['RSA-CryptoAPI'].PSPath, $sections)
            } | Should -Throw
            [string]$Error[0] | Should -BeLike $expected
        }

        It "Gets empty SD from type" {
            $actual = $ExecutionContext.InvokeProvider.SecurityDescriptor.NewOfType("certificate", "key", "All")
            $actual | Should -BeOfType ([Microsoft.PowerShell.Commands.CertificateKeySecurity])
        }

        It "Fails to get SD from type - invalid type" {
            $expected = "*. You cannot get Certificate SecurityDescriptor for the type 'invalid', only the 'Key' type is supported*"
            {
                $ExecutionContext.InvokeProvider.SecurityDescriptor.NewOfType("certificate", "invalid", "All")
            } | Should -Throw
            [string]$Error[0] | Should -BeLike $expected
        }

        It "Fails to get SD from type - invalid sections" {
            $sections = [Enum]::ToObject([System.Security.AccessControl.AccessControlSections], 20)
            $expected = "*Cannot process argument because the value of argument ""includeSections"" is not valid*"
            {
                $ExecutionContext.InvokeProvider.SecurityDescriptor.NewOfType("certificate", "Key", $sections)
            } | Should -Throw
            [string]$Error[0] | Should -BeLike $expected
        }

        It "Adds and Removes DACL for <Case> key" -TestCases $CertCases {
            param ($Case)
            $cert = $certs[$Case]

            $sd = $ExecutionContext.InvokeProvider.SecurityDescriptor.Get($cert.PSPath, 'Access')[0]

            $newRule = $sd.AccessRuleType::new(
                $everyoneSid,
                'Read',
                'Allow')
            $sd.AddAccessRule($newRule)

            try {
                $null = $ExecutionContext.InvokeProvider.SecurityDescriptor.Set($cert.PSPath, $sd)
            } catch {
                Get-Error | Out-Host
                throw
            }
            $addActual = $ExecutionContext.InvokeProvider.SecurityDescriptor.Get($cert.PSPath, 'Access')
            $everyoneRule = $addActual.Access | Where-Object IdentityReference -EQ $everyoneAccount
            $everyoneRule | Should -Not -BeNullOrEmpty

            $sd.RemoveAccessRuleSpecific($newRule)
            $null = $ExecutionContext.InvokeProvider.SecurityDescriptor.Set($cert.PSPath, $sd)
            $removeActual = $ExecutionContext.InvokeProvider.SecurityDescriptor.Get($cert.PSPath, 'Access')
            $everyoneRule = $removeActual.Access | Where-Object IdentityReference -EQ $everyoneAccount
            $everyoneRule | Should -BeNullOrEmpty
        }
    }
}

Describe "Certificate Provider tests - Admin" -Tags CI, RequireAdminOnWindows {
    BeforeAll {
        $certs = @{}
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if (-not $IsWindows -or -not $IsAdmin) {
            $PSDefaultParameterValues["It:Skip"] = $true
            return
        }

        $currentSid = [WindowsIdentity]::GetCurrent().User
        $currentAccount = $currentSid.Translate([NTAccount])

        $everyoneSid = [SecurityIdentifier]::new('S-1-1-0')
        $everyoneAccount = $everyoneSid.Translate([NTAccount])

        $certs['RSA-CryptoAPI'] = New-RSACryptoAPICertificate
        $certs['RSA-CNG'] = New-RSACNGertificate
        $certs['ECDSA-CNG'] = New-ECDSACNGCertificate
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
        foreach ($cert in $certs.Values) {
            # We cannot pipe as -DeleteKey won't work.
            Remove-Item -LiteralPath $cert.PSPath -DeleteKey
        }
    }

    It "Changes key owner for <Case> key" -TestCases $CertCases {
        param ($Case)
        $cert = $certs[$Case]

        $sd = $cert.PSPath | Get-Acl

        $oldOwner = $sd.GetOwner([SecurityIdentifier])
        $sd.SetOwner($everyoneSid)
        try {
            $sd | Set-Acl -LiteralPath $cert.PSPath

            $actual = $cert.PSPath | Get-Acl
            $actual.Owner | Should -Be $everyoneAccount.Value
        } finally {
            $sd.SetOwner($oldOwner)
            $sd | Set-Acl -LiteralPath $cert.PSPath
        }
    }

    It "Changes key group for <Case> key" -TestCases $CertCases {
        param ($Case)
        $cert = $certs[$Case]

        $sd = $cert.PSPath | Get-Acl

        $oldOwner = $sd.GetGroup([SecurityIdentifier])
        $sd.SetGroup($everyoneSid)
        try {
            $sd | Set-Acl -LiteralPath $cert.PSPath

            $actual = $cert.PSPath | Get-Acl
            $actual.Group | Should -Be $everyoneAccount.Value
        } finally {
            $sd.SetGroup($oldOwner)
            $sd | Set-Acl -LiteralPath $cert.PSPath
        }
    }

    It "Adds and Removes SACL for <Case> key" -TestCases $CertCases {
        param ($Case)
        $cert = $certs[$Case]

        # We should start off without any audit rules.
        $sd = $cert.PSPath | Get-Acl -Audit
        $sd.Audit | Should -BeNullOrEmpty

        $newRule = $sd.AuditRuleType::new(
            $everyoneSid,
            'Read',
            'Success')
        $sd.AddAuditRule($newRule)

        $sd | Set-Acl -LiteralPath $cert.PSPath
        $addActual = $cert.PSPath | Get-Acl -Audit
        $everyoneRule = $addActual.Audit | Where-Object IdentityReference -EQ $everyoneAccount
        $everyoneRule | Should -Not -BeNullOrEmpty

        $sd.RemoveAuditRuleSpecific($newRule)
        $sd | Set-Acl -LiteralPath $cert.PSPath
        $removeActual = $cert.PSPath | Get-Acl -Audit
        $everyoneRule = $removeActual.Audit | Where-Object IdentityReference -EQ $everyoneAccount
        $everyoneRule | Should -BeNullOrEmpty
    }
}

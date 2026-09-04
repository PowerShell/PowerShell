# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

using namespace System.Security.Cryptography.X509Certificates
using namespace System.Security.Cryptography

function New-CmsRecipient {
    [CmdletBinding(SupportsShouldProcess = $true)]
    [OutputType([System.Security.Cryptography.X509Certificates.X509Certificate2])]
    param([string]$Name, [Switch]$Invalid, [string]$OutPfxFile)
    $hash = [HashAlgorithmName]::SHA256
    $pad = [RSASignaturePadding]::Pkcs1
    $oids = [OidCollection]::new()
    $oids.Add("1.3.6.1.4.1.311.80.1") | Out-Null
    $ext1 = [X509KeyUsageExtension]::new([X509KeyUsageFlags]::DataEncipherment, $false)
    $ext2 = [X509EnhancedKeyUsageExtension]::new($oids, $false)
    $req = ([CertificateRequest]::new("CN=$Name", ([RSA]::Create(2048)), $hash, $pad))
    if (!$Invalid) { ($ext1, $ext2).ForEach( { $req.CertificateExtensions.Add($_) }) }
    $certTmp = $req.CreateSelfSigned([datetime]::Now.AddDays(-1), [datetime]::Now.AddDays(365))
    $certBytes = $certTmp.Export([X509ContentType]::Pfx, "tmp")
    [X509KeyStorageFlags[]]$flags = "PersistKeySet", "Exportable"
    $cert = [X509Certificate2]::new($certBytes, "tmp", $flags)
    if ($OutPfxFile) {
        $outfile = New-Item $OutPfxFile -Force
        [System.IO.File]::WriteAllBytes($outfile.FullName, $cert.Export([X509ContentType]::Pfx))
    }
    return $cert
}

Describe "CmsMessage cmdlets using X509 cert" -Tags "CI" {

    BeforeAll {
        Setup -Dir "certDir"
        Setup -File "vc1.pfx"
        Setup -File "vc2.pfx"
        Setup -File "certDir/vc3.pfx"
        Setup -File "message.txt" -Content "test"
        $file1 = "TestDrive:\vc1.pfx"
        $file2 = "TestDrive:\vc2.pfx"
        $messageFile = "TestDrive:\message.txt"
        $cipherFile = "TestDrive:\cipher.txt"
        $vc1 = New-CmsRecipient "ValidCms1" -OutPfxFile $file1
        $vc2 = New-CmsRecipient "ValidCms2" -OutPfxFile $file2
        $vc3 = New-CmsRecipient "ValidCms22" -OutPfxFile "TestDrive:\certDir\vc3.pfx"
        $ic = New-CmsRecipient "InvalidCms" -Invalid -OutPfxFile "TestDrive:\ic.pfx"
        $store = [X509Store]::new("My", [StoreLocation]::CurrentUser)
        $store.Open("ReadWrite")
        if (!$IsMacOS) {
            $store.Add($vc1)
            $store.Add($vc2)
            $store.Add($vc3)
        }
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
    }

    It "Cert Store: Encrypt/Decrypt using Subject" {
        "test" | Protect-CmsMessage -To $vc1.Subject | Unprotect-CmsMessage | Should -BeExactly "test"
        "test" | Protect-CmsMessage -To $vc1.Subject, $vc2.Subject | Unprotect-CmsMessage | Should -BeExactly "test"
    }

    It "Cert Store: Subject with wildcard (returns single cert)" {
        "test" | Protect-CmsMessage -To "*dCms1" | Unprotect-CmsMessage | Should -BeExactly "test"
    }

    It "Cert Store: Subject with wrong wildcard (returns multiple certs)" {
        { "test" | Protect-CmsMessage -To "*ValidCms*" -ErrorAction Stop } | Should -Throw -ErrorId 'IdentifierMustReferenceSingleCertificate'
    }

    It "Cert Store: Encrypt/Decrypt using Thumbprint" {
        "test" | Protect-CmsMessage -To $vc1.Thumbprint | Unprotect-CmsMessage | Should -BeExactly "test"
        "test" | Protect-CmsMessage -To $vc1.Thumbprint, $vc2.Thumbprint | Unprotect-CmsMessage | Should -BeExactly "test"
    }

    It "Cert Store: Encrypt/Decrypt subject and thumbprint" {
        "test" | Protect-CmsMessage -To $vc1.Thumbprint, $vc2.Subject | Unprotect-CmsMessage | Should -BeExactly "test"
    }

    It "Cert Store: removing test certificates" {
        $store.Remove($vc1)
        $store.Remove($vc2)
        $store.Remove($vc3)
        if ($IsMacOS) {
            $store.Remove($ic)
        }

        $store.Certificates.Find("FindByThumbprint", $vc1.Thumbprint, $false).Count | Should -BeExactly 0
        $store.Certificates.Find("FindByThumbprint", $vc2.Thumbprint, $false).Count | Should -BeExactly 0
        $store.Certificates.Find("FindByThumbprint", $vc3.Thumbprint, $false).Count | Should -BeExactly 0
        $store.Certificates.Find("FindByThumbprint", $ic.Thumbprint, $false).Count | Should -BeExactly 0
    }

    It "Encrypting with X509Cert" {
        "test" | Protect-CmsMessage -To $vc1 | Should -BeLike '-----BEGIN CMS*'
    }

    It "Encrypting with base64 string" {
        "test" | Protect-CmsMessage -To $certContent | Should -BeLike '-----BEGIN CMS*'
    }

    It "Encrypting with multiple X509Cert" {
        "test" | Protect-CmsMessage -To $vc1, $vc2 | Should -BeLike '-----BEGIN CMS*'
    }

    It "Decrypt with X509Cert" {
        "test" | Protect-CmsMessage -To $vc1 | Unprotect-CmsMessage -To $vc1 | Should -BeExactly "test"
    }

    It "Decrypt with multiple X509Cert" {
        "test" | Protect-CmsMessage -To $vc1, $vc2 | Unprotect-CmsMessage -To $vc1, $vc2 | Should -BeExactly "test"
    }

    It "Encrypt with invalid cert" {
        { "test" | Protect-CmsMessage -To $ic -ErrorAction Stop } | Should -Throw -ErrorId 'CertificateCannotBeUsedForEncryption'
    }

    It "Encrypt with valid and invalid" {
        { "test" | Protect-CmsMessage -To $vc1, $vc2, $ic -ErrorAction Stop } | Should -Throw -ErrorId 'CertificateCannotBeUsedForEncryption'
    }

    It "Encrypt/Decrypt from file" {
        Protect-CmsMessage -Path $messageFile -To $vc1 -OutFile $cipherFile
        $msg = Unprotect-CmsMessage -To $vc1 -Path $cipherFile
        $msg | Should -BeExactly "test"
    }

    It "Get-CmsMessage from content" {
        ("test" | Protect-CmsMessage -To $vc1 | Get-CmsMessage).Content | Should -BeLike '-----BEGIN CMS*'
    }

    It "Get-CmsMessage from file" {
        (Get-CmsMessage -Path $cipherFile).Content | Should -BeLike '-----BEGIN CMS*'
    }

    It "Encrypt With Single File" {
        "test" | Protect-CmsMessage -To $file1 | Unprotect-CmsMessage -To $file1 | Should -BeExactly "test"
    }

    It "Encrypt With Multiple Files" {
        $msg = "test" | Protect-CmsMessage -To $file1, $file2
        ($msg | Unprotect-CmsMessage -To $file1) | Should -BeExactly "test"
        ($msg | Unprotect-CmsMessage -To $file2) | Should -BeExactly "test"
    }

    It "Encrypt/Decrypt with Directory" {
        "test" | Protect-CmsMessage -To "TestDrive:\certDir" | Unprotect-CmsMessage -To "TestDrive:\certDir" | Should -BeExactly "test"
    }

    It "Decrypt with multiple files" {
        "test" | Protect-CmsMessage -To $vc1 | Unprotect-CmsMessage -To $file1, $file2 | Should -BeExactly "test"
    }

    AfterAll {
        $store.Dispose()
    }
}

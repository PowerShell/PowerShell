
Function Create-GoodCertificate
{
    $dataEnciphermentCert = "
MIIKYAIBAzCCCiAGCSqGSIb3DQEHAaCCChEEggoNMIIKCTCCBgoGCSqGSIb3DQEHAaCCBfsEggX3
MIIF8zCCBe8GCyqGSIb3DQEMCgECoIIE/jCCBPowHAYKKoZIhvcNAQwBAzAOBAgPOFDMBkCffQIC
B9AEggTYjY55RrmAhdj1grENxXjiPrVNdS++pb5UOn3M7O78BR0U1i2h5zvjkPjOwdLoOCbq5pgg
F0PKaMjHVu8EoZxSqsib17ptR5Rx5N23hseuJUzS8fTAHiBet9payNOJlPfkpuqMfQEmCAo9gAPz
w4RiyZNOA3NhxkfGl9yU4O9GSEr2koWKCUoCNelkIXVbkV728L7zSiWRSqRb7V4QJAtwtgPLTbl/
zo2SFhdNAGPeXbcOsKCv9trhuxPZ0FH4fukbXkHs0I3b5mYgMUI5Mds7UwT3wCtz+Ev9pLbmYN8X
NfH0tAK8ZGnQS1GcI4xCMEM8T9Tx3uwWY4arvRM3GTLwyt8JZEVZNTuYL9A9+RyeiO5d5xEKG8H4
snOCoTriT45tdl8hzMBCdc3jWxWiydmNRw47irifv5BX7BK/6FLAxkMRwACAxNO63ezG/OxuDDEz
ml+KzeZNvr4u4mTBcgZ49vMSyfRt/my+5+iLnSMGp4Rt0uix8489wctkxGlgyXGk23pA4Cj4hq/6
txopcl2gHn+DAFrHIgLg4JR8lcuOzBw8nFOrhK7iR9aMK21apxwImIaDCKJ1grOfbuElq4pkMpov
SltJe00WB94o69LibOg5LqpTrHW2/DY951sIgElF83FdUBhoZHasfCme/RxgliQf3QHedmENXSjr
8R8PAWX4wC0ZZVC0qcq5XP0PkwtuDKmfqq69R32nmBzpRrfypm9S+PfsYZeCeuROh6YKQ0ZBMnLb
8Y9povpXh0lYwVLuPanvAFiCT4vI7oRd1Mg1Zr9ZooMFAVomall1UnQQ29fvsoADjEDcPymVT80o
kTkw3NXnTX6fGZ94Eh0KZcuMgjTqIO3OKpH0lcaSBxlES4V0sO/mwP4ULy8l3dcnn440Nei33VDo
B7n2jhjJl/HvtltfEEEw1DW9AWDvkJDp878sD6VoyQZehvvxBNT0FwMr20TbVKeAGxf9n+xJ9Alo
VUS3qE7XnpxAAAR5L2OG+tMd4dyDSge1qrkVNZ3/uUzKKCZei2P7ICR9cX1FRsmcINdNfydIA2cA
Pmeq+UkRdqsJxt1NSK5bLvMM4EHRQZMMbXVKxxJ+kQDrzfQtERFPyd3Hdm2F4T/JXUQ+PrTQnRqY
LruTAiZfxygZuFrxJnNTRydRdbEaTAtMjFCMRFZ2wctJsgCb3yN9tt0JDYxIvm0MSehXiF+sCrl4
yZvvUzJqrgppHRTBR4Sao+MZ/rJ30vVU19Q3oBi9ikTqDY+4SrHsp5Y4llnsbrz0Web+h2jLvyJz
LgKuqs87qHhToVMLuULy/HLqY3m661EMwNqh5D76gSFI+TP2/rzT5mVOGglahFoc848o4cshtPWE
9MjAkDfsMbIfeKH3uh3D+eBIxYmZ5Cq2aHzqdQ0pU/nDNX7BDjC3E80VcQnXx4U6tRsQHsGtbcld
MFTp0yHJ2KLkz+inH3WPy/lYuVZ0QJe+LqvGt+bt1DgQmLBMD9WLFML3d0TtkuY3RhD5Y0wr2zt9
tT6WVTn8Hob1cJns4N7tDEr8Q3TdIar0I5Bzj3qoesJt+4lxwnVdUA1bNJ2zxXIkDfX/MTB464FI
2g9uhUs3lIOEiCjeJCwBebgZa1HlfhyCRu0E7fnNnKLaGWRs8LVy7MZIfe1kJoDVgTGB3TATBgkq
hkiG9w0BCRUxBgQEAQAAADBdBgkrBgEEAYI3EQExUB5OAE0AaQBjAHIAbwBzAG8AZgB0ACAAUwB0
AHIAbwBuAGcAIABDAHIAeQBwAHQAbwBnAHIAYQBwAGgAaQBjACAAUAByAG8AdgBpAGQAZQByMGcG
CSqGSIb3DQEJFDFaHlgAQwBlAHIAdABSAGUAcQAtAGEAYgA5AGUAZgA3AGYAOAAtAGUAYwA2AGEA
LQA0ADUAMwA4AC0AOQA4AGQAOAAtADAAYQA4AGUAYwA4ADUAOQBkADkAMgAxMIID9wYJKoZIhvcN
AQcGoIID6DCCA+QCAQAwggPdBgkqhkiG9w0BBwEwHAYKKoZIhvcNAQwBBjAOBAiYL6rZmAGN9QIC
B9CAggOw8eaNgIqx26SlOBKKQZ5O7NDZQHbytHTWn4ifNhVFUkbuaj22/VnYOFB9//8BLY6t0Dvw
X6wqXSMnbr1jOuUYaFdJzOBZBsYQfFTfoJ4iOb2jwwIpZSgTHeqgXbvnI7MIxauu6F4UseWVxt3u
ZhHjEZQjKWeC5mNCb6wX0IOQk96n1RJnJ3v1D4Q5YrVekOVq70VhRNtLOZMkrJV7vDMNlUYXD69D
PbcyPajVvETq0W98YNKB89oNwFWuKoMLNPWmSwIfn10oSEtybNEEr2IVgCBt2w2eb+nIDuA3c/Rc
qKPXwVGMzoUyAiGwTcueCdMRmiuQAuKCUyi9P/JqeIbgHtg15nAoGtw+l4MsFXfdMJjTCDd0WYff
l9ipaNnw8erCPquD0+wXeMnNivXaFzu2+CGwCSwbDl2M49HAQdtpKhNj5jKJBEP1GRQIk173gbEZ
n69IXUCsf0GDZiZVNbAQHBOuRoEHpBhendFgTJFAU2LDHlmV6OA0LYHaSn7CP/vOXOhWXJ1yGL2p
SeUepciwQV7sOHqDExWY82fd1kHSHcgCAkWLSSdIPlWhyeqjC1agSP6b74VK8uLRPkin5F9wGIPi
ewe/LsW3PTtDkDnj3DiSioKlQRUUxVxzi5qPBs+7vJEhbuO7UhtsMCWeUygDbw6n8BKan4w9iLhx
7/z6zVvQmLnK4HZChTPFuThRy1NctupoX7nE+CDgyhcmryaTDXohkviMWl4Od+8uGh8Quv2bHk6Y
UnFqNB/hSqYMkTuMLH4F9sVzoQEsYu96CDwbQaggbLMnPtmHKsPtzdnWQnys+oGT4uD8vl9xFdEW
AZdestrxbDK0La0AgGszUE+6B/GtOs2pv0fMXXYV2h+dAlwfz7oLxzm9E+SFgzviL+6PuI9fDHNd
pWeq/Rr5OpFb3rSotGTl84aIjk3hPd3uHujPQh8GO2EQ5k6p6ukk+a7gOUB+pH8fHihFl5L7pI0z
yRp0FvbZo//hmACYMvINoy2EQxjYLh7QLeE4qEr8bkzJVgEURUvcUpyHFJT6PGzUMqGx/Wjh2jJc
HfEDPMUDoTE/QRzLW7XrmQgJIRuHgPI/cqmOyvpEvuwdRhYyHEKktRO3tGjeflohDCyDW9bxOaJV
ZP64KBordM28ZHCQbnSdU0I5us6qiFX2PiLlBzRMH2ftUNMYReioqZyR+Xv5wjaoydV3//BDMH8M
1lh9GazUO8+OtzQEH0jiBi6ctlzFT8nNI2C+cOB9S3yMAjCEQa8wNzAfMAcGBSsOAwIaBBR96vF2
OksttXT1kXf+aez9EzDlsgQU4ck78h0WTy01zHLwSKNWK4wFFQM=
"

    $dataEnciphermentCert = $dataEnciphermentCert -replace '\s',''
    $certBytes = [Convert]::FromBase64String($dataEnciphermentCert)
    $certLocation = Join-Path $TestDrive "ProtectedEventLogging.pfx"
    [IO.File]::WriteAllBytes($certLocation, $certBytes)

    return $certLocation
}

Function Create-BadCertificate
{
    $codeSigningCert = "
MIIDAjCCAeqgAwIBAgIQW/oHcNaftoFGOYb4w5A0JTANBgkqhkiG9w0BAQsFADAZMRcwFQYDVQQD
DA5DTVNUZXN0QmFkQ2VydDAeFw0xNzAzMDcwNjEyMDNaFw0xODAzMDcwNjMyMDNaMBkxFzAVBgNV
BAMMDkNNU1Rlc3RCYWRDZXJ0MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAogOnCBPp
xCQXvCJOe4KUXrTL5hwzKSV/mA4pF/kWCVLseWkeTzll+02S0SzMR1oqyxGZOU+KiakmH8sIxDpS
pBpYi3R+JtaUYeK7IwvM7yMgxzYbVUFupNXDIdFcgd7FwX4R9wJGwd/hEw5fe+ua96G6bBlfQC8j
I8iHfqHZ2GmssIuSt72WhT6tKZhPJIMjwmKaB8j/gm6EC7eH83wNmVW/ss2AsG5cMT0Zmk2vkXPd
7rVYbAh9WcvxYzTYZLsPkXx/s6uanLo7pBPMqQ8fgImSXiD5EBO9d6SzqoagoAkH/l3oKCUztsqU
PAfTu1aTAYRW5O26AcICTbIYOMkDMQIDAQABo0YwRDAOBgNVHQ8BAf8EBAMCB4AwEwYDVR0lBAww
CgYIKwYBBQUHAwMwHQYDVR0OBBYEFLSLHDqLWoDBj0j/UavIf0hAHZ2YMA0GCSqGSIb3DQEBCwUA
A4IBAQB7GJ3ykI07k2D1mfiQ10+Xse4b6KXylbzYfJ1k3K0NEBwT7H/lhMu4yz95A7pXU41yKKVE
OzmpX8QGyczRM269+WpUvVOXwudQL7s/JFeZyEhxPYRP0JC8U9rur1iJeULvsPZJU2kGeLceTl7k
psuZeHouYeNuFeeKR66GcHKzqm+5odAJBxjQ/iGP+CVfNVX56Abhu8mXX6sFiorrBSV/NzPThqja
mtsMC3Fq53xANMjFT4kUqMtK+oehPf0+0jHHra4hpCVZ2KoPLLPxpJPko8hUO5LxATLU+UII7w3c
nMbw+XY4C8xdDnHfS6mF+Hol98dURB/MC/x3sZ3gSjKo
"

    $codeSigningCert = $codeSigningCert -replace '\s',''
    $certBytes = [Convert]::FromBase64String($codeSigningCert)
    $certLocation = Join-Path $TestDrive "CMSTestBadCert"
    [IO.File]::WriteAllBytes($certLocation, $certBytes)

    return $certLocation
}


Describe "CmsMessage cmdlets and Get-PfxCertificate basic tests" -Tags "CI" {
    
    BeforeAll {
        $certLocation = Create-GoodCertificate
        $certLocation | Should Not BeNullOrEmpty | Out-Null
    }

    It "Verify Get-PfxCertificate -FilePath" {
        $cert = Get-PfxCertificate -FilePath $certLocation
        $cert.Subject | Should Be "CN=MyDataEnciphermentCert"
    }

    It "Verify Get-PfxCertificate -LiteralPath" {
        $cert = Get-PfxCertificate -LiteralPath $certLocation
        $cert.Subject | Should Be "CN=MyDataEnciphermentCert"
    }

    It "Verify Get-PfxCertificate positional argument" {
        $cert = Get-PfxCertificate $certLocation
        $cert.Subject | Should Be "CN=MyDataEnciphermentCert"
    }

    It "Verify CMS message recipient resolution by path" -Skip:(!$IsWindows) {
        $errors = $null
        $recipient = [System.Management.Automation.CmsMessageRecipient] $certLocation
        $recipient.Resolve($ExecutionContext.SessionState, "Encryption", [ref] $errors)

        $recipient.Certificates.Count | Should Be 1
        $recipient.Certificates[0].Subject | Should Match 'CN=MyDataEnciphermentCert'
    }

    It "Verify CMS message recipient resolution by cert" -Skip:(!$IsWindows) {
        $errors = $null
        $cert = Get-PfxCertificate $certLocation
        $recipient = [System.Management.Automation.CmsMessageRecipient] $cert
        $recipient.Resolve($ExecutionContext.SessionState, "Encryption", [ref] $errors)

        $recipient.Certificates.Count | Should Be 1
        $recipient.Certificates[0].Subject | Should Match 'CN=MyDataEnciphermentCert'
    }

    It "Verify a CMS message can be protected / unprotected" -Skip:(!$IsWindows) {
        $protected = "Hello World","How are you?" | Protect-CmsMessage -To $certLocation
        $protected.IndexOf("-----BEGIN CMS-----") | Should Be 0

        $message = $protected | Get-CmsMessage
        $message.Recipients.Count | Should Be 1
        $message.Recipients[0].IssuerName | Should Be "CN=MyDataEnciphermentCert"

        $expected = "Hello World" + [System.Environment]::NewLine + "How are you?"
        $decrypted = $message | Unprotect-CmsMessage -To $certLocation
        $decrypted | Should Be $expected

        $decrypted = $protected | Unprotect-CmsMessage -To $certLocation
        $decrypted | Should Be $expected
    }
}


Describe "CmsMessage cmdlets thorough tests" -Tags "Feature" {

    BeforeAll {
        if ($IsWindows)
        {
            $certLocation = Create-GoodCertificate
            $certLocation | Should Not BeNullOrEmpty | Out-Null

            $badCertLocation = Create-BadCertificate
            $badCertLocation | Should Not BeNullOrEmpty | Out-Null

            if ($IsCoreCLR)
            {
                # PKI module is not available for PowerShell Core, so we need to use Windows PowerShell to import the cert
                $fullPowerShell = Join-Path "$env:SystemRoot" "System32\WindowsPowerShell\v1.0\powershell.exe"

                try {
                    $modulePathCopy = $env:PSModulePath
                    $env:PSModulePath = $null

                    $command = @"
Import-PfxCertificate $certLocation -CertStoreLocation cert:\CurrentUser\My | % PSPath
Import-Certificate $badCertLocation -CertStoreLocation Cert:\CurrentUser\My | % PSPath
"@
                    $certPaths = & $fullPowerShell -NoProfile -NonInteractive -Command $command
                    $certPaths.Count | Should Be 2 | Out-Null

                    $importedCert = Get-ChildItem $certPaths[0]
                    $testBadCert  = Get-ChildItem $certPaths[1]
                } finally {
                    $env:PSModulePath = $modulePathCopy
                }
            }
            else
            {
                $importedCert = Import-PfxCertificate $certLocation -CertStoreLocation cert:\CurrentUser\My
                $testBadCert = Import-Certificate $badCertLocation -CertStoreLocation Cert:\CurrentUser\My
            }
        }
        else
        {
            # Skip for non-Windows platforms
            $defaultParamValues = $PSdefaultParameterValues.Clone()
            $PSdefaultParameterValues = @{ "it:skip" = $true }
        }
    }

    AfterAll {
        if ($IsWindows)
        {
            if ($importedCert)
            {
                Remove-Item (Join-Path Cert:\CurrentUser\My $importedCert.Thumbprint) -Force -ErrorAction SilentlyContinue
            }
            if ($testBadCert)
            {
                Remove-Item (Join-Path Cert:\CurrentUser\My $testBadCert.Thumbprint) -Force -ErrorAction SilentlyContinue
            }
        }
        else
        {
            $PSdefaultParameterValues = $defaultParamValues
        }
    }

    It "Verify message recipient resolution by Base64Cert" {
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

            $errors = $null
            $recipient = [System.Management.Automation.CmsMessageRecipient] $certContent
            $recipient.Resolve($ExecutionContext.SessionState, "Encryption", [ref] $errors)

            $recipient.Certificates.Count | Should Be 1
            $recipient.Certificates[0].Subject | Should Match 'CN=MyDataEnciphermentCert'
    }

    It "Verify wildcarded recipient resolution by path [Decryption]" {
        $errors = $null
        $recipient = [System.Management.Automation.CmsMessageRecipient] ($certLocation + "*")
        $recipient.Resolve($ExecutionContext.SessionState, "Decryption", [ref] $errors)

        # Should have resolved single cert
        $recipient.Certificates.Count | Should Be 1
    }

    It "Verify wildcarded recipient resolution by path [Encryption]" {
        $errors = $null
        $recipient = [System.Management.Automation.CmsMessageRecipient] ($certLocation + "*")
        $recipient.Resolve($ExecutionContext.SessionState, "Encryption", [ref] $errors)

        $recipient.Certificates.Count | Should Be 1
    }

    It "Verify resolution by directory" {
        $protectedEventLoggingCertPath = Join-Path $TestDrive ProtectedEventLoggingDir
        $null = New-Item -ItemType Directory $protectedEventLoggingCertPath -Force
        Copy-Item $certLocation $protectedEventLoggingCertPath
        Copy-Item $certLocation (Join-Path $protectedEventLoggingCertPath "SecondCert.pfx")
        Copy-Item $certLocation (Join-Path $protectedEventLoggingCertPath "ThirdCert.pfx")

        $errors = $null
        $recipient = [System.Management.Automation.CmsMessageRecipient] $protectedEventLoggingCertPath
        $recipient.Resolve($executionContext.SessionState, "Decryption", [ref] $errors)

        $recipient.Certificates.Count | Should Be 1
    }

    It "Verify resolution by thumbprint" {
        $errors = $null
        $recipient = [System.Management.Automation.CmsMessageRecipient] $importedCert.Thumbprint
        $recipient.Resolve($ExecutionContext.SessionState, "Decryption", [ref] $errors)

        # "Should have certs from thumbprint in 'My' store"
        $recipient.Certificates.Count | Should Be 1
        $recipient.Certificates[0].Thumbprint | Should Be $importedCert.Thumbprint
    }

    It "Verify resolution by subject name" {
        $errors = $null
        $recipient = [System.Management.Automation.CmsMessageRecipient] $importedCert.Subject
        $recipient.Resolve($ExecutionContext.SessionState, "Decryption", [ref] $errors)

        $recipient.Certificates.Count | Should Be 1
        $recipient.Certificates[0].Thumbprint | Should Be $importedCert.Thumbprint
    }

    It "Verify error when no cert found in encryption for encryption" {
        $errors = $null
        $recipient = [System.Management.Automation.CmsMessageRecipient] "SomeCertificateThatDoesNotExist*"
        $recipient.Resolve($ExecutionContext.SessionState, "Encryption", [ref] $errors)

        $errors.Count | Should Be 1
        $errors[0].FullyQualifiedErrorId | Should Be "NoCertificateFound"
    }

    It "Verify error when encrypting to non-wildcarded identifier for decryption" {
        $errors = $null
        $recipient = [System.Management.Automation.CmsMessageRecipient] "SomeCertificateThatDoesNotExist"
        $recipient.Resolve($ExecutionContext.SessionState, "Decryption", [ref] $errors)

        $errors.Count | Should Be 1
        $errors[0].FullyQualifiedErrorId | Should Be "NoCertificateFound"
    }

    It "Verify error when encrypting to wrong cert" {
        $errors = $null
        $recipient = [System.Management.Automation.CmsMessageRecipient] $testBadCert.Thumbprint
        $recipient.Resolve($ExecutionContext.SessionState, "Encryption", [ref] $errors)

        $errors.Count | Should Be 1
        $errors[0].FullyQualifiedErrorId | Should Be "CertificateCannotBeUsedForEncryption"
    }

    It "Verify no error when encrypting to wildcarded identifier for decryption" {
        $errors = $null
        $recipient = [System.Management.Automation.CmsMessageRecipient] "SomeCertificateThatDoesNotExist*"
        $recipient.Resolve($ExecutionContext.SessionState, "Decryption", [ref] $errors)

        $errors | Should Be $null
        $recipient.Certificates.Count | Should Be 0
    }

    It "Verify Protect-CmsMessage emits recipient errors" {
        try {
            "Hello World" | Protect-CmsMessage -To "SomeThumbprintThatDoesNotExist" -ErrorAction Stop
            throw "No Exception!"
        } catch {
            $_.FullyQualifiedErrorId | Should Be "NoCertificateFound,Microsoft.PowerShell.Commands.ProtectCmsMessageCommand"
        }
    }

    It "Verify CmsMessage cmdlets works with paths" {
        try {
            $randomNum = Get-Random -Minimum 1000 -Maximum 9999
            $tempPath = Join-Path $TestDrive "$randomNum-Path-Test-File"
            $encryptedPath = $tempPath + ".encrypted.txt"
            "Hello World","How are you?" | Set-Content $tempPath

            Protect-CmsMessage -Path $tempPath -To $certLocation -OutFile $encryptedPath

            $message = Get-CmsMessage -LiteralPath $encryptedPath
            $message.Recipients.Count | Should Be 1
            $message.Recipients[0].IssuerName | Should Be "CN=MyDataEnciphermentCert"

            $expected = "Hello World" + [System.Environment]::NewLine + "How are you?" + [System.Environment]::NewLine
            $decrypted = $message | Unprotect-CmsMessage -To $certLocation
            $decrypted | Should Be $expected

            $decrypted = Unprotect-CmsMessage -Path $encryptedPath -To $certLocation
            $decrypted | Should Be $expected
        } finally {
            Remove-Item $tempPath, $encryptedPath -Force -ErrorAction SilentlyContinue
        }
    }

    It "Verify Unprotect-CmsMessage works with local store" {
        try {
            $randomNum = Get-Random -Minimum 1000 -Maximum 9999
            $tempPath = Join-Path $TestDrive "$randomNum-Path-Test-File"
            "Hello World" | Protect-CmsMessage -To $certLocation -OutFile $tempPath

            # Decrypt using $importedCert in the Cert store
            $decrypted = Unprotect-CmsMessage -Path $tempPath
            $decrypted | Should Be "Hello World"
        } finally {
            Remove-Item $tempPath -Force -ErrorAction SilentlyContinue
        }
    }

    It "Verify Unprotect-CmsMessage emits recipient errors" {
        try {
            "" | Unprotect-CmsMessage -To "SomeThumbprintThatDoesNotExist" -IncludeContext -ErrorAction Stop
            throw "No Exception!"
        } catch {
            $_.FullyQualifiedErrorId | Should Be "NoCertificateFound,Microsoft.PowerShell.Commands.UnprotectCmsMessageCommand"
        }
    }

    It "Verify failure to extract Ascii armor generates an error [Unprotect-CmsMessage]" {
        try {
            "Hello World" | Unprotect-CmsMessage -ErrorAction Stop
            throw "No Exception!"
        } catch {
            $_.FullyQualifiedErrorId | Should Be "InputContainedNoEncryptedContentIncludeContext,Microsoft.PowerShell.Commands.UnprotectCmsMessageCommand"
        }
    }

    It "Verify failure to extract Ascii armor generates an error [Get-CmsMessage]" {
        try {
            "Hello World" | Get-CmsMessage -ErrorAction Stop
            throw "No Exception!"
        } catch {
            $_.FullyQualifiedErrorId | Should Be "InputContainedNoEncryptedContent,Microsoft.PowerShell.Commands.GetCmsMessageCommand"
        }
    }

    It "Verify 'Unprotect-CmsMessage -IncludeContext' with no encrypted input" {
        # Should have round-tripped content
        $result = "Hello World" | Unprotect-CmsMessage -IncludeContext
        $result | Should Be "Hello World"
    }

    It "Verify Unprotect-CmsMessage lets you include context" {
        $protected = "Hello World" | Protect-CmsMessage -To $certLocation
        $adjustedProtected = "Pre content" + [System.Environment]::NewLine + $protected + [System.Environment]::NewLine + "Post content"

        $decryptedNoContext = $adjustedProtected | Unprotect-CmsMessage -To $certLocation
        $decryptedWithContext = $adjustedProtected | Unprotect-CmsMessage -To $certLocation -IncludeContext

        $decryptedNoContext | Should Be "Hello World"

        $expected = "Pre content" + [System.Environment]::NewLine + "Hello World" + [System.Environment]::NewLine + "Post content"
        $decryptedWithContext | Should Be $expected
    }

    It "Verify Unprotect-CmsMessage treats event logs as a first class citizen" {
        $protected = "Encrypted Message1","Encrypted Message2" | Protect-CmsMessage -To $certLocation
        $virtualEventLog = Get-WinEvent Microsoft-Windows-PowerShell/Operational -MaxEvents 1
        $savedId = $virtualEventLog.Id
        $virtualEventLog.Message = $protected

        $expected = "Encrypted Message1" + [System.Environment]::NewLine + "Encrypted Message2"
        $decrypted = $virtualEventLog | Unprotect-CmsMessage -To $certLocation
        $decrypted | Should Be $expected

        $processed = $virtualEventLog | Unprotect-CmsMessage -To $certLocation -IncludeContext
        $processed.Id | Should Be $savedId
        $processed.Message | Should Be $expected
    }

    It "Verify -DocumentEncryptionCert parameter works" {
        $foundCerts = Get-ChildItem Cert:\CurrentUser -Recurse -DocumentEncryptionCert

        # Validate they all match the EKU
        $correctMatching = $foundCerts | ? {
            ($_.EnhancedKeyUsageList.Count -gt 0) -and 
            ($_.EnhancedKeyUsageList[0].ObjectId -eq '1.3.6.1.4.1.311.80.1')
        }
        # "All Document Encryption Cert should have had correct EKU"
        @($foundCerts).Count | Should Be @($correctMatching).Count
    }

    It "Verify protect message using OutString" {
        $protected = Get-Process -Id $pid | Protect-CmsMessage -To $certLocation
        $decrypted = $protected | Unprotect-CmsMessage -To $certLocation
        # Should have had PID in output
        $decrypted | Should Match $pid
    }
}

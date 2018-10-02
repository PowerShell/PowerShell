# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Function New-GoodCertificate
{
    <#
        .NOTES
            This certificate is only issued for the following Key Usages:
                Key Encipherment
                Data Encipherment (30)
                Document Encryption (1.3.6.1.4.1.311.80.1)
            It Cannot be use for SSL/TLS Client Authentication
    #>
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

Function New-CertificatePassword
{
    $script:protectedCertPassword = ConvertTo-SecureString -Force -AsPlainText (New-RandomHexString)
    return $script:protectedCertPassword
}

Function Get-CertificatePassword
{
    if ($null -eq $script:protectedCertPassword)
    {
        throw [System.InvalidOperationException] "`$script:protectedCertPassword is not defined. Call New-CertificatePassword first."
    }
    return $script:protectedCertPassword
}

Function New-ProtectedCertificate
{
    <#
    .SYNOPSIS
    Return existing password-protected pfx certificate

    .NOTES
    Password: "password"
    #>

    $certLocation = Join-Path ([System.IO.Path]::GetTempPath()) 'protectedCert.pfx'

    $password = New-CertificatePassword

    $null = SelfSignedCertificate\New-SelfSignedCertificate `
        -CommonName 'localhost' `
        -OutCertPath $certLocation `
        -Passphrase $password `
        -Force

    return $certLocation
}

Function New-BadCertificate
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

function Install-TestCertificates
{
    $script:certLocation = New-GoodCertificate
    $script:certLocation | Should -Not -BeNullOrEmpty | Out-Null

    $script:badCertLocation = New-BadCertificate
    $script:badCertLocation | Should -Not -BeNullOrEmpty | Out-Null

    if ($IsCoreCLR -and $IsWindows)
    {
        # PKI module is not available for PowerShell Core, so we need to use Windows PowerShell to import the cert
        $fullPowerShell = Join-Path "$env:SystemRoot" "System32\WindowsPowerShell\v1.0\powershell.exe"

        try {
            $modulePathCopy = $env:PSModulePath
            $env:PSModulePath = $null

            $command = @"
Import-PfxCertificate $script:certLocation -CertStoreLocation cert:\CurrentUser\My | ForEach-Object PSPath
Import-Certificate $script:badCertLocation -CertStoreLocation Cert:\CurrentUser\My | ForEach-Object PSPath
"@
            $certPaths = & $fullPowerShell -NoProfile -NonInteractive -Command $command
            $certPaths.Count | Should -Be 2 | Out-Null

            $script:importedCert = Get-ChildItem $certPaths[0]
            $script:testBadCert  = Get-ChildItem $certPaths[1]
        } finally {
            $env:PSModulePath = $modulePathCopy
        }
    }
    elseif($IsWindows)
    {
        $script:importedCert = Import-PfxCertificate $script:certLocation -CertStoreLocation cert:\CurrentUser\My
        $script:testBadCert = Import-Certificate $script:badCertLocation -CertStoreLocation Cert:\CurrentUser\My
    }
    else {
        throw 'Not supported on non-windows platforms'
    }
}

function Get-GoodCertificateLocation
{
    return $script:certLocation
}

function Get-GoodCertificateObject
{
    return $script:importedCert
}

function Get-BadCertificateObject
{
    return $script:testBadCert
}

function Remove-TestCertificates
{
    if($IsWindows)
    {
        if ($script:importedCert)
        {
            Remove-Item (Join-Path Cert:\CurrentUser\My $script:importedCert.Thumbprint) -Force -ErrorAction SilentlyContinue
        }
        if ($script:testBadCert)
        {
            Remove-Item (Join-Path Cert:\CurrentUser\My $script:testBadCert.Thumbprint) -Force -ErrorAction SilentlyContinue
        }
    }
    else {
        throw 'Not supported on non-windows platforms'
    }
}

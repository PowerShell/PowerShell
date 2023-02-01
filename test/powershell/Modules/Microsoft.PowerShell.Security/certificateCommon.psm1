# Copyright (c) Microsoft Corporation.
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

Function New-GoodServerCertificate
{
    <#
        .NOTES
            This certificate properties:
                Subject:
                    CN = MyDataEnciphermentCert
                Subject Alternative Name:
                    DNS Name=www.fabrikam.com
                    DNS Name=www.contoso.com
                EnhancedKey Usage:
                    Client Authentication (1.3.6.1.5.5.7.3.2)
                    Server Authentication (1.3.6.1.5.5.7.3.1)
                    Document Encryption (1.3.6.1.4.1.311.80.1)
                Key Usage:
                    Key Encipherment, Data Encipherment (30)
                Thumbprint:
                    b79428ca5aa0f0620e5eba19223fdf7885fcf3c6
                Serial Number:
                    40c14ec2f84344be4965954d091c266b

            Howto update:
            1. Import the module and test certificates
                Import-Module certificateCommon.psm1 -Force
                Install-TestCertificates
            2. Read the certificate
                $cert = Get-Item Cert:\CurrentUser\My\<Thumbprint>
            3. Clone the certificate with new properties:
                $b=New-SelfSignedCertificate -CloneCert $cert -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2,1.3.6.1.5.5.7.3.1,1.3.6.1.4.1.311.80.1") -CertStoreLocation "Cert:\CurrentUser\My" -DnsName "www.fabrikam.com", "www.contoso.com"
                $b.SerialNumber
            4. Export new certificate to file and encode to Base64 (press Enter key at password prompt):
                certutil -exportpfx -user  my <Serial> C:\temp\newcert.pfx
                certutil -encode C:\temp\newcert.pfx C:\temp\newcert.txt
            5. Replace $dataEnciphermentCert with new value from C:\temp\newcert.txt
    #>
    $dataEnciphermentCert = "
MIIKmgIBAzCCClYGCSqGSIb3DQEHAaCCCkcEggpDMIIKPzCCBgAGCSqGSIb3DQEH
AaCCBfEEggXtMIIF6TCCBeUGCyqGSIb3DQEMCgECoIIE/jCCBPowHAYKKoZIhvcN
AQwBAzAOBAi0Gq/DrqxgYgICB9AEggTYy168efMBdfynAWPkh7S6VyhoaMOWR6Nt
l6MnGcWuGGFxLS2TxfZSN+mNYsAGG0yGnTyYZ/2uCW46irfMNPnq1OrYjWJB4Zmb
pRF2WI+ipVkKUSVtuCKR8GQR9Yw7C3PASqgm0JV1YpK9fw0EJ4tzeF4w81xzb5Ix
jm8jOwSBefYXjOPXwIvqVntOQHkMj75EVEjyTi/Sb3SoYk0b7uzqFsqtiQ3S9rtp
FuM3gRNY6zjHYa/iDrwt4aty8IU74yipyLj/9c4+BnaBiMCwX51FrHYGx1OYkcbN
K0iTWzxG3oYFxY9V9P6uzUqDUTsZNawkjne2WIFJrfPq25nU8vMHej0T/YqMczPF
6NtiHv0yMFNqxEh6vZgc00gitEV+Cbr9/lsctJ2YiYFW2Sc5q7wovc231xrLaDXZ
BLdnJZqqalobqdwfu84o7GiXw7y9pdvwNl4vKH4yiDv4kqCcuzqLxJ7QTk63EuGw
uiEhoC1ibvvi7+fCm60IxHxtGlUAZCaEidh70/VffRkea+oor9WLSIZ7q07ycB1+
2k0ecpeutaOPAckfIPLDb1m9BwlM47dVaSJNN5/5oQsRM7327TDjYphfVoUDX19p
eAiRcO68GbcmXQQuM8CxbCSdPC3F6Jr5YzMBnM6Yqacv8ywdYaZHmKBADF2FtuFb
zyFHFCzTYJyj3YPczK1Xmw7d4btJuIAvl5JOrbDqSEbwRvXZL2f5iIRydekcPnma
nu/mlRqupQWiasv7qoga8Gq5sKMIUAruTwAzhkNA01hj5Iv7NcFY9Ruc+MHmnGmE
BiDSh/wOuyRIGIfveR9e4msg6rP9KO7q6mnghBVy6Is6Xz5Ak/xS2v7RjxzgBxrE
LaPoeHpD2+LKN+w8eN9Pd2ewAwwHzrw65jN7P1avuioMi0KejYZZAt3scmIGTT+n
jzcmxKvAPYllRifZRoDe+hAeYHlGAA8hm/zmw5522L8hOR1j69JcbCbl8MtWww+W
2MiGYbEbbGg/HV+9PnMMU3i+eHtuxTukBYb1ksH5z7742WBYH+gE3CvPX5AZRY/N
rVo8qZPH2ZK1smmc7v2cv0ZhIGWiMe+LakjjB1QcFpCTh/uwhQhNGXOgU1oNKbuE
FE7iH3XHkDVLFxEhdtgvwXJIQp+7ROVry6sZW2cgi2yUOTWNjqDd6LlV8vy+lti6
/7L+rhfezHUpk89eWu8glEFqB96xmc9nbSF0On8aewdb43gW8h5TbBuSE1JA5SfE
cQNSxs4z+Bbx7cyX+f3CDriJkzXs5PvT861pNcYB1zjxJBStehMeazWf+D2Td10p
43nmEYaXHq/KwY1jn8yzQyj2V+c5Csw75879KHZ+6LwfzeWDCUlma+n1ZDNeDm2+
s40qBC4s7x9sEdUBQwl/JOtJt4ZlFje4xle5/RlS2il/e/X05n4XUuX7azWZjSoG
fflcIvMcnXydt/Nl05newUGobDLEw9sa9lDy8bG6+IGywg2x1A4hqsA2qUbVLyy/
VLdhIY38+FmcliL3o3uk9vsSHefODKrG1ZD+qu3+/9s3B7KmlT652cj6+fi54+N/
jM/VAT/6e8OntolNHVoauudBmgO0WRYbznvZd/C6ehTAZQo9HKiGkADk1i8Gw6NF
dgH4LZdHyWOgrvAL1FqOqzGB0zATBgkqhkiG9w0BCRUxBgQEAQAAADBdBgkqhkiG
9w0BCRQxUB5OAHQAZQAtAGYAZgBiADIAYQBiADMAYgAtAGMAZQA5ADEALQA0ADEA
MABhAC0AOAA1AGMAMQAtADkAYwBmADgAYwA3AGEAMAA5ADkAYwA4MF0GCSsGAQQB
gjcRATFQHk4ATQBpAGMAcgBvAHMAbwBmAHQAIABTAHQAcgBvAG4AZwAgAEMAcgB5
AHAAdABvAGcAcgBhAHAAaABpAGMAIABQAHIAbwB2AGkAZABlAHIwggQ3BgkqhkiG
9w0BBwagggQoMIIEJAIBADCCBB0GCSqGSIb3DQEHATAcBgoqhkiG9w0BDAEDMA4E
CNnzLxDoo0cMAgIH0ICCA/ChXpj5kGwmqH++L8JmdidMyhQAk//fnIxsE695lW4B
yUQ0wM7k2eWSdebuCMSvD1bL2A8B6qM/sfkuoAUHrSGZS56Qeh4C3j5FqLyMOg1H
7w4hkHDYTQp4s9fxk5NedqsctmmKnZmrET65g8KRMSiolFYqd69D1SWGnftUVMvU
MdrFQRP0GJPSvDzx17NJWRRiUXzYxakfpGW8QfV0I9/ZlP8uUEZlVqc1v7ikQf1e
A3i6+njdj9lkpa2CEdtAdpwVTpQ49UwGq4tD5aMlzjWNuzQpP0mEh9XzWr6J3aFz
poEpCuC7gT4tIZcC3BKS9Uvv53AShpxJCiWwG7k+CYzzKabq3guJh1uPKMqpHJGG
T/5a4lQJOQr2IyfoDfsNw2JtAVjM62haUap4ZZbuoJq9B6gECknUAbSsk1XdEktq
OhMP5HtlCEAdLeEo0ae3YGRHsKJgFSx/8R+MMdjMWMYT8+6mVSVC63KQMiBmVEBs
mDcOWY45eCHo4aXuEKcHZSxRtWvviACdEd/CSDGwFGuA8f+D0f6iLabQCLLeVRVN
iUiKER3Adw+dg6pegifM4r3trqR2/H1Z33TrarGMKGNJSy6ur8bW1L6aYjxx1hj+
dW9Tcj1QTLC1PJFaLdOD1ELho/EXdoa9VPuC2MWS9I6TqpY4Rkrcbr4KMqDLUTeN
SzgSZARVPv7VL8EDqeME24aQammby6nr25tA+Q45KBH8nc/hURVcoayl/cmH9Up3
D87oCdVuCvmiJnFhZnd7q6S3ChfIQhyGNlZQjRjDve9MhR0TwaDEm0nsBpSwSVwP
PyJ00Md/cq4nImXvMYXf1fZ3Tlp90kZ2ffQ9+EMzFXNBNLof+CjVR8EZI1tOjUQg
0GYJvDFCt+hz7AAQU+2Ggrp1L7FaSJmgEJIVYwQP4bM25ee4BiQt3hHJL8Nfr97W
NEg8W4AbpYHJkcXa6jrxZLvsKxZNnLF0eczpG6X2jq7JdbumlZ1viom2j1aPB5GF
owQOcBfI3nhA5nludg2CUT2cqtO4mKJyPXwGA0OcFZ4aVJeO7wnS/21g7AB/Opmt
BCxZBwysPns0JJbH39p2PNRw/q2Kp+pxm0MEyJJe27T/f4cEUaezaAu+7qU7diSb
bUKQZzH+zfvmGr+UyQf6t3UHMyOBMfURmYEVesY47Vu2swdN4QW+tnJ2JIdIjEIb
W+OiON35K4wJDY893wZRI9uPPOQ/6yCAuI0uEu7WV2xqmpvMOj0E4yqkX0IChFxj
Mb1wqu/wHSwktbpqZLykVfJYq5ol/gexrY04AJJSZEDXGvHLR47Qfzx2M0XcjZpi
vsa+CHXqcU5T1e+1tPp8TdcwOzAfMAcGBSsOAwIaBBQiJeRFYB+55Wp+h8bnYt0D
YSQdMwQUYoaBDgXqLdO2G97FQNL1XPhVEEkCAgfQ
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
    if (-not $script:certLocation) { return $false }

    $script:certServerLocation = New-GoodServerCertificate
    if (-not $script:certServerLocation) { return $false }

    $script:badCertLocation = New-BadCertificate
    if (-not $script:badCertLocation) { return $false }

    if ($IsWindows)
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

function Get-GoodServerCertificateObject
{
    return $script:importedServerCert
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

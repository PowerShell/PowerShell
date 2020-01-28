
# CMS Test

using namespace System.Security.Cryptography.X509Certificates
using namespace System.Security.Cryptography

function New-CmsRecipient { param([String]$Name, [Switch]$Invalid)
  $hash = [HashAlgorithmName]::SHA256
  $pad = [RSASignaturePadding]::Pkcs1

  $oids = [OidCollection]::new()
  $oids.Add("1.3.6.1.4.1.311.80.1") | Out-Null

  $ext1 = [X509KeyUsageExtension]::new([X509KeyUsageFlags]::DataEncipherment, $false)
  $ext2 = [X509EnhancedKeyUsageExtension]::new($oids,$false)
  
  $req = ([CertificateRequest]::new("CN=$Name", ([RSA]::Create(2048)), $hash, $pad))
  if(!$Invalid){($ext1, $ext2).ForEach({$req.CertificateExtensions.Add($_)})}

  return $req.CreateSelfSigned([datetime]::Now.AddDays(-1), [datetime]::Now.AddDays(365))
}


# -------------------------------------------------------------

Describe "CmsMessage cmdlets using X509 cert" -Tags "CI" {


BeforeAll  {
  Write-Host "Generating certs"  -ForegroundColor Gray
  $vc1 = New-CmsRecipient "ValidCms1"
  $vc2 = New-CmsRecipient "ValidCms2"
  $ic = New-CmsRecipient "InvalidCms" -Invalid  # invalid cert
  $tmpfile = New-TemporaryFile
}

It " Encrypting with X509Cert" {
 "test" | Protect-CmsMessage -to $vc1 | Should -BeLike '-----BEGIN CMS*'
}

It " Encrypting with multiple X509Cert" {
  $msg = "test" | Protect-CmsMessage -to $vc1, $vc2 | Should -BeLike '-----BEGIN CMS*'
}

It " Decrypt with X509Cert" {
 "test" | Protect-CmsMessage -to $vc1 | Unprotect-CmsMessage -to $vc1 | Should -Be "test"
}

It " Decrypt with multiple X509Cert" {
 "test" | Protect-CmsMessage -to $vc1, $vc2 | Unprotect-CmsMessage -to $vc1, $vc2 | Should -Be "test"
}

It " Encrypt with invalid cert" {
   $e = try { "test" | Protect-CmsMessage -to $ic} catch {$_}
   $e.FullyQualifiedErrorId | Should -BeLike '*CertificateCannotBeUsedForEncryption*'   
}

It "Encrypt with valid and invalid" {
   $e = try { "test" | Protect-CmsMessage -to $vc1, $vc2, $ic} catch {$_}
   $e.FullyQualifiedErrorId | Should -BeLike '*CertificateCannotBeUsedForEncryption*' 
}

It "Encrypt/Decrypt from file" {
 "test" | Protect-CmsMessage -To $vc1 -OutFile $tmpfile
 $msg = Unprotect-CmsMessage -to $vc1 -Path $tmpfile
 $msg | Should -Be "test"

}

It "Get-CmsMessage from content" {
 ("test" | Protect-CmsMessage -to $vc1 | Get-CmsMessage).Content | Should -BeLike '-----BEGIN CMS*'
}

It "Get-CmsMessage from file" {
 (Get-CmsMessage -Path $tmpfile).Content | Should -BeLike '-----BEGIN CMS*'
}


}


# ---------------------- FILES ----------------------------

Describe "CmsMessage cmdlets using files" -Tags "CI" {
 
BeforeAll {
  Write-Host "generating temp cert files"  -ForegroundColor Gray
  $vc1File = New-TemporaryFile
  $vc2File = New-TemporaryFile
  [System.IO.File]::WriteAllBytes("$vc1File", $vc1.Export("pfx"))
  [System.IO.File]::WriteAllBytes("$vc2File", $vc2.Export("pfx"))
}

It "Encrypt With Single File" {
  "test" | Protect-CmsMessage -to "$vc1File" | Unprotect-CmsMessage -To $vc1 | Should -Be 'test'
}

It "Encrypt With Multiple File" {
  $msg = "test" | Protect-CmsMessage -to "$vc1File", "$vc2File" 
  ($msg | Unprotect-CmsMessage -to  $vc1) | Should -Be "test" 
  ($msg | Unprotect-CmsMessage -to  $vc2) | Should -Be "test" 
}

It "Decrypt with multiple files" {
  
  "test" | Protect-CmsMessage -to $vc1 | Unprotect-CmsMessage -to "$vc1File", "$vc2File" | Should -Be 'test'

}

}


# ------------------ STORE ---------------------------------# 



Describe "CmsMessage cmdlets using cert Store" -Tags "CI" {

BeforeAll -Scriptblock { 
  Write-Host "adding temp certs to CurrentUser\My Store"  -ForegroundColor Gray
  $store = [X509Store]::new("My", [StoreLocation]::CurrentUser)
  $store.Open("ReadWrite")
  $cert1 = [X509Certificate2]::new("$vc1File")
  $cert2 = [X509Certificate2]::new("$vc2File")
  $store.Add($cert1)
  $store.Add($cert2)
  # $store.Certificates 
  }

 It "Encrypt/Decrypt using subject" {
  "test" | Protect-CmsMessage -to $cert1.Subject | Unprotect-CmsMessage | Should -Be "test"
  "test" | Protect-CmsMessage -to $cert1.Subject, $cert2.Subject | Unprotect-CmsMessage | Should -Be "test"
 }

  It "Encrypt/Decrypt using Thumbprint" {
  "test" | Protect-CmsMessage -to $cert1.Thumbprint | Unprotect-CmsMessage | Should -Be "test"
  "test" | Protect-CmsMessage -to $cert1.Thumbprint, $cert2.Thumbprint | Unprotect-CmsMessage | Should -Be "test"
  
 }

  It "Encrypt/Decrypt mix" {
    "test" | Protect-CmsMessage -to $cert1.Thumbprint, $cert2.Subject | Unprotect-CmsMessage | Should -Be "test"
  }

}


# ----------------------------------------------------------------

Write-Host "Removing temp files and certs"   -ForegroundColor Gray
$store.Remove($cert1)
$store.Remove($cert2)
$store.Dispose()

Remove-Item $vc1File, $vc2File, $tmpfile


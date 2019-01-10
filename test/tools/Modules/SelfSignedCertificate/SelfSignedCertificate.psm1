# Copyright (c) Robert Holt. All rights reserved.
# Licensed under the MIT License.

# The default length of a certificate in days
$script:DefaultCertDurationDays = 365
# Default RSA key length
$script:DefaultRsaKeyLength = 2048
# Default format for certificates
$script:DefaultCertificateFormat = [System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx
# Default name for the certificate file without extension
$script:DefaultCertificateFileName = 'certificate'
# Default key usage for certificates
$script:DefaultKeyUsage = [System.Security.Cryptography.X509Certificates.X509KeyUsageFlags]::None
# Default certificate subject Common Name
$script:DefaultCommonName = 'localhost'

$script:IsUnix = $IsLinux -or $IsMacOS

# List of certificate key usages supported
enum EnhancedKeyUsage
{
    ServerAuthentication
    ClientAuthentication
}

# Lookup table for usage OIDs
$script:SupportedUsages = @{
    [EnhancedKeyUsage]::ServerAuthentication = [System.Security.Cryptography.Oid]::new("1.3.6.1.5.5.7.3.1", "Server Authentication")
    [EnhancedKeyUsage]::ClientAuthentication = [System.Security.Cryptography.Oid]::new("1.3.6.1.5.5.7.3.2", "Client Authentication")
}

# Class to represent a certificate distinguished name
# like "CN=com.contoso, C=US, S=Nebraska, L=Omaha, O=Contoso Ltd, OU=Sales, E=sales@contoso.com".
# See https://docs.microsoft.com/en-us/windows/desktop/seccrypto/distinguished-name-fields.
class CertificateDistinguishedName
{
    # Name of a person or an object host name
    [ValidateNotNullOrEmpty()]
    [string]$CommonName

    # 2-character ISO country code 
    [ValidateLength(2, 2)]
    [string]$Country

    # The state or province where the owner is physically located
    [string]$StateOrProvince

    # The city where the owner is located
    [string]$Locality

    # The name of the registering organization
    [string]$Organization

    # The division of the organization owning the certificate
    [string]$OrganizationalUnit

    # The email address of the certificate owner
    [Obsolete("The email field is deprecated by the PKIX standard")]
    [mailaddress]$EmailAddress

    # Format the distinguished name like 'CN="com.contoso"; C="US"; S="Nebraska"'
    [string] Format()
    {
        return $this.Format(';', <# UseQuotes #> $true)
    }

    # Format the distinguished name with the given separator and quote usage setting
    [string] Format([char]$Separator, [bool]$UseQuotes)
    {
        $sb = [System.Text.StringBuilder]::new()
        
        if ($UseQuotes)
        {
            $sb.Append("CN=`"$($this.CommonName)`"")
        }
        else
        {
            $sb.Append("CN=$($this.CommonName)")
        }

        $fields = @{
            OU = $this.OrganizationalUnit
            O = $this.Organization
            L = $this.Locality
            S = $this.StateOrProvince
            C = $this.Country
            E = $this.EmailAddress.Address
        }

        foreach ($field in 'OU','O','L','S','C','E')
        {
            $val = $fields[$field]

            if (-not $val)
            {
                continue
            }

            $sb.Append($Separator)
            $sb.Append(" ")

            if ($UseQuotes)
            {
                $sb.Append("$field=`"$val`"")
            }
            else
            {
                $sb.Append("$field=$val")
            }
        }

        return $sb.ToString()
    }

    # Format the distinguished name like 'CN=com.contoso, C=US, S=Nebraska'
    [string] ToString()
    {
        return $this.Format(',', $false)
    }

    # OpenSSL expects a strange distinguished name format
    # like '/CN=com.contoso/C=US/S=Nebraska'
    [string] FormatForOpenSsl()
    {
        $sb = [System.Text.StringBuilder]::new()
        
        $sb.Append("/CN=$($this.CommonName)")

        $fields = @{
            OU = $this.OrganizationalUnit
            O = $this.Organization
            L = $this.Locality
            S = $this.StateOrProvince
            C = $this.Country
            E = $this.EmailAddress.Address
        }

        foreach ($field in 'OU','O','L','S','C','E')
        {
            $val = $fields[$field]

            if (-not $val)
            {
                continue
            }

            $sb.Append("/$field=$val")
        }

        return $sb.ToString()
    }

    # Create a new X500DistinguishedName object from this certificate
    [X500DistinguishedName] AsX500DistinguishedName()
    {
        return [X500DistinguishedName]::new($this.Format())
    }
}

# Represents the data in a self-signed certificate
class SelfSignedCertificate
{
    # The friendly name of the certificate
    [string]$FriendlyName = [string]::Empty

    # The length of the private key to use in bits
    [int]$KeyLength = $script:DefaultRsaKeyLength

    # The format of the certificate
    [System.Security.Cryptography.X509Certificates.X509ContentType]$Format = $script:DefaultCertificateFormat

    # The start time of the certificate's valid period
    [datetime]$NotBefore = [datetime]::Now

    # The end time of the certificate's valid period
    [datetime]$NotAfter = [datetime]::Now.AddDays($script:DefaultCertDurationDays)

    # The certificate's subject and issuer name (since it's self-signed)
    [CertificateDistinguishedName]$SubjectName

    # The key usages for the certificate -- what it will be used to do
    [System.Security.Cryptography.X509Certificates.X509KeyUsageFlags]$KeyUsage = $script:DefaultKeyUsage

    # Extensions to be added to the certificate beyond those added automatically
    [System.Security.Cryptography.X509Certificates.X509Extension[]]$AdditionalExtensions

    # The enhanced key usages for the certificate -- what specific scenarios it will be used for
    [EnhancedKeyUsage[]]$EnhancedKeyUsage

    # Whether or not this certificate is for a certificate authority
    [bool]$ForCertificateAuthority

    # Instantiate an X509Certificate2 object from this object
    [System.Security.Cryptography.X509Certificates.X509Certificate2] AsX509Certificate2()
    {
        $extensions = [System.Collections.Generic.List[System.Security.Cryptography.X509Certificates.X509Extension]]::new()

        if ($this.AdditionalExtensions)
        {
            $extensions.AddRange($this.AdditionalExtensions)
        }

        if ($this.KeyUsage)
        {
            # Create Key Usage
            $keyUsages = [System.Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new(
                $this.KeyUsage,
                <# critical #> $false)
            $extensions.Add($keyUsages)
        }

        # Create Enhanced Key Usage from configured usages
        if ($this.EnhancedKeyUsage)
        {
            $ekuOidCollection = [System.Security.Cryptography.OidCollection]::new()
            foreach ($usage in $this.EnhancedKeyUsage)
            {
                if ($script:SupportedUsages.Keys -contains $usage)
                {
                    $ekuOidCollection.Add($script:SupportedUsages[$usage])
                }
            }
            $enhancedKeyUsages = [System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]::new(
                $ekuOidCollection,
                <# critical #> $false)
            $extensions.Add($enhancedKeyUsages)
        }

        # Create Basic Constraints
        $basicConstraints = [System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]::new(
            <# certificateAuthority #> $this.ForCertificateAuthority,
            <# hasPathLengthConstraint #> $false,
            <# pathLengthConstraint #> 0,
            <# critical #> $false)
        $extensions.Add($basicConstraints)

        # Create Private Key
        $key = [System.Security.Cryptography.RSA]::Create($this.KeyLength)

        # Create the subject of the certificate
        $subject = $this.SubjectName.AsX500DistinguishedName()

        # Create Certificate Request
        $certRequest = [System.Security.Cryptography.X509Certificates.CertificateRequest]::new(
            $subject,
            $key,
            [System.Security.Cryptography.HashAlgorithmName]::SHA256,
            [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)

        # Create the Subject Key Identifier extension
        $subjectKeyIdentifier = [System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierExtension]::new(
            $certRequest.PublicKey,
            <# critical #> $false)
        $extensions.Add($subjectKeyIdentifier)

        # Create Authority Key Identifier if the certificate is for a CA
        if ($this.ForCertificateAuthority)
        {
            $authorityKeyIdentifier = New-AuthorityKeyIdentifier -SubjectKeyIdentifier $subjectKeyIdentifier
            $extensions.Add($authorityKeyIdentifier)
        }

        foreach ($extension in $extensions)
        {
            $certRequest.CertificateExtensions.Add($extension)
        }

        $cert = $certRequest.CreateSelfSigned($this.NotBefore, $this.NotAfter)

        # FriendlyName is not supported on UNIX platforms
        if (-not $script:IsUnix)
        {
            $cert.FriendlyName = $this.FriendlyName
        }

        return $cert
    }
}

<#
.SYNOPSIS
Creates a self-signed certificate for testing use.

.DESCRIPTION
Creates a self-signed certificate for testing usage in a
given format and using a given backend and outputs it to a given filepath.

.PARAMETER OutFilePath
The filepath to output the generated certificate to.

.PARAMETER CommonName
The common name of the certificate subject, e.g. "com.contoso" or "Jennifer McCallum".

.PARAMETER Country
The country of the certificate subject as a two-character ISO code, e.g. "US" or "GB".

.PARAMETER StateOrProvince
The state or province of the physical location of the certificate subject, e.g. "California" or "New South Wales".

.PARAMETER Locality
The city or regional locality where the certificate subject is located, e.g. "Seattle".

.PARAMETER Organization
The organization to which the certificate subject belongs, e.g. "Contoso Ltd".

.PARAMETER OrganizationalUnit
The department or sub-organizational group the certificate subject belongs to, e.g. "Marketing".

.PARAMETER EmailAddress
--DEPRECATED-- The email address of the certificate owner.

.PARAMETER FriendlyName
A descriptive, human-readable name for the certificate.

.PARAMETER CertificateFormat
The file format the certificate will take.

.PARAMETER KeyLength
The length of the key in bits.

.PARAMETER KeyUsage
What general usages the certificate will be used for.

.PARAMETER EnhancedKeyUsage
The particular scenarios the certificate will be used for.

.PARAMETER AdditionalExtension
Additional certificate extensions desired to add to the certificate.

.PARAMETER ForCertificateAuthority
Specifies that the certificate is for a certification authority (CA).

.PARAMETER Passphrase
The encryption passphrase or password for the certificate to protect its contents.

.PARAMETER Force
Force overwriting if a certificate file already exists on the path to write to.

.PARAMETER NotBefore
The time when the certificate becomes valid.

.PARAMETER NotAfter
The time when the certificate ceases to be valid.

.PARAMETER Duration
The length of the validity period of the certificate.
#>
function New-SelfSignedCertificate
{
    [CmdletBinding(DefaultParameterSetName = "NotAfter")]
    param(
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $OutCertPath = $PWD,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Alias("CN")]
        [string]
        $CommonName = $script:DefaultCommonName,

        [Parameter()]
        [Alias("C")]
        [string]
        $Country,

        [Parameter()]
        [Alias("S")]
        [string]
        $StateOrProvince,

        [Parameter()]
        [Alias("L")]
        [string]
        $Locality,

        [Parameter()]
        [Alias("O")]
        [string]
        $Organization,

        [Parameter()]
        [Alias("OU")]
        [string]
        $OrganizationalUnit,

        [Parameter()]
        [Alias("E")]
        [Obsolete("The email name component is deprecated by the PKIX standard")]
        [mailaddress]
        $EmailAddress,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $FriendlyName,

        [Parameter()]
        [System.Security.Cryptography.X509Certificates.X509ContentType]
        $CertificateFormat = $script:DefaultCertificateFormat,

        [Parameter()]
        [ValidateSet(2048, 3072, 4096)]
        [int]
        $KeyLength = $script:DefaultRsaKeyLength,

        [Parameter()]
        [System.Security.Cryptography.X509Certificates.X509KeyUsageFlags[]]
        $KeyUsage = $script:DefaultKeyUsage,

        [Parameter()]
        [EnhancedKeyUsage[]]
        $EnhancedKeyUsage,

        [Parameter()]
        [System.Security.Cryptography.X509Certificates.X509Extension[]]
        $AdditionalExtension,

        [Parameter()]
        [Alias("CA")]
        [switch]
        $ForCertificateAuthority,

        [Parameter()]
        [Alias('Password')]
        [securestring]
        $Passphrase,

        [Parameter()]
        [switch]
        $Force,

        [Parameter()]
        [Alias("StartDate")]
        [datetime]
        $NotBefore = [datetime]::Now,

        [Parameter(ParameterSetName="NotAfter")]
        [datetime]
        $NotAfter = [datetime]::Now.AddDays($script:DefaultCertDurationDays),

        [Parameter(ParameterSetName="Duration")]
        [timespan]
        $Duration = [timespan]::FromDays($script:DefaultCertDurationDays)
    )

    process
    {
        if ($PSCmdlet.ParameterSetName.Contains("Duration"))
        {
            $NotAfter = $NotBefore.Add($Duration)
        }

        # Normalize the given paths (make them absolute, with relative interpreted as relative to PWD)
        $OutCertPath = Get-AbsolutePathFromSupplied -Path $OutCertPath

        # Make sure the certificate's output path matches the given format
        $ext = Get-CertificateFormatExtension $CertificateFormat
        $OutCertPath = Get-ProperFilePath -Path $OutCertPath -DefaultFileName $script:DefaultCertificateFileName -RequiredExtension $ext

        # Ensure the directory where the certificate will go exists and that another certificate does not already exist
        $destinationDir = [System.IO.Path]::GetDirectoryName($OutCertPath)
        if (-not (Test-Path $destinationDir))
        {
            throw [System.InvalidOperationException] "Destination directory '$destinationDir' for certificate does not exist or is not accessible"
        }
        elseif (Test-Path $OutCertPath)
        {
            if ($Force)
            {
                Remove-Item -Path $OutCertPath -Force
            }
            else
            {
                throw [System.IO.IOException] "File already exists at path $OutCertPath"
            }
        }

        # Roll the key usage flags into a single value (since they are flags)
        $keyUsageFlags = [System.Security.Cryptography.X509Certificates.X509KeyUsageFlags]::None
        foreach ($keyUsageFlag in $KeyUsage)
        {
            $keyUsageFlags = $keyUsageFlags -bor $keyUsageFlag
        }

        # Construct the subject name
        $subjectName = [CertificateDistinguishedName] (Get-FalsyRemovedHashtable -Hashtable @{
            CommonName = $CommonName
            Country = $Country
            StateOrProvince = $StateOrProvince
            Locality = $Locality
            Organization = $Organization
            OrganizationalUnit = $OrganizationalUnit
            EmailAddress = $EmailAddress
        })

        # Construct the certificate object
        $certificate = [SelfSignedCertificate] (Get-FalsyRemovedHashtable -Hashtable @{
            SubjectName = $subjectName
            KeyLength = $KeyLength
            KeyUsage = $keyUsageFlags
            EnhancedKeyUsage = $EnhancedKeyUsage
            NotBefore = $NotBefore
            NotAfter = $NotAfter
            FriendlyName = $FriendlyName
            ForCertificateAuthority = $ForCertificateAuthority
            AdditionalExtensions = $AdditionalExtension
        })

        # Turn the certificate object into an X509 certificate (2) object
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$x509Certificate2 = $certificate.AsX509Certificate2()

        # Write the certificate to the file system
        if ($PSBoundParameters.ContainsKey('Passphrase'))
        {
            $bytes = $x509Certificate2.Export($CertificateFormat, $Passphrase)
            try
            {
                [System.IO.File]::WriteAllBytes($OutCertPath, $bytes)
            }
            finally
            {
                [array]::Clear($bytes, 0, $bytes.Length)
            }
        }
        else
        {
            $bytes = $x509Certificate2.Export($CertificateFormat)
            [System.IO.File]::WriteAllBytes($OutCertPath, $bytes)
        }

        Write-Host "Certificate written to $OutCertPath"

        # Return the certificate object for inspection
        return $x509Certificate2
    }
}

# Copy a hashtable with all the falsy entries removed
# @{ x = 'x'; y = '' } -> @{ x = 'x' }
function Get-FalsyRemovedHashtable
{
    param([hashtable]$Hashtable)

    $outTable = @{}

    foreach ($key in $Hashtable.Keys)
    {
        if ($Hashtable[$key])
        {
            $outTable[$key] = $Hashtable[$key]
        }
    }

    return $outTable
}

# Gets the appropriate extension for a given certificate format
function Get-CertificateFormatExtension
{
    param([System.Security.Cryptography.X509Certificates.X509ContentType]$CertificateFormat)

    switch ($CertificateFormat)
    {
        Cert
        {
            return 'cer'
        }

        Pfx
        {
            return 'pfx'
        }

        default
        {
            throw [System.NotSupportedException] "No extension known for format '$CertificateFormat'"
        }
    }
}

# Normalizes paths so that relative paths are interpreted
# relative to PWD and returned as absolute
function Get-AbsolutePathFromSupplied
{
    param(
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path))
    {
        return $Path
    }

    return [System.IO.Path]::GetFullPath((Join-Path $PWD $Path))
}

# Take a user-supplied path and fix it up to point to a file path that makes sense
function Get-ProperFilePath
{
    param(
        [string]$Path,
        [string]$DefaultFileName,
        [string]$RequiredExtension
    )

    # If we're given a directory, point the path to a default filename in that directory
    if ($Path.EndsWith([System.IO.Path]::DirectorySeparatorChar) -or (Test-Path -Path $Path -PathType Container))
    {
        return Join-Path $Path "$DefaultFileName.$RequiredExtension"
    }

    # We're pointing to a file, so just correct the extension if necessary
    if ([System.IO.Path]::GetExtension($Path) -ne $RequiredExtension)
    {
        return [System.IO.Path]::ChangeExtension($Path, $RequiredExtension)
    }

    return $Path
}

# Produce a new authority key identifier from the authority's subject key identifier
function New-AuthorityKeyIdentifier
{
    param(
        [System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierExtension]
        $SubjectKeyIdentifier,

        [switch]
        $Critical
    )

    # The canonical OID of an Authority Key Identifier
    $akiOid = "2.5.29.35"

    # AKI is not supported directly by .NET, we have to make our own
    # The ASN.1 rule we follow is:
    # AuthorityKeyId ::= SEQUENCE { keyIdentifier [0] IMPLICIT_OCTET_STRING }
    # Because nothing documents what that means in DER encoding:
    #  - SEQUENCE: 0x30 tag, then length in bytes up to 0x79
    #  - keyIdentifier: <a type hint, not encoded - equates to the [0] tag>
    #  - [0]: a context-specific tag (bit 8 = 1, bit 7 = 0) of value 0 (bits 6-1 = 0)
    #  - IMPLICIT_OCTECT_STRING: no 0x04 octet string tag, first byte is length in bytes up to 0x79, then the string content
    # Example:
    #    | SEQUENCE  | [0]  | IMPLICIT_OCTET_STRING | 0x01 0x02 0x03 0x04
    #    | 0x30 0x06 | 0x80 | 0x04                  | 0x01 0x02 0x03 0x04
    #   sequence ^ length       ^ octet string length
    #
    # For more information see:
    #  - Microsoft's resources on this: https://docs.microsoft.com/en-us/windows/desktop/seccertenroll/about-certificate-request-encoding
    #  - This helpful page: http://luca.ntop.org/Teaching/Appunti/asn1.html

    # Compose the key here
    # We could extract from the SKI's raw data, but the string is a safer bet
    $ski = $SubjectKeyIdentifier.SubjectKeyIdentifier
    $key = [System.Collections.Generic.List[byte]]::new()
    for ($i = 0; $i -lt $SubjectKeyIdentifier.SubjectKeyIdentifier.Length; $i += 2)
    {
        $x = $ski[$i] + $ski[$i+1]
        $b = [System.Convert]::ToByte($x, 16)
        [void]$key.Add($b)
    }

    # Ensure our assumptions about not having to encode too much are correct
    if ($key.Count + 2 -gt 0x79)
    {
        throw [System.InvalidOperationException] "Subject key identifier length is to high to encode: $($key.Count)"
    }

    [byte]$octetLength = $key.Count
    [byte]$sequenceLength = $octetLength+2

    [byte]$sequenceTag = 0x30
    [byte]$keyIdentifierTag = 0x80

    # Assemble the raw data
    [byte[]]$akiRawData = @($sequenceTag, $sequenceLength, $keyIdentifierTag, $octetLength) + $key

    # Construct the Authority Key Identifier extension
    return [System.Security.Cryptography.X509Certificates.X509Extension]::new(
        $akiOid,
        $akiRawData,
        $Critical)
}
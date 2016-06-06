# Utility to generate a self-signed certificate and sign a given package such as OpenPowerShell.zip/appx/msi

[CmdletBinding()]
param (
        #Path to package - Ex: OpenPowerShell.msi, OpenPowerShell.appx 
        [Parameter(Mandatory = $true)]      
        [ValidateNotNullOrEmpty()]
        [string] $PackageFilePath

)

# function to generate a self-signed certificate
# customize parameters to makecert.exe to control certificate life time and other options
function New-SelfSignedCertificate
{
    [CmdletBinding()]
    param (
        
        #Path to save generated Certificate        
        [ValidateNotNullOrEmpty()]
        [string] $CertificateFilePath = "$pwd\OpenPowerShell.cer",

        #Path to save generated pvk file
        [ValidateNotNullOrEmpty()]
        [string] $PvkFilePath = "$env:Temp\OpenPowerShell.pvk"

    )

    $makecertBinPath = "${env:ProgramFiles(x86)}\Windows Kits\10\bin\x64\MakeCert.exe"

    Write-Verbose "Ensure MakeCert.exe is present @ $makecertBinPath"
    if (-not (Test-Path $makecertBinPath))
    {
        throw "$makecertBinPath is required to generate a self-signed certificate"
    }

    Remove-Item $CertificateFilePath -Force -ErrorAction Ignore
    Remove-Item $PvkFilePath -Force -ErrorAction Ignore

    & $makecertBinPath -r -h 0 -n "CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US" -eku 1.3.6.1.5.5.7.3.3 -pe -sv $PvkFilePath $CertificateFilePath | Write-Verbose
    
    Write-Verbose "Self-Signed Cert generated @ $CertificateFilePath"

    return $CertificateFilePath
}

# Convert private pvk file format to pfx format to be consumed by signtool.exe
function ConvertTo-Pfx
{
    [CmdletBinding()]
    param (
        
        #Path to Certificate file
        [ValidateNotNullOrEmpty()]
        [string] $CertificateFilePath = "$pwd\OpenPowerShell.cer",

        #Path to pvk file
        [ValidateNotNullOrEmpty()]
        [string] $PvkFilePath = "$env:Temp\OpenPowerShell.pvk",

        #Path to generated pfx file
        [ValidateNotNullOrEmpty()]
        [string] $PfxFilePath = "$env:Temp\OpenPowerShell.pfx"
    )

    $pvk2pfxBinPath = "${env:ProgramFiles(x86)}\Windows Kits\10\bin\x64\pvk2pfx.exe"

    Write-Verbose "Ensure pvk2pfx.exe is present @ $pvk2pfxBinPath"
    if (-not (Test-Path $pvk2pfxBinPath))
    {
        throw "$pvk2pfxBinPath is required to convert pvk file to pfx file - one of the prerequisities to signing a package!"
    }
        
    Remove-Item $PfxFilePath -Force -ErrorAction Ignore

    & $pvk2pfxBinPath /pvk $PvkFilePath /spc $CertificateFilePath /pfx $PfxFilePath /f | Write-Verbose

    Write-Verbose "Pfx file generated @ $PfxFilePath"

    return $PfxFilePath
}

# Sign a given package
# this function needs the proprietary pfx file 
function Sign-Package
{
    [CmdletBinding()]
    param (   
    
        #Path to package - Ex: OpenPowerShell.msi, OpenPowerShell.appx 
        [Parameter(Mandatory = $true)]      
        [ValidateNotNullOrEmpty()]
        [string] $PackageFilePath,
        
        #Path to generated pfx file for signing the package
        [ValidateNotNullOrEmpty()]
        [string] $PfxFilePath = "$env:Temp\OpenPowerShell.pfx"

    )

    $signtoolBinPath = "${env:ProgramFiles(x86)}\Windows Kits\10\bin\x64\SignTool.exe"

    Write-Verbose "Ensure SignTool.exe is present @ $signtoolBinPath"
    if (-not (Test-Path $signtoolBinPath))
    {
        throw "$signtoolBinPath is required to sign the package!"
    }

    & $signtoolBinPath sign -f $PfxFilePath -fd SHA256 -v $PackageFilePath | Write-Verbose

    Write-Verbose "Authenticode signing successful for $PackageFilePath"

    return $PackageFilePath
}


$certificate = New-SelfSignedCertificate -Verbose
ConvertTo-Pfx -Verbose
$signedPackage = Sign-Package -PackageFilePath $PackageFilePath -Verbose

Write-Output "Signed Package is available @ `'$signedPackage`'"

Write-Output "On Windows Full SKU - Import the self-signed certificate `'$certificate`' to TrustedStore (Import-Certificate) prior to installing the package"

Write-Output "On Windows Nano - Use `'$env:Windir\System32\Certoc.exe -AddStore TrustedPeople <Certificate>`' to import the self-signed certificate `'$certificate`' to TrustedStore"
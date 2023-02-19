# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
param(
    [string] $SigningXmlPath = (Join-Path -Path $PSScriptRoot  -ChildPath 'signing.xml'),
    [switch] $SkipPwshExe
)
# Script for use in VSTS to update signing.xml

if ($SkipPwshExe) {
    ## This is required for fxdependent package as no .exe is generated.
    $xmlContent = Get-Content $SigningXmlPath | Where-Object { $_ -notmatch '__INPATHROOT__\\pwsh.exe' }
} else {
    ## We skip the global tool shim assembly for regular builds.
    $xmlContent = Get-Content $signingXmlPath | Where-Object { $_ -notmatch '__INPATHROOT__\\Microsoft.PowerShell.GlobalTool.Shim.dll' }
}

# Parse the signing xml
$signingXml = [xml] $xmlContent

# Get any variables to updating 'signType' in the XML
# Define a variable named `<signTypeInXml>SignType' in VSTS to updating that signing type
# Example:  $env:AuthenticodeSignType='newvalue'
#      will cause all files with the 'Authenticode' signtype to be updated with the 'newvalue' signtype
$signTypes = @{}
Get-ChildItem -Path env:/*SignType | ForEach-Object -Process {
    $signType = $_.Name.ToUpperInvariant().Replace('SIGNTYPE','')
    Write-Host "Found SigningType $signType with value $($_.value)"
    $signTypes[$signType] = $_.Value
}

# examine each job in the xml
$signingXml.SignConfigXML.job | ForEach-Object -Process {
    # examine each file in the job
    $_.file | ForEach-Object -Process {
        # if the sign type is one of the variables we found, update it to the new value
        $signType = $_.SignType.ToUpperInvariant()
        if($signTypes.ContainsKey($signType))
        {
            $newSignType = $signTypes[$signType]
            Write-Host "Updating $($_.src) to $newSignType"
            $_.signType = $newSignType
        }
    }
}

$signingXml.Save($signingXmlPath)

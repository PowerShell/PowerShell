#########################################################################################
#
# Copyright (c) Microsoft Corporation. All rights reserved.
#
# Appx Provider Module
#
#########################################################################################

$script:ProviderName = "appx"
$script:AppxPackageExtension = ".appx"
$script:AppxManifestFile = "AppxManifest.xml"
$script:Architecture = "Architecture"
$script:ResourceId = "ResourceId"
$script:AppxPackageSources = $null
$script:AppxLocalPath="$env:LOCALAPPDATA\Microsoft\Windows\PowerShell\AppxProvider"
$script:AppxPackageSourcesFilePath = Microsoft.PowerShell.Management\Join-Path -Path $script:AppxLocalPath -ChildPath "AppxPackageSources.xml"
$Script:ResponseUri = "ResponseUri"
$Script:StatusCode = "StatusCode"
# Wildcard pattern matching configuration.
$script:wildcardOptions = [System.Management.Automation.WildcardOptions]::CultureInvariant -bor `
                          [System.Management.Automation.WildcardOptions]::IgnoreCase
#Localized Data
Microsoft.PowerShell.Utility\Import-LocalizedData  LocalizedData -filename AppxProvider.Resource.psd1

#region Appx Provider APIs Implementation
function Get-PackageProviderName
{ 
    return $script:ProviderName
}

function Initialize-Provider{ 
  param(
  )
}

function Get-DynamicOptions
{
    param
    (
        [Microsoft.PackageManagement.MetaProvider.PowerShell.OptionCategory] 
        $category
    )

    Write-Debug ($LocalizedData.ProviderApiDebugMessage -f ('Get-DynamicOptions'))
               
    switch($category)
    {
        Install {
                    Write-Output -InputObject (New-DynamicOption -Category $category -Name Architecture -ExpectedType String -IsRequired $false)
                    Write-Output -InputObject (New-DynamicOption -Category $category -Name ResourceId -ExpectedType String -IsRequired $false)
                }
        Package {
                    Write-Output -InputObject (New-DynamicOption -Category $category -Name Architecture -ExpectedType String -IsRequired $false)
                    Write-Output -InputObject (New-DynamicOption -Category $category -Name ResourceId -ExpectedType String -IsRequired $false)
                }
    }
}

function Find-Package
{ 
    [CmdletBinding()]
    param
    (
        [string[]]
        $names,

        [string]
        $requiredVersion,

        [string]
        $minimumVersion,

        [string]
        $maximumVersion
    )   

    Write-Debug ($LocalizedData.ProviderApiDebugMessage -f ('Find-Package'))    

    $ResourceId = $null
    $Architecture = $null
    $Sources = @()    
    $streamedResults = @()    
    $namesParameterEmpty = (-not $names) -or (($names.Count -eq 1) -and ($names[0] -eq ''))

    Set-PackageSourcesVariable

    if($RequiredVersion -and $MinimumVersion)
    {

        ThrowError -ExceptionName "System.ArgumentException" `
                   -ExceptionMessage $LocalizedData.VersionRangeAndRequiredVersionCannotBeSpecifiedTogether `
                   -ErrorId "VersionRangeAndRequiredVersionCannotBeSpecifiedTogether" `
                   -CallerPSCmdlet $PSCmdlet `
                   -ErrorCategory InvalidArgument
    }    
    if($RequiredVersion -or $MinimumVersion)
    {
        if(-not $names -or $names.Count -ne 1 -or (Test-WildcardPattern -Name $names[0]))
        {
            ThrowError -ExceptionName "System.ArgumentException" `
                       -ExceptionMessage $LocalizedData.VersionParametersAreAllowedOnlyWithSinglePackage `
                       -ErrorId "VersionParametersAreAllowedOnlyWithSinglePackage" `
                       -CallerPSCmdlet $PSCmdlet `
                       -ErrorCategory InvalidArgument
        }
    }    

    $options = $request.Options            
    if($options)
    {
        foreach( $o in $options.Keys )
        {
            Write-Debug ( "OPTION: {0} => {1}" -f ($o, $options[$o]) )
        }

        if($options.ContainsKey('Source'))
        {        
            $SourceNames = $($options['Source'])
            Write-Verbose ($LocalizedData.SpecifiedSourceName -f ($SourceNames))        
            foreach($sourceName in $SourceNames)
            {            
                if($script:AppxPackageSources.Contains($sourceName))
                {
                    $Sources += $script:AppxPackageSources[$sourceName]                
                }
                else
                {
                    $sourceByLocation = Get-SourceName -Location $sourceName
                    if ($sourceByLocation -ne $null)
                    {
                        $Sources += $script:AppxPackageSources[$sourceByLocation]                                        
                    }
                    else
                    {
                            $message = $LocalizedData.PackageSourceNotFound -f ($sourceName)
                            ThrowError -ExceptionName "System.ArgumentException" `
                                -ExceptionMessage $message `
                                -ErrorId "PackageSourceNotFound" `
                                -CallerPSCmdlet $PSCmdlet `
                                -ErrorCategory InvalidArgument `
                                -ExceptionObject $sourceName
                    }
                }
            }
        }
        else
        {
            Write-Verbose $LocalizedData.NoSourceNameIsSpecified        
            $script:AppxPackageSources.Values | Microsoft.PowerShell.Core\ForEach-Object { $Sources += $_ }
        }        

        if($options.ContainsKey($script:Architecture))
        {
            $Architecture = $options[$script:Architecture]     
        }
        if($options.ContainsKey($script:ResourceId))
        {
            $ResourceId = $options[$script:ResourceId]
        }
    }             
    
    foreach($source in $Sources)
    {
        $location = $source.SourceLocation
        if($request.IsCanceled)
        {
            return
        }
        if(-not(Test-Path $location))
        {
            $message = $LocalizedData.PathNotFound -f ($Location)
            Write-Verbose $message            
            continue
        }

        $packages = Get-AppxPackagesFromPath -path $location
        foreach($pkg in  $packages)
        {
            if($request.IsCanceled)
            {
                return
            }
            
            $pkgManifest = Get-PackageManfiestData -PackageFullPath $pkg.FullName
            if(-not $pkgManifest)
            {               
                continue
            }
                
            # $pkgManifest.Name has to match any of the supplied names, using PowerShell wildcards
            if(-not($namesParameterEmpty))
            {
                if(-not(($names | Microsoft.PowerShell.Core\ForEach-Object { if ($pkgManifest.Name -like $_){return $true; break} } -End {return $false})))
                {
                    continue
                }
            }

            # Version            
            if($RequiredVersion)
            {
                if($RequiredVersion -ne $pkgManifest.Version)
                {
                    continue
                }
            }
            else
            {
                if(-not((-not $MinimumVersion -or ($MinimumVersion -le $pkgManifest.Version)) -and 
                        (-not $MaximumVersion -or ($MaximumVersion -ge $pkgManifest.Version))))
                {
                    continue
                }
            }
                  
              
            if($Architecture)
            {                
                $wildcardPattern = New-Object System.Management.Automation.WildcardPattern $Architecture, $script:wildcardOptions
                if(-not($wildcardPattern.IsMatch($pkgManifest.Architecture)))
                {
                    continue
                }                
            }            

            if($ResourceId)
            {                
                $wildcardPattern = New-Object System.Management.Automation.WildcardPattern $ResourceId, $script:wildcardOptions
                if(-not($wildcardPattern.IsMatch($pkgManifest.ResourceId)))
                {
                    continue
                }                
            }
            
            $sid = New-SoftwareIdentityFromPackage -Package $pkgManifest -Source $source.Name            
            $fastPackageReference = $sid.fastPackageReference            
            if($streamedResults -notcontains $fastPackageReference)
            {
                $streamedResults += $fastPackageReference
                Write-Output -InputObject $sid
            }
        }
    }
}

function Get-InstalledPackage
{ 
    [CmdletBinding()]
    param
    (
        [Parameter()]
        [string]
        $Name,

        [Parameter()]
        [string]
        $RequiredVersion,

        [Parameter()]
        [string]
        $MinimumVersion,

        [Parameter()]
        [string]
        $MaximumVersion
    )

    Write-Debug -Message ($LocalizedData.ProviderApiDebugMessage -f ('Get-InstalledPackage'))
    
    $Architecture = $null
    $ResourceId = $null

    $options = $request.Options
    if($options)
    {
        if($options.ContainsKey($script:Architecture))
        {
            $Architecture = $options[$script:Architecture]
        }
        if($options.ContainsKey($script:ResourceId))
        {
            $ResourceId = $options[$script:ResourceId]
        }
    }

    $params = @{}
    if($Name)
    {
        $params.Add("Name", $Name)
    }
    $packages = Appx\Get-AppxPackage @params

	foreach($package in $packages)
	{
        if($RequiredVersion)
        {
            if($RequiredVersion -ne $package.Version)
            {
                continue
            }
        }
        else
        {
            if(-not((-not $MinimumVersion -or ($MinimumVersion -le $package.Version)) -and 
                    (-not $MaximumVersion -or ($MaximumVersion -ge $package.Version))))
            {
                continue
            }
        }

        if($Architecture)
        {            
            $wildcardPattern = New-Object System.Management.Automation.WildcardPattern $Architecture, $script:wildcardOptions
            if(-not($wildcardPattern.IsMatch($package.Architecture)))
            {
                continue
            }                
        }
        if($ResourceId)
        {            
            $wildcardPattern = New-Object System.Management.Automation.WildcardPattern $ResourceId,$script:wildcardOptions
            if(-not($wildcardPattern.IsMatch($package.ResourceId)))                
            {
                continue
            }
        }

		$sid = New-SoftwareIdentityFromPackage -Package $package
        write-Output $sid
	}
}

function Install-Package
{ 
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $fastPackageReference
    )

    Write-Debug -Message ($LocalizedData.ProviderApiDebugMessage -f ('Install-Package'))
    Write-Debug -Message ($LocalizedData.FastPackageReference -f $fastPackageReference)
	
	Appx\Add-AppxPackage -Path $fastPackageReference
}

function UnInstall-Package
{ 
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $fastPackageReference
    )

    Write-Debug -Message ($LocalizedData.ProviderApiDebugMessage -f ('Uninstall-Package'))
    Write-Debug -Message ($LocalizedData.FastPackageReference -f $fastPackageReference)

	Appx\Remove-AppxPackage -Package $fastPackageReference
}

function Add-PackageSource
{
    [CmdletBinding()]
    param
    (
        [string]
        $Name,
         
        [string]
        $Location,

        [bool]
        $Trusted
    )     
    
    Write-Debug ($LocalizedData.ProviderApiDebugMessage -f ('Add-PackageSource'))

    Set-PackageSourcesVariable -Force

    if(-not (Microsoft.PowerShell.Management\Test-Path $Location) -and
       -not (Test-WebUri -uri $Location))
    {
        $LocationUri = [Uri]$Location
        if($LocationUri.Scheme -eq 'file')
        {
            $message = $LocalizedData.PathNotFound -f ($Location)
            ThrowError -ExceptionName "System.ArgumentException" `
                       -ExceptionMessage $message `
                       -ErrorId "PathNotFound" `
                       -CallerPSCmdlet $PSCmdlet `
                       -ErrorCategory InvalidArgument `
                       -ExceptionObject $Location
        }
        else
        {
            $message = $LocalizedData.InvalidWebUri -f ($Location, "Location")
            ThrowError -ExceptionName "System.ArgumentException" `
                       -ExceptionMessage $message `
                       -ErrorId "InvalidWebUri" `
                       -CallerPSCmdlet $PSCmdlet `
                       -ErrorCategory InvalidArgument `
                       -ExceptionObject $Location
        }
    }

    if(Test-WildcardPattern $Name)
    {
        $message = $LocalizedData.PackageSourceNameContainsWildCards -f ($Name)
        ThrowError -ExceptionName "System.ArgumentException" `
                    -ExceptionMessage $message `
                    -ErrorId "PackageSourceNameContainsWildCards" `
                    -CallerPSCmdlet $PSCmdlet `
                    -ErrorCategory InvalidArgument `
                    -ExceptionObject $Name
    }

    $LocationString = Get-ValidPackageLocation -LocationString $Location -ParameterName "Location"
           
    # Check if Location is already registered with another Name
    $existingSourceName = Get-SourceName -Location $LocationString

    if($existingSourceName -and 
       ($Name -ne $existingSourceName))
    {
        $message = $LocalizedData.PackageSourceAlreadyRegistered -f ($existingSourceName, $Location, $Name)
        ThrowError -ExceptionName "System.ArgumentException" `
                   -ExceptionMessage $message `
                   -ErrorId "PackageSourceAlreadyRegistered" `
                   -CallerPSCmdlet $PSCmdlet `
                   -ErrorCategory InvalidArgument
    }
        
    # Check if Name is already registered
    if($script:AppxPackageSources.Contains($Name))
    {
        $currentSourceObject = $script:AppxPackageSources[$Name]
        $null = $script:AppxPackageSources.Remove($Name)
    }
  
    # Add new package source
    $packageSource = Microsoft.PowerShell.Utility\New-Object PSCustomObject -Property ([ordered]@{
            Name = $Name
            SourceLocation = $LocationString
            Trusted=$Trusted
            Registered= $true
        })    

    $script:AppxPackageSources.Add($Name, $packageSource)
    $message = $LocalizedData.SourceRegistered -f ($Name, $LocationString)
    Write-Verbose $message

    # Persist the package sources
    Save-PackageSources

    # return the package source object.
    Write-Output -InputObject (New-PackageSourceFromSource -Source $packageSource)

}

function Resolve-PackageSource
{ 
    Write-Debug ($LocalizedData.ProviderApiDebugMessage -f ('Resolve-PackageSource'))

    Set-PackageSourcesVariable

    $SourceName = $request.PackageSources

    if(-not $SourceName)
    {
        $SourceName = "*"
    }

    foreach($src in $SourceName)
    {
        if($request.IsCanceled)
        {
            return
        }

        $wildcardPattern = New-Object System.Management.Automation.WildcardPattern $src,$script:wildcardOptions
        $sourceFound = $false

        $script:AppxPackageSources.GetEnumerator() | 
            Microsoft.PowerShell.Core\Where-Object {$wildcardPattern.IsMatch($_.Key)} | 
                Microsoft.PowerShell.Core\ForEach-Object {
                    $source = $script:AppxPackageSources[$_.Key]
                    $packageSource = New-PackageSourceFromSource -Source $source
                    Write-Output -InputObject $packageSource
                    $sourceFound = $true
                }

        if(-not $sourceFound)
        {
            $sourceName  = Get-SourceName -Location $src
            if($sourceName)
            {
                $source = $script:AppxPackageSources[$sourceName]
                $packageSource = New-PackageSourceFromSource -Source $source
                Write-Output -InputObject $packageSource
            }
            elseif( -not (Test-WildcardPattern $src))
            {
                $message = $LocalizedData.PackageSourceNotFound -f ($src)
                Write-Error -Message $message -ErrorId "PackageSourceNotFound" -Category InvalidOperation -TargetObject $src
            }
        }
    }
}

function Remove-PackageSource
{ 
    param
    (
        [string]
        $Name
    )

    Write-Debug ($LocalizedData.ProviderApiDebugMessage -f ('Remove-PackageSource'))

    Set-PackageSourcesVariable -Force

    $SourcesToBeRemoved = @()

    foreach ($sourceName in $Name)
    {
        if($request.IsCanceled)
        {
            return
        }

        # Check if $Name contains any wildcards
        if(Test-WildcardPattern $sourceName)
        {
            $message = $LocalizedData.PackageSourceNameContainsWildCards -f ($sourceName)
            Write-Error -Message $message -ErrorId "PackageSourceNameContainsWildCards" -Category InvalidOperation -TargetObject $sourceName
            continue
        }

        # Check if the specified package source name is in the registered package sources
        if(-not $script:AppxPackageSources.Contains($sourceName))
        {
            $message = $LocalizedData.PackageSourceNotFound -f ($sourceName)
            Write-Error -Message $message -ErrorId "PackageSourceNotFound" -Category InvalidOperation -TargetObject $sourceName
            continue
        }

        $SourcesToBeRemoved += $sourceName
        $message = $LocalizedData.PackageSourceUnregistered -f ($sourceName)
        Write-Verbose $message
    }

    # Remove the SourcesToBeRemoved
    $SourcesToBeRemoved | Microsoft.PowerShell.Core\ForEach-Object { $null = $script:AppxPackageSources.Remove($_) }

    # Persist the package sources
    Save-PackageSources
}
#endregion

#region Common functions

function Get-AppxPackagesFromPath
{
    param
    (
        [Parameter(Mandatory=$true)]
        $Path
    )

    $filterAppxPackages = "*"+$script:AppxPackageExtension
    $packages = Get-ChildItem -path $Path -filter $filterAppxPackages

    return $packages

}

function Get-PackageManfiestData
{
    param
    (
        [Parameter(Mandatory=$true)]
        $PackageFullPath
    )        
    $guid = [System.Guid]::NewGuid().toString()
    try
    {
        [System.IO.Compression.ZipFile]::ExtractToDirectory($PackageFullPath, "$env:TEMP\$guid")    
    }
    catch
    {    
        Write-Verbose( $LocalizedData.MetaDataExtractionFailed -f ($PackageFullPath) )
        return $null
    }
            
    [xml] $packageManifest = Get-Content "$env:TEMP\$guid\$script:AppxManifestFile" -ErrorAction SilentlyContinue
    if($packageManifest)
    {
        $Identity = $packageManifest.Package.Identity   
        $manifestData = new-object psObject -Property @{Name=$Identity.Name; Architecture=$Identity.ProcessorArchitecture; Publisher=$Identity.Publisher; Version=$Identity.Version; ResourceId=$Identity.resourceId; PackageFullName=$PackageFullPath}
        Remove-Item -Path "$env:TEMP\$guid" -Recurse -Force -ErrorAction SilentlyContinue
        return $manifestData
    }
    else
    {
        Write-Verbose ($LocalizedData.MetaDataExtractionFailed -f ($PackageFullPath) )
    }
    return $null    
}

function New-FastPackageReference
{
    param
    (
        [Parameter(Mandatory=$true)]
        [string]
        $PackageFullName
    )
    return "$PackageFullName"
}

function New-SoftwareIdentityFromPackage
{
    param
    (
        [Parameter(Mandatory=$true)]
        $Package,

        [string]
        $Source

    )

    $fastPackageReference = New-FastPackageReference -PackageFullName $Package.PackageFullName                              
    
    if(-not($Source))
    {
        $Source = $Package.Publisher
    }
    $details =  @{
                    Publisher = $Package.Publisher
                    Architecture = $Package.Architecture
                    ResourceId = $Package.ResourceId
                    PackageFullName = $Package.PackageFullName
                 }

    $params = @{
                FastPackageReference = $fastPackageReference;
                Name = $Package.Name;
                Version = $Package.Version;
                versionScheme  = "MultiPartNumeric";
                Source = $source;
                Details = $details;
               }

    $sid = New-SoftwareIdentity @params
    return $sid
}

function Test-WebUri
{
    [CmdletBinding()]
    [OutputType([bool])]
    Param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $uri
    )

    return ($uri.AbsoluteURI -ne $null) -and ($uri.Scheme -match '[http|https]')
}

function Test-WildcardPattern
{
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNull()]
        $Name
    )

    return [System.Management.Automation.WildcardPattern]::ContainsWildcardCharacters($Name)    
}

function DeSerialize-PSObject
{
    [CmdletBinding(PositionalBinding=$false)]    
    Param
    (
        [Parameter(Mandatory=$true)]        
        $Path
    )
    $filecontent = Microsoft.PowerShell.Management\Get-Content -Path $Path
    [System.Management.Automation.PSSerializer]::Deserialize($filecontent)    
}

function Get-SourceName
{
    [CmdletBinding()]
    [OutputType("string")]
    Param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Location
    )

    Set-PackageSourcesVariable

    foreach($source in $script:AppxPackageSources.Values)
    {
        if($source.SourceLocation -eq $Location)
        {
            return $source.Name
        }
    }
}

function WebRequestApisAvailable
{
    $webRequestApiAvailable = $false
    try 
    {
        [System.Net.WebRequest]
        $webRequestApiAvailable = $true
    } 
    catch 
    {
    }
    return $webRequestApiAvailable
}

function Ping-Endpoint
{
    param
    (
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Endpoint
    )   
        
    $results = @{}

    if(WebRequestApisAvailable)
    {
        $iss = [System.Management.Automation.Runspaces.InitialSessionState]::Create()
        $iss.types.clear()
        $iss.formats.clear()
        $iss.LanguageMode = "FullLanguage"

        $WebRequestcmd =  @'
            try
            {{
                $request = [System.Net.WebRequest]::Create("{0}")
                $request.Method = 'GET'
                $request.Timeout = 30000
                $response = [System.Net.HttpWebResponse]$request.GetResponse()             
                $response
                $response.Close()
            }}
            catch [System.Net.WebException]
            {{
                "Error:System.Net.WebException"
            }} 
'@ -f $EndPoint

        $ps = [powershell]::Create($iss).AddScript($WebRequestcmd)
        $response = $ps.Invoke()
        $ps.dispose()

        if ($response -ne "Error:System.Net.WebException")
        {
            $results.Add($Script:ResponseUri,$response.ResponseUri.ToString())
            $results.Add($Script:StatusCode,$response.StatusCode.value__)
        }        
    }
    else
    {
        $response = $null
        try
        {
            $httpClient = New-Object 'System.Net.Http.HttpClient'
            $response = $httpclient.GetAsync($endpoint)          
        }
        catch
        {            
        } 

        if ($response -ne $null -and $response.result -ne $null)
        {        
            $results.Add($Script:ResponseUri,$response.Result.RequestMessage.RequestUri.AbsoluteUri.ToString())
            $results.Add($Script:StatusCode,$response.result.StatusCode.value__)            
        }
    }
    return $results
}

function Get-ValidPackageLocation
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $LocationString,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $ParameterName
    )

    # Get the actual Uri from the Location
    if(-not (Microsoft.PowerShell.Management\Test-Path $LocationString))
    {
        $results = Ping-Endpoint -Endpoint $LocationString
    
        if ($results.ContainsKey("Exception"))
        {
            $Exception = $results["Exception"]
            if($Exception)
            {
                $message = $LocalizedData.InvalidWebUri -f ($LocationString, $ParameterName)
                ThrowError -ExceptionName "System.ArgumentException" `
                            -ExceptionMessage $message `
                            -ErrorId "InvalidWebUri" `
                            -ExceptionObject $Exception `
                            -CallerPSCmdlet $PSCmdlet `
                            -ErrorCategory InvalidArgument
            }
        }

        if ($results.ContainsKey("ResponseUri"))
        {
            $LocationString = $results["ResponseUri"]
        }
    }

    return $LocationString
}

function Set-PackageSourcesVariable
{
    param([switch]$Force)

    if(-not $script:AppxPackageSources -or $Force)
    {
        if(Microsoft.PowerShell.Management\Test-Path $script:AppxPackageSourcesFilePath)
        {
            $script:AppxPackageSources = DeSerialize-PSObject -Path $script:AppxPackageSourcesFilePath
        }
        else
        {
            $script:AppxPackageSources = [ordered]@{}
        }
    }   
}

function Save-PackageSources
{
    if($script:AppxPackageSources)
    {
        if(-not (Microsoft.PowerShell.Management\Test-Path $script:AppxLocalPath))
        {
            $null = Microsoft.PowerShell.Management\New-Item -Path $script:AppxLocalPath `
                                                             -ItemType Directory -Force `
                                                             -ErrorAction SilentlyContinue `
                                                             -WarningAction SilentlyContinue `
                                                             -Confirm:$false -WhatIf:$false
        }        
        Microsoft.PowerShell.Utility\Out-File -FilePath $script:AppxPackageSourcesFilePath -Force -InputObject ([System.Management.Automation.PSSerializer]::Serialize($script:AppxPackageSources))
   }   
}

function New-PackageSourceFromSource
{
    param
    (
        [Parameter(Mandatory)]
        $Source
    )
     
    # create a new package source
    $src =  New-PackageSource -Name $Source.Name `
                              -Location $Source.SourceLocation `
                              -Trusted $Source.Trusted `
                              -Registered $Source.Registered `

    Write-Verbose ( $LocalizedData.PackageSourceDetails -f ($src.Name, $src.Location, $src.IsTrusted, $src.IsRegistered) )

    # return the package source object.
    Write-Output -InputObject $src
}
#endregion

# Utility to throw an errorrecord
function ThrowError
{
    param
    (        
        [parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]
        $CallerPSCmdlet,

        [parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]        
        $ExceptionName,

        [parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $ExceptionMessage,
        
        [System.Object]
        $ExceptionObject,
        
        [parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $ErrorId,

        [parameter(Mandatory = $true)]
        [ValidateNotNull()]
        [System.Management.Automation.ErrorCategory]
        $ErrorCategory
    )
          
    $exception = New-Object $ExceptionName $ExceptionMessage;
    $errorRecord = New-Object System.Management.Automation.ErrorRecord $exception, $ErrorId, $ErrorCategory, $ExceptionObject    
    $CallerPSCmdlet.ThrowTerminatingError($errorRecord)    
}
#endregion

Export-ModuleMember -Function Find-Package, `
                              Install-Package, `
                              Uninstall-Package, `
                              Get-InstalledPackage, `
                              Remove-PackageSource, `
                              Resolve-PackageSource, `
                              Add-PackageSource, `
                              Get-DynamicOptions, `
                              Initialize-Provider, `
                              Get-PackageProviderName
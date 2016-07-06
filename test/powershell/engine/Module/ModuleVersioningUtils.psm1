<##################################################################################### 
 # File: ModuleVersioningUtils.psm1
 # Utils for Module Versioning Tests
 #
 # Copyright (c) Microsoft Corporation, 2014
 #####################################################################################>

<#
.Synopsis
   get current running script's directory
.DESCRIPTION
   get current running script's directory
.EXAMPLE
   Get-RunningScriptDirectory

#>
function Get-RunningScriptDirectory
{
    $invocation = (Get-Variable MyInvocation -Scope 1).Value
    Split-Path $invocation.MyCommand.Path
}

#return the root path of the test code
$global:scriptDirectory = (Get-Item (Get-RunningScriptDirectory)).FullName

<#
.Synopsis
   Install-MultiVersionedModule
.DESCRIPTION
   Creates and installs the multi versioned module with specified valid and invalid versions to the specified path
.EXAMPLE
   Install-MultiVersionedModule -ModuleName "MyTestModule" -ModuleGuid $MyTestModuleGuid -Versions "1.0","2.0","3.0","4.0","5.0" -InvalidVersions "6.0","7.0" -ModulePath "c:\test"
#>
function Install-MultiVersionedModule
{
    param(
        # The name of module
        [Parameter(Mandatory=$true)]
        [string] 
        $ModuleName,

        # The Guid of the module
        [Parameter()]
        [string] 
        $ModuleGuid = [Guid]::NewGuid(),

        # The valid versions
        [Parameter()]
        [string[]]
        $Versions = @("1.0","2.0","3.0","4.0","5.0"),

        # Additional parameters to New-ModuleManifest cmdlet
        [Parameter()]
        [System.Collections.Hashtable]
        $NewModuleManifestParams = @{},

        # the invalid versions wher folder name is different from ModuleVersion in the manifest file
        [Parameter()]
        [string[]]
        $InvalidVersions,

        # The module destination location
        [Parameter()]
        [string]
        $ModulePath = "$env:ProgramFiles\WindowsPowerShell\Modules"
    )

    $moduledir = Join-Path $ModulePath $ModuleName

    foreach($Version in $Versions)
    {
        $ModuleBase = $moduledir

        if($Version -ne "1.0")
        {
            $ModuleBase = Join-Path $ModuleBase $Version
        }

        $ScriptModuleContent = @"
        function Get-$moduleName
        {
            "$moduleName $Version"
        }
"@
        $null = New-Item -Path $ModuleBase -ItemType Directory -Force
        Set-Content "$ModuleBase\$ModuleName.psm1" -Value $ScriptModuleContent
        New-ModuleManifest -Path "$ModuleBase\$ModuleName.psd1" -ModuleVersion $Version -Description "$ModuleName module"  -NestedModules "$ModuleName.psm1" -Guid $ModuleGuid @NewModuleManifestParams
    }


    foreach($Version in $InvalidVersions)
    {
        $ModuleBase = $moduledir

        if($Version -ne "1.0")
        {
            $ModuleBase = Join-Path $ModuleBase $Version
        }

        $invalidVersion = "99.88.77.66"

        $null = New-Item -Path $ModuleBase -ItemType Directory -Force
        
        New-ModuleManifest -Path "$ModuleBase\$ModuleName.psd1" -ModuleVersion $invalidVersion -Description "$ModuleName module"  -Guid $ModuleGuid @NewModuleManifestParams
    }
}

<#
.Synopsis
   Uninstall-Module
.DESCRIPTION
   Uninstalls the specified module, which should be listed with Get-Module -ListAvailable MyTestModule
.EXAMPLE
   Uninstall-Module -Name MyTestModule
#>
function Uninstall-Module
{
    Param(
        # the module name
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Name
    )

    Get-Module $Name -ListAvailable | %{ Remove-Module $_ -Force -ErrorAction SilentlyContinue; Remove-item $_.ModuleBase -Recurse -Force -WarningAction SilentlyContinue}
}

<#
.Synopsis
   RemoveItem
.DESCRIPTION
   Wrapper utility for Remove-Item cmdlet
.EXAMPLE
   RemoveItem -Path "c:\test"
#>
function RemoveItem
{
    Param(
        # The path to be removed
        [ValidateNotNullOrEmpty()]
        [string]
        $Path
    )

    if(Test-Path $Path)
    {
        Remove-Item $Path -Force -Recurse -ErrorAction SilentlyContinue
    }
}

<#
.Synopsis
   Generate-Modules
.DESCRIPTION
   Generate huge number of modules with different versions. The function can also generate invalid folders among valid ones
.EXAMPLE
   Generate-Modules -number 100 -addNoise $true -outPutPath c:\test
#>
function Generate-Modules
{

    param
    (
        
        # the number of modules the function will generate
        [Int] $number = 10,

        # if set as true, generation will generate some invalid modules
        [Boolean] $addNoise,

        # the module name
        [String] $moduleName = "TestModule",
               
        # the modules output path
        [String] $outPutPath = ".\")

    begin
    {
        Write-Verbose "Start generating $number modules..."
    }
    process
    {
        $initialVersion = "1.0.0"

        $versionBuild = 0

        $modulePath = Join-Path $outPutPath $moduleName

        if (!(Test-Path $modulePath))
        {
            md $modulePath
        }
        else
        {
            Remove-Item $modulePath\*.* -Force -Recurse
        }

        $moduleGuidId = [guid]::NewGuid().Guid

        for ($i = 0; $i -lt $number; $i++)
        {
            if ($addNoise)
            {
                if (Get-Random 2)
                {
                    $folderName = [guid]::NewGuid().Guid
                    $invalidFolder = $modulePath + "\$folderName"
                    md $invalidFolder
                    "function foo {write-verbose invalidFolder}" > ($invalidFolder + '\' + $moduleName + ".psm1")
                }
            }

            #increase the valid version number randomly.
            $versionBuild = $versionBuild + (Get-Random -Minimum 1 -Maximum 3)
            $version = $initialVersion + '.' + $versionBuild
            $versionFolder = $modulePath + "\$version"
            md $versionFolder

            New-ModuleManifest -Path ($versionFolder + '\' + $moduleName + ".psd1") -ModuleVersion $version -Guid $moduleGuidId -RootModule ($moduleName + ".psm1")

            #generate the psm1 file
            $psm1FileContent = "function foo {write-host $version}"
            $psm1FileContent > ($versionFolder + '\' + $moduleName + ".psm1")


        }

        return $version
    }
}


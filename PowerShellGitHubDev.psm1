# Use the .NET Core APIs to determine the current platform; if a runtime
# exception is thrown, we are on FullCLR, not .NET Core.
#
# TODO: import Microsoft.PowerShell.Platform instead
try {
    $Runtime = [System.Runtime.InteropServices.RuntimeInformation]
    $OSPlatform = [System.Runtime.InteropServices.OSPlatform]

    $IsCore = $true
    $IsLinux = $Runtime::IsOSPlatform($OSPlatform::Linux)
    $IsOSX = $Runtime::IsOSPlatform($OSPlatform::OSX)
    $IsWindows = $Runtime::IsOSPlatform($OSPlatform::Windows)
} catch [System.Management.Automation.RuntimeException] {
    $IsCore = $false
    $IsLinux = $false
    $IsOSX = $false
    $IsWindows = $true
}

function Start-PSBuild
{
    [CmdletBinding(DefaultParameterSetName='CoreCLR')]
    param(
        [switch]$Restore,
        [switch]$Clean,

        # These runtimes must match those in project.json
        # We do not use ValidateScript since we want tab completion
        [ValidateSet("ubuntu.14.04-x64",
                     "centos.7.1-x64",
                     "win7-x64",
                     "win10-x64",
                     "osx.10.10-x64",
                     "osx.10.11-x64")]
        [Parameter(ParameterSetName='CoreCLR')]
        [string]$Runtime,

        [Parameter(ParameterSetName='FullCLR')]
        [switch]$FullCLR,

        [Parameter(ParameterSetName='FullCLR')]
        [string]$cmakeGenerator = "Visual Studio 14 2015",

        [Parameter(ParameterSetName='FullCLR')]
        [ValidateSet("Debug",
                     "Release")] 
        [string]$msbuildConfiguration = "Release"   
    )

    function precheck([string]$command, [string]$missedMessage)
    {
        $c = Get-Command $command -ErrorAction SilentlyContinue
        if (-not $c)
        {
            Write-Warning $missedMessage
            return $false
        }
        else 
        {
            return $true    
        }
    }

    function log([string]$message)
    {
        Write-Host -Foreground Green $message
    }

    # simplify ParameterSetNames
    if ($PSCmdlet.ParameterSetName -eq 'FullCLR')
    {
        $FullCLR = $true
    }

    # verify we have all tools in place to do the build
    $precheck = precheck 'dotnet' "Build dependency 'dotnet' not found in PATH! See: https://dotnet.github.io/getting-started/"
    if ($FullCLR)
    {
        # cmake is needed to build powershell.exe
        $precheck = $precheck -and (precheck 'cmake' 'cmake not found. You can install it from https://chocolatey.org/packages/cmake.portable')
        
        # msbuild is needed to build powershell.exe
        # msbuild is part of .NET Framework, we can try to get it from well-known location.
        if (-not (Get-Command -Name msbuild -ErrorAction Ignore))
        {
            $env:path += ";${env:SystemRoot}\Microsoft.Net\Framework\v4.0.30319"
        }

        $precheck = $precheck -and (precheck 'msbuild' 'msbuild not found. Install Visual Studio 2015.')
    }
    
    if (-not $precheck) { return }

    # define key build variables
    if ($FullCLR) 
    {
        $Top = "$PSScriptRoot\src\Microsoft.PowerShell.ConsoleHost"
        $framework = 'net451'
    }
    else 
    {
        $Top = "$PSScriptRoot/src/Microsoft.PowerShell.Linux.Host"   
        $framework = 'netstandardapp1.5'
    }

    # handle Restore
    if ($Restore -Or -Not (Test-Path "$Top/project.lock.json")) {
        log "Run dotnet restore"

        $Arguments = @("--verbosity")
        if ($PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent) {
            $Arguments += "Info" } else { $Arguments += "Warning" }

        if ($Runtime) { $Arguments += "--runtime", $Runtime }

        $Arguments += "$PSScriptRoot"

        dotnet restore $Arguments
    }

    # Build native components
    if (-not $FullCLR)
    {
        if ($IsLinux -Or $IsOSX) {
            log "Start building native components"
            $InstallCommand = if ($IsLinux) { "apt-get" } elseif ($IsOSX) { "brew" }
            foreach ($Dependency in "cmake", "g++") {
                if (-Not (Get-Command $Dependency -ErrorAction SilentlyContinue)) {
                    throw "Build dependency '$Dependency' not found in PATH! Run '$InstallCommand install $Dependency'"
                }
            }

            $Ext = if ($IsLinux) { "so" } elseif ($IsOSX) { "dylib" }
            $Native = "$PSScriptRoot/src/libpsl-native"
            $Lib = "$Top/libpsl-native.$Ext"
            Write-Verbose "Building $Lib"

            try {
                Push-Location $Native
                cmake -DCMAKE_BUILD_TYPE=Debug .
                make -j
                make test
            } finally {
                Pop-Location
            }

            if (-Not (Test-Path $Lib)) { throw "Compilation of $Lib failed" }
        }
    }
    else
    {
        log "Start building native powershell.exe"
        $build = "$PSScriptRoot/build"
        if ($Clean) {
            Remove-Item -Force -Recurse $build -ErrorAction SilentlyContinue
        }

        mkdir $build -ErrorAction SilentlyContinue
        try
        {
            Push-Location $build

            if ($cmakeGenerator)
            {
                cmake -G $cmakeGenerator ..\src\powershell-native
            }
            else
            {
                cmake ..\src\powershell-native
            }
            msbuild powershell.vcxproj /p:Configuration=$msbuildConfiguration
            # cp -rec $msbuildConfiguration\* $Output
        }
        finally { Pop-Location }
    }

    log "Building PowerShell"
    $Arguments = "--framework", $framework
    if ($IsLinux -Or $IsOSX) { $Arguments += "--configuration", "Linux" }
    if ($Runtime) { $Arguments += "--runtime", $Runtime }
    
    Write-Verbose "Run dotnet publish $Arguments from $pwd"

    # this try-finally is part of workaround about AssemblyKeyFileAttribute issue
    try 
    {
        # Relative paths do not work well if cwd is not changed to project
        Push-Location $Top
        dotnet build $Arguments
    }
    finally
    {
        Pop-Location
    }
}

function Start-PSPackage
{
    # PowerShell packages use Semantic Versioning http://semver.org/
    #
    # Ubuntu and OS X packages are supported.
    param(
        [string]$Version,
        [int]$Iteration = 1,
        [ValidateSet("deb", "osxpkg", "rpm")]
        [string]$Type
    )

    if ($IsWindows) { throw "Building Windows packages is not yet supported!" }

    if (-Not (Get-Command "fpm" -ErrorAction SilentlyContinue)) {
        throw "Build dependency 'fpm' not found in PATH! See: https://github.com/jordansissel/fpm"
    }

    if (-Not(Test-Path "$PSScriptRoot/bin/powershell")) {
        throw "Please Start-PSBuild with the corresponding runtime for the package"
    }

    # Change permissions for packaging
    chmod -R go=u "$PSScriptRoot/bin"

    # Decide package output type
    if (-Not($Type)) {
        $Type = if ($IsLinux) { "deb" } elseif ($IsOSX) { "osxpkg" }
        Write-Warning "-Type was not specified, continuing with $Type"
    }

    # Use Git tag if not given a version
    if (-Not($Version)) {
        $Version = (git --git-dir="$PSScriptRoot/.git" describe) -Replace '^v'
    }

    $libunwind = switch ($Type) {
        "deb" { "libunwind8" }
        "rpm" { "libunwind" }
    }

    $libicu = switch ($Type) {
        "deb" { "libicu52" }
        "rpm" { "libicu" }
    }

    # Build package
    fpm --force --verbose `
        --name "powershell" `
        --version $Version `
        --iteration $Iteration `
        --maintainer "Andrew Schwartzmeyer <andschwa@microsoft.com>" `
        --vendor "Microsoft <mageng@microsoft.com>" `
        --url "https://github.com/PowerShell/PowerShell" `
        --license "Unlicensed" `
        --description "Open PowerShell on .NET Core\nPowerShell is an open-source, cross-platform, scripting language and rich object shell. Built upon .NET Core, it is also a C# REPL.\n" `
        --category "shells" `
        --depends $libunwind `
        --depends $libicu `
        --deb-build-depends "dotnet" `
        --deb-build-depends "cmake" `
        --deb-build-depends "g++" `
        -t $Type `
        -s dir `
        "$PSScriptRoot/bin/=/usr/local/share/powershell/" `
        "$PSScriptRoot/package/powershell=/usr/local/bin"
}

function Start-DevPSGitHub
{
    param(
        [switch]$ZapDisable,
        [string[]]$ArgumentList = '',
        [switch]$LoadProfile,
        [string]$binDir = "$PSScriptRoot\binFull",
        [switch]$NoNewWindow
    )

    try
    {
        if ($LoadProfile -eq $false)
        {
            $ArgumentList = @('-noprofile') + $ArgumentList
        }

        $env:DEVPATH = $binDir
        if ($ZapDisable)
        {
            $env:COMPLUS_ZapDisable = 1
        }

        if (-Not (Test-Path $binDir\powershell.exe.config))
        {
            $configContents = @"
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
    <runtime>
        <developmentMode developerInstallation="true"/>
    </runtime>
</configuration>
"@
            $configContents | Out-File -Encoding Ascii $binDir\powershell.exe.config
        }

        # splatting for the win
        $startProcessArgs = @{
            FilePath = "$binDir\powershell.exe"
            ArgumentList = "$ArgumentList"
        }

        if ($NoNewWindow) {
            $startProcessArgs.NoNewWindow = $true
            $startProcessArgs.Wait = $true
        }

        Start-Process @startProcessArgs
    }
    finally
    {
        ri env:DEVPATH
        if ($ZapDisable)
        {
            ri env:COMPLUS_ZapDisable
        }
    }
}

## this function is from Dave Wyatt's answer on
## http://stackoverflow.com/questions/22002748/hashtables-from-convertfrom-json-have-different-type-from-powershells-built-in-h
function Convert-PSObjectToHashtable
{
    param (
        [Parameter(ValueFromPipeline)]
        $InputObject
    )

    process
    {
        if ($null -eq $InputObject) { return $null }

        if ($InputObject -is [System.Collections.IEnumerable] -and $InputObject -isnot [string])
        {
            $collection = @(
                foreach ($object in $InputObject) { Convert-PSObjectToHashtable $object }
            )

            Write-Output -NoEnumerate $collection
        }
        elseif ($InputObject -is [psobject])
        {
            $hash = @{}

            foreach ($property in $InputObject.PSObject.Properties)
            {
                $hash[$property.Name] = Convert-PSObjectToHashtable $property.Value
            }

            $hash
        }
        else
        {
            $InputObject
        }
    }
}

<#
.EXAMPLE Copy-SubmoduleFiles                # copy files FROM submodule TO src/<project> folders
.EXAMPLE Copy-SubmoduleFiles -ToSubmodule   # copy files FROM src/<project> folders TO submodule
#>
function Copy-SubmoduleFiles {
    
    [CmdletBinding()]
    param(
        [string]$mappingFilePath = "$PSScriptRoot/mapping.json",
        [switch]$ToSubmodule
    )

    
    if (-not (Test-Path $mappingFilePath))
    {
        throw "Mapping file not found in $mappingFilePath"
    }

    $m = cat -Raw $mappingFilePath | ConvertFrom-Json | Convert-PSObjectToHashtable

    # mapping.json assumes the root folder
    Push-Location $PSScriptRoot
    try
    {
        $m.GetEnumerator() | % {

            if ($ToSubmodule)
            {
                cp $_.Value $_.Key -Verbose:$Verbose
            }
            else 
            {
                mkdir (Split-Path $_.Value) -ErrorAction SilentlyContinue > $null
                cp $_.Key $_.Value -Verbose:$Verbose  
            }
        }
    }
    finally
    {
        Pop-Location
    }
}

<#
    .EXAMPLE Create-MappingFile # create mapping.json in the root folder from project.json files
#>
function New-MappingFile
{
    param(
        [string]$mappingFilePath = "$PSScriptRoot/mapping.json",
        [switch]$IgnoreCompileFiles,
        [switch]$Ignoreresource
    )

    function Get-MappingPath([string]$project, [string]$path)
    {
        if ($project -match 'TypeCatalogGen')
        {
            return Split-Path $path -Leaf
        }
        
        if ($project -match 'Microsoft.Management.Infrastructure')
        {
            return Split-Path $path -Leaf
        }

        return ($path -replace '../monad/monad/src/', '')
    }

    $mapping = [ordered]@{}

    # assumes the root folder
    Push-Location $PSScriptRoot
    try
    {
        $projects = ls .\src\ -Recurse -Depth 2 -Filter 'project.json'
        $projects | % {
            $project = Split-Path $_.FullName
            $json = cat -Raw -Path $_.FullName | ConvertFrom-Json
            if (-not $IgnoreCompileFiles) {
                $json.compileFiles | % {
                    if ($_) {
                        if (-not $_.EndsWith('AssemblyInfo.cs'))
                        {
                            $fullPath = Join-Path $project (Get-MappingPath -project $project -path $_)
                            $mapping[$_.Replace('../', 'src/')] = ($fullPath.Replace("$($pwd.Path)\",'')).Replace('\', '/')
                        }
                    }
                }
            }

            if ((-not $Ignoreresource) -and ($json.resource)) {
                $json.resource | % {
                    if ($_) {
                        ls $_.Replace('../', 'src/') | % {
                            $fullPath = Join-Path $project (Join-Path 'resources' $_.Name)
                            $mapping[$_.FullName.Replace("$($pwd.Path)\", '').Replace('\', '/')] = ($fullPath.Replace("$($pwd.Path)\",'')).Replace('\', '/')
                        }
                    }
                }
            }
        }
    }
    finally
    {
        Pop-Location
    }

    Set-Content -Value ($mapping | ConvertTo-Json) -Path $mappingFilePath -Encoding Ascii
}

function Get-InvertedOrderedMap
{
    param(
        $h
    )
    $res = [ordered]@{}
    foreach ($q in $h.GetEnumerator()) {
        if ($res.Contains($q.Value))
        {
            throw "Cannot invert hashtable: duplicated key $($q.Value)"
        }

        $res[$q.Value] = $q.Key
    }
    return $res
}

<#
.EXAMPLE Send-GitDiffToSd -diffArg1 45555786714d656bd31cbce67dbccb89c433b9cb -diffArg2 45555786714d656bd31cbce67dbccb89c433b9cb~1 -pathToAdmin d:\e\ps_dev\admin 
Apply a signle commit to admin folder
#>
function Send-GitDiffToSd
{
    param(
        [Parameter(Mandatory)]
        [string]$diffArg1,
        [Parameter(Mandatory)]
        [string]$diffArg2,
        [Parameter(Mandatory)]
        [string]$pathToAdmin,
        [string]$mappingFilePath = "$PSScriptRoot/mapping.json",
        [switch]$WhatIf
    )

    $patchPath = Join-Path (get-command git).Source ..\..\bin\patch
    $m = cat -Raw $mappingFilePath | ConvertFrom-Json | Convert-PSObjectToHashtable
    $affectedFiles = git diff --name-only $diffArg1 $diffArg2
    $rev = Get-InvertedOrderedMap $m
    foreach ($file in $affectedFiles) {
        if ($rev.Contains)
        {
            $sdFilePath = Join-Path $pathToAdmin $rev[$file].Substring('src/monad/'.Length)
            $diff = git diff $diffArg1 $diffArg2 -- $file
            if ($diff)
            {
                Write-Host -Foreground Green "Apply patch to $sdFilePath"
                Set-Content -Value $diff -Path $env:TEMP\diff -Encoding Ascii
                if ($WhatIf)
                {
                    Write-Host -Foreground Green "Patch content"
                    cat $env:TEMP\diff
                }
                else 
                {
                    & $patchPath --binary -p1 $sdFilePath $env:TEMP\diff        
                }
            }
            else 
            {
                Write-Host -Foreground Green "No changes in $file"
            }
        }
        else 
        {
            Write-Host -Foreground Green "Ignore changes in $file, because there is no mapping for it"
        }
    }    
}

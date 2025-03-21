# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

# Do NOT edit this file.  Edit dobuild.ps1
[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingWriteHost", "")]
param (
    [Parameter(ParameterSetName="build")]
    [switch]
    $Clean,

    [Parameter(ParameterSetName="build")]
    [switch]
    $Build,

    [Parameter(ParameterSetName="publish")]
    [switch]
    $Publish,

    [Parameter(ParameterSetName="publish")]
    [switch]
    $Signed,

    [Parameter(ParameterSetName="build")]
    [switch]
    $Test,

    [Parameter(ParameterSetName="build")]
    [string[]]
    [ValidateSet("Functional","StaticAnalysis")]
    $TestType = @("Functional"),

    [Parameter(ParameterSetName="help")]
    [switch]
    $UpdateHelp,

    [ValidateSet("Debug", "Release")]
    [string] $BuildConfiguration = "Debug",

    [ValidateSet("net10.0")]
    [string] $BuildFramework = "net10.0"
)

$script:ModuleName = 'Microsoft.PowerShell.NamedPipeConnection'
$script:SrcPath = Join-Path -Path $PSScriptRoot -ChildPath 'src'
$script:OutDirectory = Join-Path -Path $PSScriptRoot -ChildPath 'out'

$script:BuildConfiguration = $BuildConfiguration
$script:BuildFramework = $BuildFramework

. $PSScriptRoot/doBuild.ps1

if ($Clean.IsPresent)
{
    if (Test-Path "${PSScriptRoot}/out")
    {
        Remove-Item -Path "${PSScriptRoot}/out" -Force -Recurse -ErrorAction Stop -Verbose
    }

    if (Test-Path "${SrcPath}/code/bin")
    {
        Remove-Item -Path "${SrcPath}/code/bin" -Recurse -Force -ErrorAction Stop -Verbose
    }

    if (Test-Path "${SrcPath}/code/obj")
    {
        Remove-Item -Path "${SrcPath}/code/obj" -Recurse -Force -ErrorAction Stop -Verbose
    }
}

if ($Build.IsPresent)
{
    if (-not (Test-Path $OutDirectory))
    {
        $script:OutModule = New-Item -ItemType Directory -Path (Join-Path $OutDirectory $ModuleName)
    }
    else
    {
        $script:OutModule = Join-Path $OutDirectory $ModuleName
    }

    Write-Verbose -Verbose -Message "Invoking DoBuild script"
    DoBuild
    Write-Verbose -Verbose -Message "Finished invoking DoBuild script"
}

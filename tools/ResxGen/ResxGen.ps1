# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

<#
.SYNOPSIS
    Generates a resx file and code file from an ETW manifest
    for use with UNIX builds.

.PARAMETER Manifest
    The path to the ETW manifest file to read.

.PARAMETER Name
    The name to use for the C# class, the code file, and the resx file.
    The default value is EventResource.

.PARAMETER Namespace
    The namespace to place the C# class.
    The default is System.Management.Automation.Tracing.

.PARAMETER ResxPath
    The path to the directory to use to create the resx file.

.PARAMETER CodePath
    The path to the directory to use to create the C# code file.

.EXAMPLE
    .\tools\ResxGen\ResxGen.ps1 -Manifest .\src\PowerShell.Core.Instrumentation\PowerShell.Core.Instrumentation.man -ResxPath .\src\System.Management.Automation\resources -CodePath .\src\System.Management.Automation\CoreCLR
#>
[CmdletBinding()]
param
(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string] $Manifest,

    [string] $Name = 'EventResource',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $Namespace = 'System.Management.Automation.Tracing',

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string] $ResxPath,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string] $CodePath

)

Import-Module $PSScriptRoot\ResxGen.psm1 -Force
try
{
    ConvertTo-Resx -Manifest $Manifest -Name $Name -ResxPath $ResxPath -CodePath $CodePath -Namespace $Namespace
}
finally
{
    Remove-Module ResxGen -Force -ErrorAction Ignore
}


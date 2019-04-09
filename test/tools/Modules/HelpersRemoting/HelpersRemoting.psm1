# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
#
# This module include help functions for writing remoting tests
#

$Script:CIRemoteCred = $null

if ($IsWindows) {
    try { $Script:CIRemoteCred = Import-Clixml -Path "$env:TEMP\CIRemoteCred.xml" } catch { }
}

function Get-DefaultEndPointName
{
    $endPointName = "PowerShell.$(${PSVersionTable}.GitCommitId)"
    $endPoint = Get-PSSessionConfiguration -Name $endPointName -ErrorAction SilentlyContinue

    if ($endPoint -eq $null)
    {
        Enable-PSRemoting -SkipNetworkProfileCheck
        $endPoint = Get-PSSessionConfiguration -Name $endPointName -ErrorAction SilentlyContinue

        if ($endPoint -eq $null)
        {
            Write-Warning "Unable to create the remoting configuration endpoint for this PowerShell version: $(${PSVersionTable}.PSVersion)"
            return $endPointName
        }
    }

    if ($endPoint.Permission -like "*NT AUTHORITY\NETWORK AccessDenied*")
    {
        Enable-PSRemoting -SkipNetworkProfileCheck
        $endPoint = Get-PSSessionConfiguration -Name $endPointName -ErrorAction SilentlyContinue

        if ($endPoint.Permission -like "*NT AUTHORITY\NETWORK AccessDenied*")
        {
            Write-Warning "Unable to enable the remoting configuration endpoint: $(${PSVersionTable}.PSVersion)"
        }
    }

    return $endPointName
}

function New-RemoteRunspace
{
    param (
        [string] $ConfigurationName
    )

    # For PSCore6, we want to always test against a remoting endpoint running PSCore6 (not Windows PowerShell)
    if ([string]::IsNullOrEmpty($ConfigurationName))
    {
        $ConfigurationName = Get-DefaultEndPointName
    }

    $wsmanConInfo = [System.Management.Automation.Runspaces.WSManConnectionInfo]::new()

    $wsmanConInfo.ShellUri = 'http://schemas.microsoft.com/powershell/' + $ConfigurationName

    if ($Script:CIRemoteCred)
    {
        Write-Verbose "Using Global CI Credential" -Verbose
        $wsmanConInfo.Credential = $Script:CIRemoteCred
    }
    else
    {
        Write-Verbose "Using Implicit Credential" -Verbose
    }

    $remoteRunspace = [runspacefactory]::CreateRunspace($Host, $wsmanConInfo)
    $remoteRunspace.Open()

    Write-Verbose "Successfully created remote runspace on endpoint: $ConfigurationName"

    return $remoteRunspace
}

function New-RemoteRunspacePool
{
    param (
        [int] $MinRunspace = 1,

        [int] $MaxRunspace = 6,

        [string] $ConfigurationName
    )

    $wsmanConnection = [System.Management.Automation.Runspaces.WSManConnectionInfo]::new()

    if ($ConfigurationName -ne $null)
    {
        $wsmanConnection.ShellUri = "http://schemas.microsoft.com/powershell/$ConfigurationName"
    }

    if ($Script:CIRemoteCred)
    {
        Write-Verbose "Using Global CI Credential" -Verbose
        $wsmanConnection.Credential = $Script:CIRemoteCred
    }
    else
    {
        Write-Verbose "Using Implicit Credential" -Verbose
    }

    [System.Management.Automation.Runspaces.RunspacePool] $remoteRunspacePool = [runspacefactory]::CreateRunspacePool($MinRunspace, $MaxRunspace, $wsmanConnection)
    $remoteRunspacePool.Open()

    return $remoteRunspacePool
}

function CreateParameters
{
    param (
        [string] $ComputerName,
        [string[]] $Name,
        [string] $ConfigurationName,
        [switch] $CimSession,
        [System.Management.Automation.Remoting.PSSessionOption] $SessionOption,
        [System.Management.Automation.Runspaces.PSSession[]] $Session)

    if($ComputerName)
    {
        $parameters = @{ComputerName = $ComputerName}
    }
    else
    {
        if($Session)
        {
            $parameters = @{Session = $Session}
        }
        else
        {
            $parameters = @{ComputerName = '.'}
        }
    }

    if ($Name) {
        if($CimSession.IsPresent)
        {
            $parameters["Name"] = [String] $Name
        }
        else
        {
            $parameters["Name"] = $Name
        }
    }

    if ($ConfigurationName) {
        $parameters["ConfigurationName"] = $ConfigurationName
    }

    if ($SessionOption) {
        $parameters["SessionOption"] = $SessionOption
    }

    ## If a PSSession is provided, do not add credentials.
    if ($Script:CIRemoteCred -and (-not $Session))
    {
        Write-Verbose "Using Global CI Credential" -Verbose
        $parameters["Credential"] = $Script:CIRemoteCred
    }
    else
    {
        Write-Verbose "Using Implicit Credential" -Verbose
    }

    return $parameters
}

function New-RemoteSession
{
    param (
        [string] $Name,
        [string] $ConfigurationName,
        [switch] $CimSession,
        [System.Management.Automation.Remoting.PSSessionOption] $SessionOption
    )

    # For PSCore6, we want to always test against a remoting endpoint running PSCore6 (not Windows PowerShell)
    if ([string]::IsNullOrEmpty($ConfigurationName))
    {
        $ConfigurationName = Get-DefaultEndPointName
    }

    $parameters = CreateParameters -Name $Name -ConfigurationName $ConfigurationName -SessionOption $SessionOption -CimSession:$CimSession.IsPresent

    if ($CimSession) {
        $session = New-CimSession @parameters
    } else {
        $session = New-PSSession @parameters
    }

    Write-Verbose "Successfully created remote PSSession on endpoint: $ConfigurationName"

    return $session
}

function Invoke-RemoteCommand
{
    param (
        [string] $ComputerName,
        [scriptblock] $ScriptBlock,
        [string] $ConfigurationName,
        [switch] $InDisconnectedSession
    )

    # For PSCore6, we want to always test against a remoting endpoint running PSCore6 (not Windows PowerShell)
    if ([string]::IsNullOrEmpty($ConfigurationName))
    {
        $ConfigurationName = Get-DefaultEndPointName
    }

    $parameters = CreateParameters -ComputerName $ComputerName -ConfigurationName $ConfigurationName

    if($ScriptBlock)
    {
        $parameters.Add('ScriptBlock', $ScriptBlock)
    }

    if($InDisconnectedSession)
    {
        $parameters.Add('InDisconnectedSession', $InDisconnectedSession.IsPresent)
    }

    Invoke-Command @parameters
}

function Enter-RemoteSession
{
    param(
        [string] $Name,
        [string] $ConfigurationName,
        [System.Management.Automation.Remoting.PSSessionOption] $SessionOption)

    $parameters = CreateParameters -Name $Name -ConfigurationName $ConfigurationName -SessionOption $SessionOption
    Enter-PSSession @parameters
}

function Connect-RemoteSession
{
    param(
        [string] $ComputerName,
        [string[]] $Name,
        [System.Management.Automation.Runspaces.PSSession[]] $Session,
        [string] $ConfigurationName
    )

    $parameters = CreateParameters -ComputerName $ComputerName -Name $Name -Session $Session -ConfigurationName $ConfigurationName
    Connect-PSSession @parameters
}

function Get-PipePath {
    param (
        $PipeName
    )
    if ($IsWindows) {
        return "\\.\pipe\$PipeName"
    }
    "$([System.IO.Path]::GetTempPath())CoreFxPipe_$PipeName"
}

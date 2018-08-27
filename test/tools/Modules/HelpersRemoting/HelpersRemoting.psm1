# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
#
# This module include help functions for writing remoting tests
#

$Script:AppVeyorRemoteCred = $null

if ($IsWindows) {
    try { $Script:AppVeyorRemoteCred = Import-Clixml -Path "$env:TEMP\AppVeyorRemoteCred.xml" } catch { }
}

function New-RemoteRunspace
{
    $wsmanConInfo = [System.Management.Automation.Runspaces.WSManConnectionInfo]::new()

    if ($Script:AppVeyorRemoteCred)
    {
        Write-Verbose "Using Global AppVeyor Credential" -Verbose
        $wsmanConInfo.Credential = $Script:AppVeyorRemoteCred
    }
    else
    {
        Write-Verbose "Using Implicit Credential" -Verbose
    }

    $remoteRunspace = [runspacefactory]::CreateRunspace($Host, $wsmanConInfo)
    $remoteRunspace.Open()

    return $remoteRunspace
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
    if ($Script:AppVeyorRemoteCred -and (-not $Session))
    {
        Write-Verbose "Using Global AppVeyor Credential" -Verbose
        $parameters["Credential"] = $Script:AppVeyorRemoteCred
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
        [System.Management.Automation.Remoting.PSSessionOption] $SessionOption)

    $parameters = CreateParameters -Name $Name -ConfigurationName $ConfigurationName -SessionOption $SessionOption -CimSession:$CimSession.IsPresent

    if ($CimSession) {
        $session = New-CimSession @parameters
    } else {
        $session = New-PSSession @parameters
    }

    return $session
}

function Invoke-RemoteCommand
{
    param (
        [string] $ComputerName,
        [scriptblock] $ScriptBlock,
        [string] $ConfigurationName,
        [switch] $InDisconnectedSession)

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

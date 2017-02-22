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


function New-RemoteSession
{
    param (
        [string] $Name,
        [string] $ConfigurationName,
        [System.Management.Automation.Remoting.PSSessionOption] $SessionOption)

    $parameters = @{ ComputerName = "."; }

    if ($Name) {
        $parameters["Name"] = $Name
    }

    if ($ConfigurationName) {
        $parameters["ConfigurationName"] = $ConfigurationName
    }

    if ($SessionOption) {
        $parameters["SessionOption"] = $SessionOption
    }

    if ($Script:AppVeyorRemoteCred)
    {
        Write-Verbose "Using Global AppVeyor Credential" -Verbose
        $parameters["Credential"] = $Script:AppVeyorRemoteCred
    }
    else
    {
        Write-Verbose "Using Implicit Credential" -Verbose
    }

    $session = New-PSSession @parameters

    return $session
}

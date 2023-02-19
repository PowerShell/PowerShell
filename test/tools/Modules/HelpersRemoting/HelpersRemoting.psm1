# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

##
## WinRM Remoting helper functions for writing remoting tests
##

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
        $null = Enable-PSRemoting -SkipNetworkProfileCheck
        $endPoint = Get-PSSessionConfiguration -Name $endPointName -ErrorAction SilentlyContinue

        if ($endPoint -eq $null)
        {
            Write-Warning "Unable to create the remoting configuration endpoint for this PowerShell version: $(${PSVersionTable}.PSVersion)"
            return $endPointName
        }
    }

    if ($endPoint.Permission -like "*NT AUTHORITY\NETWORK AccessDenied*")
    {
        $null = Enable-PSRemoting -SkipNetworkProfileCheck
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

##
## SSH Remoting helper functions for writing remoting tests
##

function Get-WindowsOpenSSHLink
{
    # From the Win OpenSSH Wiki page (https://github.com/PowerShell/Win32-OpenSSH/wiki/How-to-retrieve-links-to-latest-packages)
    $origSecurityProtocol = [Net.ServicePointManager]::SecurityProtocol
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    try
    {
        $url = 'https://github.com/PowerShell/Win32-OpenSSH/releases/latest/'
        $request = [System.Net.WebRequest]::Create($url)
        $request.AllowAutoRedirect = $false
        $response = & { $request.GetResponse() } 2>$null

        if ($null -ne $response)
        {
            $location = [string] $response.GetResponseHeader("Location")
            if (! [string]::IsNullOrEmpty($location))
            {
                return $location.Replace('tag', 'download') + '/OpenSSH-Win64.zip'
            }
        }
    }
    finally
    {
        [Net.ServicePointManager]::SecurityProtocol = $origSecurityProtocol
    }

    # Default to last known latest release
    Write-Warning "Unable to get latest OpenSSH release link. Using default release link."
    return 'https://github.com/PowerShell/Win32-OpenSSH/releases/download/v8.1.0.0p1-Beta/OpenSSH-Win64.zip'
}

function Install-WindowsOpenSSH
{
    param (
        [switch] $Force
    )

    $destPath = Join-Path -Path $env:ProgramFiles -ChildPath 'OpenSSH-Win64'
    if (Test-Path -Path $destPath)
    {
        if (! $Force)
        {
            Write-Verbose -Verbose "OpenSSH-Win64 already exists, skipping install step"
            return
        }

        Write-Verbose -Verbose "Force re-install OpenSSH-Win64 ..."
        Stop-Service -Name sshd -ErrorAction SilentlyContinue
        Remove-Item -Path $destPath -Recurse -Force
    }

    # Get link to latest OpenSSH release
    Write-Verbose -Verbose "Downloading latest OpenSSH-Win64 package link ..."
    $downLoadLink = Get-WindowsOpenSSHLink

    # Download and extract OpenSSH package
    Write-Verbose -Verbose "Downloading OpenSSH-Win64 zip package ..."
    $packageFilePath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath 'OpenSSH-Win64.zip'
    $oldProgressPreference = $ProgressPreference
    $ProgressPreference = 'SilentlyContinue'
    try
    {
        Invoke-WebRequest -Uri $downLoadLink -OutFile $packageFilePath
        Expand-Archive -Path $packageFilePath -DestinationPath $env:ProgramFiles
    }
    finally
    {
        $ProgressPreference = $oldProgressPreference
    }

    # Install and start SSHD service
    Push-Location $destPath
    try
    {
        Write-Verbose -Verbose "Running install-sshd.ps1 ..."
        .\install-sshd.ps1

        $netRule = Get-NetFirewallRule -Name sshd -ErrorAction SilentlyContinue
        if ($null -eq $netRule)
        {
            Write-Verbose -Verbose "Creating firewall rule for SSHD ..."
            New-NetFirewallRule -Name sshd -DisplayName "OpenSSH Server (sshd)" -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22
        }

        Write-Verbose -Verbose "Starting SSHD service ..."
        Restart-Service -Name sshd
    }
    finally
    {
        Pop-Location
    }

    # Current release of Windows OpenSSH configures SSHD to change AuthorizedKeyFiles for administrators
    # Comment it out so that normal key based authentication works per user as with Linux platforms.
    # Match Group administrators
    #       AuthorizedKeysFile __PROGRAMDATA__/ssh/administrators_authorized_keys
    $sshdFilePath = "$env:ProgramData\ssh\sshd_config"
    $sshdContent = Get-Content $sshdFilePath
    $sshdNewContent = [string[]] @()
    $modified = $false
    foreach ($item in $sshdContent)
    {
        if ($item.TrimStart().StartsWith('Match Group administrators') -or
            $item.TrimStart().StartsWith('AuthorizedKeysFile __PROGRAMDATA'))
        {
            if (!$modified) { $modified = $true }
            $sshdNewContent += "#" + $item
        }
        else
        {
            $sshdNewContent += $item
        }
    }
    if ($modified)
    {
        $sshdNewContent | Set-Content -Path $sshdFilePath -Force
    }
}

function Install-SSHRemotingOnWindows
{
    param (
        [Parameter(Mandatory=$true)]
        [string] $PowerShellPath
    )

    # Install sshd service
    if ($null -eq (Get-Command -Name sshd -ErrorAction SilentlyContinue))
    {
        Write-Verbose -Verbose "Installing SSHD service ..."
        Install-WindowsOpenSSH -Force
    }

    if (! (Test-Path -Path "$env:ProgramData\ssh\sshd_config"))
    {
        throw "Unable to install SSH service.  Config file $env:ProgramData\ssh\sshd_config does not exist."
    }

    # Configure SSH to authenticate with keys for this user.
    # PubkeyAuthentication should be enabled by default.
    # RSA keys should be enabled by default.

    # Create user .ssh directory.
    if (! (Test-Path -Path "$HOME\.ssh"))
    {
        Write-Verbose -Verbose "Creating $HOME\.ssh directory ..."
        New-Item -Path "$HOME\.ssh" -ItemType Directory -Force
    }

    # Create new rsa keys for current user.
    if ( !(Test-Path "$HOME\.ssh\id_rsa"))
    {
        Write-Verbose -Verbose "Creating rsa keys ..."
        cmd /c "ssh-keygen -t rsa -f $HOME\.ssh\id_rsa -q -N `"`""
    }
    if (! (Test-Path "$HOME\.ssh\id_rsa"))
    {
        throw "id_rsa private key file was not created."
    }
    if (! (Test-Path "$HOME\.ssh\id_rsa.pub"))
    {
        throw "id_rsa.pub public key file was not created."
    }

    # Create authorized keys file.
    Write-Verbose -Verbose "Creating authorized_keys ..."
    Get-Content -Path "$HOME\.ssh\id_rsa.pub" | Set-Content -Path "$HOME\.ssh\authorized_keys" -Force

    # Create known_hosts file for 'localhost' connection.
    Write-Verbose -Verbose "Creating known_hosts ..."
    ssh-keyscan -H localhost | Set-Content -Path "$HOME\.ssh\known_hosts" -Force

    # Install Microsoft.PowerShell.RemotingTools module.
    if ($null -eq (Get-Module -Name Microsoft.PowerShell.RemotingTools -ListAvailable))
    {
        Write-Verbose -Verbose "Installing Microsoft.PowerShell.RemotingTools ..."
        Install-Module -Name Microsoft.PowerShell.RemotingTools -Force -SkipPublisherCheck
    }

    # Add PowerShell endpoint to SSHD.
    Write-Verbose -Verbose "Running Enable-SSHRemoting ..."
    Enable-SSHRemoting -SSHDConfigFilePath "$env:ProgramData\ssh\sshd_config" -PowerShellFilePath $PowerShellPath -Force

    Write-Verbose -Verbose "Restarting sshd service ..."
    Restart-Service -Name sshd

    # Test SSH remoting.
    Write-Verbose -Verbose "Testing SSH remote connection ..."
    $session = New-PSSession -HostName localhost
    try
    {
        if ($null -eq $session)
        {
            throw "Could not successfully create SSH remoting connection."
        }
    }
    finally
    {
        Remove-PSSession $session
    }
}

function WriteVerboseSSHDStatus
{
    param (
        [string] $Msg = 'SSHD service status'
    )

    $sshdStatus = sudo service ssh status
    Write-Verbose -Verbose "${Msg}: $sshdStatus"
}

function DumpTextFile
{
    param (
        [string] $FilePath = '/etc/ssh/sshd_config'
    )

    $content = Get-Content -Path $FilePath -Raw
    Write-Verbose -Verbose $content
}

function Install-SSHRemotingOnLinux
{
    param (
        [Parameter(Mandatory=$true)]
        [string] $PowerShellPath
    )

    # Install ssh daemon.
    if (! (Test-Path -Path /etc/ssh/sshd_config))
    {
        Write-Verbose -Verbose "Installing openssh-server ..."
        sudo apt-get install --yes openssh-server

        Write-Verbose -Verbose "Restarting sshd service after install ..."
        WriteVerboseSSHDStatus "SSHD service status before restart"
        sudo service ssh restart
        WriteVerboseSSHDStatus "SSHD service status after restart"
    }
    if (! (Test-Path -Path /etc/ssh/sshd_config))
    {
        throw "Unable to install SSH daemon.  Config file /etc/ssh/sshd_config does not exist."
    }

    # Configure SSH to authenticate with keys for this user.
    # PubkeyAuthentication should be enabled by default.
    # RSA keys should be enabled by default.

    # Create user .ssh directory.
    if (! (Test-Path -Path "$HOME/.ssh"))
    {
        Write-Verbose -Verbose "Creating $HOME/.ssh directory ..."
        New-Item -Path "$HOME/.ssh" -ItemType Directory -Force
    }

    # Create new rsa keys for current user.
    if ( !(Test-Path "$HOME/.ssh/id_rsa"))
    {
        Write-Verbose -Verbose "Creating rsa keys ..."
        bash -c "ssh-keygen -t rsa -f $HOME/.ssh/id_rsa -q -N ''"
    }
    if (! (Test-Path "$HOME/.ssh/id_rsa"))
    {
        throw "id_rsa private key file was not created."
    }
    if (! (Test-Path "$HOME/.ssh/id_rsa.pub"))
    {
        throw "id_rsa.pub public key file was not created."
    }

    # Create authorized keys file.
    Write-Verbose -Verbose "Creating authorized_keys ..."
    Get-Content -Path "$HOME/.ssh/id_rsa.pub" | Set-Content -Path "$HOME/.ssh/authorized_keys" -Force

    # Create known_hosts file for 'localhost' connection.
    Write-Verbose -Verbose "Updating known_hosts ..."
    ssh-keyscan -H localhost | Set-Content -Path "$HOME/.ssh/known_hosts" -Force

    <#
    # Install Microsoft.PowerShell.RemotingTools module.
    if ($null -eq (Get-Module -Name Microsoft.PowerShell.RemotingTools -ListAvailable))
    {
        Write-Verbose -Verbose "Installing Microsoft.PowerShell.RemotingTools ..."
        Install-Module -Name Microsoft.PowerShell.RemotingTools -Force -SkipPublisherCheck
    }
    #>

    # Add PowerShell endpoint to SSHD.
    Write-Verbose -Verbose "Running Enable-SSHRemoting ..."
    Write-Verbose -Verbose "PSScriptRoot: $PSScriptRoot"
    $modulePath = "${PSScriptRoot}\..\Microsoft.PowerShell.RemotingTools\Microsoft.PowerShell.RemotingTools.psd1"
    $sshdFilePath = '/etc/ssh/sshd_config'

    # First create a default 'powershell' named endpoint.
    $cmdLine = "Import-Module ${modulePath}; Enable-SSHRemoting -SSHDConfigFilePath $sshdFilePath -PowerShellFilePath $PowerShellPath -Force"
    Write-Verbose -Verbose "CmdLine: $cmdLine"
    sudo pwsh -c $cmdLine

    # Next create a 'pwshconfig' named configured endpoint.
    # Configuration file:
    $configFilePath = Join-Path -Path "$env:HOME" -ChildPath 'PSTestConfig.pssc'
    '@{
        GUID = "4d667b90-25f8-47d5-9c90-619b27954748"
        Author = "Microsoft"
        Description = "Test local PowerShell session configuration"
        LanguageMode = "ConstrainedLanguage"
    }' | Out-File -FilePath $configFilePath
    $cmdLine = "Import-Module ${modulePath}; Enable-SSHRemoting -SSHDConfigFilePath $sshdFilePath -PowerShellFilePath $PowerShellPath -ConfigFilePath $configFilePath -SubsystemName 'pwshconfig' -Force"
    Write-Verbose -Verbose "CmdLine: $cmdLine"
    sudo pwsh -c $cmdLine

    # Finally create a 'pwshbroken' named configured endpoint.
    $cmdLine = "Import-Module ${modulePath}; Enable-SSHRemoting -SSHDConfigFilePath $sshdFilePath -PowerShellFilePath $PowerShellPath -ConfigFilePath '$HOME/NoSuch.pssc' -SubsystemName 'pwshbroken' -Force"
    Write-Verbose -Verbose "CmdLine: $cmdLine"
    sudo pwsh -c $cmdLine

    # Restart SSHD service for changes to take effect.
    Start-Sleep -Seconds 1
    WriteVerboseSSHDStatus "SSHD service status before restart"
    Write-Verbose -Verbose "Restarting sshd ..."
    sudo service ssh restart
    WriteVerboseSSHDStatus "SSHD service status after restart"

    # Try starting again if needed.
    $status = sudo service ssh status
    $result = $status | Where-Object { ($_ -like '*not running*') -or ($_ -like '*stopped*') }
    if ($null -ne $result)
    {
        Start-Sleep -Seconds 1
        Write-Verbose -Verbose "Starting sshd again ..."
        sudo service ssh start
        WriteVerboseSSHDStatus "SSHD service status after second start attempt"
    }

    # Test SSH remoting.
    Write-Verbose -Verbose "Testing SSH remote connection ..."
    $session = New-PSSession -HostName localhost
    try
    {
        if ($null -eq $session)
        {
            throw "Could not successfully create SSH remoting connection."
        }
        else
        {
            Write-Verbose -Verbose "SUCCESS: SSH remote connection"
        }
    }
    finally
    {
        Remove-PSSession $session
    }
}

<#
.Synopsis
    Installs and configures SSH components, and creates an SSH PowerShell remoting endpoint.
.Description
    This cmdlet assumes SSH client is installed on the machine, but will check for SSHD service and
    install it if needed.
    Next, it will configure SSHD for key based user authentication, for the current user context.
    Then it configures SSHD for a PowerShell endpoint based on the provided PowerShell file path.
    If no PowerShell file path is provided, the current PowerShell instance ($PSHOME) is used.
    Finally, it will test the new SSH remoting endpoint connection to ensure it works.
    Currently, only Ubuntu and Windows platforms are supported.
.Parameter PowerShellPath
    Specifies a PowerShell, pwsh(.exe), executable path that will be used for the remoting endpoint.
#>
function Install-SSHRemoting
{
    param (
        [string] $PowerShellFilePath
    )

    Write-Verbose -Verbose "Install-SSHRemoting called with PowerShell file path: $PowerShellFilePath"

    if ($IsWindows)
    {
        if ([string]::IsNullOrEmpty($PowerShellFilePath)) { $PowerShellFilePath = "$PSHOME/pwsh.exe" }
        Install-SSHRemotingOnWindows -PowerShellPath $PowerShellFilePath
        return
    }
    elseif ($IsLinux)
    {
        $LinuxInfo = Get-Content /etc/os-release -Raw | ConvertFrom-StringData
        if ($LinuxInfo.ID -match 'ubuntu')
        {
            if ([string]::IsNullOrEmpty($PowerShellFilePath)) { $PowerShellFilePath = "$PSHOME/pwsh" }
            Install-SSHRemotingOnLinux -PowerShellPath $PowerShellFilePath
            return
        }
    }

    Write-Error "Platform not supported.  Only Windows and Ubuntu platforms are currently supported."
}

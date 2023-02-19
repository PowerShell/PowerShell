# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

##
## Enable-SSHRemoting Cmdlet
##

class PlatformInfo
{
    [bool] $isCoreCLR
    [bool] $isLinux
    [bool] $isOSX
    [bool] $isWindows

    [bool] $isAdmin

    [bool] $isUbuntu
    [bool] $isUbuntu14
    [bool] $isUbuntu16
    [bool] $isCentOS
    [bool] $isFedora
    [bool] $isOpenSUSE
    [bool] $isOpenSUSE13
    [bool] $isOpenSUSE42_1
    [bool] $isRedHatFamily
}

function DetectPlatform
{
    param (
        [ValidateNotNull()]
        [PlatformInfo] $PlatformInfo
    )

    try
    {
        $Runtime = [System.Runtime.InteropServices.RuntimeInformation]
        $OSPlatform = [System.Runtime.InteropServices.OSPlatform]

        $platformInfo.isCoreCLR = $true
        $platformInfo.isLinux = $Runtime::IsOSPlatform($OSPlatform::Linux)
        $platformInfo.isOSX = $Runtime::IsOSPlatform($OSPlatform::OSX)
        $platformInfo.isWindows = $Runtime::IsOSPlatform($OSPlatform::Windows)
    }
    catch
    {
        $platformInfo.isCoreCLR = $false
        $platformInfo.isLinux = $false
        $platformInfo.isOSX = $false
        $platformInfo.isWindows = $true
    }

    if ($platformInfo.isWindows)
    {
        $platformInfo.isAdmin = ([System.Security.Principal.WindowsPrincipal]::new([System.Security.Principal.WindowsIdentity]::GetCurrent())).IsInRole( `
            [System.Security.Principal.WindowsBuiltInRole]::Administrator)
    }

    if ($platformInfo.isLinux)
    {
        $LinuxInfo = Get-Content /etc/os-release -Raw | ConvertFrom-StringData

        $platformInfo.isUbuntu = $LinuxInfo.ID -match 'ubuntu'
        $platformInfo.isUbuntu14 = $platformInfo.isUbuntu -and ($LinuxInfo.VERSION_ID -match '14.04')
        $platformInfo.isUbuntu16 = $platformInfo.isUbuntu -and ($LinuxInfo.VERSION_ID -match '16.04')
        $platformInfo.isCentOS = ($LinuxInfo.ID -match 'centos') -and ($LinuxInfo.VERSION_ID -match '7')
        $platformInfo.isFedora = ($LinuxInfo.ID -match 'fedora') -and ($LinuxInfo.VERSION_ID -ge '24')
        $platformInfo.isOpenSUSE = $LinuxInfo.ID -match 'opensuse'
        $platformInfo.isOpenSUSE13 = $platformInfo.isOpenSUSE -and ($LinuxInfo.VERSION_ID -match '13')
        $platformInfo.isOpenSUSE42_1 = $platformInfo.isOpenSUSE -and ($LinuxInfo.VERSION_ID -match '42.1')
        $platformInfo.isRedHatFamily = $platformInfo.isCentOS -or $platformInfo.isFedora -or $platformInfo.isOpenSUSE
    }
}

class SSHSubSystemEntry
{
    [string] $subSystemLine
    [string] $subSystemName
    [string] $subSystemCommand
    [string[]] $subSystemCommandArgs
}

class SSHRemotingConfig
{
    [PlatformInfo] $platformInfo
    [SSHSubSystemEntry[]] $psSubSystemEntries = @()
    [string] $configFilePath
    [string] $subsystemName
    $configComponents = @()

    SSHRemotingConfig(
        [PlatformInfo] $platInfo,
        [string] $configFilePath,
        [string] $subsystemName)
    {
        $this.platformInfo = $platInfo
        $this.configFilePath = $configFilePath
        $this.subsystemName = $subsystemName
        $this.ParseSSHRemotingConfig()
    }

    [string[]] SplitConfigLine([string] $line)
    {
        $line = $line.Trim()
        $lineLength = $line.Length
        $rtnStrArray = [System.Collections.Generic.List[string]]::new()

        for ($i=0; $i -lt $lineLength; )
        {
            $startIndex = $i
            while (($i -lt $lineLength) -and ($line[$i] -ne " ") -and ($line[$i] -ne "`t")) { $i++ }
            $rtnStrArray.Add($line.Substring($startIndex, ($i - $startIndex)))
            while (($i -lt $lineLength) -and ($line[$i] -eq " ") -or ($line[$i] -eq "`t")) { $i++ }
        }

        return $rtnStrArray.ToArray()
    }

    ParseSSHRemotingConfig()
    {
        [string[]] $contents = Get-Content -Path $this.configFilePath
        foreach ($line in $contents)
        {
            $components = $this.SplitConfigLine($line)
            $this.configComponents += @{ Line = $line; Components = $components }

            if (($components[0] -eq "Subsystem") -and ($components[1] -eq $this.subsystemName))
            {
                $entry = [SSHSubSystemEntry]::New()
                $entry.subSystemLine = $line
                $entry.subSystemName = $components[1]
                $entry.subSystemCommand = $components[2]
                $entry.subSystemCommandArgs = @()
                for ($i=3; $i -lt $components.Count; $i++)
                {
                    $entry.subSystemCommandArgs += $components[$i]
                }

                $this.psSubSystemEntries += $entry
            }
        }
    }
}

function UpdateConfiguration
{
    param (
        [SSHRemotingConfig] $config,
        [string] $PowerShellPath,
        [string] $SubsystemName,
        [string] $ConfigFilePath
    )

    #
    # Update and re-write config file with existing settings plus new PowerShell remoting settings
    #

    # Subsystem
    [System.Collections.Generic.List[string]] $newContents = [System.Collections.Generic.List[string]]::new()
    $psSubSystemEntry = "Subsystem       {0}      {1} -SSHS -NoProfile -NoLogo" -f $SubsystemName, $powerShellPath
    if (![string]::IsNullOrEmpty($ConfigFilePath))
    {
        $psSubSystemEntry += " -ConfigurationFile {0}" -f $ConfigFilePath
    }

    $subSystemAdded = $false
    foreach ($lineItem in $config.configComponents)
    {
        $line = $lineItem.Line
        $components = $lineItem.Components

        if ($components[0] -eq "Subsystem")
        {
            if (! $subSystemAdded)
            {
                # Add new powershell subsystem entry
                $newContents.Add($psSubSystemEntry)
                $subSystemAdded = $true
            }

            if ($components[1] -eq $SubsystemName)
            {
                # Remove all existing powershell subsystem entries
                continue
            }

            # Include existing subsystem entries.
            $newContents.Add($line)
        }
        else
        {
            # Include all other configuration lines
            $newContents.Add($line)
        }
    }

    if (! $subSystemAdded)
    {
        $newContents.Add($psSubSystemEntry)
    }

    # Copy existing file to a backup version
    $uniqueName = [System.IO.Path]::GetFileNameWithoutExtension([System.IO.Path]::GetRandomFileName())
    $backupFilePath = $config.configFilePath + "_backup_" + $uniqueName
    Copy-Item -Path $config.configFilePath -Destination $backupFilePath
    if ($?)
    {
        WriteLine "A backup copy of the old sshd_config configuration file has been created at:"
        WriteLine $backupFilePath
    }

    Set-Content -Path $config.configFilePath -Value $newContents.ToArray() -ErrorAction Stop
}

function CheckPowerShellVersion
{
    param (
        [string] $FilePath
    )

    if (! (Test-Path $FilePath))
    {
        throw "CheckPowerShellVersion failed with invalid path: $FilePath"
    }

    $commandToExec = "& '$FilePath' -noprofile -noninteractive -c '`$PSVersionTable.PSVersion.Major'"
    $sb = [scriptblock]::Create($commandToExec)

    try
    {
        $psVersionMajor = [int] (& $sb) 2>$null
        Write-Verbose ""
        Write-Verbose "CheckPowerShellVersion: $psVersionMajor for FilePath: $FilePath"
    }
    catch
    {
        $psVersionMajor = 0
    }

    if ($psVersionMajor -ge 6)
    {
        return $true
    }
    else
    {
        return $false
    }
}

function WriteLine
{
    param (
        [string] $Message,
        [int] $PrependLines = 0,
        [int] $AppendLines = 0
    )

    for ($i=0; $i -lt $PrependLines; $i++)
    {
        Write-Output ""
    }

    Write-Output $Message

    for ($i=0; $i -lt $AppendLines; $i++)
    {
        Write-Output ""
    }
}

# Windows only GetShortPathName PInvoke
$typeDef = @'
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    namespace NativeUtils
    {
        public class Path
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            private static extern int GetShortPathName(
                [MarshalAs(UnmanagedType.LPTStr)]
                string path,
                [MarshalAs(UnmanagedType.LPTStr)]
                StringBuilder shortPath,
                int shortPathLength);

            public static string ConvertToShortPath(
                string longPath)
            {
                int shortPathLength = 2048;
                StringBuilder shortPath = new StringBuilder(shortPathLength);
                GetShortPathName(
                    path: longPath,
                    shortPath: shortPath,
                    shortPathLength: shortPathLength);

                return shortPath.ToString();
            }
        }
    }
'@

<#
.Synopsis
    Enables PowerShell SSH remoting endpoint on local system
.Description
    This cmdlet will set up an SSH based remoting endpoint on the local system, based on
    the PowerShell executable file path passed in. Or if no PowerShell file path is provided then
    the currently running PowerShell file path is used.
    The end point is enabled by adding a 'powershell' subsystem entry to the SSHD configuration, using
    the provided or current PowerShell file path.
    Both the SSH client and SSHD server components are detected and if not found a terminating
    error is emitted, asking the user to install the components.
    Then the sshd_config is parsed, and if a new 'powershell' subsystem entry is added.
.Parameter SSHDConfigFilePath
    File path to the SSHD service configuration file. This file will be updated to include a
    'powershell' subsystem entry to define a PowerShell SSH remoting endpoint, so current credentials
    must have write access to the file.
.Parameter PowerShellFilePath
    Specifies the file path to the PowerShell command used to host the SSH remoting PowerShell
    endpoint. If no value is specified then the currently running PowerShell executable path is used
    in the subsystem command.
.Parameter Force
    When true, this cmdlet will update the sshd_config configuration file without prompting.
#>
function Enable-SSHRemoting
{
    [CmdletBinding()]
    param (
        [string] $SSHDConfigFilePath,

        [string] $PowerShellFilePath,

        [string] $SubsystemName = "powershell",

        [string] $ConfigFilePath,

        [switch] $Force
    )

    # Detect platform
    $platformInfo = [PlatformInfo]::new()
    DetectPlatform $platformInfo
    Write-Verbose "Platform information"
    Write-Verbose "$($platformInfo | Out-String)"

    # Non-Windows platforms must run this cmdlet as 'root'
    if (!$platformInfo.isWindows)
    {
        $user = whoami
        if ($user -ne 'root')
        {
            if (! $PSCmdlet.ShouldContinue("This cmdlet must be run as 'root'. If you continue, PowerShell will restart under 'root'. Do you wish to continue?", "Enable-SSHRemoting"))
            {
                return
            }

            # Spawn new PowerShell with sudo and exit this session.
            $modFilePath = (Get-Module -Name Microsoft.PowerShell.RemotingTools | Select-Object -Property Path).Path
            $modName = [System.IO.Path]::GetFileNameWithoutExtension($modFilePath)
            $modFilePath = Join-Path -Path (Split-Path -Path $modFilePath -Parent) -ChildPath "${modName}.psd1"

            $parameters = ""
            foreach ($key in $PSBoundParameters.Keys)
            {
                $parameters += "-${key} "
                $value = $PSBoundParameters[$key]
                if ($value -is [string])
                {
                    $parameters += "'$value' "
                }
            }

            & sudo "$PSHOME/pwsh" -NoExit -c "Import-Module -Name $modFilePath; Enable-SSHRemoting $parameters"
            exit
        }
    }

    # Detect SSH client installation
    if (! (Get-Command -Name ssh -ErrorAction SilentlyContinue))
    {
        Write-Warning "SSH client is not installed or not discoverable on this machine. SSH client must be installed before PowerShell SSH based remoting can be enabled."
    }

    # Detect SSHD server installation
    $SSHDFound = $false
    if ($platformInfo.IsWindows)
    {
        $SSHDFound = $null -ne (Get-Service -Name sshd -ErrorAction SilentlyContinue)
    }
    elseif ($platformInfo.IsLinux)
    {
        $sshdStatus = sudo service ssh status
        $SSHDFound = $null -ne $sshdStatus
    }
    else
    {
        # macOS
        $SSHDFound = ($null -ne (launchctl list | Select-String 'com.openssh.sshd'))
    }
    if (! $SSHDFound)
    {
        Write-Warning "SSHD service is not found on this machine. SSHD service must be installed and running before PowerShell SSH based remoting can be enabled."
    }

    # Validate a SSHD configuration file path
    if ([string]::IsNullOrEmpty($SSHDConfigFilePath))
    {
        Write-Warning "-SSHDConfigFilePath not provided. Using default configuration file location."

        if ($platformInfo.IsWindows)
        {
            $SSHDConfigFilePath = Join-Path -Path $env:ProgramData -ChildPath 'ssh' -AdditionalChildPath 'sshd_config'
        }
        elseif ($platformInfo.isLinux)
        {
            $SSHDConfigFilePath = '/etc/ssh/sshd_config'
        }
        else
        {
            # macOS
            $SSHDConfigFilePath = '/private/etc/ssh/sshd_config'
        }
    }

    # Validate a PowerShell command to use for endpoint
    $PowerShellToUse = $PowerShellFilePath
    if (! [string]::IsNullOrEmpty($PowerShellToUse))
    {
        WriteLine "Validating provided -PowerShellFilePath argument." -AppendLines 1 -PrependLines 1

        if (! (Test-Path $PowerShellToUse))
        {
            throw "The provided PowerShell file path is invalid: $PowerShellToUse"
        }

        if (! (CheckPowerShellVersion $PowerShellToUse))
        {
            throw "The provided PowerShell file path is an unsupported version of PowerShell.  PowerShell version 6.0 or greater is required."
        }
    }
    else
    {
        WriteLine "Validating current PowerShell to use as endpoint subsystem." -AppendLines 1

        # Try currently running PowerShell
        $PowerShellToUse = Get-Command -Name "$PSHome/pwsh" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
        if (! $PowerShellToUse -or ! (CheckPowerShellVersion $PowerShellToUse))
        {
            throw "Current running PowerShell version is not valid for SSH remoting endpoint. SSH remoting is only supported for PowerShell version 6.0 and higher. Specify a valid PowerShell 6.0+ file path with the -PowerShellFilePath parameter."
        }
    }

    # SSHD configuration file uses the space character as a delimiter.
    # Consequently, the configuration Subsystem entry will not allow argument paths containing space characters.
    # For Windows platforms, we can a short cut path.
    # But for non-Windows platforms, we currently throw an error.
    #   One possible solution is to crete a symbolic link
    #   New-Item -ItemType SymbolicLink -Path <NewNoSpacesPath> -Value $<PathwithSpaces>
    if ($PowerShellToUse.Contains(' '))
    {
        if ($platformInfo.IsWindows)
        {
            Add-Type -TypeDefinition $typeDef
            $PowerShellToUse = [NativeUtils.Path]::ConvertToShortPath($PowerShellToUse)
            if (! (Test-Path -Path $PowerShellToUse))
            {
                throw "Converting long Windows file path resulted in an invalid path: ${PowerShellToUse}."
            }
        }
        else
        {
            throw "The PowerShell executable (pwsh) selected for hosting the remoting endpoint has a file path containing space characters, which cannot be used with SSHD configuration."
        }
    }

    WriteLine "Using PowerShell at this path for SSH remoting endpoint:"
    WriteLine "$PowerShellToUse" -AppendLines 1

    # Validate the SSHD configuration file path
    if (! (Test-Path -Path $SSHDConfigFilePath))
    {
        throw "The provided SSHDConfigFilePath parameter, $SSHDConfigFilePath, is not a valid path."
    }
    WriteLine "Modifying SSHD configuration file at this location:"
    WriteLine "$SSHDConfigFilePath" -AppendLines 1

    # Get the SSHD configuration
    $sshdConfig = [SSHRemotingConfig]::new($platformInfo, $SSHDConfigFilePath, $SubsystemName)

    if ($sshdConfig.psSubSystemEntries.Count -gt 0)
    {
        WriteLine "The following PowerShell subsystems were found in the sshd_config file:"
        foreach ($entry in $sshdConfig.psSubSystemEntries)
        {
            WriteLine $entry.subSystemLine
        }
        Writeline "Continuing will overwrite any existing PowerShell subsystem entries with the new subsystem." -PrependLines 1
        WriteLine "The new SSH remoting endpoint will use this PowerShell executable path:"
        WriteLine "$PowerShellToUse" -AppendLines 1
    }

    $shouldContinue = $Force
    if (! $shouldContinue)
    {
        $shouldContinue = $PSCmdlet.ShouldContinue("The SSHD service configuration file (sshd_config) will now be updated to enable PowerShell remoting over SSH. Do you wish to continue?", "Enable-SSHRemoting")
    }

    if ($shouldContinue)
    {
        WriteLine "Updating configuration file ..." -PrependLines 1 -AppendLines 1

        UpdateConfiguration $sshdConfig $PowerShellToUse $SubsystemName $ConfigFilePath

        WriteLine "The configuration file has been updated:" -PrependLines 1
        WriteLine $sshdConfig.configFilePath -AppendLines 1
        WriteLine "You must restart the SSHD service for the changes to take effect." -AppendLines 1
    }
}

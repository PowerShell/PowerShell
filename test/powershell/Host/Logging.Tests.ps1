using namespace System.Text

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module PSSysLog

<#
    Define enums that mirror the internal enums used
    in product code. These are used to configure
    syslog logging.
#>
enum LogLevel
{
    LogAlways = 0x0
    Critical = 0x1
    Error = 0x2
    Warning = 0x3
    Informational = 0x4
    Verbose = 0x5
    Debug = 0x14
}

enum LogChannel
{
    Operational = 0x10
    Analytic = 0x11
}

enum LogKeyword
{
    Runspace = 0x1
    Pipeline = 0x2
    Protocol = 0x4
    Transport = 0x8
    Host = 0x10
    Cmdlets = 0x20
    Serializer = 0x40
    Session = 0x80
    ManagedPlugin = 0x100
}

function BuildCommaString
{
    param
    (
        [object[]] $values
    )
    if ($values -eq $null -or $values.Length -eq 0)
    {
        return $null
    }
    [StringBuilder] $sb = [StringBuilder]::new()
    foreach ($value in $values)
    {
        if ($sb.Length -gt 0)
        {
            $null = $sb.Append(', ')
        }
        $null = $sb.Append($value.ToString())
    }
    return $sb.ToString()
}

<#
.SYNOPSIS
   Creates a powershell.config.json file with syslog settings

.PARAMETER logId
    The identifier to use for logging

.PARAMETER logLevel
    The optional logging level, see the LogLevel enum

.PARAMETER logChannels
    The optional logging channels to enable; see the LogChannel enum

.PARAMETER logKeywords
    The optional keywords to enable ; see the LogKeyword enum
#>
function WriteLogSettings
{
    param
    (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $LogId,

        [System.Nullable[LogLevel]] $LogLevel = $null,

        [LogChannel[]] $LogChannels = $null,

        [LogKeyword[]] $LogKeywords = $null
    )

    [StringBuilder] $sb = [StringBuilder]::new()
    $filename = [Guid]::NewGuid().ToString('N')
    $fullPath = Join-Path -Path $TestDrive -ChildPath "$filename.config.json"

    $null = $sb.AppendLine('{')
    $null = $sb.AppendFormat('"LogIdentity": "{0}"', $LogId)

    [string] $channels = BuildCommaString($LogChannels)
    [string] $keywords = BuildCommaString($LogKeywords)

    if ($null -ne $LogLevel)
    {
        $null = $sb.AppendLine(',')
        $null = $sb.AppendFormat('"LogLevel": "{0}"', $LogLevel.ToString())
    }
    if ([string]::IsNullOrEmpty($channels) -eq $false)
    {
        $null = $sb.AppendLine(',')
        $null = $sb.AppendFormat('"LogChannels": "{0}"', $channels)
    }
    if ([string]::IsNullOrEmpty($keywords) -eq $false)
    {
        $null = $sb.AppendLine(',')
        $null = $sb.AppendFormat('"LogKeywords": "{0}"', $keywords)
    }

    $null = $sb.AppendLine()
    $null = $sb.AppendLine('}')

    $sb.ToString() | Set-Content -Path $fullPath -ErrorAction Stop
    return $fullPath
}

class TestSettings
{
    [bool] $IsSupportedEnvironment
    [string] $SysLogFile = [string]::Empty

    TestSettings([bool] $linux, [string]$pwshHome)
    {
        $this.IsSupportedEnvironment = $linux

        if ($this.IsSupportedEnvironment)
        {
            if (Test-Path -Path '/var/log/syslog')
            {
                $this.SysLogFile = '/var/log/syslog'
            }
            elseif (Test-Path -Path '/var/log/messages')
            {
                $this.SysLogFile = '/var/log/messages'
            }
            else
            {
                # TODO: Look into journalctl and other variations.
                Write-Warning -Message 'Unsupported Linux syslog configuration.'
                $this.IsSupportedEnvironment = $false
            }
        }
    }
}

Describe 'Basic SysLog tests on Linux' -Tag 'CI' {

    $Settings = [TestSettings]::new($IsLinux -eq $true, $PSHome)
    $PowerShell = Join-Path -Path $PSHome -ChildPath "pwsh"

    It 'Verifies basic logging with no customizations' -Skip:(!$Settings.IsSupportedEnvironment) {
        $logId = 'DefaultSettings'
        $now = [DateTime]::Now

        $configFile = WriteLogSettings -LogId $logId
        & $Powershell -NoProfile -SettingsFile $configFile -Command '$env:PSModulePath | out-null'

        $items = [System.Collections.ArrayList]::new()
        # Get log entries from the last 100 that match our id and are after the time we launched Powershell
        Get-PSSysLog -Path $Settings.SyslogFile -Id $logId -After $now -Tail 100 -Results $items

        $items.Count | Should BeGreaterThan 2
        $items[0].EventId | Should Be 'GitCommitId'
        $items[1].EventId | Should Be 'Perftrack_ConsoleStartupStart:PowershellConsoleStartup.WinStart.Informational'
        $items[2].EventId | Should Be 'Perftrack_ConsoleStartupStop:PowershellConsoleStartup.WinStop.Informational'
    }

    It 'Verifies logging level filtering works' -Skip:(!$Settings.IsSupportedEnvironment) {
        $logId = 'WarningLevel'
        $now = [DateTime]::Now

        $configFile = WriteLogSettings -LogId $logId -LogLevel Warning
        & $Powershell -NoProfile -SettingsFile $configFile -Command '$env:PSModulePath | out-null'

        $items = [System.Collections.ArrayList]::new()
        # by default, only informational events are logged. With Level = Warning, the log should be empty.
        Get-PSSysLog -Path $Settings.SyslogFile -Id $logId -After $now -Tail 100 -Results $items
        $items.Count | Should Be 0
    }
}

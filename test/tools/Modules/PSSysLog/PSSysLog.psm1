# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Set-StrictMode -Version Latest

<#
os_log notes:

There are no public APIs on MacOS for consuming os_log data or
collecting log output in real-time. To get log data in a programmatically consumable
format, the data must first be extracted then converted to a raw text format.

Extraction requires the following steps:

1: Snapshot the current time

2: Enable persistence of PowerShell log entries
   See Set-OsLogPersistence

3: Run powershell to generate the expected log output

4: Run 'log collect' to retrieve log records after a specific timestamp
from the system logs.
Note that the extracted data is still not directly consumable.

5: Run 'log show' to convert the extracted data to text and redirect it
to a file.
The --predicate can be used at this point to filter extracted records.
The typical filter is 'process == "pwsh"'

The redirected text file can then be consumed by this module.

Example command-lines:

sudo log collect --start "2018-02-07 14:33:30" --output ./system.logarchive
log show ./system.logarchive/ --info --predicate 'process == "pwsh"' >pwsh.log.txt

Parsing Notes:
* Sample contains 6.0.1 content (which is out of date) revise with 6.1.0 preview
* Ensure analytic data is considered when parsing; specifically Provider_Lifecycle:ProviderStart.Method.Informational
* Multi-line output is expected. Parsing needs to detect the timestamp at the begining
of a line and append subsequent lines to the message until the next 'log' line is found.
* Header lines need to be skipped.

Sample output from 'log show' illustrating one single-line entry and one multi-line entry.
Analytic log items are often multi-line because the message text contains newline characters.

==========
/Users/psbuildacct/system.logarchive
==========
Timestamp                       Thread     Type        Activity             PID
2018-02-07 14:34:35.256501-0800 0x2a3730   Default     0x0                  39437  pwsh: (libpsl-native.dylib) [com.microsoft.powershell.powershell] (v6.0.1:1:10) [Perftrack_ConsoleStartupStart:PowershellConsoleStartup.WinStart.Informational] PowerShell console is starting up
2018-02-07 14:34:35.562003-0800 0x2a373a   Default     0x0                  39437  pwsh: (libpsl-native.dylib) [com.microsoft.powershell.powershell] (v6.0.1:4:11) [Provider_Lifecycle:ProviderStart.Method.Informational] Provider Alias changed state to Started.
Context:
        Severity = Informational
        Host Name = ConsoleHost
        Host Version = 6.0.1
        Host ID = 964756e8-c074-4228-a54d-d410a88e8c66
        Host Application = /usr/local/microsoft/powershell/6.0.1/pwsh.dll
        Engine Version =
        Runspace ID =
        Pipeline ID =
        Command Name =
        Command Type =
        Script Name =
        Command Path =
        Sequence Number = 1
        User = PowerShells-MacBook\psbuildacct
        Connected User =
        Shell ID = Microsoft.PowerShell
        User Data:
#>

#region Utilities

function Test-Sudo
{
    if(-not (Test-Path -Path "env:SUDO_USER"))
    {
        throw "This command must be run from sudo"
    }
}

function Test-MacOS
{
    if (-not $IsMacOS)
    {
        throw "This command requires MacOS"
    }
}

function Test-Linux
{
    if (-not $IsLinux)
    {
        throw "This command requires Linux"
    }
}

function Start-NativeExecution
{
    param
    (
        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [ScriptBlock] $command
    )
    $saveErrorAction = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'

    try
    {
        & $command
        if ($LASTEXITCODE -ne 0)
        {
            throw "Execution of {$command} failed with exit code $LASTEXITCODE"
        }
    }
    finally
    {
        $ErrorActionPreference = $saveErrorAction
    }
}

#endregion Utilities

#region SysLog support

# Defines the array indices when calling
# String.Split on a SysLog log entry
enum SysLogIds
{
    Month = 0;
    Day = 1;
    Time = 2;
    Hostname = 3;
    Id = 4;
    CommitId = 5;
    EventId = 6;
    Message = 7;
}

# Defines the array indices when calling
# String.Split on an OsLog log entry
enum OsLogIds
{
    Date = 0;
    Time = 1;
    Thread = 2;
    Type = 3;
    Activity = 4;
    Pid = 5;
    ProcessName = 6;
    Module = 7;
    Id = 8;
    CommitId = 9;
    EventId = 10;
    Message = 11;
}

class PSLogItem
{
    [string] $LogId = [string]::Empty
    [DateTime] $Timestamp = [DateTime]::Now
    [string] $Hostname = [string]::Empty
    [int] $ProcessId = 0
    [int] $ThreadId = 0
    [int] $Channel = 0
    [string] $CommitId = [string]::Empty
    [string] $EventId = [string]::Empty
    [string] $Message = [string]::Empty
    [int] $Count = 1

    hidden static $monthNames = @('Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun','Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec')

    hidden static [int] GetMonth([string] $value)
    {
        Set-StrictMode -Version Latest
        for ($x = 0; $x -lt [PSLogItem]::monthNames.Count; $x++)
        {
            [string] $monthName = [PSLogItem]::monthNames[$x]
            if ($value.StartsWith($monthName, [StringComparison]::InvariantCultureIgnoreCase))
            {
                return $x + 1
            }
        }
        return 1
    }

    static [PSLogItem] ConvertSysLog([string] $content, [string] $id, [Nullable[DateTime]] $after)
    {
        Set-StrictMode -Version Latest
        <#
        MMM dd HH:MM:SS machinename id[PID]: (commitid:TID:CHANNEL) [EventName] Message
        Expecting split to return
        0: Month (abreviated)
        1: DD
        2: HH:MM:SS
        3: hostname
        4: nameid[processid]:
        5: (commitid:treadid:channel)
        6: [EventId]
        7: Message Text

        NOTE: In some cases, syslog will detect the same message being logged multiple times.
        In those cases, a single message is logged in the following format

        MMM dd HH:MM:SS machinename id[PID]: message repeated NNN times: [(commitid:TID:CHANNEL) [EventName] Message]
        #>

        # split contents into separate space delimited tokens (first 7) and leave the rest as the message.
        [string[]] $parts = $content.Split(' ', 8, [System.StringSplitOptions]::RemoveEmptyEntries)

        if ($parts.Count -ne 8)
        {
            Write-Verbose -Message "Skipping unparsable entry: $content"
            return $null
        }

        if ($id)
        {
            # If the log entry doesn't have the expected $id, return null.
            if ($parts[[SysLogIds]::Id].StartsWith($id, [StringComparison]::OrdinalIgnoreCase) -eq $false)
            {
                return $null
            }
        }

        $now = [DateTime]::Now
        $month = [PSLogItem]::GetMonth($parts[[SysLogIds]::Month])
        $day = [int]::Parse($parts[[SysLogIds]::Day])

        # guess at the year
        $year = [DateTime]::Now.Year
        if (($month -gt $now.Month) -or ($now.Month -eq $month -and $day -gt $now.Day))
        {
            $year--;
        }
        [DateTime]$time = [DateTime]::Parse($parts[[SysLogIds]::Time], [System.Globalization.CultureInfo]::InvariantCulture)
        $time = [DateTime]::new($year, $month, $day, $time.Hour, $time.Minute, $time.Second)

        if ($after -ne $null -and $time -lt $after)
        {
            # if the entry was logged prior to the expected time, return null
            return $null
        }

        $item = [PSLogItem]::new()
        $item.Timestamp = $time

        $item.Message = $parts[[SysLogIds]::Message]

        [char[]] $splitChars = $null

        # handle log entries that have 'message repeated NNN times: ['
        if ($parts[[SysLogIds]::CommitId] -eq 'message' -and $parts[[SysLogIds]::EventId] -eq 'repeated')
        {
            # NNN times: [ message ]
            $splitChars = (' ', ':', '[')
            $subparts = $item.Message.Split($splitChars, 3, [System.StringSplitOptions]::RemoveEmptyEntries)
            $item.Count = [int]::Parse($subparts[0])
            $value = $subparts[2]

            # (commitid:TID:CHANNEL)
            $start = $value.IndexOf('(')
            $end = $value.IndexOf(')')
            $parts[[SysLogIds]::CommitId] = $value.Substring($start, $end-$start+1)
            $value = $value.Substring($end + 1)

            # [eventid]
            $start = $value.IndexOf('[')
            $end = $value.IndexOf(']')
            $parts[[SysLogIds]::EventId] = $value.Substring($start, $end-$start+1)

            # message text
            # NOTE: Skip the trailing ']'
            $end++
            $item.Message = $value.Substring($end, $value.Length - ($end + 1))
        }

        $item.Hostname = $parts[[SysLogIds]::Hostname]

        # [EventId]
        $splitChars = ('[', ']', ' ')
        $item.EventId = $parts[[SysLogIds]::EventId]
        $subparts = $item.EventId.Split($splitChars, [System.StringSplitOptions]::RemoveEmptyEntries)
        if ($subparts.Count -eq 1)
        {
            $item.EventId = $subparts[0]
        }
        else
        {
            Write-Warning -Message "Could not split EventId $($item.EventId) on '[] ' Count:$($subparts.Count)"
        }

        # (commitid:TID:ChannelID)
        [char[]] $splitChars = ('(', ')', ':', ' ')
        $item.CommitId = $parts[[SysLogIds]::CommitId]
        $subparts = $item.CommitId.Split($splitChars, [System.StringSplitOptions]::RemoveEmptyEntries)
        if ($subparts.Count -eq 3)
        {
            $item.CommitId = $subparts[0]
            $item.ThreadId = [int]::Parse($subparts[1], [System.Globalization.NumberStyles]::AllowHexSpecifier)
            $item.Channel = [int]::Parse($subparts[2])
        }
        else
        {
            Write-Warning -Message "Could not split CommitId $($item.CommitId) on '(): ' Count:$($subparts.Count)"
        }

        # nameid[PID]
        $splitChars = ('[',']',':')
        $item.LogId = $parts[[SysLogIds]::Id]
        $subparts = $item.LogId.Split($splitChars, [System.StringSplitOptions]::RemoveEmptyEntries)
        if ($subparts.Count -eq 2)
        {
            $item.LogId = $subparts[0]
            $item.ProcessId = [int]::Parse($subparts[1], [System.Globalization.NumberStyles]::AllowHexSpecifier)
        }
        else
        {
            Write-Warning -Message "Could not split LogId $($item.LogId) on '[]:' Count:$($subparts.Count)"
        }

        return $item
    }

    static [object] ConvertOsLog([string] $content, [string] $id, [Nullable[DateTime]] $after)
    {
        Set-StrictMode -Version Latest
        <#
        Expecting split to return
        0: date                         2018-02-07
        1: time                         14:34:35.256501-0800
        2: thread                       0x2a3730
        3: Type                         Default
        4: activity                     0x12
        5: PID                          39437
        6: processname                  pwsh:
        7: sourcedll                    (libpsl-native.dylib)
        8: log source                   [com.microsoft.powershell.powershell]
        9: commitid:treadid:channel     (v6.0.1:1:10)
        10:[EventId]                    [Perftrack_ConsoleStartupStart:PowershellConsoleStartup.WinStart.Informational]
        11:Message Text
        #>

        [object] $result = $content
        [char[]] $splitChars = $null
        do
        {
            # determine if the line is a log entry
            # versus an overflow of a multi-line entry

            # check for thread
            $index = $content.IndexOf(" 0x")
            if ($index -le 0)
            {
                # no thread value,
                # assume text only
                break
            }

            # look for a date/time stamp prior to the thread.
            # if this succeeds, we'll ignore the values in split.
            [DateTime] $time = [DateTime]::Now
            $value = $content.Substring(0, $index).Trim()
            if ([DateTime]::TryParse($value, [ref] $time) -eq $false)
            {
                # no timestamp,
                # assume text only
                break
            }

            if ($after -ne $null -and $time -lt $after)
            {
                $result = $null
                break
            }

            $parts = $content.Split(' ', [StringSplitOptions]::RemoveEmptyEntries)
            $item = [PSLogItem]::new()
            $item.Count = 1

            $item.Timestamp = $time
            $item.ProcessId = [int]::Parse($parts[[OsLogIds]::Pid])
            $item.Message = $parts[[OsLogIds]::Message]

            # [com.microsoft.powershell.logid]
            $splitChars = ('[', '.', ']')
            $item.LogId = $parts[[OsLogIds]::Id]
            $subparts = $item.LogId.Split($splitChars, [StringSplitOptions]::RemoveEmptyEntries)
            if ($subparts.Length -eq 4)
            {
                $item.LogId = $subparts[3]
                if ($id -ne $null -and $id -ne $item.LogId)
                {
                    # this is not the log id we're looking for.
                    $result = $null
                    break
                }
            }
            # (commitid:TID:ChannelID)
            $splitChars = ('(', ')', ':', ' ')
            $item.CommitId = $parts[[OsLogIds]::CommitId]
            $subparts = $item.CommitId.Split($splitChars, [System.StringSplitOptions]::RemoveEmptyEntries)
            if ($subparts.Count -eq 3)
            {
                $item.CommitId = $subparts[0]
                $item.ThreadId = [int]::Parse($subparts[1], [System.Globalization.NumberStyles]::AllowHexSpecifier)
                $item.Channel = [int]::Parse($subparts[2])
            }
            else
            {
                Write-Warning -Message "Could not split CommitId $($item.CommitId) on '(): ' Count:$($subparts.Count)"
            }

            # [EventId]
            $splitChars = ('[', ']', ' ')
            $item.EventId = $parts[[OsLogIds]::EventId]
            $subparts = $item.EventId.Split($splitChars, [System.StringSplitOptions]::RemoveEmptyEntries)
            if ($subparts.Count -eq 1)
            {
                $item.EventId = $subparts[0]
            }
            else
            {
                Write-Warning -Message "Could not split EventId $($item.EventId) on '[] ' Count:$($subparts.Count)"
            }

            $result = $item
        } while ($false)

        # returning [string] or [PSLogItem]
        return $result
    }
}

function ConvertFrom-SysLog
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory, ValueFromPipeline)]
        [string[]] $Content,

        [string] $Id,

        [Nullable[DateTime]] $After
    )

    Begin
    {
        [int] $totalWritten = 0
    }

    Process
    {
        foreach ($line in $Content)
        {
            [PSLogItem] $item = [PSLogItem]::ConvertSysLog($line, $id, $after)
            if ($item -ne $null)
            {
                $totalWritten++
                Write-Output $item
            }
        }
    }
    End
    {
        Write-Verbose "Found $totalWritten items"
    }
}

<#
.SYNOPSIS
    Reads log entries with the specified identifier

.PARAMETER Path
    The fully qualified path to the syslog formatted file.

.PARAMETER Id
    The identifier for the entries to read.
    The default value is 'powershell'

.PARAMETER TotalCount
    Specifies the number of items to return.
    Can be used with After and Tail

.PARAMETER Tail
    Specifies the number of lines from the end of a file
    This value is passed through to the underlying Get-Content Cmdlet and controls
    the number of lines to read from the syslog file. As a result, the number of
    returned PowerShell log items may be less than this value.

.PARAMETER After
    Returns items on or after the specified DateTime
    Can be used with TotalCount.

.EXAMPLE
    PS> Get-PSSysLog -id 'powershell' -logPath '/var/log/powershell'
    Gets all log entries from the log with the id 'powershell' from the log /var/log/powershell

.EXAMPLE
    PS> Get-PSSysLog -id 'powershell' -logPath '/var/log/syslog'
    Gets all log entries from the log with the id 'powershell' from the log /var/log/syslog

.Example
    PS> Get-PSSysLog -id 'powershell' -logPath '/var/log/syslog' -Tail 200
    Gets the last 200 log entries from /var/log/syslog and returns items from
    this set that have the id 'powershell'.

.Example
    PS> Get-PSSysLog -id 'powershell' -logPath '/var/log/syslog' -TotalCount 200
    Gets up to 200 log entries with the id 'powershell' from /var/log/syslog

.Example
    PS> $time = [DateTime]::Parse('1/19/2018 1:26:49 PM')
    PS> Get-PSSysLog -id 'powershell' -logPath '/var/log/syslog' -After $time

    Gets log entries with the id 'powershell' that occured on or after a specific date/time

.NOTES
    This function reads syslog entries using Get-Content, filters based on the id, and
    returns an object for each log entry.
#>
function Get-PSSysLog
{
    [CmdletBinding(DefaultParameterSetName='All')]
    param
    (
        [Parameter(ParameterSetName = 'All')]
        [Parameter(ParameterSetName = 'After')]
        [Parameter(ParameterSetName = 'Tail')]
        [ValidateNotNullOrEmpty()]
        [string] $Path,

        [Parameter(ParameterSetName = 'All')]
        [Parameter(ParameterSetName = 'After')]
        [Parameter(ParameterSetName = 'Tail')]
        [ValidateNotNullOrEmpty()]
        [string] $Id = 'powershell',

        [Parameter(ParameterSetName='All')]
        [Parameter(ParameterSetName = 'After')]
        [Parameter(ParameterSetName = 'Tail')]
        [ValidateRange(0, [int]::MaxValue)]
        [int] $TotalCount,

        [Parameter(ParameterSetName = 'All')]
        [Parameter(ParameterSetName = 'Tail')]
        [ValidateRange(0, [int]::MaxValue)]
        [int] $Tail,

        [Parameter(ParameterSetName = 'All')]
        [Parameter(ParameterSetName = 'After')]
        [Nullable[DateTime]] $After
    )

    [int] $maxItems = 0
    $contentParms = @{Path = $Path}
    if ($PSBoundParameters.ContainsKey('Tail'))
    {
        $contentParms['Tail'] = $Tail
    }

    if ($PSBoundParameters.ContainsKey('TotalCount'))
    {
        $maxItems = $TotalCount
    }

    if ($TotalCount -eq 0)
    {
        Get-Content @contentParms | ConvertFrom-SysLog -After $After -Id $Id
    }
    else
    {
        [string] $filter = [string]::Format(" {0}[", $id)
        Get-Content @contentParms -filter {$_.Contains($filter)} | ConvertFrom-SysLog -Id $Id -After $After | Select-Object -First $maxItems
    }
}

#endregion SysLog support

#region os_log support

<#
    Provides a utility class for handling single and multi-line os_log entries
#>
class LogItemBuilder
{
    hidden [System.Text.StringBuilder] $sb = [System.Text.StringBuilder]::new()
    hidden [PSLogItem] $item = $null

    PSLogItemBuilder()
    {
    }

    [PSLogItem] Add([object] $value)
    {
        [PSLogItem] $result = $null
        if ($value -eq $null)
        {
        }
        elseif ($value -is [PSLogItem])
        {
            # return the pending item
            $result = $this.Flush()
            if ($result -ne $null -and $this.sb.Length -gt 0)
            {
                $result.Message = $this.sb.ToString()
            }

            $null = $this.sb.Clear()
            # save the current one to handle multiline
            # message text.
            $this.item = $value
        }
        elseif ($this.item -eq $null)
        {
            # Exported logs contain header lines;
            # skip these.
        }
        else
        {
            # we have an item pending...
            # build a multi-line message property
            if ($this.sb.Length -eq 0)
            {
                $null = $this.sb.Append($this.item.Message)
            }
            $null = $this.sb.AppendLine('')
            $null = $this.sb.Append($value.ToString())
        }

        return $result
    }

    [PSLogItem] Flush()
    {
        [PSLogItem] $result = $null
        if ($this.item -ne $null)
        {
            $result = $this.item
            $this.item = $null
            if ($this.sb.Length -gt 0)
            {
                # rewrite the message wtih the multiple log lines
                $result.Message = $this.sb.ToString()
                $this.sb.Clear()
            }
        }
        return $result
    }
}

function ConvertFrom-OSLog
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory, ValueFromPipeline)]
        [string[]] $Content,

        [string] $Id,

        [Nullable[DateTime]] $After
    )

    Begin
    {
        [int] $totalWritten = 0
        # NOTE: Log items can span multiple lines
        [LogItemBuilder] $builder = [LogItemBuilder]::new()
    }

    Process
    {
        foreach ($line in $Content)
        {
            [object] $item = [PSLogItem]::ConvertOsLog($line, $id, $after)

            # os_log entries can span multiple lines when new lines are
            # included in the entry's message text.
            # To ensure the entire log entry is processed,
            #    LogItemBuilder.Add will not return an item until it encounters the start
            #    of another log entry.
            # To ensure the last item is processed, Flush needs to be called at the end of the
            #    pipeline. See the End block below.
            $item = $builder.Add($item)
            if ($item -ne $null)
            {
                $totalWritten++
                Write-Output $item
            }
        }
    }
    End
    {
        $item = $builder.Flush()
        if ($item -ne $null)
        {
            $totalWritten++
            Write-Output $item
        }
        Write-Verbose "Found $totalWritten items"
    }
}

<#
.SYNOPSIS
    Reads log entries with the specified identifier

.Description
    This cmdlet parses a text file exported from a MacOS os_log.

.PARAMETER Path
    The fully qualified path to the os_log formatted file.

.PARAMETER Id
    The identifier for the PowerShell log identity of the instance(s) producing the log content.
    The default value is 'powershell'

.PARAMETER TotalCount
    Specifies the maximum number of items to return.

.PARAMETER After
    Returns items on or after the specified DateTime

.EXAMPLE
    PS> Export-OSLog -After $timestamp | Set-Content -Path "$PSDrive/mytest.txt"
    PS> Get-PSOsLog -logPath "$PSDrive/mytest.txt"

    Gets all log entries from a given timestamp.

.EXAMPLE
    PS> Export-OSLog -After $timestamp | Set-Content -Path "$PSDrive/mytest.txt"
    PS> Get-PSOsLog -id 'mypwsh' -logPath "$PSDrive/mytest.txt" -TotalCount 200

    Gets up to 200 log entries from a given timestamp with the log identity of 'mypwsh'
#>
function Get-PSOsLog
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $Path,

        [string] $Id = 'powershell',

        [ValidateRange(0, [int]::MaxValue)]
        [int] $TotalCount,

        [Nullable[DateTime]] $After
    )

    [int] $maxItems = 0
    $contentParms = @{Path = $Path}

    if ($PSBoundParameters.ContainsKey('TotalCount'))
    {
        $maxItems = $TotalCount
    }

    if ($TotalCount -eq 0)
    {
        Get-Content @contentParms | ConvertFrom-OsLog -After $After -Id $Id
    }
    else
    {
        [string] $filter = [string]::Format("com.microsoft.powershell.{0}: (", $id)
        Get-Content @contentParms -filter {$_.Contains($filter)} | ConvertFrom-OsLog -Id $Id -After $After | Select-Object -First $maxItems
    }
}

<#
.SYNOPSIS
    Export PowerShell os_log content as text.

.PARAMETER After
    The datetime for the starting entries to export.
    Log entries with a timestamp on or after this value will be returned.

.EXAMPLE
    PS> $timestamp = [DateTime]::Now
    PS> # perform some work...
    PS> $content = (Export-PSOsLog -After $timestamp)

.EXAMPLE
    PS> $timestamp = [DateTime]::Now
    PS> # perform some work...
    PS> Export-PSOsLog -After $timestamp | Set-Content -Path "$PSDrive/mytest.txt"

.NOTES
    This command requires MacOS.
    See Get-PSOsLog for parsing content from this cmdlet
#>
function Export-PSOsLog
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [DateTime] $After,

        [string] $LogId = "powershell"
    )
    Test-MacOS
    # NOTE: The use of double quotes and single quotes for the predicate parameter
    # is mandatory. Reversing the usage (e.g., single quotes around double quotes)
    # causes the double quotes to be stripped breaking the predicate syntax expected
    # by log show
    if ($After -ne $null)
    {
        [string] $startTime = $After.ToString("yyyy-MM-dd HH:mm:ss")
        Start-NativeExecution -command {log show --info --start "$startTime" --predicate "subsystem == 'com.microsoft.powershell'"}
    }
    else
    {
        Start-NativeExecution -command {log show --info --predicate "process == 'pwsh'"}
    }
}

<#
.SYNOPSIS
    Enables or disables persistence of PowerShell logging

.PARAMETER Enable
    Enable persistence of PowerShell log items

.PARAMETER Disable
    Disables persistent of PowerShell log items.
    This reverts persistence to the system default.

.EXAMPLE
    Set-OsLogPersistence -Enable
    Enables persistence of PowerShell log entries

.EXAMPLE
    Set-OsLogPersistence -Disable
    Reverts persistence to the default state.

.NOTES
    See Get-OsLogPersistence to query the current setting.
#>
function Set-OsLogPersistence
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory, ParameterSetName='Enable')]
        [switch] $Enable,

        [Parameter(Mandatory, ParameterSetName='Disable')]
        [switch] $Disable
    )
    Test-MacOS
    Test-Sudo

    if ($Enable -eq $true)
    {
        Write-Verbose -Message "Enabling log persistence"
        Start-NativeExecution -command {log config --subsystem com.microsoft.powershell --mode=persist:info,level:info }
    }
    else
    {
        Write-Verbose -Message "Reverting log persistence to the default"
        Start-NativeExecution -command {log config --subsystem com.microsoft.powershell --mode=persist:default,level:default }
    }
}

<#
.SYNOPSIS
   Gets the current PowerShell logging persistence setting
#>
function Get-OsLogPersistence
{
    Test-MacOS
    Test-Sudo

    $result = Start-NativeExecution -command {log config --status --subsystem com.microsoft.powershell}
    $parts = $result.Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries)

    if ($parts[$parts.Length - 1] -eq 'PERSIST_DEFAULT')
    {
        # Not configured
        # Expecting a format like the following:
        # Mode for 'com.microsoft.powershell'  PERSIST_DEFAULT
        $result = new-object PSObject -Property @{
            Level = 'DEFAULT'
            Persist = $parts[$parts.Length- 1]
            Enabled = $false
        }
    }
    else
    {
        # Expecting a format like the following:
        # Mode for 'com.microsoft.powershell'  INFO PERSIST_INFO
        $result = new-object PSObject -Property @{
            Level = $parts[$parts.Length - 2]
            Persist = $parts[$parts.Length -1]
            Enabled = $true
        }
    }
    return $result
}

#region os_log support

Export-ModuleMember -Function Get-PSSysLog, Get-PSOsLog, Export-PSOsLog, Get-OsLogPersistence, Set-OsLogPersistence

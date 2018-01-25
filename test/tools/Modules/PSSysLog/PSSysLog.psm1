Set-StrictMode -Version Latest

enum Ids
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

class PSSysLogItem
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
        for ($x = 0; $x -lt [PSSysLogItem]::monthNames.Count; $x++)
        {
            [string] $monthName = [PSSysLogItem]::monthNames[$x]
            if ($value.StartsWith($monthName, [StringComparison]::InvariantCultureIgnoreCase))
            {
                return $x + 1
            }
        }
        return 1
    }

    static [PSSysLogItem] Convert([string] $content, [string] $id, [Nullable[DateTime]] $after)
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
        [string[]] $parts = $content.Split(' ', 8, [System.StringSplitOptions]::RemoveEmptyEntries)

        if ($parts.Count -ne 8)
        {
            Write-Verbose -Message "Skipping unparsable entry: $content"
            return $null
        }

        if ([string]::IsNullOrEmpty($id) -eq $false)
        {
            if ($parts[[Ids]::Id].StartsWith($id, [StringComparison]::OrdinalIgnoreCase) -eq $false)
            {
                return $null
            }
        }

        $now = [DateTime]::Now
        $month = [PSSysLogItem]::GetMonth($parts[[Ids]::Month])
        $day = [int]::Parse($parts[[Ids]::Day])

        # guess at the year
        $year = [DateTime]::Now.Year
        if (($month -gt $now.Month) -or ($now.Month -eq $month -and $day -gt $now.Day))
        {
            $year--;
        }
        [DateTime]$time = [DateTime]::Parse($parts[[ids]::Time], [System.Globalization.CultureInfo]::InvariantCulture)
        $time = [DateTime]::new($year, $month, $day, $time.Hour, $time.Minute, $time.Second)

        if ($after -ne $null -and $after -gt $time)
        {
            return $null
        }

        $item = [PSSysLogItem]::new()
        $item.Timestamp = $time

        $item.Message = $parts[[Ids]::Message]

        [char[]] $splitChars = $null

        # handle log entries that have 'message repeated NNN times: ['
        if ($parts[[Ids]::CommitId] -eq 'message' -and $parts[[Ids]::EventId] -eq 'repeated')
        {
            # NNN times: [ message ]
            $splitChars = (' ', ':', '[')
            $subparts = $item.Message.Split($splitChars, 3, [System.StringSplitOptions]::RemoveEmptyEntries)
            $item.Count = [int]::Parse($subparts[0])
            $value = $subparts[2]

            # (commitid:TID:CHANNEL)
            $start = $value.IndexOf('(')
            $end = $value.IndexOf(')')
            $parts[[Ids]::CommitId] = $value.Substring($start, $end-$start+1)
            $value = $value.Substring($end + 1)

            # [eventid]
            $start = $value.IndexOf('[')
            $end = $value.IndexOf(']')
            $parts[[Ids]::EventId] = $value.Substring($start, $end-$start+1)

            # message text
            # NOTE: Skip the trailing ']'
            $end++
            $item.Message = $value.Substring($end, $value.Length - ($end + 1))
        }

        $item.Hostname = $parts[[Ids]::Hostname]

        # [EventId]
        $splitChars = ('[', ']', ' ')
        $item.EventId = $parts[[Ids]::EventId]
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
        $splitChars = ('(', ')', ':', ' ')
        $item.CommitId = $parts[[Ids]::CommitId]
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
        $item.LogId = $parts[[Ids]::Id]
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
}
function ConvertFrom-SysLog
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory, ValueFromPipeline)]
        [ValidateNotNullOrEmpty()]
        [string] $content,

        [string] $Id,

        [Nullable[DateTime]] $After
    )

    Process
    {
        [PSSysLogItem] $item = [PSSysLogItem]::Convert($content, $id, $after)

        if ($item -ne $null)
        {
            Write-Output -InputObject $item
        }
    }
}

<#
.SYNOPSIS
    Reads log entries with the specified identifier

.DESCRIPTION
    Long description

.PARAMETER Path
    The fully qualified path to the syslog formatted file.

.PARAMETER Id
    The identifier for the entries to read.
    The default value is 'powershell'

.PARAMETER TotalCount
    Specifies the number of lines from the beginning of a file

.PARAMETER TotalCount
    Specifies the number of lines from the end of a file

.PARAMETER After
    Returns items after the specified DateTime

.EXAMPLE
    PS> Get-PSSysLog -id 'powershell' -logPath '/var/log/powershell'
    Gets all log entries from the log with the id 'powershell' from the log /var/log/powershell

.EXAMPLE
    PS> Get-PSSysLog -id 'powershell' -logPath '/var/log/syslog'
    Gets all log entries from the log with the id 'powershell' from the log /var/log/syslog

.Example
    PS> Get-PSSysLog -id 'powershell' -logPath '/var/log/syslog' -Tail 200
    Gets the last 200 items from /var/log/syslog

.Example
    PS> Get-PSSysLog -id 'powershell' -logPath '/var/log/syslog' -TotalCount 200
    Gets up to 200 items powershell from the /var/log/syslog

.Example
    PS> $time = [DateTime]::Parse('1/19/2018 1:26:49 PM')
    PS> Get-PSSysLog -id 'powershell' -logPath '/var/log/syslog' -After $time

    Gets log entries with the id 'powershell' that occured after a specific date/time

.NOTES
    This function reads syslog entries using Get-Content, filters based on the id, and
    returns an object for each log entry.
#>
function Get-PSSysLog
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string] $Path,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string] $Id = 'powershell',

        [Parameter(ParameterSetName = 'TotalCount')]
        [int] $TotalCount,

        [Parameter(ParameterSetName = 'Tail')]
        [int] $Tail,

        [Parameter(ParameterSetName = 'After')]
        [Nullable[DateTime]] $After
    )

    $contentParms = @{Path = $Path}
    if ($PSBoundParameters.ContainsKey('TotalCount'))
    {
        $contentParms['TotalCount'] = $TotalCount
    }
    elseif ($PSBoundParameters.ContainsKey('Tail'))
    {
        $contentParms['Tail'] = $Tail
    }

    if ([string]::IsNullOrEmpty($id))
    {
        Get-Content @contentParms | ConvertFrom-SysLog -After $After -Id $Id
    }
    else
    {
        [string] $filter = [string]::Format(" {0}[", $id)

        Get-Content @contentParms | Where-Object {$_.Contains($filter)} | ConvertFrom-SysLog -Id $Id -After $After
    }
}

Export-ModuleMember -Function Get-PSSysLog

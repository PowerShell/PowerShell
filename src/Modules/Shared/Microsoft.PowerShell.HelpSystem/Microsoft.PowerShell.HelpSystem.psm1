function Help {
<#
.FORWARDHELPTARGETNAME Get-Help
.FORWARDHELPCATEGORY Cmdlet
#>
[CmdletBinding(DefaultParameterSetName='AllUsersView', HelpUri='https://go.microsoft.com/fwlink/?LinkID=113316')]
param(
    [Parameter(Position=0, ValueFromPipelineByPropertyName=$true)]
    [string]
    ${Name},

    [string]
    ${Path},

    [ValidateSet('Alias','Cmdlet','Provider','General','FAQ','Glossary','HelpFile','ScriptCommand','Function','Filter','ExternalScript','All','DefaultHelp','Workflow','DscResource','Class','Configuration')]
    [string[]]
    ${Category},

    [Parameter(ParameterSetName='DetailedView', Mandatory=$true)]
    [switch]
    ${Detailed},

    [Parameter(ParameterSetName='AllUsersView')]
    [switch]
    ${Full},

    [Parameter(ParameterSetName='Examples', Mandatory=$true)]
    [switch]
    ${Examples},

    [Parameter(ParameterSetName='Parameters', Mandatory=$true)]
    [string]
    ${Parameter},

    [string[]]
    ${Component},

    [string[]]
    ${Functionality},

    [string[]]
    ${Role},

    [Parameter(ParameterSetName='Online', Mandatory=$true)]
    [switch]
    ${Online},

    [Parameter(ParameterSetName='ShowWindow', Mandatory=$true)]
    [switch]
    ${ShowWindow})

    # Display the full help topic by default but only for the AllUsersView parameter set.
    if (($psCmdlet.ParameterSetName -eq 'AllUsersView') -and !$Full) {
        $PSBoundParameters['Full'] = $true
    }

    # Nano needs to use Unicode, but Windows and Linux need the default
    $OutputEncoding = if ([System.Management.Automation.Platform]::IsNanoServer -or [System.Management.Automation.Platform]::IsIoT) {
        [System.Text.Encoding]::Unicode
    } else {
        [System.Console]::OutputEncoding
    }

    $help = Get-Help @PSBoundParameters

    # If a list of help is returned, don't pipe to more
    if (($help | Select-Object -First 1).PSTypeNames -Contains 'HelpInfoShort')
    {
        $help
    }
    else
    {
        # Respect PAGER, use more on Windows, and use less on Linux
        $moreCommand,$moreArgs = $env:PAGER -split '\s+'
        if ($moreCommand) {
            $help | & $moreCommand $moreArgs
        } elseif ($IsWindows) {
            $help | more.com
        } else {
            $help | less -Ps""Page %db?B of %D:.\. Press h for help or q to quit\.$""
        }
    }
"QQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQ"
}


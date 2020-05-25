<#
.SYNOPSIS
    Gets FileVersion of Process running
.DESCRIPTION
    A running process uses a .exe which has a FileVersion
.EXAMPLE
    PS C:\> Get-FileVersion Process/WindowName/PID
    Gets the FileVersion of the given Process  
.NOTES
    In development
#>
function Get-FileVersion
{
    [CmdletBinding()]
    param(
    ### The Process you want the FileVersion
    [Parameter(ValueFromPipeline=$true, ParameterSetName="Process")]
    [System.Diagnostics.Process]$Process,
    ### The Process Name you want the FileVersion of
    [Parameter(ValueFromPipeline=$true, ParameterSetName="WindowName")]
    [String]$WindowName = "",
    # The process ID to get the DLLs of
    [Parameter(ValueFromPipeline=$true, ParameterSetName="ProcessId")]
    [Int]$ProcessId = 0
    )

    Begin {
        $script:Processes = @()1
    }

    Process {
        if ($Process -ne $null)
        {
            $Processes = Get-Process -FileVersionInfo $Process
        }
        elseif (-not [string]::IsNullOrEmpty($ProcessName))
        {
            $Processes += (Get-Process).Where({$_.MainWindowTitle -match "$WindowName"})
        }
        elseif ($ProcessId -ne 0)
        {
            $Processes += Get-Process -Id $ProcessId
        }
    }

    End {
        if ($Processes.Length -gt 0) 
        {
            $Processes
            return
        }

        if (-not [string]::IsNullOrEmpty($Process))
        {
            $Processes = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($Process).FileVersion
        }
    
        $Processes
    }
}
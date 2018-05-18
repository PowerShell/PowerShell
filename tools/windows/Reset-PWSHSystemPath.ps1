<#
.SYNOPSIS
  Idempotently removes extra PowerShell Core paths from the machine, user and/or process environment scope with no reordering.

.DESCRIPTION
  Defaults to machine scope and leaving the last sorted path alone.
  Does not touch path if there is nothing to clean.
  Emits one simple log line about it's actions for each scope.

  Also accessible in the powershell-core Chocolatey package by using -params '"/CleanUpSystemPath"'

.PARAMETER PathScope
  Set of machine scopes to clean up.  Valid options are one or more of: Machine, User, Process.
  Default: machine

.PARAMETER RemoveAllOccurences
  By default the cleanup leaves the highest sorted PowerShell Core path alone.  
  This switch causes it to be cleaned up as well.
  Default: false

.EXAMPLE
  .\Reset-PWSHSystemPath.ps1
  
  Removes all PowerShell core paths but the very last one when sorted in ascending order from the Machine level path.
  Good for running on systems that already has at least one valid PowerShell install.

.EXAMPLE
  .\Reset-PWSHSystemPath.ps1 -RemoveAllOccurences
  
  Removes ALL PowerShell core paths from the Machine level path.
  Good for running right before upgrading PowerShell core.
.EXAMPLE
  .\Reset-PWSHSystemPath.ps1 -PathScope Machine, User, Process

  Removes all paths but the very last one when sorted in ascending order.
  Processes all path scopes including current process.
.EXAMPLE
  .\Reset-PWSHSystemPath.ps1 -PathScope Machine, User, Process -RemoveAllOccurencs

  Removes all paths from all path scopes including current process.
#>
param (
  [ValidateSet("machine","user","process")]
  [string[]]$PathScope="machine",
  [switch]$RemoveAllOccurences
)
ForEach ($PathScopeItem in $PathScope)
{
  $AssembledNewPath = $NewPath = ''
  $pathstoremove = @([Environment]::GetEnvironmentVariable("PATH","$PathScopeItem").split(';') | Where { $_ -ilike "*\Program Files\Powershell\6*"})
  If (!$RemoveAllOccurences) 
  {
    $pathstoremove = @($pathstoremove | sort-object | Select-Object -skiplast 1)
  }
  Write-Host "Reset-PWSHSystemPath: Found $($pathstoremove.count) paths to remove from $PathScopeItem path scope: $($Pathstoremove -join ', ' | out-string)"
  If ($pathstoremove.count -gt 0)
  {
    foreach ($path in [Environment]::GetEnvironmentVariable("PATH","$PathScopeItem").split(';'))
    {
      If ($Path)
      {
        If ($pathstoremove -inotcontains "$Path")
        {
          [string[]]$Newpath += "$path"
        }
      }
    }
    $AssembledNewPath = ($newpath -join(';')).trimend(';')
    $AssembledNewPath -split ';'
    [Environment]::SetEnvironmentVariable("PATH",$AssembledNewPath,"$PathScopeItem")
  }
}
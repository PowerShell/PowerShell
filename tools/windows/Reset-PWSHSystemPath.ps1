# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

<#
.SYNOPSIS
  Idempotently removes extra PowerShell Core paths from the machine, user and/or process environment scope with no reordering.

.DESCRIPTION
  Defaults to machine scope and leaving the last sorted path alone.
  Does not touch path if there is nothing to clean.
  Emits one simple log line about its actions for each scope.

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

#Do the cleanup for all specified path scopes
ForEach ($PathScopeItem in $PathScope)
{
  $AssembledNewPath = $NewPath = ''
  #From the current path scope. retrieve the array of paths that match the pathspec of PowerShell Core (to use as a filter)
  $pathstoremove = @([Environment]::GetEnvironmentVariable("PATH","$PathScopeItem").split(';') | Where { $_ -ilike "*\Program Files\Powershell\6*"})
  If (!$RemoveAllOccurences)
  {
    #If we are not removing all occurances of powershell core paths, then remove the highest sorted path from the filter
    $pathstoremove = @($pathstoremove | sort-object | Select-Object -skiplast 1)
  }
  Write-Verbose "Reset-PWSHSystemPath: Found $($pathstoremove.count) paths to remove from $PathScopeItem path scope: $($Pathstoremove -join ', ' | out-string)"
  If ($pathstoremove.count -gt 0)
  {
    foreach ($Path in [Environment]::GetEnvironmentVariable("PATH","$PathScopeItem").split(';'))
    {
      #rebuild the path in the same order, but eliminate the paths in the filter array and blanks
      If ($Path)
      {
        If ($pathstoremove -inotcontains "$Path")
        {
          [string[]]$Newpath += "$Path"
        }
      }
    }
    $AssembledNewPath = ($newpath -join(';')).trimend(';')
    $AssembledNewPath -split ';'
    [Environment]::SetEnvironmentVariable("PATH",$AssembledNewPath,"$PathScopeItem")
  }
}

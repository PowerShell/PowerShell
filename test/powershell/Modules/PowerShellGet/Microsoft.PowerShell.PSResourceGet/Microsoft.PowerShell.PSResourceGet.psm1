<#
    .SYNOPSIS
    Finds the repositories registered with PowerShellGet and registers them for PSResourceGet.
#>
function Import-PSGetRepository {
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
    param(
        # Use the -Force switch to overwrite existing repositories.
        [switch]$Force
    )

    $Runtime = [System.Runtime.InteropServices.RuntimeInformation]
    $OSPlatform = [System.Runtime.InteropServices.OSPlatform]
    # this checks for WindowsPwsh and Core
    $IsOSWindows = $Runtime::IsOSPlatform($OSPlatform::Windows)
    if ($IsOSWindows) {
        $PSGetAppLocalPath = Microsoft.PowerShell.Management\Join-Path -Path $env:LOCALAPPDATA -ChildPath 'Microsoft\Windows\PowerShell\PowerShellGet\'
    }
    else {
        $PSGetAppLocalPath = Microsoft.PowerShell.Management\Join-Path -Path ([System.Management.Automation.Platform]::SelectProductNameForDirectory('CACHE')) -ChildPath 'PowerShellGet'
    }
    $PSRepositoriesFilePath = Microsoft.PowerShell.Management\Join-Path -Path $PSGetAppLocalPath -ChildPath "PSRepositories.xml"
    $PSGetRepositories = Microsoft.PowerShell.Utility\Import-Clixml $PSRepositoriesFilePath -ea SilentlyContinue

    Microsoft.PowerShell.Utility\Write-Verbose ('Found {0} registered PowerShellGet repositories.' -f $PSGetRepositories.Count)

    if ($PSGetRepositories.Count) {
        $repos = @(
            $PSGetRepositories.Values |
            Microsoft.PowerShell.Core\Where-Object {$_.PackageManagementProvider -eq 'NuGet'-and $_.Name -ne 'PSGallery'} |
            Microsoft.PowerShell.Utility\Select-Object Name, Trusted, SourceLocation
        )

        Microsoft.PowerShell.Utility\Write-Verbose ('Selected {0} NuGet repositories.' -f $repos.Count)

        if ($repos.Count) {
            $repos | Microsoft.PowerShell.Core\ForEach-Object {
                try {
                    $message = 'Registering {0} at {1} -Trusted:${2} -Force:${3}.' -f $_.Name,
                        $_.SourceLocation, $_.Trusted, $Force
                    if ($PSCmdlet.ShouldProcess($message, $_.Name, 'Register-PSResourceRepository')) {
                        $registerPSResourceRepositorySplat = @{
                            Name = $_.Name
                            Uri = $_.SourceLocation
                            Trusted = $_.Trusted
                            PassThru = $true
                            Force = $Force
                            ApiVersion = if ([Uri]::new($_.SourceLocation).Scheme -eq 'file') {'local'} else {'v2'}
                        }
                        Register-PSResourceRepository @registerPSResourceRepositorySplat
                    }
                }
                catch [System.Management.Automation.PSInvalidOperationException] {
                    if ($_.Exception.Message -match 'already exists') {
                        Microsoft.PowerShell.Utility\Write-Warning $_.Exception.Message
                        Microsoft.PowerShell.Utility\Write-Warning 'Use the -Force switch to overwrite existing repositories.'
                    }
                    else {
                        throw $_.Exception
                    }
                }
            }
        }
    }
}

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
param(
    [ValidateSet("PSGallery", "CFS")]
    [string]$PSRepository = "PSGallery"
)

if ($PSRepository -eq "CFS" -and -not (Get-PSResourceRepository -Name CFS -ErrorAction SilentlyContinue)) {
    Register-PSResourceRepository -Name CFS -Uri "https://pkgs.dev.azure.com/powershell/PowerShell/_packaging/PowerShellGalleryMirror/nuget/v3/index.json"
}

# NOTE: Due to a bug in Install-PSResource with upstream feeds, we have to
# request an exact version. Otherwise, if a newer version is available in the
# upstream feed, it will fail to install any version at all.
Install-PSResource -Verbose -TrustRepository -RequiredResource  @{
    "Az.Accounts" = @{
        version = "4.0.2"
        repository = $PSRepository
      }
    "Az.Storage" = @{
        version = "8.1.0"
        repository = $PSRepository
    }
}

# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

<#
.SYNOPSIS
Downloads to packages from PowerShell Gallery which are missing from the Azure DevOps Artifacts feed.

.PARAMETER AzureDevOpsPAT
PAT for the username used for authenticating to the Azure DevOps Artifacts feed.

.PARAMETER Destination
Path to the folder where the packages should be stored for uploading to Azure DevOps Artifacts feed.

#>
function SyncGalleryToAzArtifacts {
    param(
        [Parameter(Mandatory = $true)] [string] $AzDevOpsFeedUserName,
        [Parameter(Mandatory = $true)] [string] $AzDevOpsPAT,
        [Parameter(Mandatory = $true)] [string] $Destination
    )

    $csproj = [xml] (Get-Content 'src/Modules/PSGalleryModules.csproj')
    $packages = @($csproj.Project.ItemGroup.PackageReference | ForEach-Object { [ordered] @{Name = $_.Include; Version = $_.Version }})

    $galleryPackages = @()
    $azArtifactsPackages = @()
    $modulesToUpdate = @()

    $galleryUrl = 'https://www.powershellgallery.com/api/v2/'
    $azArtifactsUrl = 'https://mscodehub.pkgs.visualstudio.com/_packaging/pscore-release/nuget/v2'

    $azDevOpsCreds = [pscredential]::new($AzDevOpsFeedUserName, (ConvertTo-SecureString -String $AzDevOpsPAT -AsPlainText -Force))

    foreach ($package in $packages) {
        try {
            # Get module from gallery
            $foundPackageOnGallery = Find-Package -ProviderName NuGet -Source $galleryUrl -AllVersions -Name $package.Name -Force -AllowPreReleaseVersion | Sort-Object -Property Version -Descending | Select-Object -First 1
            Write-Verbose -Verbose "Found module $($package.Name) - $($foundPackageOnGallery.Version) in gallery"
            $galleryPackages += $foundPackageOnGallery
        } catch {
            if ($_.FullyQualifiedErrorId -eq 'NoMatchFoundForCriteria,Microsoft.PowerShell.PackageManagement.Cmdlets.FindPackage') {
                # Log and ignore failure is required version is not found on gallery.
                Write-Warning "Module not found on gallery $($package.Name) - $($package.Version)"
            }
            else {
                Write-Error $_
            }
        }

        try {
            # Get module from Az Artifacts
            # There seems to be a bug in the feed with RequiredVersion matching. Adding workaround with post filtering.
            # Issue: https://github.com/OneGet/oneget/issues/397
            $foundPackageOnAz = Find-Package -ProviderName NuGet -Source $azArtifactsUrl -AllVersions -Name $package.Name -Force -Credential $azDevOpsCreds -AllowPreReleaseVersion | Sort-Object -Property Version -Descending | Select-Object -First 1
            Write-Verbose -Verbose "Found module $($package.Name) - $($foundPackageOnAz.Version) in azArtifacts"
            $azArtifactsPackages += $foundPackageOnAz
        } catch {
            if ($_.FullyQualifiedErrorId -eq 'NoMatchFoundForCriteria,Microsoft.PowerShell.PackageManagement.Cmdlets.FindPackage') {
                # Log and add the module to update list.
                Write-Verbose -Verbose "Az Artifacts Module needs update to - $($package.Name) - $($package.Version)"
                $modulesToUpdate += $package
            }
            else {
                Write-Error $_
            }
        }

        # Check if Az package version is less that gallery version
        if ($foundPackageOnAz.Version -lt $foundPackageOnGallery.Version) {
            Write-Verbose -Verbose "Module needs to be updated $($package.Name) - $($foundPackageOnGallery.Version)"
            $modulesToUpdate += $foundPackageOnGallery
        } elseif ($foundPackageOnGallery.Version -lt $foundPackageOnAz.Version) {
            Write-Warning "Newer version found on Az Artifacts - $($foundPackageOnAz.Name) - $($foundPackageOnAz.Version)"
        } else {
            Write-Verbose -Verbose "Module is in sync - $($package.Name)"
        }
    }

    "Gallery Packages:`n"
    $galleryPackages

    "Az Artifacts Packages:`n"
    $azArtifactsPackages

    "Modules to update:`n"
    $modulesToUpdate

    foreach ($package in $modulesToUpdate) {
        Save-Package -Provider NuGet -Source $galleryUrl -Name $package.Name -RequiredVersion $package.Version -Path $Destination
    }

    # Remove dependent packages downloaded by Save-Module if there are already present in AzArtifacts feed.
    try {
        Register-PackageSource -Name local -Location $Destination -ProviderName NuGet -Force
        $packageNamesToKeep = @()
        $savedPackages = Find-Package -Source local -AllVersions -AllowPreReleaseVersion

        foreach($package in $savedPackages) {
            $foundMatch = $azArtifactsPackages | Where-Object { $_.Name -eq $package.Name -and $_.Version -eq $package.Version }

            if(-not $foundMatch) {
                Write-Verbose "Keeping package $($package.PackageFileName)"
                $packageNamesToKeep += $package.PackageFilename
            }
        }

        Remove-Item -Path $Destination -Exclude $packageNamesToKeep -Recurse -Force
    }
    finally {
        Unregister-PackageSource -Name local -Force -ErrorAction SilentlyContinue
    }

}

Export-ModuleMember -Function 'SyncGalleryToAzArtifacts'

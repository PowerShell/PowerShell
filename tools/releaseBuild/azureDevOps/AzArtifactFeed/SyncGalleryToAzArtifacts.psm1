# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

function SyncGalleryToAzArtifacts {
    param(
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

    $azDevOpsCreds = [pscredential]::new($env:AzDevOpsUserName, (ConvertTo-SecureString -String $AzDevOpsPAT -AsPlainText -Force))

    foreach ($p in $packages) {
        try {
            # Get module from gallery
            $foundPackageOnGallery = Find-Package -ProviderName NuGet -Source $galleryUrl -AllVersions -Name $p.Name -Force -AllowPreReleaseVersion | Sort-Object -Property Version -Descending | Select-Object -First 1
            Write-Verbose -Verbose "Found module $($p.Name) - $($foundPackageOnGallery.Version) in gallery"
            $galleryPackages += $foundPackageOnGallery
        } catch {
            if ($_.FullyQualifiedErrorId -eq 'NoMatchFoundForCriteria,Microsoft.PowerShell.PackageManagement.Cmdlets.FindPackage') {
                # Log and ignore failure is required version is not found on gallery.
                Write-Warning "Module not found on gallery $($p.Name) - $($p.Version)"
            }
            else {
                Write-Error $_
            }
        }

        try {
            # Get module from Az Artifacts
            # There seems to be a bug in the feed with RequiredVersion matching. Adding workaround with post filtering.
            # Issue: https://github.com/OneGet/oneget/issues/397
            $foundPackageOnAz = Find-Package -ProviderName NuGet -Source $azArtifactsUrl -AllVersions -Name $p.Name -Force -Credential $azDevOpsCreds -AllowPreReleaseVersion | Sort-Object -Property Version -Descending | Select-Object -First 1
            Write-Verbose -Verbose "Found module $($p.Name) - $($foundPackageOnAz.Version) in azArtifacts"
            $azArtifactsPackages += $foundPackageOnAz
        } catch {
            if ($_.FullyQualifiedErrorId -eq 'NoMatchFoundForCriteria,Microsoft.PowerShell.PackageManagement.Cmdlets.FindPackage') {
                # Log and add the module to update list.
                Write-Verbose -Verbose "Az Artifacts Module needs update to - $($p.Name) - $($p.Version)"
                $modulesToUpdate += $p
            }
            else {
                Write-Error $_
            }
        }

        # Check if Az package version is less that gallery version
        if ($foundPackageOnAz.Version -lt $foundPackageOnGallery.Version) {
            Write-Verbose -Verbose "Module needs to be updated $($p.Name) - $($foundPackageOnGallery.Version)"
            $modulesToUpdate += $foundPackageOnGallery
        } elseif ($foundPackageOnGallery.Version -lt $foundPackageOnAz.Version) {
            Write-Warning "Newer version found on Az Artifacts - $($foundPackageOnAz.Name) - $($foundPackageOnAz.Version)"
        } else {
            Write-Verbose -Verbose "Module is in sync - $($p.Name)"
        }
    }

    Write-Verbose -Verbose "Gallery Packages:"
    $galleryPackages

    Write-Verbose -Verbose "Az Artifacts Packages:"
    $azArtifactsPackages

    Write-Verbose -Verbose "Modules to update:"
    $modulesToUpdate

    foreach ($p in $modulesToUpdate) {
        Save-Package -Provider NuGet -Source $galleryUrl -Name $p.Name -RequiredVersion $p.Version -Path $Destination
    }
}

Export-ModuleMember -Function 'SyncGalleryToAzArtifacts'

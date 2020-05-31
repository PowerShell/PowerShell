# Copyright (c) Microsoft Corporation.
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
            $foundPackageOnGallery = Find-Package -ProviderName NuGet -Source $galleryUrl -AllVersions -Name $package.Name -Force -AllowPreReleaseVersion | SortPackage | Select-Object -First 1
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
            $foundPackageOnAz = Find-Package -ProviderName NuGet -Source $azArtifactsUrl -AllVersions -Name $package.Name -Force -Credential $azDevOpsCreds -AllowPreReleaseVersion | SortPackage | Select-Object -First 1
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
        $pkgOnAzVersion = [semver]::new($foundPackageOnAz.Version)
        $pkgOnGalleryVersion = [semver]::new($foundPackageOnGallery.Version)

        if ($pkgOnAzVersion -lt $pkgOnGalleryVersion) {
            Write-Verbose -Verbose "Module needs to be updated $($package.Name) - $($foundPackageOnGallery.Version)"
            $modulesToUpdate += $foundPackageOnGallery
        } elseif ($pkgOnGalleryVersion -lt $pkgOnAzVersion) {
            Write-Warning "Newer version found on Az Artifacts - $($foundPackageOnAz.Name) - $($foundPackageOnAz.Version)"
        } else {
            Write-Verbose -Verbose "Module is in sync - $($package.Name)"
        }
    }

    "`nGallery Packages:"
    $galleryPackages

    "`nAz Artifacts Packages:`n"
    $azArtifactsPackages

    "`nModules to update:`n"
    $modulesToUpdate

    foreach ($package in $modulesToUpdate) {
        Write-Verbose -Verbose "Saving package $($package.Name) - $($package.Version)"
        Save-Package -Provider NuGet -Source $galleryUrl -Name $package.Name -RequiredVersion $package.Version -Path $Destination
    }

    if ($modulesToUpdate.Length -gt 0)
    {
        # Remove dependent packages downloaded by Save-Package if there are already present in AzArtifacts feed.
        try {
            $null = Register-PackageSource -Name local -Location $Destination -ProviderName NuGet -Force
            $packageNamesToKeep = @()
            $savedPackages = Find-Package -Source local -AllVersions -AllowPreReleaseVersion

            Write-Verbose -Verbose "Saved packages:"
            $savedPackages | Out-String | Write-Verbose -Verbose

            foreach($package in $savedPackages) {
                $pkgVersion = NormalizeVersion -version $package.Version
                $foundMatch = $azArtifactsPackages | Where-Object { $_.Name -eq $package.Name -and (NormalizeVersion -version $_.Version) -eq $pkgVersion }

                if(-not $foundMatch) {
                    Write-Verbose "Keeping package $($package.PackageFileName)" -Verbose
                    $packageNamesToKeep += "{0}*.nupkg" -f $package.Name
                }
            }

            if ($packageNamesToKeep.Length -gt 0) {
                ## Removing only if we do have some packages to keep,
                ## otherwise the '$Destination' folder will be removed.
                Remove-Item -Path $Destination -Exclude $packageNamesToKeep -Recurse -Force -Verbose
            }

            Write-Verbose -Verbose "Packages kept for upload"
            Get-ChildItem $Destination | Out-String | Write-Verbose -Verbose
        }
        finally {
            Unregister-PackageSource -Name local -Force -ErrorAction SilentlyContinue
        }
    }
}

Function SortPackage {
    param(
        [Parameter(ValueFromPipeline = $true)]
        [Microsoft.PackageManagement.Packaging.SoftwareIdentity[]]
        $packages
    )

    Begin {
        $allPackages = @()
    }

    Process {
        $allPackages += $packages
    }

    End {
        $versions = $allPackages.Version |
        ForEach-Object { ($_ -split '-')[0] } |
        Select-Object -Unique |
        Sort-Object -Descending -Property Version

        foreach ($version in $versions) {
            $exactMatch = $allPackages | Where-Object {
                Write-Verbose "testing $($_.version) -eq $version"
                $_.version -eq $version
            }

            if ($exactMatch) {
                Write-Output $exactMatch
            }

            $allPackages | Where-Object {
                $_.version -like "${version}-*"
            } | Sort-Object -Descending -Property Version | Write-Output
        }
    }
}


function NormalizeVersion {
    param ([string] $version)

    $sVer = if ($version -match "(\d+.\d+.\d+).0") {
        $Matches[1]
    } elseif ($version -match "^\d+.\d+$") {
        # Two digit versions are stored as three digit versions
        "$version.0"
    } else {
        $version
    }

    $sVer
}

Export-ModuleMember -Function 'SyncGalleryToAzArtifacts', 'SortPackage'

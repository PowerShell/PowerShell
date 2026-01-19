<#
This function gets info from pmc's derived list of all repositories and from mapping.json (which contains info on just the repositories powershell publishes packages to, their package formats, etc)
to create a list of repositories PowerShell cares about along with repository Ids, repository full Urls and associated package that will be published to it.
#>
function Get-MappedRepositoryIds {
    param(
        [Parameter(Mandatory)]
        [hashtable]
        $Mapping,

        [Parameter(Mandatory)]
        $RepoList,

        # LTS is not consider a package in this context.
        # LTS is just another package name.
        [Parameter(Mandatory)]
        [ValidateSet('stable', 'preview')]
        $Channel
    )

    $mappedReposUsedByPwsh = @()
    foreach ($package in $Mapping.Packages)
    {
        Write-Verbose "package: $package"
        $packageChannel = $package.channel
        if (!$packageChannel) {
            $packageChannel = 'all'
        }

        Write-Verbose "package channel: $packageChannel"
        if ($packageChannel -eq 'all' -or $packageChannel -eq $Channel)
        {
            $repoIds = [System.Collections.Generic.List[string]]::new()
            $packageFormat = $package.PackageFormat
            Write-Verbose "package format: $packageFormat" -Verbose
            $extension = [System.io.path]::GetExtension($packageFormat)
            $packageType = $extension -replace '^\.'

            if ($package.distribution.count -gt 1) {
                throw "Package $($package | out-string) has more than one Distribution."
            }

            foreach ($distribution in $package.distribution)
            {
                $urlGlob = $package.url
                switch ($packageType)
                {
                    'deb' {
                        $urlGlob = $urlGlob + '-apt'
                    }
                    'rpm' {
                        $urlGlob = $urlGlob + '-yum'
                    }
                    default {
                        throw "Unknown package type: $packageType"
                    }
                }

                Write-Verbose "---Finding repo id for: $urlGlob---" -Verbose
                $repos = $RepoList | Where-Object { $_.name -eq $urlGlob }

                if ($repos.id) {
                    Write-Verbose "Found repo id: $($repos.id)" -Verbose
                    $repoIds.AddRange(([string[]]$repos.id))
                }
                else {
                    Write-Failure "Could not find repo for $urlGlob"
                }

                if ($repoIds.Count -gt 0) {
                    $mappedReposUsedByPwsh += ($package + @{ "RepoId" = $repoIds.ToArray() })
                }
            }
        }
    }

    Write-Verbose -Verbose "mapped repos length: $($mappedReposUsedByPwsh.Length)"
    return $mappedReposUsedByPwsh
}

<#
This function creates package objects for the packages to be published,
with the package name (ie package name format resolve with channel based PackageName and pwsh version), repoId, distribution and package path.
#>
function Get-PackageObjects() {
    param(
        [Parameter(Mandatory)]
        [psobject[]]
        $RepoObjects,

        [Parameter(Mandatory)]
        [string]
        $ReleaseVersion,

        [Parameter(Mandatory)]
        [string[]]
        $PackageName
    )

    $packages = @()

    foreach ($pkg in $RepoObjects)
    {
        if ($pkg.RepoId.count -gt 1) {
            throw "Package $($pkg.name) has more than one repo id."
        }

        if ($pkg.Distribution.count -gt 1) {
            throw "Package $($pkg.name) has more than one Distribution."
        }

        $pkgRepo = $pkg.RepoId | Select-Object -First 1
        $pkgDistribution = $pkg.Distribution | Select-Object -First 1

        foreach ($name in $PackageName) {
            $pkgName = $pkg.PackageFormat.Replace('PACKAGE_NAME', $name).Replace('POWERSHELL_RELEASE', $ReleaseVersion)

            if ($pkgName.EndsWith('.rpm')) {
                $pkgName = $pkgName.Replace($ReleaseVersion, $ReleaseVersion.Replace('-', '_'))
            }

            $packagePath = "$pwshPackagesFolder/$pkgName"
            $packagePathExists = Test-Path -Path $packagePath
            if (!$packagePathExists)
            {
                throw "package path $packagePath does not exist"
            }

            Write-Verbose "Creating package info object for package '$pkgName' for repo '$pkgRepo'"
            $packages += @{
                PackagePath = $packagePath
                PackageName = $pkgName
                RepoId = $pkgRepo
                Distribution = $pkgDistribution
            }

            Write-Verbose -Verbose "package info obj: Name: $pkgName RepoId: $pkgRepo Distribution: $pkgDistribution PackagePath: $packagePath"
        }
    }

    Write-Verbose -Verbose "count of packages objects: $($packages.Length)"
    return $packages
}

<#
This function stages, uploads and publishes the powershell packages to their associated repositories in PMC.
#>
function Publish-PackageToPMC() {
    param(
        [Parameter(Mandatory)]
        [pscustomobject[]]
        $PackageObject,

        [Parameter(Mandatory)]
        [string]
        $ConfigPath,

        [Parameter(Mandatory)]
        [bool]
        $SkipPublish
    )

    # Don't fail outright when an error occurs, but instead pool them until
    # after attempting to publish every package. That way we can choose to
    # proceed for a partial failure.
    $errorMessage = [System.Collections.Generic.List[string]]::new()
    foreach ($finalPackage in $PackageObject)
    {
        Write-Verbose "---Staging package: $($finalPackage.PackageName)---" -Verbose
        $packagePath = $finalPackage.PackagePath
        $pkgRepo = $finalPackage.RepoId

        $extension = [System.io.path]::GetExtension($packagePath)
        $packageType = $extension -replace '^\.'
        Write-Verbose "packageType: $packageType" -Verbose

        $packageListJson = pmc --config $ConfigPath package $packageType list --file $packagePath
        $list = $packageListJson | ConvertFrom-Json

        $packageId = @()
        if ($list.count -ne 0)
        {
            Write-Verbose "Package '$packagePath' already exists, skipping upload" -Verbose
            $packageId = $list.results.id | Select-Object -First 1
        }
        else {
            # PMC UPLOAD COMMAND
            Write-Verbose -Verbose "Uploading package, config: '$ConfigPath' package: '$packagePath'"
            $uploadResult = $null
            try {
                $uploadResult = pmc --config $ConfigPath package upload $packagePath --type $packageType
            }
            catch {
                $errorMessage.Add("Uploading package $($finalPackage.PackageName) to $pkgRepo failed. See errors above for details.")
                continue
            }

            $packageId = ($uploadResult | ConvertFrom-Json).id
        }

        Write-Verbose "Got package ID: '$packageId'" -Verbose
        $distribution = $finalPackage.Distribution | select-object -First 1
        Write-Verbose "distribution: $distribution" -Verbose

        if (!$SkipPublish)
        {
            Write-Verbose "---Publishing package: $($finalPackage.PackageName) to $pkgRepo---" -Verbose

            if (($packageType -ne 'rpm') -and ($packageType -ne 'deb'))
            {
                throw "Unsupported package type: $packageType"
                return 1
            }
            else {
                # PMC UPDATE COMMAND
                $rawUpdateResponse = $null
                try {
                    if ($packageType -eq 'rpm') {
                        $rawUpdateResponse = pmc --config $ConfigPath repo package update $pkgRepo --add-packages $packageId
                    } elseif ($packageType -eq 'deb') {
                        $rawUpdateResponse = pmc --config $ConfigPath repo package update $pkgRepo $distribution --add-packages $packageId
                    }
                }
                catch {
                    $errorMessage.Add("Invoking update for package $($finalPackage.PackageName) to $pkgRepo failed. See errors above for details.")
                    continue
                }

                $state = ($rawUpdateResponse | ConvertFrom-Json).state
                Write-Verbose -Verbose "update response state: $state"
                if ($state -ne 'completed') {
                    $errorMessage.Add("Publishing package $($finalPackage.PackageName) to $pkgRepo failed: $rawUpdateResponse")
                    continue
                }
            }

            # PMC PUBLISH COMMAND
            # The CLI outputs messages and JSON in the same stream, so we must sift through it for now
            # This is planned to be fixed with a switch in a later release
            Write-Verbose -Verbose ([pscustomobject]($package + @{
                PackageId = $packageId
            }))

            # At this point, the changes are staged and will eventually be publish.
            # Running publish, causes them to go live "immediately"
            $rawPublishResponse = $null
            try {
                $rawPublishResponse = pmc --config $ConfigPath repo publish $pkgRepo
            }
            catch {
                $errorMessage.Add("Invoking final publish for package $($finalPackage.PackageName) to $pkgRepo failed. See errors above for details.")
                continue
            }

            $publishState = ($rawPublishResponse | ConvertFrom-Json).state
            Write-Verbose -Verbose "publish response state: $publishState"
            if ($publishState -ne 'completed') {
                $errorMessage.Add("Final publishing of package $($finalPackage.PackageName) to $pkgRepo failed: $rawPublishResponse")
                continue
            }
        } else {
            Write-Verbose -Verbose "Skipping Uploading package --config-file '$ConfigPath' package add '$packagePath' --repoID '$pkgRepo'"
        }
    }

    if ($errorMessage) {
        throw $errorMessage -join [Environment]::NewLine
    }
}

if ($null -eq $env:MAPPING_FILE)
{
    Write-Verbose -Verbose "MAPPING_FILE variable didn't get passed correctly"
    return 1
}

if ($null -eq $env:PWSH_PACKAGES_TARGZIP)
{
    Write-Verbose -Verbose "PWSH_PACKAGES_TARGZIP variable didn't get passed correctly"
    return 1
}

if ($null -eq $env:PMC_METADATA)
{
    Write-Verbose -Verbose "PMC_METADATA variable didn't get passed correctly"
    return 1
}

try {
    Write-Verbose -Verbose "Downloading files"
    Invoke-WebRequest -Uri $env:MAPPING_FILE -OutFile mapping.json
    Invoke-WebRequest -Uri $env:PWSH_PACKAGES_TARGZIP -OutFile packages.tar.gz
    Invoke-WebRequest -Uri $env:PMC_METADATA -OutFile pmcMetadata.json

    # create variables to those paths and test them
    $mappingFilePath = Join-Path "/package/unarchive/" -ChildPath "mapping.json"
    $mappingFilePathExists = Test-Path $mappingFilePath
    if (!$mappingFilePathExists)
    {
        Write-Verbose -Verbose "mapping.json expected at $mappingFilePath does not exist"
        return 1
    }

    $packagesTarPath = Join-Path -Path "/package/unarchive/" -ChildPath "packages.tar.gz"
    $packagesTarPathExists = Test-Path $packagesTarPath
    if (!$packagesTarPathExists)
    {
        Write-Verbose -Verbose "packages.tar.gz expected at $packagesTarPath does not exist"
        return 1
    }

    # Extract files from 'packages.tar.gz'
    Write-Verbose -Verbose "---Extracting files from packages.tar.gz---"
    $pwshPackagesFolder = Join-Path -Path "/package/unarchive/" -ChildPath "packages"
    New-Item -Path $pwshPackagesFolder -ItemType Directory
    tar -xzvf $packagesTarPath -C $pwshPackagesFolder --force-local
    Get-ChildItem $pwshPackagesFolder -Recurse

    $metadataFilePath = Join-Path -Path "/package/unarchive/" -ChildPath "pmcMetadata.json"
    $metadataFilePathExists = Test-Path $metadataFilePath
    if (!$metadataFilePathExists)
    {
        Write-Verbose -Verbose "pmcMetadata.json expected at $metadataFilePath does not exist"
        return 1
    }

    # files in the extracted Run dir
    $configPath = Join-Path '/package/unarchive/Run' -ChildPath 'settings.toml'
    $configPathExists = Test-Path -Path $configPath
    if (!$configPathExists)
    {
        Write-Verbose -Verbose "settings.toml expected at $configPath does not exist"
        return 1
    }

    $pythonDlFolder = Join-Path '/package/unarchive/Run' -ChildPath 'python_dl'
    $pyPathExists = Test-Path -Path $pythonDlFolder
    if (!$pyPathExists)
    {
        Write-Verbose -Verbose "python_dl expected at $pythonDlFolder does not exist"
        return 1
    }

    Write-Verbose -Verbose "Installing pmc-cli"
    pip install --upgrade pip
    pip --version --verbose
    pip install /package/unarchive/Run/python_dl/*.whl

    # Get metadata
    $channel = ""
    $packageNames = @()
    $metadataContent = Get-Content -Path $metadataFilePath | ConvertFrom-Json
    $releaseVersion = $metadataContent.ReleaseTag.TrimStart('v')
    $skipPublish = $metadataContent.SkipPublish
    $lts = $metadataContent.LTS

    # Check if this is a rebuild version (e.g., 7.4.13-rebuild.5)
    $isRebuild = $releaseVersion -match '-rebuild\.'

    if ($releaseVersion.Contains('-')) {
        $channel = 'preview'
        $packageNames = @('powershell-preview')
    }
    else {
        $channel = 'stable'
        $packageNames = @('powershell')
    }

    # Only add LTS package if not a rebuild branch
    if ($lts -and -not $isRebuild) {
        $packageNames += @('powershell-lts')
    }

    Write-Verbose -Verbose "---Getting repository list---"
    $rawResponse = pmc --config $configPath repo list --limit 800
    $response = $rawResponse | ConvertFrom-Json
    $limit = $($response.limit)
    $count = $($response.count)
    Write-Verbose -Verbose "'pmc repo list' limit is: $limit and count is: $count"
    $repoList = $response.results

    Write-Verbose -Verbose "---Getting package info---"


    Write-Verbose "Reading mapping file from '$mappingFilePath'" -Verbose
    $mapping = Get-Content -Raw -LiteralPath $mappingFilePath | ConvertFrom-Json -AsHashtable
    $mappedReposUsedByPwsh = Get-MappedRepositoryIds -Mapping $mapping -RepoList $repoList -Channel $channel
    $packageObjects = Get-PackageObjects -RepoObjects $mappedReposUsedByPwsh -PackageName $packageNames -ReleaseVersion $releaseVersion
    Write-Verbose -Verbose "skip publish $skipPublish"
    Publish-PackageToPMC -PackageObject $packageObjects -ConfigPath $configPath -SkipPublish $skipPublish
}
catch {
    Write-Error -ErrorAction Stop $_.Exception.Message
    return 1
}

return 0

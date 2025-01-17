function Get-Info {
    param(
        [string]
        $PkgName
    )

    Write-Verbose -Verbose "pkg name: $pkgName"
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
    $releaseVersion = $metadataContent.ReleaseTag
    $skipPublish = $metadataContent.SkipPublish
    $lts = $metadataContent.LTS
    Write-Verbose -Verbose "skip publish: $skipPublish" #TODO: remove
    Get-Info -PkgName "testing"

    if ($releaseVersion.Contains('-')) {
        $channel = 'preview'
        $packageNames = @('powershell-preview')
    }
    else {
        $channel = 'stable'
        $packageNames = @('powershell')
    }

    if ($lts) {
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
    # BEGIN Get-PackageInfo()
    # for the packages powershell publishes to, resolve it's mapping with the directory of repos and basically map our packages to their actual repo Ids
    Write-Verbose "Reading mapping file from '$mappingFilePath'" -Verbose
    $mapping = Get-Content -Raw -LiteralPath $mappingFilePath | ConvertFrom-Json -AsHashtable
    $mappedReposUsedByPwsh = @()
    foreach ($package in $mapping.Packages)
    {
        Write-Verbose "package: $package"
        $packageChannel = $package.channel
        if (!$packageChannel) {
            $packageChannel = 'all'
        }

        Write-Verbose "package channel: $packageChannel"
        if ($packageChannel -eq 'all' -or $packageChannel -eq $channel)
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
                $repos = $repoList | Where-Object { $_.name -eq $urlGlob }

                if ($repos.id) {
                    Write-Verbose "Found repo id: $($repos.id)" -Verbose
                    $repoIds.AddRange(([string[]]$repos.id)) #tbh seems like a package should only have 1 repo Id
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
    # END of Get-PackageInfo()

    # BEGIN New-RepoPackageObject()
    $repoPackageObjects = @()
    foreach ($pkg in $mappedReposUsedByPwsh)
    {
        if ($pkg.RepoId.count -gt 1) {
            throw "Package $($pkg.name) has more than one repo id."
        }

        if ($pkg.Distribution.count -gt 1) {
            throw "Package $($pkg.name) has more than one Distribution."
        }

        $pkgRepo = $pkg.RepoId | Select-Object -First 1
        $pkgDistribution = $pkg.Distribution | Select-Object -First 1

        foreach ($name in $packageNames) {
            $pkgName = $pkg.PackageFormat.Replace('PACKAGE_NAME', $name).Replace('POWERSHELL_RELEASE', $releaseVersion)
            Write-Verbose "Creating info object for package '$pkgName' for repo '$pkgRepo'"
            $result = [pscustomobject]@{
                PackageName  = $pkgName
                RepoId       = $pkgRepo
                Distribution = $pkgDistribution
            }

            Write-Verbose $result -Verbose
            $repoPackageObjects += $result
        }
    }
    # END of New-RepoPackageObject() - I think this and the method before can be combined

    Write-Verbose -Verbose "count of repoPackageObjects: $($repoPackageObjects.Length)"

    # BEGIN Publish-PackageFromBlob()
    $packages = @()

    foreach ($pkgObj in $repoPackageObjects)
    {
        # RHEL and CentOS packages have a tweak in the name...
        if ($pkgObj.PackageName.EndsWith('.rpm')) { #TODO: can I do this in the 1 condensed method
            $pkgObjName = $pkgObj.PackageName.Replace($releaseVersion, $releaseVersion.Replace('-', '_'))
        }

        $packagePath = "$pwshPackagesFolder/$pkgObjName"

        $packages += @{
            PackagePath = $packagePath
            PackageName = $pkgObjName
            RepoId = $pkgObj.RepoId
            Distribution = $pkgObj.Distribution
        }
    }

    # end block of Publish-PackageFromBlob()

    # Don't fail outright when an error occurs, but instead pool them until
    # after attempting to publish every package. That way we can choose to
    # proceed for a partial failure.
    $errorMessage = [System.Collections.Generic.List[string]]::new()
    foreach ($finalPackage in $packages)
    {
        Write-Verbose "---Staging package: $($finalPackage.PackageName)---" -Verbose
        $packagePath = $finalPackage.PackagePath
        $pkgRepo = $finalPackage.RepoId

        #TODO: should process if/else here- needed or nah?
        $extension = [System.io.path]::GetExtension($packagePath)
        $packageType = $extension -replace '^\.'
        Write-Verbose "packageType: $packageType" -Verbose

        $packageListJson = pmc --config $configPath package $packageType list --file $packagePath
        $list = $packageListJson | ConvertFrom-Json

        $packageId = @()
        if ($list.count -ne 0)
        {
            Write-Verbose "Package '$packagePath' already exists, skipping upload" -Verbose
            $packageId = $list.results.id | Select-Object -First 1
        }
        else {
            # PMC UPDATE COMMAND
            Write-Verbose -Verbose "Uploading package, config: '$configPath' package: '$packagePath'"
            $uploadResult = $null
            try {
                $uploadResult = pmc --config $configPath package upload $packagePath --type $packageType
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

        if (!$skipPublish)
        {
            Write-Verbose "---Publishing package: $($finalPackage.PackageName) to $pkgRepo---" -Verbose

            if (($packageType -ne 'rpm') -and ($packageType -ne 'deb'))
            {
                throw "Unsupported package type: $packageType"
                return 1
            }
            else {
                # UPDATE PMC COMMAND
                $rawUpdateResponse = $null
                try {
                    if ($packageType -eq 'rpm') {
                        $rawUpdateResponse = pmc --config $configPath repo package update $pkgRepo --add-packages $packageId
                    } elseif ($packageType -eq 'deb') {
                        $rawUpdateResponse = pmc --config $configPath repo package update $pkgRepo $distribution --add-packages $packageId
                    }
                }
                catch {
                    $errorMessage.Add("Invoking update for package $($finalPackage.PackageName) to $pkgRepo failed. See errors above for details.")
                    continue
                }

                $state = $rawUpdateResponse.state
                if ($state -ne 'Completed') {
                    $errorMessage.Add("Publishing package $($finalPackage.PackageName) to $pkgRepo failed: $rawUpdateResponse")
                    continue
                }
            }

            # PUBLISH PMC COMMAND
            # The CLI outputs messages and JSON in the same stream, so we must sift through it for now
            # This is planned to be fixed with a switch in a later release
            # TODO: Anam, figure out if this was fixed, and if so lets use this switch
            Write-Verbose -Verbose ([pscustomobject]($package + @{
                PackageId = $packageId
            }))

            # At this point, the changes are staged and will eventually be publish.
            # Running publish, causes them to go live "immediately"
            try {
                pmc --config $configPath repo publish $pkgRepo
            }
            catch {
                $errorMessage.Add("Running final publish for package $($finalPackage.PackageName) to $pkgRepo failed. See errors above for details.")
                continue
            }
        } else {
            Write-Verbose -Verbose "Skipping Uploading package --config-file '$configPath' package add '$packagePath' --repoID '$pkgRepo'"
        }
    }

    if ($errorMessage) {
        throw $errorMessage -join [Environment]::NewLine
    }
}
catch {
    Write-Error -ErrorAction Stop $_.Exception.Message
    return 1
}

return 0

if ($null -eq $env:MAPPING_FILE)
{
    Write-Verbose -Verbose "MAPPING_FILE variable didn't get passed correctly"
    return 1
}

if ($null -eq $env:REPO_LIST_FILE)
{
    Write-Verbose -Verbose "REPO_LIST_FILE variable didn't get passed correctly"
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
    Invoke-WebRequest -Uri $env:REPO_LIST_FILE -OutFile repoList.json
    Invoke-WebRequest -Uri $env:PWSH_PACKAGES_TARGZIP -OutFile packages.tar.gz
    Invoke-WebRequest -Uri $env:PMC_METADATA -OutFile pmcMetadata.json

    $settingsFile = Join-Path '/package/unarchive/Run' -ChildPath 'settings.toml'
    $settingsFileExists = Test-Path -Path $settingsFile
    Write-Verbose -Verbose "settings.toml exists: $settingsFileExists"
    $pythonDlFolder = Join-Path '/package/unarchive/Run' -ChildPath 'python_dl'
    $pyPathExists = Test-Path -Path $pythonDlFolder
    Write-Verbose -Verbose "python_dl folder path exists: $pyPathExists"

    Write-Verbose -Verbose "doing gci"
    Get-ChildItem "/package" -Recurse
    Write-Verbose -Verbose "Installing pmc-cli"
    crane version
    # python.exe -m pip install --upgrade pip
    pip install --upgrade pip
    pip --version --verbose
    pip install /package/unarchive/Run/python_dl/*.whl

    Write-Verbose -Verbose "Test pmc-cli"

    pmc -d -c $settingsFile repo list --name "azurelinux-3.0-prod-ms-oss-x86_64-yum" || exit 1

    $metadataFilePath = Join-Path -Path "/package/unarchive/" -ChildPath "pmcMetadata.json"
    $metadataContent = Get-Content -Path $metadataFilePath | ConvertFrom-Json
    $releaseVersion = $metadataContent.ReleaseTag



    # $repoListFilePath = Join-Path -Path "/package/unarchive/" -ChildPath "repoList.json"
    # $repoListPathExists = Test-Path $repoListFilePath
    # if (!$repoListPathExists)
    # {
    #     Write-Verbose -Verbose "repoList.json expected at $repoListFilePath does not exist"
    #     return 1
    # }

    # $repoList = Get-RepoList -RepoConfigPath $configPath -OutFile $repoListPath #TODO: seems to require repoclient cli tool

    # # Get-PackageInfo()
    # $mappingFilePath = Join-Path -Path "/package/unarchive/" -ChildPath "mapping.json"
    # $mappingPathExists = Test-Path $mappingFilePath
    # if (!$mappingPathExists)
    # {
    #     Write-Verbose -Verbose "mapping.json expected at $mappingFilePath does not exist"
    #     return 1
    # }

    # $mapping = Get-Content -Raw -LiteralPath $mappingFilePath | ConvertFrom-Json -AsHashtable
    # foreach ($package in $mapping.Packages)
    # {
    #     Write-Verbose "package: $package"
    #     $packageChannel = $package.channel
    #     if (!$packageChannel) {
    #         $packageChannel = 'all'
    #     }

    #     Write-Verbose "package channel: $packageChannel"
    #     if ($packageChannel -eq 'all' -or $packageChannel -eq $Channel) {
    #         $repoIds = [System.Collections.Generic.List[string]]::new()
    #         $packageFormat = $package.PackageFormat
    #         Write-Verbose "package format: $packageFormat" -Verbose
    #         $extension = [System.io.path]::GetExtension($packageFormat)
    #         $packageType = $extension -replace '^\.'

    #         if ($package.distribution.count -gt 1) {
    #             throw "Package $($package | out-string) has more than one Distribution."
    #         }

    #         foreach ($distribution in $package.distribution) {
    #             $urlGlob = $package.url
    #             switch ($packageType) {
    #                 'deb' {
    #                     $urlGlob = $urlGlob + '-apt'
    #                 }
    #                 'rpm' {
    #                     $urlGlob = $urlGlob + '-yum'
    #                 }
    #                 default {
    #                     throw "Unknown package type: $packageType"
    #                 }
    #             }

    #             Write-Verbose "---Finding repo id for: $urlGlob---" -Verbose

    #             $repos = $repoList | Where-Object { $_.name -eq $urlGlob }

    #             if ($repos.id) {
    #                 Write-Verbose "Found repo id: $($repos.id)" -Verbose
    #                 $repoIds.AddRange(([string[]]$repos.id))
    #             }
    #             else {
    #                 Write-Failure "Could not find repo for $urlGlob"
    #             }
    #         }

    #         if ($repoIds.Count -le 0) {
    #             Write-Verbose -Verbose "no repoIds found that match our packages"
    #             # Write-Output ($package + @{ "RepoId" = $repoIds.ToArray() })
    #         }

    #         foreach ($pkg in $Package) {
    #             if ($pkg.RepoId.count -gt 1) {
    #                 throw "Package $($pkg.name) has more than one repo id."
    #             }
    #             if ($pkg.Distribution.count -gt 1) {
    #                 throw "Package $($pkg.name) has more than one Distribution."
    #             }
    #             $repo = $pkg.RepoId | Select-Object -First 1
    #             $distribution = $pkg.Distribution | Select-Object -First 1
    #             foreach ($name in $PackageName) {
    #                     $pkgName = $pkg.PackageFormat.Replace('PACKAGE_NAME', $name).Replace('POWERSHELL_RELEASE', $ReleaseVersion)
    #                     Write-Verbose "Creating info object for package '$pkgName' for repo '$repo'"
    #                 $result = [pscustomobject]@{
    #                     PackageName  = $pkgName
    #                     RepoId       = $repo
    #                     Distribution = $distribution
    #                 }
    #                 Write-Verbose $result -Verbose
    #                 Write-Output $result
    #             }
    #         }
    #     }
    # }


}
catch {
    Write-Error -ErrorAction Stop $_.Exception.Message
    return 1
}

return 0

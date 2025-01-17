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

    # create variables to those paths and test them
    $repoListFilePath = Join-Path -Path "/package/unarchive/" -ChildPath "repoList.json"
    $repoListPathExists = Test-Path $repoListFilePath
    if (!$repoListPathExists)
    {
        Write-Verbose -Verbose "repoList.json expected at $repoListFilePath does not exist"
        return 1
    }

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

    Write-Verbose -Verbose "Test pmc-cli"
    pmc -d -c $configPath repo list --accessible || exit 1

    # Get metadata
    $channel = ""
    $packageNames = @()
    $metadataContent = Get-Content -Path $metadataFilePath | ConvertFrom-Json
    $releaseVersion = $metadataContent.ReleaseTag

    if ($releaseVersion.Contains('-')) {
        $channel = 'preview'
        $packageNames = @('powershell-preview')
    }
    else {
        $channel = 'stable'
        $packageNames = @('powershell')
    }

    # TODO: figure out where this comes in
    # if ($LTS) {
    #     $packageNames += @('powershell-lts')
    # }

    Write-Verbose -Verbose "---Getting repository list---"
    # tbh, I don't know if repoList.json is really needed- confused what the purpose of that is
    $rawResponse = pmc --config $configPath repo list --limit 8 #TODO: actually 800
    $response = $rawResponse | ConvertFrom-Json
    $limit = $($response.limit)
    $count = $($response.count)
    Write-Verbose -Verbose "limit is: $limit and count is: $count"
    $repoList = $response.results

    Write-Verbose -Verbose "---Getting package info---" #TODO rename once I get what it really does
    # start of method
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

            if ($package.distribution.count -gt 1) { # TODO: do we want to remove distribution stuff rn? ask.
                throw "Package $($package | out-string) has more than one Distribution."
            }

            foreach ($distribution in package.distribution)
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
                    $mappedReposUsedByPwsh += ($package + @{ "RepoId" = $repoIds.ToArray() }) #TODO: adjust output type for this as needed
                }
            }
        }
    }

    Write-Verbose -Verbose "mapped repos length: $($mappedReposUsedByPwsh.Length)"
    # END of Get-PackageInfo()

    #TODO: Anam: have some sort of object to contain output of the for loop

    # # Publish the packages based on the mapping file
    # $publishedPackages = Get-PackageInfo -MappingFile $MappingFilePath -RepoList $repoList -Channel $channel |
    # New-RepoPackageObject -ReleaseVersion $releaseVersion -PackageName $packageNames |
    # Publish-PackageFromBlob -PwshVersion $releaseVersion -ConfigPath $configPath -BlobUriPrefix "$BlobBaseUri/$BlobFolderName" -WhatIf:$WhatIfPreference


}
catch {
    Write-Error -ErrorAction Stop $_.Exception.Message
    return 1
}

return 0

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
    Write-Verbose -Verbose "mapping.json file: $env:MAPPING_FILE"
    Write-Verbose -Verbose "pwsh packages.tar.gz file: $env:PWSH_PACKAGES_TARGZIP"
    Write-Verbose -Verbose "pmcMetadata.json file: $env:PMC_METADATA"

    Write-Verbose -Verbose "Downloading files"
    Invoke-WebRequest -Uri $env:MAPPING_FILE -OutFile mapping.json
    Invoke-WebRequest -Uri $env:PWSH_PACKAGES_TARGZIP -OutFile packages.tar.gz
    Invoke-WebRequest -Uri $env:PMC_METADATA -OutFile pmcMetadata.json

    $settingsFile = Join-Path '/package/unarchive' -ChildPath 'settings.toml'
    $settingsFileExists = Test-Path -Path $settingsFile
    Write-Verbose -Verbose "settings.toml exists: $settingsFileExists"
    $pythonDlFolder = Join-Path '/package/unarchive' -ChildPath 'python_dl'
    $pyPathExists = Test-Path -Path $pythonDlFolder
    Write-Verbose -Verbose "python_dl folder path exists: $pyPathExists"

    Write-Verbose -Verbose "Installing pmc-cli"
    python -m pip install --upgrade pip
    pip --version --verbose
    pip install /package/unarchive/python_dl/*.whl

    Write-Verbose -Verbose "Test pmc-cli"

    which pmc
    pmc -d -c $settingsFile repo list --name "azurelinux-3.0-prod-ms-oss-x86_64-yum" || exit 1

}
catch {
    Write-Error -ErrorAction Stop $_.Exception.Message
    return 1
}

return 0

# ensure SAS variables were passed in
if ($null -eq $env:RELEASE_TAG)
{
    Write-Verbose -Verbose "RELEASE_TAG variable didn't get passed correctly"
    return 1
}

if ($null -eq $env:AAD_CLIENT_ID)
{
    Write-Verbose -Verbose "AAD_CLIENT_ID variable didn't get passed correctly"
    return 1
}

if ($null -eq $env:BLOB_FOLDER_NAME)
{
    Write-Verbose -Verbose "BLOB_FOLDER_NAME variable didn't get passed correctly"
    return 1
}

if ($null -eq $env:LTS)
{
    Write-Verbose -Verbose "LTS variable didn't get passed correctly"
    return 1
}

if ($null -eq $env:FOR_PRODUCTION)
{
    Write-Verbose -Verbose "FOR_PRODUCTION variable didn't get parsed properly"
    return 1
}

if ($null -eq $env:SKIP_PUBLISH)
{
    Write-Verbose -Verbose "SKIP_PUBLISH variable didn't get parsed properly"
    return 1
}

if ($null -eq $env:MAPPING_FILE_PATH)
{
    Write-Verbose -Verbose "MAPPING_FILE_PATH variable didn't get parsed properly"
    return 1
}

try {
    Write-Verbose -Verbose "ReleaseTag: $env:RELEASE_TAG"
    Write-Verbose -Verbose "AAD_Client_ID: $env:AAD_CLIENT_ID"
    Write-Verbose -Verbose "Blob_folder_name: $env:BLOB_FOLDER_NAME"
    Write-Verbose -Verbose "LTS: $env:LTS"
    Write-Verbose -Verbose "for_production: $env:FOR_PRODUCTION"
    Write-Verbose -Verbose "Skip_publish: $env:SKIP_PUBLISH"
    Write-Verbose -Verbose "mapping file: $env:MAPPING_FILE_PATH"
}
catch {
    Write-Error -ErrorAction Stop $_.Exception.Message
    return 1
}

return 0

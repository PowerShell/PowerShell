# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
param ( [switch]$Force, [switch]$UseExistingMsi )

$script:Constants =  @{
    AccountName   = 'PowerShell'
    ProjectName   = 'powershell-f975h'
    TestImageName = "remotetestimage"
    MsiName       = "PSCore.msi"
    Token         = "" # in this particular use we don't need a token
}

############
### MAIN ###
############

#### DOCKER OPS #####
# is docker installed?
$dockerExe = get-command docker -ea silentlycontinue
if ( $dockerExe.name -ne "docker.exe" ) {
    throw "Cannot find docker, is it installed?"
}
# Check to see if we already have an image, and if so
# delete it if -Force was used, otherwise throw and exit
$TestImage = docker images $Constants.TestImageName --format '{{.Repository}}'
if ( $TestImage -eq $Constants.TestImageName) 
{
    if ( $Force ) 
    {
        docker rmi $Constants.TestImageName
    }
    else
    {
        throw ("{0} already exists, use '-Force' to remove" -f $Constants.TestImageName)
    }
}
# check again - there could be some permission problems
$TestImage = docker images $Constants.TestImageName --format '{{.Repository}}'
if ( $TestImage -eq $Constants.TestImageName) 
{
    throw ("'{0}' still exists, giving up" -f $Constants.TestImageName)
}

#### MSI CHECKS ####
# check to see if the MSI is present
$MsiExists = test-path $Constants.MsiName
$msg = "{0} exists, use -Force to remove or -UseExistingMsi to use" -f $Constants.MsiName
if ( $MsiExists -and ! ($force -or $useExistingMsi)) 
{
    throw $msg
}

# remove the msi
if ( $MsiExists -and $Force -and ! $UseExistingMsi ) 
{
    Remove-Item -force $Constants.MsiName
    $MsiExists = $false
}

# a couple of checks before downloading or using the existing one
# if the msi exists and -UseExistingMsi is present, we'll use the
# one we found
if ( ! $MsiExists -and $UseExistingMsi )
{
    throw ("{0} does not exist" -f $Constants.MsiName)
}
elseif ( $MsiExists -and ! $UseExistingMsi ) 
{
    throw $msg
}

# last check before bulding the image
if ( ! (test-path $Constants.MsiName) )
{
    throw ("{0} does not exist, giving up" -f $Constants.MsiName)
}

# collect the builds and select the last one
Docker build --tag $Constants.TestImageName .

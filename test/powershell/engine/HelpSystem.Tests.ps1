#
# Validates Get-Help for cmdlets in Microsoft.PowerShell.Core.

function RunTestCase
{
    param ([string]$tag = "CI")

    $moduleName = "Microsoft.PowerShell.Core"

    if ($IsWindows)
    {
        if ($tag -eq "CI")
        {
            $helpContentPath = join-path $PSScriptRoot "HelpContent"
            $helpFiles = @(Get-ChildItem "$helpContentPath\*" -ea SilentlyContinue)

            if ($helpFiles.Count -eq 0)
            {
                throw "unable to find help content at '$helpContentPath'"
            }
            Update-Help -Module $moduleName -SourcePath $helpContentPath -Force -ErrorAction Stop -Verbose
        }

        else
        {
            Update-Help -Module $moduleName -Force -Verbose -ErrorAction Stop
        }
    }

    $cmdlets = get-command -module $moduleName

    $cmdletsToSkip = @(
        "Get-PSHostProcessInfo",
        "Out-Default",
        "Register-ArgumentCompleter"
    )

    foreach ($cmdletName in $cmdlets)
    {
        if ($cmdletsToSkip -notcontains $cmdletName)
        {
            It "Validate -Description and -Examples sections in help content. Run 'Get-help -name $cmdletName'" {

                $help = get-help -name $cmdletName
                $help.Description | Out-String | Should Match $cmdletName
                $help.Examples | Out-String | Should Match $cmdletName
            }

            if ($tag -eq "CI")
            {
                # For a CI test run, we are only interested in validating one cmdlet to ensure that
                # get-help <cmdletName> works.
                break
            }
        }
    }
}

Describe "Validate that get-help <cmdletName> works" -Tags "CI" {

    RunTestCase -tag "CI"
}

Describe "Validate Get-Help for all cmdlets in 'Microsoft.PowerShell.Core'" -Tags "Feature" {

    RunTestCase -tag "Feature"
}

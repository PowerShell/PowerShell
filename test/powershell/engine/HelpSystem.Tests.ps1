#
# Validates Get-Help for cmdlets in Microsoft.PowerShell.Core.

function RunTestCase
{
    param ([string]$tag = "CI")

    $cmdlets = get-command -module "Microsoft.PowerShell.Core"

    $cmdletsToSkip = @(
        "Get-PSHostProcessInfo",
        "Out-Default",
        "Register-ArgumentCompleter"
    )

    $testCaseExecuted = $false

    foreach ($cmdletName in $cmdlets)
    {
        if ($cmdletsToSkip -notcontains $cmdletName)
        {
            It "Validate -Description and -Examples sections in help content. Run 'Get-help -name $cmdletName'" {

                $help = get-help -name $cmdletName
                $help.Description | Out-String | Should Match $cmdletName
                $help.Examples | Out-String | Should Match $cmdletName
                $testCaseExecuted = $true
            }

            if (($tag -eq "CI") -and $testCaseExecuted)
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

    RunTestCase

}

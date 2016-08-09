Describe 'Basic help system tests' -Tags "CI" {

    Context "Validate help content for cmdlets in Microsof.PowerShell.Core" {

        $cmdlets = get-command -module "Microsoft.PowerShell.Core"
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
            }
        }
    }
}

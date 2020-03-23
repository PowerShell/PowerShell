# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Cmdlet declaration statement" -Tags "CI" {
    $testData = @(
        @{ Name = 'Verify non-cmdlet formatted names are allowed';
           Script = '
                function foo
                {
                    [CmdletBinding()]
                    param()

                    $a
                }' },
                @{ Name = 'Valid cmdlet names case 1';
                Script = '
                function foo
                {
                    [CmdletBinding()]
                    param()

                    $a
                }' },
                @{ Name = 'Valid cmdlet names case 2';
                Script = '
                function get-foo
                {
                    [CmdletBinding()]
                    param()

                    $a
                }' },
                @{ Name = 'Valid cmdlet names case 3';
                Script = '
                {
                    [CmdletBinding()]
                    param()

                    $a
                }' },
                @{
                Name ='Using parameter annotation in script cmdlets';
                Script = '
                function get-foo
                {
                    param([Parameter(mandatory=$true)]$a)
                    $a
                }'},
                @{
                Name = 'Cmdlet declaration parameter: should process';
                Script = '
                function get-foo
                {
                    [CmdletBinding(SupportsShouldProcess=$true)]
                    param([Parameter(mandatory=$true)]$a)
                    $a
                }' },
                @{
                Name = 'Cmdlet declaration parameter: confirm impact';
                Script = '
                function get-foo
                {
                    [CmdletBinding(SupportsShouldProcess=$true, ConfirmImpact="low")]
                    param([Parameter(mandatory=$true)]$a)
                    $a
                }'},
                @{
                Name = 'Cmdlet declaration parameter: defaultparametersetname';
                Script = '
                function get-foo
                {
                    [CmdletBinding(SupportsShouldProcess=$true, ConfirmImpact="low", defaultparametersetname="set1")]
                    param([Parameter(mandatory=$true)]$a)
                    $a
                }'},
                @{
                Name = 'cmdlet declaration within as cmdlet';
                Script = '
                function get-foo {
                    [CmdletBinding()]
                    param()

	            function get-bar
                    {
                        param([Parameter(mandatory=$true)]$a)
                        $a
                    }
                }'}
                @{
                Name = 'cmdlet declaration within scriptblock';
                Script = '
                {
                    param([Parameter(mandatory=$true)]$a)
                    $a
                }'},
                @{
                Name = 'cmdlet declaration within scriptblock 2';
                Script = '
                {
                    param([Parameter(mandatory=$true)]$a)
                    $a
                }'},
                @{
                Name = 'cmdlet declaration within scriptblock 3';
                Script = '
                {
                    [CmdletBinding(SupportsShouldProcess=$true)]
                    param([Parameter(mandatory=$true)]$a)
                    $a
                }'})

        It '<Name>' -TestCases $testData {
            param($Name, $script)

            $syntaxerrors = $null

            $null = [system.management.automation.psparser]::tokenize($script, [ref] $syntaxerrors)

            #Error should not be reported
            $syntaxerrors.Count | Should -Be 0
        }
}


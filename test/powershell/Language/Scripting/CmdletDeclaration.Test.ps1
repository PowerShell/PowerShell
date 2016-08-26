#  <Test>
#    <summary>Cmdlet declaration statement</summary>
#  </Test>

Describe "Cmdlet declaration statement" -Tags "CI" {

        It 'Verify non-cmdlet formatted names are allowed' {

            $syntaxerrors = $null
            $script = '
                function foo
                {
                    [CmdletBinding()]
                    param()

                    $a
                }'

            $null = [system.management.automation.psparser]::tokenize($script, [ref] $syntaxerrors)

            #Error should not be reported
            $syntaxerrors.Count | Should Be 0
        }

        It 'Valid cmdlet names case 1' {

            $syntaxerrors = $null
            $script = '
                function get-foo
                {
                    [CmdletBinding()]
                    param()

                    $a
                }'

            $null = [system.management.automation.psparser]::tokenize($script, [ref] $syntaxerrors)

            #Error should not be reported
            $syntaxerrors.Count | Should Be 0
        }

        It 'Valid cmdlet names case 2' {            

            $syntaxerrors = $null
            $script = '
                {
                    [CmdletBinding()]
                    param()

                    $a
                }'

            $null = [system.management.automation.psparser]::tokenize($script, [ref] $syntaxerrors)

            #Error should not be reported
            $syntaxerrors.Count | Should Be 0
        }

        It 'Valid cmdlet names case 3' {

            $syntaxerrors = $null
            $script = '
                {
                    [CmdletBinding()]
                    param()

                    $a
                }'

            $null = [system.management.automation.psparser]::tokenize($script, [ref] $syntaxerrors)

            #Error should not be reported
            $syntaxerrors.Count | Should Be 0
        }

        It 'Using parameter annotation in script cmdlets' {

            $syntaxerrors = $null
            $script = '
                function get-foo
                {
                    param([Parameter(mandatory=$true)]$a)
                    $a
                }'

            $null = [system.management.automation.psparser]::tokenize($script, [ref] $syntaxerrors)

            #Error should not be reported
            $syntaxerrors.Count | Should Be 0
        }

        It 'Cmdlet declaration parameter: should process' {
            $syntaxerrors = $null

            $script = '
                function get-foo
                {
                    [CmdletBinding(SupportsShouldProcess=$true)]
                    param([Parameter(mandatory=$true)]$a)
                    $a
                }'

            $null = [system.management.automation.psparser]::tokenize($script, [ref] $syntaxerrors)

            #Error should not be reported
            $syntaxerrors.Count | Should Be 0
        }

        It 'Cmdlet declaration parameter: confirm impact' {

            $syntaxerrors = $null

            $script = '
                function get-foo
                {
                    [CmdletBinding(SupportsShouldProcess=$true, ConfirmImpact="low")]
                    param([Parameter(mandatory=$true)]$a)
                    $a
                }'

            $null = [system.management.automation.psparser]::tokenize($script, [ref] $syntaxerrors)

            #Error should not be reported
            $syntaxerrors.Count | Should Be 0
        }

        It 'Cmdlet declaration parameter: defaultparametersetname' {

            $syntaxerrors = $null
            $script = '
                function get-foo
                {
                    [CmdletBinding(SupportsShouldProcess=$true, ConfirmImpact="low", defaultparametersetname="set1")]
                    param([Parameter(mandatory=$true)]$a)
                    $a
                }'

            $null = [system.management.automation.psparser]::tokenize($script, [ref] $syntaxerrors)

            #Error should not be reported
            $syntaxerrors.Count | Should Be 0
        }

        It 'cmdlet declaration within as cmdlet' {

            $syntaxerrors = $null

            $script = '
                function get-foo {
                    [CmdletBinding()]
                    param()

	            function get-bar
                    {
                        param([Parameter(mandatory=$true)]$a)
                        $a
                    }
                }'

            $null = [system.management.automation.psparser]::tokenize($script, [ref] $syntaxerrors)

            #Error should not be reported
            $syntaxerrors.Count | Should Be 0
        }

        It 'cmdlet declaration within scripblock' {

            $syntaxerrors = $null
            $script = '
                {
                    param([Parameter(mandatory=$true)]$a)
                    $a
                }'

            $null = [system.management.automation.psparser]::tokenize($script, [ref] $syntaxerrors)

            #Error should not be reported
            $syntaxerrors.Count | Should Be 0 
        }

        It 'cmdlet declaration within scripblock 2' {

            $syntaxerrors = $null

            $script = '
                {
                    param([Parameter(mandatory=$true)]$a)
                    $a
                }'

            $null = [system.management.automation.psparser]::tokenize($script, [ref] $syntaxerrors)

            #Error should not be reported
            $syntaxerrors.Count | Should Be 0
        }

        It 'cmdlet declaration within scripblock 3' {
        
            $syntaxerrors = $null

            $script = '
                {
                    [CmdletBinding(SupportsShouldProcess=$true)]
                    param([Parameter(mandatory=$true)]$a)
                    $a
                }'

            $null = [system.management.automation.psparser]::tokenize($script, [ref] $syntaxerrors)

            #Error should not be reported
            $syntaxerrors.Count | Should Be 0
        }
}


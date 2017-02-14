Describe "ScriptBlock.GetNewClosure()" -tags "CI" {
    
    BeforeAll {
        function SimpleFunction_GetNewClosure
        {
            param([ValidateNotNull()] $Name)

            & { 'OK' }.GetNewClosure()
        }

        function ScriptCmdlet_GetNewClosure
        {
            [CmdletBinding()]
            param(
                [Parameter()]
                [ValidateNotNullOrEmpty()]
                [string] $Name = "",

                [Parameter()]
                [ValidateRange(1,3)]
                [int] $Value = 4
            )

            & { $Value; $Name }.GetNewClosure()
        }
    }

    It "Parameter attributes should not get evaluated again in GetNewClosure - SimpleFunction" {
        SimpleFunction_GetNewClosure | Should Be "OK"
    }

    It "Parameter attributes should not get evaluated again in GetNewClosure - ScriptCmdlet" {
        $result = ScriptCmdlet_GetNewClosure
        $result.Count | Should Be 2
        $result[0] | Should Be 4
        $result[1] | Should Be ""
    }
}


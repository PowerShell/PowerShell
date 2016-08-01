Describe "MetadataTests2 (admin\monad\tests\monad\src\engine\core\MetadataTests2.cs)" -Tags "CI" {
    BeforeAll {
        function Test-Validateargumentsattribute 
        {
            param (
            [Parameter(Position = 0, ValueFromPipeline = $true)]
            [string[]] $Param0
            )

            BEGIN {
                if(!($Param0 -is [string[]]))
                {
                     throw ([MetadataException]::new("Didn't get an array of strings."))
                }

                if($Param0[0] -ne "valid")
                {
                    throw ([MetadataException]::new("invalid"))
                }

                foreach ($thispath in $Param0)
                {
                    return $thispath
                } 
            }
        }
    }

    It "Vrerify that ValidateArgumentsAttribute allows an argument though if Validate returns true" {
        $results = Test-Validateargumentsattribute valid
        $results |Should Be "valid"
    }
}
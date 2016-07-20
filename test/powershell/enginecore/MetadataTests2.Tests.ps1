Describe "MetadataTests2 (admin\monad\tests\monad\src\engine\core\MetadataTests2.cs)" {
    BeforeAll {
        $functionDefinitionFile = Join-Path -Path $TestDrive -ChildPath "functionDefinition.ps1"  
        $functionDefinition = @'
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
'@

        $functionDefinition > $functionDefinitionFile 

        $PowerShell = [powershell]::Create()
        function ExecuteCommand {
            param ([string]$command)
            try {
                $PowerShell.AddScript(". $functionDefinitionFile").Invoke()
                $PowerShell.AddScript($command).Invoke()
            }
            finally {
                $PowerShell.Commands.Clear()
            }
        }      
    }

    AfterEach {
        $PowerShell.Commands.Clear()
    }

    It "Vrerify that ValidateArgumentsAttribute allows an argument though if Validate returns true.(line 367)" {
        $results = ExecuteCommand "Test-Validateargumentsattribute valid"
        $results |Should Be "valid"
    }
}
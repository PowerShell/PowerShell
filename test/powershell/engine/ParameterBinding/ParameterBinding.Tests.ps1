Describe "Parameter Binding Tests" -Tags "CI" {
    It "Should throw a parameter binding exception when two parameters have the same position" {
        function test-PositionalBinding1 {
            [CmdletBinding()]
            param (
            [Parameter(Position = 0)] [int]$Parameter1 = 0,
            [Parameter(Position = 0)] [int]$Parameter2 = 0
            )

            Process {
                return $true
            }
        }

        try
        {
            test-PositionalBinding1 1
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be "AmbiguousPositionalParameterNoName,test-PositionalBinding1"
        }
    }

    It "a mandatory parameter can't be passed a null if it doesn't have AllowNullAttribute" {
        function test-allownullattributes {
            [CmdletBinding()]
            param (
            [string]$Parameter1 = "default1",
            [Parameter(Mandatory = $true)] [string]$Parameter2 = "default2",
            [Parameter(Mandatory = $true)] [string]$Parameter3 = "default3",
            [AllowNull()] [int]$Parameter4 = 0,
            [AllowEmptyString()][int]$Parameter5 = 0,
            [Parameter(Mandatory = $true)] [int]$ShowMe = 0
            )

            Process {
                switch ( $ShowMe )
                {
                    1 { 
                        return $Parameter1
                        break
                        }
                    2 { 
                        return $Parameter2
                        break
                        }
                    3 { 
                        return $Parameter3
                        break
                        }
                    4 { 
                        return $Parameter4
                        break
                        }
                    5 { 
                        return $Parameter5
                        break
                        }
                }
            }
        }

        try
        {
            test-allownullattributes -Parameter2 1 -Parameter3 $null -ShowMe 1
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be "ParameterArgumentValidationErrorEmptyStringNotAllowed,test-allownullattributes"
            $_.CategoryInfo | Should match "ParameterBindingValidationException"
            $_.Exception.Message | should match "Parameter3"
        }
    }

    It "can't pass an argument that looks like a boolean parameter to a named string parameter" {
        function test-namedwithboolishargument {
            [CmdletBinding()]
            param (
            [bool] $Parameter1 = $false,
            [Parameter(Position = 0)] [string]$Parameter2 = ""
            )

            Process {
                return $Parameter2
            }
        }

        try
        {
            test-namedwithboolishargument -Parameter2 -Parameter1
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be "MissingArgument,test-namedwithboolishargument"
            $_.CategoryInfo | Should match "ParameterBindingException"
            $_.Exception.Message | should match "Parameter2"
        }       
    }

    It "Verify that a SwitchParameter's IsPresent member is false if the parameter is not specified" {
        function test-singleswitchparameter {
            [CmdletBinding()]
            param (
            [switch]$Parameter1
            )

            Process {
                return $Parameter1.IsPresent
            }
        }

        $result = test-singleswitchparameter
        $result | Should Be $false
    }

    It "Verify that a bool parameter returns proper value" {
        function test-singleboolparameter {
            [CmdletBinding()]
            param (
            [bool]$Parameter1 = $false
            )

            Process {
                return $Parameter1
            }
        }

        $result1 = test-singleboolparameter
        $result1 | Should Be $false

        $result2 = test-singleboolparameter -Parameter1:1
        $result2 | Should Be $true
    }

    It "Should throw a exception when passing a string that can't be parsed by Int" {
        function test-singleintparameter {
            [CmdletBinding()]
            param (
            [int]$Parameter1 = 0
            )

            Process {
                return $Parameter1
            }
        }

        try
        {
            test-singleintparameter -Parameter1 'dookie'
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be "ParameterArgumentTransformationError,test-singleintparameter"
            $_.CategoryInfo | Should match "ParameterBindingArgumentTransformationException"
            $_.Exception.Message | should match "Input string was not in a correct format"
            $_.Exception.Message | should match "Parameter1"
        }
    }

    It "Verify that WhatIf is available when SupportShouldProcess is true" {
        function test-supportsshouldprocess2 {
            [CmdletBinding(SupportsShouldProcess = $true)]
            Param ()

            Process {
                return 1     
            }
        }

        $result = test-supportsshouldprocess2 -Whatif
        $result | Should Be 1
    }

    It "Verify that ValueFromPipeline takes precedence over ValueFromPipelineByPropertyName without type coercion" {
        function test-bindingorder2 {
            [CmdletBinding()]
            param (
            [Parameter(ValueFromPipeline = $true, ParameterSetName = "one")] [string]$Parameter1 = "",
            [Parameter(ValueFromPipelineByPropertyName = $true, ParameterSetName = "two")] [int]$Length = 0
            )

            Process {
                return "$Parameter1 - $Length"
            }
        }

        $result = '0123' | test-bindingorder2
        $result | Should Be "0123 - 0"
    }

    It "Verify that a ScriptBlock object can be delay-bound to a parameter of type FileInfo with pipeline input" {
        function test-scriptblockbindingfrompipeline {
            [CmdletBinding()]
            param (
            [Parameter(ValueFromPipeline = $true)] [System.IO.FileInfo]$Parameter1
            )

            Process {
                return $Parameter1.Name
            }
        }
        $testFile = Join-Path $TestDrive -ChildPath "testfile.txt"
        New-Item -Path $testFile -ItemType file -Force
        $result = Get-Item $testFile | test-scriptblockbindingfrompipeline -Parameter1 {$_}
        $result | Should Be "testfile.txt"
    }

    It "Verify that a dynamic parameter named WhatIf doesn't conflict if SupportsShouldProcess is false" {
        function test-dynamicparameters3 {
            [CmdletBinding(SupportsShouldProcess = $false)]
            param (
            [Parameter(ParameterSetName = "one")] [int]$Parameter1 = 0,
            [Parameter(ParameterSetName = "two")] [int]$Parameter2 = 0,
            [int]$WhatIf = 0
            )
        }

        { test-dynamicparameters3 -Parameter1 1 } | Should Not Throw
    }

    It "Verify that an int can be bound to a parameter of type Array" {
        function test-collectionbinding1 {
            [CmdletBinding()]
            param (
            [array]$Parameter1 = "",
            [int[]]$Parameter2 = ""           
            )

            Process {
                $result = ""
                if($Parameter1 -ne $null)
                {
                    $result += " P1"
                    foreach ($object in $Parameter1)
                    {
                        $result = $result + ":" + $object.GetType().Name + "," + $object
                    }
                }
                if($Parameter2 -ne $null)
                {
                    $result += " P2"
                    foreach ($object in $Parameter2)
                    {
                        $result = $result + ":" + $object.GetType().Name + "," + $object
                    }
                }
                return $result.Trim()
            }
        }

        $result = test-collectionbinding1 -Parameter1 1
        $result | Should Be "P1:Int32,1"
    }

    It "Verify that a dynamic parameter and an alias can't have the same name" {
        function test-nameconflicts6 {
            [CmdletBinding()]
            param (
            [Alias("Parameter2")]
            [int]$Parameter1 = 0,
            [int]$Parameter2 = 0 
            )         
        }

        try
        {
            test-nameconflicts6 -Parameter2 1
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be "ParameterNameConflictsWithAlias"
            $_.CategoryInfo | Should match "MetadataException"
            $_.Exception.Message | should match "Parameter1"
            $_.Exception.Message | should match "Parameter2"
        }      
    }
}

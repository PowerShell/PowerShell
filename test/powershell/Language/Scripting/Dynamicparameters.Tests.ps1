# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Dynamic parameter support in script cmdlets." -Tags "CI" {
    BeforeAll {
        Class MyTestParameter {
            [parameter(ParameterSetName = 'pset1', position = 0, mandatory = 1)]
            [string] $name
        }

        function foo-bar {
            [CmdletBinding()]
            param($path)

            dynamicparam {
                if ($PSBoundParameters["path"] -contains "abc") {
                    $attributes = [System.Management.Automation.ParameterAttribute]::New()
                    $attributes.ParameterSetName = 'pset1'
                    $attributes.Mandatory = $false

                    $attributeCollection = [System.Collections.ObjectModel.Collection``1[System.Attribute]]::new()
                    $attributeCollection.Add($attributes)

                    $dynParam1 = [System.Management.Automation.RuntimeDefinedParameter]::new("dp1", [Int32], $attributeCollection)
                    if ($PSBoundParameters["path"] -contains "realtime") {
                        return $dynParam1
                    } else {
                        $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()
                        $paramDictionary.Add("dp1", $dynParam1)

                        return $paramDictionary
                    }
                } elseif ($PSBoundParameters["path"] -contains "class") {
                    $paramDictionary = [MyTestParameter]::new()
                    return $paramDictionary
                }

                $paramDictionary = $null
                return $null
            }

            begin {
                if($dp1){
                    return $dp1
                }
                if (($null -ne $paramDictionary) -and ($paramDictionary -is [MyTestParameter]) ) {
                    $paramDictionary.name
                } elseif ($null -ne $paramDictionary) {
                    if ($null -ne $paramDictionary.dp1.Value) {
                        $paramDictionary.dp1.Value
                    } else {
                        "dynamic parameters not passed"
                    }
                } else {
                    "no dynamic parameters"
                }
            }

            process {}
            end {}
        }
    }

    It "The dynamic parameter is enabled and bound" {
        foo-bar -path abc -dp1 42 | Should -Be 42
    }

    It "When the dynamic parameter is not available, and raises an error when specified" {
        { foo-bar -path def -dp1 42 } | Should -Throw -ErrorId "NamedParameterNotFound,foo-bar"
    }

    It "No dynamic parameter shouldn't cause an errr " {
        foo-bar -path def  | Should -BeExactly 'no dynamic parameters'
    }

    It "Not specifying dynamic parameter shouldn't cause an error" {
        foo-bar -path abc | Should -BeExactly 'dynamic parameters not passed'
    }

    It "Parameter is defined in Class" {
        foo-bar -path class -Name "myName" | Should -BeExactly 'myName'
    }
    It "Parameter is bound without dictionary"{
        foo-bar -path abc,realtime -dp1 42 | Should -Be 42
    }
}

# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Basic Function Provider Tests" -Tags "CI" {
    BeforeAll {
        $existingFunction = "existingFunction"
        $nonExistingFunction = "nonExistingFunction"
        $text = "Hello World!"
        $functionValue = { return $text }
        $restoreLocation = Get-Location
        Set-Location Function:
    }

    AfterAll {
        # Restore the previous location.
        Set-Location -Path $restoreLocation
    }

    Context "Validate Set-Item Cmdlet" {
        BeforeEach {
            Set-Item $existingFunction -Options "None" -Value $functionValue
        }

        AfterEach {
            Remove-Item $existingFunction -ErrorAction SilentlyContinue -Force
            Remove-Item $nonexistingFunction -ErrorAction SilentlyContinue -Force
        }

        It "Sets the new options in existing function" {
            $newOptions = "ReadOnly, AllScope"
            (Get-Item $existingFunction).Options | Should -be "None"
            Set-Item $existingFunction -Options $newOptions
            (Get-Item $existingFunction).Options | Should -be $newOptions
        }

        It "Sets the options and a value of type ScriptBlock for a new function" {
            $options = "ReadOnly"
            Set-Item $nonExistingFunction -Options $options -value $functionValue
            (Get-Item $nonExistingFunction).Options | Should -be $options
            (Get-Item $nonExistingFunction).ScriptBlock | Should -BeLike $functionValue
        }

        It "Removes existing function if Set-Item has no arguments beside function name" {
            Set-Item $existingFunction
            $existingFunction | Should -Not -Exist
        }

        It "Sets a value of type FunctionInfo for a new function" {
            Set-Item $nonExistingFunction -value (Get-Item $existingFunction)
            Invoke-Expression $nonExistingFunction | Should -Be $text
        }

        It "Sets a value of type String for a new function" {
            Set-Item $nonExistingFunction -value "return '$text' "
            Invoke-Expression $nonExistingFunction | Should -Be $text
        }

        It "Throws PSArgumentException when Set-Item is called with incorrect function value" {
            Set-Item $nonExistingFunction -Value 123  -errorvariable x -erroraction silentlycontinue
            $x.CategoryInfo | Should -Match "PSArgumentException"
        }
    }
}

# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Basic Function Provider Tests" -Tags "CI" {
    BeforeAll {
        $existingFunction = "prompt"
        $nonExistingFunction = "nonExistingFunction"
        $text = "Hello World!"
        $functionValue = { return $text }
        $restoreLocation = Get-Location
    }

    AfterAll {
        # Restore the previous location.
        Set-Location -Path $restoreLocation
    }

    Context "Validate Set-Item Cmdlet" {

        BeforeAll {
            Set-Location Function:
        }

        AfterEach {
            # Removes $nonExistingFunction in case it was added. 
            Set-Item $nonExistingFunction
            $nonExistingFunction | Should -Not -Exist
        }

        It "Sets the new options in existing function" {
            (Get-Item $existingFunction).Options | Should -be "None"
            Set-Item $existingFunction -Options "AllScope"
            (Get-Item $existingFunction).Options | Should -be "AllScope"
        }

        It "Sets the options and a value of type ScriptBlock for a new function" {
            Set-Item $nonExistingFunction -Options "AllScope" -value $functionValue
            (Get-Item $nonExistingFunction).Options | Should -be "AllScope"
            (Get-Item $nonExistingFunction).ScriptBlock | Should -BeLike $functionValue
        }

        It "Removes existing function if Set-Item has no arguments beside function name" {
            $existingFunction | Should -Exist
            Set-Item $existingFunction
            $existingFunction | Should -Not -Exist
        }

        It "Sets a value of type FunctionInfo for a new function" {
            Set-Item $nonExistingFunction -value $functionValue
            $tmpFunction = "tmpFunction"
            Set-Item $tmpFunction -value (Get-Item $nonExistingFunction)
            Invoke-Expression $tmpFunction | Should -Be $text
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
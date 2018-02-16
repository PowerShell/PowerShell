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
            (Get-Item $existingFunction).Options | Should -Be "None"
            Set-Item $existingFunction -Options $newOptions
            (Get-Item $existingFunction).Options | Should -Be $newOptions
        }

        It "Sets the options and a value of type ScriptBlock for a new function" {
            $options = "ReadOnly"
            Set-Item $nonExistingFunction -Options $options -Value $functionValue
            (Get-Item $nonExistingFunction).Options | Should -Be $options
            (Get-Item $nonExistingFunction).ScriptBlock | Should -BeLike $functionValue
        }

        It "Removes existing function if Set-Item has no arguments beside function name" {
            Set-Item $existingFunction
            $existingFunction | Should -Not -Exist
        }

        It "Sets a value of type FunctionInfo for a new function" {
            Set-Item $nonExistingFunction -Value (Get-Item $existingFunction)
            Invoke-Expression $nonExistingFunction | Should -Be $text
        }

        It "Sets a value of type String for a new function" {
            Set-Item $nonExistingFunction -Value "return '$text' "
            Invoke-Expression $nonExistingFunction | Should -Be $text
        }

        It "Throws PSArgumentException when Set-Item is called with incorrect function value" {
            Set-Item $nonExistingFunction -Value 123 -ErrorVariable x -ErrorAction silentlycontinue
            $x.FullyQualifiedErrorId | Should -Match "Argument,Microsoft.PowerShell.Commands.SetItemCommand"
        }
    }
}

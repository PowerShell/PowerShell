# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Basic Function Provider Tests" -Tags "CI" {
    BeforeAll {
        $existingFunction = "existingFunction"
        $nonExistingFunction = "nonExistingFunction"
        $text = "Hello World!"
        $functionValue = { return $text }
        $restoreLocation = Get-Location
        $newName = "renamedFunction"
        Set-Location Function:
    }

    AfterAll {
        Set-Location -Path $restoreLocation
    }

    BeforeEach {
        Set-Item $existingFunction -Options "None" -Value $functionValue
    }

    AfterEach {
        Remove-Item $existingFunction -ErrorAction SilentlyContinue -Force
        Remove-Item $nonExistingFunction -ErrorAction SilentlyContinue -Force
        Remove-Item $newName -ErrorAction SilentlyContinue -Force
    }

    Context "Validate Set-Item Cmdlet" {
        It "Sets the new options in existing function" {
            $newOptions = "ReadOnly, AllScope"
            (Get-Item $existingFunction).Options | Should -BeExactly "None"
            Set-Item $existingFunction -Options $newOptions
            (Get-Item $existingFunction).Options | Should -BeExactly $newOptions
        }

        It "Sets the options and a value of type ScriptBlock for a new function" {
            $options = "ReadOnly"
            Set-Item $nonExistingFunction -Options $options -Value $functionValue
            $getItemResult = Get-Item $nonExistingFunction
            $getItemResult.Options | Should -BeExactly $options
            $getItemResult.ScriptBlock | Should -BeExactly $functionValue
        }

        It "Removes existing function if Set-Item has no arguments beside function name" {
            Set-Item $existingFunction
            $existingFunction | Should -Not -Exist
        }

        It "Sets a value of type FunctionInfo for a new function" {
            Set-Item $nonExistingFunction -Value (Get-Item $existingFunction)
            Invoke-Expression $nonExistingFunction | Should -BeExactly $text
        }

        It "Sets a value of type String for a new function" {
            Set-Item $nonExistingFunction -Value "return '$text' "
            Invoke-Expression $nonExistingFunction | Should -BeExactly $text
        }

        It "Throws PSArgumentException when Set-Item is called with incorrect function value" {
            { Set-Item $nonExistingFunction -Value 123 -ErrorAction Stop } | Should -Throw -ErrorId "Argument,Microsoft.PowerShell.Commands.SetItemCommand"
        }
    }

    Context "Validate Get-Item Cmdlet" {
        It "Gets existing functions by name" {
            $getItemResult = Get-Item $existingFunction
            $getItemResult.Name | Should -BeExactly $existingFunction
            $getItemResult.Options | Should -BeExactly "None"
            $getItemResult.ScriptBlock | Should -BeExactly $functionValue
        }

        It "Matches regex with stars to the function names" {
            $getItemResult = Get-Item "ex*on"
            $getItemResult.Name | Should -BeExactly $existingFunction

            # Stars representing empty string.
            $getItemResult = Get-Item "*existingFunction*"
            $getItemResult.Name | Should -BeExactly $existingFunction

            # Finds 2 functions that match the regex.
            Set-Item $nonExistingFunction -Value $functionValue
            $getItemResults =  Get-Item "*Function"
            $getItemResults.Count | Should -BeGreaterThan 1
        }
    }

    Context "Validate Remove-Item Cmdlet" {
        It "Removes function" {
            Remove-Item $existingFunction
            { Get-Item $existingFunction -ErrorAction Stop } | Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.GetItemCommand"
        }

        It "Fails to remove not existing function" {
            { Remove-Item $nonExistingFunction -ErrorAction Stop } | Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.RemoveItemCommand"
        }
    }

    Context "Validate Rename-Item Cmdlet" {
        It "Renames existing function with None options" {
            Rename-Item $existingFunction -NewName $newName
            { Get-Item $existingFunction -ErrorAction Stop } | Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.GetItemCommand"
            (Get-Item $newName).Count | Should -BeExactly 1
        }

        It "Fails to rename not existing function" {
            { Rename-Item $nonExistingFunction -NewName $newName -ErrorAction Stop } | Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.RenameItemCommand"
        }

        It "Fails to rename function which is Constant" {
            Set-Item $nonExistingFunction -Options "Constant" -Value $functionValue
            { Rename-Item $nonExistingFunction -NewName $newName -ErrorAction Stop } | Should -Throw -ErrorId "CannotRenameFunction,Microsoft.PowerShell.Commands.RenameItemCommand"
        }

        It "Fails to rename function which is ReadOnly" {
            Set-Item $nonExistingFunction -Options "ReadOnly" -Value $functionValue
            { Rename-Item $nonExistingFunction -NewName $newName -ErrorAction Stop } | Should -Throw -ErrorId "CannotRenameFunction,Microsoft.PowerShell.Commands.RenameItemCommand"
        }

        It "Renames ReadOnly function when -Force parameter is on" {
            Set-Item $nonExistingFunction -Options "ReadOnly" -Value $functionValue
            Rename-Item $nonExistingFunction -NewName $newName -Force
            { Get-Item $nonExistingFunction -ErrorAction Stop } | Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.GetItemCommand"
            (Get-Item $newName).Count | Should -BeExactly 1
        }
    }
}

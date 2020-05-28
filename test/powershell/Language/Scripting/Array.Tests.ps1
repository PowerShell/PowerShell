# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "ArrayExpression Tests" -Tags "CI" {
    It "@([object[]](1,2,3)) should return a 3-element array of object[]" {
        $result = @([object[]](1,2,3))
        $result.GetType().FullName | Should -BeExactly "System.Object[]"
        $result.Length | Should -Be 3
    }

    It "@([int[]](1,2,3)) should return a 3-element array of object[]" {
        $result = @([int[]](1,2,3))
        $result.GetType().FullName | Should -BeExactly "System.Object[]"
        $result.Length | Should -Be 3
    }

    It "@([object[]]`$null) should return a 1-element(`$null) array of object[]" {
        $result = @([object[]]$null)
        $result.GetType().FullName | Should -BeExactly "System.Object[]"
        $result.Length | Should -Be 1
        $result[0] | Should -BeNullOrEmpty
    }

    It "@([int[]]`$null) should return a 1-element(`$null) array of object[]" {
        $result = @([int[]]$null)
        $result.GetType().FullName | Should -BeExactly "System.Object[]"
        $result.Length | Should -Be 1
        $result[0] | Should -BeNullOrEmpty
    }

    It "@([object[]][System.Management.Automation.Internal.AutomationNull]::Value) should return a 1-element(`$null) array of object[]" {
        $result = @([object[]][System.Management.Automation.Internal.AutomationNull]::Value)
        $result.GetType().FullName | Should -BeExactly "System.Object[]"
        $result.Length | Should -Be 1
        $result[0] | Should -BeNullOrEmpty
    }

    It "@([int[]][System.Management.Automation.Internal.AutomationNull]::Value) should return a 1-element(`$null) array of object[]" {
        $result = @([int[]][System.Management.Automation.Internal.AutomationNull]::Value)
        $result.GetType().FullName | Should -BeExactly "System.Object[]"
        $result.Length | Should -Be 1
        $result[0] | Should -BeNullOrEmpty
    }

    It "@(`$null) should return a 1-element(`$null) array of object[]" {
        $result = @($null)
        $result.GetType().FullName | Should -BeExactly "System.Object[]"
        $result.Length | Should -Be 1
        $result[0] | Should -BeNullOrEmpty
    }

    It "@([System.Management.Automation.Internal.AutomationNull]::Value) should return an empty array of object[]" {
        $result = @([System.Management.Automation.Internal.AutomationNull]::Value)
        $result.GetType().FullName | Should -BeExactly "System.Object[]"
        $result.Length | Should -Be 0
    }

    It "@([object[]]`$a) should return a new array" {
        $a = 1,2,3
        $result = @([object[]]$a)
        $result.GetType().FullName | Should -BeExactly "System.Object[]"
        $result.Length | Should -Be 3
    }

    It "@([int[]]`$a) should return a new array" {
        $a = 1,2,3
        $result = @([int[]]$a)
        $result.GetType().FullName | Should -BeExactly "System.Object[]"
        $result.Length | Should -Be 3
    }

    It "@([System.Collections.Generic.List[object]]`$null) should return a 1-element(`$null) array of object[]" {
        $result = @([System.Collections.Generic.List[object]]$null)
        $result.GetType().FullName | Should -BeExactly "System.Object[]"
        $result.Length | Should -Be 1
        $result[0] | Should -BeNullOrEmpty
    }

    It "@([void](New-Item)) should create file" {
        try {
            $testFile = Join-Path $TestDrive (New-Guid)
            $result = @([void](New-Item $testFile -ItemType File))
            ## file should be created
            $testFile | Should -Exist
            ## the array should be empty
            $result.Count | Should -Be 0
        } finally {
            Remove-Item $testFile -Force -ErrorAction SilentlyContinue
        }
    }
}

Describe "ArrayLiteral Tests" -Tags "CI" {
    It "'[void](New-Item),2,3' should return a 3-element array and first element is AutomationNull" {
        try {
            $testFile = Join-Path $TestDrive (New-Guid)
            $result = [void](New-Item $testFile -ItemType File), 2, 3
            ## file should be created
            $testFile | Should -Exist
            ## the array should contain 3 items
            $result.Count | Should -Be 3

            ## the first item should be AutomationNull
            $result[0] | ForEach-Object { "YES" } | Should -BeNullOrEmpty
            $result | Measure-Object | ForEach-Object -MemberName Count | Should -Be 2
        } finally{
            Remove-Item $testFile -Force -ErrorAction SilentlyContinue
        }
    }

    It "'[void]1, [void](New-Item), [void]2' should return a 3-AutomationNull-element array" {
        try {
            $testFile = Join-Path $TestDrive (New-Guid)
            $result = [void]1, [void](New-Item $testFile -ItemType File), [void]2
            ## file should be created
            $testFile | Should -Exist
            ## the array should contain 3 items
            $result.Count | Should -Be 3

            ## all items should be AutomationNull
            $result | ForEach-Object { "YES" } | Should -BeNullOrEmpty
        } finally {
            Remove-Item $testFile -Force -ErrorAction SilentlyContinue
        }
    }

    It "'[void]`$arraylist1.Add(1), `$arraylist2.Clear()' should return a 2-AutomationNull-element array" {
        $arraylist1 = [System.Collections.ArrayList]::new()
        $arraylist2 = [System.Collections.ArrayList]::new()

        $arraylist2.Add(2) > $null
        $arraylist2.Count | Should -Be 1

        ## first item is a non-void method call
        ## second item is a void method call
        $result = [void]$arraylist1.Add(1), $arraylist2.Clear()
        $result.Count | Should -Be 2
        $result | ForEach-Object { "YES" } | Should -BeNullOrEmpty

        $arraylist1.Count | Should -Be 1
        $arraylist1[0] | Should -Be 1

        $arraylist2.Count | Should -Be 0
    }
}

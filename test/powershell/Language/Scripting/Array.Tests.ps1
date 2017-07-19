Describe "ArrayExpression Tests" {
    It "@([int[]](1,2,3)) should return an array of object[]" {
        $result = @([int[]](1,2,3))
        $result.GetType().FullName | Should Be "System.Object[]"
        $result.Count | Should Be 3
    }

    It "@([object[]]`$null) should return an array of object[]" {
        $result = @([object[]]$null)
        $result.GetType().FullName | Should Be "System.Object[]"
        $result.Count | Should Be 1
        $result[0] | Should Be $null
    }

    It "@([int[]]`$null) should return an array of object[]" {
        $result = @([int[]]$null)
        $result.GetType().FullName | Should Be "System.Object[]"
        $result.Count | Should Be 1
        $result[0] | Should Be $null
    }

    It "@([object[]][System.Management.Automation.Internal.AutomationNull]::Value) should return an array of object[]" {
        $result = @([object[]][System.Management.Automation.Internal.AutomationNull]::Value)
        $result.GetType().FullName | Should Be "System.Object[]"
        $result.Count | Should Be 1
        $result[0] | Should Be $null
    }

    It "@([int[]][System.Management.Automation.Internal.AutomationNull]::Value) should return an array of object[]" {
        $result = @([int[]][System.Management.Automation.Internal.AutomationNull]::Value)
        $result.GetType().FullName | Should Be "System.Object[]"
        $result.Count | Should Be 1
        $result[0] | Should Be $null
    }
}
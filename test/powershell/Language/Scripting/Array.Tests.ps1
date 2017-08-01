Describe "ArrayExpression Tests" -Tags "CI" {
    It "@([object[]](1,2,3)) should return a 3-element array of object[]" {
        $result = @([object[]](1,2,3))
        $result.GetType().FullName | Should Be "System.Object[]"
        $result.Length | Should Be 3
    }

    It "@([int[]](1,2,3)) should return a 3-element array of object[]" {
        $result = @([int[]](1,2,3))
        $result.GetType().FullName | Should Be "System.Object[]"
        $result.Length | Should Be 3
    }

    It "@([object[]]`$null) should return a 1-element(`$null) array of object[]" {
        $result = @([object[]]$null)
        $result.GetType().FullName | Should Be "System.Object[]"
        $result.Length | Should Be 1
        $result[0] | Should Be $null
    }

    It "@([int[]]`$null) should return a 1-element(`$null) array of object[]" {
        $result = @([int[]]$null)
        $result.GetType().FullName | Should Be "System.Object[]"
        $result.Length | Should Be 1
        $result[0] | Should Be $null
    }

    It "@([object[]][System.Management.Automation.Internal.AutomationNull]::Value) should return a 1-element(`$null) array of object[]" {
        $result = @([object[]][System.Management.Automation.Internal.AutomationNull]::Value)
        $result.GetType().FullName | Should Be "System.Object[]"
        $result.Length | Should Be 1
        $result[0] | Should Be $null
    }

    It "@([int[]][System.Management.Automation.Internal.AutomationNull]::Value) should return a 1-element(`$null) array of object[]" {
        $result = @([int[]][System.Management.Automation.Internal.AutomationNull]::Value)
        $result.GetType().FullName | Should Be "System.Object[]"
        $result.Length | Should Be 1
        $result[0] | Should Be $null
    }

    It "@(`$null) should return a 1-element(`$null) array of object[]" {
        $result = @($null)
        $result.GetType().FullName | Should Be "System.Object[]"
        $result.Length | Should Be 1
        $result[0] | Should Be $null
    }

    It "@([System.Management.Automation.Internal.AutomationNull]::Value) should return an empty array of object[]" {
        $result = @([System.Management.Automation.Internal.AutomationNull]::Value)
        $result.GetType().FullName | Should Be "System.Object[]"
        $result.Length | Should Be 0
    }

    It "@([object[]]`$a) should return a new array" {
        $a = 1,2,3
        $result = @([object[]]$a)
        $result.GetType().FullName | Should Be "System.Object[]"
        $result.Length | Should Be 3
    }

    It "@([int[]]`$a) should return a new array" {
        $a = 1,2,3
        $result = @([int[]]$a)
        $result.GetType().FullName | Should Be "System.Object[]"
        $result.Length | Should Be 3
    }

    It "@([System.Collections.Generic.List[object]]`$null) should return a 1-element(`$null) array of object[]" {
        $result = @([System.Collections.Generic.List[object]]$null)
        $result.GetType().FullName | Should Be "System.Object[]"
        $result.Length | Should Be 1
        $result[0] | Should Be $null
    }
}

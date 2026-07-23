# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

. (Join-Path -Path $PSScriptRoot -ChildPath Test-Mocks.ps1)
Add-TestDynamicType

Describe "Select-Object" -Tags "CI" {
    BeforeEach {
        $dirObject = GetFileMock
        $TestLength = 3
    }

    It "Handle piped input without error" {
        { $dirObject | Select-Object } | Should -Not -Throw
    }

    It "Should treat input as a single object with the inputObject parameter" {
        $result = $(Select-Object -InputObject $dirObject -Last $TestLength).Length
        $expected = $dirObject.Length
        $result | Should -Be $expected
    }

    It "Should be able to use the alias" {
        { $dirObject | select } | Should -Not -Throw
    }

    It "Should have same result when using alias" {
        $result = $dirObject | Select-Object
        $expected = $dirObject | Select-Object

        $result | Should -Be $expected
    }

    It "Should return correct object with First parameter" {
        $result = $dirObject | Select-Object -First $TestLength

        $result.Length | Should -Be $TestLength

        for ($i = 0; $i -lt $TestLength; $i++) {
            $result[$i].Name | Should -Be $dirObject[$i].Name
        }
    }

    It "Should return correct object with Last parameter" {
        $result = $dirObject | Select-Object -Last $TestLength

        $result.Length | Should -Be $TestLength

        for ($i = 0; $i -lt $TestLength; $i++) {
            $result[$i].Name | Should -Be $dirObject[$dirObject.Length - $TestLength + $i].Name
        }
    }

    It "Should work correctly with Unique parameter" {
        $result = ("a", "b", "c", "a", "a", "a" | Select-Object -Unique).Length
        $expected = 3

        $result | Should -Be $expected
    }

    It "Should work work correctly with Unique and CaseInsensitive parameter" {
        $result = "abc", "Abc" | Select-Object -Unique -CaseInsensitive

        $result.Count | Should -Be 1
        $result | Should -Be "abc"
    }

    It "Should return correct object with Skip parameter" {
        $result = $dirObject | Select-Object -Skip $TestLength

        $result.Length       | Should -Be ($dirObject.Length - $TestLength)

        for ($i = 0; $i -lt $TestLength; $i++) {
            $result[$i].Name | Should -Be $dirObject[$TestLength + $i].Name
        }
    }

    It "Should return an object with selected columns" {
        $result = $dirObject | Select-Object -Property Name, Size

        $result.Length  | Should -Be $dirObject.Length
        $result[0].Name | Should -Be $dirObject[0].Name
        $result[0].Size | Should -Be $dirObject[0].Size
        $result[0].Mode | Should -BeNullOrEmpty
    }

    It "Should send output to pipe properly" {
        { $dirObject | Select-Object -Unique | pipelineConsume } | Should -Not -Throw
    }

    It "Should select array indices with Index parameter" {
        $firstIndex = 2
        $secondIndex = 4
        $result = $dirObject | Select-Object -Index $firstIndex, $secondIndex

        $result[0].Name | Should -Be $dirObject[$firstIndex].Name
        $result[1].Name | Should -Be $dirObject[$secondIndex].Name
    }

    # Note that these two tests will modify original values of $dirObject

    It "Should not wait when used without -Wait option" {
        $orig1 = $dirObject[0].Size
        $orig2 = $dirObject[$TestLength].Size
        $result = $dirObject | addOneToSizeProperty | Select-Object -First $TestLength

        $result[0].Size              | Should -Be ($orig1 + 1)
        $dirObject[0].Size           | Should -Be ($orig1 + 1)
        $dirObject[$TestLength].Size | Should -Be $orig2
    }

    It "Should wait when used with -Wait option" {
        $orig1 = $dirObject[0].Size
        $orig2 = $dirObject[$TestLength].Size
        $result = $dirObject | addOneToSizeProperty | Select-Object -First $TestLength -Wait

        $result[0].Size              | Should -Be ($orig1 + 1)
        $dirObject[0].Size           | Should -Be ($orig1 + 1)
        $dirObject[$TestLength].Size | Should -Be ($orig2 + 1)
    }

    It "Should not leak 'StopUpstreamCommandsException' internal exception for stopping upstream" {
        1, 2 | Select-Object -First 1 -ErrorVariable err
        $err | Should -BeNullOrEmpty
    }
}

Describe "Select-Object DRT basic functionality" -Tags "CI" {
    BeforeAll {
        $employees = @(
            [pscustomobject]@{"FirstName" = "joseph"; "LastName" = "smith"; "YearsInMS" = 15 }
            [pscustomobject]@{"FirstName" = "paul"; "LastName" = "smith"; "YearsInMS" = 15 }
            [pscustomobject]@{"FirstName" = "mary"; "LastName" = "soe"; "YearsInMS" = 5 }
            [pscustomobject]@{"FirstName" = "edmund"; "LastName" = "bush"; "YearsInMS" = 9 }
        )
    }

    It "Select-Object with empty script block property should throw" {
        $e = { "bar" | Select-Object -Prop {} -ErrorAction Stop } |
            Should -Throw -ErrorId "EmptyScriptBlockAndNoName,Microsoft.PowerShell.Commands.SelectObjectCommand" -PassThru
        $e.CategoryInfo | Should -Match "PSArgumentException"
    }

    It "Select-Object with Property First Last Overlap should work" {
        $results = $employees | Select-Object -Property "YearsInMS", "L*" -First 2 -Last 3
        $results.Count | Should -Be 4
    }

    It "Select-Object with string property should work" {
        $result = "bar" | Select-Object -Prop foo | Measure-Object
        $result.Count | Should -Be 1
    }

    It "Select-Object with Property First Last should work" {
        $results = $employees | Select-Object -Property "YearsInMS", "L*" -First 2 -Last 1

        $results.Count | Should -Be 3

        $results[0].LastName | Should -Be $employees[0].LastName
        $results[1].LastName | Should -Be $employees[1].LastName
        $results[2].LastName | Should -Be $employees[3].LastName

        $results[0].YearsInMS | Should -Be $employees[0].YearsInMS
        $results[1].YearsInMS | Should -Be $employees[1].YearsInMS
        $results[2].YearsInMS | Should -Be $employees[3].YearsInMS
    }

    It "Select-Object with Property First should work" {
        $results = $employees | Select-Object -Property "YearsInMS", "L*" -First 2

        $results.Count | Should -Be 2

        $results[0].LastName | Should -Be $employees[0].LastName
        $results[1].LastName | Should -Be $employees[1].LastName

        $results[0].YearsInMS | Should -Be $employees[0].YearsInMS
        $results[1].YearsInMS | Should -Be $employees[1].YearsInMS
    }

    It "Select-Object with Property First Zero should work" {
        $results = $employees | Select-Object -Property "YearsInMS", "L*" -First 0

        $results.Count | Should -Be 0
    }

    It "Select-Object with Property Last Zero should work" {
        $results = $employees | Select-Object -Property "YearsInMS", "L*" -Last 0

        $results.Count | Should -Be 0
    }

    It "Select-Object with Unique should work" {
        $results = $employees | Select-Object -Property "YearsInMS", "L*" -Unique:$true

        $results.Count | Should -Be 3

        $results[0].LastName | Should -Be $employees[1].LastName
        $results[1].LastName | Should -Be $employees[2].LastName
        $results[2].LastName | Should -Be $employees[3].LastName

        $results[0].YearsInMS | Should -Be $employees[1].YearsInMS
        $results[1].YearsInMS | Should -Be $employees[2].YearsInMS
        $results[2].YearsInMS | Should -Be $employees[3].YearsInMS
    }

    It "Select-Object with Simple should work" {
        $employee1 = [pscustomobject]@{"FirstName" = "joesph"; "LastName" = "smith"; "YearsInMS" = 15 }
        $employee2 = [pscustomobject]@{"FirstName" = "paul"; "LastName" = "smith"; "YearsInMS" = 15 }
        $employee3 = [pscustomobject]@{"FirstName" = "mary"; "LastName" = "soe"; "YearsInMS" = 15 }
        $employees3 = @($employee1, $employee2, $employee3, $employee4)
        $results = $employees3 | Select-Object -Property "FirstName", "YearsInMS"

        $results.Count | Should -Be 3

        $results[0].FirstName | Should -Be $employees3[0].FirstName
        $results[1].FirstName | Should -Be $employees3[1].FirstName
        $results[2].FirstName | Should -Be $employees3[2].FirstName

        $results[0].YearsInMS | Should -Be $employees3[0].YearsInMS
        $results[1].YearsInMS | Should -Be $employees3[1].YearsInMS
        $results[2].YearsInMS | Should -Be $employees3[2].YearsInMS
    }

    It "Select-Object with no input should work" {
        $results = $null | Select-Object -Property "FirstName", "YearsInMS", "FirstNa*"
        $results.Count | Should -Be 0
    }

    It "Select-Object with Start-Time In Idle Process should work" {
        $results = Get-Process * | Select-Object ProcessName
        $results.Count | Should -Not -Be 0
    }

    It "Select-Object with Skip should work" {
        $results = "1", "2", "3" | Select-Object -Skip 1
        $results.Count | Should -Be 2
        $results[0] | Should -Be 2
        $results[1] | Should -Be 3
    }

    It "Select-Object with SkipLast should work" {
        $results = "1", "2", "3" | Select-Object -SkipLast 1
        $results.Count | Should -Be 2
        $results[0] | Should -BeExactly "1"
        $results[1] | Should -BeExactly "2"
    }

    It "Select-Object with Skip and SkipLast should work" {
        $results = "1", "2", "3" | Select-Object -Skip 1 -SkipLast 1
        $results.Count | Should -Be 1
        $results[0] | Should -BeExactly "2"
    }

    It "Select-Object with Skip and SkipLast should work with Skip overlapping SkipLast" {
        $results = "1", "2" | Select-Object -Skip 2 -SkipLast 1
        $results. Count | Should -Be 0
    }

    It "Select-Object with Skip and SkipLast should work with skiplast overlapping skip" {
        $results = "1", "2" | Select-Object -Skip 1 -SkipLast 2
        $results. Count | Should -Be 0
    }

    It "Select-Object with Index should work" {
        $results = "1", "2", "3" | Select-Object -Index 2
        $results.Count | Should -Be 1
        $results[0] | Should -BeExactly "3"
    }

    It "Select-Object with SkipIndex should work" {
        $results = "1", "2", "3" | Select-Object -SkipIndex 0, 2
        $results | Should -HaveCount 1
        $results[0] | Should -BeExactly "2"
    }

    It "Select-Object with SkipIndex should work with index out of range" {
        $results = 0..10 | Select-Object -SkipIndex 5, 6, 7, 8, 11
        $results | Should -HaveCount 7
        $results -join ',' | Should -BeExactly "0,1,2,3,4,9,10"
    }

    It "Select-Object should handle dynamic (DLR) properties" {
        $dynObj = [TestDynamic]::new()
        $results = $dynObj, $dynObj | Select-Object -ExpandProperty FooProp
        $results.Count | Should -Be 2
        $results[0] | Should -Be 123
        $results[1] | Should -Be 123
    }

    It "Select-Object should handle dynamic (DLR) properties without GetDynamicMemberNames hint" {
        $dynObj = [TestDynamic]::new()
        $results = $dynObj, $dynObj | Select-Object -ExpandProperty HiddenProp
        $results.Count | Should -Be 2
        $results[0] | Should -Be 789
        $results[1] | Should -Be 789
    }

    It "Select-Object should handle wildcarded dynamic (DLR) properties when hinted by GetDynamicMemberNames" {
        $dynObj = [TestDynamic]::new()
        $results = $dynObj, $dynObj | Select-Object -ExpandProperty FooP*
        $results.Count | Should -Be 2
        $results[0] | Should -Be 123
        $results[1] | Should -Be 123
    }

    It "Select-Object should work when multiple dynamic (DLR) properties match" {
        $dynObj = [TestDynamic]::new()
        $results = $dynObj, $dynObj | Select-Object *Prop
        $results.Count | Should -Be 2
        $results[0].FooProp | Should -Be 123
        $results[0].BarProp | Should -Be 456
        $results[1].FooProp | Should -Be 123
        $results[1].BarProp | Should -Be 456
    }

    It "Select-Object -ExpandProperty should yield errors if multiple dynamic (DLR) properties match" {
        $dynObj = [TestDynamic]::new()
        $e = { $results = $dynObj, $dynObj | Select-Object -ExpandProperty *Prop -ErrorAction Stop } |
            Should -Throw -PassThru -ErrorId "MutlipleExpandProperties,Microsoft.PowerShell.Commands.SelectObjectCommand"
        $e.CategoryInfo | Should -Match "PSArgumentException"
    }
}

Describe "Select-Object with Property = '*'" -Tags "CI" {

    # Issue #2420
    It "Select-Object with implicit Property = '*' don't return property named '*'" {
        $results = [pscustomobject]@{Thing = "thing1" } | Select-Object -ExcludeProperty thing
        $results.psobject.Properties.Item("*") | Should -BeNullOrEmpty
    }

    # Issue #2420
    It "Select-Object with explicit Property = '*' don't return property named '*'" {
        $results = [pscustomobject]@{Thing = "thing1" } | Select-Object -Property * -ExcludeProperty thing
        $results.psobject.Properties.Item("*") | Should -BeNullOrEmpty
    }

    # Issue #2351
    It "Select-Object with implicit Property = '*' exclude single property" {
        $results = [pscustomobject]@{Thing = "thing1" } | Select-Object -ExcludeProperty thing
        $results.psobject.Properties.Item("Thing") | Should -BeNullOrEmpty
        $results.psobject.Properties.Item("*") | Should -BeNullOrEmpty
    }

    # Issue #2351
    It "Select-Object with explicit Property = '*' exclude single property" {
        $results = [pscustomobject]@{Thing = "thing1" } | Select-Object -Property * -ExcludeProperty thing
        $results.psobject.Properties.Item("Thing") | Should -BeNullOrEmpty
        $results.psobject.Properties.Item("*") | Should -BeNullOrEmpty
    }

    # Issue #2351
    It "Select-Object with implicit Property = '*' exclude not single property" {
        $results = [pscustomobject]@{Thing = "thing1"; Param2 = "param2" } | Select-Object -ExcludeProperty Param2
        $results.Param2 | Should -BeNullOrEmpty
        $results.Thing | Should -BeExactly "thing1"
    }

    # Issue #2351
    It "Select-Object with explicit Property = '*' exclude not single property" {
        $results = [pscustomobject]@{Thing = "thing1"; Param2 = "param2" } | Select-Object -Property * -ExcludeProperty Param2
        $results.Param2 | Should -BeNullOrEmpty
        $results.Thing | Should -BeExactly "thing1"
    }

    It "Select-Object with ExpandProperty and Property don't skip processing ExcludeProperty" {
        $p = Get-Process -Id $PID | Select-Object -Property Process* -ExcludeProperty ProcessorAffinity -ExpandProperty Modules
        $p[0].psobject.Properties.Item("ProcessorAffinity") | Should -BeNullOrEmpty
    }

    It "Select-Object add 'Selected.*' type only once" {
        $obj = [PSCustomObject]@{ Name = 1 }

        $obj.psobject.TypeNames.Count | Should -Be 2
        $obj.psobject.TypeNames | Should -Not -BeLike "Selected*"

        $obj = $obj | Select-Object

        $obj.psobject.TypeNames.Count | Should -Be 3
        $obj.psobject.TypeNames[0] | Should -BeLike "Selected*"
        $obj.psobject.TypeNames[1] | Should -Not -BeLike "Selected*"
        $obj.psobject.TypeNames[2] | Should -Not -BeLike "Selected*"
    }
}

Describe 'Select-Object behaviour with hashtable entries and actual members' -Tags "CI" {

    It 'can retrieve a hashtable entry as a property' {
        $hashtable = @{ Entry = 100 }

        $result = $hashtable | Select-Object -Property Entry
        $result.Entry | Should -Be 100

        $hashtable |
            Select-Object -ExpandProperty Entry |
            Should -Be 100
    }

    It 'can retrieve true hashtable members' {
        $hashtable = @{ Value = 10 }
        $result = $hashtable | Select-Object -Property Keys
        $result.Keys | Should -Be 'Value'

        $hashtable |
            Select-Object -ExpandProperty Keys |
            Should -Be 'Value'
    }

    It 'should prioritise hashtable entries where available' {
        $hashtable = @{ Keys = 10 }
        $result = $hashtable | Select-Object -Property Keys
        $result.Keys | Should -Be 10

        $hashtable | Select-Object -ExpandProperty Keys | Should -Be 10
    }

    It 'should get the hashtable Count member' {
        $hashtable = @{ a = 10; b = 20; c = 30 }
        $result = $hashtable | Select-Object -Property Count
        $result.Count | Should -Be 3

        $hashtable | Select-Object -ExpandProperty Count | Should -Be 3
    }
}

Describe "Select-Object with properties containing wildcard characters" -Tags "CI" {

    It 'can select properties with literal wildcard characters using escaped names' {
        $testObject = [PSCustomObject]@{ 'Foo[]' = 'bar'; 'NormalProp' = 'normal' }

        # Test with escaped property name - should get exact match
        $result = $testObject | Select-Object ([WildcardPattern]::Escape('Foo[]'))
        $result.PSObject.Properties.Name | Should -Contain 'Foo[]'
        $result.'Foo[]' | Should -Be 'bar'
    }

    It 'can select properties with wildcard characters alongside normal properties' {
        $testObject = [PSCustomObject]@{ 'Foo[]' = 'bar'; 'NormalProp' = 'normal'; 'Other' = 'value' }

        # Test selecting escaped literal alongside normal properties
        $result = $testObject | Select-Object ([WildcardPattern]::Escape('Foo[]')), 'NormalProp'
        (@($result.PSObject.Properties)).Count | Should -Be 2
        $result.'Foo[]' | Should -Be 'bar'
        $result.NormalProp | Should -Be 'normal'
    }

    It 'can expand properties with literal wildcard characters using escaped names' {
        $innerObject = [PSCustomObject]@{ 'InnerProp' = 'innerValue' }
        $testObject = [PSCustomObject]@{ 'Prop[]' = $innerObject }

        # Test expanding property with escaped name
        $result = $testObject | Select-Object -ExpandProperty ([WildcardPattern]::Escape('Prop[]'))
        $result.InnerProp | Should -Be 'innerValue'
    }

    It 'can expand array properties with literal wildcard characters' {
        $testObject = [PSCustomObject]@{ 'Array*Prop' = @('item1', 'item2', 'item3') }

        # Test expanding array property with escaped name
        $result = $testObject | Select-Object -ExpandProperty ([WildcardPattern]::Escape('Array*Prop'))
        $result.Count | Should -Be 3
        $result[0] | Should -Be 'item1'
        $result[2] | Should -Be 'item3'
    }

    It 'handles properties with various wildcard characters' {
        $testObject = [PSCustomObject]@{
            'Prop*' = 'asterisk'
            'Prop?' = 'question'
            'Prop[]' = 'brackets'
            'Prop[abc]' = 'charclass'
            'Prop[0-9]' = 'range'
        }

        # Test each type of wildcard character
        $result1 = $testObject | Select-Object ([WildcardPattern]::Escape('Prop*'))
        $result1.'Prop*' | Should -Be 'asterisk'

        $result2 = $testObject | Select-Object ([WildcardPattern]::Escape('Prop?'))
        $result2.'Prop?' | Should -Be 'question'

        $result3 = $testObject | Select-Object ([WildcardPattern]::Escape('Prop[]'))
        $result3.'Prop[]' | Should -Be 'brackets'

        $result4 = $testObject | Select-Object ([WildcardPattern]::Escape('Prop[abc]'))
        $result4.'Prop[abc]' | Should -Be 'charclass'

        $result5 = $testObject | Select-Object ([WildcardPattern]::Escape('Prop[0-9]'))
        $result5.'Prop[0-9]' | Should -Be 'range'
    }

    It 'can select multiple properties with different wildcard characters' {
        $testObject = [PSCustomObject]@{
            'Test*' = 'star'
            'Test?' = 'question'
            'Normal' = 'normal'
        }

        # Test selecting multiple escaped properties at once
        $result = $testObject | Select-Object ([WildcardPattern]::Escape('Test*')), ([WildcardPattern]::Escape('Test?')), 'Normal'
        (@($result.PSObject.Properties)).Count | Should -Be 3
        $result.'Test*' | Should -Be 'star'
        $result.'Test?' | Should -Be 'question'
        $result.'Normal' | Should -Be 'normal'
    }

    It 'returns null property when escaped pattern has no exact match' {
        $testObject = [PSCustomObject]@{ 'FooBar' = 'value'; 'FooBaz' = 'other' }

        # Test that escaped pattern that doesn't match exact property returns null property
        $escapedPattern = [WildcardPattern]::Escape('Foo*')
        $result = $testObject | Select-Object $escapedPattern
        # The escaped Foo* should not match any exact property, so it creates a null property with that name
        (@($result.PSObject.Properties)).Count | Should -Be 1
        $result.PSObject.Properties.Name | Should -Contain $escapedPattern
        $result.$escapedPattern | Should -BeNullOrEmpty
    }

    It 'works with calculated properties containing wildcards' {
        $testObject = [PSCustomObject]@{ 'Calc[]' = 'original' }

        # Test calculated property that references escaped wildcard property
        $result = $testObject | Select-Object @{Name='NewName'; Expression={$_.'Calc[]'}}
        $result.NewName | Should -Be 'original'
    }

    It 'preserves normal wildcard functionality' {
        $testObject = [PSCustomObject]@{
            'Foo1' = 'value1'
            'Foo2' = 'value2'
            'Foo[]' = 'brackets'
            'Other' = 'different'
        }

        # Normal wildcards should still work and match multiple properties
        $result = $testObject | Select-Object 'Foo*'
        (@($result.PSObject.Properties)).Count | Should -Be 3  # Foo1, Foo2, Foo[]
        $result.Foo1 | Should -Be 'value1'
        $result.Foo2 | Should -Be 'value2'
        $result.'Foo[]' | Should -Be 'brackets'
    }

    It 'emits error but continues when wildcard and escaped name match same property' {
        $testObject = [PSCustomObject]@{ 'Foo[]' = 'bar' }

        # This should emit a non-terminating error but still produce output
        $result = $testObject | Select-Object 'Foo*', ([WildcardPattern]::Escape('Foo[]')) 2>&1

        # Should get one error message about duplicate property
        $errorRecords = $result | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] }
        $errorRecords.Count | Should -Be 1
        $errorRecords[0].FullyQualifiedErrorId | Should -Be 'AlreadyExistingUserSpecifiedPropertyNoExpand,Microsoft.PowerShell.Commands.SelectObjectCommand'

        # Should still get output with the property (only once)
        $outputRecords = $result | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] }
        $outputRecords.Count | Should -Be 1
        $outputRecords[0].'Foo[]' | Should -Be 'bar'
        (@($outputRecords[0].PSObject.Properties)).Count | Should -Be 1
    }

    It 'does not emit error when using wildcard or escaped separately' {
        $testObject = [PSCustomObject]@{ 'Foo[]' = 'bar'; 'FooBar' = 'baz' }

        # Test 1: Wildcard alone (should match both properties, no error)
        $result1 = $testObject | Select-Object 'Foo*' 2>&1
        $errorRecords1 = $result1 | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] }
        $errorRecords1.Count | Should -Be 0
        $outputRecords1 = $result1 | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] }
        (@($outputRecords1[0].PSObject.Properties)).Count | Should -Be 2

        # Test 2: Escaped alone (should match exact property, no error)
        $result2 = $testObject | Select-Object ([WildcardPattern]::Escape('Foo[]')) 2>&1
        $errorRecords2 = $result2 | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] }
        $errorRecords2.Count | Should -Be 0
        $outputRecords2 = $result2 | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] }
        (@($outputRecords2[0].PSObject.Properties)).Count | Should -Be 1
        $outputRecords2[0].'Foo[]' | Should -Be 'bar'
    }

    It 'handles complex nested property names with wildcards' {
        $testObject = [PSCustomObject]@{ 'Path[*].Name' = 'complex'; 'Simple' = 'value' }

        # Test complex property name with multiple wildcard types
        $result = $testObject | Select-Object ([WildcardPattern]::Escape('Path[*].Name'))
        $result.'Path[*].Name' | Should -Be 'complex'
    }

    It 'works with ExpandProperty and error handling' {
        $testObject = [PSCustomObject]@{ 'Valid[]' = 'exists'; 'Other' = 'value' }

        # Test that ExpandProperty throws proper error for non-existent escaped property
        $escapedPattern = [WildcardPattern]::Escape('NonExistent[]')
        { $testObject | Select-Object -ExpandProperty $escapedPattern -ErrorAction Stop } |
            Should -Throw -ErrorId 'ExpandPropertyNotFound,Microsoft.PowerShell.Commands.SelectObjectCommand'
    }

    Context 'Regression tests for GitHub issue #25982' {
        It 'reproduces the exact scenario from the issue' {
            # This is the exact scenario reported in the GitHub issue
            $testObject = [PSCustomObject]@{ 'Foo[]' = 'bar' }
            $result = $testObject | Select-Object ([WildcardPattern]::Escape('Foo[]'))

            # Should successfully select the property
            $result.'Foo[]' | Should -Be 'bar'
            $result.PSObject.Properties.Name | Should -Contain 'Foo[]'
        }

        It 'works with the expected behavior from the issue description' {
            # Testing that both wildcard and escaped approaches work (when not conflicting)
            $testObject = [PSCustomObject]@{ 'Foo[]' = 'bar'; 'FooBar' = 'baz' }

            # Test wildcard selection
            $result1 = $testObject | Select-Object 'Foo*'
            (@($result1.PSObject.Properties)).Count | Should -Be 2

            # Test escaped selection
            $result2 = $testObject | Select-Object ([WildcardPattern]::Escape('Foo[]'))
            (@($result2.PSObject.Properties)).Count | Should -Be 1
            $result2.'Foo[]' | Should -Be 'bar'
        }
    }
}



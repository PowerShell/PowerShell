# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Format-Wide" -Tags "CI" {
    BeforeAll {
        1..2 | ForEach-Object { New-Item -Path ("TestDrive:\Testdir{0:00}" -f $_) -ItemType Directory }
        1..2 | ForEach-Object { New-Item -Path ("TestDrive:\TestFile{0:00}.txt" -f $_) -ItemType File }
        $pathList = Get-ChildItem $TestDrive
    }

    It "Should be able to specify the columns in output using the column switch" {
        { $pathList | Format-Wide -Column 3 } | Should -Not -Throw
    }

    It "Should be able to use the autosize switch" {
        { $pathList | Format-Wide -AutoSize } | Should -Not -Throw
        { $pathList | Format-Wide -AutoSize | Out-String } | Should -Not -Throw
    }

    It "Should be able to take inputobject instead of pipe" {
        { Format-Wide -InputObject $pathList } | Should -Not -Throw
    }

    It "Should be able to use the property switch" {
        { Format-Wide -InputObject $pathList -Property Mode } | Should -Not -Throw
    }

    It "Should throw an error when property switch and view switch are used together" {
        { Format-Wide -InputObject $pathList -Property CreationTime -View aoeu } |
            Should -Throw -ErrorId "FormatCannotSpecifyViewAndProperty,Microsoft.PowerShell.Commands.FormatWideCommand"
    }

    It "Should throw and suggest proper input when view is used with invalid input without the property switch" {
        { Format-Wide -InputObject $(Get-Process) -View aoeu } | Should -Throw
    }
}

Describe "Format-Wide DRT basic functionality" -Tags "CI" {
    It "Format-Wide with array should work" {
        $al = (0..255)
        $info = @{}
        $info.array = $al
        $result = $info | Format-Wide | Out-String
        $result | Should -Match "array"
    }

    It "Format-Wide with No Objects for End-To-End should work" {
        $p = @{}
        $result = $p | Format-Wide | Out-String
        $result | Should -BeNullOrEmpty
    }

    It "Format-Wide with Null Objects for End-To-End should work" {
        $p = $null
        $result = $p | Format-Wide | Out-String
        $result | Should -BeNullOrEmpty
    }

    It "Format-Wide with single line string for End-To-End should work" {
        $p = "single line string"
        $result = $p | Format-Wide | Out-String
        $result | Should -Match $p
    }

    It "Format-Wide with multiple line string for End-To-End should work" {
        $p = "Line1\nLine2"
        $result = $p | Format-Wide | Out-String
        $result | Should -Match "Line1"
        $result | Should -Match "Line2"
    }

    It "Format-Wide with string sequence for End-To-End should work" {
        $p = "Line1", "Line2"
        $result = $p |Format-Wide | Out-String
        $result | Should -Match "Line1"
        $result | Should -Match "Line2"
    }

    It "Format-Wide with complex object for End-To-End should work" {
        Add-Type -TypeDefinition "public enum MyDayOfWeek{Sun,Mon,Tue,Wed,Thu,Fri,Sat}"
        $eto = New-Object MyDayOfWeek
        $info = @{}
        $info.intArray = 1, 2, 3, 4
        $info.arrayList = "string1", "string2"
        $info.enumerable = [MyDayOfWeek]$eto
        $info.enumerableTestObject = $eto
        $result = $info|Format-Wide|Out-String
        $result | Should -Match "intArray"
        $result | Should -Match "arrayList"
        $result | Should -Match "enumerable"
        $result | Should -Match "enumerableTestObject"
    }

    It "Format-Wide with multiple same class object with grouping should work" {
        Add-Type -TypeDefinition "public class TestGroupingClass{public TestGroupingClass(string name,int length){Name = name;Length = length;}public string Name;public int Length;public string GroupingKey;}"
        $testobject1 = [TestGroupingClass]::New('name1', 1)
        $testobject1.GroupingKey = "foo"
        $testobject2 = [TestGroupingClass]::New('name2', 2)
        $testobject1.GroupingKey = "bar"
        $testobject3 = [TestGroupingClass]::New('name3', 3)
        $testobject1.GroupingKey = "bar"
        $testobjects = @($testobject1, $testobject2, $testobject3)
        $result = $testobjects|Format-Wide -GroupBy GroupingKey|Out-String
        $result | Should -Match "GroupingKey: bar"
        $result | Should -Match "name1"
        $result | Should -Match " GroupingKey:"
        $result | Should -Match "name2\s+name3"
    }

    Context 'ExcludeProperty parameter' {
        It 'Should exclude specified property and display first remaining' {
            # PSCustomObject properties are in definition order: Name, Age, City
            $obj = [pscustomobject]@{ Name = 'Test'; Age = 30; City = 'Seattle' }
            # Exclude Name, should display Age (first remaining)
            $result = $obj | Format-Wide -ExcludeProperty Name | Out-String
            $result | Should -Match '30'
            $result | Should -Not -Match 'Test'
        }

        It 'Should work with wildcard patterns' {
            # Properties: Prop1, Prop2, Other
            $obj = [pscustomobject]@{ Prop1 = 1; Prop2 = 2; Other = 3 }
            # Exclude Prop*, should display Other (only remaining)
            $result = $obj | Format-Wide -ExcludeProperty Prop* | Out-String
            $result | Should -Match '3'
            $result | Should -Not -Match '1'
            $result | Should -Not -Match '2'
        }

        It 'Should work without Property parameter (implies -Property *)' {
            # Properties: A, B, C
            $obj = [pscustomobject]@{ A = 1; B = 2; C = 3 }
            # Exclude B, C - should display A (first remaining)
            $result = $obj | Format-Wide -ExcludeProperty B, C | Out-String
            $result | Should -Match '1'
            $result | Should -Not -Match '2'
            $result | Should -Not -Match '3'
        }

        It 'Should display first remaining property after exclusion' {
            # Properties: Name, Age, City, Country
            $obj = [pscustomobject]@{ Name = 'Test'; Age = 30; City = 'Seattle'; Country = 'USA' }
            # Exclude Name and Age, should display City (first remaining)
            $result = $obj | Format-Wide -ExcludeProperty Name, Age | Out-String
            $result | Should -Match 'Seattle'
            $result | Should -Not -Match 'Test'
            $result | Should -Not -Match '30'
            # USA might appear but not in the wide field, or might not appear at all since only first property is shown
        }

        It 'Should handle multiple excluded properties' {
            # Properties: A, B, C, D
            $obj = [pscustomobject]@{ A = 1; B = 2; C = 3; D = 4 }
            # Exclude A and B, should display C (first remaining)
            $result = $obj | Format-Wide -ExcludeProperty A, B | Out-String
            $result | Should -Match '3'
            $result | Should -Not -Match '1'
            $result | Should -Not -Match '2'
        }
    }
}

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Language Primitive Tests" -Tags "CI" {
    It "Equality comparison with string and non-numeric type should not be culture sensitive" {
        $date = [datetime]'2005,3,10'
        $val = [System.Management.Automation.LanguagePrimitives]::Equals($date, "3/10/2005")
        $val | Should -BeTrue
    }

    It "Test conversion of an PSObject with Null Base Object to bool" {
        $mshObj = New-Object psobject
        { [System.Management.Automation.LanguagePrimitives]::ConvertTo($mshObj, [bool]) } | Should -BeTrue
    }

    It "Test conversion of an PSObject with Null Base Object to string" {
        $mshObj = New-Object psobject
        { [System.Management.Automation.LanguagePrimitives]::ConvertTo($mshObj, [string]) -eq "" } | Should -BeTrue
    }

    It "Test conversion of an PSObject with Null Base Object to object" {
        $mshObj = New-Object psobject
        { $mshObj -eq [System.Management.Automation.LanguagePrimitives]::ConvertTo($mshObj, [Object]) } | Should -BeTrue
    }

    It "Test Conversion of an IEnumerable to object[]" {
        $col = [System.Diagnostics.Process]::GetCurrentProcess().Modules
        $ObjArray = [System.Management.Automation.LanguagePrimitives]::ConvertTo($col, [object[]])
        $ObjArray.Length | Should -Be $col.Count
    }

    It "Test conversion with .Net Core intrinsic type convertor" {
        $result = [System.Management.Automation.LanguagePrimitives]::ConvertTo('2,3', [System.Drawing.Point])
        $result | Should -BeOfType System.Drawing.Point
        $result.X | Should -Be 2
        $result.Y | Should -Be 3

        $result = [System.Management.Automation.LanguagePrimitives]::ConvertTo([PSObject]'2,3', [System.Drawing.Point])
        $result | Should -BeOfType System.Drawing.Point
        $result.X | Should -Be 2
        $result.Y | Should -Be 3

        $result = [System.Management.Automation.LanguagePrimitives]::ConvertTo('http://test.site.com', [System.Uri])
        $result | Should -BeOfType System.Uri
        $result.AbsoluteUri | Should -BeExactly 'http://test.site.com/'

        # accept relative URI path
        $result = [System.Management.Automation.LanguagePrimitives]::ConvertTo('..\foo', [System.Uri])
        $result | Should -BeOfType System.Uri
        $result.OriginalString | Should -BeExactly '..\foo'
    }

    It "Test conversion with .Net Core intrinsic type convertor (Windows only types)" -Skip:(-not $IsWindows) {
        $result = [System.Management.Automation.LanguagePrimitives]::ConvertTo('Microsoft Sans Serif,10', [System.Drawing.Font])
        $result | Should -BeOfType System.Drawing.Font
        $result.Size | Should -Be 10
        $result.Name | Should -BeExactly 'Microsoft Sans Serif'

        $result = [System.Management.Automation.LanguagePrimitives]::ConvertTo([PSObject]'Microsoft Sans Serif,10', [System.Drawing.Font])
        $result | Should -BeOfType System.Drawing.Font
        $result.Size | Should -Be 10
        $result.Name | Should -BeExactly 'Microsoft Sans Serif'
    }

    It "Casting recursive array to bool should not cause crash" {
        $a[0] = $a = [PSObject](, 1)
        [System.Management.Automation.LanguagePrimitives]::IsTrue($a) | Should -BeTrue
    }

    It "LanguagePrimitives.GetEnumerable should treat 'DataTable' as Enumerable" {
        $dt = [System.Data.DataTable]::new("test")
        $dt.Columns.Add("Name", [string]) > $null
        $dt.Columns.Add("Age", [string]) > $null
        $dr = $dt.NewRow(); $dr["Name"] = "John"; $dr["Age"] = "20"
        $dr2 = $dt.NewRow(); $dr["Name"] = "Susan"; $dr["Age"] = "25"
        $dt.Rows.Add($dr); $dt.Rows.Add($dr2)

        [System.Management.Automation.LanguagePrimitives]::IsObjectEnumerable($dt) | Should -BeTrue
        $count = 0
        [System.Management.Automation.LanguagePrimitives]::GetEnumerable($dt) | ForEach-Object { $count++ }
        $count | Should -Be 2
    }

    It "TryCompare should succeed on int and string" {
        $result = $null
        [System.Management.Automation.LanguagePrimitives]::TryCompare(1, "1", [ref] $result) | Should -BeTrue
        $result | Should -Be 0
    }

    It "TryCompare should fail on int and datetime" {
        $result = $null
        [System.Management.Automation.LanguagePrimitives]::TryCompare(1, [datetime]::Now, [ref] $result) | Should -BeFalse
    }

    It "TryCompare should succeed on int and int and compare correctly smaller" {
        $result = $null
        [System.Management.Automation.LanguagePrimitives]::TryCompare(1, 2, [ref] $result) | Should -BeTrue
        $result | Should -BeExactly -1
    }

    It "TryCompare should succeed on string and string and compare correctly greater" {
        $result = $null
        [System.Management.Automation.LanguagePrimitives]::TryCompare("bbb", "aaa", [ref] $result) | Should -BeTrue
        $result | Should -BeExactly 1
    }

    It "TryCompare should succeed on string and string and compare case insensitive correctly" {
        $result = $null
        [System.Management.Automation.LanguagePrimitives]::TryCompare("AAA", "aaa", $true, [ref] $result) | Should -BeTrue
        $result | Should -BeExactly 0
    }

    It "TryCompare with cultureInfo is culture sensitive" {
        $result = $null
        $swedish = [cultureinfo] 'sv-SE'
        # in Swedish, åäö appears at the end of the alphabet, and should compare greater than o
        $val = [System.Management.Automation.LanguagePrimitives]::TryCompare("ooo", "ååå", $false, $swedish, [ref] $result)
        $val | Should -BeTrue
        $result | Should -BeExactly -1
    }

    It "TryCompare compares greater than null as Compare" {
        $result = $null

        $compareResult = [System.Management.Automation.LanguagePrimitives]::Compare($null, 10)
        $val = [System.Management.Automation.LanguagePrimitives]::TryCompare($null, 10, [ref] $result)
        $val | Should -BeTrue
        $result | Should -BeExactly $compareResult
    }

    It "TryCompare compares less than null as Compare" {
        $result = $null

        $compareResult = [System.Management.Automation.LanguagePrimitives]::Compare(10, $null)
        $val = [System.Management.Automation.LanguagePrimitives]::TryCompare(10, $null, [ref] $result)
        $val | Should -BeTrue
        $result | Should -BeExactly $compareResult
    }

    It "Convert ScriptBlock to delegate type" {
        $code = @'
        using System;
        namespace Test.API
        {
            public enum TestEnum
            {
                Music,
                Video
            }
            public class LanguagePrimitivesTest
            {
                Func<string, object> _handlerReturnObject;
                Func<string, TestEnum> _handlerReturnEnum;
                public LanguagePrimitivesTest(Func<string, object> handlerReturnObject, Func<string, TestEnum> handlerReturnEnum)
                {
                    _handlerReturnObject = handlerReturnObject;
                    _handlerReturnEnum = handlerReturnEnum;
                }

                public bool TestHandlerReturnEnum()
                {
                    var value = _handlerReturnEnum("bar");
                    return value == TestEnum.Music;
                }

                public bool TestHandlerReturnObject()
                {
                    object value = _handlerReturnObject("bar");
                    return value is TestEnum;
                }
            }
        }
'@

        if (-not ("Test.API.TestEnum" -as [type]))
        {
            Add-Type -TypeDefinition $code
        }

        # The script actually returns a enum value, and the converted delegate should return the boxed enum value.
        $handlerReturnObject = [System.Func[string, object]] { param([string]$str) [Test.API.TestEnum]::Music }
        # The script actually returns a string, and the converted delegate should return the corresponding enum value.
        $handlerReturnEnum = [System.Func[string, Test.API.TestEnum]] { param([string]$str) "Music" }
        $test = [Test.API.LanguagePrimitivesTest]::new($handlerReturnObject, $handlerReturnEnum)

        $test.TestHandlerReturnEnum() | Should -BeTrue
        $test.TestHandlerReturnObject() | Should -BeTrue
    }
}

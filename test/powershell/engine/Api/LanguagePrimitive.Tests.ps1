# Copyright (c) Microsoft Corporation. All rights reserved.
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

    It "Casting recursive array to bool should not cause crash" {
        $a[0] = $a = [PSObject](,1)
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
}

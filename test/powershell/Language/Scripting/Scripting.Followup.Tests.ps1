# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Scripting.Followup.Tests" -Tags "CI" {
    It "'[void](New-Item) | <Cmdlet>' should work and behave like passing AutomationNull to the pipe" {
        try {
            $testFile = Join-Path $TestDrive (New-Guid)
            [void](New-Item $testFile -ItemType File) | ForEach-Object { "YES" } | Should -BeNullOrEmpty
            ## file should be created
            $testFile | Should -Exist
        } finally {
            Remove-Item $testFile -Force -ErrorAction SilentlyContinue
        }
    }

    ## cast non-void method call to [void]
    It "'[void]`$arraylist.Add(1) | <Cmdlet>' should work and behave like passing AutomationNull to the pipe" {
        $arraylist = [System.Collections.ArrayList]::new()
        [void]$arraylist.Add(1) | ForEach-Object { "YES" } | Should -BeNullOrEmpty
        ## $arraylist.Add(1) should be executed
        $arraylist.Count | Should -Be 1
        $arraylist[0] | Should -Be 1
    }

    ## void method call
    It "'`$arraylist2.Clear() | <Cmdlet>' should work and behave like passing AutomationNull to the pipe" {
        $arraylist = [System.Collections.ArrayList]::new()
        $arraylist.Add(1) > $null
        $arraylist.Clear() | ForEach-Object { "YES" } | Should -BeNullOrEmpty
        ## $arraylist.Clear() should be executed
        $arraylist.Count | Should -Be 0
    }

    ## fix https://github.com/PowerShell/PowerShell/issues/17165
    It "([bool] `$var = 42) should return the varaible value" {
        ([bool]$var = 42).GetType().FullName | Should -Be "System.Boolean"
        . { ([bool]$var = 42).GetType().FullName } | Should -Be "System.Boolean"
    }

    It "Setting property using 'ForEach' method should work on a scalar object" {
        $obj = [pscustomobject] @{ p = 1 }
        $obj.ForEach('p', 32) | Should -BeNullOrEmpty
        $obj.p | Should -Be 32
    }

    It "Test the special type name 'ordered'" {
        class ordered {
            [hashtable] $Member
            ordered([hashtable] $hash) {
                $this.Member = $hash
            }
        }

        ## `<expr> -as\-is [ordered]` resolves 'ordered' as a normal type name.
        $hash = @{ key = 2 }
        $result = $hash -as [ordered]
        $result.GetType().FullName | Should -BeExactly ([ordered].FullName)
        $result -is [ordered] | Should -BeTrue
        $result.Member['key'] | Should -Be 2
        $result.Member.Count | Should -Be 1

        ## `[ordered]$hash` causes parsing error.
        $err = $null
        $null = [System.Management.Automation.Language.Parser]::ParseInput('[ordered]$hash', [ref]$null, [ref]$err)
        $err.Count | Should -Be 1
        $err[0].ErrorId | Should -BeExactly 'OrderedAttributeOnlyOnHashLiteralNode'

        ## `[ordered]@{ key = 1 }` creates 'OrderedDictionary'
        $result = [ordered]@{ key = 1 }
        $result | Should -BeOfType 'System.Collections.Specialized.OrderedDictionary'
    }

    It "Don't preserve result when no need to do so in case of flow-control exception" {
        function TestFunc1([switch]$p) {
            ## No need to preserve and flush the results from the IF statement to the outer
            ## pipeline, because the results are supposed to be assigned to a variable.
            if ($p) {
                $null = if ($true) { "one"; return "two" }
            } else {
                $a = foreach ($a in 1) { "one"; return; }
            }
        }

        function TestFunc2 {
            ## The results from the sub-expression need to be preserved and flushed to the outer pipeline.
            $("1";return "2")
        }

        TestFunc1 | Should -Be $null
        TestFunc1 -p | Should -Be $null

        TestFunc2 | Should -Be @("1", "2")
    }

    It "'[NullString]::Value' should be treated as string type when resolving .NET method" {
        $testType = 'NullStringTest' -as [type]
        if (-not $testType) {
            Add-Type -TypeDefinition @'
using System;
public class NullStringTest {
    public static string Test(bool argument)
    {
        return "bool";
    }

    public static string Test(string argument)
    {
        return "string";
    }

    public static string Get<T>(T argument)
    {
        string ret = typeof(T).FullName;
        if (argument is string[] array)
        {
            if (array[1] == null)
            {
                return ret + "; 2nd element is NULL";
            }
        }

        return ret;
    }
}
'@
        }

        [NullStringTest]::Test([NullString]::Value) | Should -BeExactly 'string'
        [NullStringTest]::Get([NullString]::Value) | Should -BeExactly 'System.String'
        [NullStringTest]::Get(@('foo', [NullString]::Value, 'bar')) | Should -BeExactly 'System.String[]; 2nd element is NULL'
    }

    It 'Non-default encoding should work in PowerShell' {
        $powershell = Join-Path -Path $PSHOME -ChildPath "pwsh"
        $result = & $powershell -noprofile -c '[System.Text.Encoding]::GetEncoding("IBM437").WebName'
        $result | Should -BeExactly "ibm437"
    }

    It 'Can set DataRow with PSObject through adapter and indexer' {
        $dataTable = [Data.DataTable]::new()
        $null = $dataTable.Columns.Add('Date', [DateTime])
        $row = $dataTable.Rows.Add((Get-Date))

        $date1 = Get-Date
        $row.Date = $date1
        $row.Date.Ticks | Should -Be $date1.Ticks

        $date2 = Get-Date
        $row['Date'] = $date2
        $row.Date.Ticks | Should -Be $date2.Ticks
    }

    It 'Can set DataRowView with PSObject through adapter and indexer' {
        $dataTable = [Data.DataTable]::new()
        $null = $dataTable.Columns.Add('Date', [DateTime])
        $dataTable.Rows.Add((Get-Date))

        $dataView = [System.Data.DataView]::new($dataTable)
        $rowView = $dataView[0]

        $date1 = Get-Date
        $rowView.Date = $date1
        $rowView.Date.Ticks | Should -Be $date1.Ticks

        $date2 = Get-Date
        $rowView['Date'] = $date2
        $rowView.Date.Ticks | Should -Be $date2.Ticks
    }
}

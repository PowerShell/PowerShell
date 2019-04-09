# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Error position Tests" -Tags "CI" {

    BeforeAll {
        $switch_condition_script = @'
$test = 1
switch ($null[0]) {
    "a" {};
}
'@
        $for_expression_initializer_script = @'
$test = 1
for ($null[0];
     $test -gt 1;) { }
'@
        $for_pipeline_initializer_script = @'
$test = 1
for (Test-Path $null -ErrorAction Stop;
     $test -gt 1;) { }
'@
        $for_expression_condition_script = @'
$test = 1
for (;$null[0];)
{ }
'@
        $for_pipeline_condition_script = @'
$test = 1
for (;Test-Path $null -ErrorAction Stop;)
{ }
'@
        $do_while_expression_condition_script = @'
$test = 1
do {}
while ($null[0])
'@
        $do_while_pipeline_condition_script = @'
$test = 1
do {}
while (Test-Path $null -ErrorAction Stop)
'@
        $do_until_expression_condition_script = @'
$test = 1
do {}
until ($null[0])
'@
        $do_until_pipeline_condition_script = @'
$test = 1
do {}
until (Test-Path $null -ErrorAction Stop)
'@

        $testCases = @(
            @{ Name = "switch condition";                        FileName = "SwitchError2.ps1";  Script = $switch_condition_script;              MatchText = "SwitchError2.ps1: line 2" }
            @{ Name = "for statement expression initializer";    FileName = "ForError1.ps1";     Script = $for_expression_initializer_script;    MatchText = "ForError1.ps1: line 2" }
            @{ Name = "for statement pipeline initializer";      FileName = "ForError2.ps1";     Script = $for_pipeline_initializer_script;      MatchText = "ForError2.ps1: line 2" }
            @{ Name = "for statement expression condition";      FileName = "ForError3.ps1";     Script = $for_expression_condition_script;      MatchText = "ForError3.ps1: line 2" }
            @{ Name = "for statement pipeline condition";        FileName = "ForError4.ps1";     Script = $for_pipeline_condition_script;        MatchText = "ForError4.ps1: line 2" }
            @{ Name = "do-while statement expression condition"; FileName = "DoWhileError1.ps1"; Script = $do_while_expression_condition_script; MatchText = "DoWhileError1.ps1: line 3" }
            @{ Name = "do-while statement pipeline condition";   FileName = "DoWhileError2.ps1"; Script = $do_while_pipeline_condition_script;   MatchText = "DoWhileError2.ps1: line 3" }
            @{ Name = "do-until statement expression condition"; FileName = "DoUntilError1.ps1"; Script = $do_until_expression_condition_script; MatchText = "DoUntilError1.ps1: line 3" }
            @{ Name = "do-until statement pipeline condition";   FileName = "DoUntilError2.ps1"; Script = $do_until_pipeline_condition_script;   MatchText = "DoUntilError2.ps1: line 3" }
        )
    }

    It "<Name> evaluation failure should report correct error position" -TestCases $testCases {
        param($FileName, $Script, $MatchText)
        $testFile = Join-Path $TestDrive $FileName
        Set-Content -Path $testFile -Encoding Ascii -Value $Script
        try { & $testFile } catch { $errorRecord = $_ }
        $errorRecord | Should -Not -BeNullOrEmpty
        $errorRecord.ScriptStackTrace | Should -Match $MatchText
    }

    It "switch condition MoveNext failure should report correct error position" {
        $code = @'
using System;
using System.Collections.Generic;
namespace SwitchTest
{
    public class Test
    {
        public static IEnumerable<string> GetName()
        {
            yield return "Hello world";
            throw new ArgumentException();
        }
    }
}
'@
        $testFile = Join-Path $TestDrive "SwitchError1.ps1"
        Set-Content -Path $testFile -Encoding Ascii -Value @'
$test = 1
$enumerable = [SwitchTest.Test]::GetName()
switch ($enumerable) {
    "hello world" { $test = 1; $_ }
    "Yay" { $test = 2; $_ }
}
'@
        if (-not ("SwitchTest.Test" -as [type])) {
            Add-Type -TypeDefinition $code
        }

        try { & $testFile > $null } catch { $errorRecord = $_ }
        $errorRecord | Should -Not -BeNullOrEmpty
        $errorRecord.ScriptStackTrace | Should -Match "SwitchError1.ps1: line 3"
    }
}

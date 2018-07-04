# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Error position Tests" -Tags "CI" {
    It "switch condition evaluation failure should report correct error position" {
        $testFile = Join-Path $TestDrive "SwitchError1.ps1"
        Set-Content -Path $testFile -Encoding Ascii -Value @'
$test = 1
switch ($null[0]) {
    "a" {};
}
'@
        try { & $testFile } catch { $errorRecord = $_ }
        $errorRecord | Should -Not -BeNullOrEmpty
        $errorRecord.ScriptStackTrace | Should -Match "SwitchError1.ps1: line 2"
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
        $testFile = Join-Path $TestDrive "SwitchError2.ps1"
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
        $errorRecord.ScriptStackTrace | Should -Match "SwitchError2.ps1: line 3"
    }

    It "for statement initializer evaluation failure should report correct error position" {
        $testFile = Join-Path $TestDrive "ForError1.ps1"
        Set-Content -Path $testFile -Encoding Ascii -Value @'
$test = 1
for ($null[0];
     0 -gt 1;) { }
'@
        try { & $testFile } catch { $errorRecord = $_ }
        $errorRecord | Should -Not -BeNullOrEmpty
        $errorRecord.ScriptStackTrace | Should -Match "ForError1.ps1: line 2"
    }
}

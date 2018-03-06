# Copyright (c) Microsoft Corporation. All rights reserved.
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
        $arraylist.Count | Should -BeExactly  1
        $arraylist[0] | Should -BeExactly  1
    }

    ## void method call
    It "'`$arraylist2.Clear() | <Cmdlet>' should work and behave like passing AutomationNull to the pipe" {
        $arraylist = [System.Collections.ArrayList]::new()
        $arraylist.Add(1) > $null
        $arraylist.Clear() | ForEach-Object { "YES" } | Should -BeNullOrEmpty
        ## $arraylist.Clear() should be executed
        $arraylist.Count | Should -BeExactly  0
    }
}

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Tests for the Import-PowerShellDataFile cmdlet" -Tags "CI" {
    BeforeAll {
        $largePsd1Path = Join-Path -Path $TestDrive -ChildPath 'large.psd1'
        $largePsd1Builder = [System.Text.StringBuilder]::new('@{')
        1..501 | ForEach-Object {
            $largePsd1Builder.Append("key$_ = $_;")
        }
        $largePsd1Builder.Append('}')
        Set-Content -Path $largePsd1Path -Value $largePsd1Builder.ToString()
    }

    It "Validates error on a missing path" {
        { Import-PowerShellDataFile -Path /SomeMissingDirectory -ErrorAction Stop } |
            Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.ImportPowerShellDataFileCommand"
    }

    It "Validates error on a directory" {
        { Import-PowerShellDataFile ${TESTDRIVE} -ErrorAction Stop } |
            Should -Throw -ErrorId "PathNotFound,Microsoft.PowerShell.Commands.ImportPowerShellDataFileCommand"

    }

    It "Generates a good error on an insecure file" {

        $path = Setup -f insecure.psd1 -Content '@{ Foo = Get-Process }' -pass
        { Import-PowerShellDataFile $path -ErrorAction Stop } |
            Should -Throw -ErrorId "System.InvalidOperationException,Microsoft.PowerShell.Commands.ImportPowerShellDataFileCommand"
    }

    It "Generates a good error on a file that isn't a PowerShell Data File (missing the hashtable root)" {
        $path = Setup -f NotAPSDataFile -Content '"Hello World"' -Pass
        { Import-PowerShellDataFile $path -ErrorAction Stop } |
            Should -Throw -ErrorId "CouldNotParseAsPowerShellDataFileNoHashtableRoot,Microsoft.PowerShell.Commands.ImportPowerShellDataFileCommand"
    }

    It "Can parse a PowerShell Data File (detailed tests are in AST.SafeGetValue tests)" {
        $path = Setup -F gooddatafile -Content '@{ "Hello" = "World" }' -pass
        $result = Import-PowerShellDataFile $path -ErrorAction Stop
        $result.Hello | Should -BeExactly "World"
    }

    It 'Fails if psd1 file has more than 500 keys' {
        { Import-PowerShellDataFile $largePsd1Path } | Should -Throw -ErrorId 'System.InvalidOperationException,Microsoft.PowerShell.Commands.ImportPowerShellDataFileCommand'
    }

    It 'Succeeds if -NoLimit is used and has more than 500 keys' {
        $result = Import-PowerShellDataFile $largePsd1Path -SkipLimitCheck
        $result.Keys.Count | Should -Be 501
    }
}

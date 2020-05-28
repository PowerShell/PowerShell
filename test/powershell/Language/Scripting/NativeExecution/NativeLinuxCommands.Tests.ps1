# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "NativeLinuxCommands" -tags "CI" {
    BeforeAll {
        $originalDefaultParams = $PSDefaultParameterValues.Clone()
        $PSDefaultParameterValues["It:Skip"] = $IsWindows
        $originalPath = $env:PATH
        $env:PATH += [IO.Path]::PathSeparator + $TestDrive
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParams
        $env:PATH = $originalPath
    }

    It "Should find Application grep" {
        (Get-Command grep).CommandType | Should -Be Application
    }

    It "Should pipe to grep and get result" {
        "hello world" | grep hello | Should -BeExactly "hello world"
    }

    It "Should find Application touch" {
        (Get-Command touch).CommandType | Should -Be Application
    }

    It "Should not redirect standard input if native command is the first command in pipeline (1)" {
        df | ForEach-Object -Begin { $out = @() } -Process { $out += $_ }
        $out.Length -gt 0 | Should -BeTrue
        $out[0] -like "Filesystem*Available*" | Should -BeTrue
    }

    It "Should not redirect standard input if native command is the first command in pipeline (2)" {
        $out = df
        $out.Length -gt 0 | Should -BeTrue
        $out[0] -like "Filesystem*Available*" | Should -BeTrue
    }

    It "Should find command before script with same name" {
        Set-Content "$TestDrive\foo" -Value @"
#!/usr/bin/env bash
echo 'command'
"@ -Encoding Ascii
        chmod +x "$TestDrive/foo"
        Set-Content "$TestDrive\foo.ps1" -Value @"
'script'
"@ -Encoding Ascii
        foo | Should -BeExactly 'command'
    }
}

Describe "Scripts with extensions" -tags "CI" {
    BeforeAll {
        $data = "Hello World"
        Setup -File testScript.ps1 -Content "'$data'"
        $originalPath = $env:PATH
        $env:PATH += [IO.Path]::PathSeparator + $TestDrive
    }

    AfterAll {
        $env:PATH = $originalPath
    }

    It "Should run a script with its full name" {
        testScript.ps1 | Should -BeExactly $data
    }

    It "Should run a script with its short name" {
        testScript | Should -BeExactly $data
    }
}

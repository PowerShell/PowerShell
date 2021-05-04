# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
#
# Validates Get-Help for *nix manpages.

Import-Module HelpersCommon

Describe "Get-Help should find *nix manpage" -Tags "CI" {
    BeforeAll {
        $isManpageSupported = $IsLinux
        $command1 = "whatis"
        $command1NoEnd = "whati"
        $command1NIXName = "whatis (1)"
    }

    AfterAll {
    }

    It "Get-Help should find manpage" -Skip:(!$isManpageSupported) {
        (Get-Help $command1).Name | Should -BeExactly $command1NIXName
    }

    It "help should find manpage" -Skip:(!$isManpageSupported) {
        help $command1 | Should -Contain "    $command1NIXName"
    }

    It "Get-Help should find manpage with ? pattern" -Skip:(!$isManpageSupported) {
        (Get-Help $command1NoEnd"?").Name | Should -BeExactly $command1NIXName
    }

    It "Get-Help should find manpage with * pattern" -Skip:(!$isManpageSupported) {
        (Get-Help $command1NoEnd"*").Name | Should -BeExactly $command1NIXName
    }
}

Describe "Get-Help should include *nix in list" -Tags "CI" {
    BeforeAll {
        $isManpageSupported = $IsLinux
        $command2 = "echo"
        $command2PSName = "Write-Output"
        $command2NIXName = "echo (1)"
    }

    AfterAll {
    }

    It "Get-Help for PS command should show the PS help" -Skip:(!$isManpageSupported) {
        $help = Get-Help $command2
        $help.Synopsis | Should -Match $command2PSName
    }

    It "Get-Help for wildcard command should show both PS and *nix commands in the list" -Skip:(!$isManpageSupported) {
        $help = Get-Help $command2"*"
        $help.Name | Should -Contain $command2
        $help.Name | Should -Contain $command2NIXName
        $help.Synopsis | Should -Contain $command2PSName
    }
}

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "PSReadLine" -tags "CI" {
    BeforeAll {
        if (Get-Module PSReadLine) {
            $originalEditMode = (Get-PSReadLineOption).EditMode
            Remove-Module PSReadLine
        }
    }

    It "Should import the module correctly" {
        Import-Module PSReadLine
        $module = Get-Module PSReadLine
        $module.Name | Should -BeExactly 'PSReadLine'
        $module.Version | Should -Match '^2.3.\d$'
    }

    It "Should be installed to `$PSHOME" {
        $module = Get-Module (Join-Path -Path $PSHOME -ChildPath "Modules" -AdditionalChildPath "PSReadLine") -ListAvailable
        $module.Name | Should -BeExactly 'PSReadLine'
        $module.Version | Should -Match '^2.3.\d$'
        $module.Path | Should -Be (Join-Path -Path $PSHOME -ChildPath "Modules/PSReadLine/PSReadLine.psd1")
    }

    It "Should use Emacs Bindings on Linux and macOS" -Skip:$IsWindows {
        (Get-PSReadLineOption).EditMode | Should -BeExactly 'Emacs'
        (Get-PSReadLineKeyHandler | Where-Object { $_.Key -eq "Ctrl+A" }).Function | Should -BeExactly 'BeginningOfLine'
    }

    It "Should use Windows Bindings on Windows" -Skip:(-not $IsWindows) {
        (Get-PSReadLineOption).EditMode | Should -BeExactly 'Windows'
        (Get-PSReadLineKeyHandler | Where-Object { $_.Key -eq "Ctrl+a" }).Function | Should -BeExactly 'SelectAll'
    }

    It "Should set the edit mode" {
        Set-PSReadLineOption -EditMode Windows
        (Get-PSReadLineKeyHandler | Where-Object { $_.Key -eq "Ctrl+A" }).Function | Should -BeExactly 'SelectAll'

        Set-PSReadLineOption -EditMode Emacs
        (Get-PSReadLineKeyHandler | Where-Object { $_.Key -eq "Ctrl+A" }).Function | Should -BeExactly 'BeginningOfLine'
    }

    It "Should allow custom bindings for plain keys" {
        Set-PSReadLineKeyHandler -Key '"' -Function SelfInsert
        (Get-PSReadLineKeyHandler | Where-Object { $_.Key -eq '"' }).Function | Should -BeExactly 'SelfInsert'
    }

    It "Should report Capitalized bindings correctly" {
        Set-PSReadLineOption -EditMode Emacs
        (Get-PSReadLineKeyHandler | Where-Object { $_.Key -ceq "Alt+b" }).Function | Should -BeExactly 'BackwardWord'
        (Get-PSReadLineKeyHandler | Where-Object { $_.Key -ceq "Alt+B" }).Function | Should -BeExactly 'SelectBackwardWord'
    }

    It "Should ignore case when using Function binding" {
        $lowerCaseFunctionName = "yank"
        Set-PSReadLineKeyHandler "Ctrl+F24" -Function $lowerCaseFunctionName
        (Get-PSReadLineKeyHandler | Where-Object { $_.Key -eq "Ctrl+F24"}).Function | Should -BeExactly "Yank"
    }

    AfterAll {
        Remove-Module PSReadLine

        if ($originalEditMode) {
            Import-Module PSReadLine
            Set-PSReadLineOption -EditMode $originalEditMode
        }
    }
}

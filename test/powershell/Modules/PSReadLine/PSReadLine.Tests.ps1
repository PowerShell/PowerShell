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
        $module.Name | Should Be "PSReadLine"
        $module.Version | Should Be "1.2"
    }

    It "Should use Emacs Bindings on Linux and OS X" -skip:$IsWindows {
        (Get-PSReadLineOption).EditMode | Should Be Emacs
        (Get-PSReadlineKeyHandler | where { $_.Key -eq "Ctrl+A" }).Function | Should Be BeginningOfLine
    }

    It "Should use Windows Bindings on Windows" -skip:(-not $IsWindows) {
        (Get-PSReadLineOption).EditMode | Should Be Windows
        (Get-PSReadlineKeyHandler | where { $_.Key -eq "Ctrl+a" }).Function | Should Be SelectAll
    }

    It "Should set the edit mode" {
        Set-PSReadlineOption -EditMode Windows
        (Get-PSReadlineKeyHandler | where { $_.Key -eq "Ctrl+A" }).Function | Should Be SelectAll

        Set-PSReadlineOption -EditMode Emacs
        (Get-PSReadlineKeyHandler | where { $_.Key -eq "Ctrl+A" }).Function | Should Be BeginningOfLine
    }

    It "Should allow custom bindings for plain keys" {
        Set-PSReadlineKeyHandler -Key '"' -Function SelfInsert
        (Get-PSReadLineKeyHandler | where { $_.Key -eq '"' }).Function | Should Be SelfInsert
    }

    It "Should report Capitalized bindings correctly" {
        Set-PSReadlineOption -EditMode Emacs
        (Get-PSReadLineKeyHandler | where { $_.Key -ceq "Alt+b" }).Function | Should Be BackwardWord
        (Get-PSReadLineKeyHandler | where { $_.Key -ceq "Alt+B" }).Function | Should Be SelectBackwardWord
    }

    AfterAll {
        Remove-Module PSReadLine

        if ($originalEditMode) {
            Import-Module PSReadLine
            Set-PSReadlineOption -EditMode $originalEditMode
        }
    }
}

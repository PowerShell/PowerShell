Describe "PSReadLine" {
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

    It "Should set the edit mode" {
        Set-PSReadlineOption -EditMode Windows
        (Get-PSReadlineKeyHandler | where { $_.Key -eq "Ctrl+A" }).Function | Should Be SelectAll

        Set-PSReadlineOption -EditMode Emacs
        (Get-PSReadlineKeyHandler | where { $_.Key -eq "Ctrl+A" }).Function | Should Be BeginningOfLine
    }

    AfterAll {
        Remove-Module PSReadLine

        if ($originalEditMode) {
            Import-Module PSReadLine
            Set-PSReadlineOption -EditMode $originalEditMode
        }
    }
}

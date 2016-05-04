Describe "PSReadLine" {

    It "Should import the module correctly" {
        $result = (Import-Module PSReadLine -PassThru)
        $result.Name | Should Be "PSReadLine"
        $result.Version | Should Be "1.2"
    }

    $originalEditMode = (Get-PSReadLineOption).EditMode

    It "Should set the edit mode" {
        Set-PSReadlineOption -EditMode Windows
        (Get-PSReadlineKeyHandler | where { $_.Key -eq "Ctrl+A" }).Function | Should Be SelectAll

        Set-PSReadlineOption -EditMode Emacs
        (Get-PSReadlineKeyHandler | where { $_.Key -eq "Ctrl+A" }).Function | Should Be BeginningOfLine
    }

    Set-PSReadlineOption -EditMode $originalEditMode
}

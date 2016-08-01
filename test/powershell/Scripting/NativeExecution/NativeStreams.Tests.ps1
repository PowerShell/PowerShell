Describe "Native streams behavior with PowerShell" -Tags 'CI' {
    $powershell = Join-Path -Path $PsHome -ChildPath "powershell"

    Context "Error stream" {

        $command = [string]::Join('', @(
            '"[Console]::Error.Write("""foo`n`nbar`n`nbaz"""); ',
            'sleep -Milliseconds 10; ',
            '[Console]::Error.Write("""middle"""); ',
            'sleep -Milliseconds 10; ',
            '[Console]::Error.Write("""foo`n`nbar`n`nbaz""")"'
        ))

        $out = & $powershell -noprofile -command $command 2>&1

        It "uses ErrorRecord object to return stderr output" {
            # there are 4 objects, because of the sleeps
            ($out | measure).Count | Should Be 4
            $out[0].Exception.Message | Should Be 'foo'
            $out[1].Exception.Message | Should Be "`nbar`n`nbaz"
            $out[2].Exception.Message | Should Be "middle"
            $out[3].Exception.Message | Should Be "foo`n`nbar`n`nbaz"
        }

        It "preserves error stream as is, redirected" {
            $out | Out-String | Should Be "foo`n`nbar`n`nbazmiddlefoo`n`nbar`n`nbaz"
        }
    }
}

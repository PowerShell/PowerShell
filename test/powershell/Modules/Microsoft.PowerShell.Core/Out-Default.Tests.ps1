Describe "Out-Default Tests" -tag CI {
    BeforeAll {
        $powershell = "$PSHOME/powershell"
    }

    It "Out-Default reverts transcription state when used more than once in a pipeline" {
        & $powershell -c "Out-Default -Transcript | Out-Default -Transcript; 'Hello'" | Should BeExactly "Hello"
    }

    It "Out-Default reverts transcription state when exception occurs in pipeline" {
        & $powershell -c "try { & { throw } | Out-Default -Transcript } catch {}; 'Hello'" | Should BeExactly "Hello"
    }    
}

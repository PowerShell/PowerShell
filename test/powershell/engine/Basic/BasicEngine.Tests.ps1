Describe 'Basic engine APIs' {
    Context 'powershell::Create' {
        It 'can create default instance' {
            [powershell]::Create() | Should Not Be $null
        }
    }
}

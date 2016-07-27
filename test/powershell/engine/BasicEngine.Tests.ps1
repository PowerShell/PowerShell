Describe 'Basic engine APIs' -Tags "DRT" {
    Context 'powershell::Create' {
        It 'can create default instance' {
            [powershell]::Create() | Should Not Be $null
        }
    }
}

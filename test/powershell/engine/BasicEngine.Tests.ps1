Describe 'Basic engine APIs' -Tags "CI" {
    Context 'powershell::Create' {
        It 'can create default instance' {
            [powershell]::Create() | Should Not Be $null
        }

        It "can load the default snapin 'Microsoft.WSMan.Management'" -skip:(-not $IsWindows) {
            $ps = [powershell]::Create()
            $ps.AddScript("Get-Command -Name Test-WSMan") > $null
            
            $result = $ps.Invoke()
            $result.Count | Should Be 1
            $result[0].PSSnapIn.Name | Should Be "Microsoft.WSMan.Management"
        }
    }
}

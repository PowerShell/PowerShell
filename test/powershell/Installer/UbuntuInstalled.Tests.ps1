Describe 'Ubuntu Post-Install Tests' -Tag 'CI' {
    BeforeAll {
        $skip = $IsWindows

        if (-not $skip)
        {
            $LinuxInfo = Get-Content /etc/os-release -Raw | ConvertFrom-StringData
            $IsUbuntu = $LinuxInfo.ID -match 'ubuntu'
            $skip = -not $IsUbuntu
        }
    }

    It "libgssapi_krb5.so symbolic link exists in the $PSHOME directory" -skip:$skip {
        $expectedPath = Join-Path -Path $PSHOME -ChildPath 'libgssapi_krb5.so'

        Test-Path -path $expectedPath | Should Be $true
        $item = Get-Item -Path $expectedPath
        $item.LinkType | Should Be 'SymbolicLink'
    }
}

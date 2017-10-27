Describe 'Ubuntu/Debian Post-Install Tests' -Tag 'CI' {
    BeforeAll {
        $skip_libgssapi = $IsWindows

        if (-not $skip_libgssapi)
        {
            $LinuxInfo = Get-Content /etc/os-release -Raw | ConvertFrom-StringData
            $IsUbuntu = $LinuxInfo.ID -match 'ubuntu'
            $IsDebian = $LinuxInfo.ID -match 'debian'
            $skip_libgssapi = -not ($IsUbuntu -or $IsDebian)
        }
    }

    It "libgssapi_krb5.so symbolic link exists in the $PSHOME directory" -skip:$skip_libgssapi {
        $expectedPath = Join-Path -Path $PSHOME -ChildPath 'libgssapi_krb5.so'

        Test-Path -path $expectedPath | Should Be $true
        $item = Get-Item -Path $expectedPath
        $item.LinkType | Should Be 'SymbolicLink'
    }
}

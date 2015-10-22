Describe "Test-Get-Location aka pwd" {
    <#Dependencies:
        pushd
        popd
        $HOME

    #>
    $winHome = 'C:\Users\v-zafolw'
    $nixHome = '/home/zafolw'
    BeforeEach {
        pushd $HOME #on windows, this is c:\Users\XXXXX; for *nix, it's /home/XXXXX  
    }

    AfterEach { popd }

    It "Should list the output of the current working directory" {
        (Get-Location).Path | Should Not BeNullOrEmpty
        (Get-Location).Path | Should Be ($winHome -or $nixHome)
    }

    It "Should be able to use pwd the same way" {
        (pwd).Path | Should Not BeNullOrEmpty
        (pwd).Path | Should Be ($winHome -or $nixHome)
    }
}

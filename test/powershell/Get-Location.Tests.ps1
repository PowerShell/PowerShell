Describe "Get-Location" {
    BeforeEach {
	pushd $env:HOME
    }

    AfterEach {
	popd
    }

    It "Should list the output of the current working directory" {
	(Get-Location).Path | Should Be $env:HOME
    }

    It "Should do exactly the same thing as its alias" {
	(pwd).Path | Should Be (Get-Location).Path
    }
}

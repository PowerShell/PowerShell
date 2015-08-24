Describe "Test-Environment-Variables" {
    It "Should have environment variables" {
        Get-Item ENV: | Should Not BeNullOrEmpty
    }

    It "Should be able to access the members of the environment variable" {
        $expected = /bin/bash -c "cd ~ && pwd"

        (Get-Item ENV:HOME).Value     | Should Be $expected
        (Get-Item ENV:HOSTNAME).Value | Should Not BeNullOrEmpty
        (Get-Item ENV:PATH).Value     | Should Not BeNullOrEmpty
    }

    It "Should be able to set the environment variables" {
        { $ENV:TESTENVIRONMENTVARIABLE = "this is a test environment variable" } | Should Not Throw

        $ENV:TESTENVIRONMENTVARIABLE | Should Not BeNullOrEmpty
        $ENV:TESTENVIRONMENTVARIABLE | Should Be "this is a test environment variable"

    }
}

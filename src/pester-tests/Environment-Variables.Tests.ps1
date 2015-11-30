Describe "Environment-Variables" {
    It "Should have environment variables" {
        Get-Item ENV: | Should Not BeNullOrEmpty
    }

    It "Should have a nonempty PATH" {
        $ENV:PATH | Should Not BeNullOrEmpty
    }

    It "Should contain /bin in the PATH" {
        if ($ENV:TEMP -eq "/tmp" )
        {
            $ENV:PATH | Should Match "/bin"
        }
        else
        {
            $ENV:PATH | Should Match "C:"
        }
    }

    It "Should have the correct HOME" {
        if ($ENV:TEMP -eq "/tmp" )
        {
            $expected = /bin/bash -c "cd ~ && pwd"
            $ENV:HOME | Should Be $expected
        }
        else
        {
            $expected = "\Users\" + $ENV:USERNAME
            $ENV:HOMEPATH | Should Be $expected
        }
    }

    It "Should be able to set the environment variables" {
        $expected = "this is a test environment variable"
        { $ENV:TESTENVIRONMENTVARIABLE = $expected  } | Should Not Throw

        $ENV:TESTENVIRONMENTVARIABLE | Should Not BeNullOrEmpty
        $ENV:TESTENVIRONMENTVARIABLE | Should Be $expected

    }
}

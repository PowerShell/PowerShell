Describe "Environment-Variables" {
    $isWindows = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)

    It "Should have environment variables" {
        Get-Item ENV: | Should Not BeNullOrEmpty
    }

    It "Should have a nonempty PATH" {
        $ENV:PATH | Should Not BeNullOrEmpty
    }

    It "Should contain /bin in the PATH" {
        if ($isWindows)
        {
            $ENV:PATH | Should Match "C:"
        }
        else
        {
            $ENV:PATH | Should Match "/bin"
        }
    }

    It "Should have the correct HOME" {
        if ($isWindows)
        {
            $expected = "\Users\" + $ENV:USERNAME
        }
        else
        {
            $expected = /bin/bash -c "cd ~ && pwd"
        }
            $ENV:HOME | Should Be $expected
    }

    It "Should be able to set the environment variables" {
        $expected = "this is a test environment variable"
        { $ENV:TESTENVIRONMENTVARIABLE = $expected  } | Should Not Throw

        $ENV:TESTENVIRONMENTVARIABLE | Should Not BeNullOrEmpty
        $ENV:TESTENVIRONMENTVARIABLE | Should Be $expected

    }
}

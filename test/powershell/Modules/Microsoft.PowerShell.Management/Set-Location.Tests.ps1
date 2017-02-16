Describe "Set-Location" -Tags "CI" {

    BeforeAll {
        $startDirectory = Get-Location

        if ($IsWindows)
        {
            $target = "C:\"
        }
        else
        {
            $target = "/"
        }
    }

    AfterAll {
        Set-Location $startDirectory
    }

    It "Should be able to be called without error" {
        { Set-Location $target }    | Should Not Throw
    }

    It "Should be able to be called on different providers" {
        { Set-Location alias: } | Should Not Throw
        { Set-Location env: }   | Should Not Throw
    }

    It "Should have the correct current location when using the set-location cmdlet" {
        Set-Location $startDirectory

        $(Get-Location).Path | Should Be $startDirectory.Path
    }

    It "Should be able to use the Path switch" {
        { Set-Location -Path $target } | Should Not Throw
    }

    It "Should generate a pathinfo object when using the Passthru switch" {
        $result = Set-Location $target -PassThru
        $result | Should BeOfType System.Management.Automation.PathInfo
    }

    Context 'Set-Location with no arguments' {

        It 'Should go to $env:HOME when Set-Location run with no arguments from FileSystem provider' {
            Set-Location 'TestDrive:\'
            Set-Location
            (Get-Location).Path | Should Be (Get-PSProvider FileSystem).Home
        }

        It 'Should go to $env:HOME when Set-Location run with no arguments from Env: provider' {
            Set-Location 'Env:'
            Set-Location
            (Get-Location).Path | Should Be (Get-PSProvider FileSystem).Home
        }
    }
}

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

    Context 'Set-Location with last location history' {
        
        It 'Should go to last location when specifying minus as a path' {
            $initialLocation = Get-Location
            Set-Location ([System.IO.Path]::GetTempPath())
            Set-Location -
            (Get-Location).Path | Should Be ($initialLocation).Path
        }

        It 'Should go back to previous locations when specifying minus twice' {
            $initialLocation = (Get-Location).Path
            Set-Location ([System.IO.Path]::GetTempPath())
            $firstLocationChange = (Get-Location).Path
            Set-Location ([System.Environment]::GetFolderPath("user"))
            Set-Location -
            (Get-Location).Path | Should Be $firstLocationChange
            Set-Location -
            (Get-Location).Path | Should Be $initialLocation
        }

        It 'Location History is limited' {
            $initialLocation = (Get-Location).Path
            $maximumLocationHistory = 1000
            foreach ($i in 1..$maximumLocationHistory) {
                Set-Location ([System.IO.Path]::GetTempPath())
            }
            foreach ($i in 1..$maximumLocationHistory) {
                Set-Location -
            }
            (Get-Location).Path | Should Be $initialLocation
            { Set-Location - } | ShouldBeErrorId 'System.InvalidOperationException,Microsoft.PowerShell.Commands.SetLocationCommand'
        }
    }
}

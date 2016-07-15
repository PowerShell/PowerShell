Describe "ExecutionPolicy" {

    Context "Check Get-ExecutionPolicy behavior" {
        It "Should unrestricted when not on Windows" -Skip:$IsWindows {
            Get-ExecutionPolicy | Should Be Unrestricted
        }

        It "Should return Microsoft.Powershell.ExecutionPolicy PSObject on Windows" -Skip:($IsLinux -Or $IsOSX) {
            (Get-ExecutionPolicy).GetType() | Should Be Microsoft.Powershell.ExecutionPolicy
        }
    }

    Context "Check Set-ExecutionPolicy behavior" {
        It "Should throw PlatformNotSupported when not on Windows" -Skip:$IsWindows {
            { Set-ExecutionPolicy Unrestricted } | Should Throw "Operation is not supported on this platform."
        }

        It "Should succeed on Windows" -Skip:($IsLinux -Or $IsOSX) {
            # We use the Process scope to avoid affecting the system
            # Unrestricted is assumed "safe", otherwise these tests would not be running
            { Set-ExecutionPolicy -Force -Scope Process -ExecutionPolicy Unrestricted } | Should Not Throw
        }
    }
}

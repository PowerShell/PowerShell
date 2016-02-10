Describe "ExecutionPolicy" {
    # We check against Windows because Linux and OS X behavior is the same
    $isWindows = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)

    Context "Check Get-ExecutionPolicy behavior" {
	It "Should throw PlatformNotSupported when not on Windows" {
	    if (!$isWindows) {
		{ Get-ExecutionPolicy } | Should Throw "Operation is not supported on this platform."
	    }
	}

	It "Should return Microsoft.Powershell.ExecutionPolicy PSObject on Windows" {
	    if ($isWindows) {
		(Get-ExecutionPolicy).GetType() | Should Be Microsoft.Powershell.ExecutionPolicy
	    }
	}
    }

    Context "Check Set-ExecutionPolicy behavior" {
	It "Should throw PlatformNotSupported when not on Windows" {
	    if (!$isWindows) {
		{ Set-ExecutionPolicy Unrestricted } | Should Throw "Operation is not supported on this platform."
	    }
	}

	It "Should succeed on Windows" {
	    if ($isWindows) {
		# We use the Process scope to avoid affecting the system
		# Unrestricted is assumed "safe", otherwise these tests would not be running
		{ Set-ExecutionPolicy -Force -Scope Process -ExecutionPolicy Unrestricted } | Should Not Throw
	    }
	}
    }
}

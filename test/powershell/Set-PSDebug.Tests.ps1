Describe "Set-PSDebug" {
    # Because it is running through pester, no functions need to be called.  Pester should provide plenty
    # of output.
    It "Should be able to be called without error" {
	{ Set-PSDebug -Trace 0 } | Should Not Throw
    }

    It "Should be able to be turned off without error" {
	{ Set-PSDebug -Off } | Should Not Throw
    }

    Context "Validate functionality" {
	BeforeEach {
	    Set-PSDebug -Off
	}

	It "Should be able to go through the tracing options" {
	    { Set-PSDebug -Trace 0 } | Should Not Throw
	    { Set-PSDebug -Trace 1 } | Should Not Throw
	    { Set-PSDebug -Trace 2 } | Should Not Throw
	}

	It "Should be able to set strict" {
	    { Set-PSDebug -Strict } | Should Not Throw
	}
    }

    # final cleanup

    Set-PSDebug -Off
}

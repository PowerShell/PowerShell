Describe "Set-PSDebug" {
    Context "Tracing can be used" {
        AfterEach {
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
}

Describe "Add-Type" {
    It "Should not throw given a simple class definition" {
	{ Add-Type -TypeDefinition "public static class foo { }" } | Should Not Throw
    }
}

Describe "Add-Type" {
    It -Pending "Should not throw given a simple class definition" {
        { Add-Type -TypeDefinition "public static class foo { }" } | Should Not Throw
    }
}

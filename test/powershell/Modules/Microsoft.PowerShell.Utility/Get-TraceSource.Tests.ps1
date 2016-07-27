Describe "Get-TraceSource" -Tags "Feature" {
    It "Should output data sorted by name" {
        $expected = (Get-TraceSource | Sort-Object Name)
        Get-TraceSource | Should be $expected
    }
}

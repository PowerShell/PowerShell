Describe "Get-Host DRT Unit Tests" -Tags "CI" {
    It "Should works proper with get-host" {
        $results = Get-Host
        $results | Should Be $Host
        $results.PSObject.TypeNames[0] | Should Be "System.Management.Automation.Internal.Host.InternalHost"
    }
}

Describe "PSVersionTable" {
    It "Should have version table entries" {
        $PSVersionTable.Count | Should BeGreaterThan 6
    }

    It "Should have the right version table entries" {

        $PSVersionTable.ContainsKey("PSVersion")                 | Should Be True
        $PSVersionTable.ContainsKey("WSManStackVersion")         | Should Be True
        $PSVersionTable.ContainsKey("SerializationVersion")      | Should Be True
        $PSVersionTable.ContainsKey("CLRVersion")                | Should Be True
        $PSVersionTable.ContainsKey("BuildVersion")              | Should Be True
        $PSVersionTable.ContainsKey("PSCompatibleVersions")      | Should Be True
        $PSVersionTable.ContainsKey("PSRemotingProtocolVersion") | Should Be True

    }
}

Describe "PSVersionTable" -Tags "CI" {
    It "Should have version table entries" {
	$PSVersionTable.Count | Should Be 9
    }

    It "Should have the right version table entries" {
	$PSVersionTable.ContainsKey("PSVersion")                 | Should Be True
	$PSVersionTable.ContainsKey("PSEdition")                 | Should Be True
	$PSVersionTable.ContainsKey("WSManStackVersion")         | Should Be True
	$PSVersionTable.ContainsKey("SerializationVersion")      | Should Be True
	$PSVersionTable.ContainsKey("CLRVersion")                | Should Be True
	$PSVersionTable.ContainsKey("BuildVersion")              | Should Be True
	$PSVersionTable.ContainsKey("PSCompatibleVersions")      | Should Be True
	$PSVersionTable.ContainsKey("PSRemotingProtocolVersion") | Should Be True
	$PSVersionTable.ContainsKey("GitCommitId")               | Should Be True

    }
    It "GitCommitId property should not contain an error" {
        $PSVersionTable.GitCommitId | Should not match "powershell.version"
    }

    It "Should have the correct edition" -Skip:(!$IsCoreCLR) {
	$PSVersionTable["PSEdition"] | Should Be "Core"
    }
}

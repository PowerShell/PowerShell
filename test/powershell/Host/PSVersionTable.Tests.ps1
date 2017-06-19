Describe "PSVersionTable" -Tags "CI" {
    It "Should have version table entries" {
       $PSVersionTable.Count | Should Be 9
    }

    It "Should have the right version table entries" {
       $PSVersionTable.ContainsKey("PSVersion")                 | Should Be True
       $PSVersionTable.ContainsKey("PSEdition")                 | Should Be True
       $PSVersionTable.ContainsKey("WSManStackVersion")         | Should Be True
       $PSVersionTable.ContainsKey("SerializationVersion")      | Should Be True
       $PSVersionTable.ContainsKey("PSCompatibleVersions")      | Should Be True
       $PSVersionTable.ContainsKey("PSRemotingProtocolVersion") | Should Be True
       $PSVersionTable.ContainsKey("GitCommitId")               | Should Be True
       $PSVersionTable.ContainsKey("Platform")                  | Should Be True
       $PSVersionTable.ContainsKey("OS")                        | Should Be True

    }
    It "GitCommitId property should not contain an error" {
       $PSVersionTable.GitCommitId | Should not match "powershell.version"
    }

    It "Should have the correct platform info" {
       $platform = [String][System.Environment]::OSVersion.Platform
	   [String]$PSVersionTable["Platform"] | Should Be $platform
    }

    It "Should have the correct OS info" {
       if ($IsCoreCLR)
       {
           $OSDescription = [String][System.Runtime.InteropServices.RuntimeInformation]::OSDescription
	       [String]$PSVersionTable["OS"] | Should Be $OSDescription
       }
       else
       {
           $OSDescription = [String][System.Environment]::OSVersion
           [String]$PSVersionTable["OS"] | Should Be $OSDescription
       }
    }

    It "Verify `$PSVersionTable.PSEdition" {
        if ($isCoreCLR) {
            $edition = "Core"
        }
        else
        {
            $edition = "Desktop"
        }
        $PSVersionTable["PSEdition"] | Should Be $edition
    }

    It "Verify `$PSVersionTable is ordered and 'PSVersion' is on first place" {
        $PSVersionName = "PSVersion"
        $keys1 = ($PSVersionTable | Format-Table -HideTableHeaders -Property Name | Out-String) -split [System.Environment]::NewLine | Where-Object {$_} | ForEach-Object {$_.Trim()}

        $keys1[0] | Should Be "PSVersion"
        $keys1[1] | Should Be "PSEdition"

        $keys1last = $keys1[2..($keys1.length-1)]
        $keys1sortedlast = $keys1last | Sort-Object

        Compare-Object -ReferenceObject $keys1last -DifferenceObject $keys1sortedlast -SyncWindow 0 | Should Be $null
    }

    It "Verify `$PSVersionTable can be formatted correctly when it has non-string key" {
        try {
            $key = Get-Item $PSScriptRoot
            $PSVersionTable.Add($key, "TEST")
            { $PSVersionTable | Format-Table } | Should Not Throw
        } finally {
            $PSVersionTable.Remove($key)
        }
    }

    It "Verify `$PSVersionTable can be formatted correctly when 'PSVersion' is removed" {
        try {
            $VersionValue = $PSVersionTable["PSVersion"]
            $PSVersionTable.Remove("PSVersion")

            $keys1 = ($PSVersionTable | Format-Table -HideTableHeaders -Property Name | Out-String) -split [System.Environment]::NewLine | Where-Object {$_} | ForEach-Object {$_.Trim()}
            $keys1[0] | Should Be "PSEdition"
            $keys1.Length | Should Be $PSVersionTable.Count

            $keys1last = $keys1[1..($keys1.length-1)]
            $keys1sortedlast = $keys1last | Sort-Object
            Compare-Object -ReferenceObject $keys1last -DifferenceObject $keys1sortedlast -SyncWindow 0 | Should Be $null
        } finally {
            $PSVersionTable.Add("PSVersion", $VersionValue)
        }
    }

    It "Verify `$PSVersionTable can be formatted correctly when 'PSEdition' is removed" {
        try {
            $EditionValue = $PSVersionTable["PSEdition"]
            $PSVersionTable.Remove("PSEdition")

            $keys1 = ($PSVersionTable | Format-Table -HideTableHeaders -Property Name | Out-String) -split [System.Environment]::NewLine | Where-Object {$_} | ForEach-Object {$_.Trim()}
            $keys1[0] | Should Be "PSVersion"
            $keys1.Length | Should Be $PSVersionTable.Count

            $keys1last = $keys1[1..($keys1.length-1)]
            $keys1sortedlast = $keys1last | Sort-Object
            Compare-Object -ReferenceObject $keys1last -DifferenceObject $keys1sortedlast -SyncWindow 0 | Should Be $null
        } finally {
            $PSVersionTable.Add("PSEdition", $EditionValue)
        }
    }

    It "Verify `$PSVersionTable can be formatted correctly when both 'PSEdition' and 'PSVersion' are removed" {
        try {
            $VersionValue = $PSVersionTable["PSVersion"]
            $EditionValue = $PSVersionTable["PSEdition"]
            $PSVersionTable.Remove("PSVersion")
            $PSVersionTable.Remove("PSEdition")

            $keys1 = ($PSVersionTable | Format-Table -HideTableHeaders -Property Name | Out-String) -split [System.Environment]::NewLine | Where-Object {$_} | ForEach-Object {$_.Trim()}
            $keys1.Length | Should Be $PSVersionTable.Count

            $keys1sortedlast = $keys1 | Sort-Object
            Compare-Object -ReferenceObject $keys1 -DifferenceObject $keys1sortedlast -SyncWindow 0 | Should Be $null
        } finally {
            $PSVersionTable.Add("PSVersion", $VersionValue)
            $PSVersionTable.Add("PSEdition", $EditionValue)
        }
    }
}

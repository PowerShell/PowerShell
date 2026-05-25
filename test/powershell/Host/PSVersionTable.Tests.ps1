# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "PSVersionTable" -Tags "CI" {

    BeforeAll {
        Set-StrictMode -Version 3
        $sma = Get-Item (Join-Path $PSHOME "System.Management.Automation.dll")
        $formattedVersion = $sma.VersionInfo.ProductVersion

        function Get-ExpectedGitCommitIds
        {
            param([string]$ProductVersion)

            $versionAndSha = $ProductVersion -split " SHA: ", 2
            $version = $versionAndSha[0]
            $sourceRevision = if ($versionAndSha.Count -eq 2) { ($versionAndSha[1] -split "\+", 2)[0] } else { $null }

            if ($ProductVersion.Contains(" Commits: "))
            {
                return @($version.Replace(" Commits: ", "-") + "-g$sourceRevision")
            }

            $expectedGitCommitIds = @($version)
            if ($null -ne $sourceRevision)
            {
                $expectedGitCommitIds += "$version-0-g$sourceRevision"
            }

            return $expectedGitCommitIds
        }

        $mainVersionPattern = "(\d+\.\d+\.\d+)(-.+)?"
        $expectedPSVersion = ($formattedVersion -split " ")[0]
        $expectedVersionPattern = "^$mainVersionPattern$"
        $expectedGitCommitIds = Get-ExpectedGitCommitIds -ProductVersion $formattedVersion
    }

    It "Should have version table entries" {
       $PSVersionTable.Count | Should -Be 9
    }

    It "Should have the right version table entries" {
       $PSVersionTable.ContainsKey("PSVersion")                 | Should -BeTrue
       $PSVersionTable.ContainsKey("PSEdition")                 | Should -BeTrue
       $PSVersionTable.ContainsKey("WSManStackVersion")         | Should -BeTrue
       $PSVersionTable.ContainsKey("SerializationVersion")      | Should -BeTrue
       $PSVersionTable.ContainsKey("PSCompatibleVersions")      | Should -BeTrue
       $PSVersionTable.ContainsKey("PSRemotingProtocolVersion") | Should -BeTrue
       $PSVersionTable.ContainsKey("GitCommitId")               | Should -BeTrue
       $PSVersionTable.ContainsKey("Platform")                  | Should -BeTrue
       $PSVersionTable.ContainsKey("OS")                        | Should -BeTrue

    }

    It "PSVersion property" {
       $PSVersionTable.PSVersion | Should -BeOfType System.Management.Automation.SemanticVersion
       $PSVersionTable.PSVersion | Should -BeExactly $expectedPSVersion
       $PSVersionTable.PSVersion | Should -Match $expectedVersionPattern
       $PSVersionTable.PSVersion.Major | Should -Be 7
    }

    It "GitCommitId property" {
       $PSVersionTable.GitCommitId | Should -BeOfType System.String
       $expectedGitCommitIds.Contains($PSVersionTable.GitCommitId) | Should -BeTrue
    }

    It "GitCommitId property accepts tag builds with zero commits" {
       $expected = @(
           "7.6.2"
           "7.6.2-0-g0d3c290a8737d4252ee75c052d16190ffcdb7d19"
       )
       $actual = Get-ExpectedGitCommitIds -ProductVersion "7.6.2 SHA: 0d3c290a8737d4252ee75c052d16190ffcdb7d19"
       Compare-Object -ReferenceObject $expected -DifferenceObject $actual | Should -Be $null
    }

    It "Should have the correct platform info" {
       $platform = [String][System.Environment]::OSVersion.Platform
       [String]$PSVersionTable["Platform"] | Should -Be $platform
    }

    It "Should have the correct OS info" {
       if ($IsCoreCLR)
       {
           $OSDescription = [String][System.Runtime.InteropServices.RuntimeInformation]::OSDescription
           [String]$PSVersionTable["OS"] | Should -Be $OSDescription
       }
       else
       {
           $OSDescription = [String][System.Environment]::OSVersion
           [String]$PSVersionTable["OS"] | Should -Be $OSDescription
       }
    }

    It "Verify `$PSVersionTable.PSEdition" {
        if ($IsCoreCLR) {
            $edition = "Core"
        }
        else
        {
            $edition = "Desktop"
        }
        $PSVersionTable["PSEdition"] | Should -Be $edition
    }

    It "Verify `$PSVersionTable is ordered and 'PSVersion' is on first place" {
        $PSVersionName = "PSVersion"
        $keys1 = ($PSVersionTable | Format-Table -HideTableHeaders -Property Name | Out-String) -split [System.Environment]::NewLine | Where-Object {$_} | ForEach-Object {$_.Trim()}

        $keys1[0] | Should -Be "PSVersion"
        $keys1[1] | Should -Be "PSEdition"

        $keys1last = $keys1[2..($keys1.length-1)]
        $keys1sortedlast = $keys1last | Sort-Object

        Compare-Object -ReferenceObject $keys1last -DifferenceObject $keys1sortedlast -SyncWindow 0 | Should -Be $null
    }

    It "Verify `$PSVersionTable can be formatted correctly when it has non-string key" {
        try {
            $key = Get-Item $PSScriptRoot
            $PSVersionTable.Add($key, "TEST")
            { $PSVersionTable | Format-Table } | Should -Not -Throw
        } finally {
            $PSVersionTable.Remove($key)
        }
    }

    It "Verify `$PSVersionTable can be formatted correctly when 'PSVersion' is removed" {
        try {
            $VersionValue = $PSVersionTable["PSVersion"]
            $PSVersionTable.Remove("PSVersion")

            $keys1 = ($PSVersionTable | Format-Table -HideTableHeaders -Property Name | Out-String) -split [System.Environment]::NewLine | Where-Object {$_} | ForEach-Object {$_.Trim()}
            $keys1[0] | Should -Be "PSEdition"
            $keys1.Length | Should -Be $PSVersionTable.Count

            $keys1last = $keys1[1..($keys1.length-1)]
            $keys1sortedlast = $keys1last | Sort-Object
            Compare-Object -ReferenceObject $keys1last -DifferenceObject $keys1sortedlast -SyncWindow 0 | Should -Be $null
        } finally {
            $PSVersionTable.Add("PSVersion", $VersionValue)
        }
    }

    It "Verify `$PSVersionTable can be formatted correctly when 'PSEdition' is removed" {
        try {
            $EditionValue = $PSVersionTable["PSEdition"]
            $PSVersionTable.Remove("PSEdition")

            $keys1 = ($PSVersionTable | Format-Table -HideTableHeaders -Property Name | Out-String) -split [System.Environment]::NewLine | Where-Object {$_} | ForEach-Object {$_.Trim()}
            $keys1[0] | Should -Be "PSVersion"
            $keys1.Length | Should -Be $PSVersionTable.Count

            $keys1last = $keys1[1..($keys1.length-1)]
            $keys1sortedlast = $keys1last | Sort-Object
            Compare-Object -ReferenceObject $keys1last -DifferenceObject $keys1sortedlast -SyncWindow 0 | Should -Be $null
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
            $keys1.Length | Should -Be $PSVersionTable.Count

            $keys1sortedlast = $keys1 | Sort-Object
            Compare-Object -ReferenceObject $keys1 -DifferenceObject $keys1sortedlast -SyncWindow 0 | Should -Be $null
        } finally {
            $PSVersionTable.Add("PSVersion", $VersionValue)
            $PSVersionTable.Add("PSEdition", $EditionValue)
        }
    }

    Context "PSCompatibleVersions property" {
        It "Is of type System.Version[]" {
            Should -ActualValue $PSVersionTable.PSCompatibleVersions -BeOfType System.Version[]
        }

        It "Is sorted in ascending order" {
            $array = $PSVersionTable.PSCompatibleVersions
            [array]::Sort($array)

            $PSVersionTable.PSCompatibleVersions | Should -Be $array
        }

        It "Has no unexpected items present" {
            $expectedItems = @(
                [version]::new(1, 0)
                [version]::new(2, 0)
                [version]::new(3, 0)
                [version]::new(4, 0)
                [version]::new(5, 0)
                [version]::new(5, 1)
                [version]::new(6, 0)
                [version]::new(7, 0)
            )

            Compare-Object $expectedItems $PSVersionTable.PSCompatibleVersions | Should -Be $null
        }
    }
}

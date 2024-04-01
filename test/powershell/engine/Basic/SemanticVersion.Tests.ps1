# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
using namespace System.Management.Automation
using namespace System.Management.Automation.Language

Describe "SemanticVersion api tests" -Tags 'CI' {
    Context "Constructing valid versions" {
        It "String argument constructor" {
            $v = [SemanticVersion]::new("1.2.3-Alpha-super.3+BLD.a1-xxx.03")
            $v.Major | Should -Be 1
            $v.Minor | Should -Be 2
            $v.Patch | Should -Be 3
            $v.PreReleaseLabel | Should -Be "Alpha-super.3"
            $v.BuildLabel | Should -Be "BLD.a1-xxx.03"
            $v.ToString() | Should -Be "1.2.3-Alpha-super.3+BLD.a1-xxx.03"

            $v = [SemanticVersion]::new("1.0.0")
            $v.Major | Should -Be 1
            $v.Minor | Should -Be 0
            $v.Patch | Should -Be 0
            $v.PreReleaseLabel | Should -BeNullOrEmpty
            $v.BuildLabel | Should -BeNullOrEmpty
            $v.ToString() | Should -Be "1.0.0"

            $v = [SemanticVersion]::new("1.2.3+BLD.a1-xxx.03")
            $v.Major | Should -Be 1
            $v.Minor | Should -Be 2
            $v.Patch | Should -Be 3
            $v.PreReleaseLabel | Should -BeNullOrEmpty
            $v.BuildLabel | Should -Be "BLD.a1-xxx.03"
            $v.ToString() | Should -Be "1.2.3+BLD.a1-xxx.03"

            $v = [SemanticVersion]::new("1.0.0+META")
            $v.Major | Should -Be 1
            $v.Minor | Should -Be 0
            $v.Patch | Should -Be 0
            $v.PreReleaseLabel | Should -BeNullOrEmpty
            $v.BuildLabel | Should -Be "META"
            $v.ToString() | Should -Be "1.0.0+META"

            $v = [SemanticVersion]::new("3.0")
            $v.Major | Should -Be 3
            $v.Minor | Should -Be 0
            $v.Patch | Should -Be 0
            $v.PreReleaseLabel | Should -BeNullOrEmpty
            $v.BuildLabel | Should -BeNullOrEmpty
            $v.ToString() | Should -Be "3.0.0"

            $v = [SemanticVersion]::new("2")
            $v.Major | Should -Be 2
            $v.Minor | Should -Be 0
            $v.Patch | Should -Be 0
            $v.PreReleaseLabel | Should -BeNullOrEmpty
            $v.BuildLabel | Should -BeNullOrEmpty
            $v.ToString() | Should -Be "2.0.0"
        }

        # After the above test, we trust the properties and rely on ToString for validation

        It "Int args constructor" {
            $v = [SemanticVersion]::new(1, 0, 0)
            $v.ToString() | Should -Be "1.0.0"

            $v = [SemanticVersion]::new(3, 2, 0, "beta.1")
            $v.ToString() | Should -Be "3.2.0-beta.1"

            $v = [SemanticVersion]::new(3, 2, 0, "beta.1+meta")
            $v.ToString() | Should -Be "3.2.0-beta.1+meta"

            $v = [SemanticVersion]::new(3, 2, 0, "beta.1", "meta")
            $v.ToString() | Should -Be "3.2.0-beta.1+meta"

            $v = [SemanticVersion]::new(3, 1)
            $v.ToString() | Should -Be "3.1.0"

            $v = [SemanticVersion]::new(3)
            $v.ToString() | Should -Be "3.0.0"
        }

        It "Version arg constructor" {
            $v = [SemanticVersion]::new([Version]::new(1, 2))
            $v.ToString() | Should -Be '1.2.0'

            $v = [SemanticVersion]::new([Version]::new(1, 2, 3))
            $v.ToString() | Should -Be '1.2.3'
        }

        It "Can covert to 'Version' type" {
            $v1 = [SemanticVersion]::new(3, 2, 1, "prerelease", "meta")
            $v2 = [Version]$v1
            $v2.GetType() | Should -BeExactly "version"
            $v2.PSobject.TypeNames[0] | Should -Be "System.Version#IncludeLabel"
            $v2.Major | Should -Be 3
            $v2.Minor | Should -Be 2
            $v2.Build | Should -Be 1
            $v2.PSSemVerPreReleaseLabel | Should -Be "prerelease"
            $v2.PSSemVerBuildLabel | Should -Be "meta"
            $v2.ToString() | Should -Be "3.2.1-prerelease+meta"
        }

        It "Semantic version can round trip through version" {
            $v1 = [SemanticVersion]::new(3, 2, 1, "prerelease", "meta")
            $v2 = [SemanticVersion]::new([Version]$v1)
            $v2.ToString() | Should -Be "3.2.1-prerelease+meta"
        }
    }

    Context "Comparisons" {
        BeforeAll {
            $v1_0_0 = [SemanticVersion]::new(1, 0, 0)
            $v1_1_0 = [SemanticVersion]::new(1, 1, 0)
            $v1_1_1 = [SemanticVersion]::new(1, 1, 1)
            $v2_1_0 = [SemanticVersion]::new(2, 1, 0)
            $v1_0_0_alpha = [SemanticVersion]::new(1, 0, 0, "alpha.1.1")
            $v1_0_0_alpha2 = [SemanticVersion]::new(1, 0, 0, "alpha.1.2")
            $v1_0_0_beta = [SemanticVersion]::new(1, 0, 0, "beta")
            $v1_0_0_betaBuild = [SemanticVersion]::new(1, 0, 0, "beta", "BUILD")

            $testCases = @(
                @{ lhs = $v1_0_0; rhs = $v1_1_0 }
                @{ lhs = $v1_0_0; rhs = $v1_1_1 }
                @{ lhs = $v1_1_0; rhs = $v1_1_1 }
                @{ lhs = $v1_0_0; rhs = $v2_1_0 }
                @{ lhs = $v1_0_0_alpha; rhs = $v1_0_0_beta }
                @{ lhs = $v1_0_0_alpha; rhs = $v1_0_0_alpha2 }
                @{ lhs = $v1_0_0_alpha; rhs = $v1_0_0 }
                @{ lhs = $v1_0_0_beta; rhs = $v1_0_0 }
                @{ lhs = $v2_1_0; rhs = "3.0"}
                @{ lhs = "1.5"; rhs = $v2_1_0}
            )
        }

        It "Build meta should be ignored" {
            $v1_0_0_beta -eq $v1_0_0_betaBuild | Should -BeTrue
            $v1_0_0_betaBuild -lt $v1_0_0_beta | Should -BeFalse
            $v1_0_0_beta -lt $v1_0_0_betaBuild | Should -BeFalse
        }

        It "<lhs> less than <rhs>" -TestCases $testCases {
            param($lhs, $rhs)
            $lhs -lt $rhs | Should -BeTrue
            $rhs -lt $lhs | Should -BeFalse
        }

        It "<lhs> less than or equal <rhs>" -TestCases $testCases {
            param($lhs, $rhs)
            $lhs -le $rhs | Should -BeTrue
            $rhs -le $lhs | Should -BeFalse
            $lhs -le $lhs | Should -BeTrue
            $rhs -le $rhs | Should -BeTrue
        }

        It "<lhs> greater than <rhs>" -TestCases $testCases {
            param($lhs, $rhs)
            $lhs -gt $rhs | Should -BeFalse
            $rhs -gt $lhs | Should -BeTrue
        }

        It "<lhs> greater than or equal <rhs>" -TestCases $testCases {
            param($lhs, $rhs)
            $lhs -ge $rhs | Should -BeFalse
            $rhs -ge $lhs | Should -BeTrue
            $lhs -ge $lhs | Should -BeTrue
            $rhs -ge $rhs | Should -BeTrue
        }

        It "Equality <operand>" -TestCases @(
            @{ operand = $v1_0_0 }
            @{ operand = $v1_0_0_alpha }
        ) {
            param($operand)
            $operand -eq $operand | Should -BeTrue
            $operand -ne $operand | Should -BeFalse
            $null -eq $operand | Should -BeFalse
            $operand -eq $null | Should -BeFalse
            $null -ne $operand | Should -BeTrue
            $operand -ne $null | Should -BeTrue
        }

        It "comparisons with null" {
            $v1_0_0 -lt $null | Should -BeFalse
            $null -lt $v1_0_0 | Should -BeTrue
            $v1_0_0 -le $null | Should -BeFalse
            $null -le $v1_0_0 | Should -BeTrue
            $v1_0_0 -gt $null | Should -BeTrue
            $null -gt $v1_0_0 | Should -BeFalse
            $v1_0_0 -ge $null | Should -BeTrue
            $null -ge $v1_0_0 | Should -BeFalse
        }
    }

    Context "Error handling" {

        It "<name>: '<version>'" -TestCases @(
            @{ name = "Missing parts: 'null'";       errorId = "PSArgumentNullException";expectedResult = $false; version = $null  }
            @{ name = "Missing parts: 'NullString'"; errorId = "PSArgumentNullException";expectedResult = $false; version = [NullString]::Value }
            @{ name = "Missing parts: 'EmptyString'";errorId = "FormatException";    expectedResult = $false; version = "" }
            @{ name = "Missing parts";               errorId = "FormatException";    expectedResult = $false; version = "-" }
            @{ name = "Missing parts";               errorId = "FormatException";    expectedResult = $false; version = "." }
            @{ name = "Missing parts";               errorId = "FormatException";    expectedResult = $false; version = "+" }
            @{ name = "Missing parts";               errorId = "FormatException";    expectedResult = $false; version = "-alpha" }
            @{ name = "Missing parts";               errorId = "FormatException";    expectedResult = $false; version = "1..0" }
            @{ name = "Missing parts";               errorId = "FormatException";    expectedResult = $false; version = "1.0.-alpha" }
            @{ name = "Missing parts";               errorId = "FormatException";    expectedResult = $false; version = "1.0.+alpha" }
            @{ name = "Missing parts";               errorId = "FormatException";    expectedResult = $false; version = "1.0.0-alpha+" }
            @{ name = "Missing parts";               errorId = "FormatException";    expectedResult = $false; version = "1.0.0-+" }
            @{ name = "Missing parts";               errorId = "FormatException";    expectedResult = $false; version = "1.0.0+-" }
            @{ name = "Missing parts";               errorId = "FormatException";    expectedResult = $false; version = "1.0.0+" }
            @{ name = "Missing parts";               errorId = "FormatException";    expectedResult = $false; version = "1.0.0-" }
            @{ name = "Missing parts";               errorId = "FormatException";    expectedResult = $false; version = "1.0.0." }
            @{ name = "Missing parts";               errorId = "FormatException";    expectedResult = $false; version = "1.0." }
            @{ name = "Missing parts";               errorId = "FormatException";    expectedResult = $false; version = "1.0.." }
            @{ name = "Missing parts";               errorId = "FormatException";    expectedResult = $false; version = ".0.0" }
            @{ name = "Range check of versions";     errorId = "FormatException";    expectedResult = $false; version = "-1.0.0"  }
            @{ name = "Range check of versions";     errorId = "FormatException";    expectedResult = $false; version = "1.-1.0"  }
            @{ name = "Range check of versions";     errorId = "FormatException";    expectedResult = $false; version = "1.0.-1"  }
            @{ name = "Format errors";               errorId = "FormatException";    expectedResult = $false; version = "aa.0.0"  }
            @{ name = "Format errors";               errorId = "FormatException";    expectedResult = $false; version = "1.bb.0"  }
            @{ name = "Format errors";               errorId = "FormatException";    expectedResult = $false; version = "1.0.cc"  }
        ) {
            param($version, $expectedResult, $errorId)
            { [SemanticVersion]::new($version) } | Should -Throw -ErrorId $errorId
            if ($version -eq $null) {
                # PowerShell convert $null to Empty string
                { [SemanticVersion]::Parse($version) } | Should -Throw -ErrorId "FormatException"
            } else {
                { [SemanticVersion]::Parse($version) } | Should -Throw -ErrorId $errorId
            }
            $semVer = $null
            [SemanticVersion]::TryParse($_, [ref]$semVer) | Should -Be $expectedResult
            $semVer | Should -BeNullOrEmpty
        }

        It "Negative version arguments" {
            { [SemanticVersion]::new(-1, 0) } | Should -Throw -ErrorId "PSArgumentException"
            { [SemanticVersion]::new(1, -1) } | Should -Throw -ErrorId "PSArgumentException"
            { [SemanticVersion]::new(1, 1, -1) } | Should -Throw -ErrorId "PSArgumentException"
        }

        It "Incompatible 'Version' throws" {
            # Revision isn't supported
            { [SemanticVersion]::new([Version]::new(0, 0, 0, 4)) } | Should -Throw -ErrorId "PSArgumentException"
            { [SemanticVersion]::new([Version]::new("1.2.3.4")) } | Should -Throw -ErrorId "PSArgumentException"
        }
    }

    Context "Serialization" {
        $testCases = @(
            @{ errorId = "PSArgumentException"; expectedResult = "1.0.0"; semver = [SemanticVersion]::new(1, 0, 0) }
            @{ errorId = "PSArgumentException"; expectedResult = "1.0.1"; semver = [SemanticVersion]::new(1, 0, 1) }
            @{ errorId = "PSArgumentException"; expectedResult = "1.0.0-alpha"; semver = [SemanticVersion]::new(1, 0, 0, "alpha") }
            @{ errorId = "PSArgumentException"; expectedResult = "1.0.0-Alpha-super.3+BLD.a1-xxx.03"; semver = [SemanticVersion]::new(1, 0, 0, "Alpha-super.3+BLD.a1-xxx.03") }
        )
        It "Can round trip: <semver>" -TestCases $testCases {
            param($semver, $expectedResult)

            $ser = [PSSerializer]::Serialize($semver)
            $des = [PSSerializer]::Deserialize($ser)

            $des | Should -BeOfType System.Object
            $des.ToString() | Should -Be $expectedResult
        }
    }

    Context "Formatting" {
        It "Should not throw when default format-table is used" {
            { $PSVersionTable.PSVersion | Format-Table | Out-String } | Should -Not -Throw
        }
    }

    Context 'Semver official tests' {
        BeforeAll {
            $valid = @'
0.0.4
1.2.3
10.20.30
1.1.2-prerelease+meta
1.1.2+meta
1.1.2+meta-valid
1.0.0-alpha
1.0.0-beta
1.0.0-alpha.beta
1.0.0-alpha.beta.1
1.0.0-alpha.1
1.0.0-alpha0.valid
1.0.0-alpha.0valid
1.0.0-alpha-a.b-c-somethinglong+build.1-aef.1-its-okay
1.0.0-rc.1+build.1
2.0.0-rc.1+build.123
1.2.3-beta
10.2.3-DEV-SNAPSHOT
1.2.3-SNAPSHOT-123
1.0.0
2.0.0
1.1.7
2.0.0+build.1848
2.0.1-alpha.1227
1.0.0-alpha+beta
1.2.3----RC-SNAPSHOT.12.9.1--.12+788
1.2.3----R-S.12.9.1--.12+meta
1.2.3----RC-SNAPSHOT.12.9.1--.12
1.0.0+0.build.1-rc.10000aaa-kk-0.1
1.0.0-0A.is.legal
'@

            $validVersions = @()
            foreach ($version in $valid.Split("`n", [System.StringSplitOptions]::RemoveEmptyEntries)) {
                $validVersions += @{version = $version}
            }

            $invalid = @'
1
1.2
1.2.3-0123
1.2.3-0123.0123
1.1.2+.123
+invalid
-invalid
-invalid+invalid
-invalid.01
alpha
alpha.beta
alpha.beta.1
alpha.1
alpha+beta
alpha_beta
alpha.
alpha..
beta
1.0.0-alpha_beta
-alpha.
1.0.0-alpha..
1.0.0-alpha..1
1.0.0-alpha...1
1.0.0-alpha....1
1.0.0-alpha.....1
1.0.0-alpha......1
1.0.0-alpha.......1
01.1.1
1.01.1
1.1.01
1.2
1.2.3.DEV
1.2-SNAPSHOT
1.2.31.2.3----RC-SNAPSHOT.12.09.1--..12+788
1.2-RC-SNAPSHOT
-1.0.3-gamma+b7718
+justmeta
9.8.7+meta+meta
9.8.7-whatever+meta+meta
99999999999999999999999.999999999999999999.99999999999999999----RC-SNAPSHOT.12.09.1--------------------------------..12
'@

            $invalidVersions = @()
            foreach ($version in $invalid.Split("`n", [System.StringSplitOptions]::RemoveEmptyEntries)) {
                $invalidVersions += @{version = $version}
            }
        }

        It 'Should parse valid versions: <version>' -TestCases $validVersions {
            param($version)
            $v = [SemanticVersion]::new($version)
            $v.ToString() | Should -Be $version
        }

        It 'Should not parse invalid versions: <version>' -TestCases $invalidVersions {
            { [SemanticVersion]::new($version) } | Should -Throw
        }
    }

}

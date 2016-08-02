using namespace System.Management.Automation
using namespace System.Management.Automation.Language

Describe "SemanticVersion api tests" -Tags 'CI' {
    Context "constructing valid versions" {
        It "string argument constructor" {
            $v = [SemanticVersion]::new("1.2.3-alpha")
            $v.Major | Should Be 1
            $v.Minor | Should Be 2
            $v.Patch | Should Be 3
            $v.Label | Should Be "alpha"
            $v.ToString() | Should Be "1.2.3-alpha"

            $v = [SemanticVersion]::new("1.0.0")
            $v.Major | Should Be 1
            $v.Minor | Should Be 0
            $v.Patch | Should Be 0
            $v.Label | Should BeNullOrEmpty
            $v.ToString() | Should Be "1.0.0"
        }

	    # After the above test, we trust the properties and rely on ToString for validation

        It "int args constructor" {
            $v = [SemanticVersion]::new(1, 0, 0)
            $v.ToString() | Should Be "1.0.0"

            $v = [SemanticVersion]::new(3, 2, 0, "beta.1")
            $v.ToString() | Should Be "3.2.0-beta.1"
        }

        It "version arg constructor" {
            $v = [SemanticVersion]::new([Version]::new(1, 2, 3))
            $v.ToString() | Should Be '1.2.3'
        }

        It "semantic version can round trip through version" {
            $v1 = [SemanticVersion]::new(3, 2, 1, "prerelease")
            $v2 = [SemanticVersion]::new([Version]$v1)
            $v2.ToString() | Should Be "3.2.1-prerelease"
        }
    }

    Context "Comparisons" {
        $v1_0_0 = [SemanticVersion]::new(1, 0, 0)
        $v1_1_0 = [SemanticVersion]::new(1, 1, 0)
        $v1_1_1 = [SemanticVersion]::new(1, 1, 1)
        $v2_1_0 = [SemanticVersion]::new(2, 1, 0)
        $v1_0_0_alpha = [SemanticVersion]::new(1, 0, 0, "alpha")
        $v1_0_0_beta = [SemanticVersion]::new(1, 0, 0, "beta")

        $testCases = @(
            @{ lhs = $v1_0_0; rhs = $v1_1_0 }
            @{ lhs = $v1_0_0; rhs = $v1_1_1 }
            @{ lhs = $v1_1_0; rhs = $v1_1_1 }
            @{ lhs = $v1_0_0; rhs = $v2_1_0 }
            @{ lhs = $v1_0_0_alpha; rhs = $v1_0_0_beta }
            @{ lhs = $v1_0_0_alpha; rhs = $v1_0_0 }
            @{ lhs = $v1_0_0_beta; rhs = $v1_0_0 }
        )
        It "less than" -TestCases $testCases {
            param($lhs, $rhs)
            $lhs -lt $rhs | Should Be $true
            $rhs -lt $lhs | Should Be $false
        }
        It "less than or equal" -TestCases $testCases {
            param($lhs, $rhs)
            $lhs -le $rhs | Should Be $true
            $rhs -le $lhs | Should Be $false
            $lhs -le $lhs | Should Be $true
            $rhs -le $rhs | Should Be $true
        }
        It "greater than" -TestCases $testCases {
            param($lhs, $rhs)
            $lhs -gt $rhs | Should Be $false
            $rhs -gt $lhs | Should Be $true
        }
        It "greater than or equal" -TestCases $testCases {
            param($lhs, $rhs)
            $lhs -ge $rhs | Should Be $false
            $rhs -ge $lhs | Should Be $true
            $lhs -ge $lhs | Should Be $true
            $rhs -ge $rhs | Should Be $true
        }

        $testCases = @(
            @{ operand = $v1_0_0 }
            @{ operand = $v1_0_0_alpha }
        )
        It "Equality" -TestCases $testCases {
            param($operand)
            $operand -eq $operand | Should Be $true
            $operand -ne $operand | Should Be $false
            $null -eq $operand | Should Be $false
            $operand -eq $null | Should Be $false
            $null -ne $operand | Should Be $true
            $operand -ne $null | Should Be $true
        }

        It "comparisons with null" {
            $v1_0_0 -lt $null | Should Be $false
            $null -lt $v1_0_0 | Should Be $true
            $v1_0_0 -le $null | Should Be $false
            $null -le $v1_0_0 | Should Be $true
            $v1_0_0 -gt $null | Should Be $true
            $null -gt $v1_0_0 | Should Be $false
            $v1_0_0 -ge $null | Should Be $true
            $null -ge $v1_0_0 | Should Be $false
        }
    }

    Context "error handling" {

        # The specific errors aren't too useful here, but noted in comments
        # so when we pick up a version of Pester that will let us check FullyQualifiedErrorId,
        # it's easier to tweak the tests

        $testCases = @(
            @{ expectedResult = $false; version = $null  }
            @{ expectedResult = $false; version = [NullString]::Value }
            @{ expectedResult = $false; version = "" }
            @{ expectedResult = $false; version = "1.0.0-" }
            @{ expectedResult = $false; version = "-" }
            @{ expectedResult = $false; version = "-alpha" }
	        @{ expectedResult = $false; version = "1.0" }  # REVIEW - should this be allowed
	        @{ expectedResult = $false; version = "1..0" }
	        @{ expectedResult = $false; version = "1.0.-alpha" }
	        @{ expectedResult = $false; version = "1.0." }
	        @{ expectedResult = $false; version = ".0.0" }
        )

        It "parts of version missing" -TestCases $testCases {
            param($version, $expectedResult)
            { [SemanticVersion]::new($version) } | Should Throw # PSArgumentException
            { [SemanticVersion]::Parse($version) } | Should Throw # PSArgumentException
            $semVer = $null
            [SemanticVersion]::TryParse($_, [ref]$semVer) | Should Be $expectedResult
            $semVer | Should Be $null
        }

        $testCases = @(
            @{ expectedResult = $false; version = "-1.0.0"  }
            @{ expectedResult = $false; version = "1.-1.0"  }
            @{ expectedResult = $false; version = "1.0.-1"  }
        )

        It "range check of versions" -TestCases $testCases {
            param($version, $expectedResult)
            { [SemanticVersion]::new($version) } | Should Throw # PSArgumentException
            { [SemanticVersion]::Parse($version) } | Should Throw # PSArgumentException
            $semVer = $null
            [SemanticVersion]::TryParse($_, [ref]$semVer) | Should Be $expectedResult
            $semVer | Should Be $null
        }

        $testCases = @(
            @{ expectedResult = $false; version = "aa.0.0"  }
            @{ expectedResult = $false; version = "1.bb.0"  }
            @{ expectedResult = $false; version = "1.0.cc"  }
        )

        It "format errors" -TestCases $testCases {
            param($version, $expectedResult)
            { [SemanticVersion]::new($version) } | Should Throw # PSArgumentException
            { [SemanticVersion]::Parse($version) } | Should Throw # PSArgumentException
            $semVer = $null
            [SemanticVersion]::TryParse($_, [ref]$semVer) | Should Be $expectedResult
            $semVer | Should Be $null
        }

        It "Negative version arguments" {
            { [SemanticVersion]::new(-1, 0) } | Should Throw # PSArgumentException
            { [SemanticVersion]::new(1, -1) } | Should Throw # PSArgumentException
            { [SemanticVersion]::new(1, 1, -1) } | Should Throw # PSArgumentException
        }

        It "Incompatible version throws" {
            # Revision isn't supported
            { [SemanticVersion]::new([Version]::new(0, 0, 0, 4)) } | Should Throw # PSArgumentException
            { [SemanticVersion]::new([Version]::new("1.2.3.4")) } | Should Throw # PSArgumentException

            # Build is required
            { [SemanticVersion]::new([Version]::new(1, 2)) } | Should Throw # PSArgumentException
            { [SemanticVersion]::new([Version]::new("1.2")) } | Should Throw # PSArgumentException
        }
    }

    Context "Serialization" {
        $testCases = @(
            @{ expectedResult = "1.0.0"; semver = [SemanticVersion]::new(1, 0, 0) }
            @{ expectedResult = "1.0.1"; semver = [SemanticVersion]::new(1, 0, 1) }
            @{ expectedResult = "1.0.0-alpha"; semver = [SemanticVersion]::new(1, 0, 0, "alpha") }
            @{ expectedResult = "1.0.0-beta"; semver = [SemanticVersion]::new(1, 0, 0, "beta") }
        )
        It "Can round trip" -TestCases $testCases {
            param($semver, $expectedResult)

            $ser = [PSSerializer]::Serialize($semver)
            $des = [PSSerializer]::Deserialize($ser)

            $des | Should BeOfType System.Management.Automation.SemanticVersion
            $des.ToString() | Should Be $expectedResult
        }
    }
}

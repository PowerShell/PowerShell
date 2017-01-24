Describe "Get-StringHash" -Tags "CI" {

    BeforeAll {
        $testString = "StringForTestHash"
        $hashSHA256forEmptyString = "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855"
    }

    Context "Default result tests" {
        It "Should default to correct hash (SHA256)" {
            Get-StringHash $testString | Should Be "5B73D63F0848B3315D043DE9960BC351177B24B1BEB921425DAFA5CE079EF9BC"
        }

        It "Should accept strings from pipeline" {
            $result = $testString,""  | Get-StringHash
            $result.Count | Should Be 2
            $result[0] | Should Be "5B73D63F0848B3315D043DE9960BC351177B24B1BEB921425DAFA5CE079EF9BC"
            $result[1] | Should Be $hashSHA256forEmptyString
        }

        It "Should not throw for empty or null input string" {
            Get-StringHash -InputString "" | Should Be $hashSHA256forEmptyString
            Get-StringHash -InputString $null | Should Be $null
        }
    }

    Context "Algorithm tests" {
        BeforeAll {
            # Keep "sHA1" below! It is for testing that the cmdlet accept a hash algorithm name in any case!
            $testcases =
                @{ algorithm = "sHA1";   hash = "456AED408EDA2F2F9B6DEB4F74CE755254797CF2" },
                @{ algorithm = "SHA256"; hash = "5B73D63F0848B3315D043DE9960BC351177B24B1BEB921425DAFA5CE079EF9BC" },
                @{ algorithm = "SHA384"; hash = "F76A49D81727FF096EFAF08BF88777E569B4F53308B960EE2ABFED479A2C19C6963F38B1068F0C78C93A068C8DF506EF" },
                @{ algorithm = "SHA512"; hash = "EF1DCFCC16422807D92880198224EDFFE5C97E975B4CE001DF99F8B82416C7E2724C1735518C8237DF1CE2408D44BCF41E05CA4090BFC170A1AD2C4E9D2D7C23" },
                @{ algorithm = "MD5";    hash = "A5666A2B6D55AC53C5187128080E0AED" }
        }
        It "Should be able to get the correct hash from <algorithm> algorithm" -TestCases $testCases {
            param($algorithm, $hash)
            Get-StringHash $testString -Algorithm $algorithm | Should Be $hash
        }

        It "Should be throw for wrong algorithm name" {
            try {
                Get-StringHash -InputString $testString -Algorithm wrongAlgorithm
                throw "No Exception!"
            }
            catch {
                $_.FullyQualifiedErrorId | Should Be "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.GetStringHashCommand"
            }
        }
    }
}

Import-Module $PSScriptRoot\..\Common\Test.Helpers.psm1

Describe 'Validate Attributes Tests' -Tags 'CI' {

    Context "ValidateCount" {
        BeforeAll {
           $testCases = @(
               @{ sb = { function Local:foo { param([ValidateCount(-1,2)] [string[]] $bar) }; foo         }; FullyQualifiedErrorId = "ExceptionConstructingAttribute";             InnerErrorId = "" }
               @{ sb = { function Local:foo { param([ValidateCount(1,-1)] [string[]] $bar) }; foo         }; FullyQualifiedErrorId = "ExceptionConstructingAttribute";             InnerErrorId = "" }
               @{ sb = { function Local:foo { param([ValidateCount(2, 1)] [string[]] $bar) }; foo         }; FullyQualifiedErrorId = "ValidateRangeMaxLengthSmallerThanMinLength"; InnerErrorId = "" }
               @{ sb = { function Local:foo { param([ValidateCount(2, 2)] [string[]] $bar) }; foo 1       }; FullyQualifiedErrorId = "ParameterArgumentValidationError,foo";       InnerErrorId = "ValidateCountNotExactlyEqual" }
               @{ sb = { function Local:foo { param([ValidateCount(2, 3)] [string[]] $bar) }; foo 1       }; FullyQualifiedErrorId = "ParameterArgumentValidationError,foo";       InnerErrorId = "ValidateCountSmallerThanMin" }
               @{ sb = { function Local:foo { param([ValidateCount(2, 3)] [string[]] $bar) }; foo 1,2,3,4 }; FullyQualifiedErrorId = "ParameterArgumentValidationError,foo";       InnerErrorId = "ValidateCountGreaterThanMax" }
           )
        }

        It 'Exception: <FullyQualifiedErrorId>:<InnerErrorId>' -TestCases $testCases {
            param($sb, $FullyQualifiedErrorId, $InnerErrorId)

            $sb | ShouldBeErrorId $FullyQualifiedErrorId
            if ($InnerErrorId) {
                $error[0].exception.innerexception.errorrecord.FullyQualifiedErrorId | Should Be $InnerErrorId
            }
        }

        It 'No Exception: valid argument count' {
            { function Local:foo { param([ValidateCount(2, 4)] [string[]] $bar) }; foo 1,2,3,4 } | Should Not Throw
        }
    }
}

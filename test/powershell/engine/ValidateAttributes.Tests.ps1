Describe 'Validate Attributes Tests' -Tags 'CI' {

    Context "ValidateCount" {
        BeforeAll {
           $testCases = @(
               @{ sb = { function Local:foo { param([ValidateCount(-1,2)] [string[]] $bar) }; foo         }; FullyQualifiedErrorId = "ExceptionConstructingAttribute";             InnerErrorId = "" }
               @{ sb = { function Local:foo { param([ValidateCount(1,-1)] [string[]] $bar) }; foo         }; FullyQualifiedErrorId = "ExceptionConstructingAttribute";             InnerErrorId = "" }
               @{ sb = { function Local:foo { param([ValidateCount(2, 1)] [string[]] $bar) }; foo         }; FullyQualifiedErrorId = "ValidateRangeMaxLengthSmallerThanMinLength"; InnerErrorId = "" }
               @{ sb = { function Local:foo { param([ValidateCount(2, 2)] [string[]] $bar) }; foo 1       }; FullyQualifiedErrorId = "ParameterArgumentValidationError,foo";       InnerErrorId = "ValidateCountExactFailure" }
               @{ sb = { function Local:foo { param([ValidateCount(2, 3)] [string[]] $bar) }; foo 1       }; FullyQualifiedErrorId = "ParameterArgumentValidationError,foo";       InnerErrorId = "ValidateCountMinMaxFailure" }
               @{ sb = { function Local:foo { param([ValidateCount(2, 3)] [string[]] $bar) }; foo 1,2,3,4 }; FullyQualifiedErrorId = "ParameterArgumentValidationError,foo";       InnerErrorId = "ValidateCountMinMaxFailure" }
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

    Context "ValidatePositive" {
        BeforeAll {
           $testCases = @(
               @{ sb = { function Local:foo { param([ValidatePositive()] [int] $bar) }; foo -1  }; FullyQualifiedErrorId = "ParameterArgumentValidationError,foo";  InnerErrorId = "ValidateRangeTooSmall" }
               @{ sb = { function Local:foo { param([ValidatePositive()] [int] $bar) }; foo 0   }; FullyQualifiedErrorId = "ParameterArgumentValidationError,foo";  InnerErrorId = "ValidateRangeTooSmall" }
               @{ sb = { function Local:foo { param([ValidatePositive()] $bar) }; foo "one"     }; FullyQualifiedErrorId = "ParameterArgumentValidationError,foo";  InnerErrorId = "ValidationRangeElementType" }
           )
        }

        It 'Exception: <FullyQualifiedErrorId>:<InnerErrorId>' -TestCases $testCases {
            param($sb, $FullyQualifiedErrorId, $InnerErrorId)

            $sb | ShouldBeErrorId $FullyQualifiedErrorId
            if ($InnerErrorId) {
                $error[0].exception.innerexception.errorrecord.FullyQualifiedErrorId | Should Be $InnerErrorId
            }
        }

        It 'No Exception: positive argument' {
            { function Local:foo { param([ValidatePositive()] [int] $bar) }; foo (Get-Random -Minimum 10) } | Should Not Throw
        }
    }
    
    Context "ValidateNonNegative" {
        BeforeAll {
           $testCases = @(
               @{ sb = { function Local:foo { param([ValidateNonNegative()] [int] $bar) }; foo -1   }; FullyQualifiedErrorId = "ParameterArgumentValidationError,foo";  InnerErrorId = "ValidateRangeTooSmall" }
               @{ sb = { function Local:foo { param([ValidateNonNegative()] $bar) }; foo "one"      }; FullyQualifiedErrorId = "ParameterArgumentValidationError,foo";  InnerErrorId = "ValidationRangeElementType" }
           )

           
           $validTestCases = @(
               @{ sb = { function Local:foo { param([ValidateNonNegative()] [int] $bar) }; foo 0  }; TestValue = 0}
               @{ sb = { function Local:foo { param([ValidateNonNegative()] [int] $bar) }; foo 15 }; TestValue = 15}
           )
        }

        It 'Exception: <FullyQualifiedErrorId>:<InnerErrorId>' -TestCases $testCases {
            param($sb, $FullyQualifiedErrorId, $InnerErrorId)

            $sb | ShouldBeErrorId $FullyQualifiedErrorId
            if ($InnerErrorId) {
                $error[0].exception.innerexception.errorrecord.FullyQualifiedErrorId | Should Be $InnerErrorId
            }
        }

        It 'No Exception: non negative argument "<TestValue>"' -TestCases $validTestCases {
            param($sb, $testValue)
                $sb | Should Not Throw
        }
    }    
    
    Context "ValidateNegative" {
        BeforeAll {
           $testCases = @(
               @{ sb = { function Local:foo { param([ValidateNegative()] [int] $bar) }; foo 1   }; FullyQualifiedErrorId = "ParameterArgumentValidationError,foo";  InnerErrorId = "ValidateRangeTooBig" }
               @{ sb = { function Local:foo { param([ValidateNegative()] [int] $bar) }; foo 0   }; FullyQualifiedErrorId = "ParameterArgumentValidationError,foo";  InnerErrorId = "ValidateRangeTooBig" }
               @{ sb = { function Local:foo { param([ValidateNegative()] $bar) }; foo "one"     }; FullyQualifiedErrorId = "ParameterArgumentValidationError,foo";  InnerErrorId = "ValidationRangeElementType" }
           )
        }

        It 'Exception: <FullyQualifiedErrorId>:<InnerErrorId>' -TestCases $testCases {
            param($sb, $FullyQualifiedErrorId, $InnerErrorId)

            $sb | ShouldBeErrorId $FullyQualifiedErrorId
            if ($InnerErrorId) {
                $error[0].exception.innerexception.errorrecord.FullyQualifiedErrorId | Should Be $InnerErrorId
            }
        }

        It 'No Exception: negative argument' {
            { 
                $testValue = (Get-Random -Minimum 10) * -1
                function Local:foo { param([ValidateNegative()] [int] $bar) }; foo $testValue 
            } | Should Not Throw
        }
    }
    
    Context "ValidateNonPositive" {
        BeforeAll {
           $testCases = @(
               @{ sb = { function Local:foo { param([ValidateNonPositive()] [int] $bar) }; foo 1    }; FullyQualifiedErrorId = "ParameterArgumentValidationError,foo";  InnerErrorId = "ValidateRangeTooBig" }
               @{ sb = { function Local:foo { param([ValidateNonPositive()] $bar) }; foo "one"      }; FullyQualifiedErrorId = "ParameterArgumentValidationError,foo";  InnerErrorId = "ValidationRangeElementType" }
           )

            $validTestCases = @(
               @{ sb = { function Local:foo { param([ValidateNonPositive()] [int] $bar) }; foo 0  }; TestValue = 0}
               @{ sb = { function Local:foo { param([ValidateNonPositive()] [int] $bar) }; foo -15 }; TestValue = -15}
           )
        }

        It 'Exception: <FullyQualifiedErrorId>:<InnerErrorId>' -TestCases $testCases {
            param($sb, $FullyQualifiedErrorId, $InnerErrorId)

            $sb | ShouldBeErrorId $FullyQualifiedErrorId
            if ($InnerErrorId) {
                $error[0].exception.innerexception.errorrecord.FullyQualifiedErrorId | Should Be $InnerErrorId
            }
        }

        It 'No Exception: non positive argument "<TestValue>"' -TestCases $validTestCases {
            param($sb, $testValue)
                $sb | Should Not Throw
        }
    }    

}

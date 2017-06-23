Describe 'Validate Attributes Tests' -Tags 'CI' {

    Context "ValidateCount" {
        BeforeAll {
           $testCases = @(
               @{
                   sb = { function foo { param([ValidateCount(-1,2)] [string[]] $bar) }; foo }
                   FullyQualifiedErrorId = "ExceptionConstructingAttribute"
                   InnerErrorId = "" 
                }
               @{
                   sb = { function foo { param([ValidateCount(1,-1)] [string[]] $bar) }; foo }
                   FullyQualifiedErrorId = "ExceptionConstructingAttribute"          
                   InnerErrorId = "" 
                }
               @{ 
                   sb = { function foo { param([ValidateCount(2, 1)] [string[]] $bar) }; foo }
                    FullyQualifiedErrorId = "ValidateRangeMaxLengthSmallerThanMinLength"
                    InnerErrorId = "" 
                }
               @{ 
                   sb = { function foo { param([ValidateCount(2, 2)] [string[]] $bar) }; foo 1 } 
                    FullyQualifiedErrorId = "ParameterArgumentValidationError,foo"
                    InnerErrorId = "ValidateCountExactFailure" 
                }
               @{ 
                   sb = { function foo { param([ValidateCount(2, 3)] [string[]] $bar) }; foo 1 }
                    FullyQualifiedErrorId = "ParameterArgumentValidationError,foo"
                    InnerErrorId = "ValidateCountMinMaxFailure" 
                }
               @{ 
                   sb = { function foo { param([ValidateCount(2, 3)] [string[]] $bar) }; foo 1,2,3,4 }
                    FullyQualifiedErrorId = "ParameterArgumentValidationError,foo"
                    InnerErrorId = "ValidateCountMinMaxFailure"
                }
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
            { function foo { param([ValidateCount(2, 4)] [string[]] $bar) }; foo 1,2,3,4 } | Should Not Throw
        }
    }

    Context "ValidateRange - ParameterConstuctors" {
        BeforeAll {
            $testCases = @(
               @{ 
                   sb = { function foo { param([ValidateRange('xPositive')] $bar) }; foo }
                   FullyQualifiedErrorId = "ExceptionConstructingAttribute"
                   InnerErrorId = "SubstringDisambiguationEnumParseThrewAnException" 
                }
               @{ 
                   sb = { function foo { param([ValidateRange(2,1)] [int] $bar) }; foo }
                   FullyQualifiedErrorId = "MaxRangeSmallerThanMinRange"
                   InnerErrorId = "" 
                }
               @{ 
                   sb = { function foo { param([ValidateRange("one",10)] $bar) }; foo }
                   FullyQualifiedErrorId = "MinRangeNotTheSameTypeOfMaxRange"
                   InnerErrorId = "" 
                }
                @{ 
                   sb = { function foo { param([ValidateRange(1,"two")] $bar) }; foo }
                   FullyQualifiedErrorId = "MinRangeNotTheSameTypeOfMaxRange"
                   InnerErrorId = "" 
                }
           )
        }

        It 'Exception: <FullyQualifiedErrorId>:<InnerErrorId>' -TestCases $testCases {
            param($sb, $FullyQualifiedErrorId, $InnerErrorId)

            $sb | ShouldBeErrorId $FullyQualifiedErrorId
            if ($InnerErrorId) {
                $error[0].exception.innerexception.errorrecord.FullyQualifiedErrorId | Should Be $InnerErrorId
            }
        }
    }
    Context "ValidateRange - Range"{
        BeforeAll {
           $testCases = @(
               @{ 
                   sb = { function foo { param([ValidateRange(1,10)] [int] $bar) }; foo -1 }
                   FullyQualifiedErrorId = "ParameterArgumentValidationError,foo"
                   InnerErrorId = "ValidateRangeTooSmall" 
                }
               @{ 
                   sb = { function foo { param([ValidateRange(1,10)] [int] $bar) }; foo 11 }
                   FullyQualifiedErrorId = "ParameterArgumentValidationError,foo"
                   InnerErrorId = "ValidateRangeTooBig" 
                }
               @{ 
                   sb = { function foo { param([ValidateRange(1,10)] $bar) }; foo "one" }
                   FullyQualifiedErrorId = "ParameterArgumentValidationError,foo"
                   InnerErrorId = "ValidationRangeElementType" 
                }
           )

            $validTestCases = @(
               @{ 
                   sb = { function foo { param([ValidateRange(1,10)] [int] $bar) }; foo 5 }
                }
           )
        }

        It 'Exception: <FullyQualifiedErrorId>:<InnerErrorId>' -TestCases $testCases {
            param($sb, $FullyQualifiedErrorId, $InnerErrorId)

            $sb | ShouldBeErrorId $FullyQualifiedErrorId
            if ($InnerErrorId) {
                $error[0].exception.innerexception.errorrecord.FullyQualifiedErrorId | Should Be $InnerErrorId
            }
        }

        It 'No Exception: value within range' -TestCases $validTestCases {
            param($sb)
                $sb | Should Not Throw
        }
    }

    Context "ValidateRange - Positive" {
        BeforeAll {
           $testCases = @(
               @{ 
                   sb = { function foo { param([ValidateRange('Positive')] [int] $bar) }; foo -1 }
                   FullyQualifiedErrorId = "ParameterArgumentValidationError,foo"
                   InnerErrorId = "ValidateRangeTooSmall" 
                }
               @{ 
                   sb = { function foo { param([ValidateRange('Positive')] [int] $bar) }; foo 0 }
                   FullyQualifiedErrorId = "ParameterArgumentValidationError,foo"
                   InnerErrorId = "ValidateRangeTooSmall" 
                }
               @{ 
                   sb = { function foo { param([ValidateRange('Positive')] $bar) }; foo "one" }
                   FullyQualifiedErrorId = "ParameterArgumentValidationError,foo"
                   InnerErrorId = "" 
                }
           )

           $validTestCases = @(
               @{ 
                   sb = { function foo { param([ValidateRange('Positive')] [int] $bar) }; foo 15 }
                   TestValue = 15
                }
               @{ 
                   sb = { function foo { param([ValidateRange('Positive')] $bar) }; foo ([double]::MaxValue) }; 
                   TestValue = [double]::MaxValue
                }
           )
        }

        It 'Exception: <FullyQualifiedErrorId>:<InnerErrorId>' -TestCases $testCases {
            param($sb, $FullyQualifiedErrorId, $InnerErrorId)

            $sb | ShouldBeErrorId $FullyQualifiedErrorId
            if ($InnerErrorId) {
                $error[0].exception.innerexception.errorrecord.FullyQualifiedErrorId | Should Be $InnerErrorId
            }
        }

        It 'No Exception: positive argument "<TestValue>"' -TestCases $validTestCases {
            param($sb, $testValue)
                $sb | Should Not Throw
        }
    }
    
    Context "ValidateRange - NonNegative" {
        BeforeAll {
           $testCases = @(
               @{
                   sb = { function foo { param([ValidateRange('NonNegative')] [int] $bar) }; foo -1 }
                   FullyQualifiedErrorId = "ParameterArgumentValidationError,foo"
                   InnerErrorId = "ValidateRangeTooSmall" 
                }
               @{ 
                   sb = { function foo { param([ValidateRange('NonNegative')] $bar) }; foo "one" }
                   FullyQualifiedErrorId = "ParameterArgumentValidationError,foo"
                   InnerErrorId = "" 
                }
           )
           
           $validTestCases = @(
               @{ 
                   sb = { function foo { param([ValidateRange('NonNegative')] [int] $bar) }; foo 0 }
                   TestValue = 0
                }
               @{ 
                   sb = { function foo { param([ValidateRange('NonNegative')] [int] $bar) }; foo 15 }
                   TestValue = 15
                }
                @{ 
                   sb = { function foo { param([ValidateRange('NonNegative')] $bar) }; foo ([double]::MaxValue) }; 
                   TestValue = [double]::MaxValue
                }
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
    
    Context "ValidateRange - Negative" {
        BeforeAll {
           $testCases = @(
               @{ 
                   sb = { function foo { param([ValidateRange('Negative')] [int] $bar) }; foo 1 }
                   FullyQualifiedErrorId = "ParameterArgumentValidationError,foo"
                   InnerErrorId = "ValidateRangeTooBig" 
                }
               @{ 
                   sb = { function foo { param([ValidateRange('Negative')] [int] $bar) }; foo 0 }
                   FullyQualifiedErrorId = "ParameterArgumentValidationError,foo"
                   InnerErrorId = "ValidateRangeTooBig" 
                }
               @{ 
                   sb = { function foo { param([ValidateRange('Negative')] $bar) }; foo "one" }
                   FullyQualifiedErrorId = "ParameterArgumentValidationError,foo"
                   InnerErrorId = "" 
                }
           )

           $validTestCases = @(
               @{ 
                   sb = { function foo { param([ValidateRange('Negative')] [int] $bar) }; foo -15 }
                   TestValue = -15
                }
                @{ 
                   sb = { function foo { param([ValidateRange('Negative')] $bar) }; foo ([double]::MinValue) }; 
                   TestValue = [double]::MinValue
                }
           )
        }

        It 'Exception: <FullyQualifiedErrorId>:<InnerErrorId>' -TestCases $testCases {
            param($sb, $FullyQualifiedErrorId, $InnerErrorId)

            $sb | ShouldBeErrorId $FullyQualifiedErrorId
            if ($InnerErrorId) {
                $error[0].exception.innerexception.errorrecord.FullyQualifiedErrorId | Should Be $InnerErrorId
            }
        }

        It 'No Exception: negative argument "<TestValue>"' -TestCases $validTestCases {
            param($sb, $testValue)
                $sb | Should Not Throw
        }
    }
    
    Context "ValidateRange - NonPositive" {
        BeforeAll {
           $testCases = @(
               @{ 
                   sb = { function foo { param([ValidateRange('NonPositive')] $bar) }; foo 1 }
                   FullyQualifiedErrorId = "ParameterArgumentValidationError,foo"
                   InnerErrorId = "ValidateRangeTooBig" 
                }
               @{
                    sb = { function foo { param([ValidateRange('NonPositive')] $bar) }; foo "one" }
                    FullyQualifiedErrorId = "ParameterArgumentValidationError,foo"
                    InnerErrorId = "" 
            }
           )

            $validTestCases = @(
               @{ 
                   sb = { function foo { param([ValidateRange('NonPositive')] [int] $bar) }; foo 0 }
                   TestValue = 0
                }
               @{ 
                   sb = { function foo { param([ValidateRange('NonPositive')] [int] $bar) }; foo -15 }
                   TestValue = -15
                }
                @{ 
                   sb = { function foo { param([ValidateRange('NonPositive')] $bar) }; foo ([double]::MinValue) }
                   TestValue = [double]::MinValue
                }
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

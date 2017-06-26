Describe 'Validate Attributes Tests' -Tags 'CI' {

    Context "ValidateCount" {
        BeforeAll {
           $testCases = @(
                @{
                    sb                       = { function foo { param([ValidateCount(-1,2)] [string[]] $bar) }; foo }
                    FullyQualifiedErrorId    = "ExceptionConstructingAttribute"
                    InnerErrorId             = "" 
                }
                @{
                    sb                       = { function foo { param([ValidateCount(1,-1)] [string[]] $bar) }; foo }
                    FullyQualifiedErrorId    = "ExceptionConstructingAttribute"          
                    InnerErrorId             = "" 
                }
                @{ 
                    sb                      = { function foo { param([ValidateCount(2, 1)] [string[]] $bar) }; foo }
                    FullyQualifiedErrorId   = "ValidateRangeMaxLengthSmallerThanMinLength"
                    InnerErrorId            = "" 
                }
                @{ 
                    sb                      = { function foo { param([ValidateCount(2, 2)] [string[]] $bar) }; foo 1 } 
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,foo"
                    InnerErrorId            = "ValidateCountExactFailure" 
                }
                @{ 
                    sb                      = { function foo { param([ValidateCount(2, 3)] [string[]] $bar) }; foo 1 }
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,foo"
                    InnerErrorId            = "ValidateCountMinMaxFailure" 
                }
                @{ 
                    sb                      = { function foo { param([ValidateCount(2, 3)] [string[]] $bar) }; foo 1,2,3,4 }
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,foo"
                    InnerErrorId            = "ValidateCountMinMaxFailure"
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
                    sb                      = { function foo { param([ValidateRange('xPositive')] $bar) }; foo }
                    FullyQualifiedErrorId   = "ExceptionConstructingAttribute"
                    InnerErrorId            = "SubstringDisambiguationEnumParseThrewAnException" 
                }
                @{ 
                    sb                      = { function foo { param([ValidateRange(2,1)] [int] $bar) }; foo }
                    FullyQualifiedErrorId   = "MaxRangeSmallerThanMinRange"
                    InnerErrorId            = "" 
                }
                @{ 
                    sb                      = { function foo { param([ValidateRange("one",10)] $bar) }; foo }
                    FullyQualifiedErrorId   = "MinRangeNotTheSameTypeOfMaxRange"
                    InnerErrorId            = "" 
                }
                @{ 
                    sb                      = { function foo { param([ValidateRange(1,"two")] $bar) }; foo }
                    FullyQualifiedErrorId   = "MinRangeNotTheSameTypeOfMaxRange"
                    InnerErrorId            = "" 
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
    Context "ValidateRange - User Defined Range"{
        BeforeAll {
           $testCases = @(
                @{ 
                    sb                      = { function foo { param([ValidateRange(1,10)] [int] $bar) }; foo -1 }
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,foo"
                    InnerErrorId            = "ValidateRangeTooSmall" 
                }
                @{ 
                    sb                      = { function foo { param([ValidateRange(1,10)] [int] $bar) }; foo 11 }
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,foo"
                    InnerErrorId            = "ValidateRangeTooBig" 
                }
                @{ 
                    sb                      = { function foo { param([ValidateRange(1,10)] $bar) }; foo "one" }
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,foo"
                    InnerErrorId            = "ValidationRangeElementType" 
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

    Context "ValidateRange - Predefined Range" {
        BeforeAll {
           $testCases = @(
                @{ 
                    sb                      = { function foo { param([ValidateRange("Positive")] [int] $bar) }; foo -1 }
                    RangeType               = "Positive"
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,foo"
                    InnerErrorId            = "ValidateRangeTooSmall" 
                }
                @{ 
                    sb                      = { function foo { param([ValidateRange("Positive")] [int] $bar) }; foo 0 }
                    RangeType               = "Positive"
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,foo"
                    InnerErrorId            = "ValidateRangeTooSmall" 
                }
                @{ 
                    sb                      = { function foo { param([ValidateRange("Positive")] $bar) }; foo "one" }
                    RangeType               = "Positive"
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,foo"
                    InnerErrorId            = "" 
                }
                @{
                    sb                      = { function foo { param([ValidateRange('NonNegative')] [int] $bar) }; foo -1 }
                    RangeType               = "NonNegative"
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,foo"
                    InnerErrorId            = "ValidateRangeTooSmall" 
                }
                @{ 
                   sb                       = { function foo { param([ValidateRange('NonNegative')] $bar) }; foo "one" }
                   RangeType                = "NonNegative"
                   FullyQualifiedErrorId    = "ParameterArgumentValidationError,foo"
                   InnerErrorId             = "" 
                }
                @{ 
                    sb                      = { function foo { param([ValidateRange('Negative')] [int] $bar) }; foo 1 }
                    RangeType               = "Negative"
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,foo"
                    InnerErrorId            = "ValidateRangeTooBig" 
                }
                @{ 
                    sb                      = { function foo { param([ValidateRange('Negative')] [int] $bar) }; foo 0 }
                    RangeType               = "Negative"
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,foo"
                    InnerErrorId            = "ValidateRangeTooBig" 
                }
                @{ 
                    sb                      = { function foo { param([ValidateRange('Negative')] $bar) }; foo "one" }
                    RangeType               = "Negative"
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,foo"
                    InnerErrorId            = "" 
                }
                @{ 
                    sb                      = { function foo { param([ValidateRange('NonPositive')] $bar) }; foo 1 }
                    RangeType               = "NonPositive"
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,foo"
                    InnerErrorId            = "ValidateRangeTooBig" 
                }
                @{
                    sb                      = { function foo { param([ValidateRange('NonPositive')] $bar) }; foo "one" }
                    RangeType               = "NonPositive"
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,foo"
                    InnerErrorId            = "" 
                }
           )

           $validTestCases = @(
                @{ 
                   sb           = { function foo { param([ValidateRange("Positive")] [int] $bar) }; foo 15 }
                   RangeType    = "Positive"
                   TestValue    = 15
                }
                @{ 
                   sb           = { function foo { param([ValidateRange("Positive")] $bar) }; foo ([double]::MaxValue) }; 
                   RangeType    = "Positive"
                   TestValue    = [double]::MaxValue
                }
                @{ 
                   sb           = { function foo { param([ValidateRange('NonNegative')] [int] $bar) }; foo 0 }
                   RangeType    = "NonNegative"
                   TestValue    = 0
                }
                @{ 
                   sb           = { function foo { param([ValidateRange('NonNegative')] [int] $bar) }; foo 15 }
                   RangeType    = "NonNegative"
                   TestValue    = 15
                }
                @{ 
                   sb           = { function foo { param([ValidateRange('NonNegative')] $bar) }; foo ([double]::MaxValue) }; 
                   RangeType    = "NonNegative"
                   TestValue    = [double]::MaxValue
                }
                @{ 
                    sb          = { function foo { param([ValidateRange('Negative')] [int] $bar) }; foo -15 }
                    RangeType   = "Negative"
                    TestValue   = -15
                }
                @{ 
                    sb          = { function foo { param([ValidateRange('Negative')] $bar) }; foo ([double]::MinValue) }; 
                    TestValue   = [double]::MinValue
                    RangeType   = "Negative"
                }
                @{ 
                    sb          = { function foo { param([ValidateRange('NonPositive')] [int] $bar) }; foo 0 }
                    RangeType   = "NonPositive"
                    TestValue   = 0
                }
                @{ 
                    sb          = { function foo { param([ValidateRange('NonPositive')] [int] $bar) }; foo -15 }
                    RangeType   = "NonPositive"
                    TestValue   = -15
                }
                @{ 
                    sb          = { function foo { param([ValidateRange('NonPositive')] $bar) }; foo ([double]::MinValue) }
                    RangeType   = "NonPositive"
                    TestValue   = [double]::MinValue
                }
           )
        }

        It 'Exception: <FullyQualifiedErrorId>:<InnerErrorId>, RangeType: <RangeType>' -TestCases $testCases {
            param($sb, $RangeType, $FullyQualifiedErrorId, $InnerErrorId)

            $sb | ShouldBeErrorId $FullyQualifiedErrorId
            if ($InnerErrorId) {
                $error[0].exception.innerexception.errorrecord.FullyQualifiedErrorId | Should Be $InnerErrorId
            }
        }

        It 'No Exception: RangeType: <RangeType> - argument "<TestValue>"' -TestCases $validTestCases {
            param($sb, $RangeType, $testValue)
                $sb | Should Not Throw
        }
    }
}

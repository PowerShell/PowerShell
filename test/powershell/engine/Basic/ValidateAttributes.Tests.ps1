# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe 'Validate Attributes Tests' -Tags 'CI' {

    Context "ValidateCount" {
        BeforeAll {
           $testCases = @(
                @{
                    ScriptBlock              = { function Test-ArrayCount { param([ValidateCount(-1,2)] [string[]] $Items) }; Test-ArrayCount }
                    FullyQualifiedErrorId    = "ExceptionConstructingAttribute"
                    InnerErrorId             = ""
                }
                @{
                    ScriptBlock              = { function Test-ArrayCount { param([ValidateCount(1,-1)] [string[]] $Items) }; Test-ArrayCount }
                    FullyQualifiedErrorId    = "ExceptionConstructingAttribute"
                    InnerErrorId             = ""
                }
                @{
                    ScriptBlock             = { function Test-ArrayCount { param([ValidateCount(2, 1)] [string[]] $Items) }; Test-ArrayCount }
                    FullyQualifiedErrorId   = "ValidateRangeMaxLengthSmallerThanMinLength"
                    InnerErrorId            = ""
                }
                @{
                    ScriptBlock             = { function Test-ArrayCount { param([ValidateCount(2, 2)] [string[]] $Items) }; Test-ArrayCount 1 }
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,Test-ArrayCount"
                    InnerErrorId            = "ValidateCountExactFailure"
                }
                @{
                    ScriptBlock             = { function Test-ArrayCount { param([ValidateCount(2, 3)] [string[]] $Items) }; Test-ArrayCount 1 }
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,Test-ArrayCount"
                    InnerErrorId            = "ValidateCountMinMaxFailure"
                }
                @{
                    ScriptBlock             = { function Test-ArrayCount { param([ValidateCount(2, 3)] [string[]] $Items) }; Test-ArrayCount 1,2,3,4 }
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,Test-ArrayCount"
                    InnerErrorId            = "ValidateCountMinMaxFailure"
                }
           )
        }

        It 'Exception: <FullyQualifiedErrorId>:<InnerErrorId>' -TestCases $testCases {
            param($ScriptBlock, $FullyQualifiedErrorId, $InnerErrorId)

            $err = $ScriptBlock | Should -Throw -ErrorId $FullyQualifiedErrorId -PassThru
            if ($InnerErrorId) {
                $err.exception.innerexception.errorrecord.FullyQualifiedErrorId | Should -Be $InnerErrorId
            }
        }

        It 'No Exception: valid argument count' {
            { function Test-ArrayCount { param([ValidateCount(2, 4)] [string[]] $Items) }; Test-ArrayCount 1,2,3,4 } | Should -Not -Throw
        }
    }

    Context "ValidateRange - ParameterConstructors" {
        BeforeAll {
            $testCases = @(
                @{
                    ScriptBlock             = { function Test-NumericRange { param([ValidateRange('xPositive')] $Number) }; Test-NumericRange }
                    FullyQualifiedErrorId   = "ExceptionConstructingAttribute"
                    InnerErrorId            = "SubstringDisambiguationEnumParseThrewAnException"
                }
                @{
                    ScriptBlock             = { function Test-NumericRange { param([ValidateRange(2,1)] [int] $Number) }; Test-NumericRange }
                    FullyQualifiedErrorId   = "MaxRangeSmallerThanMinRange"
                    InnerErrorId            = ""
                }
                @{
                    ScriptBlock             = { function Test-NumericRange { param([ValidateRange("one",10)] $Number) }; Test-NumericRange }
                    FullyQualifiedErrorId   = "MinRangeNotTheSameTypeOfMaxRange"
                    InnerErrorId            = ""
                }
                @{
                    ScriptBlock             = { function Test-NumericRange { param([ValidateRange(1,"two")] $Number) }; Test-NumericRange }
                    FullyQualifiedErrorId   = "MinRangeNotTheSameTypeOfMaxRange"
                    InnerErrorId            = ""
                }
           )
        }

        It 'Exception: <FullyQualifiedErrorId>:<InnerErrorId>' -TestCases $testCases {
            param($ScriptBlock, $FullyQualifiedErrorId, $InnerErrorId)

            $err = $ScriptBlock | Should -Throw -ErrorId $FullyQualifiedErrorId -PassThru
            if ($InnerErrorId) {
                $err.exception.innerexception.errorrecord.FullyQualifiedErrorId | Should -Be $InnerErrorId
            }
        }
    }
    Context "ValidateRange - User Defined Range"{
        BeforeAll {
           $testCases = @(
                @{
                    ScriptBlock             = { function Test-NumericRange { param([ValidateRange(1,10)] [int] $Number) }; Test-NumericRange -1 }
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,Test-NumericRange"
                    InnerErrorId            = "ValidateRangeTooSmall"
                }
                @{
                    ScriptBlock             = { function Test-NumericRange { param([ValidateRange(1,10)] [int] $Number) }; Test-NumericRange 11 }
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,Test-NumericRange"
                    InnerErrorId            = "ValidateRangeTooBig"
                }
                @{
                    ScriptBlock             = { function Test-NumericRange { param([ValidateRange(1,10)] $Number) }; Test-NumericRange "one" }
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,Test-NumericRange"
                    InnerErrorId            = "ValidationRangeElementType"
                }
           )

            $validTestCases = @(
               @{
                   ScriptBlock = { function Test-NumericRange { param([ValidateRange(1,10)] [int] $Number) }; Test-NumericRange 5 }
                }
           )
        }

        It 'Exception: <FullyQualifiedErrorId>:<InnerErrorId>' -TestCases $testCases {
            param($ScriptBlock, $FullyQualifiedErrorId, $InnerErrorId)

            $err = $ScriptBlock | Should -Throw -ErrorId $FullyQualifiedErrorId -PassThru
            if ($InnerErrorId) {
                $err.exception.innerexception.errorrecord.FullyQualifiedErrorId | Should -Be $InnerErrorId
            }
        }

        It 'No Exception: value within range' -TestCases $validTestCases {
            param($ScriptBlock)
                $ScriptBlock | Should -Not -Throw
        }
    }

    Context "ValidateRange - Predefined Range" {
        BeforeAll {
           $testCases = @(
                @{
                    ScriptBlock             = { function Test-NumericRange { param([ValidateRange("Positive")] [int] $Number) }; Test-NumericRange -1 }
                    RangeType               = "Positive"
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,Test-NumericRange"
                    InnerErrorId            = "ValidateRangePositiveFailure"
                }
                @{
                    ScriptBlock             = { function Test-NumericRange { param([ValidateRange("Positive")] [int] $Number) }; Test-NumericRange 0 }
                    RangeType               = "Positive"
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,Test-NumericRange"
                    InnerErrorId            = "ValidateRangePositiveFailure"
                }
                @{
                    ScriptBlock             = { function Test-NumericRange { param([ValidateRange("Positive")] $Number) }; Test-NumericRange "one" }
                    RangeType               = "Positive"
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,Test-NumericRange"
                    InnerErrorId            = ""
                }
                @{
                    ScriptBlock             = { function Test-NumericRange { param([ValidateRange('NonNegative')] [int] $Number) }; Test-NumericRange -1 }
                    RangeType               = "NonNegative"
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,Test-NumericRange"
                    InnerErrorId            = "ValidateRangeNonNegativeFailure"
                }
                @{
                   ScriptBlock              = { function Test-NumericRange { param([ValidateRange('NonNegative')] $Number) }; Test-NumericRange "one" }
                   RangeType                = "NonNegative"
                   FullyQualifiedErrorId    = "ParameterArgumentValidationError,Test-NumericRange"
                   InnerErrorId             = ""
                }
                @{
                    ScriptBlock             = { function Test-NumericRange { param([ValidateRange('Negative')] [int] $Number) }; Test-NumericRange 1 }
                    RangeType               = "Negative"
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,Test-NumericRange"
                    InnerErrorId            = "ValidateRangeNegativeFailure"
                }
                @{
                    ScriptBlock             = { function Test-NumericRange { param([ValidateRange('Negative')] [int] $Number) }; Test-NumericRange 0 }
                    RangeType               = "Negative"
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,Test-NumericRange"
                    InnerErrorId            = "ValidateRangeNegativeFailure"
                }
                @{
                    ScriptBlock             = { function Test-NumericRange { param([ValidateRange('Negative')] $Number) }; Test-NumericRange "one" }
                    RangeType               = "Negative"
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,Test-NumericRange"
                    InnerErrorId            = ""
                }
                @{
                    ScriptBlock             = { function Test-NumericRange { param([ValidateRange('NonPositive')] $Number) }; Test-NumericRange 1 }
                    RangeType               = "NonPositive"
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,Test-NumericRange"
                    InnerErrorId            = "ValidateRangeNonPositiveFailure"
                }
                @{
                    ScriptBlock             = { function Test-NumericRange { param([ValidateRange('NonPositive')] $Number) }; Test-NumericRange "one" }
                    RangeType               = "NonPositive"
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,Test-NumericRange"
                    InnerErrorId            = ""
                }
           )

           $validTestCases = @(
                @{
                   ScriptBlock  = { function Test-NumericRange { param([ValidateRange("Positive")] [int] $Number) }; Test-NumericRange 15 }
                   RangeType    = "Positive"
                   TestValue    = 15
                }
                @{
                   ScriptBlock  = { function Test-NumericRange { param([ValidateRange("Positive")] [double]$Number) }; Test-NumericRange ([double]::MaxValue) };
                   RangeType    = "Positive"
                   TestValue    = [double]::MaxValue
                }
                @{
                   ScriptBlock  = { function Test-NumericRange { param([ValidateRange('NonNegative')] [int] $Number) }; Test-NumericRange 0 }
                   RangeType    = "NonNegative"
                   TestValue    = 0
                }
                @{
                   ScriptBlock  = { function Test-NumericRange { param([ValidateRange('NonNegative')] [int] $Number) }; Test-NumericRange 15 }
                   RangeType    = "NonNegative"
                   TestValue    = 15
                }
                @{
                   ScriptBlock  = { function Test-NumericRange { param([ValidateRange('NonNegative')] [double]$Number) }; Test-NumericRange ([double]::MaxValue) };
                   RangeType    = "NonNegative"
                   TestValue    = [double]::MaxValue
                }
                @{
                    ScriptBlock = { function Test-NumericRange { param([ValidateRange('Negative')] [int] $Number) }; Test-NumericRange -15 }
                    RangeType   = "Negative"
                    TestValue   = -15
                }
                @{
                    ScriptBlock = { function Test-NumericRange { param([ValidateRange('Negative')] [double]$Number) }; Test-NumericRange ([double]::MinValue) };
                    TestValue   = [double]::MinValue
                    RangeType   = "Negative"
                }
                @{
                    ScriptBlock = { function Test-NumericRange { param([ValidateRange('NonPositive')] [int] $Number) }; Test-NumericRange 0 }
                    RangeType   = "NonPositive"
                    TestValue   = 0
                }
                @{
                    ScriptBlock = { function Test-NumericRange { param([ValidateRange('NonPositive')] [int] $Number) }; Test-NumericRange -15 }
                    RangeType   = "NonPositive"
                    TestValue   = -15
                }
                @{
                    ScriptBlock = { function Test-NumericRange { param([ValidateRange('NonPositive')] [double]$Number) }; Test-NumericRange ([double]::MinValue) }
                    RangeType   = "NonPositive"
                    TestValue   = [double]::MinValue
                }
           )
        }

        It 'Exception: <FullyQualifiedErrorId>:<InnerErrorId>, RangeType: <RangeType>' -TestCases $testCases {
            param($ScriptBlock, $RangeType, $FullyQualifiedErrorId, $InnerErrorId)

            $err = $ScriptBlock | Should -Throw -ErrorId $FullyQualifiedErrorId -PassThru
            if ($InnerErrorId) {
                $err.exception.innerexception.errorrecord.FullyQualifiedErrorId | Should -Be $InnerErrorId
            }
        }

        It 'No Exception: RangeType: <RangeType> - argument "<TestValue>"' -TestCases $validTestCases {
            param($ScriptBlock, $RangeType, $testValue)
                $ScriptBlock | Should -Not -Throw
        }
    }

    Context "ValidateLength" {
        BeforeAll {
            $testCases = @(
                @{
                    ScriptBlock             = { function Test-StringLength { param([ValidateLength(2, 5)] [string] $InputString) }; Test-StringLength "a" }
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,Test-StringLength"
                    InnerErrorId            = "ValidateLengthMinLengthFailure"
                }
                @{
                    ScriptBlock             = { function Test-StringLength { param([ValidateLength(2, 5)] [string] $InputString) }; Test-StringLength "abcdef" }
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,Test-StringLength"
                    InnerErrorId            = "ValidateLengthMaxLengthFailure"
                }
                @{
                    ScriptBlock             = { function Test-StringLength { param([ValidateLength(2, 5)] $InputString) }; Test-StringLength 123 }
                    FullyQualifiedErrorId   = "ParameterArgumentValidationError,Test-StringLength"
                    InnerErrorId            = "ValidateLengthNotString"
                }
            )

            $validTestCases = @(
                @{
                    ScriptBlock  = { function Test-StringLength { param([ValidateLength(2, 5)] [string] $InputString) }; Test-StringLength "abc" }
                }
                @{
                    ScriptBlock  = { function Test-StringLength { param([ValidateLength(2, 5)] [string] $InputString) }; Test-StringLength "ab" }
                }
                @{
                    ScriptBlock  = { function Test-StringLength { param([ValidateLength(2, 5)] [string] $InputString) }; Test-StringLength "abcde" }
                }
            )
        }

        It 'Exception: <FullyQualifiedErrorId>:<InnerErrorId>' -TestCases $testCases {
            param($ScriptBlock, $FullyQualifiedErrorId, $InnerErrorId)

            $err = $ScriptBlock | Should -Throw -ErrorId $FullyQualifiedErrorId -PassThru
            if ($InnerErrorId) {
                $err.exception.innerexception.errorrecord.FullyQualifiedErrorId | Should -Be $InnerErrorId
            }
        }

        It 'No Exception: valid string length' -TestCases $validTestCases {
            param($ScriptBlock)
            $ScriptBlock | Should -Not -Throw
        }

        It 'ValidateLength error message should be properly formatted' {
            function Test-ValidateLengthMax { param([ValidateLength(0,2)] [string] $Value) $Value }
            function Test-ValidateLengthMin { param([ValidateLength(5,10)] [string] $Value) $Value }

            $TestStringTooLong = "11111"
            $TestStringTooShort = "123"
            $ExpectedMaxLength = 2
            $ExpectedMinLength = 5

            $err = { Test-ValidateLengthMax $TestStringTooLong } | Should -Throw -ErrorId "ParameterArgumentValidationError,Test-ValidateLengthMax" -PassThru
            $err.Exception.InnerException.Message | Should -Match ".+\($($TestStringTooLong.Length)\).+\`"$ExpectedMaxLength\`""

            $err = { Test-ValidateLengthMin $TestStringTooShort } | Should -Throw -ErrorId "ParameterArgumentValidationError,Test-ValidateLengthMin" -PassThru
            $err.Exception.InnerException.Message | Should -Match ".+\($($TestStringTooShort.Length)\).+\`"$ExpectedMinLength\`""
        }
    }

    Context "ValidateNotNull, ValidateNotNullOrEmpty, ValidateNotNullOrWhiteSpace and Not-Null-Or-Empty check for Mandatory parameter" {

        BeforeAll {
            function MandatoryFunc {
                param(
                    [Parameter(Mandatory, ParameterSetName = "ByteArray")]
                    [byte[]] $ByteArray,

                    [Parameter(Mandatory, ParameterSetName = "ByteList")]
                    [System.Collections.Generic.List[byte]] $ByteList,

                    [Parameter(Mandatory, ParameterSetName = "ByteCollection")]
                    [System.Collections.ObjectModel.Collection[byte]] $ByteCollection,

                    [Parameter(ParameterSetName = "Default")]
                    $Value
                )
            }

            function NotNullFunc {
                param(
                    [ValidateNotNull()]
                    $Value,
                    [string] $TestType
                )

                switch ($TestType) {
                    "COM-Enumerable" { $Value | ForEach-Object Name }
                    "Enumerator"     {
                        $items = foreach ($i in $Value) { $i }
                        $items -join ","
                    }
                }
            }

            function NotNullOrEmptyFunc {
                param(
                    [ValidateNotNullOrEmpty()]
                    $Value,
                    [string] $TestType
                )

                switch ($TestType) {
                    "COM-Enumerable" { $Value | ForEach-Object Name }
                    "Enumerator"     {
                        $items = foreach ($i in $Value) { $i }
                        $items -join ","
                    }
                }
            }

            function NotNullOrWhiteSpaceFunc {
                param(
                    [ValidateNotNullOrWhiteSpace()]
                    $Value,
                    [string] $TestType
                )

                switch ($TestType) {
                    "COM-Enumerable" { $Value | ForEach-Object Name }
                    "Enumerator"     {
                        $items = foreach ($i in $Value) { $i }
                        $items -join ","
                    }
                }
            }

            $filePath  = Join-Path -Path $PSHOME -ChildPath System.Management.Automation.dll
            $byteArray = [System.IO.File]::ReadAllBytes($filePath)
            $byteList  = [System.Collections.Generic.List[byte]] $byteArray
            $byteCollection = [System.Collections.ObjectModel.Collection[byte]] $byteArray
            ## Use the running time of 'MandatoryFunc -Value $byteArray' as the baseline time
            ## because it does no check on the argument.
            $baseline = (Measure-Command { MandatoryFunc -Value $byteArray }).Milliseconds
            ## Running time should be less than 'UpperBoundTime'
            ## This is not really a performance test (perf test cannot run reliably in our CI), but a test
            ## to make sure we don't check the elements of a value-type collection.
            ## The crossgen'ed 'S.M.A.dll' is about 28mb in size, and it would take more than 2000ms if we
            ## check each byte of the array, list or collection. We use ($baseline + 200)ms as the upper
            ## bound value in tests to prove that we don't check each byte.
            $UpperBoundTime = $baseline + 200

            if ($IsWindows) {
                $null = New-Item -Path $TESTDRIVE/file1
            }

            $testCases = @(
                @{ ScriptBlock = { MandatoryFunc -ByteArray $byteArray } }
                @{ ScriptBlock = { MandatoryFunc -ByteList $byteList } }
                @{ ScriptBlock = { MandatoryFunc -ByteCollection $byteCollection } }
                @{ ScriptBlock = { NotNullFunc -Value $byteArray } }
                @{ ScriptBlock = { NotNullFunc -Value $byteList } }
                @{ ScriptBlock = { NotNullFunc -Value $byteCollection } }
                @{ ScriptBlock = { NotNullOrEmptyFunc -Value $byteArray } }
                @{ ScriptBlock = { NotNullOrEmptyFunc -Value $byteList } }
                @{ ScriptBlock = { NotNullOrEmptyFunc -Value $byteCollection } }
                @{ ScriptBlock = { NotNullOrWhiteSpaceFunc -Value $byteArray } }
                @{ ScriptBlock = { NotNullOrWhiteSpaceFunc -Value $byteList } }
                @{ ScriptBlock = { NotNullOrWhiteSpaceFunc -Value $byteCollection } }
            )
        }

        It "Validate running time '<ScriptBlock>'" -TestCases $testCases {
            param ($ScriptBlock)
            (Measure-Command $ScriptBlock).Milliseconds | Should -BeLessThan $UpperBoundTime
        }

        It "COM enumerable argument should work with 'ValidateNotNull', 'ValidateNotNullOrEmpty' and 'ValidateNotNullOrWhiteSpace'" -Skip:(!$IsWindows) {
            $shell = New-Object -ComObject "Shell.Application"
            $folder = $shell.Namespace("$TESTDRIVE")
            $items = $folder.Items()

            NotNullFunc -Value $items -TestType "COM-Enumerable" | Should -Be "file1"
            NotNullOrEmptyFunc -Value $items -TestType "COM-Enumerable" | Should -Be "file1"
            NotNullOrWhiteSpaceFunc -Value $items -TestType "COM-Enumerable" | Should -Be "file1"
        }

        It "Enumerator argument should work with 'ValidateNotNull', 'ValidateNotNullOrEmpty' and 'ValidateNotNullOrWhiteSpace'" {
            $data = @(1,2,3)
            NotNullFunc -Value $data.GetEnumerator() -TestType "Enumerator" | Should -Be "1,2,3"
            NotNullOrEmptyFunc -Value $data.GetEnumerator() -TestType "Enumerator" | Should -Be "1,2,3"
            NotNullOrWhiteSpaceFunc -Value $data.GetEnumerator() -TestType "Enumerator" | Should -Be "1,2,3"
        }

        It "'ValidateNotNull' should throw on null element of a collection argument" {
            ## Should throw on null element
            { NotNullFunc -Value @("string", $null, 2) } | Should -Throw -ErrorId "ParameterArgumentValidationError,NotNullFunc"
            ## Should not throw on empty string element
            { NotNullFunc -Value @("string", "", 2) } | Should -Not -Throw
            ## Should not throw on an empty collection
            { NotNullFunc -Value @() } | Should -Not -Throw
        }

        It "'ValidateNotNullOrEmpty' should throw on null element of a collection argument or empty collection/dictionary" {
            { NotNullOrEmptyFunc -Value @("string", $null, 2) } | Should -Throw -ErrorId "ParameterArgumentValidationError,NotNullOrEmptyFunc"
            { NotNullOrEmptyFunc -Value @("string", "", 2) } | Should -Throw -ErrorId "ParameterArgumentValidationError,NotNullOrEmptyFunc"
            { NotNullOrEmptyFunc -Value @() } | Should -Throw -ErrorId "ParameterArgumentValidationError,NotNullOrEmptyFunc"
            { NotNullOrEmptyFunc -Value @{} } | Should -Throw -ErrorId "ParameterArgumentValidationError,NotNullOrEmptyFunc"
        }

        It "'ValidateNotNullOrWhiteSpace' should throw on null element of a collection argument, white-space only string element of a collection argument or empty collection/dictionary" {
            { NotNullOrWhiteSpaceFunc -Value @("string", $null, 2) } | Should -Throw -ErrorId "ParameterArgumentValidationError,NotNullOrWhiteSpaceFunc"
            { NotNullOrWhiteSpaceFunc -Value @("string", "", 2) } | Should -Throw -ErrorId "ParameterArgumentValidationError,NotNullOrWhiteSpaceFunc"
            { NotNullOrWhiteSpaceFunc -Value @("string", " ", 2) } | Should -Throw -ErrorId "ParameterArgumentValidationError,NotNullOrWhiteSpaceFunc"
            { NotNullOrWhiteSpaceFunc -Value @() } | Should -Throw -ErrorId "ParameterArgumentValidationError,NotNullOrWhiteSpaceFunc"
            { NotNullOrWhiteSpaceFunc -Value @{} } | Should -Throw -ErrorId "ParameterArgumentValidationError,NotNullOrWhiteSpaceFunc"
        }

        It "'ValidateNotNull' should throw on a scalar null value" {
            { NotNullFunc -Value $null } | Should -Throw -ErrorId "ParameterArgumentValidationError,NotNullFunc"
        }

        It "'ValidateNotNullOrEmpty' should throw on a scalar null value and scalar empty string" {
            { NotNullOrEmptyFunc -Value $null } | Should -Throw -ErrorId "ParameterArgumentValidationError,NotNullOrEmptyFunc"
            { NotNullOrEmptyFunc -Value "" } | Should -Throw -ErrorId "ParameterArgumentValidationError,NotNullOrEmptyFunc"
        }

        It "'ValidateNotNullOrWhiteSpace' should throw on a scalar null value, scalar empty string and scalar white-space string" {
            { NotNullOrWhiteSpaceFunc -Value $null } | Should -Throw -ErrorId "ParameterArgumentValidationError,NotNullOrWhiteSpaceFunc"
            { NotNullOrWhiteSpaceFunc -Value "" } | Should -Throw -ErrorId "ParameterArgumentValidationError,NotNullOrWhiteSpaceFunc"
            { NotNullOrWhiteSpaceFunc -Value " " } | Should -Throw -ErrorId "ParameterArgumentValidationError,NotNullOrWhiteSpaceFunc"
        }

        It "Mandatory parameter should throw on empty collection" {
            { MandatoryFunc -ByteArray ([byte[]]@()) } | Should -Throw -ErrorId "ParameterArgumentValidationErrorEmptyArrayNotAllowed,MandatoryFunc"
            { MandatoryFunc -ByteList ([System.Collections.Generic.List[byte]]@()) } | Should -Throw -ErrorId "ParameterArgumentValidationErrorEmptyCollectionNotAllowed,MandatoryFunc"
            { MandatoryFunc -ByteList ([System.Collections.ObjectModel.Collection[byte]]@()) } | Should -Throw -ErrorId "ParameterArgumentValidationErrorEmptyCollectionNotAllowed,MandatoryFunc"
        }
    }

    Context "ValidateSet" {
        BeforeAll {
            function ValidatingValidateSet
            {
                [CmdletBinding()]
                param(
                [ValidateSet("One","Of","These","Days")]
                [string]
                $Word
                )

                $Word
            }
        }

        it 'Ensures a value is one of a number of potential strings' {
            { ValidatingValidateSet -Word "Anything" } | Should -Throw
        }

        it 'Can be disabled' {
            (Get-Command ValidatingValidateSet).Parameters.Values.Attributes |
            Where-Object { $_ -is [ValidateSet] } |
                Foreach-Object {
                    $_.Disabled = $true
                }

            ValidatingValidateSet -Word "Anything" | Should -Be 'Anything'
        }

        it 'Can be updated' {
            (Get-Command ValidatingValidateSet).Parameters.Values.Attributes |
                Where-Object { $_ -is [ValidateSet] } |
                Foreach-Object {
                    $_.Disabled = $false
                    $_.ValidValues = [string[]]@($_.ValidValues;"It";"Will";"Work")
                }

            ValidatingValidateSet -Word "It" | Should -Be 'It'
            ValidatingValidateSet -Word "Will" | Should -Be 'Will'
            ValidatingValidateSet -Word "Work" | Should -Be 'Work'
        }
    }
}

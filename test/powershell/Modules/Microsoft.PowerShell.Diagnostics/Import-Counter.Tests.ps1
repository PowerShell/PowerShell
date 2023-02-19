# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
<############################################################################################
 # File: Import-Counter.Tests.ps1
 # Provides Pester tests for the Import-Counter cmdlet.
 ############################################################################################>

 # Counter CmdLets are removed see issue #4272
 # Tests are disabled
 return

$cmdletName = "Import-Counter"

. "$PSScriptRoot/CounterTestHelperFunctions.ps1"

$SkipTests = SkipCounterTests

if ( ! $SkipTests )
{
    $counterPaths = @(
        (TranslateCounterPath "\Memory\Available Bytes")
        (TranslateCounterPath "\processor(*)\% Processor time")
        (TranslateCounterPath "\Processor(_Total)\% Processor Time")
        (TranslateCounterPath "\PhysicalDisk(_Total)\Current Disk Queue Length")
        (TranslateCounterPath "\PhysicalDisk(_Total)\Disk Bytes/sec")
        (TranslateCounterPath "\PhysicalDisk(_Total)\Disk Read Bytes/sec")
        )
    $setNames = @{
        Memory = (TranslateCounterName "memory")
        PhysicalDisk = (TranslateCounterName "physicaldisk")
        Processor = (TranslateCounterName "processor")
        }
}
else
{
    $counterPaths = @()
    $setNames = @{}
}

$badSamplesBlgPath = Join-Path $PSScriptRoot "assets" "BadCounterSamples.blg"
$corruptBlgPath = Join-Path $PSScriptRoot "assets" "CorruptBlg.blg"
$notFoundPath = Join-Path $PSScriptRoot "DAD288C0-72F8-47D3-8C54-C69481B528DF.blg"

# Set script-scope variable values used by multiple Describes
function SetScriptVars([string]$rootPath, [int]$maxSamples, [bool]$export)
{
    $rootFilename = "exportedCounters"

    $script:blgPath = Join-Path $rootPath "$rootFilename.blg"
    $script:csvPath = Join-Path $rootPath "$rootFilename.csv"
    $script:tsvPath = Join-Path $rootPath "$rootFilename.tsv"

    $script:counterSamples = $null
    if ($maxSamples -and ! $SkipTests )
    {
        $script:counterSamples = Get-Counter -Counter $counterPaths -MaxSamples $maxSamples
    }

    if ($export -and ! $SkipTests )
    {
        Export-Counter -Force -FileFormat "blg" -Path $script:blgPath -InputObject $script:counterSamples
        Export-Counter -Force -FileFormat "csv" -Path $script:csvPath -InputObject $script:counterSamples
        Export-Counter -Force -FileFormat "tsv" -Path $script:tsvPath -InputObject $script:counterSamples
    }
}

# Build up a command to execute
function ConstructCommand($testCase)
{
    $filePath = ""
    $pathParam = ""
    $startTimeParam = ""
    $endTimeParam = ""
    if ($testCase.ContainsKey("Path"))
    {
        $filePath = $testCase.Path
    }
    else
    {
        $filePath = $script:blgPath
    }

    if ($testCase.NoDashPath)
    {
        $pathParam = $filePath
    }
    else
    {
        $pathParam = "-Path $filePath"
    }

    if ($testCase.ContainsKey("StartTime"))
    {
        $startTimeParam = "-StartTime `$testCase.StartTime"
    }
    if ($testCase.ContainsKey("EndTime"))
    {
        $endTimeParam = "-EndTime `$(`$testCase.EndTime)"
    }

    return "$cmdletName $pathParam $startTimeParam $endTimeParam $($testCase.Parameters)"
}

# Run a test that is expected to succeed
function RunTest($testCase)
{
    $skipTest = $testCase.SkipTest -or (SkipCounterTests)

    It "$($testCase.Name)" -Skip:$skipTest {

        if ($testCase.TimestampIndexes)
        {
            if ($testCase.TimestampIndexes.ContainsKey("First"))
            {
                $testCase.StartTime = $script:counterSamples[$testCase.TimestampIndexes.First].Timestamp

                # Exporting loses precision of DateTime objects, which results in almost always
                # missing the first expected item when importing with a controlling StartTime.
                # So, we'll adjust the precision of the timestamp we use for the StartTime value.
                $testCase.StartTime = New-Object System.DateTime ([Int64]([math]::floor($testCase.StartTime.Ticks / 10000)) * 10000)
            }
            if ($testCase.TimestampIndexes.ContainsKey("Last"))
            {
                $testCase.EndTime = $script:counterSamples[$testCase.TimestampIndexes.Last].Timestamp
            }
        }

        $cmd = ConstructCommand $testCase
        $cmd = $cmd + " -ErrorAction SilentlyContinue -ErrorVariable errVar"

        $errVar = $null
        $sb = [scriptblock]::Create($cmd)
        $result = &$sb
        $errVar | Should -BeNullOrEmpty

        if ($testCase.ContainsKey("Script"))
        {
            &$testCase.Script
        }
        else
        {
            if ($testCase.TimestampIndexes)
            {
                $start = 0
                $end = $script:counterSamples.Length - 1
                if ($testCase.TimestampIndexes.ContainsKey("First"))
                {
                    $start = $testCase.TimestampIndexes.First
                }
                if ($testCase.TimestampIndexes.ContainsKey("Last"))
                {
                    $end = $testCase.TimestampIndexes.Last
                }

                CompareCounterSets $result $script:counterSamples[$start..$end]
            }
            else
            {
                CompareCounterSets $result $script:counterSamples
            }
        }
    }
}

# Run a test for each file format
function RunPerFileTypeTests($testCase)
{
    if ($testCase.UseKnownSamples)
    {
        $basePath = Join-Path $PSScriptRoot "assets" "CounterSamples"
        $formats = @{
            "BLG" = "$basePath.blg"
            "CSV" = "$basePath.blg"
            "TSV" = "$basePath.blg"
        }
    }
    else
    {
        $formats = @{
            "BLG" = $script:blgPath
            "CSV" = $script:csvPath
            "TSV" = $script:tsvPath
        }
    }

    foreach ($f in $formats.GetEnumerator())
    {
        $newCase = $testCase.Clone();
        $newCase.Path = $f.Value
        $newCase.Name = "$($newCase.Name) ($($f.Name) format)"

        RunTest $newCase
    }
}

# Run a test case that is expected to fail
function RunExpectedFailureTest($testCase)
{
    It "$($testCase.Name)" -Skip:$(SkipCounterTests) {
        $cmd = ConstructCommand $testCase
        # Use $cmd to debug a test failure
        # Write-Host "Command to run: $cmd"
        $cmd = $cmd + " -ErrorAction Stop"

        if ($testCase.ContainsKey("Script"))
        {
            # Here we want to run the command then do our own post-run checks
            $sb = [ScriptBlock]::Create($cmd)
            &$sb
            &$testCase.Script
        }
        else
        {
            # Here we expect and want the command to fail
            $sb = [ScriptBlock]::Create($cmd)
            $e = { &$sb } | Should -Throw -ErrorId $testCase.ExpectedErrorId -PassThru
            if ($testCase.ExpectedErrorCategory)
            {
                $e.CategoryInfo.Category | Should -BeExactly $testCase.ExpectedErrorCategory
            }
        }
    }
}

Describe "CI tests for Import-Counter cmdlet" -Tags "CI" {

    BeforeAll {
        SetScriptVars $testDrive 0 $false
    }

    $performantTestCases = @(
        @{
            Name = "Can import all samples from known sample sets"
            UseKnownSamples = $true
            Script = {
                $result.Length | Should -Be 25
            }
        }
        @{
            Name = "Can acquire summary information"
            UseKnownSamples = $true
            Parameters = "-Summary"
            Script = {
                $result.SampleCount | Should -Be 25
                $result.OldestRecord | Should -Be (Get-Date -Year 2016 -Month 11 -Day 26 -Hour 13 -Minute 46 -Second 30 -Millisecond 874)
                $result.NewestRecord | Should -Be (Get-Date -Year 2016 -Month 11 -Day 26 -Hour 13 -Minute 47 -Second 42 -Millisecond 983)
            }
        }
    )

    foreach ($testCase in $performantTestCases)
    {
        RunPerFileTypeTests $testCase
    }
}

Describe "Feature tests for Import-Counter cmdlet" -Tags "Feature" {

    BeforeAll {
        SetScriptVars $testDrive 25 $true
    }

    AfterAll {
        Remove-Item $script:blgPath -Force -ErrorAction SilentlyContinue
        Remove-Item $script:csvPath -Force -ErrorAction SilentlyContinue
        Remove-Item $script:tsvPath -Force -ErrorAction SilentlyContinue
    }

    Context "Validate incorrect usage" {
        $testCases = @(
            @{
                Name = "Fails when given non-existent path"
                Path = $notFoundPath
                ExpectedErrorCategory = [System.Management.Automation.ErrorCategory]::ObjectNotFound
                ExpectedErrorId = "Microsoft.PowerShell.Commands.ImportCounterCommand"
            }
            @{
                Name = "Fails when given null path"
                Path = "`$null"
                ExpectedErrorId = "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.ImportCounterCommand"
            }
            @{
                Name = "Fails when -Path specified but no path given"
                Path = ""
                ExpectedErrorId = "MissingArgument,Microsoft.PowerShell.Commands.ImportCounterCommand"
            }
            @{
                Name = "Fails when given -ListSet without set names"
                Parameters = "-ListSet"
                ExpectedErrorId = "MissingArgument,Microsoft.PowerShell.Commands.ImportCounterCommand"
            }
            @{
                Name = "Fails when given -StartTime without DateTime"
                Parameters = "-StartTime"
                ExpectedErrorId = "MissingArgument,Microsoft.PowerShell.Commands.ImportCounterCommand"
            }
            @{
                Name = "Fails when given -EndTime without DateTime"
                Parameters = "-EndTime"
                ExpectedErrorId = "MissingArgument,Microsoft.PowerShell.Commands.ImportCounterCommand"
            }
            @{
                Name = "Fails when given -ListSet and -Summary"
                Parameters = "-ListSet memory -Summary"
                ExpectedErrorId = "AmbiguousParameterSet,Microsoft.PowerShell.Commands.ImportCounterCommand"
            }
            @{
                Name = "Fails when given -Summary and -Counter"
                Parameters = "-Summary -Counter `"\processor(*)\% processor time`""
                ExpectedErrorId = "AmbiguousParameterSet,Microsoft.PowerShell.Commands.ImportCounterCommand"
            }
            @{
                Name = "Fails when given -ListSet and -Counter"
                Parameters = "-ListSet memory -Counter `"\processor(*)\% processor time`""
                ExpectedErrorId = "AmbiguousParameterSet,Microsoft.PowerShell.Commands.ImportCounterCommand"
            }
            @{
                Name = "Fails when given -ListSet and -StartTime"
                StartTime = Get-Date
                Parameters = "-ListSet memory"
                ExpectedErrorId = "AmbiguousParameterSet,Microsoft.PowerShell.Commands.ImportCounterCommand"
            }
            @{
                Name = "Fails when given -ListSet and -StartTime"
                StartTime = Get-Date
                Parameters = "-ListSet memory"
                ExpectedErrorId = "AmbiguousParameterSet,Microsoft.PowerShell.Commands.ImportCounterCommand"
            }
            @{
                Name = "Fails when given -Summary and -EndTime"
                EndTime = Get-Date
                Parameters = "-Summary"
                ExpectedErrorId = "AmbiguousParameterSet,Microsoft.PowerShell.Commands.ImportCounterCommand"
            }
            @{
                Name = "Fails when given -Summary and -EndTime"
                EndTime = Get-Date
                Parameters = "-Summary"
                ExpectedErrorId = "AmbiguousParameterSet,Microsoft.PowerShell.Commands.ImportCounterCommand"
            }
            @{
                Name = "Fails when BLG file is corrupt"
                Path = $corruptBlgPath
                ExpectedErrorCategory = [System.Management.Automation.ErrorCategory]::InvalidResult
                ExpectedErrorId = "CounterApiError,Microsoft.PowerShell.Commands.ImportCounterCommand"
            }
        )

        foreach ($testCase in $testCases)
        {
            RunExpectedFailureTest $testCase
        }

        It "Multiple errors when BLG file contains bad sample data" -Skip:$(SkipCounterTests) {
            $errVar = $null
            $result = Import-Counter $badSamplesBlgPath -ErrorVariable errVar -ErrorAction SilentlyContinue
            $result.Length | Should -Be 275
            $errVar.Count | Should -Be 5
            foreach ($err in $errVar)
            {
                $err.CategoryInfo.Category | Should -BeExactly "InvalidResult"
                $err.FullyQualifiedErrorId | Should -BeExactly "CounterApiError,Microsoft.PowerShell.Commands.ImportCounterCommand"
            }
        }
    }

    Context "Import tests" {
        $performantTestCases = @(
            @{
                Name = "Can import all samples"
            }
            @{
                Name = "Can import samples beginning at a given start time"
                TimestampIndexes = @{
                    First = 6
                }
            }
            @{
                Name = "Can import samples ending at a given end time"
                TimestampIndexes = @{
                    Last = 10
                }
            }
            @{
                Name = "Can import samples of a given timestamp range"
                TimestampIndexes = @{
                    First = 4
                    Last = 19
                }
            }
            @{
                Name = "Can acquire a named list set"
                UseKnownSamples = $true
                Parameters = "-ListSet $($setNames.Memory)"
                Script = {
                    $result.Length | Should -Be 1
                    $result[0].CounterSetName | Should -BeExactly $setNames.Memory
                }
            }
            @{
                Name = "Can acquire list set from an array of names"
                UseKnownSamples = $true
                Parameters = "-ListSet $(TranslateCounterName 'memory'), $(TranslateCounterName 'processor')"
                Script = {
                    $result.Length | Should -Be 2
                    $names = @()
                    foreach ($set in $result)
                    {
                        $names = $names + $set.CounterSetName
                    }
                    $names -Contains $setNames.Memory | Should -BeTrue
                    $names -Contains $setNames.Processor | Should -BeTrue
                }
            }
            @{
                # This test will be skipped for non-English languages, since
                # there is no reasonable way to construct a wild-card pattern
                # that will, for every language, result in a known set of values
                # or evan a set with a known minimum number of items.
                Name = "Can acquire list set via wild-card name"
                SkipTest = (-not (Get-Culture).Name.StartsWith("en-", [StringComparison]::InvariantCultureIgnoreCase))
                UseKnownSamples = $true
                Parameters = "-ListSet p*"
                Script = {
                    $result.Length | Should -BeGreaterThan 1
                    $names = @()
                    foreach ($set in $result)
                    {
                        $names = $names + $set.CounterSetName
                    }
                    $names -Contains "physicaldisk" | Should -BeTrue
                    $names -Contains "processor" | Should -BeTrue
                }
            }
            @{
                # This test will be skipped for non-English languages, since
                # there is no reasonable way to construct a wild-card pattern
                # that will, for every language, result in a known set of values
                # or evan a set with a known minimum number of items.
                Name = "Can acquire list set from an array of names including wild-card"
                SkipTest = (-not (Get-Culture).Name.StartsWith("en-", [StringComparison]::InvariantCultureIgnoreCase))
                UseKnownSamples = $true
                Parameters = "-ListSet memory, p*"
                Script = {
                    $result.Length | Should -BeGreaterThan 2
                    $names = @()
                    foreach ($set in $result) { $names = $names + $set.CounterSetName }
                    $names -Contains "memory" | Should -BeTrue
                    $names -Contains "processor" | Should -BeTrue
                    $names -Contains "physicaldisk" | Should -BeTrue
                }
            }
        )

        foreach ($testCase in $performantTestCases)
        {
            RunPerFileTypeTests $testCase
        }
    }
}

Describe "Import-Counter cmdlet does not run on IoT" -Tags "CI" {

    It "Import-Counter throws PlatformNotSupportedException" -Skip:$(-not [System.Management.Automation.Platform]::IsIoT)  {
        { Import-Counter -Path "$testDrive\ProcessorData.blg" } |
	    Should -Throw -ErrorId "System.PlatformNotSupportedException,Microsoft.PowerShell.Commands.ImportCounterCommand"
    }
}

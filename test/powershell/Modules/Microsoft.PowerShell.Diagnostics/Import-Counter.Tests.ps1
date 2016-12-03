<############################################################################################
 # File: Import-Counter.Tests.ps1
 # Provides Pester tests for the Import-Counter cmdlet.
 ############################################################################################>

# Translate a counter set name to a localized name
function TranslateCounterSetName($counterName)
{
    $id = Get-PerformanceCounterID $counterName
    if ($id -ne $null)
    {
        $translatedName = Get-PerformanceCounterLocalName $id
        if ($translatedName -ne $null)
        {
            return $translatedName
        }
    }

    return $counterName
}

Describe "Tests for Import-Counter cmdlet" -Tags "CI" {

    BeforeAll {
        $cmdletName = "Import-Counter"

        . "$PSScriptRoot/CounterTestHelperFunctions.ps1"

        $counterPaths = @(
            (TranslateCounterPath("\Memory\Available Bytes"))
            (TranslateCounterPath("\processor(*)\% Processor time"))
            (TranslateCounterPath("\Processor(_Total)\% Processor Time"))
            (TranslateCounterPath("\PhysicalDisk(_Total)\Current Disk Queue Length"))
            (TranslateCounterPath("\PhysicalDisk(_Total)\Disk Bytes/sec"))
            (TranslateCounterPath("\PhysicalDisk(_Total)\Disk Read Bytes/sec"))
        )
        $setNames = @{
            Memory = (TranslateCounterSetName("memory"))
            PhysicalDisk = (TranslateCounterSetName("physicaldisk"))
            Processor = (TranslateCounterSetName("processor"))
        }

        $outputDirectory = $PSScriptRoot
        $rootFilename = "exportedCounters"
        $blgPath = "$outputDirectory/$rootFilename.blg"
        $csvPath = "$outputDirectory/$rootFilename.csv"
        $tsvPath = "$outputDirectory/$rootFilename.tsv"
        $badSamplesBlgPath = "$outputDirectory/assets/BadCounterSamples.blg"
        $corruptBlgPath = "$outputDirectory/assets/CorruptBlg.blg"
        $notFoundPath = "$outputDirectory/DAD288C0-72F8-47D3-8C54-C69481B528DF.blg"

        Write-Host "Gathering counter values for export..."
        $counterSamples = Get-Counter -Counter $counterPaths -MaxSamples 25
        Export-Counter -Force -FileFormat "blg" -Path $blgPath -InputObject $counterSamples
        Export-Counter -Force -FileFormat "csv" -Path $csvPath -InputObject $counterSamples
        Export-Counter -Force -FileFormat "tsv" -Path $tsvPath -InputObject $counterSamples

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
                $filePath = "$outputDirectory/$rootFilename.blg"
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
            It "$($testCase.Name)" {

                if ($testCase.TimestampIndexes)
                {
                    if ($testCase.TimestampIndexes.ContainsKey("First"))
                    {
                        $testCase.StartTime = $counterSamples[$testCase.TimestampIndexes.First].Timestamp

                        # Exporting loses precision of DateTime objects, which results in almost always
                        # missing the first expected item when importing with a controlling StartTime.
                        # So, we'll adjust the precision of the timestamp we use for the StartTime value.
                        $testCase.StartTime = New-Object System.DateTime ([Int64]([math]::floor($testCase.StartTime.Ticks / 10000)) * 10000)
                    }
                    if ($testCase.TimestampIndexes.ContainsKey("Last"))
                    {
                        $testCase.EndTime = $counterSamples[$testCase.TimestampIndexes.Last].Timestamp
                    }
                }

                $cmd = ConstructCommand $testCase
                Write-Host "Command to run: $cmd"
                $cmd = $cmd + " -ErrorAction SilentlyContinue -ErrorVariable errVar"

                $errVar = $null
                $sb = [scriptblock]::Create($cmd)
                $result = &$sb
                $errVar | Should BeNullOrEmpty

                if ($testCase.ContainsKey("Script"))
                {
                    &$testCase.Script
                }
                else
                {
                    if ($testCase.TimestampIndexes)
                    {
                        $start = 0
                        $end = $counterSamples.Length - 1
                        if ($testCase.TimestampIndexes.ContainsKey("First"))
                        {
                            $start = $testCase.TimestampIndexes.First
                        }
                        if ($testCase.TimestampIndexes.ContainsKey("Last"))
                        {
                            $end = $testCase.TimestampIndexes.Last
                        }

                        CompareCounterSets $result $counterSamples[$start..$end]
                    }
                    else
                    {
                        CompareCounterSets $result $counterSamples
                    }
                }
            }
        }

        # Run a test for each file format
        function RunPerFileTypeTests($testCase)
        {
            if ($testCase.UseKnownSamples)
            {
                $basePath = "$outputDirectory/assets/CounterSamples"
                $formats = @{
                    "BLG" = "$basePath.blg"
                    "CSV" = "$basePath.blg"
                    "TSV" = "$basePath.blg"
                }
            }
            else
            {
                $formats = @{
                    "BLG" = $blgPath
                    "CSV" = $csvPath
                    "TSV" = $tsvPath
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
            It "$($testCase.Name)" {
                $cmd = ConstructCommand $testCase
                Write-Host "Command to run: $cmd"
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
                    try
                    {
                        $sb = [ScriptBlock]::Create($cmd)
                        &$sb
                        throw "Did not throw expected exception"
                    }
                    catch
                    {
                        if ($testCase.ExpectedErrorId)
                        {
                            $_.FullyQualifiedErrorId | Should Be $testCase.ExpectedErrorId
                        }
                        if ($testCase.ExpectedErrorCategory)
                        {
                            $_.CategoryInfo.Category | Should Be $testCase.ExpectedErrorCategory
                        }
                    }
                }
            }
        }
    }

    AfterAll {
        Remove-Item $blgPath -ErrorAction SilentlyContinue
        Remove-Item $csvPath -ErrorAction SilentlyContinue
        Remove-Item $tsvPath -ErrorAction SilentlyContinue
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

        It "Multiple errors when BLG file contains bad sample data" {
            $errVar = $null
            $result = Import-Counter $badSamplesBlgPath -ErrorVariable errVar -ErrorAction SilentlyContinue
            $result.Length | Should Be 275
            $errVar.Count | Should Be 5
            foreach ($err in $errVar)
            {
                $err.CategoryInfo.Category | Should Be "InvalidResult"
                $err.FullyQualifiedErrorId | SHould Be "CounterApiError,Microsoft.PowerShell.Commands.ImportCounterCommand"
            }
        }
    }

    Context "Import tests" {
        $performatTestCases = @(
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
                Name = "Can acquire summary information"
                UseKnownSamples = $true
                Parameters = "-Summary"
                Script = {
                    $result.SampleCount | Should Be 25
                    $result.OldestRecord | Should Be (Get-Date -Year 2016 -Month 11 -Day 26 -Hour 13 -Minute 46 -Second 30 -Millisecond 874)
                    $result.NewestRecord | Should Be (Get-Date -Year 2016 -Month 11 -Day 26 -Hour 13 -Minute 47 -Second 42 -Millisecond 983)
                }
            }
            @{
                Name = "Can acquire a named list set"
                UseKnownSamples = $true
                Parameters = "-ListSet $($setNames.Memory)"
                Script = {
                    $result.Length | Should Be 1
                    $result[0].CounterSetName | Should Be $setNames.Memory
                }
            }
            @{
                Name = "Can acquire list set from an array of names"
                UseKnownSamples = $true
                Parameters = "-ListSet $(TranslateCounterSetName 'memory'), $(TranslateCounterSetName 'processor')"
                Script = {
                    $result.Length | Should Be 2
                    $names = @()
                    foreach ($set in $result)
                    {
                        $names = $names + $set.CounterSetName
                    }
                    $names -Contains $setNames.Memory | Should Be $true
                    $names -Contains $setNames.Processor | Should Be $true
                }
            }
            @{
                # This test should work for English, but other languages are
                # problematic since there is no reasonable way to construct
                # a wild-card pattern that will, for every language, result
                # in a known set of values or evan a set with a known minimum
                # number of items.
                Name = "Can acquire list set via wild-card name"
                UseKnownSamples = $true
                Parameters = "-ListSet p*"
                Script = {
                    $result.Length | Should Be 2
                    $names = @()
                    foreach ($set in $result)
                    {
                        $names = $names + $set.CounterSetName
                    }
                    $names -Contains "physicaldisk" | Should Be $true
                    $names -Contains "processor" | Should Be $true
                }
            }
            @{
                # This test should work for English, but other languages are
                # problematic since there is no reasonable way to construct
                # a wild-card pattern that will, for every language, result
                # in a known set of values or evan a set with a known minimum
                # number of items.
                Name = "Can acquire list set from an array of names including wild-card"
                UseKnownSamples = $true
                Parameters = "-ListSet memory, p*"
                Script = {
                    $result.Length | Should Be 3
                    $names = @()
                    foreach ($set in $result) { $names = $names + $set.CounterSetName }
                    $names -Contains "memory" | Should Be $true
                    $names -Contains "processor" | Should Be $true
                    $names -Contains "physicaldisk" | Should Be $true
                }
            }
        )

        foreach ($testCase in $performatTestCases)
        {
            RunPerFileTypeTests $testCase
        }
    }
}

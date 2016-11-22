<############################################################################################
 # File: Export-Counter.Tests.ps1
 # Provides Pester tests for the Export-Counter cmdlet.
 ############################################################################################>
Describe "Tests for Export-Counter cmdlet" -Tags "CI" {

    BeforeAll {
        $cmdletName = "Export-Counter"

        . "$PSScriptRoot/CounterTestsCommon.ps1"

        $outputDirectory = $PSScriptRoot
        $rootFilename = "exportedCounters"
        $filePath = $null
        $counterNames = @(
            (TranslateCounterPath("\Memory\Available Bytes"))
            (TranslateCounterPath("\Processor(*)\% Processor Time"))
            (TranslateCounterPath("\Processor(_Total)\% Processor Time"))
            (TranslateCounterPath("\PhysicalDisk(_Total)\Current Disk Queue Length"))
            (TranslateCounterPath("\PhysicalDisk(_Total)\Disk Bytes/sec"))
            (TranslateCounterPath("\PhysicalDisk(_Total)\Disk Read Bytes/sec"))
        )
        $counterValues = $null

        # Test the results of Export-Counter by importing the exported
        # counters and comparing the two sets
        function CheckExportResults
        {
            Test-Path $filePath | Should Be $true
            $importedCounterValues = Import-Counter $filePath

            CompareCounterSets $counterValues $importedCounterValues
        }

        # Run a test case
        function RunTest($testCase)
        {
            It "$($testCase.Name)" {
                $getCounterParams = ""
                if ($testCase.ContainsKey("GetCounterParams"))
                {
                    $getCounterParams = $testCase.GetCounterParams
                }
                $counterValues = &([ScriptBlock]::Create("Get-Counter -Counter `$counterNames $getCounterParams"))

                # build up a command
                $filePath = ""
                $pathParam = ""
                $formatParam = ""
                if ($testCase.ContainsKey("Path"))
                {
                    $filePath = $testCase.Path
                }
                else
                {
                    if ($testCase.ContainsKey("FileFormat"))
                    {
                        $formatParam = "-FileFormat $($testCase.FileFormat)"
                        $filePath = "$outputDirectory/$rootFilename.$($testCase.FileFormat)"
                    }
                    else
                    {
                        $filePath = "$outputDirectory/$rootFilename.blg"
                    }
                }
                if ($testCase.NoDashPath)
                {
                    $pathParam = $filePath
                }
                else
                {
                    $pathParam = "-Path $filePath"
                }
                $cmd = "$cmdletName $pathParam $formatParam -InputObject `$counterValues $($testCase.Parameters) -ErrorAction Stop"

                if ($testCase.CreateFirst)
                {
                    if (-not (Test-Path $filePath))
                    {
                        New-Item $filePath -ItemType file
                    }
                }

                try
                {
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
                            $_.FullyQualifiedErrorId | Should Be $testCase.ExpectedErrorId
                        }
                    }
                }
                finally
                {
                    if ($filePath)
                    {
                        Remove-Item $filePath -ErrorAction SilentlyContinue
                    }
                }
            }
        }
    }


    Context "Validate incorrect parameter usage" {
        $testCases = @(
            @{
                Name = "Fails when given invalid path"
                Path = "c:\DAD288C0-72F8-47D3-8C54-C69481B528DF\counterExport.blg"
                Parameters = ""
                ExpectedErrorId = "FileCreateFailed,Microsoft.PowerShell.Commands.ExportCounterCommand"
            }
            @{
                Name = "Fails when given null path"
                Path = "`$null"
                Parameters = ""
                ExpectedErrorId = "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.ExportCounterCommand"
            }
            @{
                Name = "Fails when -Path specified but no path given"
                Path = ""
                Parameters = ""
                ExpectedErrorId = "MissingArgument,Microsoft.PowerShell.Commands.ExportCounterCommand"
            }
            @{
                Name = "Fails when given -Circular without -MaxSize"
                Parameters = "-Circular"
                ExpectedErrorId = "CounterCircularNoMaxSize,Microsoft.PowerShell.Commands.ExportCounterCommand"
            }
            @{
                Name = "Fails when given -Circular with zero -MaxSize"
                Parameters = "-Circular -MaxSize 0"
                ExpectedErrorId = "CounterCircularNoMaxSize,Microsoft.PowerShell.Commands.ExportCounterCommand"
            }
            @{
                Name = "Fails when -MaxSize < zero"
                Parameters = "-MaxSize -2"
                ExpectedErrorId = "CannotConvertArgumentNoMessage,Microsoft.PowerShell.Commands.ExportCounterCommand"
            }
            @{
                Name = "Fails when given invalid file format"
                FileFormat = "dat"
                Parameters = ""
                ExpectedErrorId = "CounterInvalidFormat,Microsoft.PowerShell.Commands.ExportCounterCommand"
            }
        )

        foreach ($testCase in $testCases)
        {
            RunTest $testCase
        }
    }

    Context "Export tests" {
        $testCases = @(
            @{
                Name = "Fails when output file exists"
                CreateFirst = $true
                Parameters = ""
                ExpectedErrorId = "CounterFileExists,Microsoft.PowerShell.Commands.ExportCounterCommand"
            }
            @{
                Name = "Can force overwriting existing file"
                Parameters = "-Force"
                Script = { Test-Path $path | Should Be $true }
            }
            @{
                Name = "Can export BLG format"
                FileFormat = "blg"
                Parameters = ""
                GetCounterParams = "-MaxSamples 5"
                Script = { CheckExportResults }
            }
            @{
                Name = "Exports BLG format by default"
                Parameters = ""
                GetCounterParams = "-MaxSamples 5"
                Script = { CheckExportResults }
            }
            @{
                Name = "Can export CSV format"
                FileFormat = "csv"
                Parameters = ""
                GetCounterParams = "-MaxSamples 2"
                Script = { CheckExportResults }
            }
            @{
                Name = "Can export TSV format"
                FileFormat = "tsv"
                Parameters = ""
                GetCounterParams = "-MaxSamples 5"
                Script = { CheckExportResults }
            }
        )

        foreach ($testCase in $testCases)
        {
            RunTest $testCase
        }
    }
}

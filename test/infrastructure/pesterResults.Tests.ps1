# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Guards the build script against reporting a green run when Pester actually failed.
# Deciding on failure counts alone misses two cases: a file whose Discovery phase throws,
# and a failing AfterAll. Both leave FailedCount at 0 and are absent from the NUnit file.

Describe "Pester run result handling in build.psm1" -Tags CI {

    BeforeAll {
        Import-Module "$PSScriptRoot/../../build.psm1" -Force

        # The child process has to use the same Pester as this run, otherwise the test would be
        # asserting on whatever version happens to be newest on the machine.
        $script:pesterModulePath = (Get-Module Pester).Path

        $script:fixtureDir = Join-Path $TestDrive 'fixtures'
        New-Item -ItemType Directory -Path $script:fixtureDir -Force | Out-Null

        # One file per failure kind, plus the shapes that must stay green.
        # Statements are separated by ';' on purpose: 'BeforeAll { } It "t" { }' with no
        # separator parses as a single call to BeforeAll with four arguments.
        $script:fixtures = @{
            DiscoveryFails = 'Describe "d" { It "unreachable" -NoSuchParameter { $true | Should -BeTrue } }'
            BeforeAllFails = 'Describe "d" { BeforeAll { throw "boom in BeforeAll" }; It "t" { $true | Should -BeTrue } }'
            AfterAllFails  = 'Describe "d" { AfterAll { throw "boom in AfterAll" }; It "t" { $true | Should -BeTrue } }'
            TestFails      = 'Describe "d" { It "t" { $false | Should -BeTrue } }'
            AllGreen       = 'Describe "d" { It "t" { $true | Should -BeTrue } }'
            Inconclusive   = 'Describe "d" { It "t" { Set-ItResult -Inconclusive -Because "flaky" } }'
            Skipped        = 'Describe "d" { It "t" -Skip { $true | Should -BeTrue } }'
            NoTests        = 'Describe "d" { }'
        }
        foreach ($name in $script:fixtures.Keys) {
            Set-Content -Path (Join-Path $script:fixtureDir "$name.tests.ps1") -Value $script:fixtures[$name]
        }

        # Runs one fixture through the same projection that Start-PSPester splices into the
        # child process, and round-trips it through CliXml exactly as the child does.
        function Get-FixtureSummary {
            param([string] $Name)

            $resultsFile = Join-Path $TestDrive "$Name.xml"
            $summaryFile = Get-PSPesterSummaryPath -TestResultsFile $resultsFile
            Remove-Item $summaryFile -Force -ErrorAction SilentlyContinue

            $script = @"
Import-Module '$script:pesterModulePath'
`$config = New-PesterConfiguration
`$config.Run.Path = '$(Join-Path $script:fixtureDir "$Name.tests.ps1")'
`$config.Run.PassThru = `$true
`$config.Run.FailOnNullOrEmptyForEach = `$false
`$config.TestResult.Enabled = `$true
`$config.TestResult.OutputPath = '$resultsFile'
`$config.TestResult.OutputFormat = 'NUnitXml'
`$config.Output.Verbosity = 'None'
Invoke-Pester -Configuration `$config $(Get-PSPesterSummaryProjection) | Export-Clixml -Path '$summaryFile' -Force
"@
            # A separate process, so a fixture that throws cannot disturb the test run itself.
            & (Get-Process -Id $PID).Path -NoProfile -Command $script *> $null

            return Import-PSPesterSummary -TestResultsFile $resultsFile
        }
    }

    Context "The fixtures are what they claim to be" {

        It "'<Name>' is valid PowerShell" -TestCases @(
            @{ Name = 'BeforeAllFails' }
            @{ Name = 'AfterAllFails' }
            @{ Name = 'TestFails' }
            @{ Name = 'AllGreen' }
            @{ Name = 'Inconclusive' }
            @{ Name = 'Skipped' }
            @{ Name = 'NoTests' }
        ) {
            param($Name)

            $errors = $null
            $null = [System.Management.Automation.Language.Parser]::ParseFile(
                (Join-Path $script:fixtureDir "$Name.tests.ps1"), [ref]$null, [ref]$errors)
            $errors | Should -BeNullOrEmpty
        }

        It "only 'DiscoveryFails' fails discovery" -TestCases @(
            @{ Name = 'BeforeAllFails' }
            @{ Name = 'AfterAllFails' }
            @{ Name = 'TestFails' }
            @{ Name = 'AllGreen' }
        ) {
            param($Name)

            (Get-FixtureSummary -Name $Name).FailedContainersCount | Should -Be 0
        }
    }

    Context "The projection survives serialization" {

        It "produces a readable summary for '<Name>'" -TestCases @(
            @{ Name = 'DiscoveryFails' }
            @{ Name = 'BeforeAllFails' }
            @{ Name = 'AfterAllFails' }
            @{ Name = 'TestFails' }
            @{ Name = 'AllGreen' }
        ) {
            param($Name)

            $summary = Get-FixtureSummary -Name $Name
            $summary | Should -Not -BeNullOrEmpty -Because 'the projection has to round-trip through Import-Clixml'
            $summary.Result | Should -Not -BeNullOrEmpty
        }
    }

    Context "Result reflects every kind of failure" {

        It "reports '<Name>' as <Expected>" -TestCases @(
            @{ Name = 'DiscoveryFails'; Expected = 'Failed' }
            @{ Name = 'BeforeAllFails'; Expected = 'Failed' }
            @{ Name = 'AfterAllFails';  Expected = 'Failed' }
            @{ Name = 'TestFails';      Expected = 'Failed' }
            @{ Name = 'AllGreen';       Expected = 'Passed' }
            @{ Name = 'Inconclusive';   Expected = 'Passed' }
            @{ Name = 'Skipped';        Expected = 'Passed' }
            @{ Name = 'NoTests';        Expected = 'Passed' }
        ) {
            param($Name, $Expected)

            (Get-FixtureSummary -Name $Name).Result | Should -BeExactly $Expected
        }

        It "keeps FailedCount at 0 for '<Name>', which is why counts alone are not enough" -TestCases @(
            @{ Name = 'DiscoveryFails' }
            @{ Name = 'AfterAllFails' }
        ) {
            param($Name)

            $summary = Get-FixtureSummary -Name $Name
            $summary.FailedCount | Should -Be 0
            $summary.Result | Should -BeExactly 'Failed'
        }

        It "records the error message for a failed discovery" {
            $summary = Get-FixtureSummary -Name 'DiscoveryFails'
            $summary.FailedContainersCount | Should -Be 1
            ($summary.TestResult | Where-Object Kind -eq 'Container').FailureMessage |
                Should -Match 'NoSuchParameter'
        }

        # The exact wording Pester puts on a throw inside BeforeAll/AfterAll changes between
        # versions, so this asserts the invariant that matters instead: the message lives on the
        # block, and reporting only failed tests would print nothing useful.
        It "puts the message on the block, not on the test, for a failed BeforeAll" {
            $summary = Get-FixtureSummary -Name 'BeforeAllFails'

            $block = @($summary.TestResult | Where-Object Kind -eq 'Block')
            $block.Count | Should -Be 1
            $block[0].FailureMessage | Should -Not -BeNullOrEmpty

            $test = @($summary.TestResult | Where-Object Kind -eq 'Test')
            $test.Count | Should -Be 1
            $test[0].FailureMessage | Should -BeNullOrEmpty -Because 'this is why failed tests alone are not enough to report on'
        }

        It "records a block failure for a failed AfterAll, which produces no failed test at all" {
            $summary = Get-FixtureSummary -Name 'AfterAllFails'

            $summary.FailedCount | Should -Be 0
            $block = @($summary.TestResult | Where-Object Kind -eq 'Block')
            $block.Count | Should -Be 1
            $block[0].FailureMessage | Should -Not -BeNullOrEmpty
        }
    }

    Context "Assert-PSPesterRunPassed" {

        It "throws for '<Name>'" -TestCases @(
            @{ Name = 'DiscoveryFails' }
            @{ Name = 'BeforeAllFails' }
            @{ Name = 'AfterAllFails' }
            @{ Name = 'TestFails' }
        ) {
            param($Name)

            { Assert-PSPesterRunPassed -Summary (Get-FixtureSummary -Name $Name) -TestArea 'fixtures' } |
                Should -Throw -ExpectedMessage '*fixtures*'
        }

        It "does not throw for '<Name>'" -TestCases @(
            @{ Name = 'AllGreen' }
            @{ Name = 'Inconclusive' }
            @{ Name = 'Skipped' }
            @{ Name = 'NoTests' }
        ) {
            param($Name)

            { Assert-PSPesterRunPassed -Summary (Get-FixtureSummary -Name $Name) -TestArea 'fixtures' } |
                Should -Not -Throw
        }

        It "falls back to failure counts when the summary predates the Result property" {
            $legacy = [pscustomobject]@{
                TotalCount = 1; FailedCount = 0; FailedBlocksCount = 0
                FailedContainersCount = 1; TestResult = @()
            }
            { Assert-PSPesterRunPassed -Summary $legacy -TestArea 'fixtures' } | Should -Throw
        }

        It "does not throw for a legacy summary with no failures at all" {
            $legacy = [pscustomobject]@{
                TotalCount = 1; FailedCount = 0; FailedBlocksCount = 0
                FailedContainersCount = 0; TestResult = @()
            }
            { Assert-PSPesterRunPassed -Summary $legacy -TestArea 'fixtures' } | Should -Not -Throw
        }
    }

    Context "Test-PSPesterResults reads the summary next to the result file" {

        It "throws for a failed discovery even though the NUnit file records no failure" {
            $summary = Get-FixtureSummary -Name 'DiscoveryFails'
            $summary | Should -Not -BeNullOrEmpty

            $resultsFile = Join-Path $TestDrive 'DiscoveryFails.xml'
            # The NUnit file is the reason this case used to pass: it records nothing.
            ([xml](Get-Content -Raw $resultsFile)).'test-results'.failures | Should -Be 0

            { Test-PSPesterResults -TestResultsFile $resultsFile -TestArea 'fixtures' } | Should -Throw
        }

        It "does not throw for a green run" {
            $null = Get-FixtureSummary -Name 'AllGreen'
            { Test-PSPesterResults -TestResultsFile (Join-Path $TestDrive 'AllGreen.xml') -TestArea 'fixtures' } |
                Should -Not -Throw
        }
    }

    Context "Get-PSPesterSummaryPath and Import-PSPesterSummary" {

        It "puts the summary next to the result file" {
            Get-PSPesterSummaryPath -TestResultsFile '/tmp/results.xml' | Should -BeExactly '/tmp/results.xml.summary.clixml'
        }

        It "returns null when there is no summary, so foreign result files still work" {
            Import-PSPesterSummary -TestResultsFile (Join-Path $TestDrive 'does-not-exist.xml') | Should -BeNullOrEmpty
        }
    }
}

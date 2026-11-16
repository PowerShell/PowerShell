# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module "$PSScriptRoot/../../tools/codecoverage.psm1" -Force

Describe 'CI .NET code coverage' {
    InModuleScope codecoverage {
        BeforeAll {
            function New-CoverageFixture {
                param([string] $Path, [string] $Filename, [int] $FirstHits, [int] $SecondHits)
                $escaped = [System.Security.SecurityElement]::Escape($Filename)
                @"
<!DOCTYPE coverage SYSTEM "https://invalid.example/coverage.dtd">
<coverage><packages><package><classes>
  <class filename="$escaped"><lines>
    <line number="1" hits="$FirstHits"/><line number="2" hits="$SecondHits"/>
  </lines></class>
  <class filename="$escaped"><lines><line number="1" hits="0"/></lines></class>
  <class filename="test/xUnit/Test.cs"><lines><line number="1" hits="100"/></lines></class>
</classes></package></packages></coverage>
"@ | Set-Content -LiteralPath $Path
            }
        }

        It 'normalizes paths across OS/build roots and unions executable lines rather than percentages' {
            $windows = Join-Path $TestDrive 'windows.xml'
            $linux = Join-Path $TestDrive 'linux.xml'
            New-CoverageFixture $windows 'D:\a\PowerShell\PowerShell\src\Sample.cs' 2 0
            New-CoverageFixture $linux '/_/src/Sample.cs' 0 3
            $first = ConvertFrom-CodeCoverage -Path $windows -SourceFile @('src/Sample.cs')
            $second = ConvertFrom-CodeCoverage -Path $linux -SourceFile @('src/Sample.cs')
            $merged = Merge-CodeCoverage -Run @(@{ Files = $first }, @{ Files = $second })
            $result = Get-CodeCoverageSummary -File $merged
            $result.Total | Should -Be 2
            $result.Covered | Should -Be 2
            $result.Percent | Should -Be '100.00%'
            (Get-CodeCoverageSummary -File $first).Percent | Should -Be '50.00%'
        }

        It 'rejects empty/unrecognized source coverage rather than reporting success' {
            $xmlPath = Join-Path $TestDrive 'unrecognized.xml'
            New-CoverageFixture $xmlPath '/tmp/external.cs' 1 0
            { ConvertFrom-CodeCoverage -Path $xmlPath -SourceFile @('src/Sample.cs') } | Should -Throw '*No tracked*'
        }

        Context 'Collector lifecycle' {
            BeforeAll {
                Import-Module "$PSScriptRoot/../../build.psm1" -Force
            }

            BeforeEach {
                $script:collectorEnvironment = @{}
                foreach ($name in 'PS_CODE_COVERAGE', 'XDG_RUNTIME_DIR', 'TMPDIR') {
                    $script:collectorEnvironment[$name] = [System.Environment]::GetEnvironmentVariable($name)
                    [System.Environment]::SetEnvironmentVariable($name, "fixture-$name")
                }
                $script:binaryDirectory = Join-Path $TestDrive 'binaries'
                $script:outputDirectory = Join-Path $TestDrive 'coverage'
                $null = New-Item -ItemType Directory -Path $binaryDirectory -Force
                'binary' | Set-Content (Join-Path $binaryDirectory 'System.Management.Automation.dll')
                'symbols' | Set-Content (Join-Path $binaryDirectory 'System.Management.Automation.pdb')
                Mock Install-CodeCoverageTool { 'collector' }
                Mock Start-CodeCoverageCollector {}
                Mock Start-NativeExecution {
                    param($ScriptBlock)
                    if ($ScriptBlock.ToString() -match 'git') { 'src/Sample.cs' }
                }
                Mock Get-CodeCoverageSequencePoint { @{ 'src/Sample.cs' = @{ '1' = $false; '2' = $false } } }
                Mock ConvertFrom-CodeCoverage { @{ 'src/Sample.cs' = @{ '1' = $true } } }
            }

            AfterEach {
                foreach ($name in $script:collectorEnvironment.Keys) {
                    [System.Environment]::SetEnvironmentVariable($name, $script:collectorEnvironment[$name])
                }
            }

            It 'executes tests once and retains zero-hit executable lines' {
                $script:executionCount = 0
                Mock Start-CodeCoverageCollector {
                    $env:PS_CODE_COVERAGE | Should -BeExactly '1'
                    if (-not $IsWindows) {
                        $env:XDG_RUNTIME_DIR | Should -Match '^/tmp/p[A-Za-z0-9_-]{22}/$'
                        $env:TMPDIR | Should -BeExactly $env:XDG_RUNTIME_DIR
                    }
                }
                Invoke-CodeCoverageTest -BinaryDirectory $binaryDirectory -OutputDirectory $outputDirectory -Suite fixture -Test {
                    $script:executionCount++
                    $env:PS_CODE_COVERAGE | Should -BeExactly '1'
                    if (-not $IsWindows) {
                        $env:XDG_RUNTIME_DIR | Should -Match '^/tmp/p[A-Za-z0-9_-]{22}/$'
                        $env:TMPDIR | Should -BeExactly $env:XDG_RUNTIME_DIR
                    }
                }
                $executionCount | Should -Be 1
                foreach ($name in 'PS_CODE_COVERAGE', 'XDG_RUNTIME_DIR', 'TMPDIR') {
                    [System.Environment]::GetEnvironmentVariable($name) | Should -BeExactly "fixture-$name"
                }
                $data = Get-Content (Join-Path $outputDirectory 'coverage.json') -Raw | ConvertFrom-Json -AsHashtable
                $data.Passed | Should -BeTrue
                $data.Files['src/Sample.cs']['1'] | Should -BeTrue
                $data.Files['src/Sample.cs']['2'] | Should -BeFalse
                Should -Invoke Start-NativeExecution -Times 1 -Exactly -ParameterFilter { $ScriptBlock.ToString() -match 'uninstrument' }
            }

            It 'restores binaries and preserves a test failure if finalization also fails' {
                Mock Start-NativeExecution {
                    param($ScriptBlock)
                    if ($ScriptBlock.ToString() -match 'shutdown') { throw 'collector failure' }
                    if ($ScriptBlock.ToString() -match 'git') { 'src/Sample.cs' }
                }
                Mock Write-Error {}
                { Invoke-CodeCoverageTest -BinaryDirectory $binaryDirectory -OutputDirectory $outputDirectory -Suite fixture -Test {
                    throw 'original test failure'
                } } | Should -Throw '*original test failure*'
                foreach ($name in 'PS_CODE_COVERAGE', 'XDG_RUNTIME_DIR', 'TMPDIR') {
                    [System.Environment]::GetEnvironmentVariable($name) | Should -BeExactly "fixture-$name"
                }
                Should -Invoke Start-NativeExecution -Times 1 -Exactly -ParameterFilter { $ScriptBlock.ToString() -match 'uninstrument' }
                Should -Invoke Write-Error -Times 1 -Exactly -ParameterFilter { $ErrorRecord.Exception.Message -eq 'collector failure' }
            }

            It 'restores the environment when instrumentation fails before tests start' {
                Mock Start-NativeExecution {
                    param($ScriptBlock)
                    if ($ScriptBlock.ToString() -match 'git') { 'src/Sample.cs' }
                    if ($ScriptBlock.ToString() -match ' instrument ') { throw 'instrumentation failure' }
                }
                { Invoke-CodeCoverageTest -BinaryDirectory $binaryDirectory -OutputDirectory $outputDirectory -Suite fixture -Test {
                    throw 'tests should not start'
                } } | Should -Throw '*instrumentation failure*'
                foreach ($name in 'PS_CODE_COVERAGE', 'XDG_RUNTIME_DIR', 'TMPDIR') {
                    [System.Environment]::GetEnvironmentVariable($name) | Should -BeExactly "fixture-$name"
                }
                Should -Invoke Start-CodeCoverageCollector -Times 0 -Exactly
                Should -Invoke Start-NativeExecution -Times 1 -Exactly -ParameterFilter { $ScriptBlock.ToString() -match 'uninstrument' }
            }

            It 'publishes a failed suite when Pester fails with CanHaveNoResult=<AllowEmpty>' -TestCases @(
                @{ AllowEmpty = $false }
                @{ AllowEmpty = $true }
            ) {
                param([bool] $AllowEmpty)
                Mock Write-LogGroupStart {} -ModuleName build
                Mock Write-LogGroupEnd {} -ModuleName build
                Mock Show-PSPesterError {} -ModuleName build
                $resultObject = [pscustomobject]@{
                    TotalCount = 2
                    FailedCount = 1
                    TestResult = @(
                        [pscustomobject]@{ Name = 'failed test'; Passed = $false }
                        [pscustomobject]@{ Name = 'passed test'; Passed = $true }
                    )
                }
                { Invoke-CodeCoverageTest -BinaryDirectory $binaryDirectory -OutputDirectory $outputDirectory -Suite fixture -Test {
                    Test-PSPesterResults -ResultObject $resultObject -CanHaveNoResult:$AllowEmpty
                } } | Should -Throw '*1 tests in * failed'
                $data = Get-Content (Join-Path $outputDirectory 'coverage.json') -Raw | ConvertFrom-Json -AsHashtable
                $data.Passed | Should -BeFalse
                Should -Invoke Show-PSPesterError -ModuleName build -Times 1 -Exactly -ParameterFilter { $testFailureObject.Name -eq 'failed test' }
            }

            It 'requires main-run tests while permitting an explicitly empty experimental run' {
                $resultObject = [pscustomobject]@{ TotalCount = 0; FailedCount = 0; TestResult = @() }
                { Test-PSPesterResults -ResultObject $resultObject } | Should -Throw '*NO TESTS RUN*'
                { Test-PSPesterResults -ResultObject $resultObject -CanHaveNoResult } | Should -Not -Throw
            }

            It 'matches the Windows x64 unelevated collector to the test trust level' -Skip:(-not $IsWindows) {
                $savedArchitecture = $env:RUNNER_ARCH
                try {
                    $env:RUNNER_ARCH = 'X64'
                    Invoke-CodeCoverageTest -BinaryDirectory $binaryDirectory -OutputDirectory $outputDirectory -Suite UnelevatedPesterTests-CI -Test {}
                    Should -Invoke Start-CodeCoverageCollector -Times 1 -Exactly -ParameterFilter { $Unelevate }
                } finally {
                    $env:RUNNER_ARCH = $savedArchitecture
                }
            }

            It 'isolates sudo runtime storage and matches the elevated collector environment' -Skip:$IsWindows {
                $script:runtimePath = $null
                Invoke-CodeCoverageTest -BinaryDirectory $binaryDirectory -OutputDirectory $outputDirectory -Suite ElevatedPesterTests-CI -Test {
                    param($SudoEnvironment)
                    $script:runtimePath = $env:XDG_RUNTIME_DIR
                    [System.IO.File]::GetUnixFileMode($script:runtimePath) | Should -Be ([System.IO.UnixFileMode]'UserRead,UserWrite,UserExecute')
                    $SudoEnvironment.XDG_RUNTIME_DIR | Should -Match '^/tmp/r[A-Za-z0-9_-]{22}/$'
                    $SudoEnvironment.TMPDIR | Should -BeExactly $SudoEnvironment.XDG_RUNTIME_DIR
                    $SudoEnvironment.XDG_RUNTIME_DIR | Should -Not -BeExactly $script:runtimePath
                    $pipePath = Join-Path $script:runtimePath "CoreFxPipe_CodeCoverage.pipe.$([guid]::NewGuid())"
                    $socket = [System.Net.Sockets.Socket]::new(
                        [System.Net.Sockets.AddressFamily]::Unix,
                        [System.Net.Sockets.SocketType]::Stream,
                        [System.Net.Sockets.ProtocolType]::Unspecified)
                    try {
                        $socket.Bind([System.Net.Sockets.UnixDomainSocketEndPoint]::new($pipePath))
                    } finally {
                        $socket.Dispose()
                    }
                }
                Should -Invoke Start-CodeCoverageCollector -Times 1 -Exactly -ParameterFilter { $SudoEnvironment.Count -eq 2 }
                Should -Invoke Start-NativeExecution -Times 1 -Exactly -ParameterFilter { $ScriptBlock.ToString() -match 'mkdir -m 700' }
                Should -Invoke Start-NativeExecution -Times 1 -Exactly -ParameterFilter { $ScriptBlock.ToString() -match 'rm -r' }
                Test-Path -LiteralPath $script:runtimePath | Should -BeFalse
                foreach ($name in 'PS_CODE_COVERAGE', 'XDG_RUNTIME_DIR', 'TMPDIR') {
                    [System.Environment]::GetEnvironmentVariable($name) | Should -BeExactly "fixture-$name"
                }
            }

            It 'preserves an elevated test failure when sudo runtime cleanup fails' -Skip:$IsWindows {
                Mock Start-NativeExecution {
                    param($ScriptBlock)
                    if ($ScriptBlock.ToString() -match 'git') { 'src/Sample.cs' }
                    if ($ScriptBlock.ToString() -match 'rm -r') { throw 'sudo cleanup failure' }
                }
                Mock Write-Error {}
                { Invoke-CodeCoverageTest -BinaryDirectory $binaryDirectory -OutputDirectory $outputDirectory -Suite ElevatedPesterTests-CI -Test {
                    throw 'original elevated test failure'
                } } | Should -Throw '*original elevated test failure*'
                foreach ($name in 'PS_CODE_COVERAGE', 'XDG_RUNTIME_DIR', 'TMPDIR') {
                    [System.Environment]::GetEnvironmentVariable($name) | Should -BeExactly "fixture-$name"
                }
                Should -Invoke Write-Error -Times 1 -Exactly -ParameterFilter { $ErrorRecord.Exception.Message -eq 'sudo cleanup failure' }
            }
        }

        Context 'PR commit identity and report' {
            BeforeEach {
                $script:savedEnvironment = @{}
                foreach ($name in 'GITHUB_SHA', 'GITHUB_RUN_ID', 'GITHUB_RUN_ATTEMPT', 'GITHUB_STEP_SUMMARY') {
                    $script:savedEnvironment[$name] = [System.Environment]::GetEnvironmentVariable($name)
                }
                $env:GITHUB_RUN_ID = '123'
                $env:GITHUB_RUN_ATTEMPT = '2'
                $env:GITHUB_STEP_SUMMARY = Join-Path $TestDrive 'summary.md'
                $script:repository = Join-Path $TestDrive ([guid]::NewGuid().ToString())
                $null = New-Item -ItemType Directory -Path (Join-Path $repository 'src') -Force
                Push-Location $repository
                Start-NativeExecution { git init --quiet -b main } | Out-Null
                Start-NativeExecution { git config user.name 'Coverage test' } | Out-Null
                Start-NativeExecution { git config user.email 'coverage@example.invalid' } | Out-Null
                Start-NativeExecution { git config commit.gpgsign false } | Out-Null
                Start-NativeExecution { git config core.autocrlf false } | Out-Null
                "old`nunchanged`nold" | Set-Content -LiteralPath 'src/Sample.cs'
                Start-NativeExecution { git add -- . } | Out-Null
                Start-NativeExecution { git commit --quiet -m baseline } | Out-Null
                $script:baseCommit = Start-NativeExecution { git rev-parse HEAD }
                Start-NativeExecution { git checkout --quiet -b feature } | Out-Null
                $script:eventPath = Join-Path $TestDrive 'event.json'
            }

            AfterEach {
                Pop-Location
                foreach ($name in $script:savedEnvironment.Keys) {
                    [System.Environment]::SetEnvironmentVariable($name, $script:savedEnvironment[$name])
                }
            }

            It 'reports total coverage without patch coverage for documentation-only changes' {
                'documentation' | Set-Content -LiteralPath README.md
                Start-NativeExecution { git add -- .; git commit --quiet -m documentation } | Out-Null
                $head = Start-NativeExecution { git rev-parse HEAD }
                $env:GITHUB_SHA = $head
                @{ pull_request = @{ base = @{ sha = $baseCommit }; head = @{ sha = $head } } } |
                    ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $eventPath
                $change = Get-CodeCoverageChange -EventPath $eventPath
                $change.ChangedFile | Should -Be @('README.md')
                $inputPath = Join-Path $TestDrive 'documentation-input'
                $outputPath = Join-Path $TestDrive 'documentation-report'
                $null = New-Item -ItemType Directory -Path $inputPath -Force
                @{
                    Commit = $head; RunId = '123'; RunAttempt = '2'
                    Platform = 'Linux'; Architecture = 'ARM64'; Suite = 'xunit'; Passed = $true
                    Files = @{ 'src/Sample.cs' = @{ '1' = $true; '3' = $false } }
                } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $inputPath 'coverage.json')
                Write-CodeCoverageReport -InputDirectory $inputPath -OutputDirectory $outputPath -ExpectedRun 'Linux/ARM64/xunit' -Change $change
                $report = Get-Content -LiteralPath (Join-Path $outputPath 'summary.md') -Raw
                $report | Should -Match 'Current aggregate: \*\*50.00%\*\* \(1/2 executable lines\)'
                $report | Should -Match 'No C# files changed; PR patch coverage is \*\*not applicable'
                Get-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Raw | Should -Be $report
                $html = Get-Content -LiteralPath (Join-Path $outputPath 'index.html') -Raw
                $html | Should -Match 'Current: 50.00%'
                $html | Should -Match 'No C# files changed; PR patch coverage is not applicable'
                $data = Get-Content -LiteralPath (Join-Path $outputPath 'coverage.json') -Raw | ConvertFrom-Json -AsHashtable
                $data['src/Sample.cs']['1'] | Should -BeTrue
                $data['src/Sample.cs']['3'] | Should -BeFalse
            }

            It 'includes PowerShell-only and deleted-script changes in the reported range' {
                'Write-Output 1' | Set-Content -LiteralPath script.ps1
                Start-NativeExecution { git add -- .; git commit --quiet -m script } | Out-Null
                $head = Start-NativeExecution { git rev-parse HEAD }
                $env:GITHUB_SHA = $head
                @{ pull_request = @{ base = @{ sha = $baseCommit }; head = @{ sha = $head } } } |
                    ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $eventPath
                (Get-CodeCoverageChange -EventPath $eventPath).ChangedFile | Should -Be @('script.ps1')

                $deletionBase = $head
                Remove-Item -LiteralPath script.ps1
                Start-NativeExecution { git add -- .; git commit --quiet -m deletion } | Out-Null
                $head = Start-NativeExecution { git rev-parse HEAD }
                $env:GITHUB_SHA = $head
                @{ pull_request = @{ base = @{ sha = $deletionBase }; head = @{ sha = $head } } } |
                    ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $eventPath
                (Get-CodeCoverageChange -EventPath $eventPath).ChangedFile | Should -Be @('script.ps1')
            }

            It 'excludes pulled base changes and annotates the tested synthetic merge' {
                Start-NativeExecution { git checkout --quiet main } | Out-Null
                'upstream' | Set-Content -LiteralPath upstream.ps1
                Start-NativeExecution { git add -- .; git commit --quiet -m upstream } | Out-Null
                $newBase = Start-NativeExecution { git rev-parse HEAD }
                Start-NativeExecution { git checkout --quiet feature; git merge --quiet --no-edit main } | Out-Null
                "covered <tag>`nunchanged`nuncovered" | Set-Content -LiteralPath 'src/Sample.cs'
                Start-NativeExecution { git add -- .; git commit --quiet -m feature } | Out-Null
                $head = Start-NativeExecution { git rev-parse HEAD }
                Start-NativeExecution { git checkout --quiet main; git merge --quiet --no-ff --no-edit feature } | Out-Null
                $env:GITHUB_SHA = Start-NativeExecution { git rev-parse HEAD }
                @{ pull_request = @{ base = @{ sha = $newBase }; head = @{ sha = $head } } } |
                    ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $eventPath
                $change = Get-CodeCoverageChange -EventPath $eventPath
                $change.DiffBase | Should -Be $newBase
                $change.ChangedFile | Should -Not -Contain 'upstream.ps1'

                $inputPath = Join-Path $TestDrive 'input'
                $outputPath = Join-Path $TestDrive 'report'
                $null = New-Item -ItemType Directory -Path $inputPath -Force
                @{
                    Commit = $env:GITHUB_SHA; RunId = '123'; RunAttempt = '2'
                    Platform = 'Linux'; Architecture = 'ARM64'; Suite = 'xunit'; Passed = $true
                    Files = @{ 'src/Sample.cs' = @{ '1' = $true; '3' = $false } }
                } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $inputPath 'coverage.json')
                Write-CodeCoverageReport -InputDirectory $inputPath -OutputDirectory $outputPath -ExpectedRun 'Linux/ARM64/xunit' -Change $change
                $html = Get-Content -LiteralPath (Join-Path $outputPath 'index.html') -Raw
                $html | Should -Match 'class="covered".*\+covered &lt;tag&gt;'
                $html | Should -Match 'class="uncovered".*\+uncovered'
                $report = Get-Content -LiteralPath (Join-Path $outputPath 'summary.md') -Raw
                $report | Should -Match '50.00%'
                $report | Should -Match 'delta: \*\*unavailable'
                $report | Should -Match 'patch coverage, not a baseline delta'
            }

            It 'uses the merge base after a rebase rather than a previous push' {
                Start-NativeExecution { git checkout --quiet main } | Out-Null
                'upstream' | Set-Content -LiteralPath README.md
                Start-NativeExecution { git add -- .; git commit --quiet -m upstream } | Out-Null
                $newBase = Start-NativeExecution { git rev-parse HEAD }
                Start-NativeExecution { git checkout --quiet feature } | Out-Null
                'script' | Set-Content -LiteralPath changed.psm1
                Start-NativeExecution { git add -- .; git commit --quiet -m feature; git rebase --quiet main } | Out-Null
                $env:GITHUB_SHA = Start-NativeExecution { git rev-parse HEAD }
                @{ pull_request = @{ base = @{ sha = $newBase }; head = @{ sha = $env:GITHUB_SHA } } } |
                    ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $eventPath
                $change = Get-CodeCoverageChange -EventPath $eventPath
                $change.DiffBase | Should -Be $newBase
                $change.ChangedFile | Should -Contain 'changed.psm1'
                $change.ChangedFile | Should -Not -Contain 'README.md'
            }

            It 'maps the diff to merged line numbers when upstream was not pulled into the PR' {
                Start-NativeExecution { git checkout --quiet main } | Out-Null
                $original = @(1..20 | ForEach-Object { "line $_" })
                $original | Set-Content -LiteralPath 'src/Sample.cs'
                Start-NativeExecution { git add -- .; git commit --quiet -m seed; git checkout --quiet feature; git rebase --quiet main } | Out-Null
                $modified = @($original)
                $modified[19] = 'feature change'
                $modified | Set-Content -LiteralPath 'src/Sample.cs'
                Start-NativeExecution { git add -- .; git commit --quiet -m feature } | Out-Null
                $head = Start-NativeExecution { git rev-parse HEAD }
                Start-NativeExecution { git checkout --quiet main } | Out-Null
                @('upstream first', 'upstream second') + $original | Set-Content -LiteralPath 'src/Sample.cs'
                Start-NativeExecution { git add -- .; git commit --quiet -m upstream } | Out-Null
                $newBase = Start-NativeExecution { git rev-parse HEAD }
                Start-NativeExecution { git merge --quiet --no-ff --no-edit feature } | Out-Null
                $env:GITHUB_SHA = Start-NativeExecution { git rev-parse HEAD }
                @{ pull_request = @{ base = @{ sha = $newBase }; head = @{ sha = $head } } } |
                    ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $eventPath
                $change = Get-CodeCoverageChange -EventPath $eventPath
                $inputPath = Join-Path $TestDrive 'merged-input'
                $outputPath = Join-Path $TestDrive 'merged-report'
                $null = New-Item -ItemType Directory -Path $inputPath -Force
                @{
                    Commit = $env:GITHUB_SHA; RunId = '123'; RunAttempt = '2'
                    Platform = 'Linux'; Architecture = 'X64'; Suite = 'xunit'; Passed = $true
                    Files = @{ 'src/Sample.cs' = @{ '20' = $false; '22' = $true } }
                } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $inputPath 'coverage.json')
                Write-CodeCoverageReport -InputDirectory $inputPath -OutputDirectory $outputPath -ExpectedRun 'Linux/X64/xunit' -Change $change
                $html = Get-Content -LiteralPath (Join-Path $outputPath 'index.html') -Raw
                $html | Should -Match 'class="covered">22\s+covered\s+\+feature change'
                $html | Should -Not -Match '\+upstream'
            }

            It 'writes an incomplete report when all suites are missing' {
                $change = @{ TestCommit = $baseCommit; DiffBase = $baseCommit; ChangedFile = @() }
                $outputPath = Join-Path $TestDrive 'missing-report'
                { Write-CodeCoverageReport -InputDirectory (Join-Path $TestDrive 'empty') -OutputDirectory $outputPath -ExpectedRun 'Linux/X64/xunit' -Change $change } |
                    Should -Throw '*Incomplete coverage*'
                Get-Content -LiteralPath (Join-Path $outputPath 'summary.md') -Raw | Should -Match 'Missing: Linux/X64/xunit'
            }

            It 'combines retained successful suites with a retried suite from the same run and commit' {
                $inputPath = Join-Path $TestDrive 'retry-input'
                $outputPath = Join-Path $TestDrive 'retry-report'
                foreach ($attempt in 1, 2) {
                    $suitePath = Join-Path $inputPath "attempt-$attempt"
                    $null = New-Item -ItemType Directory -Path $suitePath -Force
                    @{
                        Commit = $baseCommit; RunId = '123'; RunAttempt = "$attempt"
                        Platform = 'Linux'; Architecture = 'X64'; Suite = "suite-$attempt"; Passed = $true
                        Files = @{ 'src/Sample.cs' = @{ '1' = $attempt -eq 1; '3' = $attempt -eq 2 } }
                    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $suitePath 'coverage.json')
                }

                $change = @{ TestCommit = $baseCommit; DiffBase = $baseCommit; ChangedFile = @() }
                $expected = @('Linux/X64/suite-1', 'Linux/X64/suite-2')
                Write-CodeCoverageReport -InputDirectory $inputPath -OutputDirectory $outputPath -ExpectedRun $expected -Change $change
                $report = Get-Content -LiteralPath (Join-Path $outputPath 'summary.md') -Raw
                $report | Should -Match 'Current aggregate: \*\*100.00%\*\* \(2/2 executable lines\)'
                $report | Should -Match '\| suite-1 \| 1 \|'
                $report | Should -Match '\| suite-2 \| 2 \|'
                $data = Get-Content -LiteralPath (Join-Path $outputPath 'coverage.json') -Raw | ConvertFrom-Json -AsHashtable
                $data['src/Sample.cs']['1'] | Should -BeTrue
                $data['src/Sample.cs']['3'] | Should -BeTrue

                $suitePath = Join-Path (Join-Path $inputPath 'attempt-1') 'coverage.json'
                $retained = Get-Content -LiteralPath $suitePath -Raw | ConvertFrom-Json -AsHashtable
                $retained.Passed = $false
                $retained | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $suitePath
                { Write-CodeCoverageReport -InputDirectory $inputPath -OutputDirectory $outputPath -ExpectedRun $expected -Change $change } |
                    Should -Throw '*0 missing and 1 failed suites*'
            }

            It 'rejects an invalid or future artifact attempt <Attempt>' -TestCases @(
                @{ Attempt = '3' }
                @{ Attempt = '0' }
                @{ Attempt = '-1' }
                @{ Attempt = 'invalid' }
                @{ Attempt = $null }
            ) {
                param($Attempt)
                $inputPath = Join-Path $TestDrive 'invalid-attempt'
                $null = New-Item -ItemType Directory -Path $inputPath -Force
                @{ Commit = $baseCommit; RunId = '123'; RunAttempt = $Attempt } |
                    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $inputPath 'coverage.json')
                $change = @{ TestCommit = $baseCommit; DiffBase = $baseCommit; ChangedFile = @() }
                { Write-CodeCoverageReport -InputDirectory $inputPath -OutputDirectory (Join-Path $TestDrive 'invalid-attempt-report') -ExpectedRun 'Linux/X64/xunit' -Change $change } |
                    Should -Throw '*invalid or future run attempt*'
            }

            It 'rejects coverage from a different <Identity> even on an earlier attempt' -TestCases @(
                @{ Identity = 'commit' }
                @{ Identity = 'run' }
            ) {
                param($Identity)
                $inputPath = Join-Path $TestDrive 'stale'
                $null = New-Item -ItemType Directory -Path $inputPath -Force
                $stale = @{ Commit = $baseCommit; RunId = '123'; RunAttempt = '1' }
                if ($Identity -eq 'commit') {
                    $stale.Commit = 'a' * 40
                } else {
                    $stale.RunId = '122'
                }

                $stale | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $inputPath 'coverage.json')
                $change = @{ TestCommit = $baseCommit; DiffBase = $baseCommit; ChangedFile = @() }
                { Write-CodeCoverageReport -InputDirectory $inputPath -OutputDirectory (Join-Path $TestDrive 'stale-report') -ExpectedRun 'Linux/X64/xunit' -Change $change } |
                    Should -Throw '*different commit or run*'
            }
        }
    }
}

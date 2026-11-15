# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

. (Join-Path $PSScriptRoot 'buildCommon/startNativeExecution.ps1')

function Get-CodeCoverageChange {
    <#
    .SYNOPSIS
        Determines the PR range and whether .NET coverage is needed.
    .DESCRIPTION
        Uses event commit IDs, not the previous push or a fixed number of commits.
        A synthetic PR merge is compared to its base so diff lines match tested code.
    .PARAMETER EventPath
        GitHub event JSON file.
    .PARAMETER OutputPath
        Optional GitHub Actions output file.
    #>
    [CmdletBinding()]
    param(
        [string] $EventPath = $env:GITHUB_EVENT_PATH,
        [string] $OutputPath = $env:GITHUB_OUTPUT
    )

    $eventData = Get-Content -LiteralPath $EventPath -Raw -ErrorAction Stop | ConvertFrom-Json
    if (-not $eventData.pull_request) {
        throw 'Coverage change detection requires a pull_request event.'
    }

    $baseCommit = [string] $eventData.pull_request.base.sha
    $headCommit = [string] $eventData.pull_request.head.sha
    $testCommit = $env:GITHUB_SHA
    foreach ($commit in @($baseCommit, $headCommit, $testCommit)) {
        if ($commit -notmatch '^[0-9a-f]{40}$') {
            throw "Invalid coverage commit ID: $commit"
        }
    }

    $mergeBase = Start-NativeExecution { git merge-base $baseCommit $headCommit }
    $diffBase = $mergeBase
    if ($testCommit -ne $headCommit) {
        $parents = (Start-NativeExecution { git rev-list --parents -n 1 $testCommit }) -split ' '
        if ($parents.Count -ne 3 -or $parents[1] -ne $baseCommit -or $parents[2] -ne $headCommit) {
            throw 'The tested commit is not the merge of the event base and head commits.'
        }

        $diffBase = $baseCommit
    }

    $changedFile = @(Start-NativeExecution {
        git -c core.quotepath=false diff --no-ext-diff --no-textconv --name-only --no-renames $diffBase $testCommit --
    })
    $result = [pscustomobject] @{
        Enabled = @($changedFile | Where-Object { $_ -match '\.(cs|ps1|psm1|psd1)$' }).Count -gt 0
        BaseCommit = $baseCommit
        HeadCommit = $headCommit
        TestCommit = $testCommit
        DiffBase = $diffBase
        ChangedFile = $changedFile
    }
    if ($OutputPath) {
        "enabled=$($result.Enabled.ToString().ToLowerInvariant())" | Add-Content -LiteralPath $OutputPath
    }

    return $result
}

function Install-CodeCoverageTool {
    <#
    .SYNOPSIS
        Installs the pinned collector in a job-local directory.
    .PARAMETER Path
        Directory in which to install dotnet-coverage.
    #>
    [CmdletBinding()]
    param([string] $Path = (Join-Path $env:RUNNER_TEMP 'codecoverage-tool'))

    $executable = Join-Path $Path $(if ($IsWindows) { 'dotnet-coverage.exe' } else { 'dotnet-coverage' })
    if (-not (Test-Path -LiteralPath $executable)) {
        Start-NativeExecution {
            dotnet tool install dotnet-coverage --version 18.12.0 --allow-roll-forward --tool-path $Path --source https://api.nuget.org/v3/index.json
        } | Out-Host
    }

    return $executable
}

function ConvertFrom-CodeCoverage {
    <#
    .SYNOPSIS
        Normalizes Cobertura to tracked C# paths and unique executable lines.
    .PARAMETER Path
        Cobertura XML file.
    .PARAMETER SourceFile
        Repository-relative tracked product C# paths, excluding test files.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string[]] $SourceFile
    )

    $settings = [System.Xml.XmlReaderSettings]::new()
    $settings.DtdProcessing = [System.Xml.DtdProcessing]::Ignore
    $settings.XmlResolver = $null
    $reader = [System.Xml.XmlReader]::Create($Path, $settings)
    $document = [System.Xml.XmlDocument]::new()
    $document.XmlResolver = $null
    try {
        $document.Load($reader)
    } finally {
        $reader.Dispose()
    }

    $tracked = [System.Collections.Generic.HashSet[string]]::new($SourceFile, [System.StringComparer]::Ordinal)
    $files = [System.Collections.Generic.Dictionary[string, object]]::new([System.StringComparer]::Ordinal)
    foreach ($class in $document.SelectNodes('/coverage/packages/package/classes/class')) {
        $file = ConvertTo-CodeCoverageSourcePath -Path $class.GetAttribute('filename')

        if (-not $tracked.Contains($file)) {
            continue
        }

        if (-not $files.ContainsKey($file)) {
            $files[$file] = @{}
        }

        foreach ($line in $class.SelectNodes('lines/line')) {
            $number = [int] $line.GetAttribute('number')
            $hits = [long] $line.GetAttribute('hits')
            if ($number -lt 1 -or $hits -lt 0) {
                throw "Invalid coverage line in $Path"
            }

            # A line can occur in multiple methods/classes; coverage is a union.
            $files[$file]["$number"] = $files[$file]["$number"] -eq $true -or $hits -gt 0
        }
    }

    if ($files.Count -eq 0) {
        throw "No tracked .NET source coverage found in $Path"
    }

    return ,$files
}

function ConvertTo-CodeCoverageSourcePath {
    <#
    .SYNOPSIS
        Normalizes checkout and deterministic PDB paths to repository paths.
    .PARAMETER Path
        Source path recorded in coverage or portable symbols.
    #>
    param([Parameter(Mandatory)] [string] $Path)

    $file = $Path.Replace('\', '/')
    $sourceIndex = $file.IndexOf('/src/', [System.StringComparison]::Ordinal)
    if ($sourceIndex -ge 0) {
        $file = $file.Substring($sourceIndex + 1)
    }
    return $file
}

function Get-CodeCoverageSequencePoint {
    <#
    .SYNOPSIS
        Includes unexecuted assemblies in the executable-line denominator.
    .PARAMETER Path
        Portable PDB for a product assembly, before instrumentation.
    .PARAMETER SourceFile
        Tracked repository C# files.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string[]] $SourceFile
    )

    $tracked = [System.Collections.Generic.HashSet[string]]::new($SourceFile, [System.StringComparer]::Ordinal)
    $files = [System.Collections.Generic.Dictionary[string, object]]::new([System.StringComparer]::Ordinal)
    $stream = [System.IO.File]::OpenRead($Path)
    $provider = $null
    try {
        $provider = [System.Reflection.Metadata.MetadataReaderProvider]::FromPortablePdbStream($stream)
        $reader = $provider.GetMetadataReader()
        foreach ($handle in $reader.MethodDebugInformation) {
            $method = $reader.GetMethodDebugInformation($handle)
            foreach ($point in $method.GetSequencePoints()) {
                if ($point.IsHidden) {
                    continue
                }
                $documentHandle = if ($point.Document.IsNil) { $method.Document } else { $point.Document }
                $document = $reader.GetDocument($documentHandle)
                $file = ConvertTo-CodeCoverageSourcePath -Path $reader.GetString($document.Name)
                if (-not $tracked.Contains($file)) {
                    continue
                }
                if (-not $files.ContainsKey($file)) {
                    $files[$file] = @{}
                }
                for ($number = $point.StartLine; $number -le $point.EndLine; $number++) {
                    $files[$file]["$number"] = $false
                }
            }
        }
    } finally {
        if ($provider) {
            $provider.Dispose()
        }
        $stream.Dispose()
    }
    return ,$files
}

function Start-CodeCoverageCollector {
    <#
    .SYNOPSIS
        Starts a session server without waiting on its inherited output pipe.
    .PARAMETER Path
        Collector executable.
    .PARAMETER SessionId
        Instrumentation session to collect.
    .PARAMETER SettingsPath
        Collector configuration.
    .PARAMETER OutputPath
        Cobertura report to finalize at shutdown.
    .PARAMETER Unelevate
        Starts the server at the trust level used by Windows x64 unelevated tests.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string] $SessionId,
        [Parameter(Mandatory)] [string] $SettingsPath,
        [Parameter(Mandatory)] [string] $OutputPath,
        [switch] $Unelevate
    )

    # Native PowerShell invocation waits on the background server's inherited output pipe.
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new($Path)
    $startInfo.UseShellExecute = $false
    $readyPath = $null
    if ($Unelevate) {
        $readyPath = Join-Path (Split-Path $OutputPath) "$SessionId.ready"
        $modulePath = Join-Path $PSScriptRoot 'codecoverage.psm1'
        $command = @"
try {
    Import-Module '$($modulePath.Replace("'", "''"))' -ErrorAction Stop
    & (Get-Module codecoverage) {
        Start-CodeCoverageCollector -Path '$($Path.Replace("'", "''"))' -SessionId '$SessionId' -SettingsPath '$($SettingsPath.Replace("'", "''"))' -OutputPath '$($OutputPath.Replace("'", "''"))'
    }
    [System.IO.File]::WriteAllText('$($readyPath.Replace("'", "''")).tmp', 'ready')
    [System.IO.File]::Move('$($readyPath.Replace("'", "''")).tmp', '$($readyPath.Replace("'", "''"))')
} catch {
    [System.IO.File]::WriteAllText('$($readyPath.Replace("'", "''")).tmp', `$_.ToString())
    [System.IO.File]::Move('$($readyPath.Replace("'", "''")).tmp', '$($readyPath.Replace("'", "''"))')
    throw
}
"@
        $encodedCommand = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($command))
        $startInfo.FileName = 'runas.exe'
        $startInfo.ArgumentList.Add('/trustlevel:0x20000')
        $startInfo.ArgumentList.Add("`"$(Join-Path $PSHOME 'pwsh.exe')`" -NoProfile -EncodedCommand $encodedCommand")
    } else {
        foreach ($argument in @('collect', '--session-id', $SessionId, '--server-mode', '--background',
                '--timeout', '30000', '--output', $OutputPath, '--output-format', 'cobertura',
                '--settings', $SettingsPath, '--nologo')) {
            $startInfo.ArgumentList.Add($argument)
        }
    }
    $process = [System.Diagnostics.Process]::Start($startInfo)
    try {
        if (-not $process.WaitForExit(60000)) {
            Stop-Process -Id $process.Id -ErrorAction Stop
            throw 'Coverage collector startup timed out.'
        }
        if ($process.ExitCode -ne 0) {
            throw "Coverage collector startup failed with exit code $($process.ExitCode)."
        }
        if ($readyPath) {
            $deadline = [DateTime]::UtcNow.AddSeconds(90)
            while (-not (Test-Path -LiteralPath $readyPath)) {
                if ([DateTime]::UtcNow -ge $deadline) {
                    throw 'Unelevated coverage collector startup timed out.'
                }
                Start-Sleep -Milliseconds 100
            }
            $result = Get-Content -LiteralPath $readyPath -Raw
            if ($result -ne 'ready') {
                throw "Unelevated coverage collector startup failed: $result"
            }
        }
    } finally {
        $process.Dispose()
        if ($readyPath -and (Test-Path -LiteralPath $readyPath)) {
            Remove-Item -LiteralPath $readyPath -Force
        }
        if ($readyPath -and (Test-Path -LiteralPath "$readyPath.tmp")) {
            Remove-Item -LiteralPath "$readyPath.tmp" -Force
        }
    }
}

function Invoke-CodeCoverageTest {
    <#
    .SYNOPSIS
        Runs a test operation once with static .NET instrumentation.
    .DESCRIPTION
        Static instrumentation also collects from subprocesses and works on Arm64
        on Linux and macOS. The original binaries are restored even on test failure.
    .PARAMETER Test
        The existing CI test operation.
    .PARAMETER BinaryDirectory
        Directory containing the assemblies actually used by the tests.
    .PARAMETER Suite
        Unique suite name (including tag/elevation when applicable).
    .PARAMETER OutputDirectory
        Job-local coverage artifact directory.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [scriptblock] $Test,
        [Parameter(Mandatory)] [string] $BinaryDirectory,
        [Parameter(Mandatory)] [string] $Suite,
        [string] $OutputDirectory = (Join-Path $env:GITHUB_WORKSPACE 'coverage')
    )

    $env:DOTNET_COVERAGE_TELEMETRY_OPTOUT = '1'
    $collector = Install-CodeCoverageTool
    $null = New-Item -ItemType Directory -Path $OutputDirectory -Force
    $rawPath = Join-Path $OutputDirectory 'coverage.cobertura.xml'
    $configurationPath = Join-Path $OutputDirectory 'settings.xml'
    @'
<Configuration>
  <CodeCoverage>
    <EnableStaticManagedInstrumentation>True</EnableStaticManagedInstrumentation>
    <EnableDynamicManagedInstrumentation>False</EnableDynamicManagedInstrumentation>
    <SkipAutoProperties>False</SkipAutoProperties>
    <Attributes mergeDefaults="False"><Exclude/></Attributes>
    <AllowLowIntegrityProcesses>True</AllowLowIntegrityProcesses>
    <Sources><Include><Source>.*[/\\]src[/\\].*\.cs$</Source></Include></Sources>
  </CodeCoverage>
</Configuration>
'@ | Set-Content -LiteralPath $configurationPath

    $binary = @(Get-ChildItem -LiteralPath $BinaryDirectory -Filter '*.dll' -File | Where-Object {
        $_.BaseName -match '^(pwsh|System\.Management\.Automation|Microsoft\.PowerShell\..*|Microsoft\.WSMan\..*|Microsoft\.Management\.(Infrastructure\.CimCmdlets|UI\.Internal))$' -and
        (Test-Path -LiteralPath ([System.IO.Path]::ChangeExtension($_.FullName, '.pdb')))
    })
    if ($binary.Count -eq 0) {
        throw "No PowerShell assemblies with symbols found in $BinaryDirectory"
    }

    $sourceFile = @(Start-NativeExecution { git -c core.quotepath=false ls-files -- 'src/*.cs' })
    $sequencePoints = @(foreach ($assembly in $binary) {
        @{ Files = Get-CodeCoverageSequencePoint -Path ([System.IO.Path]::ChangeExtension($assembly.FullName, '.pdb')) -SourceFile $sourceFile }
    })
    $session = [guid]::NewGuid().ToString()
    $instrumented = [System.Collections.Generic.List[string]]::new()
    $failures = [System.Collections.Generic.List[System.Management.Automation.ErrorRecord]]::new()
    $started = $false
    $passed = $false
    $savedEnvironment = @{ PS_CODE_COVERAGE = $env:PS_CODE_COVERAGE }
    if (-not $IsWindows) {
        $savedEnvironment['XDG_RUNTIME_DIR'] = $env:XDG_RUNTIME_DIR
        $savedEnvironment['TMPDIR'] = $env:TMPDIR
    }
    try {
        $env:PS_CODE_COVERAGE = '1'
        if (-not $IsWindows) {
            # sudo -E must not give root the runner user's private mutex directories.
            $env:XDG_RUNTIME_DIR = $null
            $env:TMPDIR = $null
        }
        foreach ($assembly in $binary) {
            $assemblyPath = $assembly.FullName
            $instrumented.Add($assemblyPath)
            Start-NativeExecution {
                & $collector instrument $assemblyPath --session-id $session --settings $configurationPath --nologo
            } | Out-Host
        }

        $unelevateCollector = $IsWindows -and $env:RUNNER_ARCH -eq 'X64' -and $Suite.StartsWith('UnelevatedPesterTests-')
        Start-CodeCoverageCollector -Path $collector -SessionId $session -SettingsPath $configurationPath -OutputPath $rawPath -Unelevate:$unelevateCollector
        $started = $true
        & $Test
        $passed = $true
    } catch {
        $failures.Add($_)
    } finally {
        try {
            if ($started) {
                Start-NativeExecution { & $collector shutdown $session --timeout 30000 --nologo } | Out-Host
                $measured = @{ Files = ConvertFrom-CodeCoverage -Path $rawPath -SourceFile $sourceFile }
                $data = @{
                    Commit = $env:GITHUB_SHA
                    RunId = $env:GITHUB_RUN_ID
                    RunAttempt = $env:GITHUB_RUN_ATTEMPT
                    Platform = $env:RUNNER_OS
                    Architecture = $env:RUNNER_ARCH
                    Suite = $Suite
                    Passed = $passed
                    Files = Merge-CodeCoverage -Run @($sequencePoints + $measured)
                }
                $data | ConvertTo-Json -Depth 8 -Compress | Set-Content -LiteralPath (Join-Path $OutputDirectory 'coverage.json')
            }
        } catch {
            $failures.Add($_)
        } finally {
            foreach ($assemblyPath in $instrumented) {
                try {
                    Start-NativeExecution { & $collector uninstrument $assemblyPath --nologo } | Out-Host
                } catch {
                    $failures.Add($_)
                }
            }
            foreach ($name in $savedEnvironment.Keys) {
                [System.Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name])
            }
        }
    }

    if ($failures.Count) {
        foreach ($failure in $failures | Select-Object -Skip 1) {
            Write-Error -ErrorRecord $failure -ErrorAction Continue
        }
        throw $failures[0]
    }
}

function Invoke-CodeCoverageXUnit {
    <#
    .SYNOPSIS
        Builds xUnit dependencies before instrumentation, then runs tests once.
    #>
    [CmdletBinding()]
    param()

    $options = Get-PSOptions
    $project = Join-Path $PSScriptRoot '../test/xUnit/xUnit.tests.csproj'
    Start-NativeExecution { dotnet build $project --configuration $options.Configuration --nologo } | Out-Host
    $directory = Join-Path $PSScriptRoot "../test/xUnit/bin/$($options.Configuration)/$($options.Framework)"
    Invoke-CodeCoverageTest -BinaryDirectory $directory -Suite xunit -Test { Invoke-CIxUnit -SkipBuild }
}

function Merge-CodeCoverage {
    <#
    .SYNOPSIS
        Unions line coverage without averaging platform percentages.
    .PARAMETER Run
        Normalized coverage runs from the same tested commit and CI attempt.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)] [AllowEmptyCollection()] [object[]] $Run)

    $files = [System.Collections.Generic.Dictionary[string, object]]::new([System.StringComparer]::Ordinal)
    foreach ($coverageRun in $Run) {
        foreach ($file in $coverageRun.Files.Keys) {
            if (-not $files.ContainsKey($file)) {
                $files[$file] = @{}
            }

            foreach ($number in $coverageRun.Files[$file].Keys) {
                $files[$file][$number] = $files[$file][$number] -eq $true -or $coverageRun.Files[$file][$number] -eq $true
            }
        }
    }

    return ,$files
}

function Get-CodeCoverageSummary {
    <#
    .SYNOPSIS
        Counts unique executable/covered lines.
    .PARAMETER File
        Normalized file-to-line coverage map.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)] [System.Collections.IDictionary] $File)

    $total = 0
    $covered = 0
    foreach ($lines in $File.Values) {
        foreach ($visited in $lines.Values) {
            $total++
            if ($visited) {
                $covered++
            }
        }
    }

    return [pscustomobject] @{
        Total = $total
        Covered = $covered
        Percent = $(if ($total) { '{0:F2}%' -f (100.0 * $covered / $total) } else { 'N/A' })
    }
}

function Write-CodeCoverageReport {
    <#
    .SYNOPSIS
        Publishes the aggregate, per-run breakdown, and coverage-colored PR diff.
    .DESCRIPTION
        Rejects stale data and labels missing/failed suites as incomplete.
        Historical base coverage and causal script/test deltas are not inferred.
    .PARAMETER InputDirectory
        Downloaded coverage artifacts from this CI run.
    .PARAMETER OutputDirectory
        Report artifact directory.
    .PARAMETER ExpectedRun
        Expected platform/architecture/suite identifiers.
    .PARAMETER Change
        PR range returned by Get-CodeCoverageChange.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $InputDirectory,
        [Parameter(Mandatory)] [string] $OutputDirectory,
        [Parameter(Mandatory)] [string[]] $ExpectedRun,
        [Parameter(Mandatory)] [object] $Change
    )

    $null = New-Item -ItemType Directory -Path $OutputDirectory -Force
    $null = New-Item -ItemType Directory -Path $InputDirectory -Force
    $runs = @(Get-ChildItem -LiteralPath $InputDirectory -Filter coverage.json -Recurse -File | ForEach-Object {
        Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json -AsHashtable
    })
    foreach ($coverageRun in $runs) {
        if ($coverageRun.Commit -ne $Change.TestCommit -or $coverageRun.RunId -ne $env:GITHUB_RUN_ID -or
            $coverageRun.RunAttempt -ne $env:GITHUB_RUN_ATTEMPT) {
            throw 'Coverage artifact belongs to a different commit, run, or attempt.'
        }
    }

    $identifier = @($runs | ForEach-Object { "$($_.Platform)/$($_.Architecture)/$($_.Suite)" })
    if (@($identifier | Select-Object -Unique).Count -ne $identifier.Count) {
        throw 'Duplicate coverage suite artifacts.'
    }

    $missing = @($ExpectedRun | Where-Object { $_ -notin $identifier })
    $failed = @($runs | Where-Object { -not $_.Passed })
    $files = Merge-CodeCoverage -Run $runs
    $summary = Get-CodeCoverageSummary -File $files
    $markdown = [System.Text.StringBuilder]::new()
    $null = $markdown.AppendLine('# .NET code coverage')
    $null = $markdown.AppendLine()
    $null = $markdown.AppendLine("Current aggregate: **$($summary.Percent)** ($($summary.Covered)/$($summary.Total) executable lines). Lines covered by any runner count once.")
    $null = $markdown.AppendLine()
    $null = $markdown.AppendLine('Base coverage and PR coverage delta: **unavailable**. Tests run once; no historical baseline is used.')
    $null = $markdown.AppendLine()
    $null = $markdown.AppendLine("Tested commit: ``$($Change.TestCommit)``; diff base: ``$($Change.DiffBase)``.")
    $null = $markdown.AppendLine()
    if ($missing.Count -or $failed.Count) {
        $null = $markdown.AppendLine('**INCOMPLETE: missing or failed suites. This is not a complete aggregate.**')
        foreach ($name in $missing) {
            $null = $markdown.AppendLine("- Missing: $name")
        }
    }

    $null = $markdown.AppendLine()
    $null = $markdown.AppendLine('| Platform | Architecture | Test run | Covered / executable | Coverage | Status |')
    $null = $markdown.AppendLine('| --- | --- | --- | --- | --- | --- |')
    foreach ($coverageRun in $runs | Sort-Object Platform, Architecture, Suite) {
        $runSummary = Get-CodeCoverageSummary -File $coverageRun.Files
        $status = if ($coverageRun.Passed) { 'Passed' } else { 'Failed / partial' }
        $null = $markdown.AppendLine("| $($coverageRun.Platform) | $($coverageRun.Architecture) | $($coverageRun.Suite) | $($runSummary.Covered) / $($runSummary.Total) | $($runSummary.Percent) | $status |")
    }

    $html = [System.Text.StringBuilder]::new()
    $null = $html.AppendLine('<!doctype html><html lang="en"><meta charset="utf-8"><title>.NET PR coverage</title>')
    $null = $html.AppendLine('<style>body{font-family:system-ui}pre{margin:0;white-space:pre-wrap}.covered{background:#dafbe1;color:#116329}.uncovered{background:#ffebe9;color:#82071e}.unknown{background:#fff8c5}.removed{color:#57606a}</style>')
    $null = $html.AppendLine('<h1>.NET PR coverage</h1><p>Green: covered executable line. Red: uncovered executable line. Yellow: C# coverage unavailable. Uninstrumented/non-executable lines are neutral. Removed lines have no current coverage.</p>')
    $null = $html.AppendLine("<p>Current: $($summary.Percent). Base coverage/delta unavailable; tests run once.</p>")
    if ($missing.Count -or $failed.Count) {
        $null = $html.AppendLine('<p><strong>INCOMPLETE: missing or failed test suites.</strong></p>')
    }

    $changedLines = @{}
    foreach ($file in $Change.ChangedFile) {
        $patch = @(Start-NativeExecution {
            git -c core.quotepath=false diff --no-ext-diff --no-textconv --no-color --no-renames --unified=3 $Change.DiffBase $Change.TestCommit -- $file
        })
        $null = $html.AppendLine("<h2>$([System.Net.WebUtility]::HtmlEncode($file))</h2>")
        $lineNumber = 0
        $inHunk = $false
        foreach ($text in $patch) {
            $hunk = [regex]::Match($text, '^@@ -\d+(?:,\d+)? \+(\d+)(?:,\d+)? @@')
            $style = ''
            $label = ''
            if ($hunk.Success) {
                $lineNumber = [int] $hunk.Groups[1].Value
                $inHunk = $true
            } elseif ($inHunk -and ($text.StartsWith('+') -or $text.StartsWith(' '))) {
                if ($file.EndsWith('.cs', [System.StringComparison]::Ordinal)) {
                    if ($files.ContainsKey($file)) {
                        if ($files[$file].ContainsKey("$lineNumber")) {
                            $visited = $files[$file]["$lineNumber"]
                            $style = if ($visited) { 'covered' } else { 'uncovered' }
                            $label = if ($visited) { 'covered' } else { 'uncovered' }
                            if ($text.StartsWith('+')) {
                                $changedLines["${file}:$lineNumber"] = $visited
                            }
                        }
                    } else {
                        $style = 'unknown'
                        $label = 'coverage unavailable'
                    }
                }

                $label = "$lineNumber $label"
                $lineNumber++
            } elseif ($inHunk -and $text.StartsWith('-')) {
                $style = 'removed'
            }

            $null = $html.AppendLine("<pre class=`"$style`">$([System.Net.WebUtility]::HtmlEncode($label.PadRight(28) + $text))</pre>")
        }
    }

    $null = $html.AppendLine('</html>')
    $patchSummary = Get-CodeCoverageSummary -File @{ Diff = $changedLines }
    $null = $markdown.AppendLine()
    $null = $markdown.AppendLine("Added/modified executable C# lines: **$($patchSummary.Percent)** ($($patchSummary.Covered)/$($patchSummary.Total)). This is patch coverage, not a baseline delta.")
    $null = $markdown.AppendLine()
    $null = $markdown.AppendLine('PowerShell-only changes still collect C# execution coverage from Pester and xUnit. Their causal effect cannot be separated from existing coverage in a single run.')
    $null = $markdown.AppendLine()
    $null = $markdown.AppendLine('Download the **codecoverage-report** artifact and open **index.html** for the green/red coverage-annotated code diff, including PowerShell changes (neutral because only .NET is instrumented). **coverage.json** contains the aggregate executable-line map.')
    $markdown.ToString() | Set-Content -LiteralPath (Join-Path $OutputDirectory 'summary.md')
    $html.ToString() | Set-Content -LiteralPath (Join-Path $OutputDirectory 'index.html')
    $files | ConvertTo-Json -Depth 8 -Compress | Set-Content -LiteralPath (Join-Path $OutputDirectory 'coverage.json')
    if ($env:GITHUB_STEP_SUMMARY) {
        $markdown.ToString() | Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY
    }

    if ($missing.Count -or $failed.Count) {
        throw "Incomplete coverage: $($missing.Count) missing and $($failed.Count) failed suites."
    }
}

function Publish-CodeCoverageReport {
    <#
    .SYNOPSIS
        Publishes the expected six-platform, five-suite PR coverage aggregate.
    #>
    [CmdletBinding()]
    param()

    $expected = foreach ($platform in 'Windows', 'Linux', 'macOS') {
        foreach ($architecture in 'X64', 'ARM64') {
            foreach ($suite in 'xunit', 'UnelevatedPesterTests-CI', 'ElevatedPesterTests-CI', 'UnelevatedPesterTests-Others', 'ElevatedPesterTests-Others') {
                "$platform/$architecture/$suite"
            }
        }
    }

    Write-CodeCoverageReport -InputDirectory (Join-Path $env:GITHUB_WORKSPACE 'coverage-input') `
        -OutputDirectory (Join-Path $env:GITHUB_WORKSPACE 'coverage-report') -ExpectedRun $expected -Change (Get-CodeCoverageChange)
}

Export-ModuleMember -Function Get-CodeCoverageChange, Invoke-CodeCoverageTest, Invoke-CodeCoverageXUnit, Publish-CodeCoverageReport

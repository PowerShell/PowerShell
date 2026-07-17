---
applyTo:
  - "**/*"
---

# Comparing Release Automation Test Results

Use these instructions to compare platform test results from the current PowerShell release automation run with the previous release.

## Select the Comparison Path

Prefer the `powershell-release` MCP when it is available and both builds are in the `powershell-rel` organization:

1. Call `Initialize_AzureDevOpsModule` with a short-lived, read-only Azure DevOps token that has **Build (Read)** and **Test Management (Read)** permissions. Never echo or persist the token.
1. Call `Compare_ReleaseAutomationBuild` with the older build ID as `Reference` and the newer build ID as `Difference`.
1. Preserve the exact tool output in the report and analyze every new failure it identifies.

The MCP comparison is the fastest first pass because it reads the Azure DevOps test runs directly. Use the manual artifact comparison below when:

- The MCP is unavailable or cannot access the build.
- The builds are in the `mscodehub` organization.
- Failure messages and stack traces are needed.
- A failing test is missing from the baseline and must be distinguished from a newly introduced test.

## Download the Test Artifacts

1. Open the published artifacts for the current test pipeline execution. Replace `BUILD-ID-HERE` in the URL for the organization that contains the build:

   - `https://dev.azure.com/powershell-rel/Release-Automation/_build/results?buildId=BUILD-ID-HERE&view=artifacts&pathAsName=false&type=publishedArtifacts`
   - `https://mscodehub.visualstudio.com/PowerShellCore/_build/results?buildId=BUILD-ID-HERE&view=artifacts&pathAsName=false&type=publishedArtifacts`

1. Download every platform artifact, such as `AzureLinux30`, `macOS_LTS`, and the other published platform names.

1. Move the downloaded ZIP files into a directory named for the release version. Keep each ZIP named for its platform. For example:

   ```text
   ./7.6.3/AzureLinux30.zip
   ./7.6.3/macOS_LTS.zip
   ```

1. Repeat the process for the comparison pipeline execution from the previous release:

   ```text
   ./7.6.2/AzureLinux30.zip
   ./7.6.2/macOS_LTS.zip
   ```

## Load the Comparison Functions

Use PowerShell 7 or later (`pwsh`). Paste the following functions into the terminal, or save the code block as a `.ps1` file and dot-source it.

```powershell
function Compare-ManualTestResult {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $CurrentReleasePath,

        [Parameter(Mandatory)]
        [string] $LastReleasePath,

        [switch] $IncludeKnownFailure
    )
    begin {
        # Manually update if `GetNewPath` throws.
        $pathPrefixes = (
            '/Users/runner/PSPackage/download/Test/',
            'C:\ProgramData\PSPackage\download\Test\',
            '/home/cloudtest/PSPackage/download/Test/',
            '/home/cloudtest_azpcontainer/PSPackage/download/Test/')

        function GetNewPath {
            param($Path)
            end {
                foreach ($pathPrefix in $pathPrefixes) {
                    if (-not $Path.StartsWith($pathPrefix, [StringComparison]::Ordinal)) {
                        continue
                    }

                    return $Path.Substring($pathPrefix.Length)
                }

                throw "Path prefix not found for path: $Path"
            }
        }

        function GetTestCase {
            [CmdletBinding()]
            param(
                $Prefix,

                $Path,

                [Parameter(ValueFromPipeline)]
                $Node
            )
            process {
                $currentPrefix = $null
                if ($Node.Name -ne 'Pester') {
                    if (-not $MyInvocation.BoundParameters['Path']) {
                        $Path = GetNewPath $Node.description
                    } elseif ($Prefix) {
                        $currentPrefix = $Prefix, $Node.description -join '.'
                    } else {
                        $currentPrefix = $Node.description
                    }
                }

                $results = $Node.results
                $suites = $results.'test-suite'
                if ($suites) {
                    $suites | GetTestCase $currentPrefix $Path
                }

                $cases = $results.'test-case'
                foreach ($case in $cases) {
                    [PSCustomObject]@{
                        Success = $case.success -eq 'True'
                        Executed = $case.executed -eq 'True'
                        Description = $case.description
                        Message = [string] $case.failure.message
                        StackTrace = [string] $case.failure.'stack-trace'
                        Id = $case.name
                        Path = $Path
                    }
                }
            }
        }

        function GetTestResult {
            [CmdletBinding()]
            param(
                [string] $Path
            )
            end {
                $basePath = (Resolve-Path $Path -ErrorAction Stop).ProviderPath
                $result = @{}
                foreach ($zip in Get-ChildItem -LiteralPath $basePath -Filter '*.zip' -File -ErrorAction Stop) {
                    $platform = $result[$zip.BaseName] = @{}
                    $extractedPath = Join-Path $basePath $zip.BaseName
                    if (-not (Test-Path -LiteralPath $extractedPath -PathType Container)) {
                        Expand-Archive -LiteralPath $zip.FullName -DestinationPath $extractedPath -ErrorAction Stop
                    }

                    # Azure DevOps ZIP downloads can contain the files directly or under a
                    # top-level directory named for the artifact.
                    $artifactPath = $extractedPath
                    $nestedArtifactPath = Join-Path $extractedPath $zip.BaseName
                    if (Test-Path -LiteralPath $nestedArtifactPath -PathType Container) {
                        $artifactPath = $nestedArtifactPath
                    }

                    foreach ($elevation in 'Elevated', 'Unelevated') {
                        $testPath = Join-Path $artifactPath "Tests_$elevation.xml"
                        if (-not (Test-Path -LiteralPath $testPath)) {
                            $platform[$elevation] = @{}
                            continue
                        }

                        $xml = [xml](Get-Content -LiteralPath $testPath -Raw -ErrorAction Stop)
                        $run = $platform[$elevation] = @{}

                        foreach ($case in $xml.'test-results'.'test-suite'.results.'test-suite' | GetTestCase) {
                            $run[($case.Path, $case.Id) -join '.'] = $case
                        }
                    }
                }

                $result
            }
        }

        function MakeCompareResult {
            param(
                [ValidateSet('NewTest', 'KnownFailure', 'NewFailure')]
                $Type,

                $Result,

                $Platform,

                $Elevation
            )
            end {
                [pscustomobject]@{
                    Type = $Type
                    Run = $Platform, $Elevation -join ' - '
                    Test = $Result.Id
                    Path = $Result.Path
                    Result = $Result
                }
            }
        }
    }
    end {
        $current = GetTestResult $CurrentReleasePath
        $last = GetTestResult $LastReleasePath
        foreach ($platformName in $current.Keys | Sort-Object) {
            $lastPlatform = $last[$platformName] ?? @{}

            foreach ($elevation in 'Elevated', 'Unelevated') {
                $run = $current[$platformName][$elevation] ?? @{}
                $lastRun = $lastPlatform[$elevation] ?? @{}
                foreach ($caseKey in $run.Keys | Sort-Object) {
                    $case = $run[$caseKey]
                    if ($case.Success) {
                        continue
                    }

                    $lastCase = $lastRun[$caseKey]
                    if (-not $lastCase) {
                        MakeCompareResult NewTest $case $platformName $elevation
                        continue
                    }

                    if ($lastCase.Success) {
                        MakeCompareResult NewFailure $case $platformName $elevation
                        continue
                    }

                    if (-not $IncludeKnownFailure) {
                        continue
                    }

                    MakeCompareResult KnownFailure $case $platformName $elevation
                }
            }
        }
    }
}

function Format-ManualTestComparison {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline)]
        [psobject] $InputObject
    )
    begin {
        $pipe = { Format-Table -GroupBy Run -Property Type, Test }.GetSteppablePipeline($MyInvocation.CommandOrigin)
        $pipe.Begin($PSCmdlet)
    }
    process {
        $pipe.Process($PSItem)
    }
    end {
        $pipe.End()
    }
}
```

If the functions were saved to `Compare-ManualTestResult.ps1`, load them with:

```powershell
. ./Compare-ManualTestResult.ps1
```

## Compare the Releases

Run the comparison with the current release directory first and the previous release directory second:

```powershell
$comparison = Compare-ManualTestResult ./7.6.3 ./7.6.2
$comparison | Format-ManualTestComparison
```

Replace the example versions with the versions being compared. Add `-IncludeKnownFailure` only when the report should include failures that also occurred in the previous release.

To inspect the failure details used in the analysis, run:

```powershell
$comparison | Select-Object Type, Run, Test, Path, @{
    Name = 'Message'
    Expression = { $_.Result.Message }
}, @{
    Name = 'StackTrace'
    Expression = { $_.Result.StackTrace }
} | Format-List
```

## Interpret the Results

- `NewFailure` means the same test passed in the previous release and failed in the current release.
- `NewTest` means a failing current test had no matching path and test ID in the previous artifact. Check whether the test was newly added or renamed, or whether the previous platform/elevation artifact was missing, before calling it a regression.
- `KnownFailure` means the test failed in both releases and is returned only with `-IncludeKnownFailure`.

## Report the Results

Always provide both:

1. The raw, unmodified output from the selected comparison path:
   - The exact `Compare_ReleaseAutomationBuild` output when using the MCP.
   - The exact output from the following command when using downloaded artifacts:

     ```powershell
     $comparison | Format-ManualTestComparison
     ```

1. An analysis that identifies:
   - Each `NewFailure` and the affected platform and elevation.
   - Each `NewTest`, distinguishing a newly introduced or renamed test from missing baseline coverage.
   - Common failure patterns across platforms.
   - Relevant failure messages and stack traces from the manual comparison objects.
   - Missing platform or elevation artifacts that could make results appear new.

Do not omit, rewrite, or replace the raw comparison output with only a summary.

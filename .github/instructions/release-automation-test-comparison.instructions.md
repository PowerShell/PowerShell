---
applyTo:
  - "**/*"
---

# Comparing Release Automation Test Results

Use these instructions to compare platform test results from the current PowerShell release automation run with a previous release.

Release automation runs in the MSCodeHub `PowerShellCore` project as pipeline definition `4223`:

`https://mscodehub.visualstudio.com/PowerShellCore/_build?definitionId=4223&_a=summary`

## Automated MCP Comparison

Prefer the `powershell-release` MCP:

1. Use the older build ID as `Reference` and the newer build ID as `Difference`.
1. Call `Compare_ReleaseAutomationBuild`.
1. Add `IncludeKnownFailure=true` only when failures present in both builds are needed.

The tool authenticates to MSCodeHub through the current Azure CLI Microsoft Entra session. Never ask the user for an Azure DevOps PAT. If authentication fails, ask the user to run `az login`, then retry.

The tool:

- Validates that both builds belong to pipeline definition `4223` and are complete.
- Downloads the individual platform artifacts rather than the multi-gigabyte aggregate artifacts.
- Compares `Tests_Elevated.xml` and `Tests_Unelevated.xml`.
- Preserves repeated and case-sensitive test executions.
- Distinguishes failures from inconclusive or unexecuted tests.
- Deletes temporary downloads and extracted files.

The result contains:

- `RawOutput`: the exact grouped comparison table.
- `Comparison`: structured `NewFailure`, `NewTest`, and optional `KnownFailure` records with messages and stack traces.
- `ComparedArtifact` and `ComparedRun`: artifacts and platform/elevation runs that had XML in both builds.
- `MissingFromReference` and `MissingFromDifference`: platform artifacts absent from one build.
- `MissingTestResult`: artifacts or elevations without comparable test XML.

## Manual Artifact Comparison

Use the manual path when the MCP cannot download a build or when working from already downloaded artifacts.

### Download the Platform Artifacts

Open:

`https://mscodehub.visualstudio.com/PowerShellCore/_build/results?buildId=BUILD-ID-HERE&view=artifacts&pathAsName=false&type=publishedArtifacts`

For both builds:

1. Download every individual platform artifact, such as `AzureLinux30`, `macOS_LTS`, and the Windows variants.
1. Do not download the large aggregate `tests` or `product` pipeline artifacts.
1. Keep each ZIP named for its platform in separate current and previous directories:

   ```text
   ./current/AzureLinux30.zip
   ./current/macOS_LTS.zip
   ./previous/AzureLinux30.zip
   ./previous/macOS_LTS.zip
   ```

### Run the Script-Based Comparison

The supported script implementation is exported by the `ReleaseMcp` module in the PowerShell Infrastructure repository. It incorporates the `Compare-ManualTestResult` artifact parsing workflow.

From the Infrastructure repository:

```powershell
Import-Module ./tools/mcp-servers/ReleaseMcp/ReleaseMcp.psd1

$comparison = Compare-ReleaseAutomationArtifact `
    -CurrentReleasePath ./current `
    -LastReleasePath ./previous
```

To include failures present in both builds:

```powershell
$comparison = Compare-ReleaseAutomationArtifact `
    -CurrentReleasePath ./current `
    -LastReleasePath ./previous `
    -IncludeKnownFailure
```

Display the exact comparison table:

```powershell
$comparison.RawOutput
```

Inspect failure details:

```powershell
$comparison.Comparison | Format-List
```

Inspect incomplete test coverage:

```powershell
$comparison.MissingTestResult | Format-Table
```

## Interpret the Results

- `NewFailure`: the test identity existed in the previous build, but the current build has more failing executions.
- `NewTest`: a failing test identity is absent from the previous build. Check whether it was added or renamed before calling it a regression.
- `KnownFailure`: a failing execution is represented in both builds and is returned only with `IncludeKnownFailure`.
- `MissingTestResult`: test XML is absent. Do not interpret that platform or elevation as passing.

Repeated tests are compared as a multiset because NUnit XML does not provide a unique ID for every parameterized execution.

## Report the Results

Always provide:

1. The raw, unmodified `RawOutput`. If it is empty, state that the comparison produced no rows.
1. Every `NewFailure`, including platform, elevation, message, and relevant stack trace.
1. Every `NewTest`, distinguishing a newly introduced or renamed test from missing baseline coverage.
1. Every `MissingTestResult` and missing platform artifact.
1. Common failure patterns across platforms when known failures were requested.

Do not replace the raw output with only a summary.

# Preview Changelog

## [7.6.0-preview.5] - 2025-09-30

### Engine Updates and Fixes

- Allow opt-out of the named-pipe listener using the environment variable `POWERSHELL_DIAGNOSTICS_OPTOUT` (#26086)
- Ensure that socket timeouts are set only during the token validation (#26066)
- Fix race condition in `RemoteHyperVSocket` (#26057)
- Fix `stderr` output of console host to respect `NO_COLOR` (#24391)
- Update PSRP protocol to deprecate session key exchange between newer client and server (#25774)
- Fix the `ssh` PATH check in `SSHConnectionInfo` when the default Runspace is not available (#25780) (Thanks @jborean93!)
- Adding hex format for native command exit codes (#21067) (Thanks @sba923!)
- Fix infinite loop crash in variable type inference (#25696) (Thanks @MartinGC94!)
- Add `PSForEach` and `PSWhere` as aliases for the PowerShell intrinsic methods `Where` and `Foreach` (#25511) (Thanks @powercode!)

### General Cmdlet Updates and Fixes

- Remove `IsScreenReaderActive()` check from `ConsoleHost` (#26118)
- Fix `ConvertFrom-Json` to ignore comments inside array literals (#14553) (#26050) (Thanks @MatejKafka!)
- Fix `-Debug` to not trigger the `ShouldProcess` prompt (#26081)
- Add the parameter `Register-ArgumentCompleter -NativeFallback` to support registering a cover-all completer for native commands (#25230)
- Change the default feedback provider timeout from 300ms to 1000ms (#25910)
- Update PATH environment variable for package manager executable on Windows (#25847)
- Fix `Write-Host` to respect `OutputRendering = PlainText` (#21188)
- Improve the `$using` expression support in `Invoke-Command` (#24025) (Thanks @jborean93!)
- Use parameter `HelpMessage` for tool tip in parameter completion (#25108) (Thanks @jborean93!)
- Revert "Never load a module targeting the PSReadLine module's `SessionState`" (#25792)
- Fix debug tracing error with magic extents (#25726) (Thanks @jborean93!)
- Add `MethodInvocation` trace for overload tracing (#21320) (Thanks @jborean93!)
- Improve verbose and debug logging level messaging in web cmdlets (#25510) (Thanks @JustinGrote!)
- Fix quoting in completion if the path includes a double quote character (#25631) (Thanks @MartinGC94!)
- Fix the common parameter `-ProgressAction` for advanced functions (#24591) (Thanks @cmkb3!)
- Use absolute path in `FileSystemProvider.CreateDirectory` (#24615) (Thanks @Tadas!)
- Make inherited protected internal instance members accessible in PowerShell class scope (#25245) (Thanks @mawosoft!)
- Treat `-Target` as literal in `New-Item` (#25186) (Thanks @GameMicrowave!)
- Remove duplicate modules from completion results (#25538) (Thanks @MartinGC94!)
- Add completion for variables assigned in `ArrayLiteralAst` and `ParenExpressionAst` (#25303) (Thanks @MartinGC94!)
- Add support for thousands separators in `[bigint]` casting (#25396) (Thanks @AbishekPonmudi!)
- Add internal methods to check Preferences (#25514) (Thanks @iSazonov!)
- Improve debug logging of Web cmdlet request and response (#25479) (Thanks @JustinGrote!)
- Revert "Allow empty prefix string in 'Import-Module -Prefix' to override default prefix in manifest (#20409)" (#25462) (Thanks @MartinGC94!)
- Fix the `NullReferenceException` when writing progress records to console from multiple threads (#25440) (Thanks @kborowinski!)
- Update `Get-Service` to ignore common errors when retrieving non-critical properties for a service (#24245) (Thanks @jborean93!)
- Add single/double quote support for `Join-String` Argument Completer (#25283) (Thanks @ArmaanMcleod!)
- Fix tab completion for env/function variables (#25346) (Thanks @jborean93!)
- Fix `Out-GridView` by replacing use of obsolete `BinaryFormatter` with custom implementation (#25497) (Thanks @mawosoft!)
- Remove the use of Windows PowerShell ETW provider id from code base and update the `PSDiagnostics` module to work for PowerShell 7 (#25590)

### Code Cleanup

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@xtqqczze, @mawosoft, @ArmaanMcleod</p>

</summary>

<ul>
<li>Enable CA2021: Do not call Enumerable.Cast<T> or Enumerable.OfType<T> with incompatible types (#25813) (Thanks @xtqqczze!)</li>
<li>Remove some unused <code>ConsoleControl</code> structs (#26063) (Thanks @xtqqczze!)</li>
<li>Remove unused <code>FileStreamBackReader.NativeMethods</code> type (#26062) (Thanks @xtqqczze!)</li>
<li>Ensure data-serialization files end with one newline (#26039) (Thanks @xtqqczze!)</li>
<li>Remove unnecessary <code>CS0618</code> suppressions from Variant APIs (#26006) (Thanks @xtqqczze!)</li>
<li>Ensure <code>.cs</code> files end with exactly one newline (#25968) (Thanks @xtqqczze!)</li>
<li>Remove obsolete <code>CA2105</code> rule suppression (#25938) (Thanks @xtqqczze!)</li>
<li>Remove obsolete <code>CA1703</code> rule suppression (#25955) (Thanks @xtqqczze!)</li>
<li>Remove obsolete <code>CA2240</code> rule suppression (#25957) (Thanks @xtqqczze!)</li>
<li>Remove obsolete <code>CA1701</code> rule suppression (#25948) (Thanks @xtqqczze!)</li>
<li>Remove obsolete <code>CA2233</code> rule suppression (#25951) (Thanks @xtqqczze!)</li>
<li>Remove obsolete <code>CA1026</code> rule suppression (#25934) (Thanks @xtqqczze!)</li>
<li>Remove obsolete <code>CA1059</code> rule suppression (#25940) (Thanks @xtqqczze!)</li>
<li>Remove obsolete <code>CA2118</code> rule suppression (#25924) (Thanks @xtqqczze!)</li>
<li>Remove redundant <code>System.Runtime.Versioning</code> attributes (#25926) (Thanks @xtqqczze!)</li>
<li>Seal internal types in <code>Microsoft.PowerShell.Commands.Utility</code> (#25892) (Thanks @xtqqczze!)</li>
<li>Seal internal types in <code>Microsoft.PowerShell.Commands.Management</code> (#25849) (Thanks @xtqqczze!)</li>
<li>Make the interface <code>IDeepCloneable</code> internal to minimize confusion (#25552)</li>
<li>Remove <code>OnDeserialized</code> and <code>Serializable</code> attributes from <code>Microsoft.Management.UI.Internal</code> project (#25548)</li>
<li>Refactor Tooltip/ListItemText mapping to use <code>CompletionDisplayInfoMapper</code> delegate (#25395) (Thanks @ArmaanMcleod!)</li>
</ul>

</details>

### Tools

- Add Codeql Suppressions (#25943, #26132)
- Update CODEOWNERS to add Justin as a maintainer (#25386)
- Do not run labels workflow in the internal repo (#25279)

### Tests

- Mark the 3 consistently failing tests as pending to unblock PRs (#26091)
- Make some tests less noisy on failure (#26035) (Thanks @xtqqczze!)
- Suppress false positive `PSScriptAnalyzer` warnings in tests and build scripts (#25864)
- Fix updatable help test for new content (#25819)
- Add more tests for `PSForEach` and `PSWhere` methods (#25519)
- Fix the isolated module test that was disabled previously (#25420)

### Build and Packaging Improvements

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@alerickson, @senerh, @RichardSlater, @xtqqczze</p>

</summary>

<ul>
<li>Update package references for the master branch (#26124)</li>
<li>Remove <code>ThreadJob</code> module and update <code>PSReadLine</code> to 2.4.4-beta4 (#26120)</li>
<li>Automate Store Publishing (#25725)</li>
<li>Add global config change detection to action (#26082)</li>
<li>Update outdated package references (#26069)</li>
<li>Ensure that the workflows are triggered on <code>.globalconfig</code> and other files at the root of the repo (#26034)</li>
<li>Update <code>Microsoft.PowerShell.PSResourceGet</code> to 1.2.0-preview3 (#26056) (Thanks @alerickson!)</li>
<li>Update metadata for Stable to v7.5.3 and LTS to v7.4.12 (#26054) (Thanks @senerh!)</li>
<li>Bump github/codeql-action from 3.30.2 to 3.30.3 (#26036)</li>
<li>Update version for the package <code>Microsoft.PowerShell.Native</code> (#26041)</li>
<li>Fix the APIScan pipeline (#26016)</li>
<li>Move PowerShell build to use .NET SDK 10.0.100-rc.1 (#26027)</li>
<li>fix(apt-package): add libicu76 dependency to support Debian 13 (#25866) (Thanks @RichardSlater!)</li>
<li>Bump github/codeql-action from 3.30.1 to 3.30.2 (#26029)</li>
<li>Update Ev2 Shell Extension Image to AzureLinux 3 for PMC Release (#26025)</li>
<li>Bump github/codeql-action from 3.30.0 to 3.30.1 (#26008)</li>
<li>Bump actions/github-script from 7 to 8 (#25983)</li>
<li>Fix variable reference for release environment in pipeline (#26012)</li>
<li>Add LinuxHost Network configuration to PowerShell Packages pipeline (#26000)</li>
<li>Make logical template name consistent between pipelines (#25990)</li>
<li>Update container images to use mcr.microsoft.com for Linux and Azure GǪ (#25981)</li>
<li>Bump github/codeql-action from 3.29.11 to 3.30.0 (#25966)</li>
<li>Bump actions/setup-dotnet from 4 to 5 (#25978)</li>
<li>Add build to vPack Pipeline (#25915)</li>
<li>Replace <code>DOTNET_SKIP_FIRST_TIME_EXPERIENCE</code> with <code>DOTNET_NOLOGO</code> (#25946) (Thanks @xtqqczze!)</li>
<li>Bump actions/dependency-review-action from 4.7.2 to 4.7.3 (#25930)</li>
<li>Bump github/codeql-action from 3.29.10 to 3.29.11 (#25889)</li>
<li>Remove AsyncSDL from Pipelines Toggle Official/NonOfficial Runs (#25885)</li>
<li>Specify .NET Search by Build Type (#25837)</li>
<li>Update PowerShell to use .NET SDK v10-preview.7 (#25876)</li>
<li>Bump actions/dependency-review-action from 4.7.1 to 4.7.2 (#25882)</li>
<li>Bump github/codeql-action from 3.29.9 to 3.29.10 (#25881)</li>
<li>Change the macos runner image to macos 15 large (#25867)</li>
<li>Bump actions/checkout from 4 to 5 (#25853)</li>
<li>Bump github/codeql-action from 3.29.7 to 3.29.9 (#25857)</li>
<li>Update to .NET 10 Preview 6 (#25828)</li>
<li>Bump agrc/create-reminder-action from 1.1.20 to 1.1.22 (#25808)</li>
<li>Bump agrc/reminder-action from 1.0.17 to 1.0.18 (#25807)</li>
<li>Bump github/codeql-action from 3.28.19 to 3.29.5 (#25797)</li>
<li>Bump super-linter/super-linter from 7.4.0 to 8.0.0 (#25770)</li>
<li>Update metadata for v7.5.2 and v7.4.11 releases (#25687)</li>
<li>Correct Capitalization Referencing Templates (#25669)</li>
<li>Change linux packaging tests to ubuntu latest (#25634)</li>
<li>Bump github/codeql-action from 3.28.18 to 3.28.19 (#25636)</li>
<li>Move to .NET 10 preview 4 and update package references (#25602)</li>
<li>Revert &quot;Add windows signing for pwsh.exe&quot; (#25586)</li>
<li>Bump ossf/scorecard-action from 2.4.1 to 2.4.2 (#25628)</li>
<li>Publish <code>.msixbundle</code> package as a VPack (#25612)</li>
<li>Bump agrc/reminder-action from 1.0.16 to 1.0.17 (#25573)</li>
<li>Bump agrc/create-reminder-action from 1.1.18 to 1.1.20 (#25572)</li>
<li>Bump github/codeql-action from 3.28.17 to 3.28.18 (#25580)</li>
<li>Bump super-linter/super-linter from 7.3.0 to 7.4.0 (#25563)</li>
<li>Bump actions/dependency-review-action from 4.7.0 to 4.7.1 (#25562)</li>
<li>Update metadata.json with 7.4.10 (#25554)</li>
<li>Bump github/codeql-action from 3.28.16 to 3.28.17 (#25508)</li>
<li>Bump actions/dependency-review-action from 4.6.0 to 4.7.0 (#25529)</li>
<li>Move MSIXBundle to Packages and Release to GitHub (#25512)</li>
<li>Update outdated package references (#25506)</li>
<li>Bump github/codeql-action from 3.28.15 to 3.28.16 (#25429)</li>
<li>Fix Conditional Parameter to Skip NuGet Publish (#25468)</li>
<li>Update metadata.json (#25438)</li>
<li>Fix MSIX artifact upload, vPack template, changelog hashes, git tag command (#25437)</li>
<li>Use new variables template for vPack (#25434)</li>
<li>Bump <code>agrc/create-reminder-action</code> from 1.1.17 to 1.1.18 (#25416)</li>
<li>Add PSScriptAnalyzer (#25423)</li>
<li>Update outdated package references (#25392)</li>
<li>Use GitHubReleaseTask instead of custom script (#25398)</li>
<li>Update APIScan to use new symbols server (#25388)</li>
<li>Retry ClearlyDefined operations (#25385)</li>
<li>Update to .NET 10.0.100-preview.3 (#25358)</li>
<li>Enhance path filters action to set outputs for all changes when not a PR (#25367)</li>
<li>Combine GitHub and Nuget Release Stage (#25318)</li>
<li>Add Windows Store Signing to MSIX bundle (#25296)</li>
<li>Bump skitionek/notify-microsoft-teams from 190d4d92146df11f854709774a4dae6eaf5e2aa3 to e7a2493ac87dad8aa7a62f079f295e54ff511d88 (#25366)</li>
<li>Add CodeQL suppressions for PowerShell intended behavior (#25359)</li>
<li>Migrate MacOS Signing to OneBranch (#25295)</li>
<li>Bump github/codeql-action from 3.28.13 to 3.28.15 (#25290)</li>
<li>Update test result processing to use NUnitXml format and enhance logging for better clarity (#25288)</li>
<li>Fix R2R for fxdependent packaging (#26131)</li>
<li>Remove <code>UseDotnet</code> task and use the <code>dotnet-install</code> script (#26093)</li>
</ul>

</details>

### Documentation and Help Content

- Fix a typo in the 7.4 changelog (#26038) (Thanks @VbhvGupta!)
- Add 7.4.12 Changelog (#26011)
- Add v7.5.3 Changelog (#25994)
- Fix typo in CHANGELOG for script filename suggestion (#25962)
- Update CHANGELOG for v7.5.2 (#25668)
- Update CHANGELOG for v7.4.11 (#25667)
- Update build documentation with instruction of dev terminal (#25587)
- Update links and contribution guide in documentation (#25532) (Thanks @JustinGrote!)
- Add 7.4.10 Changelog (#25520)
- Add 7.5.1 Change log (#25382)

[7.6.0-preview.5]: https://github.com/PowerShell/PowerShell/compare/v7.6.0-preview.4...v7.6.0-preview.5

## [7.6.0-preview.4]

### Breaking Changes

- Fix `WildcardPattern.Escape` to escape lone backticks correctly (#25211) (Thanks @ArmaanMcleod!)
- Convert `-ChildPath` parameter to `string[]` for `Join-Path` cmdlet (#24677) (Thanks @ArmaanMcleod!)

PowerShell 7.6-preview.4 includes the following updated modules:

- **Microsoft.PowerShell.ThreadJob** v2.2.0
- **ThreadJob** v2.1.0
The **ThreadJob** module was renamed to **Microsoft.PowerShell.ThreadJob**. There is no difference
in the functionality of the module. To ensure backward compatibility for scripts that use the old
name, the **ThreadJob** v2.1.0 module is a proxy module that points to the
**Microsoft.PowerShell.ThreadJob** v2.2.0.

### Engine Updates and Fixes

- Add `PipelineStopToken` to `Cmdlet` which will be signaled when the pipeline is stopping (#24620) (Thanks @jborean93!)
- Fallback to AppLocker after `WldpCanExecuteFile` (#24912)
- Move .NET method invocation logging to after the needed type conversion is done for method arguments (#25022)
- Fix share completion with provider and spaces (#19440) (Thanks @MartinGC94!)

### General Cmdlet Updates and Fixes

- Exclude `-OutVariable` assignments within the same `CommandAst` when inferring variables (#25224) (Thanks @MartinGC94!)
- Fix infinite loop in variable type inference (#25206) (Thanks @MartinGC94!)
- Update `Microsoft.PowerShell.PSResourceGet` version in `PSGalleryModules.csproj` (#25135)
- Add tooltips for hashtable key completions (#17864) (Thanks @MartinGC94!)
- Fix type inference of parameters in classic functions (#25172) (Thanks @MartinGC94!)
- Improve assignment type inference (#21143) (Thanks @MartinGC94!)
- Fix `TypeName.GetReflectionType()` to work when the `TypeName` instance represents a generic type definition within a `GenericTypeName` (#24985)
- Remove the old fuzzy suggestion and fix the local script filename suggestion (#25177)
- Improve variable type inference (#19830) (Thanks @MartinGC94!)
- Fix parameter completion when script requirements fail (#17687) (Thanks @MartinGC94!)
- Improve the completion for attribute arguments (#25129) (Thanks @MartinGC94!)
- Fix completion that relies on pseudobinding in script blocks (#25122) (Thanks @MartinGC94!)
- Don't complete duplicate command names (#21113) (Thanks @MartinGC94!)
- Make `SystemPolicy` public APIs visible but non-op on Unix platforms so that they can be included in `PowerShellStandard.Library` (#25051)
- Set standard handles explicitly when starting a process with `-NoNewWindow` (#25061)
- Fix tooltip for variable expansion and include desc (#25112) (Thanks @jborean93!)
- Add type inference for functions without OutputType attribute and anonymous functions (#21127) (Thanks @MartinGC94!)
- Add completion for variables assigned by command redirection (#25104) (Thanks @MartinGC94!)
- Handle type inference for redirected commands (#21131) (Thanks @MartinGC94!)
- Allow empty prefix string in `Import-Module -Prefix` to override default prefix in manifest (#20409) (Thanks @MartinGC94!)
- Update variable/property assignment completion so it can fallback to type inference (#21134) (Thanks @MartinGC94!)
- Use `Get-Help` approach to find `about_*.help.txt` files with correct locale for completions (#24194) (Thanks @MartinGC94!)
- Use script filepath when completing relative paths for using statements (#20017) (Thanks @MartinGC94!)
- Fix completion of variables assigned inside Do loops (#25076) (Thanks @MartinGC94!)
- Fix completion of provider paths when a path returns itself instead of its children (#24755) (Thanks @MartinGC94!)
- Enable completion of scoped variables without specifying scope (#20340) (Thanks @MartinGC94!)
- Fix issue with incomplete results when completing paths with wildcards in non-filesystem providers (#24757) (Thanks @MartinGC94!)
- Allow DSC parsing through OS architecture translation layers (#24852) (Thanks @bdeb1337!)

### Code Cleanup

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@ArmaanMcleod, @pressRtowin</p>

</summary>

<ul>
<li>Refactor and add comments to <code>CompletionRequiresQuotes</code> to clarify implementation (#25223) (Thanks @ArmaanMcleod!)</li>
<li>Add <code>QuoteCompletionText</code> method to CompletionHelpers class (#25180) (Thanks @ArmaanMcleod!)</li>
<li>Remove CompletionHelpers <code>escape</code> parameter from <code>CompletionRequiresQuotes</code> (#25178) (Thanks @ArmaanMcleod!)</li>
<li>Refactor CompletionHelpers <code>HandleDoubleAndSingleQuote</code> to have less nesting logic (#25179) (Thanks @ArmaanMcleod!)</li>
<li>Make the use of Oxford commas consistent (#25139)(#25140)(Thanks @pressRtowin!)</li>
<li>Move common completion methods to CompletionHelpers class (#25138) (Thanks @ArmaanMcleod!)</li>
<li>Return <code>Array.Empty</code> instead of collection <code>[]</code> (#25137) (Thanks @ArmaanMcleod!)</li>
</ul>

</details>

### Tools

- Check GH token availability for Get-Changelog (#25133)

### Tests

- Add XUnit test for `HandleDoubleAndSingleQuote` in CompletionHelpers class (#25181) (Thanks @ArmaanMcleod!)

### Build and Packaging Improvements

<details>

<ul>
<li>Switch to ubuntu-lastest for CI (#25247)</li>
<li>Update outdated package references (#25026)(#25232)</li>
<li>Bump <code>Microsoft.PowerShell.ThreadJob</code> and <code>ThreadJob</code> modules (#25232)</li>
<li>Bump github/codeql-action from 3.27.9 to 3.28.13 (#25218)(#25231)</li>
<li>Update .NET SDK to <code>10.0.100-preview.2</code> (#25154)(#25225)</li>
<li>Remove obsolete template from Windows Packaging CI (#25226)</li>
<li>Bump actions/upload-artifact from 4.5.0 to 4.6.2 (#25220)</li>
<li>Bump agrc/reminder-action from 1.0.15 to 1.0.16 (#25222)</li>
<li>Bump actions/checkout from 2 to 4 (#25221)</li>
<li>Add <code>NoWarn NU1605</code> to System.ServiceModel.* (#25219)</li>
<li>Bump actions/github-script from 6 to 7 (#25217)</li>
<li>Bump ossf/scorecard-action from 2.4.0 to 2.4.1 (#25216)</li>
<li>Bump super-linter/super-linter from 7.2.1 to 7.3.0 (#25215)</li>
<li>Bump agrc/create-reminder-action from 1.1.16 to 1.1.17 (#25214)</li>
<li>Remove dependabot updates that don't work (#25213)</li>
<li>Update GitHub Actions to work in private GitHub repo (#25197)</li>
<li>Cleanup old release pipelines (#25201)</li>
<li>Update package pipeline windows image version (#25191)</li>
<li>Skip additional packages when generating component manifest (#25102)</li>
<li>Only build Linux for packaging changes (#25103)</li>
<li>Remove Az module installs and AzureRM uninstalls in pipeline (#25118)</li>
<li>Add GitHub Actions workflow to verify PR labels (#25145)</li>
<li>Add back-port workflow using dotnet/arcade (#25106)</li>
<li>Make Component Manifest Updater use neutral target in addition to RID target (#25094)</li>
<li>Make sure the vPack pipeline does not produce an empty package (#24988)</li>
</ul>

</details>

### Documentation and Help Content

- Add 7.4.9 changelog (#25169)
- Create changelog for 7.4.8 (#25089)

[7.6.0-preview.4]: https://github.com/PowerShell/PowerShell/compare/v7.6.0-preview.3...v7.6.0-preview.4

## [7.6.0-preview.3]

### Breaking Changes

- Remove trailing space from event source name (#24192) (Thanks @MartinGC94!)

### General Cmdlet Updates and Fixes

- Add completion single/double quote support for `-Noun` parameter for `Get-Command` (#24977) (Thanks @ArmaanMcleod!)
- Stringify `ErrorRecord` with empty exception message to empty string (#24949) (Thanks @MatejKafka!)
- Add completion single/double quote support for `-PSEdition` parameter for `Get-Module` (#24971) (Thanks @ArmaanMcleod!)
- Error when `New-Item -Force` is passed an invalid directory name (#24936) (Thanks @kborowinski!)
- Allow `Start-Transcript`to use `$Transcript` which is a `PSObject` wrapped string to specify the transcript path (#24963) (Thanks @kborowinski!)
- Add quote handling in `Verb`, `StrictModeVersion`, `Scope` & `PropertyType` Argument Completers with single helper method (#24839) (Thanks @ArmaanMcleod!)
- Improve `Start-Process -Wait` polling efficiency (#24711) (Thanks @jborean93!)
- Convert `InvalidCommandNameCharacters` in `AnalysisCache` to `SearchValues<char>` for more efficient char searching (#24880) (Thanks @ArmaanMcleod!)
- Convert `s_charactersRequiringQuotes` in Completion Completers to `SearchValues<char>` for more efficient char searching (#24879) (Thanks @ArmaanMcleod!)

### Code Cleanup

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@xtqqczze, @fMichaleczek, @ArmaanMcleod</p>

</summary>

<ul>
<li>Fix <code>RunspacePool</code>, <code>RunspacePoolInternal</code> and <code>RemoteRunspacePoolInternal</code> <code>IDisposable</code> implementation (#24720) (Thanks @xtqqczze!)</li>
<li>Remove redundant <code>Attribute</code> suffix (#24940) (Thanks @xtqqczze!)</li>
<li>Fix formatting of the XML comment for <code>SteppablePipeline.Clean()</code> (#24941)</li>
<li>Use <code>Environment.ProcessId</code> in <code>SpecialVariables.PID</code> (#24926) (Thanks @fMichaleczek!)</li>
<li>Replace char[] array in <code>CompletionRequiresQuotes</code> with cached SearchValues<char> (#24907) (Thanks @ArmaanMcleod!)</li>
<li>Update <code>IndexOfAny</code> calls with invalid path/filename to <code>SearchValues&lt;char&gt;</code> for more efficient char searching (#24896) (Thanks @ArmaanMcleod!)</li>
<li>Seal internal types in <code>PlatformInvokes</code> (#24826) (Thanks @xtqqczze!)</li>
</ul>

</details>

### Tools

- Update CODEOWNERS (#24989)

### Build and Packaging Improvements

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@xtqqczze, @KyZy7</p>

</summary>

<ul>
<li>Update branch for release  - Transitive - false - none (#24995)</li>
<li>Add setup dotnet action to the build composite action (#24996)</li>
<li>Give the pipeline runs meaningful names (#24987)</li>
<li>Fix V-Pack download package name (#24866)</li>
<li>Set <code>LangVersion</code> compiler option to <code>13.0</code> in Test.Common.props (#24621) (Thanks @xtqqczze!)</li>
<li>Fix release branch filters (#24933)</li>
<li>Fix GitHub Action filter overmatching (#24929)</li>
<li>Add UseDotnet task for installing dotnet (#24905)</li>
<li>Convert powershell/PowerShell-CI-macos to GitHub Actions (#24914)</li>
<li>Convert powershell/PowerShell-CI-linux to GitHub Actions (#24913)</li>
<li>Convert powershell/PowerShell-Windows-CI to GitHub Actions (#24899)</li>
<li>Fix MSIX stage in release pipeline (#24900)</li>
<li>Update .NET SDK (#24906)</li>
<li>Update metadata.json (#24862)</li>
<li>PMC parse state correctly from update command's response (#24850)</li>
<li>Add EV2 support for publishing PowerShell packages to PMC (#24841)</li>
<li>Remove AzDO credscan as it is now in GitHub (#24842)</li>
<li>Add *.props and sort path filters for windows CI  (#24822)</li>
<li>Use work load identity service connection to download makeappx tool from storage account (#24817)</li>
<li>Update path filters for Windows CI (#24809)</li>
<li>Update outdated package references (#24758)</li>
<li>Update metadata.json (#24787) (Thanks @KyZy7!)</li>
<li>Add tool package download in publish nuget stage (#24790)</li>
<li>Fix Changelog content grab during GitHub Release (#24788)</li>
<li>Update metadata.json (#24764)</li>
<li>Update <code>Microsoft.PowerShell.PSResourceGet</code> to <code>1.1.0</code> (#24767)</li>
<li>Add a parameter that skips verify packages step (#24763)</li>
</ul>

</details>

### Documentation and Help Content

- Add 7.4.7 Changelog (#24844)
- Create changelog for v7.5.0 (#24808)
- Update Changelog for v7.6.0-preview.2 (#24775)

[7.6.0-preview.3]: https://github.com/PowerShell/PowerShell/compare/v7.6.0-preview.2...v7.6.0-preview.3

## [7.6.0-preview.2] - 2025-01-14

### General Cmdlet Updates and Fixes

- Add the `AIShell` module to telemetry collection list (#24747)
- Add helper in `EnumSingleTypeConverter` to get enum names as array (#17785) (Thanks @fflaten!)
- Return correct FileName property for `Get-Item` when listing alternate data streams (#18019) (Thanks @kilasuit!)
- Add `-ExcludeModule` parameter to `Get-Command` (#18955) (Thanks @MartinGC94!)
- Update Named and Statement block type inference to not consider AssignmentStatements and Increment/decrement operators as part of their output (#21137) (Thanks @MartinGC94!)
- Update `DnsNameList` for `X509Certificate2` to use `X509SubjectAlternativeNameExtension.EnumerateDnsNames` Method (#24714) (Thanks @ArmaanMcleod!)
- Add completion of modules by their shortname (#20330) (Thanks @MartinGC94!)
- Fix `Get-ItemProperty` to report non-terminating error for cast exception (#21115) (Thanks @ArmaanMcleod!)
- Add `-PropertyType` argument completer for `New-ItemProperty` (#21117) (Thanks @ArmaanMcleod!)
- Fix a bug in how `Write-Host` handles `XmlNode` object (#24669) (Thanks @brendandburns!)

### Code Cleanup

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@xtqqczze</p>

</summary>

<ul>
<li>Seal <code>ClientRemoteSessionDSHandlerImpl</code> (#21218) (Thanks @xtqqczze!)</li>
<li>Seal internal type <code>ClientRemoteSessionDSHandlerImpl</code> (#24705) (Thanks @xtqqczze!)</li>
<li>Seal classes in <code>RemotingProtocol2</code> (#21164) (Thanks @xtqqczze!)</li>
</ul>

</details>

### Tools

- Added Justin Chung as Powershell team memeber on releaseTools.psm1 (#24672)

### Tests

- Skip CIM ETS member test on older Windows platforms (#24681)

### Build and Packaging Improvements

<details>

<summary>

<p>Updated SDK to 9.0.101</p>

</summary>

<ul>
<li>Update branch for release  - Transitive - false - none (#24754)</li>
<li>Update <code>Microsoft.PowerShell.PSResourceGet</code> to <code>1.1.0</code> (#24767)</li>
<li>Add a parameter that skips verify packages step (#24763)</li>
<li>Make the <code>AssemblyVersion</code> not change for servicing releases (#24667)</li>
<li>Fixed release pipeline errors and switched to KS3 (#24751)</li>
<li>Update outdated package references (#24580)</li>
<li>Bump actions/upload-artifact from 4.4.3 to 4.5.0 (#24689)</li>
<li>Update .NET feed with new domain as <code>azureedge</code> is retiring (#24703)</li>
<li>Bump super-linter/super-linter from 7.2.0 to 7.2.1 (#24678)</li>
<li>Bump github/codeql-action from 3.27.7 to 3.27.9 (#24674)</li>
<li>Bump actions/dependency-review-action from 4.4.0 to 4.5.0 (#24607)</li>
</ul>

</details>

### Documentation and Help Content

- Update cmdlets WG members (#24275) (Thanks @kilasuit!)

[7.6.0-preview.2]: https://github.com/PowerShell/PowerShell/compare/v7.6.0-preview.1...v7.6.0-preview.2

## [7.6.0-preview.1] - 2024-12-16

### Breaking Changes

- Treat large Enum values as numbers in `ConvertTo-Json` (#20999) (Thanks @jborean93!)

### General Cmdlet Updates and Fixes

- Add proper error for running `Get-PSSession -ComputerName` on Unix (#21009) (Thanks @jborean93!)
- Resolve symbolic link target relative to the symbolic link instead of the working directory (#15235) (#20943) (Thanks @MatejKafka!)
- Fix up buffer management getting network roots (#24600) (Thanks @jborean93!)
- Support `PSObject` wrapped values in `ArgumentToEncodingTransformationAttribute` (#24555) (Thanks @jborean93!)
- Update PSReadLine to 2.3.6 (#24380)
- Add telemetry to track the use of features (#24247)
- Handle global tool specially when prepending `PSHome` to `PATH` (#24228)
- Fix how processor architecture is validated in `Import-Module` (#24265)
- Make features `PSCommandNotFoundSuggestion`, `PSCommandWithArgs`, and `PSModuleAutoLoadSkipOfflineFiles` stable (#24246)
- Write type data to the pipeline instead of collecting it (#24236) (Thanks @MartinGC94!)
- Add support to `Get-Error` to handle BoundParameters (#20640)
- Fix `Get-FormatData` to not cast a type incorrectly (#21157)
- Delay progress bar in `Copy-Item` and `Remove-Item` cmdlets (#24013) (Thanks @TheSpyGod!)
- Add `-Force` parameter to `Resolve-Path` and `Convert-Path` cmdlets to support wildcard hidden files (#20981) (Thanks @ArmaanMcleod!)
- Use host exe to determine `$PSHOME` location when `SMA.dll` location is not found (#24072)
- Fix `Test-ModuleManifest` so it can use a UNC path (#24115)

### Code Cleanup

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@eltociear, @JayBazuzi</p>

</summary>

<ul>
<li>Fix typos in <code>ShowModuleControl.xaml.cs</code> (#24248) (Thanks @eltociear!)</li>
<li>Fix a typo in the build doc (#24172) (Thanks @JayBazuzi!)</li>
</ul>

</details>

### Tools

- Fix devcontainer extensions key (#24359) (Thanks @ThomasNieto!)
- Support new backport branch format (#24378)
- Update markdownLink.yml to not run on release branches (#24323)
- Remove old code that downloads msix for win-arm64 (#24175)

### Tests

- Fix cleanup in PSResourceGet test (#24339)

### Build and Packaging Improvements

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@MartinGC94, @jborean93, @xtqqczze, @alerickson, @iSazonov, @rzippo</p>

</summary>

<ul>
<li>Deploy Box update (#24632)</li>
<li>Remove Regex use (#24235) (Thanks @MartinGC94!)</li>
<li>Improve cim ETS member inference completion (#24235) (Thanks @MartinGC94!)</li>
<li>Emit ProgressRecord in CLIXML minishell output (#21373) (Thanks @jborean93!)</li>
<li>Assign the value returned by the <code>MaybeAdd</code> method</li> (#24652)
<li>Add support for interface static abstract props (#21061) (Thanks @jborean93!)</li>
<li>Change call to optional add in the binder expression (#24451) (Thanks @jborean93!)</li>
<li>Turn off AMSI member invocation on nix release builds (#24451) (Thanks @jborean93!)</li>
<li>Bump github/codeql-action from 3.27.0 to 3.27.6 (#24639)</li>
<li>Update src/Microsoft.PowerShell.ConsoleHost/host/msh/ConsoleHost.cs (#24239) (Thanks @jborean93!)</li>
<li>Apply suggestions from code review (#24239) (Thanks @jborean93!)</li>
<li>Add remote runspace check for PushRunspace (#24239) (Thanks @jborean93!)</li>
<li>Set LangVersion compiler option to 13.0 (#24619) (Thanks @xtqqczze!)</li>
<li>Set <code>LangVersion</code> compiler option to <code>13.0</code> (#24617) (Thanks @xtqqczze!)</li>
<li>Update metadata.json for PowerShell 7.5 RC1 release (#24589)</li>
<li>Update nuget publish to use Deploy Box (#24596)</li>
<li>Added Deploy Box Product Pathway to GitHub Release and NuGet Release Pipelines (#24583)</li>
<li>Update machine pool for copy blob and upload buildinfo stage (#24587)</li>
<li>Bump .NET 9 and dependencies (#24573)</li>
<li>Bump actions/dependency-review-action from 4.3.4 to 4.4.0 (#24503)</li>
<li>Bump actions/checkout from 4.2.1 to 4.2.2 (#24488)</li>
<li>Bump agrc/reminder-action from 1.0.14 to 1.0.15 (#24384)</li>
<li>Bump actions/upload-artifact from 4.4.0 to 4.4.3 (#24410)</li>
<li>Update branch for release (#24534)</li>
<li>Revert &quot;Update package references (#24414)&quot; (#24532)</li>
<li>Add a way to use only NuGet feed sources (#24528)</li>
<li>Update PSResourceGet to v1.1.0-RC2 (#24512) (Thanks @alerickson!)</li>
<li>Bump .NET to 9.0.100-rc.2.24474.11 (#24509)</li>
<li>Fix seed max value for Container Linux CI (#24510)</li>
<li>Update metadata.json for 7.2.24 and 7.4.6 releases (#24484)</li>
<li>Download package from package build for generating vpack (#24481)</li>
<li>Keep the roff file when gzipping it. (#24450)</li>
<li>Delete the msix blob if it's already there (#24353)</li>
<li>Add PMC mapping for debian 12 (bookworm) (#24413)</li>
<li>Checkin generated manpage (#24423)</li>
<li>Add CodeQL scanning to APIScan build (#24303)</li>
<li>Update package references (#24414)</li>
<li>Update vpack pipeline (#24281)</li>
<li>Bring changes from v7.5.0-preview.5 Release Branch to Master (#24369)</li>
<li>Bump agrc/create-reminder-action from 1.1.15 to 1.1.16 (#24375)</li>
<li>Add <code>BaseUrl</code> to <code>buildinfo</code> json file (#24376)</li>
<li>Update metadata.json (#24352)</li>
<li>Copy to static site instead of making blob public (#24269)</li>
<li>Update <code>Microsoft.PowerShell.PSResourceGet</code> to <code>1.1.0-preview2</code> (#24300) (Thanks @alerickson!)</li>
<li>add updated libicu dependency for debian packages (#24301)</li>
<li>add mapping to azurelinux repo (#24290)</li>
<li>Remove the MD5 branch in the strong name signing token calculation (#24288)</li>
<li>Bump .NET 9 to <code>9.0.100-rc.1.24452.12</code> (#24273)</li>
<li>Ensure the official build files CodeQL issues (#24278)</li>
<li>Update experimental-feature json files (#24271)</li>
<li>Make some release tests run in a hosted pools (#24270)</li>
<li>Do not build the exe for Global tool shim project (#24263)</li>
<li>Update and add new NuGet package sources for different environments. (#24264)</li>
<li>Bump skitionek/notify-microsoft-teams (#24261)</li>
<li>Create new pipeline for compliance  (#24252)</li>
<li>Capture environment better (#24148)</li>
<li>Add specific path for issues in tsaconfig (#24244)</li>
<li>Use Managed Identity for APIScan authentication (#24243)</li>
<li>Add windows signing for pwsh.exe (#24219)</li>
<li>Bump super-linter/super-linter from 7.0.0 to 7.1.0 (#24223)</li>
<li>Update the URLs used in nuget.config files (#24203)</li>
<li>Check <code>Create and Submit</code> in vPack build by default (#24181)</li>
<li>Replace <code>PSVersion</code> source generator with incremental one (#23815) (Thanks @iSazonov!)</li>
<li>Save man files in <code>/usr/share/man</code> instead of <code>/usr/local/share/man</code> (#23855) (Thanks @rzippo!)</li>
<li>Bump super-linter/super-linter from 6.8.0 to 7.0.0 (#24169)</li>
</ul>

</details>

### Documentation and Help Content

- Updated Third Party Notices (#24666)
- Update `HelpInfoUri` for 7.5 (#24610)
- Update changelog for v7.4.6 release (#24496)
- Update to the latest NOTICES file (#24259)
- Update the changelog `preview.md` (#24213)
- Update changelog readme with 7.4 (#24182) (Thanks @ThomasNieto!)
- Fix Markdown linting error (#24204)
- Updated changelog for v7.2.23 (#24196) (Internal 32131)
- Update changelog and `metadata.json` for v7.4.5 release (#24183)
- Bring 7.2 changelogs back to master (#24158)

[7.6.0-preview.1]: https://github.com/PowerShell/PowerShell/compare/v7.5.0-rc.1...v7.6.0-preview.1

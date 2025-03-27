# Preview Changelog

## [7.6.0-preview.4] 

### Breaking Changes

- Fix `WildcardPattern.Escape` to escape lone backticks correctly (#25211) (Thanks @ArmaanMcleod!)
- Convert `-ChildPath` parameter to `string[]` for `Join-Path` cmdlet (#24677) (Thanks @ArmaanMcleod!)

### Engine Updates and Fixes

- Add `PipelineStopToken` to `Cmdlet` which will be signaled when the pipeline is stopping (#24620) (Thanks @jborean93!)
- Fallback to AppLocker after `WldpCanExecuteFile` (#24912)
- Move .NET method invocation logging to after the needed type conversion is done for method arguments (#25022)
- Fix share completion with provider and spaces (#19440) (Thanks @MartinGC94!)

### General Cmdlet Updates and Fixes

- Exclude outvariable assignments within the same CommandAst when inferring variables (#25224) (Thanks @MartinGC94!)
- Fix infinite loop in variable type inference (#25206) (Thanks @MartinGC94!)
- Update `Microsoft.PowerShell.PSResourceGet` version in `PSGalleryModules.csproj` (#25135)
- Add tooltips for hashtable key completions (#17864) (Thanks @MartinGC94!)
- Fix type inference of parameters in classic functions (#25172) (Thanks @MartinGC94!)
- Improve assignment type inference (#21143) (Thanks @MartinGC94!)
- Fix `TypeName.GetReflectionType()` to work when the `TypeName` instance represents a generic type definition within a `GenericTypeName` (#24985)
- Remove the old fuzzy suggestion and fix the local script file name suggestion (#25177)
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
- Allow empty prefix string in 'Import-Module -Prefix' to override default prefix in manifest (#20409) (Thanks @MartinGC94!)
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

<summary>

<p>We thank the following contributors!</p>

</summary>

<ul>
<li>Update outdated package references (#25026)(#25232)</li>
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
- Create Change log for 7.4.8 (#25089)

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

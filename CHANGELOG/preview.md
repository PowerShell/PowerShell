# Preview Changelog

## [7.5.0-preview.5] - 2024-10-01

### Breaking Changes

- Treat large `Enum` values as numbers in `ConvertTo-Json` (#20999) (#24304)

### Engine Updates and Fixes

- Fix how processor architecture is validated in `Import-Module` (#24265) (#24317)

### Experimental Features

### General Cmdlet Updates and Fixes

- Add `-Force` parameter to `Resolve-Path` and `Convert-Path` cmdlets to support wildcard hidden files (#20981) (#24344)
- Add telemetry to track the use of features (#24247) (#24331)
- Treat large `Enum` values as numbers in `ConvertTo-Json` (#20999) (#24304)
- Make features `PSCommandNotFoundSuggestion`, `PSCommandWithArgs`, and `PSModuleAutoLoadSkipOfflineFiles` stable (#24246) (#24310)
- Handle global tool when prepending `$PSHome` to `PATH` (#24228) (#24307)

### Tests

- Fix cleanup in `PSResourceGet` test (#24339) (#24345)

### Build and Packaging Improvements

<details>

<summary>

<p>Bump .NET SDK to 9.0.100-rc.1.24452.12</p>

</summary>

<ul>
<li>Fixed Test Scenario for <code>Compress-PSResource</code> (Internal 32696)</li>
<li>Add back local NuGet source for test packages (Internal 32693)</li>
<li>Fix typo in <code>release-MakeBlobPublic.yml</code> (Internal 32689)</li>
<li>Copy to static site instead of making blob public (#24269) (#24343)</li>
<li>Update <code>Microsoft.PowerShell.PSResourceGet</code> to <code>1.1.0-preview2</code> (#24300) (#24337)</li>
<li>Remove the MD5 branch in the strong name signing token calculation (#24288) (#24321)</li>
<li>Update experimental-feature json files (#24271) (#24319)</li>
<li>Add updated <code>libicu</code> dependency for Debian packages (#24301) (#24324)</li>
<li>Add mapping to <code>AzureLinux</code> repo (#24290) (#24322)</li>
<li>Update and add new NuGet package sources for different environments. (#24264) (#24316)</li>
<li>Bump .NET 9 to <code>9.0.100-rc.1.24452.12</code> (#24273) (#24320)</li>
<li>Make some release tests run in a hosted pools (#24270) (#24318)</li>
<li>Do not build the exe for Global tool shim project (#24263) (#24315)</li>
<li>Delete <code>assets/AppImageThirdPartyNotices.txt</code> (#24256) (#24313)</li>
<li>Create new pipeline for compliance (#24252) (#24312)</li>
<li>Add specific path for issues in tsaconfig (#24244) (#24309)</li>
<li>Use Managed Identity for APIScan authentication (#24243) (#24308)</li>
<li>Add Windows signing for <code>pwsh.exe</code> (#24219) (#24306)</li>
<li>Check <code>Create and Submit</code> in vPack build by default (#24181) (#24305)</li>
</ul>

</details>

### Documentation and Help Content

- Delete demos directory (#24258) (#24314)

[7.5.0-preview.5]: https://github.com/PowerShell/PowerShell/compare/v7.5.0-preview.4...v7.5.0-preview.5

## [7.5.0-preview.4] - 2024-08-28

### Engine Updates and Fixes

- RecommendedAction: Explicitly start and stop ANSI Error Color (#24065) (Thanks @JustinGrote!)
- Improve .NET overload definition of generic methods (#21326) (Thanks @jborean93!)
- Optimize the `+=` operation for a collection when it's an object array (#23901) (Thanks @jborean93!)
- Allow redirecting to a variable as experimental feature `PSRedirectToVariable` (#20381)

### General Cmdlet Updates and Fixes

- Change type of `LineNumber` to `ulong` in `Select-String` (#24075) (Thanks @Snowman-25!)
- Fix `Invoke-RestMethod` to allow `-PassThru` and `-Outfile` work together (#24086) (Thanks @jshigetomi!)
- Fix Hyper-V Remoting when the module is imported via implicit remoting (#24032) (Thanks @jborean93!)
- Add `ConvertTo-CliXml` and `ConvertFrom-CliXml` cmdlets (#21063) (Thanks @ArmaanMcleod!)
- Add `OutFile` property in `WebResponseObject` (#24047) (Thanks @jshigetomi!)
- Show filename in `Invoke-WebRequest -OutFile -Verbose` (#24041) (Thanks @jshigetomi!)
- `Set-Acl`: Do not fail on untranslatable SID (#21096) (Thanks @jborean93!)
- Fix the extent of the parser error when a number constant is invalid (#24024)
- Fix `Move-Item` to throw error when moving into itself (#24004)
- Fix up .NET method invocation with `Optional` argument (#21387) (Thanks @jborean93!)
- Fix progress calculation on `Remove-Item` (#23869) (Thanks @jborean93!)
- Fix WebCmdlets when `-Body` is specified but `ContentType` is not (#23952) (Thanks @CarloToso!)
- Enable `-NoRestart` to work with `Register-PSSessionConfiguration` (#23891)
- Add `IgnoreComments` and `AllowTrailingCommas` options to `Test-Json` cmdlet (#23817) (Thanks @ArmaanMcleod!)
- Get-Help may report parameters with `ValueFromRemainingArguments` attribute as pipeline-able (#23871)

### Code Cleanup

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@xtqqczze, @eltociear</p>

</summary>

<ul>
<li>Minor cleanup on local variable names within a method (#24105)</li>
<li>Remove explicit <code>IDE1005</code> suppressions (#21217) (Thanks @xtqqczze!)</li>
<li>Fix a typo in <code>WebRequestSession.cs</code> (#23963) (Thanks @eltociear!)</li>
</ul>

</details>

### Tools

- devcontainers: mount workspace in /PowerShell (#23857) (Thanks @rzippo!)

### Tests

- Add debugging to the MTU size test (#21463)

### Build and Packaging Improvements

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@bosesubham2011</p>

</summary>

<ul>
<li>Update third party notices (Internal 32128)</li>
<li>Update cgmanifest (#24163)</li>
<li>Fixes to Azure Public feed usage (#24149)</li>
<li>Add support for back porting PRs from GitHub or the Private Azure Repos (#20670)</li>
<li>Move to <code>9.0.0-preview.6.24327.7</code> (#24133)</li>
<li>update path (#24134)</li>
<li>Update to the latest NOTICES file (#24131)</li>
<li>Fix semver issue with updating cgmanifest (#24132)</li>
<li>Add ability to capture MSBuild Binary logs when restore fails (#24128)</li>
<li>add ability to skip windows stage (#24116)</li>
<li>chore: Refactor Nuget package source creation to use New-NugetPackageSource function (#24104)</li>
<li>Make Microsoft feeds the default (#24098)</li>
<li>Cleanup unused csproj (#23951)</li>
<li>Add script to update SDK version during release (#24034)</li>
<li>Enumerate over all signed zip packages (#24063)</li>
<li>Update metadata.json for PowerShell July releases (#24082)</li>
<li>Add macos signing for package files (#24015)</li>
<li>Update <code>install-powershell.sh</code> to support azure-linux (#23955) (Thanks @bosesubham2011!)</li>
<li>Skip build steps that do not have exe packages (#23945)</li>
<li>Update metadata.json for PowerShell June releases (#23973)</li>
<li>Create <code>powershell.config.json</code> for <code>PowerShell.Windows.x64</code> global tool (#23941)</li>
<li>Fix error in the vPack release, debug script that blocked release (#23904)</li>
<li>Add vPack release (#23898)</li>
<li>Fix exe signing with third party signing for WiX engine (#23878)</li>
<li>Update wix installation in CI (#23870)</li>
<li>Add checkout to fix TSA config paths (#23865)</li>
<li>Merge the <code>v7.5.0-preview.3</code> release branch to GitHub master branch</li>
<li>Update <code>metadata.json</code> for the <code>v7.5.0-preview.3</code> release (#23862)</li>
<li>Bump <code>PSResourceGet</code> to <code>1.1.0-preview1</code> (#24129)</li>
<li>Bump <code>github/codeql-action</code> from 3.25.8 to 3.26.0 (#23953) (#23999) (#24053) (#24069) (#24095) (#24118)</li>
<li>Bump <code>actions/upload-artifact</code> from 4.3.3 to 4.3.6 (#24019) (#24113) (#24119)</li>
<li>Bump <code>agrc/create-reminder-action</code> from 1.1.13 to 1.1.15 (#24029) (#24043)</li>
<li>Bump <code>agrc/reminder-action</code> from 1.0.12 to 1.0.14 (#24028) (#24042)</li>
<li>Bump <code>super-linter/super-linter</code> from 5.7.2 to 6.8.0 (#23809) (#23856) (#23894) (#24030) (#24103)</li>
<li>Bump <code>ossf/scorecard-action</code> from 2.3.1 to 2.4.0 (#23802) (#24096)</li>
<li>Bump <code>actions/dependency-review-action</code> from 4.3.2 to 4.3.4 (#23897) (#24046)</li>
<li>Bump <code>actions/checkout</code> from 4.1.5 to 4.1.7 (#23813) (#23947)</li>
<li>Bump <code>github/codeql-action</code> from 3.25.4 to 3.25.8 (#23801) (#23893)</li>
</ul>

</details>

### Documentation and Help Content

- Update docs sample nuget.config (#24109)
- Update Code of Conduct and Security Policy (#23811)
- Update working-group-definitions.md for the Security WG (#23884)
- Fix up broken links in Markdown files (#23863)
- Update Engine Working Group Members (#23803) (Thanks @kilasuit!)
- Remove outdated and contradictory information from `README` (#23812)

[7.5.0-preview.4]: https://github.com/PowerShell/PowerShell/compare/v7.5.0-preview.3...v7.5.0-preview.4

## [7.5.0-preview.3] - 2024-05-16

### Breaking Changes

- Remember installation options and used them to initialize options for the next installation (#20420) (Thanks @reduckted!)
- `ConvertTo-Json`: Serialize `BigInteger` as a number (#21000) (Thanks @jborean93!)

### Engine Updates and Fixes

- Fix generating `OutputType` when running in Constrained Language Mode (#21605)
- Revert the PR #17856 (Do not preserve temporary results when no need to do so) (#21368)
- Make sure the assembly/library resolvers are registered at early stage (#21361)
- Fix PowerShell class to support deriving from an abstract class with abstract properties (#21331)
- Fix error formatting for pipeline enumeration exceptions (#20211)

### General Cmdlet Updates and Fixes

- Added progress bar for `Remove-Item` cmdlet (#20778) (Thanks @ArmaanMcleod!)
- Expand `~` to `$home` on Windows with tab completion (#21529)
- Separate DSC configuration parser check for ARM processor (#21395) (Thanks @dkontyko!)
- Fix `[semver]` type to pass `semver.org` tests (#21401)
- Don't complete when declaring parameter name and class member (#21182) (Thanks @MartinGC94!)
- Add `RecommendedAction` to `ConciseView` of the error reporting (#20826) (Thanks @JustinGrote!)
- Fix the error when using `Start-Process -Credential` without the admin privilege (#21393) (Thanks @jborean93!)
- Fix `Test-Path -IsValid` to check for invalid path and filename characters (#21358)
- Fix build failure due to missing reference in `GlobalToolShim.cs` (#21388)
- Fix argument passing in `GlobalToolShim` (#21333) (Thanks @ForNeVeR!)
- Make sure both stdout and stderr can be redirected from a native executable (#20997)
- Handle the case that `Runspace.DefaultRunspace == null` when logging for WDAC Audit (#21344)
- Fix a typo in `releaseTools.psm1` (#21306) (Thanks @eltociear!)
- `Get-Process`: Remove admin requirement for `-IncludeUserName` (#21302) (Thanks @jborean93!)
- Fall back to type inference when hashtable key-value cannot be retrieved from safe expression (#21184) (Thanks @MartinGC94!)
- Fix the regression when doing type inference for `$_` (#21223) (Thanks @MartinGC94!)
- Revert "Adjust PUT method behavior to POST one for default content type in WebCmdlets" (#21049)
- Fix a regression in `Format-Table` when header label is empty (#21156)

### Code Cleanup

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@xtqqczze</p>

</summary>

<ul>
<li>Enable <code>CA1868</code>: Unnecessary call to 'Contains' for sets (#21165) (Thanks @xtqqczze!)</li>
<li>Remove <code>JetBrains.Annotations</code> attributes (#21246) (Thanks @xtqqczze!)</li>
</ul>

</details>

### Tests

- Update `metadata.json` and `README.md` (#21454)
- Skip test on Windows Server 2012 R2 for `no-nl` (#21265)

### Build and Packaging Improvements

<details>

<summary>

<p>Bump to .NET 9.0.0-preview.3</p>
<p>We thank the following contributors!</p>
<p>@alerickson, @tgauth, @step-security-bot, @xtqqczze</p>

</summary>

<ul>
<li>Fix PMC publish and the file path for msixbundle</li>
<li>Fix release version and stage issues in build and packaging</li>
<li>Add release tag if the environment variable is set</li>
<li>Update installation on Wix module (#23808)</li>
<li>Updates to package and release pipelines (#23800)</li>
<li>Update <code>PSResourceGet</code> to 1.0.5 (#23796)</li>
<li>Bump <code>actions/upload-artifact</code> from 4.3.2 to 4.3.3 (#21520)</li>
<li>Bump <code>actions/dependency-review-action</code> from 4.2.5 to 4.3.2 (#21560)</li>
<li>Bump <code>actions/checkout</code> from 4.1.2 to 4.1.5 (#21613)</li>
<li>Bump <code>github/codeql-action</code> from 3.25.1 to 3.25.4 (#22071)</li>
<li>Use feed with Microsoft Wix toolset (#21651) (Thanks @tgauth!)</li>
<li>Bump to .NET 9 preview 3 (#21782)</li>
<li>Use <code>PSScriptRoot</code> to find path to Wix module (#21611)</li>
<li>Create the <code>Windows.x64</code> global tool with shim for signing (#21559)</li>
<li>Update Wix package install (#21537) (Thanks @tgauth!)</li>
<li>Add branch counter variables for daily package builds (#21523)</li>
<li>Use correct signing certificates for RPM and DEBs (#21522)</li>
<li>Revert to version available on <code>Nuget</code> for <code>Microsoft.CodeAnalysis.Analyzers</code> (#21515)</li>
<li>Official PowerShell Package pipeline (#21504)</li>
<li>Add a PAT for fetching PMC cli (#21503)</li>
<li>Bump <code>ossf/scorecard-action</code> from 2.0.6 to 2.3.1 (#21485)</li>
<li>Apply security best practices (#21480) (Thanks @step-security-bot!)</li>
<li>Bump <code>Microsoft.CodeAnalysis.Analyzers</code> (#21449)</li>
<li>Fix package build to not check some files for a signature. (#21458)</li>
<li>Update PSResourceGet version from 1.0.2 to 1.0.4.1 (#21439) (Thanks @alerickson!)</li>
<li>Verify environment variable for OneBranch before we try to copy (#21441)</li>
<li>Add back two transitive dependency packages (#21415)</li>
<li>Multiple fixes in official build pipeline (#21408)</li>
<li>Update <code>PSReadLine</code> to <code>v2.3.5</code> (#21414)</li>
<li>PowerShell co-ordinated build OneBranch pipeline (#21364)</li>
<li>Add file description to <code>pwsh.exe</code> (#21352)</li>
<li>Suppress MacOS package manager output (#21244) (Thanks @xtqqczze!)</li>
<li>Update <code>metadata.json</code> and <code>README.md</code> (#21264)</li>
</ul>

</details>

### Documentation and Help Content

- Update the doc about how to build PowerShell (#21334) (Thanks @ForNeVeR!)
- Update the member lists for the Engine and Interactive-UX working groups (#20991) (Thanks @kilasuit!)
- Update CHANGELOG for `v7.2.19`, `v7.3.12` and `v7.4.2` (#21462)
- Fix grammar in `FAQ.md` (#21468) (Thanks @CodingGod987!)
- Fix typo in `SessionStateCmdletAPIs.cs` (#21413) (Thanks @eltociear!)
- Fix typo in a test (#21337) (Thanks @testwill!)
- Fix typo in `ast.cs` (#21350) (Thanks @eltociear!)
- Adding Working Group membership template (#21153)

[7.5.0-preview.3]: https://github.com/PowerShell/PowerShell/compare/v7.5.0-preview.2...v7.5.0-preview.3

## [7.5.0-preview.2] - 2024-02-22

### Engine Updates and Fixes

- Fix `using assembly` to use `Path.Combine` when constructing assembly paths (#21169)
- Validate the value for `using namespace` during semantic checks to prevent declaring invalid namespaces (#21162)

### General Cmdlet Updates and Fixes

- Add `WinGetCommandNotFound` and `CompletionPredictor` modules to track usage (#21040)
- `ConvertFrom-Json`: Add `-DateKind` parameter (#20925) (Thanks @jborean93!)
- Add tilde expansion for windows native executables (#20402) (Thanks @domsleee!)
- Add `DirectoryInfo` to the `OutputType` for `New-Item` (#21126) (Thanks @MartinGC94!)
- Fix `Get-Error` serialization of array values (#21085) (Thanks @jborean93!)

### Code Cleanup

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@eltociear</p>

</summary>

<ul>
<li>Fix a typo in <code>CoreAdapter.cs</code> (#21179) (Thanks @eltociear!)</li>
<li>Remove <code>PSScheduledJob</code> module source code (#21189)</li>
</ul>

</details>

### Tests

- Rewrite the mac syslog tests to make them less flaky (#21174)

### Build and Packaging Improvements

<details>

<summary>
<p>Bump to .NET 9 Preview 1</p>
<p>We thank the following contributors!</p>
<p>@gregsdennis</p>

</summary>

<ul>
<li>Bump to .NET 9 Preview 1 (#21229)</li>
<li>Add dotnet-runtime-9.0 as a dependency for the Mariner package</li>
<li>Add dotenv install as latest version does not work with current Ruby version (#21239)</li>
<li>Remove <code>surrogateFile</code> setting of APIScan (#21238)</li>
<li>Update experimental-feature json files (#21213)</li>
<li>Update to the latest NOTICES file (#21236)(#21177)</li>
<li>Update the cgmanifest (#21237)(#21093)</li>
<li>Update the cgmanifest (#21178)</li>
<li>Bump XunitXml.TestLogger from 3.1.17 to 3.1.20 (#21207)</li>
<li>Update versions of PSResourceGet (#21190)</li>
<li>Generate MSI for <code>win-arm64</code> installer (#20516)</li>
<li>Bump JsonSchema.Net to v5.5.1 (#21120) (Thanks @gregsdennis!)</li>
</ul>

</details>

### Documentation and Help Content

- Update `README.md` and `metadata.json` for v7.5.0-preview.1 release (#21094)
- Fix incorrect examples in XML docs in `PowerShell.cs` (#21173)
- Update WG members (#21091)
- Update changelog for v7.4.1 (#21098)

[7.5.0-preview.2]: https://github.com/PowerShell/PowerShell/compare/v7.5.0-preview.1...v7.5.0-preview.2

## [7.5.0-preview.1] - 2024-01-18

### Breaking Changes

- Fix `-OlderThan` and `-NewerThan` parameters for `Test-Path` when using `PathType` and date range (#20942) (Thanks @ArmaanMcleod!)
- Previously `-OlderThan` would be ignored if specified together
- Change `New-FileCatalog -CatalogVersion` default to 2 (#20428) (Thanks @ThomasNieto!)

### General Cmdlet Updates and Fixes

- Fix completion crash for the SCCM provider (#20815, #20919, #20915) (Thanks @MartinGC94!)
- Fix regression in `Get-Content` when `-Tail 0` and `-Wait` are used together (#20734) (Thanks @CarloToso!)
- Add `Aliases` to the properties shown up when formatting the help content of the parameter returned by `Get-Help` (#20994)
- Add implicit localization fallback to `Import-LocalizedData` (#19896) (Thanks @chrisdent-de!)
- Change `Test-FileCatalog` to use `File.OpenRead` to better handle the case where the file is being used (#20939) (Thanks @dxk3355!)
- Added `-Module` completion for `Save-Help` and `Update-Help` commands (#20678) (Thanks @ArmaanMcleod!)
- Add argument completer to `-Verb` for `Start-Process` (#20415) (Thanks @ArmaanMcleod!)
- Add argument completer to `-Scope` for `*-Variable`, `*-Alias` & `*-PSDrive` commands (#20451) (Thanks @ArmaanMcleod!)
- Add argument completer to `-Verb` for `Get-Verb` and `Get-Command` (#20286) (Thanks @ArmaanMcleod!)
- Fixing incorrect formatting string in `CommandSearcher` trace logging (#20928) (Thanks @powercode!)
- Ensure the filename is not null when logging WDAC ETW events (#20910) (Thanks @jborean93!)
- Fix four regressions introduced by the WDAC logging feature (#20913)
- Leave the input, output, and error handles unset when they are not redirected (#20853)
- Fix `Start-Process -PassThru` to make sure the `ExitCode` property is accessible for the returned `Process` object (#20749) (Thanks @CodeCyclone!)
- Fix `Group-Object` output using interpolated strings (#20745) (Thanks @mawosoft!)
- Fix rendering of `DisplayRoot` for network `PSDrive` (#20793)
- Fix `Invoke-WebRequest` to report correct size when `-Resume` is specified (#20207) (Thanks @LNKLEO!)
- Add `PSAdapter` and `ConsoleGuiTools` to module load telemetry allow list (#20641)
- Fix Web Cmdlets to allow `WinForm` apps to work correctly (#20606)
- Block getting help from network locations in restricted remoting sessions (#20593)
- Fix `Group-Object` to use current culture for its output (#20608)
- Add argument completer to `-Version` for `Set-StrictMode` (#20554) (Thanks @ArmaanMcleod!)
- Fix `Copy-Item` progress to only show completed when all files are copied (#20517)
- Fix UNC path completion regression (#20419) (Thanks @MartinGC94!)
- Add telemetry to check for specific tags when importing a module (#20371)
- Report error if invalid `-ExecutionPolicy` is passed to `pwsh` (#20460)
- Add `HelpUri` to `Remove-Service` (#20476)
- Fix `unixmode` to handle `setuid` and `sticky` when file is not an executable (#20366)
- Fix `Test-Connection` due to .NET 8 changes (#20369)
- Fix implicit remoting proxy cmdlets to act on common parameters (#20367)
- Set experimental features to stable for 7.4 release (#20285)
- Revert changes to continue using `BinaryFormatter` for `Out-GridView` (#20300)
- Fix `Get-Service` non-terminating error message to include category (#20276)
- Prevent `Export-CSV` from flushing with every input (#20282) (Thanks @Chris--A!)
- Fix a regression in DSC (#20268)
- Include the module version in error messages when module is not found (#20144) (Thanks @ArmaanMcleod!)
- Add `-Empty` and `-InputObject` parameters to `New-Guid` (#20014) (Thanks @CarloToso!)
- Remove the comment trigger from feedback provider (#20136)
- Prevent fallback to file completion when tab completing type names (#20084) (Thanks @MartinGC94!)
- Add the alias `r` to the parameter `-Recurse` for the `Get-ChildItem` command (#20100) (Thanks @kilasuit!)

### Code Cleanup

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@eltociear, @ImportTaste, @ThomasNieto, @0o001</p>

</summary>

<ul>
<li>Fix typos in the code base (#20147, #20492, #20632, #21015, #20838) (Thanks @eltociear!)</li>
<li>Add the missing alias <code>LP</code> to <code>-LiteralPath</code> for some cmdlets (#20820) (Thanks @ImportTaste!)</li>
<li>Remove parenthesis for empty attribute parameters (#20087) (Thanks @ThomasNieto!)</li>
<li>Add space around keyword according to the <code>CodeFactor</code> rule (#20090) (Thanks @ThomasNieto!)</li>
<li>Remove blank lines as instructed by <code>CodeFactor</code> rules (#20086) (Thanks @ThomasNieto!)</li>
<li>Remove trailing whitespace (#20085) (Thanks @ThomasNieto!)</li>
<li>Fix typo in error message (#20145) (Thanks @0o001!)</li>
</ul>

</details>

### Tools

- Make sure feedback link in the bot's comment is clickable (#20878) (Thanks @floh96!)
- Fix bot so anyone who comments will remove the "Resolution-No Activity" label (#20788)
- Fix bot configuration to prevent multiple comments about "no activity" (#20758)
- Add bot logic for closing GitHub issues after 6 months of "no activity" (#20525)
- Refactor bot for easier use and updating (#20805)
- Configure bot to add survey comment for closed issues (#20397)

### Tests

- Suppress error output from `Set-Location` tests (#20499)
- Fix typo in `FileCatalog.Tests.ps1` (#20329) (Thanks @eltociear!)
- Continue to improve tests for release automation (#20182)
- Skip the test on x86 as `InstallDate` is not visible on `Wow64` (#20165)
- Harden some problematic release tests (#20155)

### Build and Packaging Improvements

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@alerickson, @Zhoneym, @0o001</p>

</summary>

<ul>
<li>Bump .NET SDK to 8.0.101 (#21084)</li>
<li>Update the cgmanifest (#20083, #20436, #20523, #20560, #20627, #20764, #20906, #20933, #20955, #21047)</li>
<li>Update to the latest NOTICES file (#20074, #20161, #20385, #20453, #20576, #20590, #20880, #20905)</li>
<li>Bump StyleCop.Analyzers from 1.2.0-beta.507 to 1.2.0-beta.556 (#20953)</li>
<li>Bump xUnit to 2.6.6 (#21071)</li>
<li>Bump JsonSchema.Net to 5.5.0 (#21027)</li>
<li>Fix failures in GitHub action <code>markdown-link-check</code> (#20996)</li>
<li>Bump xunit.runner.visualstudio to 2.5.6 (#20966)</li>
<li>Bump github/codeql-action from 2 to 3 (#20927)</li>
<li>Bump Markdig.Signed to 0.34.0 (#20926)</li>
<li>Bump Microsoft.ApplicationInsights from 2.21.0 to 2.22.0 (#20888)</li>
<li>Bump Microsoft.NET.Test.Sdk to 17.8.0 (#20660)</li>
<li>Update <code>apiscan.yml</code> to have access to the <code>AzDevOpsArtifacts</code> variable group (#20671)</li>
<li>Set the <code>ollForwardOnNoCandidateFx</code> in <code>runtimeconfig.json</code> to roll forward only on minor and patch versions (#20689)</li>
<li>Sign the global tool shim executable (#20794)</li>
<li>Bump actions/github-script from 6 to 7 (#20682)</li>
<li>Remove RHEL7 publishing to packages.microsoft.com as it's no longer supported (#20849)</li>
<li>Bump Microsoft.CodeAnalysis.CSharp to 4.8.0 (#20751)</li>
<li>Add internal nuget feed to compliance build (#20669)</li>
<li>Copy azure blob with PowerShell global tool to private blob and move to CDN during release (#20659)</li>
<li>Fix release build by making the internal SDK parameter optional (#20658)</li>
<li>Update PSResourceGet version to 1.0.1 (#20652)</li>
<li>Make internal .NET SDK URL as a parameter for release builld (#20655)</li>
<li>Fix setting of variable to consume internal SDK source (#20644)</li>
<li>Bump Microsoft.Management.Infrastructure to v3.0.0 (#20642)</li>
<li>Bump Microsoft.PowerShell.Native to v7.4.0 (#20617)</li>
<li>Bump Microsoft.Security.Extensions from 1.2.0 to 1.3.0 (#20556)</li>
<li>Fix package version for .NET nuget packages (#20551)</li>
<li>Add SBOM for release pipeline (#20519)</li>
<li>Block any preview vPack release (#20243)</li>
<li>Only registry App Path for release package (#20478)</li>
<li>Increase timeout when publishing packages to <code>pacakages.microsoft.com</code> (#20470)</li>
<li>Fix alpine tar package name and do not crossgen alpine fxdependent package (#20459)</li>
<li>Bump PSReadLine from 2.2.6 to 2.3.4 (#20305)</li>
<li>Remove the <code>ref</code> folder before running compliance (#20373)</li>
<li>Updates RIDs used to generate component Inventory (#20370)</li>
<li>Bump XunitXml.TestLogger from 3.1.11 to 3.1.17 (#20293)</li>
<li>Update experimental-feature json files (#20335)</li>
<li>Use <code>fxdependent-win-desktop</code> runtime for compliance runs (#20326)</li>
<li>Release build: Change the names of the PATs (#20307)</li>
<li>Add mapping for mariner arm64 stable (#20213)</li>
<li>Put the calls to <code>Set-AzDoProjectInfo</code> and <code>Set-AzDoAuthToken</code> in the right order (#20306)</li>
<li>Enable vPack provenance data (#20220)</li>
<li>Bump actions/checkout from 3 to 4 (#20205)</li>
<li>Start using new <code>packages.microsoft.com</code> cli (#20140, #20141)</li>
<li>Add mariner arm64 to PMC release (#20176)</li>
<li>Fix typo <code>donet</code> to <code>dotnet</code> in build scripts and pipelines (#20122) (Thanks @0o001!)</li>
<li>Install the pmc cli</li>
<li>Add skip publish parameter</li>
<li>Add verbose to clone</li>
</ul>

</details>

### Documentation and Help Content

- Include information about upgrading in readme (#20993)
- Expand "iff" to "if-and-only-if" in XML doc content (#20852)
- Update LTS links in README.md to point to the v7.4 packages (#20839) (Thanks @kilasuit!)
- Update `README.md` to improve readability (#20553) (Thanks @AnkitaSikdar005!)
- Fix link in `docs/community/governance.md` (#20515) (Thanks @suravshresth!)
- Update `ADOPTERS.md` (#20555) (Thanks @AnkitaSikdar005!)
- Fix a typo in `ADOPTERS.md` (#20504, #20520) (Thanks @shruti-sen2004!)
- Correct grammatical errors in `README.md` (#20509) (Thanks @alienishi!)
- Add 7.3 changelog URL to readme (#20473) (Thanks @Saibamen!)
- Clarify some comments and documentation (#20462) (Thanks @darkstar!)

[7.5.0-preview.1]: https://github.com/PowerShell/PowerShell/compare/v7.4.1...v7.5.0-preview.1

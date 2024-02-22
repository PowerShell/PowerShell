# Preview Changelog

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
- Add `PSAdapter` and `ConsoleGuiTools` to module load telemetry whitelist (#20641)
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

- Include information about upgrading in README (#20993)
- Expand "iff" to "if-and-only-if" in XML doc content (#20852)
- Update LTS links in README.md to point to the v7.4 packages (#20839) (Thanks @kilasuit!)
- Update `README.md` to improve readability (#20553) (Thanks @AnkitaSikdar005!)
- Fix link in `docs/community/governance.md` (#20515) (Thanks @suravshresth!)
- Update `ADOPTERS.md` (#20555) (Thanks @AnkitaSikdar005!)
- Fix a typo in `ADOPTERS.md` (#20504, #20520) (Thanks @shruti-sen2004!)
- Correct grammatical errors in `README.md` (#20509) (Thanks @alienishi!)
- Add 7.3 changelog URL to Readme (#20473) (Thanks @Saibamen!)
- Clarify some comments and documentation (#20462) (Thanks @darkstar!)

[7.5.0-preview.1]: https://github.com/PowerShell/PowerShell/compare/v7.4.1...v7.5.0-preview.1

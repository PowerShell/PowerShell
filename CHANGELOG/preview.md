# Preview Changelog

## [7.7.0-preview.3]

[7.7.0-preview.3]: https://github.com/PowerShell/PowerShell/compare/v7.7.0-preview.2...v7.7.0-preview.3

## [7.7.0-preview.2]

### Engine Updates and Fixes

- Update `MaxVisitCount` and `MaxHashtableKeyCount` if `VisitorSafeValueContext` indicates `SkipLimitCheck` is true (#27306)
- Enable usage in AppContainers (#27266)

### General Cmdlet Updates and Fixes

- Handle empty-string and null-value results returned from custom argument completer more properly (#27398)
- Add missing resource strings for `Get-WinEvent` (#27397) (Thanks @MartinGC94!)
- Improve `Get-WinEvent -ListLog` exception handling (#27395) (Thanks @MartinGC94!)
- Update PowerShell telemetry to respect the diagnostics and feedback setting on Windows (#27328)

### Code Cleanup

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@xtqqczze</p>

</summary>

<ul>
<li>Remove eager initialization of <code>_startupScripts</code> to enable lazy thread-safe initialization (#25767) (Thanks @xtqqczze!)</li>
<li>Fix <code>IDE0049</code> in <code>System.Management.Automation</code> [Part 4] (#27380) (Thanks @xtqqczze!)</li>
<li>Fix <code>IDE0049</code> in <code>System.Management.Automation</code> [Part 3] (#27379) (Thanks @xtqqczze!)</li>
<li>Fix <code>IDE0049</code> in <code>System.Management.Automation</code> [Part 2] (#27378) (Thanks @xtqqczze!)</li>
</ul>

</details>

### Tools

- Add an instruction file to ensure the Copyright header is present at the start of script and module files (#27408)

### Build and Packaging Improvements

<details>

<summary>

<p>Update to .NET SDK 11.0.100-preview.4</p>

</summary>

<ul>
<li>Update branch to use the .NET 11 SDK 11.0.100-preview.4 (#27504)</li>
<li>Update <code>metadata.json</code> for the servicing releases (#27488)</li>
<li>Update CHANGELOG for v7.4.16, v7.5.7, and v7.6.2 releases (#27494)</li>
<li>Remove unused step that clones <code>Internal-PowerShellTeam-Tools</code> repo in PMC publish pipeline (#27495)</li>
<li>Update Microsoft.PowerShell.PSResourceGet version to 1.3.0-preview1 (#27487)</li>
<li>Verify Apple codesign immediately after ESRP signing (#27486) (Thanks @andyleejordan!)</li>
<li>Add <code>appLicensing</code> capability to Appx manifest to allow it to run without acquiring a Store license (#27412)</li>
<li>Bump actions/dependency-review-action from 4.9.0 to 5.0.0 (#27411)</li>
<li>Bump github/codeql-action from 4.35.3 to 4.35.4 (#27404)</li>
<li>Specify <code>linux-arm64</code> runtime if package type is <code>deb-arm64</code> in <code>packaging.psm1</code> (#27401)</li>
<li>Bump github/codeql-action from 4.35.1 to 4.35.3 (#27394)</li>
<li>Update <code>Microsoft.PowerShell.Native</code> to the latest GA version (#27400)</li>
<li>Update the <code>MSIXBundle-VPack</code> pipeline to create VPack for both LTS and Stable channel packages (#27384)</li>
<li>Create PowerShell package for arm debian distribution (#26925)</li>
<li>Merge release/v7.7.0-preview.1 into master (#27374)</li>
<li>Update <code>metadata.json</code> for the new servicing and preview releases (#27307)</li>
<li>Fix changelog grab failure when only one header exists. (#27371)</li>
<li>Remove mariner2.0 from PMC mapping (#27068)</li>
<li>Download PMC Packages through <code>TemplateContext</code> (#27326)</li>
<li>Correct Variable Template Reference in NonOfficial Pipeline Templates (#27275)</li>
<li>PMC release: Use slash instead of back-slash for Linux container (#27315)</li>
</ul>

</details>

### Documentation and Help Content

- Update `README.md` to call out `PowerShell.Core.Instrumentation` needs to be in sync between `PowerShell` and `PowerShell-Native` repos (#27399)
- Update changelog for the v7.5.6 release (#27320)
- Update CHANGELOG for v7.4.15 (#27314)
- Update Changelog for release v7.6.1 (#27304)

[7.7.0-preview.2]: https://github.com/PowerShell/PowerShell/compare/v7.7.0-preview.1...v7.7.0-preview.2

## [7.7.0-preview.1]

### Breaking Changes

- Add `ValidateNotNullOrEmpty` attribute to the `-Property` of `Format-Table/List/Custom` (#26552)
- Fix to use accurate message for validating a string argument is not null and not an empty string (#26668)
- Correct handling of explicit `-[Operator]:$false` parameter values in `Where-Object` (#26485) (Thanks @yotsuda!)

### Engine Updates and Fixes

- Update `MaxVisitCount` and `MaxHashtableKeyCount` if `VisitorSafeValueContext` indicates `SkipLimitCheck` is true
(#27308)
- Enable usage in AppContainers (#27305)
- Delay update notification for one week to ensure all packages become available (#27095)
- Fix up default value for parameters with the `in` modifier (#26785) (Thanks @jborean93!)
- Fix `WSManInstance` COM interface with `ResourceURI` (#26692) (Thanks @jborean93!)
- Refactor the module path construction code to make it more robust and easier to maintain (#26565)
- Fix checks for local user config file paths (#26269)

### General Cmdlet Updates and Fixes

- Add verbose message to `Get-Service` when properties cannot be returned (#27109) (Thanks @reabr!)
- Fix `Remove-Item` confirmation message to use provider path instead (#27123) (Thanks @scuzqy!)
- PSStyle: validate background index against `BackgroundColorMap` (#27106) (Thanks @cuiweixie!)
- Update PowerShell Profile DSC resource manifests to allow null for content (#26929)
- Add `SubjectAlternativeName` property to the `Signature` object returned from `Get-AuthenticodeSignature` (#26252)
- Mark `-NoTypeInformation` as obsolete no-op and evaluate `-IncludeTypeInformation` on by value on Csv cmdlets (#26719) (Thanks @yotsuda!)
- Support `TargetObject` position in `ParserErrors` (#26649) (Thanks @jborean93!)
- Fix the CLR internal error and null ref exception when running `show-command` with PowerShell API (#26669)
- Fix `Test-Json` false positive errors when using `oneOf` or `anyOf` in schema (#26618) (Thanks @yotsuda!)
- Add `ToRegex` method to `WildcardPattern` class (#26515) (Thanks @yotsuda!)
- Add `-ExcludeProperty` parameter to `Format-*` cmdlets (#26514) (Thanks @yotsuda!)
- Fix NOTES section formatting in comment-based help (#26512) (Thanks @yotsuda!)
- Disable AMSI content logging in release (#26235) (Thanks @xtqqczze!)
- Add tab completion for `$PSBoundParameters.Keys` switch cases and access patterns (#26483) (Thanks @yotsuda!)
- Fix formatting to properly handle the `Reset` VT sequences that appear in the middle of a string (#26424)
- Add `-Extension` parameter to `Join-Path` cmdlet (#26482) (Thanks @yotsuda!)
- Make `Export-Csv` `-Append` and `-NoHeader` mutually exclusive (#26472) (Thanks @yotsuda!)
- Respect `-Qualifier/-NoQualifier/-Leaf/-IsAbsolute:$false` in `Split-Path` (#26474) (Thanks @yotsuda!)
- Respect `-UseWindowsPowerShell:$false` in `New-PSSession` (#26469) (Thanks @yotsuda!)
- Respect `-Repeat/-MtuSize/-Traceroute:$false` in `Test-Connection` (#26479) (Thanks @yotsuda!)
- Fix `Invoke-RestMethod` to support read-only files in multipart form data (#26454) (Thanks @yotsuda!)
- Respect `-ListAvailable:$false` in `Get-TimeZone` (#26463) (Thanks @yotsuda!)
- Respect `-Shuffle:$false` in `Get-SecureRandom` (#26460) (Thanks @yotsuda!)
- Respect `-Shuffle:$false` in `Get-Random` (#26457) (Thanks @yotsuda!)
- DSC v3 resource for Powershell Profile (#26157)
- Make the experimental feature `PSFeedbackProvider` stable (#26343)
- Make some experimental features stable (#26348)
- Add `PSApplicationOutputEncoding` variable (#21219) (Thanks @jborean93!)
- Dynamically evaluate width of `LastWriteTime` for formatting output on Unix (#24624) (Thanks @MathiasMagnus!)
- Handle null reference exception in CsvCommands.cs: `ConvertPSObjectToCSV` (#26144) (Thanks @mikkas456!)
- Improve `ValidateLength` error message consistency and refactor validation tests (#25806) (Thanks @jorgeasaurus!)
- Correct handling of explicit `-Since:$false` parameter value in `Get-Uptime` (#26141) (Thanks @logiclrd!)
- Add property and event for debug attach (#25788) (Thanks @jborean93!)
- Fix memory leak in `GetFileShares` (#25896) (Thanks @xtqqczze!)
- Correct handling of explicit `-Empty:$false` parameter value in `New-Guid` (#26140) (Thanks @logiclrd!)

### Code Cleanup

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@xtqqczze, @yotsuda, @ThioJoe, @rwp0, @amritanand-py</p>

</summary>

<ul>
<li>Fix <code>IDisposable</code> implementation in sealed classes (#26215) (Thanks @xtqqczze!)</li>
<li>Enable CA1852: Seal internal types (#25890) (Thanks @xtqqczze!)</li>
<li>Remove obsolete <code>CA2006</code> rule suppression (#25939) (Thanks @xtqqczze!)</li>
<li>Use consistent indentation in the file <code>HelpersCommon.psm1</code> (#26608)</li>
<li>Centralize <code>ExcludeProperty</code> filter application in <code>ViewGenerator</code> base class (#26574) (Thanks @yotsuda!)</li>
<li>Refactor <code>IsComputerNameValid</code> character validation (#26274) (Thanks @xtqqczze!)</li>
<li>Remove obsolete test/docker/networktest directory (#26388)</li>
<li>Avoid regex for exact word matching in <code>DscClassCache</code> (#26306) (Thanks @xtqqczze!)</li>
<li>Enable analyzers: Use char overload (#26301) (Thanks @xtqqczze!)</li>
<li>Enable CA1200: Avoid using cref tags with a prefix (#26298) (Thanks @xtqqczze!)</li>
<li>Remove unused timeout variable from <code>RemoteHyperVTests</code> class (#26297) (Thanks @xtqqczze!)</li>
<li>Enable CA2022: Avoid inexact read with <code>Stream.Read</code> (#25814) (Thanks @xtqqczze!)</li>
<li>Fix a few simple typos in comments and string outputs (#25805) (Thanks @ThioJoe!)</li>
<li>Remove unused Azure Devops windows CI workflows (#26245)</li>
<li>Fix CA1837: Use <code>Environment.ProcessId</code> (#26242) (Thanks @xtqqczze!)</li>
<li>Enable IDE0080: RemoveConfusingSuppressionForIsExpression (#26206) (Thanks @xtqqczze!)</li>
<li>Remove redundant <code>CharSet</code> from <code>StructLayout</code> attributes. Part 1 (#26216) (Thanks @xtqqczze!)</li>
<li>Fix IDE0083: UseNotPattern (#26213) (Thanks @xtqqczze!)</li>
<li>Fix <code>IDE0049</code> for <code>string</code> in <code>System.Management.Automation</code> (#25921) (Thanks @xtqqczze!)</li>
<li>Fix <code>IDE0049</code> for <code>object</code> in <code>System.Management.Automation</code>. Part 1 (#25923) (Thanks @xtqqczze!)</li>
<li>Replace stackallocs with collection expressions (#25803) (Thanks @xtqqczze!)</li>
<li>Capitalize Windows in <code>PSNativeWindowsTildeExpansion</code> experimental feature description (#25266) (Thanks @rwp0!)</li>
<li>Fix <code>SA1028</code>: Code should not contain trailing whitespace. Part 1. (#26203) (Thanks @xtqqczze!)</li>
<li>Fix IDE0083: UseNotPattern (#26209) (Thanks @xtqqczze!)</li>
<li>Fix CA1852: Seal internal types. Part 1 (#26205) (Thanks @xtqqczze!)</li>
<li>Enable IDE0019: InlineAsTypeCheck (#25920) (Thanks @xtqqczze!)</li>
<li>Fix mismatched indentation in <code>.config/suppress.json</code> (#26192) (Thanks @xtqqczze!)</li>
<li>Replace custom method with <code>File.ReadAllText()</code> in ScriptAnalysis.cs (#26060) (Thanks @amritanand-py!)</li>
<li>Avoid possible multiple enumerations in <code>ImportModuleCommand.IsPs1xmlFileHelper_IsPresentInEntries</code> (#26104) (Thanks @xtqqczze!)</li>
<li>Enable <code>SA1206</code>: Declaration keywords should follow order (#24973) (Thanks @xtqqczze!)</li>
<li>Disable IDE0049: PreferBuiltInOrFrameworkType (#26094) (Thanks @xtqqczze!)</li>
<li>Enable CA1853: Unnecessary call to <code>Dictionary.ContainsKey(key)</code> (#26106) (Thanks @xtqqczze!)</li>
<li>Enable CA1860: Avoid using <code>Enumerable.Any()</code> extension method (#26109) (Thanks @xtqqczze!)</li>
<li>Enable CA1858: Use <code>StartsWith</code> instead of <code>IndexOf</code> (#26107) (Thanks @xtqqczze!)</li>
<li>Add <code>CodeQL</code> suppressions for <code>NativeCommandProcessor</code> (#26729)</li>
</ul>

</details>

### Tools

- Add GitOps policy to auto-label backport candidates when CL-BuildPackaging is added (#26881)
- Add Pester CI Analysis Skill (#26806)
- Delete unused winget release script (#26683)
- Improve error message from `Start-NativeExecution` (#26500) (Thanks @logiclrd!)
- Add default CODEOWNERS entry for maintainers (#26660)
- Add Attack Surface Analyzer Script (#26379)
- Add merge conflict marker detection to linux-ci workflow and refactor existing actions to use reusable get-changed-files action (#26350)
- Add reusable get-changed-files action and refactor existing actions (#26355)
- Refactor analyze job to reusable workflow and enable on Windows CI (#26322)
- Create github copilot setup workflow (#26285)
- Update dependabot.yml to monitor release/* branches (#26251)

### Tests

- Fix the `PSNativeCommandArgumentPassing` test (#27057)
- Fix `Import-Module.Tests.ps1` to handle Arm32 platform (#26862)
- Add comprehensive PowerShell class tests for `ConvertTo-Json` (#26769) (Thanks @yotsuda!)
- Add comprehensive `PSCustomObject` tests for `ConvertTo-Json` (#26743) (Thanks @yotsuda!)
- Add GitHub Actions annotations for Pester test failures (#26789)
- Add comprehensive depth and multilevel composition tests for `ConvertTo-Json` (#26744) (Thanks @yotsuda!)
- Add comprehensive array and dictionary tests for `ConvertTo-Json` (#26742) (Thanks @yotsuda!)
- Add comprehensive scalar type tests for `ConvertTo-Json` (#26736) (Thanks @yotsuda!)
- Fix the fuzzy test (#26402)
- Add Fuzz Tests (#26384)
- Fix merge conflict checker for empty file lists and filter *.cs files (#26365)
- Fix linux_packaging job being skipped when only packaging files change (#26315)
- Use `[initialsessionstate]` type accelerator (#25912) (Thanks @xtqqczze!)
- Add markdown link verification for PRs (#26219)
- Check for `GetWindowPlacement` success (#26122) (Thanks @xtqqczze!)

### Build and Packaging Improvements

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@powercode, @kasperk81, @xtqqczze</p>

</summary>

<ul>
<li>Update branch for release (#27291)</li>
<li>Remove package verification from the notice pipeline (#27289)</li>
<li>Remove MSI from publishing pipeline (#27213)</li>
<li>Externalize <code>findMissingNotices</code> target framework selection with ordered Windows fallback (#27269)</li>
<li>Fix the package pipeline by adding in PDP-Media directory (#27254)</li>
<li>Bump actions/checkout from 4 to 6.0.2 (#27206)</li>
<li>Build, package, and create VPack for the PowerShell-LTS store package within the same <code>msixbundle-vpack</code> pipeline (#150) (#27209)</li>
<li>Pin ready-to-merge.yml reusable workflow to commit SHA (#27204)</li>
<li>Change the display name of <code>PowerShell-LTS</code> MSIX package to &quot;PowerShell LTS&quot; (#27203)</li>
<li>[StepSecurity] ci: Harden GitHub Actions (#27201)</li>
<li>[StepSecurity] ci: Harden GitHub Actions (#27202)</li>
<li>Redo windows image fix to use latest image (#27198)</li>
<li>Separate Store Package Creation, Skip Polling for Store Publish, Clean up PDP-Media (#27024)</li>
<li>Revert &quot;Fetch latest ICU release version dynamically&quot; (#27127)</li>
<li>Update package references and move to .NET SDK 11.0-preview.2 (#27117)</li>
<li>Add comment-based help documentation to <code>build.psm1</code> functions (#27122) (Thanks @powercode!)</li>
<li>Bump github/codeql-action from 3.30.3 to 4.35.1 (#27120)</li>
<li>Select New MSIX Package Name (#27096)</li>
<li>Separate Official and NonOfficial templates for ADO pipelines (#26897)</li>
<li>Update the <code>PhoneProductId</code> to be the official LTS id used by Store (#27077)</li>
<li>release-upload-buildinfo: replace version-comparison channel gating with metadata flags (#27074)</li>
<li>Update build to create two msix's and msixbundles for LTS and Stable (#27056)</li>
<li>Update <code>metadata.json</code> for the v7.6.0 release (#27054)</li>
<li>Move <code>_GetDependencies</code> MSBuild target from dynamic generation in <code>build.psm1</code> into <code>Microsoft.PowerShell.SDK.csproj</code> (#27052)</li>
<li>Fix PMC repo URL for RHEL10  (#27059)</li>
<li>Create Linux LTS deb/rpm packages for LTS releases (#27049)</li>
<li>Create LTS pkg and non-LTS pkg for macOS for LTS releases (#27039)</li>
<li>Fix the container image for vPack, MSIX vPack and Package pipelines (#27015)</li>
<li>Update <code>Microsoft.PowerShell.PSResourceGet</code> version to 1.2.0 (#27003)</li>
<li>Fix ConvertFrom-ClearlyDefinedCoordinates to handle API object coordinates (#26893)</li>
<li>Bump actions/upload-artifact from 4 to 7 (#26914)</li>
<li>Bump actions/dependency-review-action from 4.7.3 to 4.9.0 (#26938)</li>

<li>Hardcode Official templates (#26928)</li>
<li>Add PMC packages for debian13 and rhel10 (#26912)</li>
<li>Split TPN manifest and Component Governance manifest (#26891)</li>
<li>Add version in description and pass store task on failure (#26885)</li>
<li>Correct the package name for .deb and .rpm packages (#26877)</li>
<li>Fix a preview detection test for the packaging script (#26882)</li>
<li>Exclude .exe packages from publishing to GitHub (#26859)</li>
<li>Update metadata.json for v7.6.0-rc.1 (#26856)</li>
<li>Fetch latest ICU release version dynamically (#26827) (Thanks @kasperk81!)</li>
<li>Update <code>LangVersion</code> to <code>preview</code> (#26214) (Thanks @xtqqczze!)</li>
<li>Update to .NET 11 SDK and update dependencies (#26783)</li>
<li>Update outdated package references (#26771)</li>
<li>Create es-metadata (#26759)</li>
<li>Add policy to restrict the <code>Approved-LowRisk</code> label (#26728)</li>
<li>Move PowerShell build to depend on .NET SDK 10.0.102 (#26697)</li>
<li>Update metadata.json to update the Latest attribute with a better name (#26380)</li>
<li>Update outdated package references (#26656)</li>
<li>Bring release changes from the v7.6.0-preview.6 release branch (#26627)</li>
<li>Update build to use .NET SDK 10.0.100 (#26448)</li>
<li>Update the macos package name for preview releases to match the previous pattern (#26429)</li>
<li>Fix condition syntax for StoreBroker package tasks in MSIX pipeline (#26427)</li>
<li>Fix template path for rebuild branch check in package.yml (#26425)</li>
<li>Update the WCF packages to the latest version that is compatible with v4.10.3 (#26406)</li>
<li>Add rebuild branch support with conditional MSIX signing (#26415)</li>
<li>Optimize/split windows package signing (#26403)</li>
<li>Improve ADO package build and validation across platforms (#26398)</li>
<li>Update outdated test package references (#26368)</li>
<li>Delete this way of collecting feedback (#26364)</li>
<li>Update the <code>Microsoft.PowerShell.Native</code> package version (#26347)</li>
<li>Add log grouping to build.psm1 for collapsible GitHub Actions logs (#26326)</li>
<li>Bump actions/setup-dotnet from 4 to 5 (#26327)</li>
<li>Update SDK to 10.0.100-rc.2.25502.107 (#26305)</li>
<li>Replace <code>fpm</code> with <code>dpkg-deb</code> for DEB package generation (#26281)</li>
<li>Replace fpm with native macOS packaging tools (pkgbuild/productbuild) (#26268)</li>
<li>Separate Store Automation Service Endpoints, Resolve AppID (#26210)</li>
<li>Update concurrency groups to prevent merge runs and pull request runs from canceling each other (#26257)</li>
<li>Update release tags to version 7.5.4 and 7.4.13 (#26258)</li>
<li>Update outdated package references (#26148)</li>
<li>Refactor: Centralize xUnit tests into reusable workflow and remove legacy verification (#26243)</li>
<li>Convert Azure DevOps Linux Packaging pipeline to GitHub Actions workflow (#26225)</li>
<li>Update vPack name (#26090)</li>
<li>Update <code>metadata.json</code> for v7.6.0-preview.5 release (#26158)</li>
<li>Bump ossf/scorecard-action from 2.4.2 to 2.4.3 (#26128)</li>
</ul>

</details>

### Documentation and Help Content

- Check in `7.6.md` after v7.6.0 release (#27063)
- Update changelog for release v7.5.5 (#27014)
- Add 7.4.14 changelog (#26998)
- Update `SECURITY.md` to remove email reporting option (#26653)
- Update changelog for the release v7.6.0-preview.6 (#26597)
- Explain the parameter `-UseNuGetOrg` in build documentation (#26507) (Thanks @logiclrd!)
- Update backport prompt (#26392)
- Add a backport prompt for copilot (#26383)
- Update `linux.md` documentation to reflect current CI build configuration (#26255)
- Add GitHub Copilot instruction files for PowerShell CI build system (#26253)
- Add documentation for publishing Pester test results in GitHub Actions (#26254)
- Remove Gitter from README (#26200) (Thanks @xtqqczze!)
- Remove nightly build status section from README.md (#26227) (Thanks @xtqqczze!)
- Update changelog for v7.5.4 and v7.4.13 (#26202)

[7.7.0-preview.1]: https://github.com/PowerShell/PowerShell/compare/v7.6.0-rc.1...v7.7.0-preview.1

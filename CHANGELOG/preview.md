# Current preview release

## [7.2.0-preview.4] - 2021-03-16

### Breaking Changes

- Fix `Get-Date -UFormat` `%G` and `%g` behavior (#14555) (Thanks @brianary!)

### Engine Updates and Fixes

- Update engine script signature validation to match `Get-AuthenticodeSignature` logic (#14849)
- Avoid array allocations from `GetDirectories` and `GetFiles` (#14327) (Thanks @xtqqczze!)

### General Cmdlet Updates and Fixes

- Add `UseOSCIndicator` setting to enable progress indicator in terminal (#14927)
- Re-enable VT mode on Windows after running command in `ConsoleHost` (#14413)
- Fix `Move-Item` for `FileSystemProvider` to use copy-delete instead of move for DFS paths (#14913)
- Fix `PromptForCredential()` to add `targetName` as domain (#14504)
- Update `Concise` `ErrorView` to not show line information for errors from script module functions (#14912)
- Remove the 32,767 character limit on the environment block for `Start-Process` (#14111) (Thanks @hbuckle!)
- Don't write possible secrets to verbose stream for web cmdlets (#14788)

### Tools

- Update `dependabot` configuration to V2 format (#14882)
- Add tooling issue slots in PR template (#14697)

### Tests

- Move misplaced test file to tests directory (#14908) (Thanks @MarianoAlipi!)
- Refactor MSI CI (#14753)

### Build and Packaging Improvements

<details>

<summary>
Update .NET to version <code>6.0.100-preview.2.21155.3</code>
</summary>

<ul>
<li>Update .NET to version <code>6.0.100-preview.2.21155.3</code> (#15007)</li>
<li>Bump <code>Microsoft.PowerShell.Native</code> to <code>7.2.0-preview.1</code> (#15030)</li>
<li>Create MSIX Bundle package in release pipeline (#14982)</li>
<li>Build self-contained minimal size package for Guest Config team (#14976)</li>
<li>Bump XunitXml.TestLogger from 3.0.62 to 3.0.66 (#14993) (Thanks @dependabot[bot]!)</li>
<li>Enable building PowerShell for Apple M1 runtime (#14923)</li>
<li>Fix the variable name in the condition for miscellaneous analysis CI (#14975)</li>
<li>Fix the variable usage in CI yaml (#14974)</li>
<li>Disable running markdown link verification in release build CI (#14971)</li>
<li>Bump Microsoft.CodeAnalysis.CSharp from 3.9.0-3.final to 3.9.0 (#14934) (Thanks @dependabot[bot]!)</li>
<li>Declare which variable group is used for checking the blob in the release build (#14970)</li>
<li>Update metadata and script to enable consuming .NET daily builds (#14940)</li>
<li>Bump NJsonSchema from 10.3.9 to 10.3.10 (#14933) (Thanks @dependabot[bot]!)</li>
<li>Use template that disables component governance for CI (#14938)</li>
<li>Add suppress for nuget multi-feed warning (#14893)</li>
<li>Bump NJsonSchema from 10.3.8 to 10.3.9 (#14926) (Thanks @dependabot[bot]!)</li>
<li>Add exe wrapper to release (#14881)</li>
<li>Bump Microsoft.ApplicationInsights from 2.16.0 to 2.17.0 (#14847)</li>
<li>Bump Microsoft.NET.Test.Sdk from 16.8.3 to 16.9.1 (#14895) (Thanks @dependabot[bot]!)</li>
<li>Bump NJsonSchema from 10.3.7 to 10.3.8 (#14896) (Thanks @dependabot[bot]!)</li>
<li>Disable codesign validation where the file type is not supported (#14885)</li>
<li>Fixing broken Experimental Feature list in <code>powershell.config.json</code> (#14858)</li>
<li>Bump NJsonSchema from 10.3.6 to 10.3.7 (#14855)</li>
<li>Add exe wrapper for Microsoft Update scenarios (#14737)</li>
<li>Install wget on <code>CentOS</code> 7 docker image (#14857)</li>
<li>Fix install-dotnet download (#14856)</li>
<li>Fix Bootstrap step in Windows daily test runs (#14820)</li>
<li>Bump NJsonSchema from 10.3.5 to 10.3.6 (#14818)</li>
<li>Bump <code>NJsonSchema</code> from <code>10.3.4</code> to <code>10.3.5</code> (#14807)</li>
</ul>

</details>

### Documentation and Help Content

- Update `README.md` and `metadata.json` for upcoming releases (#14755)
- Merge 7.1.3 and 7.0.6 Change log to master (#15009)
- Update `README` and `metadata.json` for releases (#14997)
- Update ChangeLog for `v7.1.2` release (#14783)
- Update ChangeLog for `v7.0.5` release (#14782) (Internal 14479)

[7.2.0-preview.4]: https://github.com/PowerShell/PowerShell/compare/v7.2.0-preview.3...v7.2.0-preview.4

## [7.2.0-preview.3] - 2021-02-11

### Breaking Changes

- Fix `Get-Date -UFormat %u` behavior to comply with ISO 8601 (#14549) (Thanks @brianary!)

### Engine Updates and Fixes

- Together with `PSDesiredStateConfiguration` `v3` module allows `Get-DscResource`, `Invoke-DscResource` and DSC configuration compilation on all platforms, supported by PowerShell (using class-based DSC resources).

### Performance

- Avoid array allocations from `Directory.GetDirectories` and `Directory.GetFiles`. (#14326) (Thanks @xtqqczze!)
- Avoid `string.ToLowerInvariant()` from `GetEnvironmentVariableAsBool()` to avoid loading libicu at startup (#14323) (Thanks @iSazonov!)
- Get PowerShell version in `PSVersionInfo` using assembly attribute instead of `FileVersionInfo` (#14332) (Thanks @Fs00!)

### General Cmdlet Updates and Fixes

- Suppress `Write-Progress` in `ConsoleHost` if output is redirected and fix tests (#14716)
- Experimental feature `PSAnsiProgress`: Add minimal progress bar using ANSI rendering (#14414)
- Fix web cmdlets to properly construct URI from body when using `-NoProxy` (#14673)
- Update the `ICommandPredictor` to provide more feedback and also make feedback easier to be correlated (#14649)
- Reset color after writing `Verbose`, `Debug`, and `Warning` messages (#14698)
- Fix using variable for nested `ForEach-Object -Parallel` calls (#14548)
- When formatting, if collection is modified, don't fail the entire pipeline (#14438)
- Improve completion of parameters for attributes (#14525) (Thanks @MartinGC94!)
- Write proper error messages for `Get-Command ' '` (#13564) (Thanks @jakekerr!)
- Fix typo in the resource string `ProxyURINotSupplied` (#14526) (Thanks @romero126!)
- Add support to `$PSStyle` for strikethrough and hyperlinks (#14461)
- Fix `$PSStyle` blink codes (#14447) (Thanks @iSazonov!)

### Code Cleanup

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@xtqqczze, @powercode</p>

</summary>

<ul>
<li>Fix coding style issues: RCS1215, IDE0090, SA1504, SA1119, RCS1139, IDE0032 (#14356, #14341, #14241, #14204, #14442, #14443) (Thanks @xtqqczze!)</li>
<li>Enable coding style checks: CA2249, CA1052, IDE0076, IDE0077, SA1205, SA1003, SA1314, SA1216, SA1217, SA1213 (#14395, #14483, #14494, #14495, #14441, #14476, #14470, #14471, #14472) (Thanks @xtqqczze!)</li>
<li>Enable nullable in PowerShell codebase (#14160, #14172, #14088, #14154, #14166, #14184, #14178) (Thanks @powercode!)</li>
<li>Use <code>string.Split(char)</code> instead of <code>string.Split(string)</code> (#14465) (Thanks @xtqqczze!)</li>
<li>Use <code>string.Contains(char)</code> overload (#14368) (Thanks @xtqqczze!)</li>
<li>Refactor complex <code>if</code> statements (#14398) (Thanks @xtqqczze!)</li>
</ul>

</details>

### Tools

- Update script to use .NET 6 build resources (#14705)
- Fix the daily GitHub action (#14711) (Thanks @imba-tjd!)
- GitHub Actions: fix deprecated `::set-env` (#14629) (Thanks @imba-tjd!)
- Update markdown test tools (#14325) (Thanks @RDIL!)
- Upgrade `StyleCopAnalyzers` to `v1.2.0-beta.312` (#14354) (Thanks @xtqqczze!)

### Tests

- Remove packaging from daily Windows build (#14749)
- Update link to the Manning book (#14750)
- A separate Windows packaging CI (#14670)
- Update `ini` component version in test `package.json` (#14454)
- Disable `libmi` dependent tests for macOS. (#14446)

### Build and Packaging Improvements

<details>

<ul>
<li>Fix the NuGet feed name and URL for .NET 6</li>
<li>Fix third party signing for files in sub-folders (#14751)</li>
<li>Make build script variable an <code>ArrayList</code> to enable <code>Add()</code> method (#14748)</li>
<li>Remove old .NET SDKs to make <code>dotnet restore</code> work with the latest SDK in CI pipeline (#14746)</li>
<li>Remove outdated Linux dependencies (#14688)</li>
<li>Bump .NET SDK version to 6.0.0-preview.1 (#14719)</li>
<li>Bump <code>NJsonSchema</code> to 10.3.4 (#14714)</li>
<li>Update daily GitHub action to allow manual trigger (#14718)</li>
<li>Bump <code>XunitXml.TestLogger</code> to 3.0.62 (#14702)</li>
<li>Make universal deb package based on the deb package specification (#14681)</li>
<li>Add manual release automation steps and improve changelog script (#14445)</li>
<li>Fix release build to upload global tool packages to artifacts (#14620)</li>
<li>Port changes from the PowerShell v7.0.4 release (#14637)</li>
<li>Port changes from the PowerShell v7.1.1 release (#14621)</li>
<li>Updated README and <code>metadata.json</code> (#14401, #14606, #14612)</li>
<li>Do not push nupkg artifacts to MyGet (#14613)</li>
<li>Use one feed in each <code>nuget.config</code> in official builds (#14363)</li>
<li>Fix path signed RPMs are uploaded from in release build (#14424)</li>
</ul>

</details>

### Documentation and Help Content

- Update distribution support request template to point to .NET 5.0 support document (#14578)
- Remove security GitHub issue template (#14453)
- Add intent for using the Discussions feature in repo (#14399)
- Fix Universal Dashboard to refer to PowerShell Universal (#14437)
- Update document link because of HTTP 301 redirect (#14431) (Thanks @xtqqczze!)

[7.2.0-preview.3]: https://github.com/PowerShell/PowerShell/compare/v7.2.0-preview.2...v7.2.0-preview.3

## [7.2.0-preview.2] - 2020-12-15

### Breaking Changes

- Improve detection of mutable value types (#12495) (Thanks @vexx32!)
- Ensure `-PipelineVariable` is set for all output from script cmdlets (#12766) (Thanks @vexx32!)

### Experimental Features

- `PSAnsiRendering`: Enable ANSI formatting via `$PSStyle` and support suppressing ANSI output (#13758)

### Performance

- Optimize `IEnumerable` variant of replace operator (#14221) (Thanks @iSazonov!)
- Refactor multiply operation for better performance in two `Microsoft.PowerShell.Commands.Utility` methods (#14148) (Thanks @xtqqczze!)
- Use `Environment.TickCount64` instead of `Datetime.Now` as the random seed for AppLocker test file content (#14283) (Thanks @iSazonov!)
- Avoid unnecessary array allocations when searching in GAC (#14291) (Thanks @xtqqczze!)
- Use `OrdinalIgnoreCase` in `CommandLineParser` (#14303) (Thanks @iSazonov!)
- Use `StringComparison.Ordinal` instead of `StringComparison.CurrentCulture` (#14298) (Thanks @iSazonov!)
- Avoid creating instances of the generated delegate helper class in `-replace` implementation (#14128)

### General Cmdlet Updates and Fixes

- Write better error message if config file is broken (#13496) (Thanks @iSazonov!)
- Make AppLocker Enforce mode take precedence over UMCI Audit mode (#14353)
- Add `-SkipLimitCheck` switch to `Import-PowerShellDataFile` (#13672)
- Restrict `New-Object` in NoLanguage mode under lock down (#14140) (Thanks @krishnayalavarthi!)
- The `-Stream` parameter now works with directories (#13941) (Thanks @kyanha!)
- Avoid an exception if file system does not support reparse points (#13634) (Thanks @iSazonov!)
- Enable `CA1012`: Abstract types should not have public constructors (#13940) (Thanks @xtqqczze!)
- Enable `SA1212`: Property accessors should follow order (#14051) (Thanks @xtqqczze!)

### Code Cleanup

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@xtqqczze, @matthewjdegarmo, @powercode, @Gimly</p>

</summary>

<ul>
<li>Enable <code>SA1007</code>: Operator keyword should be followed by space (#14130) (Thanks @xtqqczze!)</li>
<li>Expand <code>where</code> alias to <code>Where-Object</code> in <code>Reset-PWSHSystemPath.ps1</code> (#14113) (Thanks @matthewjdegarmo!)</li>
<li>Fix whitespace issues (#14092) (Thanks @xtqqczze!)</li>
<li>Add <code>StyleCop.Analyzers</code> package (#13963) (Thanks @xtqqczze!)</li>
<li>Enable <code>IDE0041</code>: <code>UseIsNullCheck</code> (#14041) (Thanks @xtqqczze!)</li>
<li>Enable <code>IDE0082</code>: <code>ConvertTypeOfToNameOf</code> (#14042) (Thanks @xtqqczze!)</li>
<li>Remove unnecessary usings part 4 (#14023) (Thanks @xtqqczze!)</li>
<li>Fix <code>PriorityAttribute</code> name (#14094) (Thanks @xtqqczze!)</li>
<li>Enable nullable: <code>System.Management.Automation.Interpreter.IBoxableInstruction</code> (#14165) (Thanks @powercode!)</li>
<li>Enable nullable: <code>System.Management.Automation.Provider.IDynamicPropertyProvider</code> (#14167) (Thanks @powercode!)</li>
<li>Enable nullable: <code>System.Management.Automation.Language.IScriptExtent</code> (#14179) (Thanks @powercode!)</li>
<li>Enable nullable: <code>System.Management.Automation.Language.ICustomAstVisitor2</code> (#14192) (Thanks @powercode!)</li>
<li>Enable nullable: <code>System.Management.Automation.LanguagePrimitives.IConversionData</code> (#14187) (Thanks @powercode!)</li>
<li>Enable nullable: <code>System.Automation.Remoting.Client.IWSManNativeApiFacade</code> (#14186) (Thanks @powercode!)</li>
<li>Enable nullable: <code>System.Management.Automation.Language.ISupportsAssignment</code> (#14180) (Thanks @powercode!)</li>
<li>Enable nullable: <code>System.Management.Automation.ICommandRuntime2</code> (#14183) (Thanks @powercode!)</li>
<li>Enable nullable: <code>System.Management.Automation.IOutputProcessingState</code> (#14175) (Thanks @powercode!)</li>
<li>Enable nullable: <code>System.Management.Automation.IJobDebugger</code> (#14174) (Thanks @powercode!)</li>
<li>Enable nullable: <code>System.Management.Automation.Interpreter.IInstructionProvider</code> (#14173) (Thanks @powercode!)</li>
<li>Enable nullable: <code>System.Management.Automation.IHasSessionStateEntryVisibility</code> (#14169) (Thanks @powercode!)</li>
<li>Enable nullable: <code>System.Management.Automation.Tracing.IEtwEventCorrelator</code> (#14168) (Thanks @powercode!)</li>
<li>Fix syntax error in Windows packaging script (#14377)</li>
<li>Remove redundant local assignment in <code>AclCommands</code> (#14358) (Thanks @xtqqczze!)</li>
<li>Enable nullable: <code>System.Management.Automation.Language.IAstPostVisitHandler</code> (#14164) (Thanks @powercode!)</li>
<li>Enable nullable: <code>System.Management.Automation.IModuleAssemblyInitializer</code> (#14158) (Thanks @powercode!)</li>
<li>Use <code>Microsoft.PowerShell.MarkdownRender</code> package from <code>nuget.org</code> (#14090)</li>
<li>Replace <code>GetFiles</code> in <code>TestModuleManifestCommand</code> (#14317) (Thanks @xtqqczze!)</li>
<li>Enable nullable: <code>System.Management.Automation.Provider.IContentWriter</code> (#14152) (Thanks @powercode!)</li>
<li>Simplify getting Encoding in <code>TranscriptionOption.FlushContentToDisk</code> (#13910) (Thanks @Gimly!)</li>
<li>Mark applicable structs as <code>readonly</code> and use <code>in</code>-modifier (#13919) (Thanks @xtqqczze!)</li>
<li>Enable nullable: <code>System.Management.Automation.IArgumentCompleter</code> (#14182) (Thanks @powercode!)</li>
<li>Enable <code>CA1822</code>: Mark <code>private</code> members as <code>static</code> (#13897) (Thanks @xtqqczze!)</li>
<li>Fix <code>IDE0090</code>: Simplify <code>new</code> expression part 6 (#14338) (Thanks @xtqqczze!)</li>
<li>Avoid array allocations from <code>GetDirectories</code>/<code>GetFiles</code>. (#14328) (Thanks @xtqqczze!)</li>
<li>Avoid array allocations from <code>GetDirectories</code>/<code>GetFiles</code>. (#14330) (Thanks @xtqqczze!)</li>
<li>Fix <code>RCS1188</code>: Remove redundant auto-property initialization part 2 (#14262) (Thanks @xtqqczze!)</li>
<li>Enable nullable: <code>System.Management.Automation.Host.IHostSupportsInteractiveSession</code> (#14170) (Thanks @powercode!)</li>
<li>Enable nullable: <code>System.Management.Automation.Provider.IPropertyCmdletProvider</code> (#14176) (Thanks @powercode!)</li>
<li>Fix <code>IDE0090</code>: Simplify new expression part 5 (#14301) (Thanks @xtqqczze!)</li>
<li>Enable <code>IDE0075</code>: <code>SimplifyConditionalExpression</code> (#14078) (Thanks @xtqqczze!)</li>
<li>Remove unnecessary usings part 9 (#14288) (Thanks @xtqqczze!)</li>
<li>Fix StyleCop and MarkdownLint CI failures (#14297) (Thanks @xtqqczze!)</li>
<li>Enable <code>SA1000</code>: Keywords should be spaced correctly (#13973) (Thanks @xtqqczze!)</li>
<li>Fix <code>RCS1188</code>: Remove redundant auto-property initialization part 1 (#14261) (Thanks @xtqqczze!)</li>
<li>Mark <code>private</code> members as <code>static</code> part 10 (#14235) (Thanks @xtqqczze!)</li>
<li>Mark <code>private</code> members as <code>static</code> part 9 (#14234) (Thanks @xtqqczze!)</li>
<li>Fix <code>SA1642</code> for <code>Microsoft.Management.Infrastructure.CimCmdlets</code> (#14239) (Thanks @xtqqczze!)</li>
<li>Use <code>AsSpan</code>/<code>AsMemory</code> slice constructor (#14265) (Thanks @xtqqczze!)</li>
<li>Fix <code>IDE0090</code>: Simplify <code>new</code> expression part 4.6 (#14260) (Thanks @xtqqczze!)</li>
<li>Fix <code>IDE0090</code>: Simplify <code>new</code> expression part 4.5 (#14259) (Thanks @xtqqczze!)</li>
<li>Fix <code>IDE0090</code>: Simplify <code>new</code> expression part 4.3 (#14257) (Thanks @xtqqczze!)</li>
<li>Fix <code>IDE0090</code>: Simplify <code>new</code> expression part 4.2 (#14256) (Thanks @xtqqczze!)</li>
<li>Fix <code>IDE0090</code>: Simplify <code>new</code> expression part 2 (#14200) (Thanks @xtqqczze!)</li>
<li>Enable <code>SA1643</code>: Destructor summary documentation should begin with standard text (#14236) (Thanks @xtqqczze!)</li>
<li>Fix <code>IDE0090</code>: Simplify new expression part 4.4 (#14258) (Thanks @xtqqczze!)</li>
<li>Use xml documentation child blocks correctly (#14249) (Thanks @xtqqczze!)</li>
<li>Fix <code>IDE0090</code>: Simplify <code>new</code> expression part 4.1 (#14255) (Thanks @xtqqczze!)</li>
<li>Use consistent spacing in xml documentation tags (#14231) (Thanks @xtqqczze!)</li>
<li>Enable <code>IDE0074</code>: Use coalesce compound assignment (#13396) (Thanks @xtqqczze!)</li>
<li>Remove unnecessary finalizers (#14248) (Thanks @xtqqczze!)</li>
<li>Mark local variable as <code>const</code> (#13217) (Thanks @xtqqczze!)</li>
<li>Fix <code>IDE0032</code>: <code>UseAutoProperty</code> part 2 (#14244) (Thanks @xtqqczze!)</li>
<li>Fix <code>IDE0032</code>: <code>UseAutoProperty</code> part 1 (#14243) (Thanks @xtqqczze!)</li>
<li>Mark <code>private</code> members as <code>static</code> part 8 (#14233) (Thanks @xtqqczze!)</li>
<li>Fix <code>CA1822</code>: Mark members as <code>static</code> part 6 (#14229) (Thanks @xtqqczze!)</li>
<li>Fix <code>CA1822</code>: Mark members as <code>static</code> part 5 (#14228) (Thanks @xtqqczze!)</li>
<li>Fix <code>CA1822</code>: Mark members as <code>static</code> part 4 (#14227) (Thanks @xtqqczze!)</li>
<li>Fix <code>CA1822</code>: Mark members as <code>static</code> part 3 (#14226) (Thanks @xtqqczze!)</li>
<li>Fix <code>CA1822</code>: Mark members as <code>static</code> part 2 (#14225) (Thanks @xtqqczze!)</li>
<li>Fix <code>CA1822</code>: Mark members as <code>static</code> part 1 (#14224) (Thanks @xtqqczze!)</li>
<li>Use <code>see</code> keyword in documentation (#14220) (Thanks @xtqqczze!)</li>
<li>Enable <code>CA2211</code>: Non-constant fields should not be visible (#14073) (Thanks @xtqqczze!)</li>
<li>Enable <code>CA1816</code>: <code>Dispose</code> methods should call <code>SuppressFinalize</code> (#14074) (Thanks @xtqqczze!)</li>
<li>Remove incorrectly implemented finalizer (#14246) (Thanks @xtqqczze!)</li>
<li>Fix <code>CA1822</code>: Mark members as <code>static</code> part 7 (#14230) (Thanks @xtqqczze!)</li>
<li>Fix <code>SA1122</code>: Use <code>string.Empty</code> for empty strings (#14218) (Thanks @xtqqczze!)</li>
<li>Fix various xml documentation issues (#14223) (Thanks @xtqqczze!)</li>
<li>Remove unnecessary <code>using</code>s part 8 (#14072) (Thanks @xtqqczze!)</li>
<li>Enable <code>SA1006</code>: Preprocessor keywords should not be preceded by space (#14052) (Thanks @xtqqczze!)</li>
<li>Fix <code>SA1642</code> for <code>Microsoft.PowerShell.Commands.Utility</code> (#14142) (Thanks @xtqqczze!)</li>
<li>Enable <code>CA2216</code>: Disposable types should declare finalizer (#14089) (Thanks @xtqqczze!)</li>
<li>Wrap and name <code>LoadBinaryModule</code> arguments (#14193) (Thanks @xtqqczze!)</li>
<li>Wrap and name <code>GetListOfFilesFromData</code> arguments (#14194) (Thanks @xtqqczze!)</li>
<li>Enable <code>SA1002</code>: Semicolons should be spaced correctly (#14197) (Thanks @xtqqczze!)</li>
<li>Fix <code>IDE0090</code>: Simplify <code>new</code> expression part 3 (#14201) (Thanks @xtqqczze!)</li>
<li>Enable <code>SA1106</code>: Code should not contain empty statements (#13964) (Thanks @xtqqczze!)</li>
<li>Code performance fixes follow-up (#14207) (Thanks @xtqqczze!)</li>
<li>Remove uninformative comments (#14199) (Thanks @xtqqczze!)</li>
<li>Fix <code>IDE0090</code>: Simplify <code>new</code> expression part 1 (#14027) (Thanks @xtqqczze!)</li>
<li>Enable <code>SA1517</code>: Code should not contain blank lines at start of file (#14131) (Thanks @xtqqczze!)</li>
<li>Enable <code>SA1131</code>: Use readable conditions (#14132) (Thanks @xtqqczze!)</li>
<li>Enable <code>SA1507</code>: Code should not contain multiple blank lines in a row (#14136) (Thanks @xtqqczze!)</li>
<li>Enable <code>SA1516</code> Elements should be separated by blank line (#14137) (Thanks @xtqqczze!)</li>
<li>Enable <code>IDE0031</code>: Null check can be simplified (#13548) (Thanks @xtqqczze!)</li>
<li>Enable <code>CA1065</code>: Do not raise exceptions in unexpected locations (#14117) (Thanks @xtqqczze!)</li>
<li>Enable <code>CA1000</code>: Do not declare <code>static</code> members on generic types (#14097) (Thanks @xtqqczze!)</li>
</ul>

</details>

### Tools

- Fixing formatting in `Reset-PWSHSystemPath.ps1` (#13689) (Thanks @dgoldman-msft!)

### Tests

- Reinstate `Test-Connection` tests (#13324)
- Update markdown test packages with security fixes (#14145)

### Build and Packaging Improvements

<details>

<ul>
<li>Fix a typo in the <code>Get-ChangeLog</code> function (#14129)</li>
<li>Update <code>README</code> and <code>metadata.json</code> for <code>7.2.0-preview.1</code> release (#14104)</li>
<li>Bump <code>NJsonSchema</code> from <code>10.2.2</code> to <code>10.3.1</code> (#14040)</li>
<li>Move windows package signing to use ESRP (#14060)</li>
<li>Use one feed in each <code>nuget.config</code> in official builds (#14363)</li>
<li>Fix path signed RPMs are uploaded from in release build (#14424)</li>
<li>Add <code>Microsoft.PowerShell.MarkdownRender</code> to the package reference list (#14386)</li>
<li>Fix issue with unsigned build (#14367)</li>
<li>Move macOS and nuget to ESRP signing (#14324)</li>
<li>Fix nuget packaging to scrub <code>NullableAttribute</code> (#14344)</li>
<li>Bump <code>Microsoft.NET.Test.Sdk</code> from 16.8.0 to 16.8.3 (#14310)</li>
<li>Bump <code>Markdig.Signed</code> from 0.22.0 to 0.22.1 (#14305)</li>
<li>Bump <code>Microsoft.ApplicationInsights</code> from 2.15.0 to 2.16.0 (#14031)</li>
<li>Move Linux to ESRP signing (#14210)</li>
</ul>

</details>

### Documentation and Help Content

- Fix example `nuget.config` (#14349)
- Fix a broken link in Code Guidelines doc (#14314) (Thanks @iSazonov!)

[7.2.0-preview.2]: https://github.com/PowerShell/PowerShell/compare/v7.2.0-preview.1...v7.2.0-preview.2

## [7.2.0-preview.1] - 2020-11-17

### Engine Updates and Fixes

- Change the default fallback encoding for `GetEncoding` in `Start-Transcript` to be `UTF8` without a BOM (#13732) (Thanks @Gimly!)

### General Cmdlet Updates and Fixes

- Update `pwsh -?` output to match docs (#13748)
- Fix `NullReferenceException` in `Test-Json` (#12942) (Thanks @iSazonov!)
- Make `Dispose` in `TranscriptionOption` idempotent (#13839) (Thanks @krishnayalavarthi!)
- Add additional Microsoft PowerShell modules to the tracked modules list (#12183)
- Relax further `SSL` verification checks for `WSMan` on non-Windows hosts with verification available (#13786) (Thanks @jborean93!)
- Add the `OutputTypeAttribute` to `Get-ExperimentalFeature` (#13738) (Thanks @ThomasNieto!)
- Fix blocking wait when starting file associated with a Windows application (#13750)
- Emit warning if `ConvertTo-Json` exceeds `-Depth` value (#13692)

### Code Cleanup

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@xtqqczze, @mkswd, @ThomasNieto, @PatLeong, @paul-cheung, @georgettica</p>

</summary>

<ul>
<li>Fix RCS1049: Simplify boolean comparison (#13994) (Thanks @xtqqczze!)</li>
<li>Enable IDE0062: Make local function static (#14044) (Thanks @xtqqczze!)</li>
<li>Enable CA2207: Initialize value type static fields inline (#14068) (Thanks @xtqqczze!)</li>
<li>Enable CA1837: Use <code>ProcessId</code> and <code>CurrentManagedThreadId</code> from <code>System.Environment</code> (#14063) (Thanks @xtqqczze and @PatLeong!)</li>
<li>Remove unnecessary using directives (#14014, #14017, #14021, #14050, #14065, #14066, #13863, #13860, #13861, #13814) (Thanks @xtqqczze and @ThomasNieto!)</li>
<li>Remove unnecessary usage of LINQ <code>Count</code> method (#13545) (Thanks @xtqqczze!)</li>
<li>Fix SA1518: The code must not contain extra blank lines at the end of the file (#13574) (Thanks @xtqqczze!)</li>
<li>Enable CA1829: Use the <code>Length</code> or <code>Count</code> property instead of <code>Count()</code> (#13925) (Thanks @xtqqczze!)</li>
<li>Enable CA1827: Do not use <code>Count()</code> or <code>LongCount()</code> when <code>Any()</code> can be used (#13923) (Thanks @xtqqczze!)</li>
<li>Enable or fix nullable usage in a few files (#13793, #13805, #13808, #14018, #13804) (Thanks @mkswd and @georgettica!)</li>
<li>Enable IDE0040: Add accessibility modifiers (#13962, #13874) (Thanks @xtqqczze!)</li>
<li>Make applicable private Guid fields readonly (#14000) (Thanks @xtqqczze!)</li>
<li>Fix CA1003: Use generic event handler instances (#13937) (Thanks @xtqqczze!)</li>
<li>Simplify delegate creation (#13578) (Thanks @xtqqczze!)</li>
<li>Fix RCS1033: Remove redundant boolean literal (#13454) (Thanks @xtqqczze!)</li>
<li>Fix RCS1221: Use pattern matching instead of combination of <code>as</code> operator and null check (#13333) (Thanks @xtqqczze!)</li>
<li>Use <code>is not</code> syntax (#13338) (Thanks @xtqqczze!)</li>
<li>Replace magic number with constant in PDH (#13536) (Thanks @xtqqczze!)</li>
<li>Fix accessor order (#13538) (Thanks @xtqqczze!)</li>
<li>Enable IDE0054: Use compound assignment (#13546) (Thanks @xtqqczze!)</li>
<li>Fix RCS1098: Constant values should be on right side of comparisons (#13833) (Thanks @xtqqczze!)</li>
<li>Enable CA1068: <code>CancellationToken</code> parameters must come last (#13867) (Thanks @xtqqczze!)</li>
<li>Enable CA10XX rules with suggestion severity (#13870, #13928, #13924) (Thanks @xtqqczze!)</li>
<li>Enable IDE0064: Make Struct fields writable (#13945) (Thanks @xtqqczze!)</li>
<li>Run <code>dotnet-format</code> to improve formatting of source code (#13503) (Thanks @xtqqczze!)</li>
<li>Enable CA1825: Avoid zero-length array allocations (#13961) (Thanks @xtqqczze!)</li>
<li>Add IDE analyzer rule IDs to comments (#13960) (Thanks @xtqqczze!)</li>
<li>Enable CA1830: Prefer strongly-typed <code>Append</code> and <code>Insert</code> method overloads on <code>StringBuilder</code> (#13926) (Thanks @xtqqczze!)</li>
<li>Enforce code style in build (#13957) (Thanks @xtqqczze!)</li>
<li>Enable CA1836: Prefer <code>IsEmpty</code> over <code>Count</code> when available (#13877) (Thanks @xtqqczze!)</li>
<li>Enable CA1834: Consider using <code>StringBuilder.Append(char)</code> when applicable (#13878) (Thanks @xtqqczze!)</li>
<li>Fix IDE0044: Make field readonly (#13884, #13885, #13888, #13892, #13889, #13886, #13890, #13891, #13887, #13893, #13969, #13967, #13968, #13970, #13971, #13966, #14012) (Thanks @xtqqczze!)</li>
<li>Enable IDE0048: Add required parentheses (#13896) (Thanks @xtqqczze!)</li>
<li>Enable IDE1005: Invoke delegate with conditional access (#13911) (Thanks @xtqqczze!)</li>
<li>Enable IDE0036: Enable the check on the order of modifiers (#13958, #13881) (Thanks @xtqqczze!)</li>
<li>Use span-based <code>String.Concat</code> instead of <code>String.Substring</code> (#13500) (Thanks @xtqqczze!)</li>
<li>Enable CA1050: Declare types in namespace (#13872) (Thanks @xtqqczze!)</li>
<li>Fix minor keyword typo in C# code comment (#13811) (Thanks @paul-cheung!)</li>
</ul>

</details>

### Tools

- Enable `CodeQL` Security scanning (#13894)
- Add global `AnalyzerConfig` with default configuration (#13835) (Thanks @xtqqczze!)

### Build and Packaging Improvements

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@mkswd, @xtqqczze</p>

</summary>

<ul>
<li>Bump <code>Microsoft.NET.Test.Sdk</code> to <code>16.8.0</code> (#14020)</li>
<li>Bump <code>Microsoft.CodeAnalysis.CSharp</code> to <code>3.8.0</code> (#14075)</li>
<li>Remove workarounds for .NET 5 RTM builds (#14038)</li>
<li>Migrate 3rd party signing to ESRP (#14010)</li>
<li>Fixes to release pipeline for GA release (#14034)</li>
<li>Don't do a shallow checkout (#13992)</li>
<li>Add validation and dependencies for Ubuntu 20.04 distribution to packaging script (#13993)</li>
<li>Add .NET install workaround for RTM (#13991)</li>
<li>Move to ESRP signing for Windows files (#13988)</li>
<li>Update <code>PSReadLine</code> version to <code>2.1.0</code> (#13975)</li>
<li>Bump .NET to version <code>5.0.100-rtm.20526.5</code> (#13920)</li>
<li>Update script to use .NET RTM feeds (#13927)</li>
<li>Add checkout step to release build templates (#13840)</li>
<li>Turn on <code>/features:strict</code> for all projects (#13383) (Thanks @xtqqczze!)</li>
<li>Bump <code>NJsonSchema</code> to <code>10.2.2</code> (#13722, #13751)</li>
<li>Add flag to make Linux script publish to production repo (#13714)</li>
<li>Bump <code>Markdig.Signed</code> to <code>0.22.0</code> (#13741)</li>
<li>Use new release script for Linux packages (#13705)</li>
</ul>

</details>

### Documentation and Help Content

- Fix links to LTS versions for Windows (#14070)
- Fix `crontab` formatting in example doc (#13712) (Thanks @dgoldman-msft!)

[7.2.0-preview.1]: https://github.com/PowerShell/PowerShell/compare/v7.1.0...v7.2.0-preview.1

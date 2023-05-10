# Current preview release

## [7.4.0-preview.3] - 2023-04-20

### Breaking Changes

- Remove code related to `#requires -pssnapin` (#19320)

### Engine Updates and Fixes

- Change the arrow used in feedback suggestion to a more common Unicode character (#19534)
- Support trigger registration in feedback provider (#19525)
- Update the `ICommandPredictor` interface to reduce boilerplate code from predictor implementation (#19414)
- Fix a crash in the type inference code (#19400) (Thanks @MartinGC94!)

### Performance

- Speed up `Resolve-Path` relative path resolution (#19171) (Thanks @MartinGC94!)

### General Cmdlet Updates and Fixes

- Infer external application output as strings (#19193) (Thanks @MartinGC94!)
- Fix a race condition in `Add-Type` (#19471)
- Detect insecure `https-to-http` redirect only if both URIs are absolute (#19468) (Thanks @CarloToso!)
- Support `Ctrl+c` when connection hangs while reading data in WebCmdlets (#19330) (Thanks @stevenebutler!)
- Enable type conversion of `AutomationNull` to `$null` for assignment (#19415)
- Add the parameter `-Environment` to `Start-Process` (#19374)
- Add the parameter `-RelativeBasePath` to `Resolve-Path` (#19358) (Thanks @MartinGC94!)
- Exclude redundant parameter aliases from completion results (#19382) (Thanks @MartinGC94!)
- Allow using a folder path in WebCmdlets' `-OutFile` parameter (#19007) (Thanks @CarloToso!)

### Code Cleanup

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@eltociear, @CarloToso</p>

</summary>

<ul>
<li>Fix typo in <code>typeDataXmlLoader.cs</code> (#19319) (Thanks @eltociear!)</li>
<li>Fix typo in <code>Compiler.cs</code> (#19491) (Thanks @eltociear!)</li>
<li>Inline the <code>GetResponseObject</code> method (#19380) (Thanks @CarloToso!)</li>
<li>Simplify <code>ContentHelper</code> methods (#19367) (Thanks @CarloToso!)</li>
<li>Initialize regex lazily in <code>BasicHtmlWebResponseObject</code> (#19361) (Thanks @CarloToso!)</li>
<li>Fix codefactor issue in if-statement (part 5) (#19286) (Thanks @CarloToso!)</li>
<li>Add nullable annotations in <code>WebRequestSession.cs</code> (#19291) (Thanks @CarloToso!)</li>
</ul>

</details>

### Tests

- Harden the default command test (#19416)
- Skip VT100 tests on Windows Server 2012R2 as console does not support it (#19413)
- Improve package management acceptance tests by not going to the gallery (#19412)

### Build and Packaging Improvements

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@dkattan</p>

</summary>

<ul>
<li>Fixing MSI checkbox (#19325)</li>
<li>Update the experimental feature JSON files (#19297)</li>
<li>Update the cgmanifest (#19459, #19465)</li>
<li>Update .NET SDK version to <code>8.0.100-preview.3.23178.7</code> (#19381)</li>
<li>Force updating the transitive dependency on <code>Microsoft.CSharp</code> (#19514)</li>
<li>Update <code>DotnetRuntimeMetadata.json</code> to consume the .NET 8.0.0-preview.3 release (#19529)</li>
<li>Move PSGallery sync to a pool (#19523)</li>
<li>Fix the regex used for package name check in vPack build (#19511)</li>
<li>Make the vPack PAT library more obvious (#19505)</li>
<li>Change <code>Microsoft.CodeAnalysis.CSharp</code> back to 4.5.0 (#19464) (Thanks @dkattan!)</li>
<li>Update to the latest NOTICES file (#19332)</li>
<li>Add <code>PoolNames</code> variable group to compliance pipeline (#19408)</li>
<li>Fix stage dependencies and typo in release build (#19353)</li>
<li>Fix issues in release build and release pipeline (#19338)</li>
</ul>

</details>

[7.4.0-preview.3]: https://github.com/PowerShell/PowerShell/compare/v7.4.0-preview.2...v7.4.0-preview.3

## [7.4.0-preview.2] - 2023-03-14

### Breaking Changes

- Update some PowerShell APIs to throw `ArgumentException` instead of `ArgumentNullException` when the argument is an empty string (#19215) (Thanks @xtqqczze!)
- Add the parameter `-ProgressAction` to the common parameters (#18887)

### Engine Updates and Fixes

- Fix `PlainText` output to correctly remove the `Reset` VT sequence without number (#19283)
- Fix `ConciseView` to handle custom `ParserError` error records (#19239)
- Fix `VtSubstring` helper method to correctly check characters copied (#19240)
- Update the `FeedbackProvider` interface to return structured data (#19133)
- Make the exception error in PowerShell able to associate with the right history entry (#19095)
- Fix for JEA session leaking functions (#19024)
- Add WDAC events and system lockdown notification (#18893)
- Fix support for nanoserver due to lack of AMSI (#18882)

### Performance

- Use interpolated strings (#19002)(#19003)(#18977)(#18980)(#18996)(#18979)(#18997)(#18978)(#18983)(#18992)(#18993)(#18985)(#18988) (Thanks @CarloToso!)

### General Cmdlet Updates and Fixes

- Fix completion for `PSCustomObject` variable properties (#18682) (Thanks @MartinGC94!)
- Improve type inference for `Get-Random` (#18972) (Thanks @MartinGC94!)
- Make `-Encoding` parameter able to take `ANSI` encoding in PowerShell (#19298) (Thanks @CarloToso!)
- Telemetry improvements for tracking experimental feature opt out (#18762)
- Support HTTP persistent connections in Web Cmdlets (#19249) (Thanks @stevenebutler!)
- Fix using xml `-Body` in webcmdlets without an encoding (#19281) (Thanks @CarloToso!)
- Add the `Statement` property to `$MyInvocation` (#19027) (Thanks @IISResetMe!)
- Fix `Start-Process` `-Wait` with `-Credential` (#19096) (Thanks @jborean93!)
- Adjust `PUT` method behavior to `POST` one for default content type in WebCmdlets (#19152) (Thanks @CarloToso!)
- Improve verbose message in web cmdlets when content length is unknown (#19252) (Thanks @CarloToso!)
- Preserve `WebSession.MaximumRedirection` from changes (#19190) (Thanks @CarloToso!)
- Take into account `ContentType` from Headers in WebCmdlets (#19227) (Thanks @CarloToso!)
- Use C# 11 UTF-8 string literals (#19243) (Thanks @turbedi!)
- Add property assignment completion for enums (#19178) (Thanks @MartinGC94!)
- Fix class member completion for classes with base types (#19179) (Thanks @MartinGC94!)
- Add `-Path` and `-LiteralPath` parameters to `Test-Json` cmdlet (#19042) (Thanks @ArmaanMcleod!)
- Allow to preserve the original HTTP method by adding `-PreserveHttpMethodOnRedirect` to Web cmdlets (#18894) (Thanks @CarloToso!)
- Webcmdlets display an error on https to http redirect (#18595) (Thanks @CarloToso!)
- Build the relative URI for links from the response in `Invoke-WebRequest` (#19092) (Thanks @CarloToso!)
- Fix redirection for `-CustomMethod` `POST` in WebCmdlets (#19111) (Thanks @CarloToso!)
- Dispose previous response in Webcmdlets (#19117) (Thanks @CarloToso!)
- Improve `Invoke-WebRequest` xml and json errors format (#18837) (Thanks @CarloToso!)
- Fix error formatting to remove the unneeded leading newline for concise view (#19080)
- Add `-NoHeader` parameter to `ConvertTo-Csv` and `Export-Csv` cmdlets (#19108) (Thanks @ArmaanMcleod!)
- Fix `Start-Process -Credential -Wait` to work on Windows (#19082)
- Add `ValidateNotNullOrEmpty` to `OutFile` and `InFile` parameters of WebCmdlets (#19044) (Thanks @CarloToso!)
- Correct spelling of "custom" in event (#19059) (Thanks @spaette!)
- Ignore expected error for file systems not supporting alternate streams (#19065)
- Adding missing guard for telemetry opt out to avoid `NullReferenceException` when importing modules (#18949) (Thanks @powercode!)
- Fix progress calculation divide by zero in Copy-Item (#19038)
- Add progress to `Copy-Item` (#18735)
- WebCmdlets parse XML declaration to get encoding value, if present. (#18748) (Thanks @CarloToso!)
- `HttpKnownHeaderNames` update headers list  (#18947) (Thanks @CarloToso!)
- Fix bug with managing redirection and `KeepAuthorization` in Web cmdlets (#18902) (Thanks @CarloToso!)
- Fix `Get-Error` to work with strict mode (#18895)
- Add `AllowInsecureRedirect` switch to Web cmdlets (#18546) (Thanks @CarloToso!)
- `Invoke-RestMethod` `-FollowRelLink` fix links containing commas (#18829) (Thanks @CarloToso!)
- Prioritize the default parameter set when completing positional arguments (#18755) (Thanks @MartinGC94!)
- Add `-CommandWithArgs` parameter to pwsh (#18726)
- Enable creating composite subsystem implementation in modules (#18888)
- Fix `Format-Table -RepeatHeader` for property derived tables (#18870)
- Add `StatusCode` to `HttpResponseException` (#18842) (Thanks @CarloToso!)
- Fix type inference for all scope variables (#18758) (Thanks @MartinGC94!)
- Add completion for Using keywords (#16514) (Thanks @MartinGC94!)

### Code Cleanup

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@CarloToso, @iSazonov, @xtqqczze, @turbedi, @syntax-tm, @eltociear, @ArmaanMcleod</p>

</summary>

<ul>
<li>Small cleanup in the WebCmdlet code (#19299) (Thanks @CarloToso!)</li>
<li>Remove unused GUID detection code from console host (#18871) (Thanks @iSazonov!)</li>
<li>Fix <code>CodeFactor</code> issues in the code base - part 4 (#19270) (Thanks @CarloToso!)</li>
<li>Fix codefactor if part 3 (#19269) (Thanks @CarloToso!)</li>
<li>Fix codefactor if part 2 (#19267) (Thanks @CarloToso!)</li>
<li>Fix codefactor if part 1 (#19266) (Thanks @CarloToso!)</li>
<li>Remove comment and simplify condition in WebCmdlets (#19251) (Thanks @CarloToso!)</li>
<li>Small style changes (#19241) (Thanks @CarloToso!)</li>
<li>Use <code>ArgumentException.ThrowIfNullOrEmpty</code> as appropriate [part 1] (#19215) (Thanks @xtqqczze!)</li>
<li>Use <code>using</code> variable to reduce the nested level (#19229) (Thanks @CarloToso!)</li>
<li>Use <code>ArgumentException.ThrowIfNullOrEmpty()</code> in more places (#19213) (Thanks @CarloToso!)</li>
<li>Replace <code>BitConverter.ToString</code> with <code>Convert.ToHexString</code> where appropriate (#19216) (Thanks @turbedi!)</li>
<li>Replace <code>Requires.NotNullOrEmpty(string)</code> with <code>ArgumentException.ThrowIfNullOrEmpty</code> (#19197) (Thanks @xtqqczze!)</li>
<li>Use <code>ArgumentOutOfRangeException.ThrowIfNegativeOrZero</code> when applicable (#19201) (Thanks @xtqqczze!)</li>
<li>Use <code>CallerArgumentExpression</code> on <code>Requires.NotNull</code> (#19200) (Thanks @xtqqczze!)</li>
<li>Revert a few change to not use 'ArgumentNullException.ThrowIfNull' (#19151)</li>
<li>Corrected some minor spelling mistakes (#19176) (Thanks @syntax-tm!)</li>
<li>Fix a typo in <code>InitialSessionState.cs</code> (#19177) (Thanks @eltociear!)</li>
<li>Fix a typo in <code>pwsh</code> help content (#19153)</li>
<li>Revert comment changes in <code>WebRequestPSCmdlet.Common.cs</code> (#19136) (Thanks @CarloToso!)</li>
<li>Small cleanup webcmdlets (#19128) (Thanks @CarloToso!)</li>
<li>Merge partials in <code>WebRequestPSCmdlet.Common.cs</code> (#19126) (Thanks @CarloToso!)</li>
<li>Cleanup WebCmdlets comments (#19124) (Thanks @CarloToso!)</li>
<li>Added minor readability and refactoring fixes to <code>Process.cs</code> (#19123) (Thanks @ArmaanMcleod!)</li>
<li>Small changes in Webcmdlets (#19109) (Thanks @CarloToso!)</li>
<li>Rework <code>SetRequestContent</code> in WebCmdlets (#18964) (Thanks @CarloToso!)</li>
<li>Small cleanup WebCmdlets (#19030) (Thanks @CarloToso!)</li>
<li>Update additional interpolated string changes (#19029)</li>
<li>Revert some of the interpolated string changes (#19018)</li>
<li>Cleanup <code>StreamHelper.cs</code>, <code>WebRequestPSCmdlet.Common.cs</code> and <code>InvokeRestMethodCommand.Common.cs</code> (#18950) (Thanks @CarloToso!)</li>
<li>Small cleanup common code of webcmdlets (#18946) (Thanks @CarloToso!)</li>
<li>Simplification of <code>GetHttpMethod</code> and <code>HttpMethod</code> in WebCmdlets (#18846) (Thanks @CarloToso!)</li>
<li>Fix typo in <code>ModuleCmdletBase.cs</code> (#18933) (Thanks @eltociear!)</li>
<li>Fix regression in <code>RemoveNulls</code> (#18881) (Thanks @iSazonov!)</li>
<li>Replace all <code>NotNull</code> with <code>ArgumentNullException.ThrowIfNull</code> (#18820) (Thanks @CarloToso!)</li>
<li>Cleanup <code>InvokeRestMethodCommand.Common.cs</code> (#18861) (Thanks @CarloToso!)</li>
</ul>

</details>

### Tools

- Add a Mariner install script (#19294)
- Add tool to trigger license information gathering for NuGet modules (#18827)

### Tests

- Update and enable the test for the type of `$input` (#18968) (Thanks @MartinGC94!)
- Increase the timeout for creating the `WebListener` (#19268)
- Increase the timeout when waiting for the event log (#19264)
- Add Windows ARM64 CI (#19040)
- Change test so output does not include newline (#19026)
- Allow system lock down test debug hook to work with new WLDP API (#18962)
- Add tests for `Allowinsecureredirect` parameter in Web cmdlets (#18939) (Thanks @CarloToso!)
- Enable `get-help` pattern tests on Unix (#18855) (Thanks @xtqqczze!)
- Create test to check if WebCmdlets decompress brotli-encoded data (#18905) (Thanks @CarloToso!)

### Build and Packaging Improvements

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@bergmeister, @xtqqczze</p>

</summary>

<ul>
<li>Restructure the package build to simplify signing and packaging stages (#19321)</li>
<li>Bump <code>Microsoft.CodeAnalysis.CSharp</code> from <code>4.4.0</code> to <code>4.6.0-2.23152.6</code> (#19306)(#19233)</li>
<li>Test fixes for stabilizing tests (#19068)</li>
<li>Bump <code>Newtonsoft.Json</code> from <code>13.0.2</code> to <code>13.0.3</code> (#19290)(#19289)</li>
<li>Fix mariner sudo detection (#19304)</li>
<li>Add stage for symbols job in Release build (#18937)</li>
<li>Bump .NET to Preview 2 version (#19305)</li>
<li>Move workflows that create PRs to private repo (#19276)</li>
<li>Use reference assemblies generated by dotnet (#19302)</li>
<li>Update the cgmanifest (#18814)(#19165)(#19296)</li>
<li>Always regenerate files WXS fragment (#19196)</li>
<li>MSI installer: Add checkbox and MSI property DISABLE_TELEMETRY to optionally disable telemetry. (#10725) (Thanks @bergmeister!)</li>
<li>Add <code>-Force</code> to <code>Move-Item</code> to fix the GitHub workflow (#19262)</li>
<li>Update and remove outdated docs to fix the URL link checks (#19261)</li>
<li>Bump <code>Markdig.Signed</code> from <code>0.30.4</code> to <code>0.31.0</code> (#19232)</li>
<li>Add pattern to replace for reference API generation (#19214)</li>
<li>Split test artifact build into windows and non-windows (#19199)</li>
<li>Set <code>LangVersion</code> compiler option to <code>11.0</code> (#18877) (Thanks @xtqqczze!)</li>
<li>Update to .NET 8 preview 1 build (#19194)</li>
<li>Simplify Windows Packaging CI Trigger YAML (#19160)</li>
<li>Bump <code>Microsoft.NET.Test.Sdk</code> from <code>17.4.0</code> to <code>17.5.0</code> (#18823)(#19191)</li>
<li>Add URL for all distributions (#19159)</li>
<li>Bump <code>Microsoft.Extensions.ObjectPool</code> from <code>7.0.1</code> to <code>7.0.3</code> (#18925)(#19155)</li>
<li>Add verification of R2R at packaging (#19129)</li>
<li>Allow cross compiling windows (#19119)</li>
<li>Update CodeQL build agent (#19113)</li>
<li>Bump <code>XunitXml.TestLogger</code> from <code>3.0.70</code> to <code>3.0.78</code> (#19066)</li>
<li>Bump <code>Microsoft.CodeAnalysis.Analyzers</code> from <code>3.3.3</code> to <code>3.3.4</code> (#18975)</li>
<li>Bump <code>BenchmarkDotNet</code> to <code>0.13.3</code> (#18878) (Thanks @xtqqczze!)</li>
<li>Bump <code>Microsoft.PowerShell.Native</code> from <code>7.4.0-preview.1</code> to <code>7.4.0-preview.2</code> (#18910)</li>
<li>Add checks for Windows 8.1 and Server 2012 in the MSI installer (#18904)</li>
<li>Update build to include <code>WinForms</code> / <code>WPF</code> in all Windows builds (#18859)</li>
</ul>

</details>

### Documentation and Help Content

- Update to the latest NOTICES file (#19169)(#19309)(#19086)(#19077)
- Update supported distros in Readme (#18667) (Thanks @techguy16!)
- Remove the 'Code Coverage Status' badge (#19265)
- Pull in change logs for `v7.2.10` and `v7.3.3` releases (#19219)
- Update tools `metadata` and `README` (#18831)(#19204)(#19014)
- Update a broken link in the `README.md` (#19187)
- Fix typos in comments (#19064) (Thanks @spaette!)
- Add `7.2` and `7.3` change logs (#19025)
- typos (#19058) (Thanks @spaette!)
- Fix typo in `dotnet-tools/README.md` (#19021) (Thanks @spaette!)
- Fix up all comments to be in the proper order with proper spacing (#18619)
- Change log for `v7.4.0-preview.1` release (#18835)

[7.4.0-preview.2]: https://github.com/PowerShell/PowerShell/compare/v7.4.0-preview.1...v7.4.0-preview.2

## [7.4.0-preview.1] - 2022-12-20

### Engine Updates and Fixes

- Add Instrumentation to `AmsiUtil` and make the init variable readonly (#18727)
- Fix typo in `OutOfProcTransportManager.cs` (#18766) (Thanks @eltociear!)
- Allow non-default encodings to be used in user's script/code (#18605)
- Add `Dim` and `DimOff` to `$PSStyle` (#18653)
- Change `exec` from alias to function to handle arbitrary arguments (#18567)
- The command prefix should also be in the error color for `NormalView` (#18555)
- Skip cloud files marked as "not on disk" during command discovery (#18152)
- Replace `UTF8Encoding(false)` with `Encoding.Default` (#18356) (Thanks @xtqqczze!)
- Fix `Switch-Process` to set `termios` appropriate for child process (#18467)
- On Unix, only explicitly terminate the native process if not in background (#18215)
- Treat `[NullString]::Value` as the string type when resolving methods (#18080)
- Improve pseudo binding for dynamic parameters (#18030) (Thanks @MartinGC94!)
- Make experimental feature `PSAnsiRenderingFileInfo` stable (#18042)
- Update to use version `2.21.0` of Application Insights. (#17903)
- Do not preserve temporary results when no need to do so (#17856)

### Performance

- Remove some static constants from `Utils.Separators` (#18154) (Thanks @iSazonov!)
- Avoid using regex when unnecessary in `ScriptWriter` (#18348)
- Use source generator for `PSVersionInfo` to improve startup time (#15603) (Thanks @iSazonov!)
- Skip evaluating suggestions at startup (#18232)
- Avoid using `Regex` when not necessary (#18210)

### General Cmdlet Updates and Fixes

- Update to use `ComputeCore.dll` for PowerShell Direct (#18194)
- Replace `ArgumentNullException(nameof())` with `ArgumentNullException.ThrowIfNull()` (#18792)(#18784) (Thanks @CarloToso!)
- Remove `TabExpansion` from remote session configuration (#18795) (Internal 23331)
- WebCmdlets get Retry-After from headers if status code is 429 (#18717) (Thanks @CarloToso!)
- Implement `SupportsShouldProcess` in `Stop-Transcript` (#18731) (Thanks @JohnLBevan!)
- Fix `New-Item -ItemType Hardlink` to resolve target to absolute path and not allow link to itself (#18634)
- Add output types to Format commands (#18746) (Thanks @MartinGC94!)
- Fix the process `CommandLine` on Linux (#18710) (Thanks @jborean93!)
- Fix `SuspiciousContentChecker.Match` to detect a pre-defined string when the text starts with it (#18693)
- Switch `$PSNativeCommandUseErrorActionPreference` to `$true` when feature is enabled (#18695)
- Fix `Start-Job` to check the existence of working directory using the PowerShell way (#18675)
- Webcmdlets add 308 to redirect codes and small cleanup (#18536) (Thanks @CarloToso!)
- Ensure `HelpInfo.Category` is consistently a string (#18254)
- Remove `gcloud` from the legacy list because it's resolved to a .ps1 script (#18575)
- Add `gcloud` and `sqlcmd` to list to use legacy argument passing (#18559)
- Fix native access violation (#18545) (#18547) (Thanks @chrullrich!)
- Fix issue when completing the first command in a script with an empty array expression (#18355) (Thanks @MartinGC94!)
- Improve type inference of hashtable keys (#17907) (Thanks @MartinGC94!)
- Fix `Switch-Process` to copy the current env to the new process (#18452)
- Fix `Switch-Process` error to include the command that is not found (#18443)
- Update `Out-Printer` to remove all decorating ANSI escape sequences from PowerShell formatting (#18425)
- Web cmdlets set default charset encoding to `UTF8` (#18219) (Thanks @CarloToso!)
- Fix incorrect cmdlet name in the script used by `Restart-Computer` (#18374) (Thanks @urizen-source!)
- Add the function `cd~` (#18308) (Thanks @GigaScratch!)
- Fix type inference error for empty return statements (#18351) (Thanks @MartinGC94!)
- Fix the exception reporting in `ConvertFrom-StringData` (#18336) (Thanks @GigaScratch!)
- Implement `IDisposable` in `NamedPipeClient` (#18341) (Thanks @xtqqczze!)
- Replace command-error suggestion with new implementation based on subsystem plugin (#18252)
- Remove the `ProcessorArchitecture` portion from the full name as it's obsolete (#18320)
- Make the fuzzy searching flexible by passing in the fuzzy matcher (#18270)
- Add `-FuzzyMinimumDistance` parameter to `Get-Command` (#18261)
- Improve startup time by triggering initialization of additional types on background thread (#18195)
- Fix decompression in web cmdlets (#17955) (Thanks @iSazonov!)
- Add `CustomTableHeaderLabel` formatting to differentiate table header labels that are not property names (#17346)
- Remove the extra new line form List formatting (#18185)
- Minor update to the `FileInfo` table formatting on Unix to make it more concise (#18183)
- Fix Parent property on processes with complex name (#17545) (Thanks @jborean93!)
- Make PowerShell class not affiliate with `Runspace` when declaring the `NoRunspaceAffinity` attribute (#18138)
- Complete the progress bar rendering in `Invoke-WebRequest` when downloading is complete or cancelled (#18130)
- Display download progress in human readable format for `Invoke-WebRequest` (#14611) (Thanks @bergmeister!)
- Update `WriteConsole` to not use `stackalloc` for buffer with too large size (#18084)
- Filter out compiler generated types for `Add-Type -PassThru` (#18095)
- Fixing `CA2014` warnings and removing the warning suppression (#17982) (Thanks @creative-cloud!)
- Make experimental feature `PSNativeCommandArgumentPassing` stable (#18044)
- Make experimental feature `PSAMSIMethodInvocationLogging` stable (#18041)
- Handle `PSObject` argument specially in method invocation logging (#18060)
- Fix typos in `EventResource.resx` (#18063) (Thanks @eltociear!)
- Make experimental feature `PSRemotingSSHTransportErrorHandling` stable (#18046)
- Make experimental feature `PSExec` stable (#18045)
- Make experimental feature `PSCleanBlock` stable (#18043)
- Fix error formatting to use color defined in `$PSStyle.Formatting` (#17987)
- Remove unneeded use of `chmod 777` (#17974)
- Support mapping foreground/background `ConsoleColor` values to VT escape sequences (#17938)
- Make `pwsh` server modes implicitly not show banner (#17921)
- Add output type attributes for `Get-WinEvent` (#17948) (Thanks @MartinGC94!)
- Remove 1 second minimum delay in `Invoke-WebRequest` for small files, and prevent file-download-error suppression. (#17896) (Thanks @AAATechGuy!)
- Add completion for values in comparisons when comparing Enums (#17654) (Thanks @MartinGC94!)
- Fix positional argument completion (#17796) (Thanks @MartinGC94!)
- Fix member completion in attribute argument (#17902) (Thanks @MartinGC94!)
- Throw when too many parameter sets are defined (#17881) (Thanks @fflaten!)
- Limit searching of `charset` attribute in `meta` tag for HTML to first 1024 characters in webcmdlets (#17813)
- Fix `Update-Help` failing silently with implicit non-US culture. (#17780) (Thanks @dkaszews!)
- Add the `ValidateNotNullOrWhiteSpace` attribute (#17191) (Thanks @wmentha!)
- Improve enumeration of inferred types in pipeline (#17799) (Thanks @MartinGC94!)

### Code Cleanup

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@MartinGC94, @CarloToso, @iSazonov, @xtqqczze, @turbedi, @trossr32, @eltociear, @AtariDreams, @jborean93</p>

</summary>

<ul>
<li>Add TSAUpload for APIScan (#18446)</li>
<li>Use Pattern matching in <code>ast.cs</code> (#18794) (Thanks @MartinGC94!)</li>
<li>Cleanup <code>webrequestpscmdlet.common.cs</code> (#18596) (Thanks @CarloToso!)</li>
<li>Unify <code>CreateFile</code> <code>pinvoke</code> in SMA (#18751) (Thanks @iSazonov!)</li>
<li>Cleanup <code>webresponseobject.common</code> (#18785) (Thanks @CarloToso!)</li>
<li><code>InvokeRestMethodCommand.Common</code> cleanup and merge partials (#18736) (Thanks @CarloToso!)</li>
<li>Replace <code>GetDirectories</code> in <code>CimDscParser</code> (#14319) (Thanks @xtqqczze!)</li>
<li>WebResponseObject.Common merge partials atomic commits (#18703) (Thanks @CarloToso!)</li>
<li>Enable pending test for <code>Start-Process</code> (#18724) (Thanks @iSazonov!)</li>
<li>Remove one CreateFileW (#18732) (Thanks @iSazonov!)</li>
<li>Replace <code>DllImport</code> with <code>LibraryImport</code> for WNetAddConnection2 (#18721) (Thanks @iSazonov!)</li>
<li>Use File.OpenHandle() instead CreateFileW pinvoke (#18722) (Thanks @iSazonov!)</li>
<li>Replace DllImport with LibraryImport for WNetGetConnection (#18690) (Thanks @iSazonov!)</li>
<li>Replace <code>DllImport</code> with <code>LibraryImport</code> - 1 (#18603) (Thanks @iSazonov!)</li>
<li>Replace DllImport with LibraryImport in SMA 3 (#18564) (Thanks @iSazonov!)</li>
<li>Replace <code>DllImport</code> with <code>LibraryImport</code> in SMA - 7 (#18594) (Thanks @iSazonov!)</li>
<li>Use static <code>DateTime.UnixEpoch</code> and <code>RandomNumberGenerator.Fill()</code> (#18621) (Thanks @turbedi!)</li>
<li>Rewrite Get-FileHash to use static HashData methods (#18471) (Thanks @turbedi!)</li>
<li>Replace DllImport with LibraryImport in SMA 8 (#18599) (Thanks @iSazonov!)</li>
<li>Replace DllImport with LibraryImport in SMA 4 (#18579) (Thanks @iSazonov!)</li>
<li>Remove NativeCultureResolver as dead code (#18582) (Thanks @iSazonov!)</li>
<li>Replace DllImport with LibraryImport in SMA 6 (#18581) (Thanks @iSazonov!)</li>
<li>Replace DllImport with LibraryImport in SMA 2 (#18543) (Thanks @iSazonov!)</li>
<li>Use standard SBCS detection (#18593) (Thanks @iSazonov!)</li>
<li>Remove unused pinvokes in RemoteSessionNamedPipe (#18583) (Thanks @iSazonov!)</li>
<li>Replace DllImport with LibraryImport in SMA 5 (#18580) (Thanks @iSazonov!)</li>
<li>Remove SafeRegistryHandle (#18597) (Thanks @iSazonov!)</li>
<li>Remove <code>ArchitectureSensitiveAttribute</code> from the code base (#18598) (Thanks @iSazonov!)</li>
<li>Build COM adapter only on Windows (#18590)</li>
<li>Include timer instantiation for legacy telemetry in conditional compiler statements in Get-Help (#18475) (Thanks @trossr32!)</li>
<li>Convert <code>DllImport</code> to <code>LibraryImport</code> for recycle bin, clipboard, and computerinfo cmdlets (#18526)</li>
<li>Replace DllImport with LibraryImport in SMA 1 (#18520) (Thanks @iSazonov!)</li>
<li>Replace DllImport with LibraryImport in engine (#18496)</li>
<li>Fix typo in InitialSessionState.cs (#18435) (Thanks @eltociear!)</li>
<li>Remove remaining unused strings from resx files (#18448)</li>
<li>Use new LINQ Order() methods instead of OrderBy(static x =&gt; x) (#18395) (Thanks @turbedi!)</li>
<li>Make use of StringSplitOptions.TrimEntries when possible (#18412) (Thanks @turbedi!)</li>
<li>Replace some <code>string.Join(string)</code> calls with <code>string.Join(char)</code> (#18411) (Thanks @turbedi!)</li>
<li>Remove unused strings from FileSystem and Registry providers (#18403)</li>
<li>Use generic <code>GetValues&lt;T&gt;</code>, <code>GetNames&lt;T&gt;</code> enum methods (#18391) (Thanks @xtqqczze!)</li>
<li>Remove unused resource strings from <code>SessionStateStrings</code> (#18394)</li>
<li>Remove unused resource strings in <code>System.Management.Automation</code> (#18388)</li>
<li>Use <code>Enum.HasFlags</code> part 1 (#18386) (Thanks @xtqqczze!)</li>
<li>Remove unused strings from parser (#18383)</li>
<li>Remove unused strings from Utility module (#18370)</li>
<li>Remove unused console strings (#18369)</li>
<li>Remove unused strings from <code>ConsoleInfoErrorStrings.resx</code> (#18367)</li>
<li>Code cleanup in <code>ContentHelper.Common.cs</code> (#18288) (Thanks @CarloToso!)</li>
<li>Remove <code>FusionAssemblyIdentity</code> and <code>GlobalAssemblyCache</code> as they are not used (#18334) (Thanks @iSazonov!)</li>
<li>Remove some static initializations in <code>StringManipulationHelper</code> (#18243) (Thanks @xtqqczze!)</li>
<li>Use <code>MemoryExtensions.IndexOfAny</code> in <code>PSv2CompletionCompleter</code> (#18245) (Thanks @xtqqczze!)</li>
<li>Use <code>MemoryExtensions.IndexOfAny</code> in <code>WildcardPattern</code> (#18242) (Thanks @xtqqczze!)</li>
<li>Small cleanup of the stub code (#18301) (Thanks @CarloToso!)</li>
<li>Fix typo in <code>RemoteRunspacePoolInternal.cs</code> (#18263) (Thanks @eltociear!)</li>
<li>Some more code cleanup related to the use of <code>PSVersionInfo</code> (#18231)</li>
<li>Use <code>MemoryExtensions.IndexOfAny</code> in <code>SessionStateInternal</code> (#18244) (Thanks @xtqqczze!)</li>
<li>Use overload APIs that take <code>char</code> instead of <code>string</code> when it's possible (#18179) (Thanks @iSazonov!)</li>
<li>Replace <code>UTF8Encoding(false)</code> with <code>Encoding.Default</code> (#18144) (Thanks @xtqqczze!)</li>
<li>Remove unused variables (#18058) (Thanks @AtariDreams!)</li>
<li>Fix typo in PowerShell.Core.Instrumentation.man (#17963) (Thanks @eltociear!)</li>
<li>Migrate <code>WinTrust</code> functions to a common location (#17598) (Thanks @jborean93!)</li>
</ul>

</details>

### Tools

- Add a function to get the PR Back-port report (#18299)
- Add a workaround in automatic rebase workflow to continue on error (#18176)
- Update list of PowerShell team members in release tools (#17909)
- Don't block if we fail to create the comment (#17869)

### Tests

- Add `testexe.exe -echocmdline` to output raw command line received by the process on Windows (#18591)
- Mark charset test as pending (#18511)
- Skip output rendering tests on Windows Server 2012 R2 (#18382)
- Increase timeout to make subsystem tests more reliable (#18380)
- Add missing -Tag 'CI' to describe blocks. (#18316)
- Use short path instead of multiple quotes in `Get-Item` test relying on node (#18250)
- Replace the CIM class used for `-Amended` parameter test (#17884) (Thanks @sethvs!)
- Stop ongoing progress-bar in `Write-Progress` test (#17880) (Thanks @fflaten!)

### Build and Packaging Improvements

<details>

<summary>

<p>We thank the following contributors!</p>

</summary>

<ul>
<li>Fix reference assembly generation logic for <code>Microsoft.PowerShell.Commands.Utility</code> (#18818)</li>
<li>Update the cgmanifest (#18676)(#18521)(#18415)(#18408)(#18197)(#18111)(#18051)(#17913)(#17867)(#17934)(#18088)</li>
<li>Bump <code>Microsoft.PowerShell.Native</code> to the latest preview version <code>v7.4.0-preview.1</code> (#18805)</li>
<li>Remove unnecessary reference to <code>System.Runtime.CompilerServices.Unsafe</code> (#18806)</li>
<li>Update the release tag in <code>metadata.json</code> for next preview (#18799)</li>
<li>Bump Microsoft.CodeAnalysis.NetAnalyzers (#18750)</li>
<li>Bump .NET SDK to version <code>7.0.101</code> (#18786)</li>
<li>Bump <code>cirrus-actions/rebase</code> from 1.7 to 1.8 (#18788)</li>
<li>Bump <code>decode-uri-component</code> from 0.2.0 to 0.2.2 (#18712)</li>
<li>Bump Microsoft.CodeAnalysis.CSharp from 4.4.0-4.final to 4.4.0 (#18562)</li>
<li>Bump Newtonsoft.Json from 13.0.1 to 13.0.2 (#18657)</li>
<li>Apply expected file permissions to Linux files after Authenticode signing (#18643)</li>
<li>Remove extra quotes after agent moves to pwsh 7.3 (#18577)</li>
<li>Don't install based on build-id for RPM (#18560)</li>
<li>Bump Microsoft.NET.Test.Sdk from 17.3.2 to 17.4.0 (#18487)</li>
<li>Bump <code>minimatch</code> from 3.0.4 to 3.1.2 (#18514)</li>
<li>Avoid depending on the pre-generated experimental feature list in private and CI builds (#18484)</li>
<li>Update <code>release-MsixBundle.yml</code> to add retries (#18465)</li>
<li>Bump System.Data.SqlClient from 4.8.4 to 4.8.5 in /src/Microsoft.PowerShell.SDK (#18515)</li>
<li>Bump to use internal .NET 7 GA build (#18508)</li>
<li>Insert the pre-release nuget feed before building test artifacts (#18507)</li>
<li>Add test for framework dependent package in release pipeline (#18506) (Internal 23139)</li>
<li>Update to azCopy 10 (#18509)</li>
<li>Fix issues with uploading changelog to GitHub release draft (#18504)</li>
<li>Bump Microsoft.CodeAnalysis.NetAnalyzers (#18442)</li>
<li>Add authenticode signing for assemblies on linux builds (#18440)</li>
<li>Do not remove <code>penimc_cor3.dll</code> from build (#18438)</li>
<li>Bump <code>Microsoft.PowerShell.Native</code> from 7.3.0-rc.1 to 7.3.0 (#18405)</li>
<li>Allow two-digit revisions in <code>vPack</code> package validation pattern (#18392)</li>
<li>Bump <code>Microsoft.CodeAnalysis.NetAnalyzers</code> (#18363)</li>
<li>Bump to .NET 7 RC2 official version (#18328)</li>
<li>Bump to .NET 7 to version <code>7.0.100-rc.2.22477.20</code> (#18286)</li>
<li>Replace win7 runtime with win8 and remove APISets (#18304)</li>
<li>Bump Microsoft.CodeAnalysis.NetAnalyzers (#18312)</li>
<li>Recurse the file listing. (#18277)</li>
<li>Create tasks to collect and publish hashes for build files. (#18276)</li>
<li>Bump Microsoft.CodeAnalysis.NetAnalyzers (#18262)</li>
<li>Remove ETW trace collection and uploading for CLR CAP (#18253)</li>
<li>Do not cleanup <code>pwsh.deps.json</code> for framework dependent packages (#18226)</li>
<li>Add branch counter to APIScan build (#18214)</li>
<li>Remove unnecessary native dependencies from the package (#18213)</li>
<li>Remove XML files for min-size package (#18189)</li>
<li>Bump Microsoft.CodeAnalysis.NetAnalyzers (#18216)</li>
<li>Bump <code>Microsoft.PowerShell.Native</code> from <code>7.3.0-preview.1</code> to <code>7.3.0-rc.1</code> (#18217)</li>
<li>Bump Microsoft.CodeAnalysis.NetAnalyzers (#18201)</li>
<li>Move ApiScan to compliance build (#18191)</li>
<li>Fix the verbose message when using <code>dotnet-install.sh</code> (#18184)</li>
<li>Bump Microsoft.NET.Test.Sdk from 17.3.1 to 17.3.2 (#18163)</li>
<li>Bump Microsoft.CodeAnalysis.NetAnalyzers (#18164)</li>
<li>Make the link to minimal package blob public during release (#18158)</li>
<li>Bump <code>Microsoft.CodeAnalysis.NetAnalyzers</code> (#18147)</li>
<li>Update MSI exit message (#18137)</li>
<li>Bump Microsoft.CodeAnalysis.CSharp from 4.4.0-1.final to 4.4.0-2.final (#18132)</li>
<li>Re-enable building with Ready-to-Run (#18105)</li>
<li>Update <code>DotnetRuntimeMetadata.json</code> for .NET 7 RC1 build (#18091)</li>
<li>Bump <code>Microsoft.CodeAnalysis.NetAnalyzers</code> (#18096)</li>
<li>Add schema for cgmanifest.json (#18036)</li>
<li>Bump <code>Microsoft.CodeAnalysis.CSharp</code> from 4.3.0-3.final to 4.3.0 (#18012)</li>
<li>Add XML reference documents to NuPkg files for SDK (#17997)</li>
<li>Bump Microsoft.NET.Test.Sdk from 17.3.0 to 17.3.1 (#18000)</li>
<li>Bump <code>Microsoft.CodeAnalysis.NetAnalyzers</code> (#17988)</li>
<li>Bump Microsoft.CodeAnalysis.NetAnalyzers (#17983)</li>
<li>Bump Microsoft.CodeAnalysis.NetAnalyzers (#17945)</li>
<li>Make sure <code>Security.types.ps1xml</code> gets signed in release build (#17916)</li>
<li>Make Register Microsoft Update timeout (#17910)</li>
<li>Merge changes from v7.0.12 v7.2.6 and v7.3.0-preview.7</li>
<li>Bump Microsoft.NET.Test.Sdk from 17.2.0 to 17.3.0 (#17871)</li>
</ul>

</details>

### Documentation and Help Content

- Update readme and metadata for releases (#18780)(#18493)(#18393)(#18332)(#18128)(#17870)
- Remove 'please' and 'Core' from README.md per MS style guide (#18578) (Thanks @Rick-Anderson!)
- Change unsupported XML documentation tag (#18608)
- Change public API mention of `monad` to PowerShell (#18491)
- Update security reporting policy to recommend security portal for more streamlined reporting (#18437)
- Change log for v7.3.0 (#18505) (Internal 23161)
- Replace `msh` in public API comment based documentation with PowerShell equivalent (#18483)
- Add missing XML doc elements for methods in `RunspaceFactory` (#18450)
- Change log for `v7.3.0-rc.1` (#18400)
- Update change logs for `v7.2.7` and `v7.0.13` (#18342)
- Update the change log for v7.3.0-preview.8 (#18136)
- Add the `ConfigurationFile` option to the PowerShell help content (#18093)
- Update help content about the PowerShell flag `-NonInteractive` (#17952)

[7.4.0-preview.1]: https://github.com/PowerShell/PowerShell/compare/v7.3.0-preview.8...v7.4.0-preview.1

